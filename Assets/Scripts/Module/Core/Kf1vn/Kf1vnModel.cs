namespace Shenxiao.Module.Core.Kf1vn
{
    /// <summary>
    /// 诸天王者(跨服1vn)数据(对标老客户端 Kf1vnModel)。只承载 62101 下发的阶段信息(stage_info),
    /// 供主界面图标(621,loc6)显隐与文字判定用。老端 ShowIcon 的驱动来源即 62101 的 stage:
    /// stage>=1 且 stage!=6 → 显示图标(报名中/进行中),否则删除。
    /// 大量玩法协议(62100 报名页/62104~62136 竞猜/战斗/结算等)与面板本期均未移植,只做图标。
    /// </summary>
    public sealed class Kf1vnModel
    {
        public static readonly Kf1vnModel Instance = new Kf1vnModel();
        private Kf1vnModel() { }

        /// <summary>主界面图标类型(对标老端 Kf1vnModel.ICON_TYPE="621",位置 loc6 通知区)。</summary>
        public const string ICON_TYPE = "621";

        // 62101 阶段信息(对标老端 stage_info)。stage 是图标显隐/文字的唯一驱动量;
        // turn/edtime/sub_stage/sub_edtime 面板与战斗流程用,本期图标不需,按协议读齐即可。
        public int Stage;       // 活动阶段(0 未开启,1 报名,2/3 进行,5 擂主战,6 结束等)
        public int Turn;        // 当前轮次
        public long Edtime;     // u32 Unix 秒
        public int SubStage;    // 子阶段
        public long SubEdtime;  // u32 Unix 秒

        /// <summary>是否已下发过阶段信息(老端 stage_info 为 false 时 ShowIcon 直接 return)。</summary>
        public bool HasStageInfo;

        public void SetStageInfo(int stage, int turn, long edtime, int subStage, long subEdtime)
        {
            Stage = stage;
            Turn = turn;
            Edtime = edtime;
            SubStage = subStage;
            SubEdtime = subEdtime;
            HasStageInfo = true;
        }

        /// <summary>
        /// 入口开启状态(对标老端 ShowIcon 的显隐条件:stage>=1 且 stage!=6 才显示,否则删除)。
        /// 服务端 62101 只在活动开启后才下发 stage>=1;活动未开/结束时 stage=0 或 6 → 关闭。
        /// 注:老端在 stage==1 且已报名(is_sign,来自 62100 报名页)时会隐藏图标,该细化依赖未移植的
        /// 62100,本期不做——仅按 stage 判显隐(报名阶段一律显示"报名中")。
        /// </summary>
        public bool GetEntranceOpenState()
        {
            return HasStageInfo && Stage >= 1 && Stage != 6;
        }

        /// <summary>
        /// 图标文字(对标老端 ShowIcon:stage==1→"报名中",stage>=2→"进行中")。
        /// 老端的"已报名"文字依赖 62100 的 is_sign,本期未移植,故报名阶段统一显示"报名中"。
        /// </summary>
        public string GetIconText()
        {
            if (Stage == 1) return "报名中";
            if (Stage >= 2) return "进行中";
            return "";
        }

        public void Reset()
        {
            Stage = 0;
            Turn = 0;
            Edtime = 0;
            SubStage = 0;
            SubEdtime = 0;
            HasStageInfo = false;
        }
    }
}
