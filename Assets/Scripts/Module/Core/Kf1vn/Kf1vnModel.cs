namespace Shenxiao.Module.Core.Kf1vn
{
    /// <summary>
    /// 诸天王者(跨服1vn)数据(对标老客户端 Kf1vnModel)。承载 62101 阶段信息与 62100
    /// 活动报名原始快照，只供主界面图标(621,loc6)显隐与文字判定：stage==1 且明确已报名
    /// (is_sign==1)时隐藏，未报名显示“报名中”，stage>=2 且 stage!=6 显示“进行中”。
    /// 62102 报名交易及 62104~62136 竞猜/战斗/结算玩法与面板均未移植。
    /// </summary>
    public sealed class Kf1vnModel
    {
        public static readonly Kf1vnModel Instance = new Kf1vnModel();
        private Kf1vnModel() { }

        /// <summary>主界面图标类型(对标老端 Kf1vnModel.ICON_TYPE="621",位置 loc6 通知区)。</summary>
        public const string ICON_TYPE = "621";

        // 62101 阶段信息(对标老端 stage_info)。stage 是图标显隐/文字的主驱动量;
        // turn/edtime/sub_stage/sub_edtime 面板与战斗流程用,本期图标不需,按协议读齐即可。
        public int Stage;       // 活动阶段(0 未开启,1 报名,2/3 进行,5 擂主战,6 结束等)
        public int Turn;        // 当前轮次
        public long Edtime;     // u32 Unix 秒
        public int SubStage;    // 子阶段
        public long SubEdtime;  // u32 Unix 秒

        /// <summary>是否已下发过阶段信息(老端 stage_info 为 false 时 ShowIcon 直接 return)。</summary>
        public bool HasStageInfo;
        /// <summary>是否收到过 62100 原始活动信息。</summary>
        public bool HasActivityInfo { get; private set; }
        public byte IsSign { get; private set; }
        public uint SignNum { get; private set; }
        public ushort DefNum { get; private set; }
        public byte Zone { get; private set; }

        public void SetActivityInfo(byte isSign, uint signNum, ushort defNum, byte zone)
        {
            HasActivityInfo = true;
            IsSign = isSign;
            SignNum = signNum;
            DefNum = defNum;
            Zone = zone;
        }

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
        /// stage==1 时再按 62100 原始 is_sign 精确细化：只有协议定义的已报名值 1 隐藏；
        /// 0 与未知原始值仍保留报名入口，避免把异常值乐观解释成已报名。
        /// </summary>
        public bool GetEntranceOpenState()
        {
            return HasStageInfo && Stage >= 1 && Stage != 6
                && !(Stage == 1 && HasActivityInfo && IsSign == 1);
        }

        /// <summary>
        /// 图标文字(对标老端 ShowIcon:stage==1→"报名中",stage>=2→"进行中")。
        /// stage==1 且已报名时入口已隐藏，因此可见的报名阶段统一显示“报名中”。
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
            HasActivityInfo = false;
            IsSign = 0;
            SignNum = 0;
            DefNum = 0;
            Zone = 0;
        }
    }
}
