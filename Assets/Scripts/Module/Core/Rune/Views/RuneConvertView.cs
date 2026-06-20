using Shenxiao.Generated.UI.Rune;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Rune
{
    /// <summary>
    /// 九霄劫魄·劫魄宝库弹窗(对标老客户端 rune/RuneConvertView.ts):从九霄劫魄主面板按钮打开的二级子窗。
    /// 老端:LoadSuccess 读 config_rune_exchange + 设标题「劫魄宝库」/tips「劫魄寻幽可获得大量劫魄碎片！」;
    /// OpenCallback 铺兑换列表(scroll_group 克隆 RuneConvertItem)+ 成本道具(icon_conta 挂 BaseAwardItem)+ num 显碎片数;
    /// getBtn(兑换)→ 校验功能开放后 Fire RuneTreasureMainView 并关闭本窗;closeBtn → Close。
    ///
    /// 降级:RuneModel(rune_info/config_rune_exchange)、RuneConvertItem/BaseAwardItem 列表项、LoopScrowViewMgr 均未移植 →
    /// scroll_group 列表空、_tpl_BaseAwardItem/_tpl_RuneConvertItem/_tpl_RuneTreasureMainView 模板隐藏;
    /// getBtn(兑换)仅日志降级(老端会打开 RuneTreasureBaseView);closeBtn → Hide(老端 Close,关闭返回主面板)。
    /// </summary>
    public sealed class RuneConvertView : RuneConvertViewBind
    {
        protected override void OnInit()
        {
            // 隐藏模板节点(GameObject 用 SetActive,严禁 HideNode)。
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
            if (_tpl_RuneConvertItem != null) _tpl_RuneConvertItem.SetActive(false);
            if (_tpl_RuneTreasureMainView != null) _tpl_RuneTreasureMainView.SetActive(false);

            // 关闭按钮 → Hide。
            BindClose(closeBtn);
            // 兑换按钮 → 日志降级(待对接 RuneTreasureModel 打开宝库)。
            BindBtn(getBtn, "兑换");
        }

        protected override void OnShow(object args)
        {
            // 老端 open → OpenCallback:ApplyViewLabels + SetBagItemList(config_rune_exchange 铺列表)+ num 显碎片数。
            // RuneModel/RuneConvertItem 未移植 → 列表空、默认降级。
            GameLog.Info("Rune", "RuneConvertView 打开 → 待对接 RuneModel(列表空/默认降级)");
        }

        /// <summary>关闭按钮 → Hide(关闭返回九霄劫魄主面板)。</summary>
        private void BindClose(Component target)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, Hide);
        }

        /// <summary>动作按钮 → 日志降级(待对接 RuneModel/RuneTreasureModel 协议)。</summary>
        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("Rune", "点击[{0}] → 待对接", label));
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
