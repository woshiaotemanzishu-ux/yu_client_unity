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

        private RuneDecomposeView _decView;
        private RuneResolutionView _resView;
        private bool _built;

        protected override void OnShow(object args)
        {
            // 老端 LoadSuccess → initTab:建双页签(分解「万魄炼散」/拆解「破印焚魄」)+ SwtichView 铺子页。
            // Unity 已写两页 → 克隆 _tpl_RuneDecomposeTab 建 2 页签,reparent 两页进 sub_group,默认显分解。RuneModel 数据未移植 → 列表空降级。
            BuildTabsOnce();
            SelectPage(0);
            GameLog.Info("Rune", "RuneDecMainView 打开 → 分解/拆解 双页签(RuneModel 列表空降级)");
        }

        /// <summary>懒建一次:把同模块的分解/拆解页 reparent 进 sub_group(先隐),并克隆 2 个页签按钮入 Content。</summary>
        private void BuildTabsOnce()
        {
            if (_built) return;
            _built = true;
            Transform root = transform.root;
            _decView = root.GetComponentInChildren<RuneDecomposeView>(true);
            _resView = root.GetComponentInChildren<RuneResolutionView>(true);
            if (sub_group != null)
            {
                if (_decView != null) { _decView.transform.SetParent(sub_group, false); _decView.gameObject.SetActive(false); }
                if (_resView != null) { _resView.transform.SetParent(sub_group, false); _resView.gameObject.SetActive(false); }
            }
            BuildTab(0);
            BuildTab(1);
        }

        private void BuildTab(int idx)
        {
            if (_tpl_RuneDecomposeTab == null || Content == null) return;
            RectTransform parent = Content.content != null ? Content.content : (RectTransform)Content.transform;
            GameObject go = Instantiate(_tpl_RuneDecomposeTab, parent);
            go.SetActive(true);
            Image img = go.GetComponentInChildren<Image>(true);
            if (img != null) { img.raycastTarget = true; UIUtil.AddClick(img, () => SelectPage(idx)); }
        }

        /// <summary>切页:0=分解 RuneDecomposeView / 1=拆解 RuneResolutionView(显选中、隐另一)。</summary>
        private void SelectPage(int idx)
        {
            if (_decView != null) { if (idx == 0) _decView.Show(); else _decView.Hide(); }
            if (_resView != null) { if (idx == 1) _resView.Show(); else _resView.Hide(); }
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
