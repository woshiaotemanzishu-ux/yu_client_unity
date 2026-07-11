using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.ActivityForeshow
{
    /// <summary>
    /// 活动预告/日历数据(对标老客户端 commonModel/ActivityForeshowManager)。本期只承载"预告图标"
    /// 显隐所需的最小状态,不做完整的日历时间窗/倒计时/提示弹窗。
    ///
    /// 老端预告管理器的图标由两个条件驱动:①客户端 XianShiActivity 日历时间窗(time_region,判定
    /// UnOpen/Show/CountDown/InProgress/Finish);②各活动自己的开启态检查(CheckActivityOpenState)。
    /// 本期未移植 ①(纯客户端配置,量大),只忠实实现 ② 里唯一带服务端信号的那一路:
    ///
    /// - 领地夺宝(652@31@0):由协议 65208 驱动。65208 回包 [dun_id, end_time],老端存为 timeMsgData,
    ///   checkOpen() 用 end_time 判活动是否仍开启。
    /// - 九魂圣殿(135@0@1 / 135@0@2):老端开启态=NineSkyModel.GetOpenState()=功能开放
    ///   (CommonManager.CheckFuncOpenState("BattleFieldEnterView")),这是纯客户端功能开放+日历配置,
    ///   无任何服务端协议信号。按"无服务端信号"约定用静态开关兜底(见 NineSkyForeshowEnabled)。
    /// </summary>
    public sealed class ActivityForeshowModel
    {
        public static readonly ActivityForeshowModel Instance = new ActivityForeshowModel();
        private ActivityForeshowModel() { }

        /// <summary>九魂圣殿 预告图标一(对标老端 FORE_TYPE.NINE_SKY_ONE)。</summary>
        public const string ICON_NINE_SKY_ONE = "135@0@1";
        /// <summary>九魂圣殿 预告图标二(对标老端 FORE_TYPE.NINE_SKY_TWO)。</summary>
        public const string ICON_NINE_SKY_TWO = "135@0@2";
        /// <summary>领地夺宝 预告图标(对标老端 FORE_TYPE.SNATCHTREASURE)。</summary>
        public const string ICON_SNATCH_TREASURE = "652@31@0";

        /// <summary>
        /// 九魂圣殿两图标是否放行显示。老端由 NineSkyModel.GetOpenState()(功能开放 BattleFieldEnterView)
        /// + XianShiActivity 日历时间窗共同把控 —— 二者均为客户端配置,无任何服务端信号(无对应协议)。
        /// 本期未移植该功能开放/日历配置,故默认 false(不显示),避免脱离时间窗后常驻误显;待日历/功能开放
        /// 配置移植后再置 true 放开。参照 HARD CONSTRAINT:无服务端信号的图标用静态 bool 默认 false 兜底。
        /// </summary>
        public static bool NineSkyForeshowEnabled = false;

        // ---- 领地夺宝 65208 时间信息(对标老端 SnatchTreasureModel.timeMsgData = {dunid, end_time}) ----
        public bool HasSnatchInfo;   // 是否已收到 65208
        public int SnatchDunId;      // 当前领地夺宝副本 id(dun_id)
        public long SnatchEndTime;   // 本轮结束时间戳(unix 秒),0 表示无有效会话

        public void SetSnatchTimeMsg(int dunId, long endTime)
        {
            HasSnatchInfo = true;
            SnatchDunId = dunId;
            SnatchEndTime = endTime;
        }

        /// <summary>
        /// 领地夺宝预告图标是否应显示。对标老端 SnatchTreasureModel.checkOpen():
        ///   let flag = true; if (timeMsgData && end_time != 0 && serverTime >= end_time) flag = false; return flag;
        /// 老端 checkOpen 默认 true(含 end_time==0),因真正的显隐主闸是 XianShiActivity 日历时间窗(本期未移植)。
        /// 脱离日历后以 65208 的 end_time 作为唯一驱动:仅当存在未来 end_time(领地夺宝会话进行中)才显示;
        /// end_time==0 或已过期均不显示,避免无时间窗时常驻误显。
        /// </summary>
        public bool GetSnatchOpenState()
        {
            return HasSnatchInfo && SnatchEndTime != 0 && TimeUtil.NowSec() < SnatchEndTime;
        }

        public void Reset()
        {
            HasSnatchInfo = false;
            SnatchDunId = 0;
            SnatchEndTime = 0;
        }
    }
}
