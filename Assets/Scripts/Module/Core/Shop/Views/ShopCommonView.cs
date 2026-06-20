using Shenxiao.Generated.UI.Shop;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;

namespace Shenxiao.Module.Core.Shop
{
    /// <summary>
    /// 常规商城(对标老客户端 shop/ShopCommonView.ts):左侧系列页签(_list_tab_con 克隆 ShopSeriesTab)+ 商品列表
    /// (scroll/scroll_group 克隆 ShopItem/ShopLimitItem)+ 货币显示(money_conta/num、money_conta2/num2)+
    /// 空态(none_conta:无商品时显示)+ 荣誉/勋章提示(gloryLabel/_lb_medal_tips)。
    ///
    /// 降级:ShopModel/商品配置(config_shop)/货币(GoodsModel)/购买协议、ShopItem/ShopSeriesTab/ShopLimitItem 列表项与
    /// LoopScrowViewMgr 均未移植 → 列表空(显 none_conta)、货币 0、_tpl_* 模板隐藏;OnShow 打日志。无独立关闭按钮 →
    /// 由 HUD 商城按钮再点关闭(ShopFlow.Toggle)。事件驱动窗口,默认关闭、不进 FirstPass。神秘商店/批量购买等分类后续 tick 补。
    /// </summary>
    public sealed class ShopCommonView : ShopCommonViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_ShopLimitItem != null) _tpl_ShopLimitItem.SetActive(false);
            if (_tpl_ShopSeriesTab != null) _tpl_ShopSeriesTab.SetActive(false);
            if (_tpl_ShopItem != null) _tpl_ShopItem.SetActive(false);
        }

        protected override void OnShow(object args)
        {
            // 老端 LoadSuccess → 读 config_shop 铺页签/商品 + 刷货币。数据未移植 → 列表空、显空态、货币 0。
            if (none_conta != null) none_conta.gameObject.SetActive(true);
            if (num != null) num.text = "0";
            if (num2 != null) num2.text = "0";
            GameLog.Info("Shop", "常规商城打开 → 待对接 ShopModel(商品列表空/默认降级)");
        }
    }
}
