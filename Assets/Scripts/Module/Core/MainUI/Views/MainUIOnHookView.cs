using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>挂机经验入口；显隐只等权威挂机/场景/任务数据接入，不在主界面硬编码开放条件。</summary>
    public sealed class MainUIOnHookView : MainUIOnHookViewBind
    {
        protected override void OnInit()
        {
            if (_box_outline_exp != null) _box_outline_exp.gameObject.SetActive(false);
            if (_box_old_outline_exp != null) _box_old_outline_exp.gameObject.SetActive(false);

            RouteClick(_box_outline_exp, "onhook");
            RouteClick(_box_exp_btn, "onhook");
            RouteClick(_box_old_outline_exp, "onhook");
            RouteClick(_img_add, "onhook_addition");
        }

        private static void RouteClick(Component target, string viewKey)
        {
            if (target != null) UIUtil.AddClick(target, () => MainUIRouter.Open(viewKey));
        }
    }
}
