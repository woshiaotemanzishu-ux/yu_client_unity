using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Activity;
using Shenxiao.Module.Core.CustomActivity;

namespace Shenxiao.Module.Core.Activity
{
    public sealed class RechargeReturnView : RechargeReturnViewBind
    {
        private CustomActivityModel.ActEntry _info;

        protected override void OnInit()
        {
            ActivityViewUtil.BindClick(_gp_btn, OnClick);
        }

        protected override void OnShow(object args)
        {
            _info = args as CustomActivityModel.ActEntry;
            if (_lb_btn != null)
            {
                if (_info != null && _info.BaseType == 86 && _info.ShowId == 2) _lb_btn.text = "查看七日活动";
                else _lb_btn.text = "前往充值";
            }
        }

        private void OnClick()
        {
            if (_info != null && _info.BaseType == 86 && _info.ShowId == 2)
            {
                GameLog.Warn("Activity", "七日活动目标路由尚未完成，保留充值返还页");
                return;
            }
            ActivityViewUtil.OpenRecharge();
        }
    }
}
