using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Medal
{
    /// <summary>
    /// 天境称号配置。键严格使用 config_title_cfg 的 id@star，展示顺序按 id；
    /// 激活、升星结果仍只认 13403/13405 权威回包。
    /// </summary>
    public static class TitleConfigs
    {
        public sealed class AttributeValue
        {
            public int Id { get; internal set; }
            public long Value { get; internal set; }
        }

        public sealed class CostValue
        {
            public int Type { get; internal set; }
            public int TypeId { get; internal set; }
            public long Count { get; internal set; }
        }

        public sealed class Row
        {
            public uint Id { get; internal set; }
            public ushort Star { get; internal set; }
            public string Name { get; internal set; } = string.Empty;
            public string Description { get; internal set; } = string.Empty;
            public int ShowId { get; internal set; }
            public IReadOnlyList<AttributeValue> Attributes { get; internal set; }
                = Array.Empty<AttributeValue>();
            public IReadOnlyList<CostValue> Costs { get; internal set; }
                = Array.Empty<CostValue>();
        }

        private static IReadOnlyDictionary<string, Row> _rows;
        private static IReadOnlyList<uint> _titleIds = Array.Empty<uint>();
        private static Task _loading;

        public static bool IsLoaded => _rows != null && _rows.Count > 0 && _titleIds.Count > 0;
        public static int Count => _rows?.Count ?? 0;
        public static IReadOnlyList<uint> TitleIds => _titleIds;

        public static Task EnsureLoaded()
        {
            if (IsLoaded) return Task.CompletedTask;
            if (_loading == null || _loading.IsCompleted) _loading = LoadAsync();
            return _loading;
        }

        public static Row Get(uint id, ushort star)
            => _rows != null && _rows.TryGetValue(Key(id, star), out Row row) ? row : null;

        public static Row GetFirst(uint id) => Get(id, 0);

        public static Row GetNext(uint id, ushort star)
        {
            if (star == ushort.MaxValue) return null;
            return Get(id, (ushort)(star + 1));
        }

        public static string EffectName(int showId)
            => showId > 0 ? "effect_shenmingjiemian_" + showId.ToString("00") : string.Empty;

        public static IReadOnlyDictionary<string, Row> ParseForValidation(string json)
            => ParseRows(JObject.Parse(json));

        private static async Task LoadAsync()
        {
            TextAsset asset = await ResManager.LoadAsync<TextAsset>(
                GameResPath.GetServerConfigPath("config_title_cfg"));
            if (asset == null)
            {
                GameLog.Error("Title", "缺少 config_title_cfg，天境拒绝展示伪数据");
                _loading = null;
                return;
            }

            try
            {
                IReadOnlyDictionary<string, Row> parsed = ParseRows(JObject.Parse(asset.text));
                uint[] ids = parsed.Values.Select(row => row.Id).Distinct().OrderBy(id => id).ToArray();
                if (parsed.Count == 0 || ids.Length == 0)
                {
                    GameLog.Error("Title", "config_title_cfg 无有效行");
                    return;
                }
                _rows = parsed;
                _titleIds = ids;
                GameLog.Info("Title", "config_title_cfg rows={0}, titles={1}", parsed.Count, ids.Length);
            }
            catch (Exception e)
            {
                GameLog.Error("Title", "config_title_cfg 解析失败: {0}", e);
            }
            finally
            {
                ResManager.Release(asset);
                if (!IsLoaded) _loading = null;
            }
        }

        private static IReadOnlyDictionary<string, Row> ParseRows(JObject root)
        {
            var rows = new SortedDictionary<string, Row>(StringComparer.Ordinal);
            foreach (JProperty property in root.Properties())
            {
                if (!(property.Value is JObject value)) continue;
                long rawId = value.Value<long?>("id") ?? 0L;
                int rawStar = value.Value<int?>("star") ?? -1;
                if (rawId <= 0 || rawId > uint.MaxValue || rawStar < 0 || rawStar > ushort.MaxValue)
                    continue;

                var row = new Row
                {
                    Id = (uint)rawId,
                    Star = (ushort)rawStar,
                    Name = value.Value<string>("name") ?? string.Empty,
                    Description = value.Value<string>("desc") ?? string.Empty,
                    ShowId = ParseShowId(value["show_id"]),
                    Attributes = ParseAttributes(value.Value<string>("attr")),
                    Costs = ParseCosts(value.Value<string>("cost")),
                };
                rows[Key(row.Id, row.Star)] = row;
            }
            return rows;
        }

        private static int ParseShowId(JToken token)
        {
            if (token == null) return 0;
            return int.TryParse(token.ToString(), out int value) ? Math.Max(0, value) : 0;
        }

        private static IReadOnlyList<AttributeValue> ParseAttributes(string raw)
        {
            var values = new List<AttributeValue>();
            foreach (JToken token in ParseArray(raw))
            {
                if (!(token is JObject item)) continue;
                int id = item.Value<int?>("0") ?? 0;
                long value = item.Value<long?>("1") ?? 0L;
                if (id > 0) values.Add(new AttributeValue { Id = id, Value = value });
            }
            return values;
        }

        private static IReadOnlyList<CostValue> ParseCosts(string raw)
        {
            var values = new List<CostValue>();
            foreach (JToken token in ParseArray(raw))
            {
                if (!(token is JObject item)) continue;
                int typeId = item.Value<int?>("1") ?? 0;
                long count = item.Value<long?>("2") ?? 0L;
                if (typeId <= 0 || count <= 0) continue;
                values.Add(new CostValue
                {
                    Type = item.Value<int?>("0") ?? 0,
                    TypeId = typeId,
                    Count = count,
                });
            }
            return values;
        }

        private static JArray ParseArray(string raw)
            => string.IsNullOrWhiteSpace(raw) ? new JArray() : JArray.Parse(raw);

        private static string Key(uint id, ushort star) => id + "@" + star;
    }
}
