using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Skill
{
    /// <summary>
    /// Passive-skill display configuration matching the legacy client list semantics.
    /// Entries retain the original JSON row order and are never sorted by skill ID.
    /// </summary>
    public static class SkillPassiveConfigs
    {
        public sealed class PassiveSkillCfg
        {
            public int DunId;
            public int SkillId;
            public int TaskId;
        }

        private static JObject _root;
        private static readonly Dictionary<int, List<PassiveSkillCfg>> _careerCache =
            new Dictionary<int, List<PassiveSkillCfg>>();

        public static bool IsLoaded => _root != null;

        public static async Task EnsureLoaded()
        {
            if (_root != null) return;

            string key = GameResPath.GetServerConfigPath("config_dungeon_learn_skill");
            TextAsset asset = await ResManager.LoadAsync<TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("Skill", "missing config_dungeon_learn_skill: {0}", key);
                _root = new JObject();
                return;
            }

            try
            {
                _root = JObject.Parse(asset.text);
                _careerCache.Clear();
            }
            catch (System.Exception ex)
            {
                GameLog.Error("Skill", "invalid config_dungeon_learn_skill: {0} ({1})", key, ex.Message);
                _root = new JObject();
            }
            finally
            {
                ResManager.Release(asset);
            }
        }

        /// <summary>Returns passive skills and unlock tasks in original configuration order.</summary>
        public static List<PassiveSkillCfg> GetForCareer(int career)
        {
            if (_root == null || career <= 0) return new List<PassiveSkillCfg>();
            if (_careerCache.TryGetValue(career, out List<PassiveSkillCfg> cached)) return cached;

            var result = new List<PassiveSkillCfg>();
            foreach (JProperty row in _root.Properties())
            {
                if (!(row.Value is JObject rowObj))
                {
                    GameLog.Warn("Skill", "skip malformed config_dungeon_learn_skill row {0}", row.Name);
                    continue;
                }

                int dunId = ReadInt(rowObj["dun_id"]);
                int taskId = ReadInt(rowObj["task_id"]);
                if (dunId <= 0 || taskId <= 0)
                {
                    GameLog.Warn("Skill", "skip invalid learn-skill row {0}: dun_id={1}, task_id={2}",
                        row.Name, dunId, taskId);
                    continue;
                }

                string rawSkillList = rowObj.Value<string>("skill_list");
                if (string.IsNullOrEmpty(rawSkillList))
                {
                    GameLog.Warn("Skill", "skip empty skill_list in learn-skill row {0}", row.Name);
                    continue;
                }

                JArray skillList;
                try
                {
                    skillList = JToken.Parse(rawSkillList) as JArray;
                }
                catch (System.Exception ex)
                {
                    GameLog.Warn("Skill", "skip invalid skill_list in row {0}: {1}", row.Name, ex.Message);
                    continue;
                }

                if (skillList == null)
                {
                    GameLog.Warn("Skill", "skip non-array skill_list in row {0}", row.Name);
                    continue;
                }

                foreach (JToken skillInfoToken in skillList)
                {
                    if (!(skillInfoToken is JObject skillInfo))
                    {
                        GameLog.Warn("Skill", "skip malformed career entry in row {0}", row.Name);
                        continue;
                    }

                    if (ReadInt(skillInfo["0"]) != career) continue;
                    if (!(skillInfo["1"] is JArray careerSkills)
                        || careerSkills.Count == 0
                        || !(careerSkills[0] is JObject skillEntry))
                    {
                        GameLog.Warn("Skill", "skip malformed skill entry in row {0}, career {1}",
                            row.Name, career);
                        continue;
                    }

                    int skillId = ReadInt(skillEntry["0"]);
                    if (skillId <= 0)
                    {
                        GameLog.Warn("Skill", "skip invalid skill id in row {0}, career {1}",
                            row.Name, career);
                        continue;
                    }

                    result.Add(new PassiveSkillCfg
                    {
                        DunId = dunId,
                        SkillId = skillId,
                        TaskId = taskId,
                    });
                }
            }

            _careerCache[career] = result;
            return result;
        }

        private static int ReadInt(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null) return 0;
            return token.Type == JTokenType.Integer
                ? token.Value<int>()
                : int.TryParse(token.ToString(), out int value) ? value : 0;
        }
    }
}
