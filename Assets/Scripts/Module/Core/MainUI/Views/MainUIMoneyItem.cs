using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 顶部货币项(对标老客户端 MainUIMoneyItem.ts)。
    /// 货币类型沿用老客户端:0=元宝(diamond/jin)、1=绑元(bind_diamond/jinLock)、2=铜币(gold/tong),
    /// 对应 RoleModel.Gold / BGold / Coin。图标走 GameResPath.GetIcon("mainUI", ...),
    /// 数值只读 RoleModel(唯一真相源,不造假数据)。铜币按老客户端 SetValue 的 money_type==2 分支
    /// 做万/亿缩写(<10000 原样)。值刷新由父级 MainUITopView 在 EVT_ROLE_INFO_UPDATE 时驱动,
    /// 货币项不自行注册事件(动态项无稳定 OnDispose,避免 On/Off 失配)。
    /// </summary>
    public sealed class MainUIMoneyItem : MainUIMoneyItemBind
    {
        public const int TYPE_GOLD = 0;  // 元宝
        public const int TYPE_BGOLD = 1; // 绑元
        public const int TYPE_COIN = 2;  // 铜币

        private const long WAN = 10000L;
        private const long BAI_WAN = 1000000L;
        private const long YI = 100000000L;
        private const long BAI_YI = 10000000000L;

        private int _moneyType = -1;

        /// <summary>设置货币类型:换图标 + 刷新数值(对标 SetMoneyType→RefreshType)。</summary>
        public void SetMoneyType(int type)
        {
            _moneyType = type;
            string iconRes = IconResOf(type);
            if (string.IsNullOrEmpty(iconRes))
            {
                GameLog.Warn("MainUI", "未知货币类型: {0}", type);
                return;
            }
            // 换图保留场景尺寸(对标 Laya skin=,nativeSize:false 不撑成原图大小)
            _ = ResManager.SetImageAsync(_img_icon, GameResPath.GetIcon("mainUI", iconRes), nativeSize: false);
            Refresh();
        }

        /// <summary>仅刷新数值(EVT_ROLE_INFO_UPDATE 时由父级调用,对标 SetValue)。</summary>
        public void Refresh()
        {
            if (_moneyType < 0) return;
            _lb_value.text = FormatValue(_moneyType);
        }

        private static string IconResOf(int type)
        {
            switch (type)
            {
                case TYPE_GOLD: return "diamond";
                case TYPE_BGOLD: return "bind_diamond";
                case TYPE_COIN: return "gold";
                default: return null;
            }
        }

        private static string FormatValue(int type)
        {
            RoleModel m = RoleModel.Instance;
            switch (type)
            {
                case TYPE_GOLD: return m.Gold.ToString();
                case TYPE_BGOLD: return m.BGold.ToString();
                case TYPE_COIN: return FormatCoin(m.Coin);
                default: return "0";
            }
        }

        /// <summary>铜币缩写,1:1 对标 MainUIMoneyItem.ts SetValue 的 money_type==2 分支。</summary>
        private static string FormatCoin(long value)
        {
            if (value >= WAN && value < BAI_WAN)
            {
                return (value / (double)WAN).ToString("0.00") + "万";
            }
            if (value >= BAI_WAN && value < YI)
            {
                return (value / WAN) + "万";
            }
            if (value >= YI && value < BAI_YI)
            {
                return (value / (double)YI).ToString("0.00") + "亿";
            }
            if (value >= BAI_YI)
            {
                return (value / YI) + "亿";
            }
            return value.ToString();
        }
    }
}
