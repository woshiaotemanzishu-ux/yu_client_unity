using Shenxiao.Generated.UI.Equip;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 装备洗魄面板(对标老客户端 equipWash/EquipWashView.ts):左侧穿戴装备列表(Content/_Scroller1 铺 EquipWashItem)+
    /// 当前洗魄装备格(cur_wash_group)+ 洗魄属性条(wash_prop_group/EquipWashPropItem)+ 升段条件(_gp_up_cond:评分 _lb_cond_socre/阶数 _lb_cond_order)+
    /// 洗魄石/勾玉消耗(wash_stone_group/gp_purple)+ 额外保底道具(_gp_extra)+ 战力(_bit_figth)+ 洗魄/升段按钮(btn_wash/lb_wash)+ 强者礼包入口(giftIcon)。
    ///
    /// 降级:EquipModel/GoodsModel/RoleManager/config_equip_wash 等数据与协议(15213 洗魄、15252 升段)均未移植 →
    /// 红点(_img_red)/各模板(_tpl_*)先隐藏;btn_wash 点击打日志「待对接」;列表空、属性默认降级。事件驱动窗口,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class EquipWashView : EquipWashViewBind
    {
        protected override void OnInit()
        {
            HideReds();
            HideTemplates();
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            // 老端 LoadSuccess/InitView 铺装备列表 + SetSelectDefault 默认选中并 Fire SELECT_WASH_EQUIP。数据未移植 → 空。
            GameLog.Info("Equip", "EquipWashView 打开 → 待对接 EquipModel/协议(列表空/属性默认降级)");
        }

        private void HideReds()
        {
            HideNode(_img_red);
        }

        private void HideTemplates()
        {
            if (_tpl_EquipWashPropItem != null) _tpl_EquipWashPropItem.SetActive(false);
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
            if (_tpl_EquipmentItem != null) _tpl_EquipmentItem.SetActive(false);
            if (_tpl_GiftPushIcon != null) _tpl_GiftPushIcon.SetActive(false);
        }

        private void BindButtons()
        {
            BindBtn(btn_wash, "洗魄/升段 btn_wash");
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击 → 打日志(降级:子窗/逻辑待对接)。</summary>
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
