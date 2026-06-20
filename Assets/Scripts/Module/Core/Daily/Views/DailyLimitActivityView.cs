using Shenxiao.Generated.UI.Daily;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Daily
{
    /// <summary> 限时活动页(对标老端 daily/DailyLimitActivityView.ts,DailyView 标签内容)。降级:DailyModel/协议未移植 → 红点/模板隐藏、列表空;动作按钮日志;无关闭(每日窗框统一关闭)。 </summary>
    public sealed class DailyLimitActivityView : DailyLimitActivityViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_DailyLimitActivityItem != null) _tpl_DailyLimitActivityItem.SetActive(false);
            if (_tpl_DailyBottomView != null) _tpl_DailyBottomView.SetActive(false);
        }
        protected override void OnShow(object args)
        {
            GameLog.Info("Daily", "限时活动打开 → 待对接 DailyModel(默认降级)");
        }
        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("Daily", "点击[" + label + "] → 待对接"));
        }
        private static void HideNode(Component c) { if (c != null) c.gameObject.SetActive(false); }
    }
}
