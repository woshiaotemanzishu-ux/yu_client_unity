using Shenxiao.Generated.UI.Rune;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Rune
{
    /// <summary>
    /// 符文分解·分解页(对标老客户端 rune/RuneDecomposeView.ts,RuneDecMainView 分解子页「万魄炼散」):
    /// 符文背包列表(_list_item)+ 品质筛选(_cb_blue/_cb_purple/_cb_orange/_cb_red)+ 收益预览(_group_gain1..3)+ 领取(_btn_get)/分解(_btn_decompose)+ 红点(_reddot)+ 空态(none_conta/tips)。
    ///
    /// 降级:RuneModel/分解协议、列表项未移植 → 红点隐藏、列表空(空态常显);领取/分解按钮打日志降级。
    /// 由 RuneDecMainView reparent 进其 sub_group 显示(分解窗默认页)。事件驱动,不进 FirstPass。
    /// </summary>
    public sealed class RuneDecomposeView : RuneDecomposeViewBind
    {
        protected override void OnInit()
        {
            HideNode(_reddot);
            BindBtn(_btn_get, "符文分解·领取");
            BindBtn(_btn_decompose, "符文分解·分解");
        }

        protected override void OnShow(object args)
        {
            // 老端 open → 读 RuneModel 铺可分解符文列表 + 收益。数据未移植 → 列表空降级(空态常显)。
            GameLog.Info("Rune", "符文分解页打开 → 待对接 RuneModel(列表空降级)");
        }

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
