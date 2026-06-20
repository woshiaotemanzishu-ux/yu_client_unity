using Shenxiao.Generated.UI.Revelation;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary> 启示圣铠页(对标老端 RevelationEquipView.ts,BagView 标签内容)。降级:Model/协议未移植 → 红点/模板隐藏、列表空;动作按钮日志;无关闭(背包窗框统一关闭,shared-prefab 独立视图,根即视图)。 </summary>
    public sealed class RevelationEquipView : RevelationEquipViewBind
    {
        protected override void OnInit()
        {
            HideNode(comRed);
            HideNode(devourRed);
            if (_tpl_RevelationBagItem != null) _tpl_RevelationBagItem.SetActive(false);
            if (_tpl_FightingShowSmallItem != null) _tpl_FightingShowSmallItem.SetActive(false);
            if (_tpl_DemonMainView != null) _tpl_DemonMainView.SetActive(false);
            if (_tpl_longlanguageView != null) _tpl_longlanguageView.SetActive(false);
            if (_tpl_RevelationEquipItem != null) _tpl_RevelationEquipItem.SetActive(false);
            BindBtn(suitBtn, "启示圣铠·套装");
            BindBtn(getBtn, "启示圣铠·获取");
            BindBtn(comBtn, "启示圣铠·合成");
            BindBtn(soulBtn, "启示圣铠·魂");
            BindBtn(propBtn, "启示圣铠·属性");
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("Bag", "启示圣铠打开 → 待对接 Model(默认降级)");
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
