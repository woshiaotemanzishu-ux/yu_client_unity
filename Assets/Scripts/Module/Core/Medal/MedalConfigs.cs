using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Medal
{
    /// <summary>
    /// 地境勋章的只读配置与升级预检。最终扣料、升阶结果始终以 13402 权威回包为准。
    /// </summary>
    public static class MedalConfigs
    {
        public enum ConditionType
        {
            Power,
            Layer,
            Item,
        }

        public enum UpgradeBlock
        {
            None,
            ConfigNotReady,
            SnapshotNotReady,
            MissingCurrentConfig,
            MaxLevel,
            LayerNotEnough,
            MaterialNotEnough,
            PowerNotEnough,
        }

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
            public string MedalName { get; internal set; } = string.Empty;
            public int LargeImageId { get; internal set; }
            public int SmallImageId { get; internal set; }
            public int Star { get; internal set; }
            public long UpgradePower { get; internal set; }
            public int RequiredLayer { get; internal set; }
            public string Title { get; internal set; } = string.Empty;
            public IReadOnlyList<AttributeValue> Attributes { get; internal set; }
                = Array.Empty<AttributeValue>();
            public IReadOnlyList<CostValue> Costs { get; internal set; }
                = Array.Empty<CostValue>();
        }

        public sealed class ConditionState
        {
            public ConditionType Type { get; internal set; }
            public int ItemTypeId { get; internal set; }
            public long Current { get; internal set; }
            public long Required { get; internal set; }
            public bool Enough => Current >= Required;
        }

        public sealed class UpgradePreview
        {
            public Row Current { get; internal set; }
            public Row Next { get; internal set; }
            public UpgradeBlock Block { get; internal set; }
            public IReadOnlyList<ConditionState> Conditions { get; internal set; }
                = Array.Empty<ConditionState>();
            public bool CanUpgrade => Block == UpgradeBlock.None;
            public bool ShouldJump => Block == UpgradeBlock.LayerNotEnough;
            public bool IsMax => Block == UpgradeBlock.MaxLevel;
        }

        private static IReadOnlyDictionary<uint, Row> _rows;
        private static Task _loading;

        public static bool IsLoaded => _rows != null && _rows.Count > 0;
        public static int Count => _rows?.Count ?? 0;

        public static Task EnsureLoaded()
        {
            if (IsLoaded) return Task.CompletedTask;
            if (_loading == null || _loading.IsCompleted) _loading = LoadAsync();
            return _loading;
        }

        public static Row Get(uint id)
            => _rows != null && _rows.TryGetValue(id, out Row row) ? row : null;

        /// <summary>服务端 id=0 表示尚未激活；老端固定用配置 id=1 展示与预检。</summary>
        public static uint ResolveCurrentId(uint serverId) => serverId == 0 ? 1u : serverId;

        public static UpgradePreview Evaluate(uint serverId, bool hasSnapshot,
            long rolePower, uint passLayers, Func<int, long> itemCounter)
            => Evaluate(_rows, serverId, hasSnapshot, rolePower, passLayers, itemCounter);

        /// <summary>公开纯函数重载供 CliVerify 使用，不依赖 Addressables 或在线状态。</summary>
        public static UpgradePreview Evaluate(IReadOnlyDictionary<uint, Row> rows,
            uint serverId, bool hasSnapshot, long rolePower, uint passLayers,
            Func<int, long> itemCounter)
        {
            var preview = new UpgradePreview();
            if (rows == null || rows.Count == 0)
            {
                preview.Block = UpgradeBlock.ConfigNotReady;
                return preview;
            }
            if (!hasSnapshot)
            {
                preview.Block = UpgradeBlock.SnapshotNotReady;
                return preview;
            }

            uint currentId = ResolveCurrentId(serverId);
            if (!rows.TryGetValue(currentId, out Row current) || current == null)
            {
                preview.Block = UpgradeBlock.MissingCurrentConfig;
                return preview;
            }
            preview.Current = current;
            rows.TryGetValue(currentId + 1u, out Row next);
            preview.Next = next;
            if (next == null)
            {
                preview.Block = UpgradeBlock.MaxLevel;
                return preview;
            }

            var conditions = new List<ConditionState>();
            if (current.UpgradePower > 0)
            {
                conditions.Add(new ConditionState
                {
                    Type = ConditionType.Power,
                    Current = Math.Max(0L, rolePower),
                    Required = current.UpgradePower,
                });
            }
            if (current.RequiredLayer > 0)
            {
                conditions.Add(new ConditionState
                {
                    Type = ConditionType.Layer,
                    Current = passLayers,
                    Required = current.RequiredLayer,
                });
            }
            for (int i = 0; i < current.Costs.Count; i++)
            {
                CostValue cost = current.Costs[i];
                if (cost == null || cost.TypeId <= 0 || cost.Count <= 0) continue;
                long have = itemCounter != null ? itemCounter(cost.TypeId) : 0L;
                conditions.Add(new ConditionState
                {
                    Type = ConditionType.Item,
                    ItemTypeId = cost.TypeId,
                    Current = Math.Max(0L, have),
                    Required = cost.Count,
                });
            }
            preview.Conditions = conditions;

            // 点击语义严格对标老端：层数不足优先变成“前往挑战”；否则再拦材料、战力。
            if (current.RequiredLayer > 0 && passLayers < current.RequiredLayer)
                preview.Block = UpgradeBlock.LayerNotEnough;
            else if (HasFailed(conditions, ConditionType.Item))
                preview.Block = UpgradeBlock.MaterialNotEnough;
            else if (HasFailed(conditions, ConditionType.Power))
                preview.Block = UpgradeBlock.PowerNotEnough;
            else
                preview.Block = UpgradeBlock.None;
            return preview;
        }

        /// <summary>公开解析入口仅返回独立只读表，不会污染运行时缓存。</summary>
        public static IReadOnlyDictionary<uint, Row> ParseForValidation(string json)
            => ParseRows(JObject.Parse(json));

        private static bool HasFailed(IReadOnlyList<ConditionState> values, ConditionType type)
        {
            for (int i = 0; i < values.Count; i++)
                if (values[i].Type == type && !values[i].Enough) return true;
            return false;
        }

        private static async Task LoadAsync()
        {
            TextAsset asset = await ResManager.LoadAsync<TextAsset>(
                GameResPath.GetServerConfigPath("config_medal"));
            if (asset == null)
            {
                GameLog.Error("Medal", "缺少 config_medal，地境勋章拒绝展示伪数据");
                _loading = null;
                return;
            }
            try
            {
                IReadOnlyDictionary<uint, Row> parsed = ParseRows(JObject.Parse(asset.text));
                if (parsed.Count == 0)
                {
                    GameLog.Error("Medal", "config_medal 无有效行");
                    return;
                }
                _rows = parsed;
                GameLog.Info("Medal", "config_medal rows={0}", parsed.Count);
            }
            catch (Exception e)
            {
                GameLog.Error("Medal", "config_medal 解析失败: {0}", e);
            }
            finally
            {
                ResManager.Release(asset);
                if (!IsLoaded) _loading = null;
            }
        }

        private static IReadOnlyDictionary<uint, Row> ParseRows(JObject root)
        {
            var result = new SortedDictionary<uint, Row>();
            foreach (JProperty property in root.Properties())
            {
                if (!(property.Value is JObject value)) continue;
                long rawId = value.Value<long?>("id") ?? 0L;
                if (rawId <= 0 || rawId > uint.MaxValue) continue;
                var row = new Row
                {
                    Id = (uint)rawId,
                    MedalName = value.Value<string>("medal_name") ?? string.Empty,
                    LargeImageId = Math.Max(0, value.Value<int?>("large_image_id") ?? 0),
                    SmallImageId = Math.Max(0, value.Value<int?>("small_image_id") ?? 0),
                    Star = Math.Max(0, value.Value<int?>("medal_start") ?? 0),
                    UpgradePower = Math.Max(0L, value.Value<long?>("upgrade_power") ?? 0L),
                    Title = value["title"]?.ToString() ?? string.Empty,
                    Attributes = ParseAttributes(value.Value<string>("add_attr")),
                    Costs = ParseCosts(value.Value<string>("cost")),
                    RequiredLayer = ParseRequiredLayer(value.Value<string>("other_condition")),
                };
                result[row.Id] = row;
            }
            return result;
        }

        private static IReadOnlyList<AttributeValue> ParseAttributes(string raw)
        {
            var result = new List<AttributeValue>();
            foreach (JToken token in ParseArray(raw))
            {
                if (!(token is JObject item)) continue;
                int id = item.Value<int?>("0") ?? 0;
                long amount = item.Value<long?>("1") ?? 0L;
                if (id > 0) result.Add(new AttributeValue { Id = id, Value = amount });
            }
            return result;
        }

        private static IReadOnlyList<CostValue> ParseCosts(string raw)
        {
            var result = new List<CostValue>();
            foreach (JToken token in ParseArray(raw))
            {
                if (!(token is JObject item)) continue;
                int typeId = item.Value<int?>("1") ?? 0;
                long count = item.Value<long?>("2") ?? 0L;
                if (typeId <= 0 || count <= 0) continue;
                result.Add(new CostValue
                {
                    Type = item.Value<int?>("0") ?? 0,
                    TypeId = typeId,
                    Count = count,
                });
            }
            return result;
        }

        private static int ParseRequiredLayer(string raw)
        {
            foreach (JToken token in ParseArray(raw))
            {
                if (!(token is JObject item)) continue;
                if (!string.Equals(item.Value<string>("0"), "dunid", StringComparison.Ordinal)) continue;
                return Math.Max(0, item.Value<int?>("1") ?? 0);
            }
            return 0;
        }

        private static JArray ParseArray(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return new JArray();
            return JArray.Parse(raw);
        }
    }
}
