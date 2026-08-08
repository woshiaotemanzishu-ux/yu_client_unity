using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.AttributePotion
{
    /// <summary>属性药剂页面与 217 使用裁剪的统一配置源。</summary>
    public static class AttributePotionConfigs
    {
        public sealed class Attr { public int Id; public long Value; }

        public sealed class Potion
        {
            public int GoodsId;
            public byte Level;
            public readonly List<Attr> Attrs = new List<Attr>();
        }

        public sealed class Limit
        {
            public int GoodsId;
            public int MinRoleLevel;
            public int MaxRoleLevel;
            public uint DayTimes;
            public ulong AllTimes;
        }

        public sealed class FirstGuide
        {
            public string Text;
            public int Direction;
            public float EffectScaleX = 1f;
            public float EffectScaleY = 1f;
            public float EffectScaleZ = 1f;
        }

        private static readonly Dictionary<int, Potion> Potions = new Dictionary<int, Potion>();
        private static readonly Dictionary<byte, List<Potion>> PotionsByLevel = new Dictionary<byte, List<Potion>>();
        private static readonly List<Limit> Limits = new List<Limit>();
        private static Task _loading;

        public static bool IsLoaded { get; private set; }
        public static int PotionCount => Potions.Count;
        public static int LimitCount => Limits.Count;
        public static FirstGuide Guide { get; private set; }

        public static Task EnsureLoaded() => IsLoaded ? Task.CompletedTask : (_loading ?? (_loading = LoadAsync()));

        private static async Task LoadAsync()
        {
            UnityEngine.TextAsset potionAsset = await ResManager.LoadAsync<UnityEngine.TextAsset>(
                GameResPath.GetServerConfigPath("config_attr_medicament"));
            UnityEngine.TextAsset limitAsset = await ResManager.LoadAsync<UnityEngine.TextAsset>(
                GameResPath.GetServerConfigPath("config_attr_medicament_use_count"));
            UnityEngine.TextAsset guideAsset = await ResManager.LoadAsync<UnityEngine.TextAsset>(
                GameResPath.GetClientConfigPath("ConfigFuncFirstOpenArrow"));

            Potions.Clear();
            PotionsByLevel.Clear();
            Limits.Clear();
            Guide = null;

            if (potionAsset != null)
            {
                foreach (JProperty property in JObject.Parse(potionAsset.text).Properties())
                {
                    if (!(property.Value is JObject row)) continue;
                    int goodsId = row.Value<int?>("good_id") ?? 0;
                    int level = row.Value<int?>("lv") ?? 0;
                    if (goodsId <= 0 || level <= 0 || level > byte.MaxValue) continue;

                    var potion = new Potion { GoodsId = goodsId, Level = (byte)level };
                    ParseAttrs(row.Value<string>("attr_list"), potion.Attrs);
                    Potions[goodsId] = potion;
                }

                foreach (IGrouping<byte, Potion> group in Potions.Values.GroupBy(x => x.Level))
                    PotionsByLevel[group.Key] = group.OrderBy(x => x.GoodsId).ToList();
            }

            if (limitAsset != null)
            {
                foreach (JProperty property in JObject.Parse(limitAsset.text).Properties())
                {
                    if (!(property.Value is JObject row)) continue;
                    int goodsId = row.Value<int?>("good_id") ?? 0;
                    if (goodsId <= 0) continue;
                    Limits.Add(new Limit
                    {
                        GoodsId = goodsId,
                        MinRoleLevel = row.Value<int?>("min_role_lv") ?? 0,
                        MaxRoleLevel = row.Value<int?>("max_role_lv") ?? 0,
                        DayTimes = (uint)(row.Value<long?>("day_times") ?? 0L),
                        AllTimes = (ulong)(row.Value<long?>("all_times") ?? 0L),
                    });
                }
            }

            ParseFirstGuide(guideAsset);
            if (potionAsset != null) ResManager.Release(potionAsset);
            if (limitAsset != null) ResManager.Release(limitAsset);
            if (guideAsset != null) ResManager.Release(guideAsset);

            IsLoaded = true;
            GameLog.Info("AttributePotion", "configs potion={0}, use_count={1}, guide={2}",
                Potions.Count, Limits.Count, Guide != null);
        }

        private static void ParseAttrs(string raw, ICollection<Attr> result)
        {
            if (string.IsNullOrWhiteSpace(raw)) return;
            try
            {
                foreach (JToken token in JArray.Parse(raw))
                {
                    if (!(token is JObject row)) continue;
                    int id = row.Value<int?>("0") ?? 0;
                    long value = row.Value<long?>("1") ?? 0L;
                    if (id > 0) result.Add(new Attr { Id = id, Value = value });
                }
            }
            catch (Exception e)
            {
                GameLog.Warn("AttributePotion", "attr_list parse failed: {0}", e.Message);
            }
        }

        private static void ParseFirstGuide(UnityEngine.TextAsset asset)
        {
            if (asset == null) return;
            try
            {
                JObject root = JObject.Parse(asset.text);
                if (!(root["1"] is JObject potion)
                    || !(potion["steps"] is JArray steps)
                    || !(steps.First is JObject step)) return;
                Guide = new FirstGuide
                {
                    Text = step.Value<string>("text") ?? string.Empty,
                    Direction = step.Value<int?>("direction") ?? 6,
                    EffectScaleX = step.Value<float?>("effect_scaleX") ?? 1f,
                    EffectScaleY = step.Value<float?>("effect_scaleY") ?? 1f,
                    EffectScaleZ = step.Value<float?>("effect_scaleZ") ?? 1f,
                };
            }
            catch (Exception e)
            {
                GameLog.Warn("AttributePotion", "ConfigFuncFirstOpenArrow parse failed: {0}", e.Message);
            }
        }

        public static bool TryGetPotion(int goodsId, out Potion row) => Potions.TryGetValue(goodsId, out row);

        public static IReadOnlyList<Potion> GetPotions(byte level)
            => PotionsByLevel.TryGetValue(level, out List<Potion> rows) ? rows : Array.Empty<Potion>();

        public static bool HasPotionLevel(byte level) => PotionsByLevel.ContainsKey(level);

        public static bool TryGetLimit(int goodsId, int roleLevel, out Limit row)
        {
            for (int i = 0; i < Limits.Count; i++)
            {
                Limit candidate = Limits[i];
                if (candidate.GoodsId == goodsId
                    && roleLevel >= candidate.MinRoleLevel
                    && roleLevel <= candidate.MaxRoleLevel)
                {
                    row = candidate;
                    return true;
                }
            }
            row = null;
            return false;
        }
    }
}
