using Shenxiao.Generated.UI.EquipRefinement;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 神屠九炼页(对标老客户端 equipRefinement/EquipRefinementView.ts,EquipView 标签4 内容):当前装备(_img_equip)+ 属性
    /// (_gp_attr/_gp_extra_attr)+ 当前/下一级(_gp_now_lv/_gp_next_lv,加成 _bit_add)+ 消耗(_gp_cost/_gp_cost_goods)+ 精炼按钮(_btn_refi)+
    /// 提示(_gp_notice/lb_notice)+ 说明(insBtn)+ 红点(_img_red)/满级(_img_max)。
    ///
    /// 降级:EquipRefinementModel/精炼协议、属性/消耗/材料列表项均未移植 → 红点/满级/模板隐藏、列表空;_btn_refi 精炼、insBtn 说明打日志降级。
    /// 无独立关闭按钮(装备窗框统一关闭)→ 作 EquipView 标签4 内容由 EquipFlow reparent 进窗框内容区。事件驱动,不进 FirstPass。
    /// </summary>
    public sealed class EquipRefinementView : EquipRefinementViewBind
    {
        protected override void OnInit()
        {
            HideNode(_img_red);
            HideNode(_img_max);
            if (_tpl_EquipWashItem != null) _tpl_EquipWashItem.SetActive(false);
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
            if (_tpl_EquipRefiAttrsItem != null) _tpl_EquipRefiAttrsItem.SetActive(false);
            BindBtn(_btn_refi, "神屠九炼·精炼");
            BindBtn(insBtn, "神屠九炼·说明");
        }

        protected override void OnShow(object args)
        {
            // 老端 open → 读 EquipRefinementModel 铺属性/消耗/材料。数据未移植 → 默认降级。
            GameLog.Info("Equip", "神屠九炼页打开 → 待对接 EquipRefinementModel(默认降级)");
        }

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
