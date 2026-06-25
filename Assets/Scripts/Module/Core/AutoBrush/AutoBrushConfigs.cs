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
            public int Coin;
            public int ExpAdd;
            public string OtherRewardList = "";
            public string OtherGoodsPool = "";
        }

        private static JObject _boss;
        private static JObject _client;
        private static readonly Dictionary<int, BossCfg> _bossCache = new Dictionary<int, BossCfg>();

        public static bool IsLoaded => _boss != null && _client != null;
        public static int MaxLevel { get; private set; }

        public static async Task EnsureLoaded()
        {
            if (_boss != null && _client != null) return;

            _boss = await LoadConfig(GameResPath.GetServerConfigPath("config_enchantment_guard_boss"),
                "config_enchantment_guard_boss");
            _client = await LoadConfig(GameResPath.GetClientConfigPath("configautobrush"),
                "ConfigAutoBrush");
            MaxLevel = ReadInt(_client, "max_level");
            if (MaxLevel > 0) AutoBrushModel.Instance.SetMaxLevel(MaxLevel);
        }

        public static BossCfg GetBoss(int level)
        {
            if (level <= 0 || _boss == null) return null;
            if (_bossCache.TryGetValue(level, out BossCfg cached)) return cached;
            if (!(_boss[level.ToString()] is JObject obj)) return null;

            BossCfg cfg = new BossCfg
            {
                Id = ReadInt(obj, "0"),
                Coin = ReadInt(obj, "2"),
                ExpAdd = ReadInt(obj, "5"),
                OtherRewardList = ReadString(obj, "6"),
                OtherGoodsPool = ReadString(obj, "7"),
            };
            _bossCache[level] = cfg;
            return cfg;
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
