using Shenxiao.Generated.UI.Vip;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Vip
{
    /// <summary> 充值面板(对标老端 RechargeView.ts)。降级:Model/协议未移植 → 红点/模板隐藏、列表空;动作按钮日志;关闭按钮→Hide。 </summary>
    public sealed class RechargeView : RechargeViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_RechargeItem != null) _tpl_RechargeItem.SetActive(false);
            BindClose(_img_close);
            BindBtn(_btn_recharge, "充值");
            BindBtn(more_btn, "更多");
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("Vip", "充值面板打开 → 待对接 Model(默认降级)");
        }

        private void BindClose(Component target) { var i = target as Image ?? (target!=null?target.GetComponentInChildren<Image>(true):null); if (i==null) return; i.raycastTarget=true; UIUtil.AddClick(i, Hide); }
        private void BindBtn(Component target, string label) { var i = target as Image ?? (target!=null?target.GetComponentInChildren<Image>(true):null); if (i==null) return; i.raycastTarget=true; UIUtil.AddClick(i, () => GameLog.Info("Vip", "点击[" + label + "] → 待对接")); }
        private static void HideNode(Component c) { if (c != null) c.gameObject.SetActive(false); }
    }
}
