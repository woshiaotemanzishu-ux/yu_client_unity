using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Rune
{
    /// <summary>九霄劫魄十张权威服务端配置的最小运行时快照。</summary>
    public static class RuneConfigs
    {
        public readonly struct AttrValue
        {
            public int AttrId { get; }
            public long Value { get; }
            public AttrValue(int attrId, long value) { AttrId = attrId; Value = value; }
        }

        public sealed class PositionRow
        {
            public int Position;
            public int TowerFloor;
            public readonly HashSet<int> IncludedSubtypes = new HashSet<int>();
            public readonly HashSet<int> ExcludedSubtypes = new HashSet<int>();
        }

        public sealed class ExchangeRow
        {
            public int Id;
            public int RuneChip;
            public int TowerFloor;
            public int GoodsTypeId;
            public long GoodsCount;
        }

        private static readonly string[] Names =
        {
            "config_rune_pos", "config_rune_all_show", "config_rune_attr_num",
            "config_rune_attr_coefficient", "config_rune_exchange", "config_rune_wake_up",
            "config_rune_wake_up_exp", "config_rune_wake_up_lv",
            "config_rune_awake_skill", "config_rune_skill",
        };

        private static readonly Dictionary<string, JObject> Tables = new Dictionary<string, JObject>();
        private static readonly Dictionary<int, PositionRow> Positions = new Dictionary<int, PositionRow>();
        private static readonly List<ExchangeRow> Exchanges = new List<ExchangeRow>();
        private static Task _loading;

        public static bool IsLoaded { get; private set; }
        public static IReadOnlyList<ExchangeRow> ExchangeRows => Exchanges;

        public static Task EnsureLoaded() => IsLoaded ? Task.CompletedTask : (_loading ?? (_loading = LoadAsync()));

        public static bool TryGetPosition(int position, out PositionRow row) => Positions.TryGetValue(position, out row);

        public static int GetUpgradeCost(int subtype, int color, int nextLevel)
        {
            JToken row = Get("config_rune_attr_num")?[subtype + "@" + nextLevel];
            JToken coefficient = Get("config_rune_attr_coefficient")?[subtype + "@" + color];
            long raw = row?.Value<long?>("3") ?? 0L;
            long rate = coefficient?.Value<long?>("lv_up_coefficient") ?? 0L;
            return (int)Math.Max(0L, (long)Math.Round(raw * rate / 1000d));
        }

        public static long GetAwakenBonus(int color, int attrId, int awakeLevel)
        {
            if (color <= 0 || attrId <= 0 || awakeLevel <= 0) return 0L;
            JToken row = Get("config_rune_wake_up")?[color + "@" + attrId + "@" + awakeLevel];
            JArray values = ParseNestedArray(row?.Value<string>("attr"));
            for (int i = 0; i < values.Count; i++)
            {
                int id = values[i].Value<int?>("0") ?? 0;
                if (id == attrId) return values[i].Value<long?>("1") ?? 0L;
            }
            return 0L;
        }

        public static IReadOnlyList<AttrValue> GetComputedAttributes(int subtype, int color, int level)
        {
            JObject numberTable = Get("config_rune_attr_num");
            JObject coefficientTable = Get("config_rune_attr_coefficient");
            JToken numberRow = numberTable?[subtype + "@" + level];
            JToken coefficientRow = coefficientTable?[subtype + "@" + color];
            if (numberRow == null || coefficientRow == null) return Array.Empty<AttrValue>();

            var values = new Dictionary<int, long>();
            foreach (JToken item in ParseNestedArray(numberRow.Value<string>("2")))
            {
                int id = item.Value<int?>("0") ?? 0;
                if (id > 0) values[id] = item.Value<long?>("1") ?? 0L;
            }
            var result = new List<AttrValue>();
            foreach (JToken item in ParseNestedArray(coefficientRow.Value<string>("attr_coefficient_list")))
            {
                int id = item.Value<int?>("0") ?? 0;
                if (id <= 0 || !values.TryGetValue(id, out long raw)) continue;
                long coefficient = item.Value<long?>("1") ?? 0L;
                result.Add(new AttrValue(id, (long)Math.Round(raw * coefficient / 1000d)));
            }
            return result;
        }

        public static int GetSkillIdForSubtype(int subtype)
        {
            JToken row = Get("config_rune_skill")?[subtype.ToString()];
            return row?.Value<int?>("skill_id") ?? 0;
        }

        public static string GetSkillCondition(int skillId, int level)
        {
            JToken row = Get("config_rune_awake_skill")?[skillId + "@" + level];
            return row?.Value<string>("doc") ?? string.Empty;
        }

        private static JObject Get(string name) => Tables.TryGetValue(name, out JObject value) ? value : null;

        private static async Task LoadAsync()
        {
            var tasks = new Task<TextAsset>[Names.Length];
            for (int i = 0; i < Names.Length; i++)
                tasks[i] = ResManager.LoadAsync<TextAsset>(GameResPath.GetServerConfigPath(Names[i]));
            TextAsset[] assets = await Task.WhenAll(tasks);
            try
            {
                Tables.Clear();
                Positions.Clear();
                Exchanges.Clear();
                for (int i = 0; i < assets.Length; i++)
                {
                    if (assets[i] == null) throw new InvalidOperationException("missing " + Names[i]);
                    Tables[Names[i]] = JObject.Parse(assets[i].text);
                }
                ParsePositions(Get("config_rune_pos"));
                ParseExchanges(Get("config_rune_exchange"));
                IsLoaded = Tables.Count == Names.Length && Positions.Count == 10 && Exchanges.Count > 0;
                GameLog.Info("Rune", "configs tables={0} positions={1} exchanges={2} ready={3}",
                    Tables.Count, Positions.Count, Exchanges.Count, IsLoaded);
            }
            catch (Exception ex)
            {
                IsLoaded = false;
                Tables.Clear();
                Positions.Clear();
                Exchanges.Clear();
                _loading = null;
                GameLog.Error("Rune", "配置加载失败: {0}", ex);
            }
            finally
            {
                for (int i = 0; i < assets.Length; i++) if (assets[i] != null) ResManager.Release(assets[i]);
            }
        }

        private static void ParsePositions(JObject root)
        {
            if (root == null) return;
            foreach (JProperty property in root.Properties())
            {
                if (!(property.Value is JObject value)) continue;
                int position = value.Value<int?>("rune_pos") ?? 0;
                if (position <= 0) continue;
                var row = new PositionRow { Position = position };
                foreach (JToken condition in ParseNestedArray(value.Value<string>("condition")))
                {
                    string type = condition.Value<string>("0") ?? string.Empty;
                    JToken argument = condition["1"];
                    if (type == "rune_tower") row.TowerFloor = argument?.Value<int>() ?? 0;
                    else if (type == "exclude_rune_subtype") AddInts(argument, row.ExcludedSubtypes);
                    else if (type == "rune_subtype") AddInts(argument, row.IncludedSubtypes);
                }
                Positions[position] = row;
            }
        }

        private static void ParseExchanges(JObject root)
        {
            if (root == null) return;
            foreach (JProperty property in root.Properties())
            {
                if (!(property.Value is JObject value)) continue;
                var row = new ExchangeRow
                {
                    Id = value.Value<int?>("id") ?? 0,
                    RuneChip = value.Value<int?>("rune_chip_num") ?? 0,
                };
                JArray goods = ParseNestedArray(value.Value<string>("goods_list"));
                if (goods.Count > 0)
                {
                    row.GoodsTypeId = goods[0].Value<int?>("1") ?? 0;
                    row.GoodsCount = goods[0].Value<long?>("2") ?? 0L;
                }
                foreach (JToken condition in ParseNestedArray(value.Value<string>("condition")))
                    if ((condition.Value<string>("0") ?? string.Empty) == "rune_tower")
                        row.TowerFloor = condition.Value<int?>("1") ?? 0;
                if (row.Id > 0) Exchanges.Add(row);
            }
            Exchanges.Sort((left, right) => left.Id.CompareTo(right.Id));
        }

        private static JArray ParseNestedArray(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return new JArray();
            try { return JArray.Parse(raw); }
            catch { return new JArray(); }
        }

        private static void AddInts(JToken token, HashSet<int> target)
        {
            if (!(token is JArray values)) return;
            foreach (JToken value in values)
            {
                int id = value.Value<int>();
                if (id > 0) target.Add(id);
            }
        }
    }
}
