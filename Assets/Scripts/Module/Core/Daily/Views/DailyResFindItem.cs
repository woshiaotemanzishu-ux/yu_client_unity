using Shenxiao.Generated.UI.Daily;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Daily
{
    /// <summary>
    /// 资源找回·行项(对标老客户端 daily/DailyResFindItem.ts):标题(title)+ 奖励格列表(_Scroller1/Content)+
    /// 找回按钮(findBtn,带价格 price/icon)/已完成(doneBtn)/免费提示(free_lb)。
    ///
    /// 老端 findBtn → Fire(OPEN_VIEW, "DailyResFindTipsView") 弹出找回确认。此处前向接:findBtn → DailyFlow.OpenSub("DailyResFindTipsView")。
    /// 降级:DailyModel/价格/次数协议未移植 → doneBtn 打日志;SetData 仅落标题占位。由 DailyResFindView 列表克隆。
    /// </summary>
    public sealed class DailyResFindItem : DailyResFindItemBind
    {
        protected override void OnInit()
        {
            BindClick(findBtn, () => DailyFlow.OpenSub("DailyResFindTipsView"));
            BindClick(doneBtn, () => GameLog.Info("Daily", "点击[资源找回·已完成] → 待对接"));
        }

        /// <summary>填资源找回行(对标 SetData):标题占位,奖励格/价格/状态待 DailyModel。</summary>
        public void SetData(string titleText)
        {
            if (title != null) title.text = titleText;
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
