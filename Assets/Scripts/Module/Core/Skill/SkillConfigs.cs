using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Skill
{
    /// <summary>
    /// 服务端 config_skill 访问层(对标老端 SkillManager.getSkillFromConfig:Config.PRELOAD_SERVER_CONFIG.config_skill[id])。
    /// 表以技能 id(字符串键)索引,字段 name/career/type/is_normal + lv_data(每级一个 JSON 串,含 icon/lv_name/desc/cd)。
    /// lv_data 是每行内嵌的 JSON 字符串,按需(取某技能某级图标时)二次解析并缓存,避免一次性展开 2MB 全表。
    /// 进游戏后由 <see cref="SkillController"/> 预载(EnsureLoaded 幂等,对标 TaskConfigs/NpcConfigs)。
    /// </summary>
    public static class SkillConfigs
    {
        private static JObject _skill;
        private static readonly Dictionary<int, JArray> _lvCache = new Dictionary<int, JArray>();

        public static bool IsLoaded => _skill != null;

        public static async Task EnsureLoaded()
        {
            if (_skill != null) return;

            string key = GameResPath.GetServerConfigPath("config_skill");
            TextAsset asset = await ResManager.LoadAsync<TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("Skill", "missing config_skill: {0}(跑 神霄/配表/同步客户端配置 同步 config_skill)", key);
                _skill = new JObject();
                return;
            }

            _skill = JObject.Parse(asset.text);
            _lvCache.Clear();
            ResManager.Release(asset);
        }

        /// <summary>技能是否在 config_skill 中(对标 getSkillFromConfig != null,用于 21002 过滤合法技能)。</summary>
        public static bool Has(int skillId)
            => _skill != null && _skill[skillId.ToString()] is JObject;

        /// <summary>技能名(config_skill[id].name)。</summary>
        public static string GetName(int skillId)
            => _skill?[skillId.ToString()] is JObject o ? (o.Value<string>("name") ?? "") : "";

        /// <summary>是否普攻(config_skill[id].is_normal==1)。无 ConfigSkillUI 时用于剔除普攻槽。</summary>
        public static bool IsNormal(int skillId)
            => _skill?[skillId.ToString()] is JObject o && (o.Value<int?>("is_normal") ?? 0) == 1;

        /// <summary>职业(config_skill[id].career,对标 SkillVo.getCarrer)。52=伙伴技能。</summary>
        public static int GetCareer(int skillId)
            => _skill?[skillId.ToString()] is JObject o ? (o.Value<int?>("career") ?? 0) : 0;

        /// <summary>选取模式(config_skill[id].obj,对标 SkillVo.GetSelectType:1自己 2最近敌方 3最近队友)。</summary>
        public static int GetSelectType(int skillId)
            => _skill?[skillId.ToString()] is JObject o ? (o.Value<int?>("obj") ?? 0) : 0;

        /// <summary>技能类型(config_skill[id].type,对标 SkillVo.GetSkillType:1主动 2辅助 3被动 4门派)。</summary>
        public static int GetSkillType(int skillId)
            => _skill?[skillId.ToString()] is JObject o ? (o.Value<int?>("type") ?? 0) : 0;

        /// <summary>攻击参照类型(config_skill[id].att_obj,对标 SkillVo.GetSkillAttObj:1自己 2选取对象)。</summary>
        public static int GetAttObj(int skillId)
            => _skill?[skillId.ToString()] is JObject o ? (o.Value<int?>("att_obj") ?? 0) : 0;

        /// <summary>
        /// 是否群攻(AOE)模式(逐字段对标 SkillVo.IsAoeMode:config_skill[id].mod==1 → 单体非 AOE,
        /// 其余值/缺省 → AOE)。单体技能的 20001 目标列表=[主目标],可逐字段确定;AOE 需 distance/area/num
        /// + FindTargets 几何收集链(未移植),本轮 AOE 不发包,见 SceneCombat。
        /// </summary>
        public static bool IsAoe(int skillId)
        {
            if (_skill?[skillId.ToString()] is JObject o)
            {
                int? mod = o.Value<int?>("mod");
                if (mod.HasValue) return mod.Value != 1; // mod==1 → 单体;否则 AOE(对标老端 Number(mod)==1?false:true)
            }
            return true; // mod 缺失/空 → 老端 is_aoe_mode=true
        }

        /// <summary>攻击距离(对标 SkillVo.GetDistance:lv_data[level-1].distance,缺省 50)。用于真实攻击范围判定。</summary>
        public static int GetDistanceForLevel(int skillId, int level)
        {
            JArray lv = GetLvData(skillId);
            int idx = (level > 0 ? level : 1) - 1;
            if (lv != null && idx >= 0 && idx < lv.Count && lv[idx] is JObject lvo)
            {
                int? dist = lvo.Value<int?>("distance");
                if (dist.HasValue && dist.Value > 0) return dist.Value;
            }
            return 50;
        }

        /// <summary>攻击范围半径(对标 SkillVo.GetArea:lv_data[level-1].area,缺省 0)。圆形 AOE 收集半径来源。</summary>
        public static int GetAreaForLevel(int skillId, int level)
        {
            JArray lv = GetLvData(skillId);
            int idx = (level > 0 ? level : 1) - 1;
            if (lv != null && idx >= 0 && idx < lv.Count && lv[idx] is JObject lvo)
                return lvo.Value<int?>("area") ?? 0;
            return 0;
        }

        /// <summary>AOE 选取方式(对标 SkillVo.GetAoeMode:config_skill[id].range;1圆形 2直线 3扇形,0/缺省→1)。</summary>
        public static int GetAoeMode(int skillId)
        {
            if (_skill?[skillId.ToString()] is JObject o)
            {
                int? range = o.Value<int?>("range");
                if (range.HasValue && range.Value != 0) return range.Value;
            }
            return 1;
        }

        /// <summary>攻击目标数量(对标 SkillVo.GetAttackNum:lv_data[level-1].num=[玩家数, 怪物数],缺省 [0,0])。
        /// 0 表示不限(对标老端 att_num==0 → 99)。怪物数用于圆形 AOE 收集上限。</summary>
        public static int[] GetAttackNumForLevel(int skillId, int level)
        {
            JArray lv = GetLvData(skillId);
            int idx = (level > 0 ? level : 1) - 1;
            if (lv != null && idx >= 0 && idx < lv.Count && lv[idx] is JObject lvo
                && lvo["num"] is JArray num && num.Count >= 2)
            {
                return new[] { num[0].Value<int>(), num[1].Value<int>() };
            }
            return new[] { 0, 0 };
        }

        /// <summary>
        /// 普攻 combo 链的"下一跳副技能 + 发包延迟"(对标服务端 mod_battle.erl 的 is_att=0 engage 帧 + combo 机制):
        /// config_skill[id].combo 是一段 JSON 串 = <c>[{"0":skillId,"1":time(ms),"2":_}, ...]</c>。给定当前(engage)
        /// 技能 id,在链里定位它,返回 <b>紧随其后的副技能 id</b> 与 <b>当前元素的 time(发副技能的延迟,毫秒)</b>。
        /// 无 combo 串 / 已是链尾 → 返回 (0,0)。
        ///
        /// 背景(第14轮根因):普攻主技能(如 御剑一式 59100001)<c>is_att=0 / calc=0</c>,服务端
        /// <c>mod_battle.erl</c> 的 <c>is_att=0</c> 分支只回一个 <c>damage=0</c> 的"进战斗 engage 帧"(NoHurtDerList);
        /// 真正扣血的是它的 combo 副技能(如 59100002)<c>is_att=1 / calc=1</c>,走真实伤害结算。老端 fight-movie 在
        /// comboSkills 时点对同目标补发副技能 20001;本端据此 combo 链补发,闭合真实伤害链(副技能 id 从配置读,不 hardcode)。
        /// </summary>
        public static (int comboSkillId, int delayMs) GetComboNext(int skillId)
        {
            if (_skill?[skillId.ToString()] is JObject o)
            {
                string raw = o.Value<string>("combo");
                if (!string.IsNullOrEmpty(raw) && raw != "[]")
                {
                    try
                    {
                        JArray arr = JArray.Parse(raw);
                        for (int i = 0; i < arr.Count; i++)
                        {
                            if (arr[i] is JObject e && (e.Value<int?>("0") ?? 0) == skillId)
                            {
                                int delay = e.Value<int?>("1") ?? 0;
                                if (i + 1 < arr.Count && arr[i + 1] is JObject nx)
                                    return (nx.Value<int?>("0") ?? 0, delay);
                                return (0, 0); // 链尾:无后续副技能
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        GameLog.Warn("Skill", "parse combo failed skill={0}: {1}", skillId, ex.Message);
                    }
                }
            }
            return (0, 0);
        }

        /// <summary>取某级图标资源名(对标 SkillVo.GetIcon:lv_data[level-1].icon,缺省回落技能 id)。</summary>
        public static string GetIconForLevel(int skillId, int level)
        {
            JArray lv = GetLvData(skillId);
            int idx = level - 1;
            if (lv != null && idx >= 0 && idx < lv.Count && lv[idx] is JObject lvo)
            {
                string icon = lvo.Value<string>("icon");
                if (!string.IsNullOrEmpty(icon)) return icon;
            }
            return skillId.ToString();
        }

        // lv_data 是字符串里再裹一层 JSON 数组,按技能 id 懒解析 + 缓存。
        private static JArray GetLvData(int skillId)
        {
            if (_lvCache.TryGetValue(skillId, out JArray cached)) return cached;

            JArray arr = null;
            if (_skill?[skillId.ToString()] is JObject o)
            {
                string raw = o.Value<string>("lv_data");
                if (!string.IsNullOrEmpty(raw))
                {
                    try
                    {
                        arr = JArray.Parse(raw);
                    }
                    catch (System.Exception ex)
                    {
                        GameLog.Warn("Skill", "parse lv_data failed skill={0}: {1}", skillId, ex.Message);
                        arr = null;
                    }
                }
            }
            _lvCache[skillId] = arr;
            return arr;
        }
    }
}
