using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Baby
{
    public static class BabyPraiseConfigs
    {
        public sealed class RewardItem
        {
            public int Type;
            public long TypeId;
            public long Num;
        }

        public sealed class PraiseCfg
        {
            public int Rank1;
            public int Rank2;
            public readonly List<RewardItem> Rewards = new List<RewardItem>();
        }

        private static readonly IReadOnlyList<PraiseCfg> Empty = new List<PraiseCfg>();
        private static IReadOnlyList<PraiseCfg> _all = Empty;
        private static Task _loading;

        public static bool IsLoaded { get; private set; }
        public static IReadOnlyList<PraiseCfg> All => _all;

        public static Task EnsureLoaded()
        {
            if (IsLoaded) return Task.CompletedTask;
            if (_loading == null || _loading.IsCompleted) _loading = LoadAsync();
            return _loading;
        }

        public static PraiseCfg GetByRank(int rank)
        {
            if (rank <= 0) return null;
            for (int i = 0; i < _all.Count; i++)
            {
                PraiseCfg cfg = _all[i];
                if (rank >= cfg.Rank1 && rank <= cfg.Rank2) return cfg;
            }
            return null;
        }

        private static async Task LoadAsync()
        {
            var all = new List<PraiseCfg>();
            string key = GameResPath.GetServerConfigPath("config_baby_praise");
            TextAsset asset = await ResManager.LoadAsync<TextAsset>(key);
            if (asset == null)
            {
                GameLog.Warn("Baby", "missing baby praise config: {0}", key);
                _all = Empty;
                IsLoaded = true;
                return;
            }

            try
            {
                JObject root = JObject.Parse(asset.text);
                foreach (KeyValuePair<string, JToken> pair in root)
                {
                    if (!(pair.Value is JObject row)) continue;
                    var cfg = new PraiseCfg
                    {
                        Rank1 = ReadInt(row, "rank1"),
                        Rank2 = ReadInt(row, "rank2")
                    };
                    if (cfg.Rank1 <= 0 || cfg.Rank2 < cfg.Rank1) continue;
                    ParseRewards(row["reward"]?.ToString(), cfg.Rewards);
                    all.Add(cfg);
                }
            }
            catch (System.Exception e)
            {
                GameLog.Warn("Baby", "parse baby praise config failed: {0}", e.Message);
            }
            finally
            {
                ResManager.Release(asset);
            }

            _all = all.AsReadOnly();
            IsLoaded = true;
        }

        private static int ReadInt(JObject row, string key)
            => int.TryParse(row[key]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                ? value : 0;

        private static long ReadLong(JObject row, string key)
            => long.TryParse(row[key]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long value)
                ? value : 0;

        private static void ParseRewards(string raw, List<RewardItem> rewards)
        {
            if (string.IsNullOrEmpty(raw)) return;
            foreach (JToken token in JArray.Parse(raw))
            {
                if (!(token is JObject item)) continue;
                rewards.Add(new RewardItem
                {
                    Type = ReadInt(item, "0"),
                    TypeId = ReadLong(item, "1"),
                    Num = ReadLong(item, "2")
                });
            }
        }
    }
}
