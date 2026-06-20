using Shenxiao.Generated.UI.Friend;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Friend
{
    /// <summary>
    /// 邮件界面(对标老客户端 friend/EmailView.ts):邮件列表(itemScroller/Content 克隆 _tpl_EmailItem)+ 空态(nullGroup)+
    /// 删除已读(btnDelet)+ 一键领取(btnGet)。由二级 HUD 邮件按钮打开(FriendModule 内顶层窗,经 FriendFlow.OpenView)。
    ///
    /// 降级:MailController/MailModel(19001 列表/19007 读/19008 领取/19009 删除,后端已移植但 EmailItem/列表渲染未接)、
    /// EmailItem 列表项、LoopScrowViewMgr 均未接 → 列表空(显 nullGroup)、_tpl_EmailItem 模板隐藏;删除/领取按钮打日志降级。
    /// 无独立关闭按钮 → 由二级 HUD 邮件按钮再点关闭(FriendFlow.ToggleEmail)。事件驱动窗口,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class EmailView : EmailViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_EmailItem != null) _tpl_EmailItem.SetActive(false);
            BindBtn(btnDelet, "删除已读邮件(协议 19009)");
            BindBtn(btnGet, "一键领取(协议 19008)");
        }

        protected override void OnShow(object args)
        {
            // 老端 open → 请求邮件列表(19001)+ UpdateView 铺列表/空态。列表渲染未接 → 列表空、显空态。
            if (nullGroup != null) nullGroup.gameObject.SetActive(true);
            GameLog.Info("Friend", "邮件界面打开 → 待对接 MailModel 列表渲染(列表空降级)");
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击 → 打日志(降级:协议/逻辑待对接)。</summary>
        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("Friend", "点击[{0}] → 待对接", label));
        }
    }
}
