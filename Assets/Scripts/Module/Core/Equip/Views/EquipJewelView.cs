using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Jewel;
using Shenxiao.Module.Core.Bag;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 骸珀镶嵌主页签(对标老客户端 jewel/EquipJewelView.ts):装备位列表(left/right_groupEquip,EquipJewelPosItem)+
    /// 当前选中件的镶嵌槽预览(pos_1..pos_6,EquipJewelItem)+ 战力(_fight_power)+ 一键镶嵌/升级(btnStrAll)+
    /// 淬炉宗师式大师入口(btnMaster→骸珀镶嵌大师,type=3)+ 雕刻入口(_img_crave→星骸凿痕子窗)。
    ///
    /// **装备位列表/镶嵌槽渲染降级说明**:老端两者都是运行时 new 出来克隆同一个 LayaUI 资源(装备位列表借用
    /// "equip/EquipStrenItem" 布局,即 <see cref="Shenxiao.Generated.UI.Equip.EquipStrenItemBind"/>;镶嵌槽是
    /// <see cref="EquipJewelItem"/>)。核查当前烤图(JewelModule.prefab):left/right_groupEquip 与 pos_1..pos_6
    /// 均为**空容器**,全仓库都没有对应的 `_tpl_EquipStrenItem`/`_tpl_EquipJewelItem` 克隆模板可查(同类空缺在
    /// EquipStrenView 自己的 left/right_groupEquip 上也一样未处理,不是本轮新增的坑)——故本页签这两块列表
    /// 保持烤图原样,不臆造节点;<see cref="EquipJewelItem"/> 类已按 <see cref="EquipJewelBagView"/> 联动写好
    /// 真实点击/数据逻辑,待未来补烤模板即可直接铺用。
    ///
    /// btnStrAll 简化为"一键升级"单一模式(老端另有"一键镶嵌"模式,需 config_equip_stone_inlay 自动选材,未移植
    /// 跳过):对标 EquipStrenView.BindStren 先例,无"当前选中装备位"数据源 → 固定读武器槽(equip_type=1,
    /// <see cref="EquipAutoWear.GetWorn"/>)的已镶嵌宝石,取第一个调 15215(upgrade_type=1,一键低级宝石)。
    /// </summary>
    public sealed class EquipJewelView : EquipJewelViewBind
    {
        /// <summary>骸珀镶嵌大师(全身奖励)type(对标老端 EquipDefine.JEWEL_WHOLE_TYPE=3)。</summary>
        private const int JewelWholeType = 3;

        /// <summary>TODO:无当前选中装备位数据源,暂固定武器槽,同 EquipStrenView.BindStren 既有先例。</summary>
        private const int CurrentEquipType = 1;

        protected override void OnInit()
        {
            HideReds();
            HideEffects();
            if (lb_strAll != null) lb_strAll.text = "一键升级";

            BindClick(btnMaster, () =>
            {
                GameLog.Info("Equip", "点击[骸珀镶嵌大师] → OpenSub(EquipJewelMasterView, type={0})", JewelWholeType);
                EquipFlow.OpenSub("EquipJewelMasterView");
            });
            BindClick(_img_crave, () =>
            {
                GameLog.Info("Equip", "点击[雕刻入口] → OpenSub(EquipJewelCraveView)");
                EquipFlow.OpenSub("EquipJewelCraveView");
            });
            BindClick(btnStrAll, OnClickUpgradeAll);
        }

        protected override void OnShow(object args)
        {
            EventDispatcher.On(GlobalEvent.EVT_EQUIP_WHOLE_UPDATE, RefreshGrade);
            GameLog.Info("Equip", "EquipJewelView 打开 → 装备位列表/镶嵌槽渲染待补烤模板(见类注释),战力(15254)本轮不接");
            RefreshGrade();

            // 预热武器槽详情缓存,供一键升级按钮判断 stone_list(对标 EquipWashView 既有先例)。
            BagGoods worn = EquipAutoWear.GetWorn(CurrentEquipType);
            if (worn != null) GoodsDynamicModel.Instance.RequestDetail(worn.GoodsId);
        }

        protected override void OnHide()
        {
            EventDispatcher.Off(GlobalEvent.EVT_EQUIP_WHOLE_UPDATE, RefreshGrade);
        }

        /// <summary>大师阶数展示(对标 SetGradeData):config_equip_whole_reward(lv→"阶"文案)未移植 →
        /// 直显 EquipWholeAwardModel 里的真实 whole_lv 数值,不臆造"阶"文案换算。</summary>
        private void RefreshGrade()
        {
            int lv = EquipWholeAwardModel.Instance.GetWholeLv(JewelWholeType);
            if (grade != null) grade.text = lv > 0 ? ("Lv." + lv) : "";
        }

        private void HideReds()
        {
            // red_dot(一键镶嵌/升级红点)/img_redMaster(大师红点)/crave_red(雕刻红点)均依赖 EquipModel 计算
            // (CheckEquipJewelRedAllChange/CheckMasterRed/RedDotManager JEWEL_CRAVE_RED),未移植 → 隐藏。
            HideNode(red_dot);
            HideNode(img_redMaster);
            HideNode(crave_red);
        }

        private void HideEffects()
        {
            HideNode(_group_eff);
        }

        /// <summary>btnStrAll → 一键升级(简化版,见类注释):固定武器槽,取第一个已镶嵌宝石调 15215(type=1)。</summary>
        private void OnClickUpgradeAll()
        {
            BagGoods worn = EquipAutoWear.GetWorn(CurrentEquipType);
            if (worn == null)
            {
                TipsManager.Toast("暂无可升级宝石");
                return;
            }
            GoodsDetailVo vo = GoodsDynamicModel.Instance.Peek(worn.GoodsId);
            if (vo?.StoneList != null)
            {
                foreach (GoodsStoneSlot slot in vo.StoneList)
                {
                    if (slot.TypeId <= 0) continue;
                    GameLog.Info("Equip", "点击[一键升级] → UpgradeStone(equip_type={0},pos={1},type=1)", CurrentEquipType, slot.Pos);
                    EquipJewelController.Instance.UpgradeStone(CurrentEquipType, slot.Pos, 1);
                    return;
                }
            }
            TipsManager.Toast("暂无可升级宝石");   // 对标老端 EquipJewelView.ts:103/116 字面文案
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }

        private void BindClick(Component target, System.Action onClick)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, onClick);
        }
    }
}
