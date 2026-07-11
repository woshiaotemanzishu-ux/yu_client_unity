namespace Shenxiao.Module.Core.KfHolyArea
{
    /// <summary>
    /// 神陨禁区(跨服圣域)数据(对标老客户端 KfHolyAreaModel,模块 KfHolyAreaDefine.Module_Id=284)。
    /// 本期只做主界面图标(284)显隐:承载 28410 下发的活动时间窗(act_start/act_end)。
    /// 老端 RefreshIcon 用 GetOpenState()(配置 open_lv + 开服天窗 open_min/open_max + 每日时段)判显隐——
    /// 那套阈值 Unity 侧尚未导入(config_c_sanctuary_type/kv),故图标的"等级/开服天"门槛改由 ActivityIconManager
    /// 的图标配置(config_c_function_icon "284" 的 open_lv/open_day)在 AddIcon 时统一把控;
    /// 本模型只承载服务端下发的"本期活动窗是否存在"这一驱动信号(act_end>0),对标 Festival 的 uid>0。
    /// 场景/建筑/积分/排行/拍卖等玩法协议(28400-28423)与面板均未移植,待用户验收。
    /// </summary>
    public sealed class KfHolyAreaModel
    {
        public static readonly KfHolyAreaModel Instance = new KfHolyAreaModel();
        private KfHolyAreaModel() { }

        /// <summary>主界面图标类型(对标老端 KfHolyAreaDefine.Module_Id=284,神陨禁区,loc5)。</summary>
        public const string ICON_TYPE = "284";

        // 28410 活动时间窗(对标老端 time_data = {act_start, act_end})
        public int ActStart; // 活动开始时间戳
        public int ActEnd;   // 活动结束时间戳(>0 表示本期神陨禁区已配置/开启)

        /// <summary>写入 28410 下发的活动时间窗。</summary>
        public void SetActTime(int actStart, int actEnd)
        {
            ActStart = actStart;
            ActEnd = actEnd;
        }

        /// <summary>
        /// 入口开启状态(对标老端 GetOpenState() 的"活动本期在运行"这一支)。
        /// 服务端 do_handle(28410):仅在开服天数 > 1 才 get_act_opentime 下发窗口,并在活动状态变化时广播 28410;
        /// 故 act_end>0 即"本期神陨禁区已开启"的服务端驱动信号(对标 Festival 的 uid>0)。
        /// 主角等级/开服天门槛由 ActivityIconManager 的图标配置(284)在 AddIcon 时另行把控,此处不重复计算。
        /// </summary>
        public bool GetEntranceOpenState()
        {
            return ActEnd > 0;
        }

        public void Reset()
        {
            ActStart = 0;
            ActEnd = 0;
        }
    }
}
