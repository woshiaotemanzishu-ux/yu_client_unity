using System.Collections.Generic;

namespace Shenxiao.Module.Core.BrightSea
{
    /// <summary>无尽之海七份独立只读原始快照；列表保留服务端 wire 顺序与重复项。</summary>
    public sealed class BrightSeaModel
    {
        public sealed class ShippingEntry
        {
            public ulong AutoId;
            public byte ShippingId;
            public uint ServerId;
            public uint ServerNumber;
            public ulong GuildId;
            public string GuildName;
            public ulong RoleId;
            public string RoleName;
            public ushort RoleLevel;
            public ulong Power;
            public byte Sex;
            public ushort Career;
            public byte Turn;
            public string Picture;
            public uint PictureVersion;
            public uint EndTime;
            public byte RobTimes;
        }

        public sealed class ServerEntry
        {
            public uint ServerId;
            public ushort ServerNumber;
            public string ServerName;
            public ushort WorldLevel;
        }

        public sealed class ObjectEntry
        {
            public byte Type;
            public uint TypeId;
            public uint Num;
        }

        public sealed class CruiseLogEntry
        {
            public ulong AutoId;
            public byte Type;
            public uint RoberServerId;
            public uint RoberServerNumber;
            public ulong RoberGuildId;
            public string RoberGuildName;
            public ulong RoberId;
            public string RoberName;
            public ulong RoberPower;
            public byte ShippingId;
            public readonly List<ObjectEntry> Reward = new List<ObjectEntry>();
            public readonly List<ObjectEntry> BackList = new List<ObjectEntry>();
            public readonly List<ObjectEntry> ReceiveList = new List<ObjectEntry>();
            public uint Time;
        }

        public static readonly BrightSeaModel Instance = new BrightSeaModel();
        private BrightSeaModel() { }

        public string Picture { get; private set; }
        public uint PictureVersion { get; private set; }
        public byte RewardTimes { get; private set; }
        public byte TotalRewardTimes { get; private set; }
        public byte RobTimes { get; private set; }
        public byte TotalRobTimes { get; private set; }
        public ulong AutoId { get; private set; }
        public byte Status { get; private set; }
        public bool HasInfo { get; private set; }
        public readonly List<ShippingEntry> SendList = new List<ShippingEntry>();

        // ---- 18915 跨服信息（独立于 18900 主快照）----
        public byte TreasureModule { get; private set; }
        public ushort WorldLevel { get; private set; }
        public readonly List<ServerEntry> EnemyServers = new List<ServerEntry>();
        public byte UnsatisfiedModule { get; private set; }
        public ushort UnsatisfiedWorldLevel { get; private set; }
        public ushort MinWorldLevel { get; private set; }
        public readonly List<ServerEntry> UnsatisfiedServers = new List<ServerEntry>();
        public bool HasServerInfo { get; private set; }

        // ---- 18901 巡航/掠夺记录（独立于 18900/18915）----
        public readonly List<CruiseLogEntry> CruiseLogs = new List<CruiseLogEntry>();
        public bool HasCruiseLogs { get; private set; }

        // ---- 18902 巡航船只页状态（独立于 18900/18901/18915） ----
        public bool HasShipInfo { get; private set; }
        public byte ShippingId { get; private set; }
        public ushort LuckeyValue { get; private set; }
        public byte ShipRewardTimes { get; private set; }
        public byte ShipTotalRewardTimes { get; private set; }
        public byte UpTimes { get; private set; }
        public byte TotalUpTimes { get; private set; }

        // ---- 18916 协助绑元日次数（独立于其余六个快照）----
        public bool HasAssistBGoldInfo { get; private set; }
        public ushort AssistBGoldNum { get; private set; }
        public ushort AssistBGoldMax { get; private set; }

        // ---- 18917 本人巡航状态（独立于其余六个快照）----
        public bool HasShipStatus { get; private set; }
        public ulong ShipStatusAutoId { get; private set; }
        public byte ShipStatus { get; private set; }
        public byte ShipStatusRewardTimes { get; private set; }
        public byte ShipStatusTotalRewardTimes { get; private set; }

        // ---- 18904 巡航结算详情（独立于其余六个快照）----
        public bool HasCruiseDetail { get; private set; }
        public ulong CruiseDetailAutoId { get; private set; }
        public uint CruiseDetailRoberServerId { get; private set; }
        public uint CruiseDetailRoberServerNumber { get; private set; }
        public ulong CruiseDetailRoberId { get; private set; }
        public string CruiseDetailRoberName { get; private set; }
        public ulong CruiseDetailRoberPower { get; private set; }
        public byte CruiseDetailShippingId { get; private set; }
        public readonly List<ObjectEntry> CruiseDetailReward = new List<ObjectEntry>();
        public readonly List<ObjectEntry> CruiseDetailRobReward = new List<ObjectEntry>();
        public uint CruiseDetailTime { get; private set; }

        /// <summary>每个 18900 包无条件整体替换；空列表也为已加载快照。</summary>
        public void Replace(string picture, uint pictureVersion, byte rewardTimes, byte totalRewardTimes,
            byte robTimes, byte totalRobTimes, ulong autoId, byte status, List<ShippingEntry> sendList)
        {
            Picture = picture;
            PictureVersion = pictureVersion;
            RewardTimes = rewardTimes;
            TotalRewardTimes = totalRewardTimes;
            RobTimes = robTimes;
            TotalRobTimes = totalRobTimes;
            AutoId = autoId;
            Status = status;
            SendList.Clear();
            if (sendList != null) SendList.AddRange(sendList);
            HasInfo = true;
        }

        /// <summary>18915 两个服务器列表按 wire 原序整体替换；空列表也为已加载快照。</summary>
        public void ReplaceServerInfo(byte treasureModule, ushort worldLevel, List<ServerEntry> enemyServers,
            byte unsatisfiedModule, ushort unsatisfiedWorldLevel, ushort minWorldLevel, List<ServerEntry> unsatisfiedServers)
        {
            TreasureModule = treasureModule;
            WorldLevel = worldLevel;
            EnemyServers.Clear();
            if (enemyServers != null) EnemyServers.AddRange(enemyServers);
            UnsatisfiedModule = unsatisfiedModule;
            UnsatisfiedWorldLevel = unsatisfiedWorldLevel;
            MinWorldLevel = minWorldLevel;
            UnsatisfiedServers.Clear();
            if (unsatisfiedServers != null) UnsatisfiedServers.AddRange(unsatisfiedServers);
            HasServerInfo = true;
        }

        /// <summary>18901 整表替换；日志及三个 ObjectList 均保留 wire 原序与重复项。</summary>
        public void ReplaceCruiseLogs(List<CruiseLogEntry> cruiseLogs)
        {
            CruiseLogs.Clear();
            if (cruiseLogs != null) CruiseLogs.AddRange(cruiseLogs);
            HasCruiseLogs = true;
        }

        public void ReplaceShipInfo(byte shippingId, ushort luckeyValue, byte rewardTimes, byte totalRewardTimes, byte upTimes, byte totalUpTimes)
        {
            ShippingId = shippingId;
            LuckeyValue = luckeyValue;
            ShipRewardTimes = rewardTimes;
            ShipTotalRewardTimes = totalRewardTimes;
            UpTimes = upTimes;
            TotalUpTimes = totalUpTimes;
            HasShipInfo = true;
        }

        public void ReplaceAssistBGoldInfo(ushort num, ushort max)
        {
            AssistBGoldNum = num;
            AssistBGoldMax = max;
            HasAssistBGoldInfo = true;
        }

        /// <summary>18917 每包完整覆盖；全零也是有效已加载快照。</summary>
        public void ReplaceShipStatus(ulong autoId, byte status, byte rewardTimes, byte totalRewardTimes)
        {
            ShipStatusAutoId = autoId;
            ShipStatus = status;
            ShipStatusRewardTimes = rewardTimes;
            ShipStatusTotalRewardTimes = totalRewardTimes;
            HasShipStatus = true;
        }

        public void ReplaceCruiseDetail(ulong autoId, uint roberServerId, uint roberServerNumber, ulong roberId, string roberName,
            ulong roberPower, byte shippingId, List<ObjectEntry> reward, List<ObjectEntry> robReward, uint time)
        {
            CruiseDetailAutoId = autoId; CruiseDetailRoberServerId = roberServerId; CruiseDetailRoberServerNumber = roberServerNumber;
            CruiseDetailRoberId = roberId; CruiseDetailRoberName = roberName; CruiseDetailRoberPower = roberPower;
            CruiseDetailShippingId = shippingId; CruiseDetailReward.Clear(); CruiseDetailRobReward.Clear();
            if (reward != null) CruiseDetailReward.AddRange(reward);
            if (robReward != null) CruiseDetailRobReward.AddRange(robReward);
            CruiseDetailTime = time; HasCruiseDetail = true;
        }

        public void Clear()
        {
            Picture = null;
            PictureVersion = 0;
            RewardTimes = TotalRewardTimes = RobTimes = TotalRobTimes = Status = 0;
            AutoId = 0;
            SendList.Clear();
            HasInfo = false;
            TreasureModule = UnsatisfiedModule = 0;
            WorldLevel = UnsatisfiedWorldLevel = MinWorldLevel = 0;
            EnemyServers.Clear();
            UnsatisfiedServers.Clear();
            HasServerInfo = false;
            CruiseLogs.Clear();
            HasCruiseLogs = false;
            ShippingId = ShipRewardTimes = ShipTotalRewardTimes = UpTimes = TotalUpTimes = 0;
            LuckeyValue = 0;
            HasShipInfo = false;
            AssistBGoldNum = AssistBGoldMax = 0;
            HasAssistBGoldInfo = false;
            ShipStatusAutoId = 0;
            ShipStatus = ShipStatusRewardTimes = ShipStatusTotalRewardTimes = 0;
            HasShipStatus = false;
            CruiseDetailAutoId = CruiseDetailRoberId = CruiseDetailRoberPower = 0;
            CruiseDetailRoberServerId = CruiseDetailRoberServerNumber = CruiseDetailTime = 0;
            CruiseDetailRoberName = null; CruiseDetailShippingId = 0;
            CruiseDetailReward.Clear(); CruiseDetailRobReward.Clear(); HasCruiseDetail = false;
        }
    }
}
