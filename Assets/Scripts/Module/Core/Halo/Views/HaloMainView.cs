using Shenxiao.Generated.UI.Halo;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Halo
{
    /// <summary> 光环面板(对标老端 HaloMainView.ts)。降级:Model/协议未移植 → 红点/模板隐藏、列表空;动作按钮日志;关闭按钮→Hide。 </summary>
    public sealed class HaloMainView : HaloMainViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_HaloItem != null) _tpl_HaloItem.SetActive(false);
            BindClose(img_btn_close);
            BindBtn(img_btn_buy, "光环·购买");
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("Halo", "光环面板打开 → 待对接 Model(默认降级)");
        }

        private void BindClose(Component target) { var i = target as Image ?? (target != null ? target.GetComponentInChildren<Image>(true) : null); if (i == null) return; i.raycastTarget = true; UIUtil.AddClick(i, Hide); }
        private void BindBtn(Component target, string label) { var i = target as Image ?? (target != null ? target.GetComponentInChildren<Image>(true) : null); if (i == null) return; i.raycastTarget = true; UIUtil.AddClick(i, () => GameLog.Info("Halo", "点击[" + label + "] → 待对接")); }
        private static void HideNode(Component c) { if (c != null) c.gameObject.SetActive(false); }
    }
}
