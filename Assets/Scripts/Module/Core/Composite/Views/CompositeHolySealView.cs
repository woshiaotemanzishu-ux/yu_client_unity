using Shenxiao.Generated.UI.Composite;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Composite
{
    /// <summary> 影骸战衣合成页(对标老端 composite/CompositeHolySealView.ts,CompositeView 标签内容)。降级:CompositeModel/协议未移植 → 红点/模板隐藏、列表空;动作按钮日志;无关闭(合成窗框统一关闭)。 </summary>
    public sealed class CompositeHolySealView : CompositeHolySealViewBind
    {
        protected override void OnInit()
        {
            HideNode(_img_composite_red);
            if (_tpl_EquipmentItem != null) _tpl_EquipmentItem.SetActive(false);
            if (_tpl_CompositeHolySealMatItem != null) _tpl_CompositeHolySealMatItem.SetActive(false);
            if (_tpl_UIVerTabBar != null) _tpl_UIVerTabBar.SetActive(false);
        }
        protected override void OnShow(object args)
        {
            GameLog.Info("Composite", "影骸战衣合成打开 → 待对接 CompositeModel(默认降级)");
        }
        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("Composite", "点击[" + label + "] → 待对接"));
        }
        private static void HideNode(Component c) { if (c != null) c.gameObject.SetActive(false); }
    }
}
