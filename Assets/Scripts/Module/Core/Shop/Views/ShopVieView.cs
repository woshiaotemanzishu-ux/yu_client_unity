using Shenxiao.Generated.UI.Shop;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Shop
{
    /// <summary>
    /// 抢购商城(对标老客户端 shop/ShopVieView.ts,ShopView 抢购标签内容):限时抢购商品列表(_dgp_item/_scroll 克隆 ShopVieItem)+ 倒计时(_lb_time)。
    ///
    /// 降级:ShopModel/抢购协议、ShopVieItem 列表项未移植 → 模板隐藏、列表空、倒计时默认;OnShow 打日志。
    /// 无独立关闭按钮(商城窗框统一关闭)→ 作 ShopView 抢购标签内容,由 ShopFlow 经 ConfigureShared override 在该标签 reparent 进窗框内容区。事件驱动,不进 FirstPass。
    /// </summary>
    public sealed class ShopVieView : ShopVieViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_ShopVieItem != null) _tpl_ShopVieItem.SetActive(false);
        }

        protected override void OnShow(object args)
        {
            // 老端 open → 读 ShopModel 铺抢购商品 + 倒计时。数据未移植 → 列表空降级。
            GameLog.Info("Shop", "抢购商城打开 → 待对接 ShopModel(列表空降级)");
        }
    }
}
