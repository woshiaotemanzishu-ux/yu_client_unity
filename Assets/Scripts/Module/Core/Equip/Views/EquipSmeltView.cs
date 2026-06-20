using Shenxiao.Generated.UI.Equip;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 神兵淬炼(精炼)界面(对标老客户端 equip/EquipSmeltView.ts):左右两列穿戴装备格(left/right_groupEquip 内
    /// EquipSmeltItem)+ 选中展示位(gp_show)+ 战力(_fight_power FightingShowSmallItem)+ 属性列表(listAttr
    /// EquipAttrItem)+ 消耗材料(goods_icon/goods_icon_top BaseAwardItem,标题"神兵淬炼消耗")+ 精炼/一键精炼
    /// 按钮(btnStrOne/btnStrAll)+ 红点(_reddot)+ 各特效层(_group_eff*/gp_effect)。
    ///
    /// 降级:EquipModel/GoodsModel/RedDotManager 协议(15251 精炼请求、UPDATE_*_SMELT 回包、config_equip_refine_*
    /// 配置)、子项(EquipSmeltItem/EquipAttrItem/BaseAwardItem/FightingShowSmallItem)与淬炼特效均未移植 →
    /// 红点/模板先隐藏;按钮点击打日志「待对接」;装备列表/属性/消耗为空、战力默认;静态标题文案照搬老端。
    /// 事件驱动窗口,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class EquipSmeltView : EquipSmeltViewBind
    {
        protected override void OnInit()
        {
            HideReds();
            HideTemplates();
            SetStaticLabels();
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            // 老端 LoadSuccess/InitView:铺穿戴装备格 + 默认选中 + 刷消耗/属性/战力/红点。数据未移植 → 列表空、属性默认降级。
            GameLog.Info("Equip", "神兵淬炼界面打开 → 待对接 EquipModel/协议(列表空/属性默认降级)");
        }

        private void HideReds()
        {
            HideNode(_reddot);
        }

        private void HideTemplates()
        {
            if (_tpl_EquipSmeltItem != null) _tpl_EquipSmeltItem.SetActive(false);
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
            if (_tpl_EquipmentItem != null) _tpl_EquipmentItem.SetActive(false);
            if (_tpl_FightingShowSmallItem != null) _tpl_FightingShowSmallItem.SetActive(false);
            if (_tpl_EquipAttrItem != null) _tpl_EquipAttrItem.SetActive(false);
        }

        /// <summary>静态文案(对标老端 LoadSuccess 里固定赋值的标题/按钮文字)。</summary>
        private void SetStaticLabels()
        {
            if (_Label2 != null) _Label2.text = "神兵淬炼消耗";
            if (_lb_strOne != null) _lb_strOne.text = "精炼";
            if (_lb_strAll != null) _lb_strAll.text = "一键精炼";
        }

        private void BindButtons()
        {
            BindBtn(btnStrOne, "精炼");
            BindBtn(btnStrAll, "一键精炼");
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击 → 打日志(降级:精炼协议待对接)。</summary>
        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("Equip", "点击[{0}] → 待对接", label));
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
