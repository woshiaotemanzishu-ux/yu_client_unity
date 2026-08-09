using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.AutoBrush
{
    public static class AutoBrushConfigs
    {
        public sealed class BossCfg
        {
            public int Id;
            public int Power;
            public int Coin;
            public int ExpAdd;
            public string OtherRewardList = "";
            public string OtherGoodsPool = "";
        }

        /// <summary>主线副本大妖的客户端模型覆盖(对标老端 Monster.InitAutoBrushMoster:res/name/scene_scale)。</summary>
        public sealed class BrushBossModel
        {
            public int Res;
            public string Name = "";
            public float SceneScale;
        }

        public sealed class StageRewardCfg
        {
            public int Gate;
            public AutoBrushModel.RewardEntry Reward;
        }

        private static JObject _boss;
        private static JObject _client;
        private static JObject _stageRewards;
        private static readonly Dictionary<int, BossCfg> _bossCache = new Dictionary<int, BossCfg>();

        public static bool IsLoaded => _boss != null && _client != null && _stageRewards != null;
        public static int MaxLevel { get; private set; }

        public static async Task EnsureLoaded()
        {
            if (IsLoaded) return;

            _boss = await LoadConfig(GameResPath.GetServerConfigPath("config_enchantment_guard_boss"),
                "config_enchantment_guard_boss");
            _client = await LoadConfig(GameResPath.GetClientConfigPath("configautobrush"),
                "ConfigAutoBrush");
            _stageRewards = await LoadConfig(GameResPath.GetServerConfigPath("config_enchantment_guard_stage_reward"),
                "config_enchantment_guard_stage_reward");
            MaxLevel = ReadInt(_client, "max_level");
            if (MaxLevel > 0) AutoBrushModel.Instance.SetMaxLevel(MaxLevel);
        }

        public static StageRewardCfg GetStageReward(ulong gate)
        {
            if (_stageRewards == null) return null;
            JObject obj = gate > 0 && gate <= int.MaxValue
                ? _stageRewards[gate.ToString()] as JObject
                : null;
            if (obj == null && gate == 0)
            {
                int maxGate = 0;
                foreach (JProperty property in _stageRewards.Properties())
                {
                    if (int.TryParse(property.Name, out int value) && value > maxGate
                        && property.Value is JObject candidate)
                    {
                        maxGate = value;
                        obj = candidate;
                    }
                }
            }
            if (obj == null) return null;

            JArray rewards = ParseArray(ReadString(obj, "reward"));
            if (rewards == null || rewards.Count == 0) return null;
            JToken reward = rewards[0];
            return new StageRewardCfg
            {
                Gate = ReadInt(obj, "gate"),
                Reward = new AutoBrushModel.RewardEntry(
                    ReadIndexedInt(reward, 0), ReadIndexedInt(reward, 1), ReadIndexedLong(reward, 2)),
            };
        }

        public static BossCfg GetBoss(int level)
        {
            if (level <= 0 || _boss == null) return null;
            if (_bossCache.TryGetValue(level, out BossCfg cached)) return cached;
            if (!(_boss[level.ToString()] is JObject obj)) return null;

            BossCfg cfg = new BossCfg
            {
                Id = ReadInt(obj, "0"),
                Power = ReadInt(obj, "1"),
                Coin = ReadInt(obj, "2"),
                ExpAdd = ReadInt(obj, "5"),
                OtherRewardList = ReadString(obj, "6"),
                OtherGoodsPool = ReadString(obj, "7"),
            };
            _bossCache[level] = cfg;
            return cfg;
        }

        /// <summary>
        /// 主线副本大妖(怪 type=<see cref="AutoBrushModel.AutoBrushMonsterId"/>=7001,服务端只下发占位 "主线副本")
        /// 的真实模型/名字/缩放,由客户端按当前挂机层级从 ConfigAutoBrush 决定(对标老端
        /// Monster.InitAutoBrushMoster,yu_client/h5/src/scene/sceneobj/Monster.ts:685-709)。
        /// next_level = 当前层 + 1;优先 level_boss_cfg[next_level],否则 turn_boss_cfg[next_level % 10]。
        /// </summary>
        public static BrushBossModel GetBrushBossModel(int nextLevel)
        {
            if (_client == null || nextLevel <= 0) return null;

            JObject cfg = null;
            if (_client["level_boss_cfg"] is JObject levelCfg && levelCfg[nextLevel.ToString()] is JObject byLevel)
            {
                cfg = byLevel;
            }
            if (cfg == null && _client["turn_boss_cfg"] is JObject turnCfg)
            {
                int index = nextLevel % 10;
                if (turnCfg[index.ToString()] is JObject byTurn) cfg = byTurn;
            }
            if (cfg == null) return null;

            return new BrushBossModel
            {
                Res = ReadInt(cfg, "res"),
                Name = ReadString(cfg, "name"),
                SceneScale = ReadFloat(cfg, "scene_scale", 0f),
            };
        }

        public static List<AutoBrushModel.RewardEntry> BuildBossRewards(int level, int career)
        {
            var rewards = new List<AutoBrushModel.RewardEntry>();
            BossCfg cfg = GetBoss(level);
            if (cfg == null) return rewards;

            if (cfg.Coin > 0) rewards.Add(new AutoBrushModel.RewardEntry(3, 0, cfg.Coin));
            AppendCareerRewards(rewards, cfg.OtherRewardList, career);
            AppendRewardPool(rewards, cfg.OtherGoodsPool);
            return rewards;
        }

        public static string BuildExpText(int level, bool done)
        {
            BossCfg current = GetBoss(done ? level : level - 1);
            BossCfg next = done ? null : GetBoss(level);

            long currentValue = current != null ? GetHourValue(current.ExpAdd) : 0;
            string text = ConvertValue(currentValue);
            if (next != null)
            {
                long diff = GetHourValue(next.ExpAdd) - currentValue;
                if (diff > 0) text += " +" + ConvertValue(diff);
            }
            return text;
        }

        public static long GetHourValue(int fiveSecondValue) => fiveSecondValue * 720L;

        public static string ConvertValue(long value)
        {
            if (value > 10000000L)
            {
                return (value / 10000f).ToString("0.##") + "万/时";
            }
            return value + "/时";
        }

        private static async Task<JObject> LoadConfig(string key, string label)
        {
            TextAsset asset = await ResManager.LoadAsync<TextAsset>(key);
            if (asset == null)
            {
                GameLog.Warn("AutoBrush", "missing {0}: {1}", label, key);
                return new JObject();
            }

            JObject obj = JObject.Parse(asset.text);
            ResManager.Release(asset);
            return obj;
        }

        private static void AppendCareerRewards(List<AutoBrushModel.RewardEntry> rewards, string json, int career)
        {
            if (string.IsNullOrWhiteSpace(json)) return;
            JArray arr = ParseArray(json);
            if (arr == null) return;

            foreach (JToken entry in arr)
            {
                if (ReadIndexedInt(entry, 0) != career) continue;
                if (!(ReadIndexedToken(entry, 1) is JArray subRewards)) continue;
                foreach (JToken reward in subRewards) AppendReward(rewards, reward);
            }
        }

        private static void AppendRewardPool(List<AutoBrushModel.RewardEntry> rewards, string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return;
            JArray arr = ParseArray(json);
            if (arr == null) return;
            foreach (JToken reward in arr) AppendReward(rewards, reward);
        }

        private static void AppendReward(List<AutoBrushModel.RewardEntry> rewards, JToken reward)
        {
            int style = ReadIndexedInt(reward, 0);
            int typeId = ReadIndexedInt(reward, 1);
            long count = ReadIndexedLong(reward, 2);
            if (count > 0) rewards.Add(new AutoBrushModel.RewardEntry(style, typeId, count));
        }

        private static JArray ParseArray(string json)
        {
            try { return JArray.Parse(json); }
            catch (System.Exception e)
            {
                GameLog.Warn("AutoBrush", "reward json parse failed: {0}", e.Message);
                return null;
            }
        }

        private static JToken ReadIndexedToken(JToken token, int index)
        {
            if (token is JObject obj) return obj[index.ToString()];
            if (token is JArray arr && index >= 0 && index < arr.Count) return arr[index];
            return null;
        }

        private static int ReadIndexedInt(JToken token, int index)
            => ReadInt(ReadIndexedToken(token, index));

        private static long ReadIndexedLong(JToken token, int index)
            => ReadLong(ReadIndexedToken(token, index));

        private static int ReadInt(JObject obj, string key) => obj == null ? 0 : ReadInt(obj[key]);

        private static int ReadInt(JToken token)
        {
            if (token == null) return 0;
            if (token.Type == JTokenType.Integer) return token.Value<int>();
            return int.TryParse(token.ToString(), out int value) ? value : 0;
        }

        private static float ReadFloat(JObject obj, string key, float fallback)
        {
            JToken token = obj?[key];
            if (token == null || token.Type == JTokenType.Null) return fallback;
            if (token.Type == JTokenType.Float || token.Type == JTokenType.Integer) return token.Value<float>();
            return float.TryParse(token.ToString(), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out float value) ? value : fallback;
        }

        private static long ReadLong(JToken token)
        {
            if (token == null) return 0;
            if (token.Type == JTokenType.Integer) return token.Value<long>();
            return long.TryParse(token.ToString(), out long value) ? value : 0;
        }

        private static string ReadString(JObject obj, string key)
        {
            JToken token = obj?[key];
            return token == null || token.Type == JTokenType.Null ? "" : token.ToString();
        }
    }
}
