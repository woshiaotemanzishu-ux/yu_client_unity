using Shenxiao.Generated.UI.Bag;
using Shenxiao.Framework.UI;
using UnityEngine;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 熔炼属性弹窗(对标老客户端 bag/SmeltPropView.ts):显示熔炼累计总属性(标题 + 属性文本)。
    ///
    /// SetData(title, propText) 填标题 + 配表计算出的熔炼累计属性，并按 TMP preferred height 驱动真实滚动内容高度。
    /// 由 BagSmeltView 的属性按钮打开。事件驱动弹窗,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class SmeltPropView : SmeltPropViewBind
    {
        public sealed class Presentation
        {
            public string Title;
            public string Text;
        }

        public override UILayer Layer => UILayer.Popup;

        protected override void OnShow(object args)
        {
            if (args is Presentation data) SetData(data.Title, data.Text);
            ResetScrollToTop();
        }

        protected override void OnHide()
        {
            StopScroll();
            BagFlow.NotifyActivitySubHidden(this);
        }

        /// <summary>填熔炼属性(对标 SetData)。</summary>
        public void SetData(string title, string propText)
        {
            if (property_title != null && title != null) property_title.text = title;
            if (prop_text == null) return;

            prop_text.text = propText ?? "";
            prop_text.ForceMeshUpdate();
            float viewportHeight = prop_scroller != null && prop_scroller.viewport != null
                ? prop_scroller.viewport.rect.height
                : 0f;
            float contentHeight = Mathf.Max(viewportHeight, prop_text.preferredHeight);
            prop_text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
            if (Content != null)
                Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
        }

        private void ResetScrollToTop()
        {
            if (prop_scroller == null) return;
            StopScroll();
            if (prop_scroller.content != null)
            {
                Vector2 anchored = prop_scroller.content.anchoredPosition;
                anchored.y = 0f;
                prop_scroller.content.anchoredPosition = anchored;
            }
            prop_scroller.verticalNormalizedPosition = 1f;
        }

        private void StopScroll()
        {
            if (prop_scroller == null) return;
            prop_scroller.StopMovement();
            prop_scroller.velocity = Vector2.zero;
        }
    }
}
