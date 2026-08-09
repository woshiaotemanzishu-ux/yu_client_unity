using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Unreal
{
    /// <summary>
    /// 九霄冥饰页面的十张权威配置闭包。这里只提供只读展示、筛选与事务前置数据；
    /// 14901/02/03/05 的资产变更仍必须等完整 GoodsModel/属性下游接通后才能开放。
    /// </summary>
    public static class UnrealConfigs
    {
        public readonly struct GoodsAmount
        {
            public readonly int Type;
            public readonly int TypeId;
            public readonly long Count;
            public GoodsAmount(int type, int typeId, long count)
            {
                Type = type;
                TypeId = typeId;
                Count = count;
            }
        }

        public sealed class AttrRow
        {
            public int GoodsId;
            public int Stage;
            public int Star;
            public long BaseRating;
            public string RecommendAttr = string.Empty;
            public string ColorAttr = string.Empty;
        }

        public sealed class LevelRow
        {
            public int Position;
            public int Level;
            public IReadOnlyList<GoodsAmount> Costs = Array.Empty<GoodsAmount>();
            public string Attr = string.Empty;
        }

        public sealed class StageRow
        {
            public int GoodsId;
            public int NewGoodsId;
            public IReadOnlyList<GoodsAmount> Costs = Array.Empty<GoodsAmount>();
        }

        public sealed class Snapshot
        {
            internal readonly Dictionary<int, AttrRow> AttrRows = new Dictionary<int, AttrRow>();
            internal readonly Dictionary<string, int> LevelMaxRows = new Dictionary<string, int>();
            internal readonly Dictionary<string, LevelRow> LevelRows = new Dictionary<string, LevelRow>();
            internal readonly Dictionary<int, StageRow> StageRows = new Dictionary<int, StageRow>();
            internal readonly SortedDictionary<int, int> StageMaxRows = new SortedDictionary<int, int>();
            internal readonly Dictionary<int, int> UnlockRows = new Dictionary<int, int>();
            internal readonly Dictionary<int, IReadOnlyList<GoodsAmount>> DecomposeRows =
                new Dictionary<int, IReadOnlyList<GoodsAmount>>();
            internal readonly Dictionary<string, JObject> SoulAttrRows = new Dictionary<string, JObject>();
            internal readonly Dictionary<int, string> PositionNames = new Dictionary<int, string>();

            public int BagCapacity { get; internal set; }
            public int AttrCount => AttrRows.Count;
            public int LevelCount => LevelRows.Count;
            public int StageCount => StageRows.Count;
            public int SoulAttrCount => SoulAttrRows.Count;
            public int PositionCount => PositionNames.Count;
            public bool IsValid => BagCapacity > 0 && AttrRows.Count > 0 && LevelRows.Count > 0
                && StageRows.Count > 0 && StageMaxRows.Count > 0 && UnlockRows.Count == 6
                && DecomposeRows.Count > 0 && SoulAttrRows.Count > 0 && PositionNames.Count == 6;

            public bool TryGetAttr(int goodsId, out AttrRow row) => AttrRows.TryGetValue(goodsId, out row);
            public bool TryGetLevel(int position, int level, out LevelRow row) =>
                LevelRows.TryGetValue(position + "@" + level, out row);
            public int GetLevelLimit(int position, int stage, int color) =>
                LevelMaxRows.TryGetValue(position + "@" + stage + "@" + color, out int value) ? value : 0;
            public bool TryGetStage(int goodsId, out StageRow row) => StageRows.TryGetValue(goodsId, out row);
            public int GetUnlockStage(int position) => UnlockRows.TryGetValue(position, out int value) ? value : -1;
            public string GetPositionName(int position) =>
                PositionNames.TryGetValue(position, out string value) ? value : string.Empty;
            public IReadOnlyList<GoodsAmount> GetDecomposeRewards(int goodsId) =>
                DecomposeRows.TryGetValue(goodsId, out IReadOnlyList<GoodsAmount> value)
                    ? value : Array.Empty<GoodsAmount>();
            public JObject GetSoulAttrRow(int subtype, int level) =>
                SoulAttrRows.TryGetValue(subtype + "@" + level, out JObject value)
                    ? (JObject)value.DeepClone() : null;

            public int GetMaxStage(int roleLevel)
            {
                int result = 0;
                foreach (KeyValuePair<int, int> row in StageMaxRows)
                {
                    if (row.Key > roleLevel) break;
                    result = row.Value;
                }
                return result;
            }
        }

        private static readonly string[] ConfigNames =
        {
            "config_decoration_kv", "config_decoration_attr", "config_decoration_level_max",
            "config_decoration_level", "config_decoration_stage", "config_decoration_stage_max",
            "config_dec_unlock_cell", "config_goods_decompose", "config_soul_attr_num", "goodssubtype",
        };

        private static Task _loading;
        public static Snapshot Current { get; private set; }
        public static bool IsLoaded => Current != null && Current.IsValid;
        public static Task EnsureLoaded() => IsLoaded ? Task.CompletedTask : (_loading ?? (_loading = LoadAsync()));

        /// <summary>供 CliVerify/静态工具使用；不会覆盖运行时 Current。</summary>
        public static Snapshot ParseForValidation(IReadOnlyDictionary<string, string> jsonByName)
        {
            if (jsonByName == null) throw new ArgumentNullException(nameof(jsonByName));
            var roots = new Dictionary<string, JObject>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < ConfigNames.Length; i++)
            {
                string name = ConfigNames[i];
                if (!jsonByName.TryGetValue(name, out string json) || string.IsNullOrWhiteSpace(json))
                    throw new InvalidOperationException("缺少九霄冥饰配置: " + name);
                roots[name] = JObject.Parse(json);
            }
            return Parse(roots);
        }

        private static async Task LoadAsync()
        {
            var tasks = new Task<TextAsset>[ConfigNames.Length];
            for (int i = 0; i < ConfigNames.Length; i++)
                tasks[i] = ResManager.LoadAsync<TextAsset>(GameResPath.GetServerConfigPath(ConfigNames[i]));
            TextAsset[] assets = await Task.WhenAll(tasks);
            try
            {
                var roots = new Dictionary<string, JObject>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < assets.Length; i++)
                {
                    if (assets[i] == null) throw new InvalidOperationException("缺少九霄冥饰配置: " + ConfigNames[i]);
                    roots[ConfigNames[i]] = JObject.Parse(assets[i].text);
                }
                Snapshot parsed = Parse(roots);
                if (!parsed.IsValid) throw new InvalidOperationException(
                    $"九霄冥饰配置不完整: bag={parsed.BagCapacity},attr={parsed.AttrCount},level={parsed.LevelCount},stage={parsed.StageCount},pos={parsed.PositionCount}");
                Current = parsed;
                GameLog.Info("Unreal", "configs attr={0},level={1},stage={2},positions={3}",
                    parsed.AttrCount, parsed.LevelCount, parsed.StageCount, parsed.PositionCount);
            }
            catch (Exception e)
            {
                Current = null;
                _loading = null;
                GameLog.Error("Unreal", "配置加载失败: {0}", e);
            }
            finally
            {
                for (int i = 0; i < assets.Length; i++) if (assets[i] != null) ResManager.Release(assets[i]);
            }
        }

        private static Snapshot Parse(IReadOnlyDictionary<string, JObject> roots)
        {
            var result = new Snapshot();
            JObject kv = roots["config_decoration_kv"];
            result.BagCapacity = Math.Max(0, ReadInt(kv["1"] as JObject, "value"));

            foreach (JProperty p in roots["config_decoration_attr"].Properties())
            {
                if (!(p.Value is JObject row)) continue;
                int id = ReadInt(row, "goods_id");
                if (id <= 0) continue;
                result.AttrRows[id] = new AttrRow
                {
                    GoodsId = id,
                    Stage = Math.Max(0, ReadInt(row, "stage")),
                    Star = Math.Max(0, ReadInt(row, "star")),
                    BaseRating = Math.Max(0L, ReadLong(row, "base_rating")),
                    RecommendAttr = ReadString(row, "recommend_attr"),
                    ColorAttr = ReadString(row, "color_attr"),
                };
            }

            foreach (JProperty p in roots["config_decoration_level_max"].Properties())
            {
                if (!(p.Value is JObject row)) continue;
                result.LevelMaxRows[p.Name] = Math.Max(0, ReadInt(row, "limit_level"));
            }
            foreach (JProperty p in roots["config_decoration_level"].Properties())
            {
                if (!(p.Value is JObject row)) continue;
                int position = ReadInt(row, "0");
                int level = ReadInt(row, "1");
                if (position <= 0 || level < 0) continue;
                result.LevelRows[p.Name] = new LevelRow
                {
                    Position = position,
                    Level = level,
                    Costs = ParseAmounts(ReadString(row, "2")),
                    Attr = ReadString(row, "3"),
                };
            }
            foreach (JProperty p in roots["config_decoration_stage"].Properties())
            {
                if (!(p.Value is JObject row)) continue;
                int id = ReadInt(row, "goods_id");
                if (id <= 0) continue;
                result.StageRows[id] = new StageRow
                {
                    GoodsId = id,
                    NewGoodsId = Math.Max(0, ReadInt(row, "new_goods_id")),
                    Costs = ParseAmounts(ReadString(row, "cost")),
                };
            }
            foreach (JProperty p in roots["config_decoration_stage_max"].Properties())
            {
                if (!(p.Value is JObject row)) continue;
                int level = ReadInt(row, "player_level");
                if (level >= 0) result.StageMaxRows[level] = Math.Max(0, ReadInt(row, "limit_stage"));
            }
            foreach (JProperty p in roots["config_dec_unlock_cell"].Properties())
            {
                if (!(p.Value is JObject row)) continue;
                int cell = ReadInt(row, "equip_cell");
                if (cell > 0) result.UnlockRows[cell] = Math.Max(0, ReadInt(row, "unlock_stage"));
            }
            foreach (JProperty p in roots["config_goods_decompose"].Properties())
            {
                if (!(p.Value is JObject row) || !int.TryParse(p.Name, out int id)) continue;
                result.DecomposeRows[id] = ParseAmounts(ReadString(row, "5"));
            }
            foreach (JProperty p in roots["config_soul_attr_num"].Properties())
                if (p.Value is JObject row) result.SoulAttrRows[p.Name] = (JObject)row.DeepClone();
            foreach (JProperty p in roots["goodssubtype"].Properties())
            {
                if (!(p.Value is JObject row) || ReadInt(row, "type") != 55) continue;
                int subtype = ReadInt(row, "subtype");
                if (subtype >= 1 && subtype <= UnrealController.EquipCellCount)
                    result.PositionNames[subtype] = ReadString(row, "subtype_name");
            }
            return result;
        }

        private static IReadOnlyList<GoodsAmount> ParseAmounts(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return Array.Empty<GoodsAmount>();
            var result = new List<GoodsAmount>();
            foreach (JToken token in JArray.Parse(raw))
            {
                if (!(token is JObject row)) continue;
                int typeId = ReadInt(row, "1");
                long count = ReadLong(row, "2");
                if (typeId > 0 && count > 0) result.Add(new GoodsAmount(ReadInt(row, "0"), typeId, count));
            }
            return result.AsReadOnly();
        }

        private static int ReadInt(JObject row, string key) => row?.Value<int?>(key) ?? 0;
        private static long ReadLong(JObject row, string key) => row?.Value<long?>(key) ?? 0L;
        private static string ReadString(JObject row, string key) => row?.Value<string>(key) ?? string.Empty;
    }
}
