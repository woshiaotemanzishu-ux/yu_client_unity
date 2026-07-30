using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.Module.Core.Armor
{
    /// <summary>不朽圣骸只读配置与打造预检；真正扣料和最终状态始终以服务端 14402 为准。</summary>
    public static class ArmorConfigs
    {
        public sealed class CostItem
        {
            public int Type { get; internal set; }
            public int TypeId { get; internal set; }
            public int Num { get; internal set; }
            public bool IsArmorState { get; internal set; }
        }

        public sealed class AttrItem
        {
            public int AttrId { get; internal set; }
            public long Value { get; internal set; }
        }

        public sealed class EquipmentCfg
        {
            public int Id { get; internal set; }
            public byte Stage { get; internal set; }
            public byte Type { get; internal set; }
            public byte Position { get; internal set; }
            public byte PreStage { get; internal set; }
            public IReadOnlyList<CostItem> Costs { get; internal set; }
            public IReadOnlyList<AttrItem> Attributes { get; internal set; }
        }

        public sealed class SuitCfg
        {
            public byte Stage { get; internal set; }
            public byte Type { get; internal set; }
            public int OpenLevel { get; internal set; }
            public IReadOnlyList<AttrItem> Attributes { get; internal set; }
        }

        public enum MakeBlock
        {
            None,
            ConfigNotReady,
            RoleNotReady,
            SnapshotNotReady,
            MissingConfig,
            LevelLocked,
            MissingPreviousStage,
            AlreadyMade,
            MaterialNotEnough,
        }

        public sealed class PreviewResult
        {
            public EquipmentCfg Equipment { get; internal set; }
            public SuitCfg Suit { get; internal set; }
            public MakeBlock Block { get; internal set; }
            public IReadOnlyList<CostItem> DisplayCosts { get; internal set; } = EmptyCosts;
            public IReadOnlyList<CostItem> RealCosts { get; internal set; } = EmptyCosts;
            public bool CanMake => Block == MakeBlock.None;
        }

        private static readonly IReadOnlyList<CostItem> EmptyCosts = new List<CostItem>();
        private static readonly IReadOnlyList<AttrItem> EmptyAttrs = new List<AttrItem>();
        private static Dictionary<int, EquipmentCfg> _equipmentById;
        private static Dictionary<string, EquipmentCfg> _equipmentByKey;
        private static Dictionary<string, SuitCfg> _suits;
        private static Dictionary<byte, IReadOnlyList<byte>> _positions;
        private static Task _loading;

        public static bool IsLoaded => _equipmentById != null && _equipmentByKey != null && _suits != null && _positions != null;
        public static int EquipmentCount => _equipmentById?.Count ?? 0;
        public static int SuitCount => _suits?.Count ?? 0;

        public static Task EnsureLoaded()
        {
            if (IsLoaded) return Task.CompletedTask;
            if (_loading == null || _loading.IsCompleted) _loading = LoadAsync();
            return _loading;
        }

        public static EquipmentCfg GetEquipment(byte stage, byte type, byte position)
            => _equipmentByKey != null && _equipmentByKey.TryGetValue(Key(stage, type, position), out EquipmentCfg value) ? value : null;

        public static EquipmentCfg GetEquipment(int typeId)
            => _equipmentById != null && _equipmentById.TryGetValue(typeId, out EquipmentCfg value) ? value : null;

        public static SuitCfg GetSuit(byte stage, byte type)
            => _suits != null && _suits.TryGetValue(SuitKey(stage, type), out SuitCfg value) ? value : null;

        public static IReadOnlyList<byte> GetPositions(byte type)
            => _positions != null && _positions.TryGetValue(type, out IReadOnlyList<byte> value) ? value : Array.Empty<byte>();

        public static IReadOnlyList<AttrItem> GetEquipmentAttributes(int typeId)
            => GetEquipment(typeId)?.Attributes ?? EmptyAttrs;

        public static IReadOnlyList<AttrItem> GetSuitAttributes(byte stage, byte type)
            => GetSuit(stage, type)?.Attributes ?? EmptyAttrs;

        /// <summary>
        /// 与服务端 lib_armour_check 顺序一致：配置/角色/快照、等级、前阶、未打造、真实背包材料。
        /// consume 中的圣骸装备只是“前阶状态展示物”，对应服务端 get_real_consume_goods 的过滤项，绝不按背包物品扣除。
        /// </summary>
        public static PreviewResult Preview(byte stage, byte type, byte position)
        {
            var result = new PreviewResult();
            if (!IsLoaded) { result.Block = MakeBlock.ConfigNotReady; return result; }
            if (!RoleModel.Instance.HasBaseInfo) { result.Block = MakeBlock.RoleNotReady; return result; }
            if (!ArmorModel.Instance.HasData) { result.Block = MakeBlock.SnapshotNotReady; return result; }

            EquipmentCfg equipment = GetEquipment(stage, type, position);
            SuitCfg suit = GetSuit(stage, type);
            result.Equipment = equipment;
            result.Suit = suit;
            if (equipment == null || suit == null) { result.Block = MakeBlock.MissingConfig; return result; }

            var display = new List<CostItem>(equipment.Costs.Count);
            var real = new List<CostItem>(equipment.Costs.Count);
            for (int i = 0; i < equipment.Costs.Count; i++)
            {
                CostItem source = equipment.Costs[i];
                var cost = new CostItem
                {
                    Type = source.Type,
                    TypeId = source.TypeId,
                    Num = source.Num,
                    IsArmorState = _equipmentById.ContainsKey(source.TypeId),
                };
                display.Add(cost);
                if (!cost.IsArmorState) real.Add(cost);
            }
            result.DisplayCosts = display;
            result.RealCosts = real;

            if (RoleModel.Instance.Level < suit.OpenLevel) { result.Block = MakeBlock.LevelLocked; return result; }
            if (equipment.PreStage != 0 && !ArmorModel.Instance.IsMade(equipment.PreStage, type, position))
            {
                result.Block = MakeBlock.MissingPreviousStage;
                return result;
            }
            if (ArmorModel.Instance.IsMade(stage, type, position)) { result.Block = MakeBlock.AlreadyMade; return result; }

            for (int i = 0; i < real.Count; i++)
            {
                CostItem cost = real[i];
                if (cost.Type != 0 || cost.TypeId <= 0 || cost.Num <= 0
                    || BagModel.Instance.GetTypeGoodsNum(cost.TypeId) < cost.Num)
                {
                    result.Block = MakeBlock.MaterialNotEnough;
                    return result;
                }
            }
            result.Block = MakeBlock.None;
            return result;
        }

        public static string GetBlockText(PreviewResult preview)
        {
            if (preview == null) return "圣骸数据未就绪";
            switch (preview.Block)
            {
                case MakeBlock.ConfigNotReady: return "圣骸配置加载中";
                case MakeBlock.RoleNotReady: return "角色数据未就绪";
                case MakeBlock.SnapshotNotReady: return "圣骸数据加载中";
                case MakeBlock.MissingConfig: return "圣骸配置缺失";
                case MakeBlock.LevelLocked: return preview.Suit == null ? "等级不足" : (preview.Suit.OpenLevel + "级开启");
                case MakeBlock.MissingPreviousStage: return "请先打造同部位前一阶圣骸";
                case MakeBlock.AlreadyMade: return "该部位已经打造";
                case MakeBlock.MaterialNotEnough: return "打造材料不足";
                default: return string.Empty;
            }
        }

        public static string BuildFingerprint(PreviewResult preview)
        {
            if (preview?.Equipment == null) return string.Empty;
            var sb = new StringBuilder();
            EquipmentCfg cfg = preview.Equipment;
            sb.Append(cfg.Stage).Append(':').Append(cfg.Type).Append(':').Append(cfg.Position).Append(':').Append(cfg.Id)
                .Append(':').Append((int)preview.Block).Append(':').Append(RoleModel.Instance.Level);
            for (int i = 0; i < preview.DisplayCosts.Count; i++)
            {
                CostItem cost = preview.DisplayCosts[i];
                long have = cost.IsArmorState
                    ? (IsArmorStateAvailable(cost.TypeId) ? 1L : 0L)
                    : BagModel.Instance.GetTypeGoodsNum(cost.TypeId);
                sb.Append('|').Append(cost.Type).Append(':').Append(cost.TypeId).Append(':').Append(cost.Num)
                    .Append(':').Append(cost.IsArmorState ? 1 : 0).Append(':').Append(have);
            }
            return sb.ToString();
        }

        public static bool IsArmorStateAvailable(int equipmentTypeId)
        {
            EquipmentCfg cfg = GetEquipment(equipmentTypeId);
            return cfg != null && ArmorModel.Instance.IsMade(cfg.Stage, cfg.Type, cfg.Position);
        }

        public static IReadOnlyList<AttrItem> GetAllActiveAttributes()
        {
            var values = new SortedDictionary<int, long>();
            IReadOnlyList<ArmorModel.StageEntry> stages = ArmorModel.Instance.Stages;
            for (int i = 0; i < stages.Count; i++)
            {
                ArmorModel.StageEntry stage = stages[i];
                for (int j = 0; j < stage.Types.Count; j++)
                {
                    ArmorModel.TypeEntry type = stage.Types[j];
                    if (type.Status == 1) Add(values, GetSuitAttributes(stage.Stage, type.Type));
                    for (int k = 0; k < type.Positions.Count; k++)
                    {
                        ArmorModel.PositionEntry position = type.Positions[k];
                        if (position.Status == 1) Add(values, GetEquipmentAttributes(unchecked((int)position.GTypeId)));
                    }
                }
            }
            var result = new List<AttrItem>(values.Count);
            foreach (KeyValuePair<int, long> pair in values) result.Add(new AttrItem { AttrId = pair.Key, Value = pair.Value });
            return result;
        }

        private static void Add(IDictionary<int, long> values, IReadOnlyList<AttrItem> attrs)
        {
            for (int i = 0; i < attrs.Count; i++)
            {
                AttrItem attr = attrs[i];
                values[attr.AttrId] = values.TryGetValue(attr.AttrId, out long old) ? old + attr.Value : attr.Value;
            }
        }

        private static async Task LoadAsync()
        {
            TextAsset equipmentAsset = null;
            TextAsset suitAsset = null;
            TextAsset kvAsset = null;
            try
            {
                equipmentAsset = await ResManager.LoadAsync<TextAsset>(GameResPath.GetServerConfigPath("config_armour_equipment"));
                suitAsset = await ResManager.LoadAsync<TextAsset>(GameResPath.GetServerConfigPath("config_armour_suit"));
                kvAsset = await ResManager.LoadAsync<TextAsset>(GameResPath.GetServerConfigPath("config_armour_kv"));
                if (equipmentAsset == null || suitAsset == null || kvAsset == null) throw new InvalidOperationException("armor config asset missing");

                var byId = new Dictionary<int, EquipmentCfg>();
                var byKey = new Dictionary<string, EquipmentCfg>();
                foreach (KeyValuePair<string, JToken> pair in JObject.Parse(equipmentAsset.text))
                {
                    if (!(pair.Value is JObject row)) continue;
                    var cfg = new EquipmentCfg
                    {
                        Id = ReadInt(row, "id"),
                        Stage = ReadByte(row, "stage"),
                        Type = ReadByte(row, "type"),
                        Position = ReadByte(row, "pos"),
                        PreStage = ReadByte(row, "pre_stage"),
                        Costs = ParseCosts(row["consume"]?.ToString()),
                        Attributes = ParseAttributes(row["attr"]?.ToString()),
                    };
                    if (cfg.Id <= 0 || cfg.Stage == 0 || cfg.Type == 0 || cfg.Position == 0) continue;
                    byId[cfg.Id] = cfg;
                    byKey[Key(cfg.Stage, cfg.Type, cfg.Position)] = cfg;
                }
                foreach (EquipmentCfg cfg in byId.Values)
                    for (int i = 0; i < cfg.Costs.Count; i++) cfg.Costs[i].IsArmorState = byId.ContainsKey(cfg.Costs[i].TypeId);

                var suits = new Dictionary<string, SuitCfg>();
                foreach (KeyValuePair<string, JToken> pair in JObject.Parse(suitAsset.text))
                {
                    if (!(pair.Value is JObject row)) continue;
                    var cfg = new SuitCfg
                    {
                        Stage = ReadByte(row, "stage"),
                        Type = ReadByte(row, "type"),
                        OpenLevel = ReadInt(row, "open_lv"),
                        Attributes = ParseAttributes(row["attr"]?.ToString()),
                    };
                    if (cfg.Stage != 0 && cfg.Type != 0) suits[SuitKey(cfg.Stage, cfg.Type)] = cfg;
                }

                var positions = new Dictionary<byte, IReadOnlyList<byte>>();
                foreach (KeyValuePair<string, JToken> pair in JObject.Parse(kvAsset.text))
                {
                    if (!(pair.Value is JObject row)) continue;
                    byte type = ReadByte(row, "key");
                    var list = new List<byte>();
                    JArray array = JArray.Parse(row["value"]?.ToString() ?? "[]");
                    for (int i = 0; i < array.Count; i++)
                        if (byte.TryParse(array[i]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out byte pos)) list.Add(pos);
                    if (type != 0) positions[type] = list;
                }

                if (byId.Count == 0 || suits.Count == 0 || positions.Count == 0) throw new InvalidOperationException("armor config parsed empty");
                _equipmentById = byId;
                _equipmentByKey = byKey;
                _suits = suits;
                _positions = positions;
            }
            catch (Exception e)
            {
                _equipmentById = new Dictionary<int, EquipmentCfg>();
                _equipmentByKey = new Dictionary<string, EquipmentCfg>();
                _suits = new Dictionary<string, SuitCfg>();
                _positions = new Dictionary<byte, IReadOnlyList<byte>>();
                GameLog.Warn("Armor", "parse armor configs failed: {0}", e.Message);
            }
            finally
            {
                if (equipmentAsset != null) ResManager.Release(equipmentAsset);
                if (suitAsset != null) ResManager.Release(suitAsset);
                if (kvAsset != null) ResManager.Release(kvAsset);
            }
        }

        private static List<CostItem> ParseCosts(string raw)
        {
            var result = new List<CostItem>();
            ErlangTerm root = ErlangParser.Parse(raw ?? "[]");
            IReadOnlyList<ErlangTerm> items = root?.Items;
            if (items == null) return result;
            for (int i = 0; i < items.Count; i++)
            {
                IReadOnlyList<ErlangTerm> tuple = items[i]?.Items;
                if (tuple == null || tuple.Count < 3) continue;
                result.Add(new CostItem { Type = tuple[0].As<int>(), TypeId = tuple[1].As<int>(), Num = tuple[2].As<int>() });
            }
            return result;
        }

        private static List<AttrItem> ParseAttributes(string raw)
        {
            var result = new List<AttrItem>();
            ErlangTerm root = ErlangParser.Parse(raw ?? "[]");
            IReadOnlyList<ErlangTerm> items = root?.Items;
            if (items == null) return result;
            for (int i = 0; i < items.Count; i++)
            {
                IReadOnlyList<ErlangTerm> tuple = items[i]?.Items;
                if (tuple == null || tuple.Count < 2) continue;
                result.Add(new AttrItem { AttrId = tuple[0].As<int>(), Value = tuple[1].As<long>() });
            }
            return result;
        }

        private static string Key(byte stage, byte type, byte position) => stage + "@" + type + "@" + position;
        private static string SuitKey(byte stage, byte type) => stage + "@" + type;
        private static int ReadInt(JObject row, string key)
            => int.TryParse(row?[key]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : 0;
        private static byte ReadByte(JObject row, string key)
            => byte.TryParse(row?[key]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out byte value) ? value : (byte)0;
    }
}
