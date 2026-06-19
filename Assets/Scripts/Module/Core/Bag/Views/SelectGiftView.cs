using System;
using Shenxiao.Generated.UI.Bag;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 选择礼包弹窗(对标老客户端 bag/SelectGiftView.ts):打开自选礼包,列出可选物品(SelectGiftItem),选定后确定。
    ///
    /// 降级:礼包数据(BagModel/GoodsModel 自选礼包配置)+ 领取协议未移植 → 列表项模板隐藏、列表空、
    /// ok 点击打日志「待对接」;close → Hide。事件驱动弹窗,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class SelectGiftView : SelectGiftViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_SelectGiftItem != null) _tpl_SelectGiftItem.SetActive(false);
            BindBtn(close_btn, () => Hide());
            BindBtn(ok_btn, () => GameLog.Info("Bag", "选择礼包 确定 → 待对接 自选礼包领取协议(BagModel)"));
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("Bag", "选择礼包打开 → 待对接 可选物品列表(BagModel 自选礼包配置)");
        }

        private void BindBtn(Component target, Action onClick)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, onClick);
        }
    }
}
