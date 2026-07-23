using System.Collections.Generic;

namespace Shenxiao.Module.Core.BrightSea
{
    /// <summary>18900 无尽之海主页面完整快照。航运项保留服务端 wire 顺序与重复项。</summary>
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

        public void Clear()
        {
            Picture = null;
            PictureVersion = 0;
            RewardTimes = TotalRewardTimes = RobTimes = TotalRobTimes = Status = 0;
            AutoId = 0;
            SendList.Clear();
            HasInfo = false;
        }
    }
}
