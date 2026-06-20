using Shenxiao.Generated.UI.Rune;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Rune
{
    /// <summary>
    /// 符文分解·拆解页(对标老客户端 rune/RuneResolutionView.ts,RuneDecMainView 拆解子页「破印焚魄」):
    /// 符文列表(_list_item)+ 收益面板(reward_panel/_gp_item)+ 拆解按钮(_btn)+ 空态(none_conta/_gp_null/tips)。
    ///
    /// 降级:RuneModel/拆解协议、列表项未移植 → 列表空(空态常显)、收益默认;拆解按钮打日志降级。
    /// 由 RuneDecMainView reparent 进其 sub_group(拆解标签内容)。事件驱动,不进 FirstPass。
    /// </summary>
    public sealed class RuneResolutionView : RuneResolutionViewBind
    {
        protected override void OnInit()
        {
            BindBtn(_btn, "符文拆解·拆解");
        }

        protected override void OnShow(object args)
        {
            // 老端 open → 读 RuneModel 铺可拆解符文 + 收益。数据未移植 → 列表空降级(空态常显)。
            GameLog.Info("Rune", "符文拆解页打开 → 待对接 RuneModel(列表空降级)");
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
    }
}
