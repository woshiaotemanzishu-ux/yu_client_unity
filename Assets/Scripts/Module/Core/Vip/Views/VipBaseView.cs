using Shenxiao.Generated.UI.Vip;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Vip
{
    /// <summary> VIP面板(对标老端 VipBaseView.ts)。降级:Model/协议未移植 → 红点/模板隐藏、列表空;动作按钮日志;关闭按钮→Hide。 </summary>
    public sealed class VipBaseView : VipBaseViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_VipPrivilegeCardView != null) _tpl_VipPrivilegeCardView.SetActive(false);
            if (_tpl_VipCardItem != null) _tpl_VipCardItem.SetActive(false);
            if (_tpl_VipSubFlagItem != null) _tpl_VipSubFlagItem.SetActive(false);
            if (_tpl_VipPrivilegeShowView != null) _tpl_VipPrivilegeShowView.SetActive(false);
            if (_tpl_VipAwardItem != null) _tpl_VipAwardItem.SetActive(false);
            if (_tpl_VipInstructionItem != null) _tpl_VipInstructionItem.SetActive(false);
            if (_tpl_VipTabButton != null) _tpl_VipTabButton.SetActive(false);
            if (_tpl_VipTopCardItem != null) _tpl_VipTopCardItem.SetActive(false);

            BindClose(close_btn);
            BindBtn(recharge_btn, "VIP·充值");
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("Vip", "VIP面板打开 → 待对接 Model(默认降级)");
        }

        private void BindClose(Component target)
        {
            var i = target as Image ?? (target != null ? target.GetComponentInChildren<Image>(true) : null);
            if (i == null) return;
            i.raycastTarget = true;
            UIUtil.AddClick(i, Hide);
        }

        private void BindBtn(Component target, string label)
        {
            var i = target as Image ?? (target != null ? target.GetComponentInChildren<Image>(true) : null);
            if (i == null) return;
            i.raycastTarget = true;
            UIUtil.AddClick(i, () => GameLog.Info("Vip", "点击[" + label + "] → 待对接"));
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
