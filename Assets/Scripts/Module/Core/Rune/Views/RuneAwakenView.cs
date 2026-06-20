using Shenxiao.Generated.UI.Rune;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Rune
{
    /// <summary>
    /// 符文觉醒(劫魄觉醒)子窗(对标老客户端 rune/RuneAwakenView.ts):从九霄劫魄主面板按钮打开的二级窗。
    /// 左侧万魄录列表(_item_list 克隆 RuneAwakenItem)+ 属性列表(_attr_list/RuneAwakenAttrItem)+ 可吞噬劫魄背包
    /// (bagGp/RuneAwakeBagItem)+ 战力展示(_fight_con/FightingShowSmallItem)+ 装备展示(_gp_show/EquipmentItem)+
    /// 觉醒(_btn,吞噬劫魄升觉醒等级)+ 全选(selectGp)+ 说明(tipsImg)+ 关闭(_btn_close → Close)。
    ///
    /// 降级:RuneModel(GetRunesAwakenInBag、awakeSelect、RuneEvent.RUNE_AWAKEN/RUNE_AWAKEN_RETURN/SELECT_AWAKE_MAT 协议)、
    /// GoodsModel、LoopScrowViewMgr、EquipmentItem/FightingShowSmallItem/RuneAwakenItem/RuneAwakeBagItem 列表项均未移植
    /// → 列表/背包空、模板(_tpl_EquipmentItem/_tpl_FightingShowSmallItem)隐藏、红点(_red)隐藏、nothingLb 保持默认;
    /// _btn(觉醒)仅日志降级(老端会校验觉醒满级/未选吞噬劫魄并发 RUNE_AWAKEN 协议);
    /// _btn_close → Hide(老端 Close,关闭返回主面板)。
    /// </summary>
    public sealed class RuneAwakenView : RuneAwakenViewBind
    {
        protected override void OnInit()
        {
            // 红点降级隐藏(老端 _red.visible 由 updateCost 按可觉醒态控制,数据未移植 → 常隐)
            HideNode(_red);

            // 列表项模板隐藏(运行期克隆用,降级阶段不显示;GameObject 用 SetActive,严禁 HideNode)
            if (_tpl_EquipmentItem != null) _tpl_EquipmentItem.SetActive(false);
            if (_tpl_FightingShowSmallItem != null) _tpl_FightingShowSmallItem.SetActive(false);

            // 关闭按钮 → Hide(老端 _btn_close → Close)
            BindClose(_btn_close);

            // 觉醒按钮 → 日志降级(待对接 RuneModel RUNE_AWAKEN 吞噬劫魄协议)
            BindBtn(_btn, "觉醒");
        }

        protected override void OnShow(object args)
        {
            // 老端 LoadSuccess/initItem → GetRunesAwakenInBag 铺万魄录列表并刷新觉醒态。RuneModel 未移植 → 列表空、保持默认空态。
            GameLog.Info("Rune", "RuneAwakenView 打开 → 待对接 RuneModel(列表空/默认降级)");
        }

        /// <summary>关闭按钮 → Hide(关闭返回主面板)。</summary>
        private void BindClose(Component target)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, Hide);
        }

        /// <summary>动作按钮 → 日志降级(待对接 RuneModel 觉醒协议)。</summary>
        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("Rune", "点击[{0}] → 待对接", label));
        }

        /// <summary>红点/节点降级隐藏。</summary>
        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
