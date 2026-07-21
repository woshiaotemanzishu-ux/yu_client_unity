using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Bag;
using UnityEngine;

namespace Shenxiao.Module.Core.Baby
{
    /// <summary>宝宝装备铭刻配置和纯本地预览；服务端才是最终扣料与概率裁决者。</summary>
    public static class BabyEquipEngraveConfigs
    {
        public sealed class EngraveCfg { public int Color; public int GoodsId; public int Num; public int Ratio; }
        public sealed class CostItem { public int TypeId; public int Num; }
        public sealed class PreviewResult
        {
            public bool Valid;
            public bool Enough;
            public int Ratio;
            public readonly List<CostItem> Costs = new List<CostItem>();
        }

        private static Dictionary<string, EngraveCfg> _byKey;
        private static Dictionary<int, List<EngraveCfg>> _byColor;
        private static Task _loading;
        public static bool IsLoaded => _byKey != null;

        public static Task EnsureLoaded()
        {
            if (IsLoaded) return Task.CompletedTask;
            if (_loading == null || _loading.IsCompleted) _loading = LoadAsync();
            return _loading;
        }

        public static EngraveCfg Get(int color, int goodsTypeId)
            => _byKey != null && _byKey.TryGetValue(Key(color, goodsTypeId), out EngraveCfg cfg) ? cfg : null;

        public static IReadOnlyList<EngraveCfg> GetColorCandidates(int color)
            => _byColor != null && _byColor.TryGetValue(color, out List<EngraveCfg> values) ? values : Empty;

        public static PreviewResult Preview(BabyEquipEntry entry, IReadOnlyList<int> selectedTypeIds)
        {
            var result = new PreviewResult();
            if (!IsLoaded || entry == null || entry.SkillId > 0 || selectedTypeIds == null || selectedTypeIds.Count == 0) return result;
            BabyEquipConfigs.EquipCfg equip = BabyEquipConfigs.Get(entry.GoodsTypeId);
            if (equip == null || equip.Color <= 0) return result;
            var totals = new Dictionary<int, int>();
            int ratio = 0;
            for (int i = 0; i < selectedTypeIds.Count; i++)
            {
                int typeId = selectedTypeIds[i];
                EngraveCfg cfg = Get(equip.Color, typeId);
                if (typeId <= 0 || cfg == null || cfg.Num <= 0) return result;
                totals[typeId] = totals.TryGetValue(typeId, out int num) ? num + cfg.Num : cfg.Num;
                ratio = System.Math.Min(10000, ratio + System.Math.Max(0, cfg.Ratio));
            }
            result.Valid = true;
            result.Ratio = ratio;
            result.Enough = true;
            foreach (KeyValuePair<int, int> pair in totals)
            {
                result.Costs.Add(new CostItem { TypeId = pair.Key, Num = pair.Value });
                if (BagModel.Instance.GetTypeGoodsNum(pair.Key) < pair.Value) result.Enough = false;
            }
            return result;
        }

        private static readonly IReadOnlyList<EngraveCfg> Empty = new List<EngraveCfg>();
        private static async Task LoadAsync()
        {
            var byKey = new Dictionary<string, EngraveCfg>();
            var byColor = new Dictionary<int, List<EngraveCfg>>();
            TextAsset asset = null;
            try
            {
                asset = await ResManager.LoadAsync<TextAsset>(GameResPath.GetServerConfigPath("config_baby_equip_engrave"));
                if (asset == null)
                {
                    GameLog.Warn("Baby", "missing baby equip engrave config");
                }
                else foreach (KeyValuePair<string, JToken> pair in JObject.Parse(asset.text))
                {
                    if (!(pair.Value is JObject row)) continue;
                    var cfg = new EngraveCfg { Color = Read(row, "color"), GoodsId = Read(row, "goods_id"), Num = Read(row, "num"), Ratio = Read(row, "ratio") };
                    if (cfg.Color <= 0 || cfg.GoodsId <= 0 || cfg.Num <= 0 || pair.Key != Key(cfg.Color, cfg.GoodsId)) continue;
                    byKey[pair.Key] = cfg;
                    if (!byColor.TryGetValue(cfg.Color, out List<EngraveCfg> list)) byColor[cfg.Color] = list = new List<EngraveCfg>();
                    list.Add(cfg);
                }
            }
            catch (System.Exception e) { GameLog.Warn("Baby", "parse baby equip engrave config failed: {0}", e.Message); }
            finally { if (asset != null) ResManager.Release(asset); }
            _byKey = byKey;
            _byColor = byColor;
        }

        private static string Key(int color, int goodsId) => color + "@" + goodsId;
        private static int Read(JObject row, string key) => int.TryParse(row?[key]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : 0;
    }
}
