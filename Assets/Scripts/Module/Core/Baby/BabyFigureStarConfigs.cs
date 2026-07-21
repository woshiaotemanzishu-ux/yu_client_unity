using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Baby
{
    public static class BabyFigureStarConfigs
    {
        public sealed class CostItem { public int Type; public long TypeId; public long Num; }
        public sealed class AttrItem { public int AttrId; public long Value; }
        public sealed class BabyFigureStarCfg
        {
            public int BabyId;
            public int Star;
            public readonly List<CostItem> Costs = new List<CostItem>();
            public readonly List<AttrItem> BaseAttrs = new List<AttrItem>();
            public int Power;
        }

        private static Dictionary<long, BabyFigureStarCfg> _byKey;
        private static Task _loading;
        public static bool IsLoaded => _byKey != null;

        public static Task EnsureLoaded()
        {
            if (_byKey != null) return Task.CompletedTask;
            if (_loading == null || _loading.IsCompleted) _loading = LoadAsync();
            return _loading;
        }

        public static BabyFigureStarCfg Get(int babyId, int star)
            => _byKey != null && _byKey.TryGetValue(Key(babyId, star), out BabyFigureStarCfg cfg) ? cfg : null;

        private static async Task LoadAsync()
        {
            var byKey = new Dictionary<long, BabyFigureStarCfg>();
            string key = GameResPath.GetServerConfigPath("config_baby_figure_star");
            TextAsset asset = await ResManager.LoadAsync<TextAsset>(key);
            if (asset == null)
            {
                GameLog.Warn("Baby", "missing baby figure star config: {0}", key);
                _byKey = byKey;
                return;
            }
            try
            {
                JObject root = JObject.Parse(asset.text);
                foreach (KeyValuePair<string, JToken> pair in root)
                {
                    if (!(pair.Value is JObject row)) continue;
                    int babyId = ReadInt(row, "baby_id");
                    int star = ReadInt(row, "star");
                    if (babyId <= 0 || star <= 0) continue;
                    var cfg = new BabyFigureStarCfg { BabyId = babyId, Star = star, Power = ReadInt(row, "power") };
                    ParseCosts(row["cost"]?.ToString(), cfg.Costs);
                    ParseAttrs(row["base_attr"]?.ToString(), cfg.BaseAttrs);
                    byKey[Key(babyId, star)] = cfg;
                }
            }
            catch (System.Exception e) { GameLog.Warn("Baby", "parse baby figure star config failed: {0}", e.Message); }
            finally { ResManager.Release(asset); }
            _byKey = byKey;
        }

        private static long Key(int babyId, int star) => ((long)babyId << 32) | (uint)star;
        private static int ReadInt(JObject row, string key) => int.TryParse(row[key]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : 0;
        private static long ReadLong(JObject row, string key) => long.TryParse(row[key]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long value) ? value : 0;
        private static void ParseCosts(string raw, List<CostItem> items)
        {
            if (string.IsNullOrEmpty(raw)) return;
            foreach (JToken token in JArray.Parse(raw))
                if (token is JObject item)
                    items.Add(new CostItem
                    {
                        Type = ReadInt(item, "0"),
                        TypeId = ReadLong(item, "1"),
                        Num = ReadLong(item, "2"),
                    });
        }

        private static void ParseAttrs(string raw, List<AttrItem> items)
        {
            if (string.IsNullOrEmpty(raw)) return;
            foreach (JToken token in JArray.Parse(raw))
                if (token is JObject item)
                    items.Add(new AttrItem
                    {
                        AttrId = ReadInt(item, "0"),
                        Value = ReadLong(item, "1"),
                    });
        }
    }
}
