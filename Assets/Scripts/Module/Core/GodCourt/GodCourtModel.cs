using System;
using System.Collections.Generic;

namespace Shenxiao.Module.Core.GodCourt
{
    /// <summary>神庭 233xx 原始协议状态；所有有序表均保留服务端 wire 顺序和重复项。</summary>
    public sealed class GodCourtModel
    {
        public sealed class AttrEntry
        {
            public ushort AttrId { get; }
            public uint Value { get; }

            public AttrEntry(ushort attrId, uint value)
            {
                AttrId = attrId;
                Value = value;
            }
        }

        public sealed class EquipEntry
        {
            public byte Pos { get; }
            public ulong EquipId { get; }
            public byte Stage { get; }

            public EquipEntry(byte pos, ulong equipId, byte stage)
            {
                Pos = pos;
                EquipId = equipId;
                Stage = stage;
            }
        }

        public sealed class SuitEntry
        {
            public byte Stage { get; }
            public ushort Num { get; }

            public SuitEntry(byte stage, ushort num)
            {
                Stage = stage;
                Num = num;
            }
        }

        public sealed class CourtEntry
        {
            public uint CourtId { get; }
            public ushort CourtLevel { get; }
            public ulong Power { get; }
            public IReadOnlyList<AttrEntry> Attrs { get; }
            public byte IsActive { get; }
            public IReadOnlyList<EquipEntry> Equips { get; }
            public IReadOnlyList<SuitEntry> Suits { get; }

            public CourtEntry(uint courtId, ushort courtLevel, ulong power, IReadOnlyList<AttrEntry> attrs,
                byte isActive, IReadOnlyList<EquipEntry> equips, IReadOnlyList<SuitEntry> suits)
            {
                CourtId = courtId;
                CourtLevel = courtLevel;
                Power = power;
                Attrs = Freeze(attrs);
                IsActive = isActive;
                Equips = Freeze(equips);
                Suits = Freeze(suits);
            }
        }

        public sealed class OverviewSnapshot
        {
            public IReadOnlyList<CourtEntry> Courts { get; }

            public OverviewSnapshot(IReadOnlyList<CourtEntry> courts)
            {
                Courts = Freeze(courts);
            }
        }

        public sealed class GrandStatusEntry
        {
            public ushort Times { get; }
            public byte Status { get; }

            public GrandStatusEntry(ushort times, byte status)
            {
                Times = times;
                Status = status;
            }
        }

        public sealed class HouseSnapshot
        {
            public ushort RewardLevel { get; }
            public uint SumNum { get; }
            public byte CrystalColor { get; }
            public uint DailyNum { get; }
            public ushort HouseLevel { get; }
            public ushort HouseExp { get; }
            public IReadOnlyList<GrandStatusEntry> GrandStatuses { get; }

            public HouseSnapshot(ushort rewardLevel, uint sumNum, byte crystalColor, uint dailyNum,
                ushort houseLevel, ushort houseExp, IReadOnlyList<GrandStatusEntry> grandStatuses)
            {
                RewardLevel = rewardLevel;
                SumNum = sumNum;
                CrystalColor = crystalColor;
                DailyNum = dailyNum;
                HouseLevel = houseLevel;
                HouseExp = houseExp;
                GrandStatuses = Freeze(grandStatuses);
            }
        }

        public sealed class ErrorSnapshot
        {
            public uint ErrorCode { get; }
            public string ErrorArgs { get; }

            public ErrorSnapshot(uint errorCode, string errorArgs)
            {
                ErrorCode = errorCode;
                ErrorArgs = errorArgs;
            }
        }

        public static readonly GodCourtModel Instance = new GodCourtModel();

        private readonly Dictionary<uint, CourtEntry> _courtUpdates = new Dictionary<uint, CourtEntry>();
        private GodCourtModel() { }

        public OverviewSnapshot Overview { get; private set; }
        public HouseSnapshot House { get; private set; }
        public ErrorSnapshot LastError { get; private set; }
        public IReadOnlyDictionary<uint, CourtEntry> CourtUpdates => _courtUpdates;

        public bool HasOverview => Overview != null;
        public bool HasHouse => House != null;
        public bool HasError => LastError != null;
        public int CourtUpdateCount => _courtUpdates.Count;

        public void ReplaceOverview(IReadOnlyList<CourtEntry> courts)
        {
            Overview = new OverviewSnapshot(courts);
        }

        public void ReplaceHouse(ushort rewardLevel, uint sumNum, byte crystalColor, uint dailyNum,
            ushort houseLevel, ushort houseExp, IReadOnlyList<GrandStatusEntry> grandStatuses)
        {
            House = new HouseSnapshot(rewardLevel, sumNum, crystalColor, dailyNum, houseLevel, houseExp, grandStatuses);
        }

        public void ReplaceError(uint errorCode, string errorArgs)
        {
            LastError = new ErrorSnapshot(errorCode, errorArgs);
        }

        public void ReplaceCourtUpdate(CourtEntry court)
        {
            if (court == null) return;
            _courtUpdates[court.CourtId] = court;
        }

        public bool TryGetCourtUpdate(uint courtId, out CourtEntry court)
        {
            return _courtUpdates.TryGetValue(courtId, out court);
        }

        public void Reset()
        {
            Overview = null;
            House = null;
            LastError = null;
            _courtUpdates.Clear();
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
