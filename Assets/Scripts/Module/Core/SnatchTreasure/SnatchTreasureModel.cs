using System.Collections.Generic;

namespace Shenxiao.Module.Core.SnatchTreasure
{
    /// <summary>领地夺宝 65201 的入口只读快照；不承载进入、战斗、奖励或 65208 预告。</summary>
    public sealed class SnatchTreasureModel
    {
        public static readonly SnatchTreasureModel Instance = new SnatchTreasureModel();
        private SnatchTreasureModel() { }

        public sealed class BelongEntry
        {
            public uint DunId;
            public ushort Score;
            public ulong GuildId;
            public string GuildName = "";
        }

        public bool HasEntryInfo { get; private set; }
        public ushort TerritoryScore { get; private set; }
        public byte HaveTerritory { get; private set; }
        public List<BelongEntry> BelongList { get; private set; } = new List<BelongEntry>();

        public void ReplaceEntryInfo(List<BelongEntry> belongList, ushort territoryScore, byte haveTerritory)
        {
            BelongList = belongList ?? new List<BelongEntry>();
            TerritoryScore = territoryScore;
            HaveTerritory = haveTerritory;
            HasEntryInfo = true;
        }

        public void Clear()
        {
            HasEntryInfo = false;
            TerritoryScore = 0;
            HaveTerritory = 0;
            BelongList = new List<BelongEntry>();
        }
    }
}
