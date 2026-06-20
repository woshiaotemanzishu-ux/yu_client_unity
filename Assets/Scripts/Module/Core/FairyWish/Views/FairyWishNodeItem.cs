using System;
using Shenxiao.Generated.UI.FairyWish;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.FairyWish
{
    /// <summary>
    /// 仙缘许愿节点项(对标老客户端 fairyWish/FairyWishNodeItem.ts):节点图标(_img_node)+ 选中态(_img_select)+ 红点(_img_red),点击(_click)选节点。
    ///
    /// SetSelected(bool) + SetClick(cb)。降级:节点图标/数据待接 → 选中切换 + 点击可用,红点 OnInit 隐藏。由 FairyWishView 克隆。
    /// </summary>
    public sealed class FairyWishNodeItem : FairyWishNodeItemBind
    {
        private Action _onClick;

        protected override void OnInit()
        {
            if (_img_red != null) _img_red.gameObject.SetActive(false);
            if (_img_select != null) _img_select.gameObject.SetActive(false);
            BindClick(_click, () => { if (_onClick != null) _onClick(); });
        }

        public void SetClick(Action onClick) { _onClick = onClick; }

        public void SetSelected(bool selected)
        {
            if (_img_select != null) _img_select.gameObject.SetActive(selected);
        }

        private void BindClick(Component area, Action onClick)
        {
            if (area == null) return;
            Image img = area.GetComponent<Image>();
            if (img == null) img = area.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, onClick);
        }
    }
}
