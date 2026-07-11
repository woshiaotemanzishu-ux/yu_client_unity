namespace Shenxiao.Module.Core.DiamondFight
{
    /// <summary>
    /// 灵玉(勾玉)大战数据(对标老客户端 DiamondFightModel,模块 137)。本期只承载 13700 下发的
    /// 活动阶段信息(war_state/end_time),供主界面图标(137,loc6→Secondary 副活动位)显隐判定用。
    /// 图标开关完全由 war_state 驱动:war_state 1..4 且未报名(is_sign!=1)时显示,否则删除
    /// (对标老端 RegisterProtocal(13700) 里的 addIcon/deleteIcon 分支)。
    /// 面板/报名/竞猜/战斗等玩法协议(13701-13724)与其 UI 本期一律不移植,待用户验收。
    /// </summary>
    public sealed class DiamondFightModel
    {
        public static readonly DiamondFightModel Instance = new DiamondFightModel();
        private DiamondFightModel() { }

        /// <summary>主界面图标类型(对标老端 DiamondFightModel.MODULE_ID=137)。</summary>
        public const string ICON_TYPE = "137";

        // 活动阶段 war_state(对标老端 stage_info.war_state):0 未开启 / 1 报名中 / 2..4 进行中 / 5 结束。
        public int WarState;
        // 当前阶段结束时间戳(对标老端 13700 EndTime,面板倒计时用,本期图标不需但按协议读入保留)。
        public int EndTime;

        /// <summary>
        /// 是否已报名(对标老端 user_info.is_sign)。老端 is_sign 由面板协议 13701/报名回包 13702 置 1,
        /// 由 13700 的 war_state==0/5 分支复位为 0。本期只注册 13700、不移植 13701/13702,
        /// 故 is_sign 恒为 0(仅供图标条件 is_sign!=1 表达完整,与老端语义一致):报名后老端会隐藏图标,
        /// 移植面板/报名协议后在此接入即可自动生效。
        /// </summary>
        public int IsSign;

        /// <summary>写入 13700 下发的阶段信息(对标老端 SetStageInfo)。</summary>
        public void SetStageInfo(int warState, int endTime)
        {
            WarState = warState;
            EndTime = endTime;
        }

        /// <summary>
        /// 图标开启状态(对标老端 13700 分支:war_state>0 && war_state<5,且非「war_state==1 报名中已报名」)。
        /// 即 war_state ∈ [1,4] 且 is_sign!=1 显示,否则删除。门槛(等级/开服天)另由图标配置
        /// (FunIsOpenByIconType 的 open_lv/open_day)与服务端 war_state 下发共同把控。
        /// </summary>
        public bool GetIconOpenState()
        {
            return WarState >= 1 && WarState <= 4 && IsSign != 1;
        }

        /// <summary>
        /// 图标角标文案(对标老端:war_state==1 未报名→"报名中",war_state>=2→"进行中")。
        /// 仅在 GetIconOpenState()==true 时取用。
        /// </summary>
        public string GetIconText()
        {
            if (WarState == 1) return "报名中";
            if (WarState >= 2) return "进行中";
            return "";
        }

        public void Reset()
        {
            WarState = 0;
            EndTime = 0;
            IsSign = 0;
        }
    }
}
