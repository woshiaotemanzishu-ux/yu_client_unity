namespace Shenxiao.Module.Core.TopVip
{
    /// <summary>
    /// 至尊VIP(TopVip)数据(对标老客户端 TopVipModel)。承载 45101 下发的至尊vip基础信息,
    /// 供主界面图标(451)显隐判定与后续面板展示用。
    /// 图标门槛不看 45101,而是看主角 vip_flag/level(对标老端 TopVipController 里 vip_flag 闭包:
    /// vip_flag>=4 且 level>=160 才 addIcon(451))——GetEntranceOpenState 承载该判定。
    /// 面板/技能/任务/商店等 UI 待用户验收,本期只做图标。
    /// </summary>
    public sealed class TopVipModel
    {
        public static readonly TopVipModel Instance = new TopVipModel();
        private TopVipModel() { }

        /// <summary>主界面图标类型(对标老端 addIcon(451)/getIconInfo(451))。</summary>
        public const string ICON_TYPE = "451";

        // 图标开启门槛(对标老端 TopVipController.vip_flag 闭包:vip_flag>=4 且 level>=160)。
        public const int RequireVipFlag = 4;   // 需激活 vip4(老端 "需激活vip4才可购买至尊vip")
        public const int RequireLevel = 160;   // 需达到 160 级

        // 45101 至尊vip基础信息(对标老端 SetMyTopVipVo,面板用;图标判定不依赖这些字段)。
        public int SupvipType;    // 至尊vip类型/档位
        public int SupvipTime;    // 到期时间戳(0/永久)
        public int ChargeDay;     // 累计充值天数
        public int TodayGold;     // 今日已领勾玉
        public int IsFreeProtect; // 免费保护标记

        public void SetInfo(int supvipType, int supvipTime, int chargeDay, int todayGold, int isFreeProtect)
        {
            SupvipType = supvipType;
            SupvipTime = supvipTime;
            ChargeDay = chargeDay;
            TodayGold = todayGold;
            IsFreeProtect = isFreeProtect;
        }

        /// <summary>
        /// 入口开启状态(对标老端 vip_flag 闭包判定:vip_flag>=4 且 level>=160)。
        /// 与 45101 回包无关,纯看主角 vip_flag/level;由控制器从 RoleModel 取值后传入。
        /// </summary>
        public bool GetEntranceOpenState(int vipFlag, int level)
        {
            return vipFlag >= RequireVipFlag && level >= RequireLevel;
        }

        public void Reset()
        {
            SupvipType = 0;
            SupvipTime = 0;
            ChargeDay = 0;
            TodayGold = 0;
            IsFreeProtect = 0;
        }
    }
}
