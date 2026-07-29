using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Shenxiao.Module.Core.Auction
{
    /// <summary>拍卖154家族的原始读侧状态；各协议切片互不交叉修改。</summary>
    public sealed class AuctionModel
    {
        public readonly struct GoodsKey : IEquatable<GoodsKey>
        {
            public uint AuctionType { get; }
            public ulong GoodsId { get; }

            public GoodsKey(uint auctionType, ulong goodsId)
            {
                AuctionType = auctionType;
                GoodsId = goodsId;
            }

            public bool Equals(GoodsKey other) =>
                AuctionType == other.AuctionType && GoodsId == other.GoodsId;
            public override bool Equals(object obj) => obj is GoodsKey other && Equals(other);
            public override int GetHashCode() => unchecked(((int)AuctionType * 397) ^ GoodsId.GetHashCode());
        }

        public readonly struct ModuleKey : IEquatable<ModuleKey>
        {
            public uint AuctionType { get; }
            public uint ModuleId { get; }

            public ModuleKey(uint auctionType, uint moduleId)
            {
                AuctionType = auctionType;
                ModuleId = moduleId;
            }

            public bool Equals(ModuleKey other) =>
                AuctionType == other.AuctionType && ModuleId == other.ModuleId;
            public override bool Equals(object obj) => obj is ModuleKey other && Equals(other);
            public override int GetHashCode() => unchecked(((int)AuctionType * 397) ^ (int)ModuleId);
        }

        public sealed class Goods
        {
            public ulong GoodsId { get; }
            public uint ModuleId { get; }
            public uint TypeId { get; }
            public ushort WorldLevel { get; }
            public uint CurrentPrice { get; }
            public uint NextPrice { get; }
            public uint Price { get; }
            public uint StartTime { get; }
            public uint EndTime { get; }
            public ulong TopPlayerId { get; }
            public byte IsDelay { get; }
            public byte HadBonus { get; }

            public Goods(ulong goodsId, uint moduleId, uint typeId, ushort worldLevel,
                uint currentPrice, uint nextPrice, uint price, uint startTime, uint endTime,
                ulong topPlayerId, byte isDelay, byte hadBonus)
            {
                GoodsId = goodsId;
                ModuleId = moduleId;
                TypeId = typeId;
                WorldLevel = worldLevel;
                CurrentPrice = currentPrice;
                NextPrice = nextPrice;
                Price = price;
                StartTime = startTime;
                EndTime = endTime;
                TopPlayerId = topPlayerId;
                IsDelay = isDelay;
                HadBonus = hadBonus;
            }
        }

        public sealed class GoodsSnapshot
        {
            public uint AuctionType { get; }
            public IReadOnlyList<Goods> GoodsList { get; }

            public GoodsSnapshot(uint auctionType, IReadOnlyList<Goods> goodsList)
            {
                AuctionType = auctionType;
                GoodsList = Freeze(goodsList);
            }
        }

        public sealed class GoodsUpdate
        {
            public ulong GoodsId { get; }
            public uint ModuleId { get; }
            public uint AuctionType { get; }
            public uint CurrentPrice { get; }
            public uint NextPrice { get; }
            public uint EndTime { get; }
            public ulong TopPlayerId { get; }
            public byte IsDelay { get; }
            public byte GoodsStatus { get; }

            public GoodsUpdate(ulong goodsId, uint moduleId, uint auctionType, uint currentPrice,
                uint nextPrice, uint endTime, ulong topPlayerId, byte isDelay, byte goodsStatus)
            {
                GoodsId = goodsId;
                ModuleId = moduleId;
                AuctionType = auctionType;
                CurrentPrice = currentPrice;
                NextPrice = nextPrice;
                EndTime = endTime;
                TopPlayerId = topPlayerId;
                IsDelay = isDelay;
                GoodsStatus = goodsStatus;
            }
        }

        public sealed class EstimateSnapshot
        {
            public uint AuctionType { get; }
            public uint ModuleId { get; }
            public uint EstimatedGold { get; }
            public uint EstimatedBoundGold { get; }

            public EstimateSnapshot(uint auctionType, uint moduleId, uint estimatedGold,
                uint estimatedBoundGold)
            {
                AuctionType = auctionType;
                ModuleId = moduleId;
                EstimatedGold = estimatedGold;
                EstimatedBoundGold = estimatedBoundGold;
            }
        }

        public sealed class LifecycleSnapshot
        {
            public uint AuctionType { get; }
            public uint ModuleId { get; }
            public byte Type { get; }

            public LifecycleSnapshot(uint auctionType, uint moduleId, byte type)
            {
                AuctionType = auctionType;
                ModuleId = moduleId;
                Type = type;
            }
        }

        public sealed class PersonalRecord
        {
            public byte OperationType { get; }
            public uint ModuleId { get; }
            public byte PriceType { get; }
            public ushort Gold { get; }
            public ushort BoundGold { get; }
            public uint TypeId { get; }
            public ushort WorldLevel { get; }
            public uint Time { get; }

            public PersonalRecord(byte operationType, uint moduleId, byte priceType, ushort gold,
                ushort boundGold, uint typeId, ushort worldLevel, uint time)
            {
                OperationType = operationType;
                ModuleId = moduleId;
                PriceType = priceType;
                Gold = gold;
                BoundGold = boundGold;
                TypeId = typeId;
                WorldLevel = worldLevel;
                Time = time;
            }
        }

        public sealed class PersonalRecordsSnapshot
        {
            public IReadOnlyList<PersonalRecord> Records { get; }
            public PersonalRecordsSnapshot(IReadOnlyList<PersonalRecord> records) => Records = Freeze(records);
        }

        public sealed class BonusRecord
        {
            public uint ModuleId { get; }
            public ushort Gold { get; }
            public ushort BoundGold { get; }
            public uint Time { get; }

            public BonusRecord(uint moduleId, ushort gold, ushort boundGold, uint time)
            {
                ModuleId = moduleId;
                Gold = gold;
                BoundGold = boundGold;
                Time = time;
            }
        }

        public sealed class BonusInfo
        {
            public uint ModuleId { get; }
            public ushort GoldReceived { get; }
            public ushort BoundGoldReceived { get; }

            public BonusInfo(uint moduleId, ushort goldReceived, ushort boundGoldReceived)
            {
                ModuleId = moduleId;
                GoldReceived = goldReceived;
                BoundGoldReceived = boundGoldReceived;
            }
        }

        public sealed class BonusSnapshot
        {
            public IReadOnlyList<BonusRecord> Records { get; }
            public IReadOnlyList<BonusInfo> Infos { get; }

            public BonusSnapshot(IReadOnlyList<BonusRecord> records, IReadOnlyList<BonusInfo> infos)
            {
                Records = Freeze(records);
                Infos = Freeze(infos);
            }
        }

        public sealed class AllCloseSnapshot
        {
            public byte RawValue { get; }
            public AllCloseSnapshot(byte rawValue) => RawValue = rawValue;
        }

        public static readonly AuctionModel Instance = new AuctionModel();

        private readonly Dictionary<uint, GoodsSnapshot> _goodsByAuctionType =
            new Dictionary<uint, GoodsSnapshot>();
        private readonly Dictionary<GoodsKey, GoodsUpdate> _updates =
            new Dictionary<GoodsKey, GoodsUpdate>();
        private readonly Dictionary<ModuleKey, EstimateSnapshot> _estimates =
            new Dictionary<ModuleKey, EstimateSnapshot>();
        private readonly Dictionary<ModuleKey, LifecycleSnapshot> _lifecycles =
            new Dictionary<ModuleKey, LifecycleSnapshot>();
        private readonly IReadOnlyDictionary<uint, GoodsSnapshot> _goodsView;
        private readonly IReadOnlyDictionary<GoodsKey, GoodsUpdate> _updatesView;
        private readonly IReadOnlyDictionary<ModuleKey, EstimateSnapshot> _estimatesView;
        private readonly IReadOnlyDictionary<ModuleKey, LifecycleSnapshot> _lifecyclesView;

        private AuctionModel()
        {
            _goodsView = new ReadOnlyDictionary<uint, GoodsSnapshot>(_goodsByAuctionType);
            _updatesView = new ReadOnlyDictionary<GoodsKey, GoodsUpdate>(_updates);
            _estimatesView = new ReadOnlyDictionary<ModuleKey, EstimateSnapshot>(_estimates);
            _lifecyclesView = new ReadOnlyDictionary<ModuleKey, LifecycleSnapshot>(_lifecycles);
        }

        public IReadOnlyDictionary<uint, GoodsSnapshot> GoodsByAuctionType => _goodsView;
        public IReadOnlyDictionary<GoodsKey, GoodsUpdate> Updates => _updatesView;
        public IReadOnlyDictionary<ModuleKey, EstimateSnapshot> Estimates => _estimatesView;
        public IReadOnlyDictionary<ModuleKey, LifecycleSnapshot> Lifecycles => _lifecyclesView;
        public PersonalRecordsSnapshot PersonalRecords { get; private set; }
        public BonusSnapshot BonusRecords { get; private set; }
        public AllCloseSnapshot AllClose { get; private set; }
        public bool HasPersonalRecords => PersonalRecords != null;
        public bool HasBonusRecords => BonusRecords != null;
        public bool HasAllClose => AllClose != null;

        public void ReplaceGoods(uint auctionType, IReadOnlyList<Goods> goods) =>
            _goodsByAuctionType[auctionType] = new GoodsSnapshot(auctionType, goods);

        public void ReplaceUpdate(GoodsUpdate update) =>
            _updates[new GoodsKey(update.AuctionType, update.GoodsId)] = update;

        public void ReplaceEstimate(EstimateSnapshot estimate) =>
            _estimates[new ModuleKey(estimate.AuctionType, estimate.ModuleId)] = estimate;

        public void ReplaceLifecycle(LifecycleSnapshot lifecycle) =>
            _lifecycles[new ModuleKey(lifecycle.AuctionType, lifecycle.ModuleId)] = lifecycle;

        public void ReplacePersonalRecords(IReadOnlyList<PersonalRecord> records) =>
            PersonalRecords = new PersonalRecordsSnapshot(records);

        public void ReplaceBonusRecords(IReadOnlyList<BonusRecord> records, IReadOnlyList<BonusInfo> infos) =>
            BonusRecords = new BonusSnapshot(records, infos);

        public void ReplaceAllClose(byte rawValue) => AllClose = new AllCloseSnapshot(rawValue);

        public bool TryGetGoods(uint auctionType, out GoodsSnapshot snapshot) =>
            _goodsByAuctionType.TryGetValue(auctionType, out snapshot);
        public bool TryGetUpdate(uint auctionType, ulong goodsId, out GoodsUpdate update) =>
            _updates.TryGetValue(new GoodsKey(auctionType, goodsId), out update);
        public bool TryGetEstimate(uint auctionType, uint moduleId, out EstimateSnapshot estimate) =>
            _estimates.TryGetValue(new ModuleKey(auctionType, moduleId), out estimate);
        public bool TryGetLifecycle(uint auctionType, uint moduleId, out LifecycleSnapshot lifecycle) =>
            _lifecycles.TryGetValue(new ModuleKey(auctionType, moduleId), out lifecycle);

        public void Reset()
        {
            _goodsByAuctionType.Clear();
            _updates.Clear();
            _estimates.Clear();
            _lifecycles.Clear();
            PersonalRecords = null;
            BonusRecords = null;
            AllClose = null;
        }

        private static IReadOnlyList<T> Freeze<T>(IReadOnlyList<T> source)
        {
            if (source == null || source.Count == 0) return Array.Empty<T>();
            var copy = new List<T>(source.Count);
            for (int i = 0; i < source.Count; i++) copy.Add(source[i]);
            return copy.AsReadOnly();
        }
    }
}
