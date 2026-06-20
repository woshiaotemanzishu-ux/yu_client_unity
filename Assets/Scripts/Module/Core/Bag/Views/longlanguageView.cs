using Shenxiao.Generated.UI.Longlanguage;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary> 九天神祭页(对标老端 longlanguageView.ts,BagView 标签内容)。降级:Model/协议未移植 → 红点/模板隐藏、列表空;动作按钮日志;无关闭(背包窗框统一关闭,shared-prefab 独立视图,根即视图)。 </summary>
    public sealed class longlanguageView : LonglanguageViewBind
    {
        protected override void OnInit()
        {
            HideNode(red2);
            HideNode(red3);
            HideNode(red4);
            if (_tpl_HolySealBagItem != null) _tpl_HolySealBagItem.SetActive(false);
            if (_tpl_FightingShowSmallItem != null) _tpl_FightingShowSmallItem.SetActive(false);
            if (_tpl_DemonMainView != null) _tpl_DemonMainView.SetActive(false);
            if (_tpl_longlangEquipItem != null) _tpl_longlangEquipItem.SetActive(false);
            BindBtn(btn1, "九天神祭·分页1");
            BindBtn(btn2, "九天神祭·分页2");
            BindBtn(btn3, "九天神祭·分页3");
            BindBtn(btn4, "九天神祭·分页4");
            BindBtn(shopBtn, "九天神祭·商店");
            BindBtn(propBtn, "九天神祭·道具");
        }
        protected override void OnShow(object args)
        {
            GameLog.Info("Bag", "九天神祭打开 → 待对接 Model(默认降级)");
        }
        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("Bag", "点击[" + label + "] → 待对接"));
        }
        private static void HideNode(Component c) { if (c != null) c.gameObject.SetActive(false); }
    }
}
