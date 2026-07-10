using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Skill
{
    /// <summary>
    /// 客户端 ConfigSkillUI 访问层(对标老端 Config.PRELOAD_CLIENT_CONFIG.ConfigSkillUI.carrerSkillList[career])。
    /// carrerSkillList: { "1":[{skill_id,common?,stren}], "2":[...], ... } —— 每职业的技能槽顺序,首项 common=1 是普攻。
    /// shortcutList = 去掉 common 的那几项,按 skill_id 升序;决定首屏技能 4 槽的内容与顺序。
    /// 客户端散表(地址小写约定),进游戏后由 <see cref="SkillController"/> 预载。
    /// </summary>
    public static class SkillUIConfigs
    {
        public struct CareerSkill
        {
            public int SkillId;
            public bool Common;
        }

        private static JObject _root;
        private static JObject _carrerList;
        private static JObject _innateSkill;

        public static bool IsLoaded => _root != null;

        public static async Task EnsureLoaded()
        {
            if (_root != null) return;

            string key = GameResPath.GetClientConfigPath("configskillui");
            TextAsset asset = await ResManager.LoadAsync<TextAsset>(key);
            if (asset == null)
            {
                GameLog.Warn("Skill", "missing ConfigSkillUI: {0}(跑 神霄/配表/同步客户端配置 同步 ConfigSkillUI;缺则 shortcutList 回落 21002 全表)", key);
                _root = new JObject();
                return;
            }

            _root = JObject.Parse(asset.text);
            _carrerList = _root["carrerSkillList"] as JObject;
            _innateSkill = _root["innateSkill"] as JObject;
            ResManager.Release(asset);
        }

        /// <summary>取某职业的技能槽列表(原始顺序,含 common 普攻项;调用方按需过滤)。</summary>
        public static List<CareerSkill> GetCareerSkills(int career)
        {
            var list = new List<CareerSkill>();
            if (_carrerList?[career.ToString()] is JArray arr)
            {
                foreach (JToken t in arr)
                {
                    if (t is JObject o)
                    {
                        list.Add(new CareerSkill
                        {
                            SkillId = o.Value<int?>("skill_id") ?? 0,
                            Common = (o.Value<int?>("common") ?? 0) == 1,
                        });
                    }
                }
            }
            return list;
        }

        // ===================== innateSkill:天赋技能面板布局配置(对标老端 ConfigSkillUI.innateSkill[type]) =====================
        // 真实数据形状(实测 Assets/GameRes/resource/config/client/configskillui.json):
        //   innateSkill = { "5":{...}, "6":{...}, "7":{...}, "8":{...} }(5攻击/6防守/7通用/8绝对,与服务端 21010 SkillType 一致)
        //   每个 type 对象: "1".."8"(行号,可缺)→ 该行技能位数组,元素要么是裸 skillId(number),要么是按职业变体的 [id1,id2,...] 数组;
        //                  "pos_"+行号 → 与该行位数组等长的 {x,y} 坐标数组; "name" → 分支中文名; "open"(可选)→ {is_open?,open_lv?,turn?}。

        /// <summary>全部天赋分支 type,升序(对标老端 InnateSkillView.ts 的 table.sort(list_,...))。</summary>
        public static List<int> GetInnateTypesSorted()
        {
            var list = new List<int>();
            if (_innateSkill != null)
            {
                foreach (JProperty prop in _innateSkill.Properties())
                    if (int.TryParse(prop.Name, out int t)) list.Add(t);
            }
            list.Sort();
            return list;
        }

        /// <summary>分支中文名(如"攻击"/"防御"/"通用"/"绝对"),供 InnateUpCondItem "XX系投入" 文案用。</summary>
        public static string GetInnateTypeName(int type)
            => _innateSkill?[type.ToString()] is JObject o ? (o.Value<string>("name") ?? "") : "";

        /// <summary>某分支的开启条件(对标老端 RefSelType 的 ui_cfg[type]["open"] 遍历)。无 "open" 键 = 恒开放(HasCond=false)。</summary>
        public struct InnateOpenCond
        {
            public bool HasCond;
            public bool HasIsOpenFlag;
            public bool IsOpen;
            public bool HasLevelReq;
            public int OpenLv;
            public bool HasTurnReq;
            public int Turn;
        }

        public static InnateOpenCond GetInnateOpen(int type)
        {
            var result = default(InnateOpenCond);
            if (_innateSkill?[type.ToString()] is JObject o && o["open"] is JObject openObj)
            {
                result.HasCond = true;
                if (openObj["is_open"] != null) { result.HasIsOpenFlag = true; result.IsOpen = openObj.Value<bool>("is_open"); }
                if (openObj["open_lv"] != null) { result.HasLevelReq = true; result.OpenLv = openObj.Value<int>("open_lv"); }
                if (openObj["turn"] != null) { result.HasTurnReq = true; result.Turn = openObj.Value<int>("turn"); }
            }
            return result;
        }

        /// <summary>一个天赋技能树坐标槽(已按职业解析出真实 skillId)。</summary>
        public struct InnateSlot
        {
            public int SkillId;
            public float X;
            public float Y;
        }

        /// <summary>某分支全部技能槽位(对标老端 InnateListItem.ts:InitView 的 "1".."5" 行遍历 + "pos_"+i 配对;
        /// 行号上限放宽到 8 对齐 type8 实测最多 5 行)。career 用于按职业解析变体位([career-1]),1 起始。</summary>
        public static List<InnateSlot> GetInnateSlots(int type, int career)
        {
            var list = new List<InnateSlot>();
            if (!(_innateSkill?[type.ToString()] is JObject o)) return list;

            for (int row = 1; row <= 8; row++)
            {
                if (!(o[row.ToString()] is JArray vo)) continue;
                JArray pos = o["pos_" + row] as JArray;
                for (int j = 0; j < vo.Count; j++)
                {
                    int skillId = ResolveInnateSkillId(vo[j], career);
                    if (skillId <= 0) continue;
                    float x = 0f, y = 0f;
                    if (pos != null && j < pos.Count && pos[j] is JObject p)
                    {
                        x = p.Value<float?>("x") ?? 0f;
                        y = p.Value<float?>("y") ?? 0f;
                    }
                    list.Add(new InnateSlot { SkillId = skillId, X = x, Y = y });
                }
            }
            return list;
        }

        /// <summary>对标老端 SkillUIModel.GetInnateSkillIdByCfg:裸数字/字符串直接就是 skillId;数组按职业变体取
        /// cfg[career-1](越界回退首项)。</summary>
        private static int ResolveInnateSkillId(JToken cfg, int career)
        {
            if (cfg is JArray arr)
            {
                int idx = career - 1;
                if (idx >= 0 && idx < arr.Count) return arr[idx].Value<int?>() ?? 0;
                return arr.Count > 0 ? (arr[0].Value<int?>() ?? 0) : 0;
            }
            return cfg?.Value<int?>() ?? 0;
        }
    }
}
