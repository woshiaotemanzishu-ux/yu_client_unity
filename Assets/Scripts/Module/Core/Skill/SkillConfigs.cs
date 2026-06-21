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
