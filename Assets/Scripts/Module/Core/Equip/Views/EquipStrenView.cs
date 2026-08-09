using Shenxiao.Common.Tips;
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
    /// 强化协议(15204/15205,经 EquipStrenController)已接线；单件/一键强化都必须先由真实装备格建立选中态，
    /// 当前列表未落地时明确阻断，禁止固定操作武器槽；btnStopAll(连续强化)仍打日志占位。
    /// 降级:EquipModel/GoodsModel/RoleManager/TaskModel、config_equip_attr/stren_lv/strengthen_max 等配置、
    /// 淬炉宗师子窗、装备/属性/战力小项(_tpl_EquipmentItem/_tpl_FightingShowSmallItem/_tpl_EquipAttrItem)
    /// 与飞入特效均未移植 → 红点/模板先隐藏;列表空、属性/消耗/阶数走默认。
    /// 事件驱动窗口,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class EquipStrenView : EquipStrenViewBind
    {
        private int _selectedEquipType;

        protected override void OnInit()
        {
            HideReds();
            HideTemplates();
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            _selectedEquipType = 0;
            // 老端 LoadSuccess/InitView:铺装备格 + 选默认装备 + 刷属性/消耗/战力(EquipModel.GetWearEquipDynamics…)。数据未移植 → 空/默认。
            GameLog.Info("Equip", "EquipStrenView 打开 → 待对接 EquipModel/协议(列表空/属性默认降级)");
        }

        /// <summary>仅接受真实装备列表点击产生的部位；列表未落地时保持 0，禁止默认操作武器槽。</summary>
        public void SelectEquipType(int equipType)
        {
            _selectedEquipType = equipType > 0 ? equipType : 0;
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
            BindStren(btnStrOne, "强化");
            BindStrenAll(btnStrAll, "一键强化");
            BindBtn(btnStopAll, "停止一键强化");   // 连续强化未移植,留日志占位
            // 淬炉宗师总览子窗(已移植)→ 真打开(EquipFlow.OpenSub 叠主面板上,EquipStrenMasterView.btn_close 返回)。
            BindOpen(btnMaster, "EquipStrenMasterView", "淬炉宗师");
        }

        /// <summary>btnStrOne → EquipStrenController.StrenOne(当前槽位)。未从真实装备格建立选中态时拒绝发送。</summary>
        private void BindStren(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () =>
            {
                if (_selectedEquipType <= 0)
                {
                    TipsManager.Toast("请先选择已穿戴装备");
                    GameLog.Warn("Equip", "点击[{0}] 被阻止：装备列表尚未建立真实选中态", label);
                    return;
                }
                GameLog.Info("Equip", "点击[{0}] → StrenOne(equip_type={1})", label, _selectedEquipType);
                EquipStrenController.Instance.StrenOne(_selectedEquipType);
            });
        }

        /// <summary>btnStrAll → EquipStrenController.StrenAll()(equip_type=0,type=2,一键强化全身)。</summary>
        private void BindStrenAll(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () =>
            {
                if (_selectedEquipType <= 0)
                {
                    TipsManager.Toast("请先选择已穿戴装备");
                    GameLog.Warn("Equip", "点击[{0}]被阻止：装备列表尚未建立真实选中态", label);
                    return;
                }
                GameLog.Info("Equip", "点击[{0}] → StrenAll()", label);
                EquipStrenController.Instance.StrenAll();
            });
        }

        /// <summary>按钮 → 打开装备模块内子窗(EquipFlow.OpenSub 按 View 子类名查找并叠在主面板上)。</summary>
        private void BindOpen(Component target, string viewType, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () =>
            {
                GameLog.Info("Equip", "点击[{0}] → 打开 {1}", label, viewType);
                EquipFlow.OpenSub(viewType);
            });
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
