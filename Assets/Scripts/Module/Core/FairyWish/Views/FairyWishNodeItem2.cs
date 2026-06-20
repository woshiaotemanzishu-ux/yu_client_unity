using System;
using Shenxiao.Generated.UI.FairyWish;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.FairyWish
{
    /// <summary>
    /// 仙缘许愿节点项2(对标老客户端 fairyWish/FairyWishNodeItem2.ts):比 NodeItem 多文本(_lb_text);节点图标+选中+红点,点击选。
    ///
    /// SetData(text) + SetSelected(bool) + SetClick(cb)。降级:节点图标/数据待接 → 文本/选中/点击可用,红点隐藏。由 FairyWishView 克隆。
    /// </summary>
    public sealed class FairyWishNodeItem2 : FairyWishNodeItem2Bind
    {
        private Action _onClick;

        protected override void OnInit()
        {
            if (_img_red != null) _img_red.gameObject.SetActive(false);
            if (_img_select != null) _img_select.gameObject.SetActive(false);
            BindClick(_click, () => { if (_onClick != null) _onClick(); });
        }

        public void SetData(string text)
        {
            if (_lb_text != null) _lb_text.text = text ?? "";
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
