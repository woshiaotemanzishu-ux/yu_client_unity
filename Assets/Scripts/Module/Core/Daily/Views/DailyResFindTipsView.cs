using Shenxiao.Generated.UI.Daily;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Daily
{
    /// <summary>
    /// 资源找回·确认弹窗(对标老客户端 daily/DailyResFindTipsView.ts):标题(title)+ 价格(price)+ 数量滑条(slider_group/_tpl_WithBtnHSlider)+
    /// 取消(cancleBtn)/确认(confirmBtn)/关闭(closeBtn)。由 DailyResFindItem 的找回按钮经 DailyFlow.OpenSub 叠开。
    ///
    /// 降级:DailyModel/找回次数·价格协议、数量滑条均未移植 → 滑条模板隐藏;closeBtn/cancleBtn → Hide 关闭;confirmBtn 打日志降级。
    /// </summary>
    public sealed class DailyResFindTipsView : DailyResFindTipsViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_WithBtnHSlider != null) _tpl_WithBtnHSlider.SetActive(false);
            if (closeBtn != null) { closeBtn.raycastTarget = true; UIUtil.AddClick(closeBtn, Hide); }
            BindClick(cancleBtn, Hide);
            BindClick(confirmBtn, () => GameLog.Info("Daily", "点击[资源找回·确认] → 待对接 DailyModel"));
        }

        private static void BindClick(Component target, System.Action onClick)
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
