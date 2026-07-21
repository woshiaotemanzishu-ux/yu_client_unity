using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Baby
{
    public static class BabyEquipConfigs
    {
        public sealed class EquipCfg
        {
            public int GoodsId;
            public int PosId;
            public int Color;
            public int EquipStage;
            public readonly List<int> Skills = new List<int>();
        }

        private static Dictionary<int, EquipCfg> _byGoods;
        private static readonly IReadOnlyList<EquipCfg> Empty = new List<EquipCfg>();
        private static IReadOnlyList<EquipCfg> _all = Empty;
        private static Task _loading;

        public static bool IsLoaded => _byGoods != null;
        public static IReadOnlyList<EquipCfg> All => _all;

        public static Task EnsureLoaded()
        {
            if (IsLoaded) return Task.CompletedTask;
            if (_loading == null || _loading.IsCompleted) _loading = LoadAsync();
            return _loading;
        }

        public static EquipCfg Get(int goodsTypeId)
            => _byGoods != null && _byGoods.TryGetValue(goodsTypeId, out EquipCfg cfg) ? cfg : null;

        public static bool CanWear(int goodsTypeId, int posId, int babyStage)
        {
            EquipCfg cfg = Get(goodsTypeId);
            return cfg != null && posId >= 1 && posId <= 6 && cfg.PosId == posId && babyStage >= cfg.EquipStage;
        }

        private static async Task LoadAsync()
        {
            var byGoods = new Dictionary<int, EquipCfg>();
            string key = GameResPath.GetServerConfigPath("config_baby_equip");
            TextAsset asset = await ResManager.LoadAsync<TextAsset>(key);
            if (asset == null)
            {
                GameLog.Warn("Baby", "missing baby equip config: {0}", key);
                _byGoods = byGoods;
                _all = Empty;
                return;
            }

            try
            {
                foreach (KeyValuePair<string, JToken> pair in JObject.Parse(asset.text))
                {
                    if (!(pair.Value is JObject row)) continue;
                    var cfg = new EquipCfg
                    {
                        GoodsId = Read(row, "goods_id"),
                        PosId = Read(row, "pos_id"),
                        Color = Read(row, "color"),
                        EquipStage = Read(row, "equip_stage")
                    };
                    if (cfg.GoodsId <= 0) continue;
                    ParseSkills(row["skills"], cfg.Skills);
                    byGoods[cfg.GoodsId] = cfg;
                }
            }
            catch (System.Exception e)
            {
                GameLog.Warn("Baby", "parse baby equip config failed: {0}", e.Message);
            }
            finally
            {
                ResManager.Release(asset);
            }

            _all = new List<EquipCfg>(byGoods.Values).AsReadOnly();
            _byGoods = byGoods;
        }

        private static int Read(JObject row, string key)
            => int.TryParse(row[key]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                ? value : 0;

        private static void ParseSkills(JToken token, List<int> skills)
        {
            if (token == null) return;
            string raw = token.ToString();
            if (raw.StartsWith("["))
            {
                foreach (JToken item in JArray.Parse(raw))
                {
                    if (int.TryParse(item.ToString(), out int value) && value > 0) skills.Add(value);
                }
            }
            else if (int.TryParse(raw, out int value) && value > 0)
            {
                skills.Add(value);
            }
        }
    }
}
