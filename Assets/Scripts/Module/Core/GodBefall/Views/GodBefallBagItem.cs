using Shenxiao.Generated.UI.GodBefall;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.GodBefall
{
    /// <summary>
    /// 神祇降临背包格(对标老客户端 godBefall/GodBefallBagItem.ts):一个装备背包格,
    /// 内含装备图标容器(_gp_item 克隆 EquipmentItem,SetData(type_id, goods_num, bind) + 点击弹 GodEquipTips)、
    /// 隐藏的占位底图(_img_bg)、锁图标(_img_lock)、可升级提示箭头(_img_up)、红点(_reddot)、备用图标(_img_icon)。
    /// 老端 InitEvent 把 _img_bg 隐藏、_gp_item 显示并塞入 EquipmentItem;dataChanged 据 EquipModel/GodBefallModel
    /// 对比已穿戴装备评级决定 _img_up 是否显示。
    ///
    /// 降级:EquipmentItem 子件、EquipModel/GodBefallModel(GetEquipData/GetGodVoInDic/GetGodData/GetRedStatus 等)、
    /// UIToolTipMgr 装备 tips、RedDotManager 均未移植 →
    /// OnInit 隐藏红点 + 可升级箭头 + 锁图标,并按老端把 _img_bg 隐藏、_gp_item 显示;
    /// SetData 仅落最小渲染(无配置 → 图标走 Model 待对接),点击/对比逻辑打日志「待对接」。列表项,由背包面板克隆铺设。
    /// </summary>
    public sealed class GodBefallBagItem : GodBefallBagItemBind
    {
        protected override void OnInit()
        {
            // 红点:对标 _reddot,RedDotManager 数据未移植 → 隐藏。
            HideNode(_reddot);
            // 可升级箭头:对标 _img_up,需 EquipModel/GodBefallModel 评级对比(未移植)→ 默认隐藏。
            HideNode(_img_up);
            // 锁图标:对标 _img_lock,锁定态数据未移植 → 默认隐藏。
            HideNode(_img_lock);

            // 老端 InitEvent:隐藏底图 _img_bg、显示装备容器 _gp_item(内放 EquipmentItem 子件)。
            HideNode(_img_bg);
            if (_gp_item != null) _gp_item.gameObject.SetActive(true);
        }

        /// <summary>
        /// 填背包格数据(对标 dataChanged → EquipmentItem.SetData(type_id, goods_num, bind) + 点击弹 tips + 评级对比刷箭头)。
        /// 降级:EquipmentItem 子件 / EquipModel / GodBefallModel / UIToolTipMgr 均未移植 →
        /// 图标走 Model 待对接、可升级箭头默认隐藏、点击仅打日志。
        /// </summary>
        public void SetData(int typeId, int goodsNum, int bind, int godId)
        {
            // 老端:this._item.SetData(type_id, goods_num, bind) 显装备图标。EquipmentItem 子件未移植 → 图标待对接。
            // 老端:GetEquipData/GetGodVoInDic/GetGodData/GetRedStatus 对比 base_rating 决定 _img_up.visible。数据层未移植 → 箭头隐藏。
            HideNode(_img_up);
            // 点击:老端弹 GodEquipTips(UIToolTipMgr.AppendGodEquipTips)。tips 未移植 → 仅日志占位。
            BindBtn(_gp_item, "装备格点击弹 GodEquipTips");

            GameLog.Info("GodBefall",
                "GodBefallBagItem.SetData(typeId={0}, goodsNum={1}, bind={2}, godId={3}) → 待对接 EquipmentItem 图标 + EquipModel/GodBefallModel 评级对比 + UIToolTipMgr",
                typeId, goodsNum, bind, godId);
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击 → 打日志(降级:逻辑/协议待对接)。</summary>
        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("GodBefall", "点击[{0}] → 待对接", label));
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
