using Shenxiao.Common.Tips;
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
    /// 精炼协议(15250/15251,经 EquipSmeltController,自动循环 轮4 队列#4)已接线；单件/一键精炼都必须先由
    /// 真实装备格建立选中态，当前列表未落地时明确阻断；EquipSmeltItem 选中态由
    /// <see cref="SelectEquipType"/> 记录,供后续列表铺格接入。
    /// 降级:EquipModel/GoodsModel/RedDotManager(UPDATE_*_SMELT 回包展示、config_equip_refine_* 配置)、子项渲染
    /// (EquipSmeltItem/EquipAttrItem/BaseAwardItem/FightingShowSmallItem)与淬炼特效均未移植 →
    /// 红点/模板先隐藏;装备列表/属性/消耗为空、战力默认;静态标题文案照搬老端。
    /// 事件驱动窗口,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class EquipSmeltView : EquipSmeltViewBind
    {
        /// <summary>当前选中部位(对标老端 SELECT_SMELT_EQUIP)。装备列表未铺格时保持 0，
        /// <see cref="EquipSmeltItem"/> 铺格后经 SelectEquipType 建立真实选择。</summary>
        private int _selectedEquipType;

        protected override void OnInit()
        {
            HideReds();
            HideTemplates();
            SetStaticLabels();
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            _selectedEquipType = 0;
            // 老端 LoadSuccess/InitView:铺穿戴装备格 + 默认选中 + 刷消耗/属性/战力/红点。数据未移植 → 列表空、属性默认降级。
            GameLog.Info("Equip", "神兵淬炼界面打开 → 待对接 EquipModel/协议(列表空/属性默认降级)");
        }

        /// <summary>更新当前选中部位(由 EquipSmeltItem 点击回调驱动,对标 SELECT_SMELT_EQUIP)。</summary>
        public void SelectEquipType(int equipType)
        {
            if (equipType == 0) return;
            _selectedEquipType = equipType;
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
            BindClick(btnStrOne, () =>
            {
                if (_selectedEquipType <= 0)
                {
                    TipsManager.Toast("请先选择已穿戴装备");
                    GameLog.Warn("Equip", "点击[精炼]被阻止：装备列表尚未建立真实选中态");
                    return;
                }
                GameLog.Info("Equip", "点击[精炼] → SmeltOne(equip_type={0})", _selectedEquipType);
                EquipSmeltController.Instance.SmeltOne(_selectedEquipType);
            });
            BindClick(btnStrAll, () =>
            {
                if (_selectedEquipType <= 0)
                {
                    TipsManager.Toast("请先选择已穿戴装备");
                    GameLog.Warn("Equip", "点击[一键精炼]被阻止：装备列表尚未建立真实选中态");
                    return;
                }
                GameLog.Info("Equip", "点击[一键精炼] → SmeltAll()");
                EquipSmeltController.Instance.SmeltAll();
            });
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击回调。</summary>
        private void BindClick(Component target, System.Action onClick)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, onClick);
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
