using Shenxiao.Generated.UI.Friend;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Friend
{
    /// <summary>
    /// 好友主界面(对标老客户端 friend/FriendView.ts):好友列表(itemScroller/Content 克隆 FriendListItem)+ 数量(numlabel)+
    /// 空态(nullGroup,无好友时显示)+ 加好友(btnAdd→FriendAddPopView)/好友申请(btnAplly→FriendApllyPopView)/
    /// 黑名单(btnBlacklist→FriendBlackListPopView)+ 申请红点(redDot)。
    ///
    /// 降级:FriendModel(好友/申请/黑名单数据、协议)、FriendListItem 列表项、LoopScrowViewMgr 均未移植 →
    /// 列表空(显 nullGroup、数量 0)、红点隐藏、_tpl_FriendListItem 模板隐藏;三按钮经 FriendFlow.OpenSub 打开对应子窗
    /// (子窗未写时日志降级)。无独立关闭按钮 → 由 HUD 好友按钮再点关闭(FriendFlow.Toggle)。事件驱动窗口,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class FriendView : FriendViewBind
    {
        protected override void OnInit()
        {
            if (redDot != null) redDot.gameObject.SetActive(false);
            if (_tpl_FriendListItem != null) _tpl_FriendListItem.SetActive(false);

            BindOpen(btnAdd, "FriendAddPopView", "加好友");
            BindOpen(btnAplly, "FriendApllyPopView", "好友申请");
            BindOpen(btnBlacklist, "FriendBlackListPopView", "黑名单");
        }

        protected override void OnShow(object args)
        {
            // 老端 LoadSuccess → 请求好友列表 + UpdateView 铺列表/数量/空态。FriendModel/协议未移植 → 列表空、显空态。
            if (nullGroup != null) nullGroup.gameObject.SetActive(true);
            if (numlabel != null) numlabel.text = "0";
            GameLog.Info("Friend", "好友界面打开 → 待对接 FriendModel(列表空/默认降级)");
        }

        /// <summary>按钮 → 打开好友模块内子窗(FriendFlow.OpenSub 按 View 子类名查找并叠在主面板上)。</summary>
        private void BindOpen(Component target, string viewType, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () =>
            {
                GameLog.Info("Friend", "点击[{0}] → 打开 {1}", label, viewType);
                FriendFlow.OpenSub(viewType);
            });
        }
    }
}
