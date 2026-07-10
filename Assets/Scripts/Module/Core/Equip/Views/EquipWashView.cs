using System.Collections.Generic;
using Shenxiao.Generated.UI.Equip;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Bag;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 装备洗魄面板(对标老客户端 equipWash/EquipWashView.ts):左侧穿戴装备列表(Content/_Scroller1 铺 EquipWashItem)+
    /// 当前洗魄装备格(cur_wash_group)+ 洗魄属性条(wash_prop_group/EquipWashPropItem)+ 升段条件(_gp_up_cond:评分 _lb_cond_socre/阶数 _lb_cond_order)+
    /// 洗魄石/勾玉消耗(wash_stone_group/gp_purple)+ 额外保底道具(_gp_extra)+ 战力(_bit_figth)+ 洗魄/升段按钮(btn_wash/lb_wash)+ 强者礼包入口(giftIcon)。
    ///
    /// 洗魄协议(15212/15213/15214/15252,经 EquipWashController,自动循环 轮4 队列#4)已接线:btn_wash 按老端
    /// WashBtnCallBack 同一按钮兼二态——槽位已满(GoodsDetailVo.WashAttrs.Count 达 4,服务端 pt_152 guard 槽位范围
    /// 1-4)→ 升段(15252,is_buy 固定 0);未满 → 洗魄(15213,锁定位读 EquipWashModel,ratio_plus 固定 0/普通模式)。
    /// TODO:无选中态(装备列表未铺格),暂借 EquipAutoWear.GetWorn 固定武器槽当"当前装备",同 EquipStrenView 先例;
    /// 橙色以上属性二次确认(ConfirmDialog)/紫红橙保底模式选择/材料不够二次确认均依赖 config_equip_wash* 表
    /// (EquipConfigs 缺表,见其类注释)与背包材料计数联动,本轮不臆造,直接发送交服务端兜底(见 OnShow/BindWash 内 log)。
    ///
    /// 降级:EquipModel/GoodsModel/RoleManager 等数据、config_equip_wash 完整表均未移植 →
    /// 红点(_img_red)/各模板(_tpl_*)先隐藏;列表空、属性默认降级。事件驱动窗口,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class EquipWashView : EquipWashViewBind
    {
        /// <summary>服务端 15212 guard:洗魄槽位范围 1-4(pt_152 侦察报告);槽位数达此值视为"已满"走升段分支。</summary>
        private const int MaxWashSlots = 4;

        /// <summary>TODO:无当前槽位数据源,暂固定武器槽(equip_type=1),同 EquipStrenView.BindStren 既有先例。</summary>
        private const int CurrentEquipType = 1;

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
            _ = EquipConfigs.EnsureLoaded();
            // 预热当前(占位)装备的详情缓存,供 btn_wash 点击时判断槽位是否已满(见 BindButtons)。
            BagGoods worn = EquipAutoWear.GetWorn(CurrentEquipType);
            if (worn != null) GoodsDynamicModel.Instance.RequestDetail(worn.GoodsId);
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
            BindWash(btn_wash, "洗魄/升段");
        }

        /// <summary>btn_wash → 已穿戴武器槽满 4 槽走升段(15252),否则洗魄(15213;锁定位读 EquipWashModel,
        /// 保底模式固定 0/普通,同 EquipStrenView 既有"无选中态先直发"简化处理)。未穿武器槽 → 跳过并日志。</summary>
        private void BindWash(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () =>
            {
                BagGoods worn = EquipAutoWear.GetWorn(CurrentEquipType);
                if (worn == null)
                {
                    GameLog.Info("Equip", "点击[{0}] → 武器槽未穿戴装备,跳过", label);
                    return;
                }
                GoodsDetailVo vo = GoodsDynamicModel.Instance.Peek(worn.GoodsId);
                bool slotsFull = vo != null && vo.WashAttrs != null && vo.WashAttrs.Count >= MaxWashSlots;
                if (slotsFull)
                {
                    GameLog.Info("Equip", "点击[{0}] → 槽位已满,UpgradeDivision(equip_type={1},isBuy=0)", label, CurrentEquipType);
                    EquipWashController.Instance.UpgradeDivision(CurrentEquipType, 0);
                }
                else
                {
                    List<int> locked = EquipWashModel.Instance.GetLockedIndices(CurrentEquipType);
                    GameLog.Info("Equip", "点击[{0}] → WashExecute(equip_type={1},lockCount={2},ratioPlus=0)",
                        label, CurrentEquipType, locked.Count);
                    EquipWashController.Instance.WashExecute(CurrentEquipType, locked, 0);
                }
            });
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
