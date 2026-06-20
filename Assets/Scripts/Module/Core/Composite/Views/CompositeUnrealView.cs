using Shenxiao.Generated.UI.Composite;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Composite
{
    /// <summary> 九霄冥饰合成页(对标老端 composite/CompositeUnrealView.ts,CompositeView 标签内容)。降级:CompositeModel/协议未移植 → 红点/模板隐藏、列表空;动作按钮日志;无关闭(合成窗框统一关闭)。 </summary>
    public sealed class CompositeUnrealView : CompositeUnrealViewBind
    {
        protected override void OnInit()
        {
            HideNode(_img_one_key_add_red);
            HideNode(_img_composite_red);
            HideNode(_sub_btn_red0);
            HideNode(_sub_btn_red1);
            HideNode(_sub_btn_red2);
            HideNode(_sub_btn_red3);
            HideNode(_sub_btn_red4);
            HideNode(_sub_btn_red5);
            if (_tpl_CompositeUnrealMatItem != null) _tpl_CompositeUnrealMatItem.SetActive(false);
            if (_tpl_EquipmentItem != null) _tpl_EquipmentItem.SetActive(false);
            if (_tpl_UIVerTabBar != null) _tpl_UIVerTabBar.SetActive(false);
            BindBtn(_sub_btn_img0, "九霄冥饰合成");
            BindBtn(_sub_btn_img1, "九霄冥饰合成");
            BindBtn(_sub_btn_img2, "九霄冥饰合成");
            BindBtn(_sub_btn_img3, "九霄冥饰合成");
            BindBtn(_sub_btn_img4, "九霄冥饰合成");
            BindBtn(_sub_btn_img5, "九霄冥饰合成");
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("Composite", "九霄冥饰合成打开 → 待对接 CompositeModel(默认降级)");
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
