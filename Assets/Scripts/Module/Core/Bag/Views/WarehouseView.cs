using Shenxiao.Generated.UI.Bag;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 仓库面板(对标老客户端 bag/WarehouseView.ts):左右两个物品格子滚动列表(背包/仓库,_Scroller1/2)+
    /// 扩展/熔炼/使用/红装 按钮 + 红点。
    ///
    /// 降级:背包/仓库数据(BagModel/WarehouseModel/GoodsModel)、各功能与子窗路由未移植 → 红点 + 物品模板隐藏;
    /// 按钮点击打日志「待对接」;两格子暂空(待数据 + BaseAwardItem 填充)。事件驱动窗口,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class WarehouseView : WarehouseViewBind
    {
        protected override void OnInit()
        {
            if (suitRed != null) suitRed.gameObject.SetActive(false);
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);

            BindBtn(expandBtn1, "扩展背包 ExpandBagView");
            BindBtn(expandBtn2, "扩展仓库 ExpandBagView");
            BindBtn(smeltBtn, "熔炼 BagSmeltView");
            BindBtn(useBtn, "使用");
            BindBtn(redequipBtn, "红装");
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("Bag", "仓库打开 → 待对接 背包/仓库数据(BagModel/WarehouseModel)铺格子");
        }

        private void BindBtn(Image img, string label)
        {
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("Bag", "点击仓库按钮[{0}] → 待对接", label));
        }
    }
}
