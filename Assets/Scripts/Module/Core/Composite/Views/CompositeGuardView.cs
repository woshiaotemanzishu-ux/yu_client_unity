using Shenxiao.Generated.UI.Composite;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Composite
{
    /// <summary> 灵宠炼合页(对标老端 composite/CompositeGuardView.ts,CompositeView 标签内容)。降级:CompositeModel/协议未移植 → 红点/模板隐藏、列表空;动作按钮日志;无关闭(合成窗框统一关闭)。 </summary>
    public sealed class CompositeGuardView : CompositeGuardViewBind
    {
        protected override void OnInit()
        {
            HideNode(_img_composite_red);
            if (_tpl_CompositeGuardAttItem != null) _tpl_CompositeGuardAttItem.SetActive(false);
            if (_tpl_EquipmentItem != null) _tpl_EquipmentItem.SetActive(false);
            if (_tpl_CompositeGoodsMatItem != null) _tpl_CompositeGoodsMatItem.SetActive(false);
            if (_tpl_UIVerTabBar != null) _tpl_UIVerTabBar.SetActive(false);
            if (_tpl_UIVerTabSubBtn2 != null) _tpl_UIVerTabSubBtn2.SetActive(false);
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("Composite", "灵宠炼合打开 → 待对接 CompositeModel(默认降级)");
        }

        private static void HideNode(Component c) { if (c != null) c.gameObject.SetActive(false); }
    }
}
