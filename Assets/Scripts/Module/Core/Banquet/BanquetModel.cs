namespace Shenxiao.Module.Core.Banquet
{
    /// <summary>
    /// 婚宴(婚礼)数据(对标老客户端 BanquetModel)。巨型婚宴系统本期只移植主界面两个图标所需的最小状态:
    ///   172@1 婚礼(loc6):17256 婚礼召集(SetBanquetCall)—— type==1 且 wedding_list 非空时显示。
    ///   172@2 宾客管理(loc6):17249 婚礼状态(On17249)—— now_wedding_state==2 时显示。
    /// 婚宴报名/邀请/场景玩法(17250-17298 等)全部不做。只承载图标显隐判定用的字段。
    /// </summary>
    public sealed class BanquetModel
    {
        public static readonly BanquetModel Instance = new BanquetModel();
        private BanquetModel() { }

        /// <summary>婚礼图标(对标老端 addIcon("172@1"))。婚礼活动开启时显示,配置 open_lv=130、controll_by_own_fun=true。</summary>
        public const string ICON_TYPE_WEDDING = "172@1";

        /// <summary>宾客管理图标(对标老端 addIcon("172@2"))。now_wedding_state==2 时显示,配置 open_lv=0、controll_by_own_fun=true。</summary>
        public const string ICON_TYPE_GUEST = "172@2";

        // 17249 婚礼状态(对标老端 On17249 / banquetModel.banqState)
        public int BanqState;       // 上一次 now_wedding_state,供 172@2 的 tri-state 增删判定(2→0 才删)
        public int NowWeddingState; // 当前 now_wedding_state:0 无婚宴 / 2 婚宴报名开启(其余状态图标不动)
        public int BeginTime;       // 婚宴开始时间戳(老端据此算图标倒计时文本,本期图标只做显隐,读存不用于判定)

        // 17256 婚礼召集(对标老端 SetBanquetCall / weddingInfo)
        public bool WeddingActive;  // type==1 且 wedding_list 非空 → 有婚礼进行/待开始

        /// <summary>172@2 宾客管理入口开启(对标老端 On17249:now_wedding_state==2)。</summary>
        public bool GetGuestIconOpen()
        {
            return NowWeddingState == 2;
        }

        /// <summary>172@1 婚礼入口开启(对标老端 SetBanquetCall:type==1 且 wedding_list 非空)。</summary>
        public bool GetWeddingIconOpen()
        {
            return WeddingActive;
        }

        public void Reset()
        {
            BanqState = 0;
            NowWeddingState = 0;
            BeginTime = 0;
            WeddingActive = false;
        }
    }
}
