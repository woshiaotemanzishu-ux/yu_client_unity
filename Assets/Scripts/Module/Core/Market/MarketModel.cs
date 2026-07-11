namespace Shenxiao.Module.Core.Market
{
    /// <summary>
    /// 市场(交易行)数据(对标老客户端 commonModel/MarketModel,只取图标相关部分)。
    /// 15121 下发跨服市场开放时间 open_time;据此在两个主界面图标间切换:未到跨服开放时间
    /// 显示本服市场 151,已到显示跨服市场 151@1(对标老端 showIcon:is_kf?151@1:151)。
    /// 交易/上架/下架/购买/求购/记录等玩法(15100-15120/15122)与配置(type_cfg 等)全部不移植,本期只做图标。
    /// </summary>
    public sealed class MarketModel
    {
        public static readonly MarketModel Instance = new MarketModel();
        private MarketModel() { }

        /// <summary>本服市场图标(对标老端 showIcon 的 "151")。</summary>
        public const string ICON_TYPE_LOCAL = "151";

        /// <summary>跨服市场图标(对标老端 showIcon 的 "151@1",loc1 网格)。</summary>
        public const string ICON_TYPE_KF = "151@1";

        // 15121 跨服市场开放时间戳(unix 秒,0 表示未配置/未开放)。对标老端 kf_open_time。
        public int KfOpenTime;

        // 跨服市场是否已开放(对标老端 kf_open = open_time>0 && serverTime>open_time)。
        public bool KfOpen;

        public void SetKfOpen(int openTime, bool kfOpen)
        {
            KfOpenTime = openTime;
            KfOpen = kfOpen;
        }

        /// <summary>当前应显示的图标(对标老端 showIcon:kf_open?151@1:151)。</summary>
        public string GetShowIconType()
        {
            return KfOpen ? ICON_TYPE_KF : ICON_TYPE_LOCAL;
        }

        /// <summary>当前应删除的另一图标(对标老端 showIcon 里 del_icon:kf_open?151:151@1)。</summary>
        public string GetHideIconType()
        {
            return KfOpen ? ICON_TYPE_LOCAL : ICON_TYPE_KF;
        }

        public void Reset()
        {
            KfOpenTime = 0;
            KfOpen = false;
        }
    }
}
