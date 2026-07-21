using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Baby
{
    public static class BabyFigureConfigs
    {
        public sealed class BabyFigureCfg
        {
            public int BabyId;
            public string BabyName = "";
            public string ResourceId = "";
            public int ActiveStage;
            public int Power;
        }

        private static Dictionary<int, BabyFigureCfg> _byBabyId;
        private static readonly IReadOnlyList<BabyFigureCfg> Empty = new List<BabyFigureCfg>();
        private static IReadOnlyList<BabyFigureCfg> _all = Empty;
        private static Task _loading;

        public static bool IsLoaded => _byBabyId != null;
        public static IReadOnlyList<BabyFigureCfg> All => _all;

        public static Task EnsureLoaded()
        {
            if (_byBabyId != null) return Task.CompletedTask;
            if (_loading == null || _loading.IsCompleted) _loading = LoadAsync();
            return _loading;
        }

        public static BabyFigureCfg Get(int babyId)
            => _byBabyId != null && _byBabyId.TryGetValue(babyId, out BabyFigureCfg cfg) ? cfg : null;

        private static async Task LoadAsync()
        {
            var byBabyId = new Dictionary<int, BabyFigureCfg>();
            string key = GameResPath.GetServerConfigPath("config_baby_figure");
            TextAsset asset = await ResManager.LoadAsync<TextAsset>(key);
            if (asset == null)
            {
                GameLog.Warn("Baby", "missing baby figure config: {0}", key);
                _byBabyId = byBabyId;
                _all = Empty;
                return;
            }

            try
            {
                JObject root = JObject.Parse(asset.text);
                foreach (KeyValuePair<string, JToken> pair in root)
                {
                    if (!(pair.Value is JObject row)) continue;
                    int babyId = ReadInt(row, "baby_id");
                    if (babyId <= 0) continue;
                    byBabyId[babyId] = new BabyFigureCfg
                    {
                        BabyId = babyId,
                        BabyName = row["baby_name_con"]?.ToString() ?? "",
                        ResourceId = row["resource_id"]?.ToString() ?? "",
                        ActiveStage = ReadInt(row, "active_stage"),
                        Power = ReadInt(row, "power"),
                    };
                }
            }
            catch (System.Exception e)
            {
                GameLog.Warn("Baby", "parse baby figure config failed: {0}", e.Message);
            }
            finally
            {
                ResManager.Release(asset);
            }

            var all = new List<BabyFigureCfg>(byBabyId.Values);
            all.Sort((a, b) => a.BabyId.CompareTo(b.BabyId));
            _all = all.AsReadOnly();
            _byBabyId = byBabyId;
        }

        private static int ReadInt(JObject row, string key)
            => int.TryParse(row[key]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                ? value : 0;
    }
}
