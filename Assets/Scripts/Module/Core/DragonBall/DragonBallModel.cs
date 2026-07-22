using System.Collections.Generic;
using Shenxiao.Module.Core.FirstRecharge;
using Shenxiao.Module.Core.Game;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Common;

namespace Shenxiao.Module.Core.DragonBall
{
    /// <summary>
    /// 龙玉(龙珠)数据(对标老客户端 DragonBallModel)。14310 保存雕像快照，14303 按类型更新套装概览；
    /// 「龙珠礼包」活动图标(DragonGiftIconType=143)由 14311(dragon_gift_data)驱动显隐。
    /// 龙珠本体/苍龙镇世/操作链(14300-14302/14304-14306/14312)不在本期。
    ///
    /// 老端 RefreshGiftIcon 显隐门槛(faithful):
    ///   1. GetOpenState()          —— 功能开放且非审核服;
    ///   2. dragon_gift_data.id > 0 —— 服务端有下发可购礼包;
    ///   3. config_start_nuclear[id] 存在 且 role_lv>=open_lv 且 open_day 已到 且 times_limit-buy_times>0;
    ///   4. IsDoneFirstRecharge()   —— 已完成首充。
    /// Unity 侧显式加载 config_start_nuclear，并在本模型完整判定功能开放、alpha、等级、开服天、
    /// 限购余量与首充；AddIconAsync 的图标配置门只作为第二道公共保险，不能替代上述业务门。
    /// </summary>
    public sealed class DragonBallModel
    {
        public static readonly DragonBallModel Instance = new DragonBallModel();
        private DragonBallModel() { }

        /// <summary>龙珠礼包活动图标类型(对标老端 DragonBallModel.DragonGiftIconType=143)。</summary>
        public const string ICON_TYPE = "143";

        public byte StatueStatus { get; private set; }
        public ulong StatuePreviewPower { get; private set; }
        public bool HasStatueOverview { get; private set; }

        public sealed class SuitEntry
        {
            public byte Type { get; }
            public byte Level { get; }
            public ulong Power { get; }
            public ulong NextPower { get; }
            public SuitEntry(byte type, byte level, ulong power, ulong nextPower) { Type = type; Level = level; Power = power; NextPower = nextPower; }
        }

        private readonly Dictionary<byte, SuitEntry> _suits = new Dictionary<byte, SuitEntry>();
        public IReadOnlyDictionary<byte, SuitEntry> Suits => _suits;
        public byte WearType { get; private set; }
        public bool HasSuitData { get; private set; }

        /// <summary>14303 按 type 更新，包中缺席的旧 type 保留；空列表只更新穿戴类型和已收包状态。</summary>
        public void SetSuitData(byte wearType, List<SuitEntry> entries)
        {
            WearType = wearType;
            if (entries != null) for (int i = 0; i < entries.Count; i++) _suits[entries[i].Type] = entries[i];
            HasSuitData = true;
        }

        /// <summary>14310 是全量雕像总览；status=1 时服务端下发 power=0 也必须覆盖旧值。</summary>
        public void SetStatueOverview(byte status, ulong power)
        {
            StatueStatus = status;
            StatuePreviewPower = power;
            HasStatueOverview = true;
        }

        // 14311 龙珠礼包数据(对标老端 dragon_gift_data / SetDragonBallGiftData)
        public int GiftId;    // 礼包活动id(config_start_nuclear 主键;0 表示无可购礼包)
        public int BuyTimes;  // 已购买次数；config_start_nuclear.times_limit-BuyTimes>0 才显示

        public void SetGiftInfo(int giftId, int buyTimes)
        {
            GiftId = giftId;
            BuyTimes = buyTimes;
        }

        /// <summary>
        /// 龙珠礼包图标完整开启状态，对标老端 RefreshGiftIcon；异步配置未就绪时保持关闭。
        /// </summary>
        public bool GetGiftIconOpenState()
        {
            // 两份异步配置都就绪前保持关闭，避免 DragonBall 配置先到、功能开放表仍在加载时短暂误显。
            if (!DragonBallConfigs.IsLoaded || !FuncOpenConfig.IsLoaded) return false;
            int level = RoleModel.Instance.HasBaseInfo ? RoleModel.Instance.Level : 0;
            return GetGiftIconOpenState(level, ServerTimeModel.GetOpenServerDay(), FirstRechargeModel.Instance.IsDoneFirstRecharge(), FuncOpenConfig.CheckFuncOpenState("DragonBallView"), PlatformModel.IsAlpha);
        }

        public bool GetGiftIconOpenState(int roleLevel, int openServerDay, bool firstRechargeDone)
            => GetGiftIconOpenState(roleLevel, openServerDay, firstRechargeDone, true, false);

        public bool GetGiftIconOpenState(int roleLevel, int openServerDay, bool firstRechargeDone, bool functionOpen, bool isAlpha)
        {
            if (GiftId <= 0 || !DragonBallConfigs.IsLoaded || !firstRechargeDone || !functionOpen || isAlpha) return false;
            DragonBallConfigs.Row row = DragonBallConfigs.Get(GiftId);
            return row != null && roleLevel >= row.OpenLevel && openServerDay >= row.OpenDay && row.TimesLimit - BuyTimes > 0;
        }

        public void Reset()
        {
            StatueStatus = 0;
            StatuePreviewPower = 0;
            HasStatueOverview = false;
            _suits.Clear();
            WearType = 0;
            HasSuitData = false;
            GiftId = 0;
            BuyTimes = 0;
        }
    }
}
