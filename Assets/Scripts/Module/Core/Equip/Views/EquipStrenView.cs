using Shenxiao.Generated.UI.Equip;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 装备强化(天殒淬炉)界面(对标老客户端 equip/EquipStrenView.ts):左右两列装备格(left/right_groupEquip)+
    /// 中央展示位(gp_show)+ 战力(_fight_power)+ 属性列表(listAttr,当前/下一级)+ 消耗物(goods_icon/have_num/need_num)+
    /// 单强化(btnStrOne「强化」)/一键强化(btnStrAll「一键强化」)/停止(btnStopAll)/淬炉宗师(btnMaster→EquipStrenMasterView)+
    /// 阶数(grade)+ 满级提示(group_max_tip)+ 强化特效(gp_effect)+ 大师红点(red_master)/强化红点(_reddot)。
    ///
    /// 降级:EquipModel/GoodsModel/RoleManager/TaskModel、config_equip_attr/stren_lv/strengthen_max 等配置、
    /// 强化协议(15205)、淬炉宗师子窗、装备/属性/战力小项(_tpl_EquipmentItem/_tpl_FightingShowSmallItem/_tpl_EquipAttrItem)
    /// 与飞入特效均未移植 → 红点/模板先隐藏;按钮点击打日志「待对接」;列表空、属性/消耗/阶数走默认。
    /// 事件驱动窗口,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class EquipStrenView : EquipStrenViewBind
    {
        protected override void OnInit()
        {
            HideReds();
            HideTemplates();
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            // 老端 LoadSuccess/InitView:铺装备格 + 选默认装备 + 刷属性/消耗/战力(EquipModel.GetWearEquipDynamics…)。数据未移植 → 空/默认。
            GameLog.Info("Equip", "EquipStrenView 打开 → 待对接 EquipModel/协议(列表空/属性默认降级)");
        }

        /// <summary>红点(大师红点 red_master、强化红点 _reddot)依赖 EquipModel.CheckMasterRed/CheckStrenRedDotOne,未移植先隐藏。</summary>
        private void HideReds()
        {
            HideNode(red_master);
            HideNode(_reddot);
        }

        /// <summary>装备/战力/属性小项模板(由各 Item View 克隆),数据未移植先隐藏。</summary>
        private void HideTemplates()
        {
            if (_tpl_EquipmentItem != null) _tpl_EquipmentItem.SetActive(false);
            if (_tpl_FightingShowSmallItem != null) _tpl_FightingShowSmallItem.SetActive(false);
            if (_tpl_EquipAttrItem != null) _tpl_EquipAttrItem.SetActive(false);
        }

        private void BindButtons()
        {
            BindBtn(btnStrOne, "强化");
            BindBtn(btnStrAll, "一键强化");
            BindBtn(btnStopAll, "停止一键强化");
            BindBtn(btnMaster, "淬炉宗师 EquipStrenMasterView");
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击 → 打日志(降级:协议/子窗待对接)。</summary>
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
