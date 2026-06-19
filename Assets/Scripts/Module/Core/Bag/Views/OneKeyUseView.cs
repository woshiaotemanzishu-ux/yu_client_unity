using System;
using Shenxiao.Generated.UI.Bag;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 一键使用弹窗(对标老客户端 bag/OneKeyUseView.ts):列出可一键使用的物品(分类页签 OneKeyUseTab + 列表),
    /// 勾选后一键使用。closeBtn 关闭、useBtn 一键使用。
    ///
    /// 降级:背包/一键使用数据(BagModel)、使用协议未移植 → 页签模板隐藏、列表空、显 nothingLb 空提示、
    /// useBtn 点击打日志「待对接」;close → Hide。事件驱动弹窗,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class OneKeyUseView : OneKeyUseViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_OneKeyUseTab != null) _tpl_OneKeyUseTab.SetActive(false);
            BindBtn(closeBtn, () => Hide());
            BindBtn(useBtn, () => GameLog.Info("Bag", "一键使用 → 待对接 BagModel 一键使用协议"));
        }

        protected override void OnShow(object args)
        {
            // 无数据 → 显空提示(对标 nothingLb)。
            if (nothingLb != null) nothingLb.gameObject.SetActive(true);
            GameLog.Info("Bag", "一键使用打开 → 待对接 可用物品列表(BagModel)");
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
