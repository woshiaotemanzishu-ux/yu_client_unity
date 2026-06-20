using Shenxiao.Generated.UI.Rune;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Rune
{
    /// <summary>
    /// 符文分解主界面(对标老客户端 rune/RuneDecMainView.ts):从九霄劫魄主面板按钮打开的子窗。
    /// 顶部双页签 Content(分解「万魄炼散」/拆解「破印焚魄」)+ 子页内容容器 sub_group(克隆
    /// _tpl_RuneDecomposeView/_tpl_RuneResolutionView)+ 关闭(_btn_close,老端 Close 关闭返回主面板)。
    ///
    /// 降级:RuneModel(RuneDecSelectTab/DEC_MAIN_VIEW_RED 红点协议)、LoopScrowViewMgr 页签滚动、
    /// RuneDecomposeTab/RuneDecomposeView/RuneDecomposeItem/RuneResolutionView/RuneResolutionItem 均未移植 →
    /// 全部 _tpl_* 模板隐藏、sub_group 空、不建页签不切页;_btn_close → Hide(老端 Close)。
    /// 无红点字段、无空态字段 → 仅默认降级。
    /// </summary>
    public sealed class RuneDecMainView : RuneDecMainViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_RuneDecomposeTab != null) _tpl_RuneDecomposeTab.SetActive(false);
            if (_tpl_RuneDecomposeView != null) _tpl_RuneDecomposeView.SetActive(false);
            if (_tpl_RuneDecomposeItem != null) _tpl_RuneDecomposeItem.SetActive(false);
            if (_tpl_RuneResolutionView != null) _tpl_RuneResolutionView.SetActive(false);
            if (_tpl_RuneResolutionItem != null) _tpl_RuneResolutionItem.SetActive(false);

            BindClose(_btn_close);
        }

        protected override void OnShow(object args)
        {
            // 老端 LoadSuccess → initTab:建页签 + SwtichView 铺子页(分解/拆解)。RuneModel/LoopScrowViewMgr 未移植 →
            // 列表空、sub_group 空、默认降级。
            GameLog.Info("Rune", "RuneDecMainView 打开 → 待对接 RuneModel(列表空/默认降级)");
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

        /// <summary>动作按钮 → 日志降级(待对接 RuneModel 页签/分解协议)。</summary>
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
