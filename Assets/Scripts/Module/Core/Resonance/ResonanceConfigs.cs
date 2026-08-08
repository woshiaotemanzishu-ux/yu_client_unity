using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Equip;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.Module.Core.Resonance
{
    /// <summary>
    /// “共鸣”三张同源配置及事务前置判定。这里只做只读预检；材料扣除、共鸣等级和属性变化
    /// 始终以服务端 15221/15222 及随后 15220 权威快照为准。
    /// </summary>
    public static class ResonanceConfigs
    {
        public readonly struct TabDefinition
        {
            public readonly byte SuitType;
            public readonly byte SubType;
            public readonly string Label;

            public TabDefinition(byte suitType, byte subType, string label)
            {
                SuitType = suitType;
                SubType = subType;
                Label = label;
            }
        }

        public sealed class AttrValue
        {
            public int AttrId { get; internal set; }
            public long Value { get; internal set; }
        }

        public sealed class AttrTier
        {
            public int Count { get; internal set; }
            public IReadOnlyList<AttrValue> Attributes { get; internal set; } = Array.Empty<AttrValue>();
        }

        public sealed class SuitItem
        {
            public byte SuitType { get; internal set; }
            public byte SubType { get; internal set; }
            public ushort Level { get; internal set; }
            public string Name { get; internal set; } = string.Empty;
            public byte MaxCount { get; internal set; }
            public IReadOnlyList<AttrTier> Tiers { get; internal set; } = Array.Empty<AttrTier>();
        }

        public sealed class CostItem
        {
            public int Type { get; internal set; }
            public int RawTypeId { get; internal set; }
            public int TypeId { get; internal set; }
            public int Need { get; internal set; }
            public long Have { get; internal set; }
            public bool Enough => TypeId > 0 && Need > 0 && Have >= Need;
        }

        public sealed class MakeItem
        {
            public byte Position { get; internal set; }
            public byte SubType { get; internal set; }
            public ushort Level { get; internal set; }
            public byte NeedColor { get; internal set; }
            public byte NeedStar { get; internal set; }
            public byte NeedStage { get; internal set; }
            public string StageOperator { get; internal set; } = "ge";
            internal Dictionary<int, IReadOnlyList<CostItem>> CostsBySex { get; set; }
        }

        public enum BuildBlock
        {
            None,
            ConfigNotReady,
            SnapshotNotReady,
            RoleNotReady,
            PositionNotInTab,
            NoEquipment,
            MissingEquipmentConfig,
            MaxLevel,
            EquipmentCondition,
            MaterialNotEnough,
            OperationPending,
        }

        public sealed class BuildPreview
        {
            public TabDefinition Tab { get; internal set; }
            public byte Position { get; internal set; }
            public ushort CurrentLevel { get; internal set; }
            public BagGoods Equipment { get; internal set; }
            public GoodsModel.EquipAttr EquipmentAttr { get; internal set; }
            public SuitItem CurrentItem { get; internal set; }
            public SuitItem NextItem { get; internal set; }
            public MakeItem NextMake { get; internal set; }
            public IReadOnlyList<CostItem> Costs { get; internal set; } = Array.Empty<CostItem>();
            public BuildBlock Block { get; internal set; }
            public ushort MaxReachableLevel { get; internal set; }
            public string Fingerprint { get; internal set; } = string.Empty;
            public bool CanBuild => Block == BuildBlock.None;
            public bool IsMax => Block == BuildBlock.MaxLevel;
        }

        public static readonly TabDefinition[] Tabs =
        {
            new TabDefinition(1, 1, "妖魂共鸣"),
            new TabDefinition(1, 2, "战魂共鸣"),
            new TabDefinition(1, 3, "万物共鸣"),
            new TabDefinition(2, 1, "饰物共鸣"),
        };

        private static Dictionary<byte, byte> _positionTypes;
        private static Dictionary<string, SuitItem> _suitItems;
        private static Dictionary<string, MakeItem> _makeItems;
        private static Dictionary<string, IReadOnlyList<SuitItem>> _suitLists;
        private static Dictionary<string, IReadOnlyList<MakeItem>> _makeLists;
        private static Dictionary<byte, IReadOnlyList<byte>> _positions;
        private static Task _loading;

        public static bool IsLoaded => _positionTypes != null && _suitItems != null && _makeItems != null
            && _suitLists != null && _makeLists != null && _positions != null;
        public static int PositionCount => _positionTypes?.Count ?? 0;
        public static int SuitItemCount => _suitItems?.Count ?? 0;
        public static int MakeItemCount => _makeItems?.Count ?? 0;

        public static Task EnsureLoaded()
        {
            if (IsLoaded) return Task.CompletedTask;
            if (_loading == null || _loading.IsCompleted) _loading = LoadAsync();
            return _loading;
        }

        public static TabDefinition GetTab(int index)
        {
            if (index < 0 || index >= Tabs.Length) return Tabs[0];
            return Tabs[index];
        }

        public static IReadOnlyList<byte> GetPositions(byte suitType)
            => _positions != null && _positions.TryGetValue(suitType, out IReadOnlyList<byte> list)
                ? list : Array.Empty<byte>();

        public static byte GetPositionSuitType(byte position)
            => _positionTypes != null && _positionTypes.TryGetValue(position, out byte value) ? value : (byte)0;

        public static SuitItem GetSuitItem(byte suitType, byte subType, ushort level)
            => _suitItems != null && _suitItems.TryGetValue(SuitKey(suitType, subType, level), out SuitItem item)
                ? item : null;

        public static IReadOnlyList<SuitItem> GetSuitItems(byte suitType, byte subType)
            => _suitLists != null && _suitLists.TryGetValue(SuitListKey(suitType, subType), out IReadOnlyList<SuitItem> list)
                ? list : Array.Empty<SuitItem>();

        public static MakeItem GetMakeItem(byte position, byte subType, ushort level)
            => _makeItems != null && _makeItems.TryGetValue(MakeKey(position, subType, level), out MakeItem item)
                ? item : null;

        public static IReadOnlyList<MakeItem> GetMakeItems(byte position, byte subType)
            => _makeLists != null && _makeLists.TryGetValue(MakeListKey(position, subType), out IReadOnlyList<MakeItem> list)
                ? list : Array.Empty<MakeItem>();

        public static ushort GetCurrentLevel(byte suitType, byte subType, byte position)
            => EquipReadModel.Instance.GetSuitLevel(position, subType, suitType);

        /// <summary>
        /// 返回装备格应展示的最高共鸣特效阶级。对标老端 GetPosEquipShowSuitAni：
        /// 当前阶级达到该装备在此阶可打造的最高等级后才点亮，并从高阶向低阶回退。
        /// </summary>
        public static byte GetPositionEffectTier(byte position, BagGoods equipment)
        {
            if (!IsLoaded || equipment == null || !EquipReadModel.Instance.HasSuitInfo) return 0;
            BagGoods worn = BagModel.Instance.GetEquipmentAt(position);
            if (worn == null || equipment.GoodsId <= 0 || worn.GoodsId != equipment.GoodsId) return 0;
            byte suitType = GetPositionSuitType(position);
            if (suitType == 0) return 0;

            byte highestTier = suitType == 1 ? (byte)3 : (byte)1;
            for (int value = highestTier; value >= 1; value--)
            {
                byte tier = (byte)value;
                ushort current = GetCurrentLevel(suitType, tier, position);
                ushort required = GetMaxReachableLevel(position, tier, equipment);
                if (required == 0) required = 1;
                if (current >= required) return tier;
            }
            return 0;
        }

        /// <summary>共享装备槽与共鸣页面共同使用的资源名映射；各宿主的缩放由各自所有者负责。</summary>
        public static string GetEffectName(byte suitType, byte subType)
        {
            if (suitType == 2) return "ui_shenzhuang03";
            switch (subType)
            {
                case 1: return "ui_shenzhuang01";
                case 2: return "ui_shenzhuang02";
                case 3: return "ui_shenzhuang03";
                default: return string.Empty;
            }
        }

        /// <summary>
        /// 对标服务端 lib_equip_check:quality 的真实语义：更高品质直接通过；品质恰好相等时才比较星级。
        /// stage 的 ge/le/gt/lt/eq 同样按配置比较，当前三张表实际只使用 ge。
        /// </summary>
        public static bool MeetsEquipmentCondition(BagGoods equipment, MakeItem make, out GoodsModel.EquipAttr attr)
        {
            attr = equipment != null ? GoodsModel.GetEquipAttr(equipment.TypeId) : null;
            if (equipment == null || make == null || attr == null) return false;
            bool quality = equipment.Color > make.NeedColor
                || (equipment.Color == make.NeedColor && attr.Star >= make.NeedStar);
            return quality && Compare(attr.Stage, make.StageOperator, make.NeedStage);
        }

        public static ushort GetMaxReachableLevel(byte position, byte subType, BagGoods equipment)
        {
            ushort max = 0;
            IReadOnlyList<MakeItem> list = GetMakeItems(position, subType);
            for (int i = 0; i < list.Count; i++)
            {
                if (MeetsEquipmentCondition(equipment, list[i], out _)) max = list[i].Level;
            }
            return max;
        }

        public static BuildPreview Preview(int tabIndex, byte position, bool operationPending = false)
        {
            TabDefinition tab = GetTab(tabIndex);
            var result = new BuildPreview { Tab = tab, Position = position };
            if (!IsLoaded) { result.Block = BuildBlock.ConfigNotReady; return result; }
            if (!RoleModel.Instance.HasBaseInfo) { result.Block = BuildBlock.RoleNotReady; return result; }
            if (!EquipReadModel.Instance.HasSuitInfo) { result.Block = BuildBlock.SnapshotNotReady; return result; }
            if (GetPositionSuitType(position) != tab.SuitType)
            {
                result.Block = BuildBlock.PositionNotInTab;
                return result;
            }

            result.CurrentLevel = GetCurrentLevel(tab.SuitType, tab.SubType, position);
            result.CurrentItem = GetSuitItem(tab.SuitType, tab.SubType, result.CurrentLevel);
            result.Equipment = BagModel.Instance.GetEquipmentAt(position);
            if (result.Equipment == null)
            {
                result.Block = BuildBlock.NoEquipment;
                result.Fingerprint = BuildFingerprint(result);
                return result;
            }

            result.EquipmentAttr = GoodsModel.GetEquipAttr(result.Equipment.TypeId);
            result.MaxReachableLevel = GetMaxReachableLevel(position, tab.SubType, result.Equipment);
            ushort nextLevel = unchecked((ushort)(result.CurrentLevel + 1));
            result.NextItem = GetSuitItem(tab.SuitType, tab.SubType, nextLevel);
            result.NextMake = GetMakeItem(position, tab.SubType, nextLevel);
            if (result.NextItem == null || result.NextMake == null)
            {
                result.Block = BuildBlock.MaxLevel;
                result.Fingerprint = BuildFingerprint(result);
                return result;
            }
            if (result.EquipmentAttr == null)
            {
                result.Block = BuildBlock.MissingEquipmentConfig;
                result.Fingerprint = BuildFingerprint(result);
                return result;
            }

            result.Costs = ResolveCosts(result.NextMake, RoleModel.Instance.Sex);
            if (!MeetsEquipmentCondition(result.Equipment, result.NextMake, out _))
                result.Block = BuildBlock.EquipmentCondition;
            else if (!AllCostsEnough(result.Costs))
                result.Block = BuildBlock.MaterialNotEnough;
            else if (operationPending)
                result.Block = BuildBlock.OperationPending;
            else
                result.Block = BuildBlock.None;
            result.Fingerprint = BuildFingerprint(result);
            return result;
        }

        public static int GetActiveCount(byte suitType, byte subType, ushort level)
        {
            if (level == 0) return 0;
            int count = 0;
            IReadOnlyList<byte> positions = GetPositions(suitType);
            for (int i = 0; i < positions.Count; i++)
            {
                ushort current = GetCurrentLevel(suitType, subType, positions[i]);
                if (suitType == 1 ? current == level : current >= level) count++;
            }
            return count;
        }

        public static string GetBlockText(BuildPreview preview)
        {
            if (preview == null) return "共鸣数据未就绪";
            switch (preview.Block)
            {
                case BuildBlock.ConfigNotReady: return "共鸣配置加载中";
                case BuildBlock.SnapshotNotReady: return "共鸣数据加载中";
                case BuildBlock.RoleNotReady: return "角色数据加载中";
                case BuildBlock.PositionNotInTab: return "该部位不属于当前共鸣类型";
                case BuildBlock.NoEquipment: return "当前部位未穿戴装备";
                case BuildBlock.MissingEquipmentConfig: return "装备阶星配置缺失";
                case BuildBlock.MaxLevel: return "已达到最高共鸣等级";
                case BuildBlock.EquipmentCondition:
                    if (preview.NextMake == null) return "装备条件不足";
                    return string.Format(CultureInfo.InvariantCulture, "需穿戴{0}阶、品质{1}星级{2}及以上装备",
                        preview.NextMake.NeedStage, preview.NextMake.NeedColor, preview.NextMake.NeedStar);
                case BuildBlock.MaterialNotEnough: return "打造材料不足";
                case BuildBlock.OperationPending: return "共鸣操作处理中";
                default: return string.Empty;
            }
        }

        public static string BuildFingerprint(BuildPreview preview)
        {
            if (preview == null) return string.Empty;
            var sb = new StringBuilder(128);
            sb.Append(preview.Tab.SuitType).Append(':').Append(preview.Tab.SubType).Append(':')
                .Append(preview.Position).Append(':').Append(preview.CurrentLevel).Append(':')
                .Append(EquipReadModel.Instance.Version).Append(':').Append(RoleModel.Instance.Sex);
            BagGoods equipment = preview.Equipment;
            if (equipment != null)
            {
                GoodsModel.EquipAttr attr = preview.EquipmentAttr ?? GoodsModel.GetEquipAttr(equipment.TypeId);
                sb.Append('|').Append(equipment.GoodsId).Append(':').Append(equipment.TypeId).Append(':')
                    .Append(equipment.Color).Append(':').Append(attr?.Stage ?? -1).Append(':').Append(attr?.Star ?? -1);
            }
            else sb.Append("|none");
            IReadOnlyList<CostItem> costs = preview.Costs ?? Array.Empty<CostItem>();
            for (int i = 0; i < costs.Count; i++)
            {
                CostItem c = costs[i];
                sb.Append('|').Append(c.Type).Append(':').Append(c.RawTypeId).Append(':').Append(c.TypeId)
                    .Append(':').Append(c.Need).Append(':').Append(BagModel.Instance.GetTypeGoodsNum(c.TypeId));
            }
            return sb.ToString();
        }

        public static string BuildReturnFingerprint(int tabIndex, byte position)
        {
            TabDefinition tab = GetTab(tabIndex);
            BagGoods equipment = BagModel.Instance.GetEquipmentAt(position);
            return string.Format(CultureInfo.InvariantCulture, "{0}:{1}:{2}:{3}:{4}:{5}:{6}",
                tab.SuitType, tab.SubType, position, GetCurrentLevel(tab.SuitType, tab.SubType, position),
                EquipReadModel.Instance.Version, equipment?.GoodsId ?? 0L, equipment?.TypeId ?? 0);
        }

        private static IReadOnlyList<CostItem> ResolveCosts(MakeItem make, int sex)
        {
            if (make?.CostsBySex == null || !make.CostsBySex.TryGetValue(sex, out IReadOnlyList<CostItem> source))
                return Array.Empty<CostItem>();
            var result = new List<CostItem>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                CostItem c = source[i];
                (int goodsId, _) = GoodsModel.GetMappingTypeId(c.Type, c.RawTypeId);
                result.Add(new CostItem
                {
                    Type = c.Type,
                    RawTypeId = c.RawTypeId,
                    TypeId = goodsId,
                    Need = c.Need,
                    Have = BagModel.Instance.GetTypeGoodsNum(goodsId),
                });
            }
            return result;
        }

        private static bool AllCostsEnough(IReadOnlyList<CostItem> costs)
        {
            if (costs == null || costs.Count == 0) return false;
            for (int i = 0; i < costs.Count; i++) if (!costs[i].Enough) return false;
            return true;
        }

        private static async Task LoadAsync()
        {
            TextAsset positionAsset = null;
            TextAsset itemAsset = null;
            TextAsset makeAsset = null;
            try
            {
                Task goods = GoodsModel.EnsureLoaded();
                Task<TextAsset> positionLoad = ResManager.LoadAsync<TextAsset>(GameResPath.GetServerConfigPath("config_equip_pos2suittype"));
                Task<TextAsset> itemLoad = ResManager.LoadAsync<TextAsset>(GameResPath.GetServerConfigPath("config_equip_suit_item"));
                Task<TextAsset> makeLoad = ResManager.LoadAsync<TextAsset>(GameResPath.GetServerConfigPath("config_equip_suit_make"));
                await Task.WhenAll(goods, positionLoad, itemLoad, makeLoad);
                positionAsset = positionLoad.Result;
                itemAsset = itemLoad.Result;
                makeAsset = makeLoad.Result;
                if (positionAsset == null || itemAsset == null || makeAsset == null)
                    throw new InvalidOperationException("resonance config asset missing");

                Dictionary<byte, byte> positionTypes = ParsePositions(positionAsset.text);
                Dictionary<string, SuitItem> suitItems = ParseSuitItems(itemAsset.text);
                Dictionary<string, MakeItem> makeItems = ParseMakeItems(makeAsset.text);
                if (positionTypes.Count != 10 || suitItems.Count != 46 || makeItems.Count != 252)
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                        "config closure mismatch positions={0}/10 suit={1}/46 make={2}/252",
                        positionTypes.Count, suitItems.Count, makeItems.Count));

                Dictionary<byte, IReadOnlyList<byte>> positions = BuildPositions(positionTypes);
                Dictionary<string, IReadOnlyList<SuitItem>> suitLists = BuildSuitLists(suitItems);
                Dictionary<string, IReadOnlyList<MakeItem>> makeLists = BuildMakeLists(makeItems);
                ValidateClosure(positions, suitLists, makeLists);

                // 全部解析与闭包校验通过后才原子换入，失败时保留上一份可用快照。
                _positionTypes = positionTypes;
                _suitItems = suitItems;
                _makeItems = makeItems;
                _positions = positions;
                _suitLists = suitLists;
                _makeLists = makeLists;
                GameLog.Info("Resonance", "configs ready positions={0} suit={1} make={2}",
                    positionTypes.Count, suitItems.Count, makeItems.Count);
            }
            catch (Exception e)
            {
                GameLog.Error("Resonance", "config load failed; previous snapshot kept: {0}", e.Message);
            }
            finally
            {
                if (positionAsset != null) ResManager.Release(positionAsset);
                if (itemAsset != null) ResManager.Release(itemAsset);
                if (makeAsset != null) ResManager.Release(makeAsset);
            }
        }

        private static Dictionary<byte, byte> ParsePositions(string json)
        {
            var result = new Dictionary<byte, byte>();
            foreach (KeyValuePair<string, JToken> pair in JObject.Parse(json))
            {
                if (!(pair.Value is JObject row)) continue;
                byte pos = ReadByte(row, "pos");
                byte type = ReadByte(row, "type");
                if (pos != 0 && (type == 1 || type == 2)) result[pos] = type;
            }
            return result;
        }

        private static Dictionary<string, SuitItem> ParseSuitItems(string json)
        {
            var result = new Dictionary<string, SuitItem>();
            foreach (KeyValuePair<string, JToken> pair in JObject.Parse(json))
            {
                if (!(pair.Value is JObject row)) continue;
                var item = new SuitItem
                {
                    SuitType = ReadByte(row, "type"),
                    SubType = ReadByte(row, "lv"),
                    Level = ReadUShort(row, "slv"),
                    Name = row["name"]?.ToString() ?? string.Empty,
                    MaxCount = ReadByte(row, "max_count"),
                    Tiers = ParseAttrTiers(row["attr_list"]?.ToString()),
                };
                if (item.SuitType == 0 || item.SubType == 0 || item.Level == 0 || item.Tiers.Count == 0) continue;
                result[SuitKey(item.SuitType, item.SubType, item.Level)] = item;
            }
            return result;
        }

        private static Dictionary<string, MakeItem> ParseMakeItems(string json)
        {
            var result = new Dictionary<string, MakeItem>();
            foreach (KeyValuePair<string, JToken> pair in JObject.Parse(json))
            {
                if (!(pair.Value is JObject row)) continue;
                var item = new MakeItem
                {
                    Position = ReadByte(row, "pos"),
                    SubType = ReadByte(row, "lv"),
                    Level = ReadUShort(row, "slv"),
                    CostsBySex = ParseCosts(row["cost"]?.ToString()),
                };
                ParseConditions(row["condition"]?.ToString(), item);
                if (item.Position == 0 || item.SubType == 0 || item.Level == 0 || item.CostsBySex.Count == 0) continue;
                result[MakeKey(item.Position, item.SubType, item.Level)] = item;
            }
            return result;
        }

        private static IReadOnlyList<AttrTier> ParseAttrTiers(string raw)
        {
            var result = new List<AttrTier>();
            JArray array = ParseEmbeddedArray(raw);
            for (int i = 0; i < array.Count; i++)
            {
                if (!(array[i] is JObject tier)) continue;
                var attrs = new List<AttrValue>();
                if (tier["1"] is JArray values)
                {
                    for (int j = 0; j < values.Count; j++)
                    {
                        if (!(values[j] is JObject value)) continue;
                        attrs.Add(new AttrValue { AttrId = ReadInt(value, "0"), Value = ReadLong(value, "1") });
                    }
                }
                result.Add(new AttrTier { Count = ReadInt(tier, "0"), Attributes = attrs });
            }
            return result;
        }

        private static Dictionary<int, IReadOnlyList<CostItem>> ParseCosts(string raw)
        {
            var result = new Dictionary<int, IReadOnlyList<CostItem>>();
            JArray array = ParseEmbeddedArray(raw);
            for (int i = 0; i < array.Count; i++)
            {
                if (!(array[i] is JObject branch)) continue;
                int sex = ReadInt(branch, "0");
                var costs = new List<CostItem>();
                if (branch["1"] is JArray values)
                {
                    for (int j = 0; j < values.Count; j++)
                    {
                        if (!(values[j] is JObject value)) continue;
                        costs.Add(new CostItem
                        {
                            Type = ReadInt(value, "0"),
                            RawTypeId = ReadInt(value, "1"),
                            Need = ReadInt(value, "2"),
                        });
                    }
                }
                if (sex > 0 && costs.Count > 0) result[sex] = costs;
            }
            return result;
        }

        private static void ParseConditions(string raw, MakeItem target)
        {
            JArray array = ParseEmbeddedArray(raw);
            for (int i = 0; i < array.Count; i++)
            {
                if (!(array[i] is JObject condition)) continue;
                string kind = condition["0"]?.ToString() ?? string.Empty;
                if (kind == "quality")
                {
                    target.NeedColor = ReadByte(condition, "1");
                    target.NeedStar = ReadByte(condition, "2");
                }
                else if (kind == "stage")
                {
                    target.StageOperator = condition["1"]?.ToString() ?? "ge";
                    target.NeedStage = ReadByte(condition, "2");
                }
            }
        }

        private static JArray ParseEmbeddedArray(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return new JArray();
            JToken token = JToken.Parse(raw);
            return token as JArray ?? new JArray();
        }

        private static Dictionary<byte, IReadOnlyList<byte>> BuildPositions(Dictionary<byte, byte> source)
        {
            var temp = new Dictionary<byte, List<byte>> { [1] = new List<byte>(), [2] = new List<byte>() };
            foreach (KeyValuePair<byte, byte> pair in source) temp[pair.Value].Add(pair.Key);
            var result = new Dictionary<byte, IReadOnlyList<byte>>();
            foreach (KeyValuePair<byte, List<byte>> pair in temp)
            {
                pair.Value.Sort();
                result[pair.Key] = pair.Value;
            }
            return result;
        }

        private static Dictionary<string, IReadOnlyList<SuitItem>> BuildSuitLists(Dictionary<string, SuitItem> source)
        {
            var temp = new Dictionary<string, List<SuitItem>>();
            foreach (SuitItem item in source.Values)
            {
                string key = SuitListKey(item.SuitType, item.SubType);
                if (!temp.TryGetValue(key, out List<SuitItem> list)) temp[key] = list = new List<SuitItem>();
                list.Add(item);
            }
            var result = new Dictionary<string, IReadOnlyList<SuitItem>>();
            foreach (KeyValuePair<string, List<SuitItem>> pair in temp)
            {
                pair.Value.Sort((a, b) => a.Level.CompareTo(b.Level));
                result[pair.Key] = pair.Value;
            }
            return result;
        }

        private static Dictionary<string, IReadOnlyList<MakeItem>> BuildMakeLists(Dictionary<string, MakeItem> source)
        {
            var temp = new Dictionary<string, List<MakeItem>>();
            foreach (MakeItem item in source.Values)
            {
                string key = MakeListKey(item.Position, item.SubType);
                if (!temp.TryGetValue(key, out List<MakeItem> list)) temp[key] = list = new List<MakeItem>();
                list.Add(item);
            }
            var result = new Dictionary<string, IReadOnlyList<MakeItem>>();
            foreach (KeyValuePair<string, List<MakeItem>> pair in temp)
            {
                pair.Value.Sort((a, b) => a.Level.CompareTo(b.Level));
                result[pair.Key] = pair.Value;
            }
            return result;
        }

        private static void ValidateClosure(Dictionary<byte, IReadOnlyList<byte>> positions,
            Dictionary<string, IReadOnlyList<SuitItem>> suits,
            Dictionary<string, IReadOnlyList<MakeItem>> makes)
        {
            if (!positions.TryGetValue(1, out IReadOnlyList<byte> equipment) || equipment.Count != 6
                || !positions.TryGetValue(2, out IReadOnlyList<byte> accessories) || accessories.Count != 4)
                throw new InvalidOperationException("position closure must be 6 equipment + 4 accessories");
            int[] expected = { 16, 10, 8, 12 };
            for (int i = 0; i < Tabs.Length; i++)
            {
                TabDefinition tab = Tabs[i];
                if (!suits.TryGetValue(SuitListKey(tab.SuitType, tab.SubType), out IReadOnlyList<SuitItem> list)
                    || list.Count != expected[i])
                    throw new InvalidOperationException("suit stage closure mismatch for tab " + i);
                IReadOnlyList<byte> tabPositions = positions[tab.SuitType];
                for (int j = 0; j < tabPositions.Count; j++)
                {
                    if (!makes.TryGetValue(MakeListKey(tabPositions[j], tab.SubType), out IReadOnlyList<MakeItem> makeList)
                        || makeList.Count != expected[i])
                        throw new InvalidOperationException("make stage closure mismatch at pos " + tabPositions[j] + " tab " + i);
                }
            }
        }

        private static bool Compare(int value, string op, int target)
        {
            switch (op)
            {
                case "ge": return value >= target;
                case "gt": return value > target;
                case "le": return value <= target;
                case "lt": return value < target;
                case "eq": return value == target;
                default: return false;
            }
        }

        private static string SuitKey(byte suitType, byte subType, ushort level)
            => suitType + "@" + subType + "@" + level;
        private static string SuitListKey(byte suitType, byte subType) => suitType + "@" + subType;
        private static string MakeKey(byte position, byte subType, ushort level)
            => position + "@" + subType + "@" + level;
        private static string MakeListKey(byte position, byte subType) => position + "@" + subType;
        private static int ReadInt(JObject row, string key)
            => int.TryParse(row?[key]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : 0;
        private static long ReadLong(JObject row, string key)
            => long.TryParse(row?[key]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long value) ? value : 0L;
        private static byte ReadByte(JObject row, string key)
            => byte.TryParse(row?[key]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out byte value) ? value : (byte)0;
        private static ushort ReadUShort(JObject row, string key)
            => ushort.TryParse(row?[key]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort value) ? value : (ushort)0;
    }
}
