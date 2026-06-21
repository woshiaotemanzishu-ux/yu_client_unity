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
    }
}
