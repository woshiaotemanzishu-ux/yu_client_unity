using Shenxiao.Generated.UI.Bag;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 主背包面板(对标老客户端 bag/BagComponentView.ts):角色模型展示 + 物品格子滚动列表(bag_con) +
    /// 一键使用/熔炼/红装/扩展/使用 等按钮 + 守护/龙珠入口 + 各红点。
    ///
    /// 降级:背包数据层(BagModel/GoodsModel 物品列表)、装备/守护/龙珠系统、角色模型展示、各子窗路由均未移植 →
    /// 红点/模板先隐藏;按钮点击打日志「待对接」;物品格子空(待 BagModel 数据 + bagItemRenderer 挂载,后者受
    /// filler 大小写坑影响)。子窗(一键使用 OneKeyUseView/熔炼 BagSmeltView/扩展 ExpandBagView)在 BagModule 内,
    /// 待 BagFlow 编排打开。事件驱动窗口,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class BagComponentView : BagComponentViewBind
    {
        protected override void OnInit()
        {
            HideReds();
            HideTemplates();
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            // 老端 open 刷物品格子(BagModel.GetBagList → LoopScrowViewMgr 铺 bagItemRenderer)。数据未移植 → 空。
            GameLog.Info("Bag", "主背包打开 → 待对接 背包数据(BagModel/GoodsModel)铺物品格子");
        }

        private void HideReds()
        {
            HideNode(suitRed); HideNode(guard1_red); HideNode(guard2_red);
            HideNode(dragonball_red); HideNode(red_quick); HideNode(useRed); HideNode(smeltRed);
        }

        private void HideTemplates()
        {
            if (_tpl_BagEquipmentIcon != null) _tpl_BagEquipmentIcon.SetActive(false);
            if (_tpl_FightingShowSmallItem != null) _tpl_FightingShowSmallItem.SetActive(false);
        }

        private void BindButtons()
        {
            // 已移植子窗(早期 bag 模块已写 View 并挂载)→ 真切换打开(BagFlow.ToggleSub 叠背包面板上,再点关闭)。
            BindToggle(onekeyBtn, "OneKeyUseView", "一键使用");
            BindToggle(smeltBtn, "BagSmeltView", "熔炼");
            BindToggle(expandBtn, "ExpandBagView", "扩展背包");
            // 未移植/纯逻辑 → 暂日志。
            BindBtn(redequipBtn, "红装");
            BindBtn(useBtn, "使用");
            BindBtn(_btn_guard1, "守护1");
            BindBtn(_btn_guard2, "守护2");
            BindBtn(_btn_dragonball, "龙珠");
        }

        /// <summary>按钮 → 切换背包模块内子窗(BagFlow.ToggleSub 按 View 子类名查找,叠在背包面板上,再点关闭)。</summary>
        private void BindToggle(Component target, string viewType, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () =>
            {
                GameLog.Info("Bag", "点击背包按钮[{0}] → 切换 {1}", label, viewType);
                BagFlow.ToggleSub(viewType);
            });
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击 → 打日志(降级:子窗/逻辑待对接)。</summary>
        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("Bag", "点击背包按钮[{0}] → 待对接", label));
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
