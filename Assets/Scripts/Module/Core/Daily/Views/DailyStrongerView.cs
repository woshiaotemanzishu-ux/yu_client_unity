using Shenxiao.Generated.UI.Daily;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Daily
{
    /// <summary> 我要变强页(对标老端 daily/DailyStrongerView.ts,DailyView 标签内容)。降级:DailyModel/协议未移植 → 红点/模板隐藏、列表空;动作按钮日志;无关闭(每日窗框统一关闭)。 </summary>
    public sealed class DailyStrongerView : DailyStrongerViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_DailyStrongerItem != null) _tpl_DailyStrongerItem.SetActive(false);
            if (_tpl_DailyStrongerTab != null) _tpl_DailyStrongerTab.SetActive(false);
        }
        protected override void OnShow(object args)
        {
            GameLog.Info("Daily", "我要变强打开 → 待对接 DailyModel(默认降级)");
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
