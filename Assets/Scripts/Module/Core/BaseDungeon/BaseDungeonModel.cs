namespace Shenxiao.Module.Core.BaseDungeon
{
    /// <summary>
    /// 限时爬塔数据(对标老客户端 BaseDungeonModel 的限时爬塔部分)。BaseDungeon 本是大副本系统,
    /// 本期只承载「限时爬塔」活动图标(331@97)所需字段,副本/爬塔玩法逻辑(挑战/扫荡/领奖 61118 等)一律不移植。
    /// 61117 下发 round/over_time/reward_mode,决定主界面限时塔图标显隐(带倒计时 over_time)。
    /// </summary>
    public sealed class BaseDungeonModel
    {
        public static readonly BaseDungeonModel Instance = new BaseDungeonModel();
        private BaseDungeonModel() { }

        /// <summary>限时爬塔图标类型(对标老端 BaseDungeonModel.TOWER_ICON_TYPE="331@97")。</summary>
        public const string TOWER_ICON_TYPE = "331@97";

        // 61117 限时爬塔基础信息(对标老端 dungeon_tower_scmd)
        public int Round;      // 当前轮次(0 表示活动未开启)
        public int OverTime;   // 活动倒计时(0 表示未开启,>0 走图标倒计时)
        public int RewardMode; // 大奖领取态 0 不可领取 1 可领取 2 已领取

        /// <summary>
        /// 领取大奖后是否本次登录仍保持图标显示(对标老端 is_continue_show)。
        /// 一旦图标被加过就置 true,使得会话内即便 reward_mode 变 2 也不撤图标。
        /// </summary>
        public bool IsContinueShow;

        public void SetTowerInfo(int round, int overTime, int rewardMode)
        {
            Round = round;
            OverTime = overTime;
            RewardMode = rewardMode;
        }

        /// <summary>大奖领取态(对标老端 GetTowerRewardState():dungeon_tower_scmd.reward_mode || 0)。</summary>
        public int GetTowerRewardState()
        {
            return RewardMode;
        }

        /// <summary>
        /// 限时塔图标显隐判定(对标老端 AddTowerIcon 的显示条件)。
        /// over_time 为 0(活动未开)→ 不显示;或(本次登录未持续显示过 且 大奖已领 reward_mode==2)→ 不显示;
        /// 其余(活动进行中)→ 显示。注:读取时不改 IsContinueShow,置位由控制器在显示分支完成(对标老端逻辑)。
        /// </summary>
        public bool GetTowerIconOpenState()
        {
            if (OverTime == 0) return false;
            if (!IsContinueShow && GetTowerRewardState() == 2) return false;
            return true;
        }

        /// <summary>限时塔图标图(对标老端 GetLimitTowerIcon(round):"33197_"+轮次,round 为 0 取 1)。</summary>
        public string GetLimitTowerIcon(int round)
        {
            return "33197_" + (round == 0 ? 1 : round);
        }

        public void Reset()
        {
            Round = 0;
            OverTime = 0;
            RewardMode = 0;
            IsContinueShow = false;
        }
    }
}
