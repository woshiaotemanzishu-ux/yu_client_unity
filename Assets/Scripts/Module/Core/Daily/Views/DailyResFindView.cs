using Shenxiao.Generated.UI.Daily;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Daily
{
    /// <summary>
    /// 每日·资源找回页(对标老客户端 daily/DailyResFindView.ts,DailyView 标签3 内容):
    /// 资源找回列表(scroll/Content 克隆 DailyResFindItem)+ 空提示(none_conta/tips)+ 两个切换/筛选按钮(checkBtn0/checkBtn1)+ 领取(receive_gp)。
    ///
    /// 降级:DailyModel/资源找回协议、DailyResFindItem 列表项均未移植 → 列表空、模板隐藏、空提示常显;checkBtn0/checkBtn1/receive_gp 打日志降级;
    /// 本页无独立关闭按钮(老端靠 DailyView 窗框关闭)→ 二级 HUD 每日寻宝按钮再点关闭(DailyFlow.Toggle)。事件驱动,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class DailyResFindView : DailyResFindViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_DailyResFindItem != null) _tpl_DailyResFindItem.SetActive(false);
            BindBtn(checkBtn0, "资源找回·筛选0");
            BindBtn(checkBtn1, "资源找回·筛选1");
            BindBtn(receive_gp, "一键领取找回资源");
        }

        protected override void OnShow(object args)
        {
            // 老端 open → 读 DailyModel 铺资源找回行。数据未移植 → 列表空降级。
            GameLog.Info("Daily", "资源找回页打开 → 待对接 DailyModel(列表空降级)");
        }

        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("Daily", "点击[{0}] → 待对接", label));
        }
    }
}
