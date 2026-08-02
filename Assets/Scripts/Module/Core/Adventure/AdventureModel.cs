using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;

namespace Shenxiao.Module.Core.Adventure
{
    /// <summary>
    /// 天天冒险数据(对标老客户端 AdventureModel)。只承载 42700 下发的活动时间窗信息
    /// (stage/start_time/end_time),供主界面活动图标(42701/42702)显隐判定用。
    /// 开启判定对标老端 SetTimeInfo 的 act_open_ 计算:神装功能已开(GodEquipBuildView)
    /// 且 stage>0 且当前时间落在 [start_time, end_time) 内。
    /// 本期保留图标与42701主状态；42702-42706投掷/重置/商店/购买链与UI待用户验收。
    /// </summary>
    public sealed class AdventureModel
    {
        public static readonly AdventureModel Instance = new AdventureModel();
        private AdventureModel() { }

        // 主界面图标类型(对标老端 ai_mgr.addIcon("42701")/("42702"))。
        // 老端按星期几从 config_adventure_kv[12] 映射当天用哪个(默认 "42701",另一版为 "42702")。
        // 该"周几→图标"配置尚未移植到 Unity,故当前恒取默认 ICON_TYPE_A;关闭时两个都删,不遗留。
        public const string ICON_TYPE_A = "42701"; // 默认版
        public const string ICON_TYPE_B = "42702"; // 周几切换的另一版(config_adventure_kv[12] 决定)

        /// <summary>老端 CheckFuncOpenState 的功能视图名(神装打造,活动前置)。</summary>
        public const string FUNC_VIEW = "GodEquipBuildView";

        // 42700 活动时间窗信息(对标老端 info_.stage / start_time / end_time)
        public int Stage;        // 活动阶段(>0 才算开启)
        public long StartTime;   // 活动开始时间戳(秒)
        public long EndTime;     // 活动结束时间戳(秒)
        public bool HasBoardState { get; private set; }
        public ushort Circle { get; private set; }
        public ushort Location { get; private set; }
        public ushort LeftTimes { get; private set; }
        public ushort ThrowTimes { get; private set; }
        public ushort FreeResetTimes { get; private set; }
        public ushort FreeThrowTimes { get; private set; }
        public void ReplaceBoardState(ushort circle, ushort location, ushort leftTimes, ushort throwTimes, ushort freeResetTimes, ushort freeThrowTimes)
        {
            Circle = circle; Location = location; LeftTimes = leftTimes; ThrowTimes = throwTimes; FreeResetTimes = freeResetTimes; FreeThrowTimes = freeThrowTimes; HasBoardState = true;
        }

        public void SetTimeInfo(int stage, long startTime, long endTime)
        {
            Stage = stage;
            StartTime = startTime;
            EndTime = endTime;
        }

        /// <summary>
        /// 活动开启态(对标老端 SetTimeInfo 的 act_open_ 判定):
        /// 神装功能已开(GodEquipBuildView) 且 stage>0 且 start_time <= now < end_time。
        /// 神装门槛由 FuncOpenConfig(开服天/等级/前置任务)把控;时间窗由服务端 42700 下发。
        /// </summary>
        public bool IsActivityOpen()
        {
            if (!FuncOpenConfig.CheckFuncOpenState(FUNC_VIEW)) return false;
            if (Stage <= 0) return false;
            long now = TimeUtil.NowSec();
            return now >= StartTime && now < EndTime;
        }

        /// <summary>
        /// 当前应显示的图标类型(对标老端 iocn_type = kv_value[cur_ween],默认 "42701")。
        /// "周几→图标"映射(config_adventure_kv[12])尚未移植,恒返回默认版;
        /// 另一版 ICON_TYPE_B 在关闭/刷新时一并删除,避免残留。
        /// </summary>
        public string GetCurIconType()
        {
            return ICON_TYPE_A;
        }

        public void Reset()
        {
            Stage = 0;
            StartTime = 0;
            EndTime = 0;
            HasBoardState = false; Circle = 0; Location = 0; LeftTimes = 0; ThrowTimes = 0; FreeResetTimes = 0; FreeThrowTimes = 0;
        }
    }
}
