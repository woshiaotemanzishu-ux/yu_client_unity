using Shenxiao.Generated.UI.Friend;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Friend
{
    /// <summary>
    /// 黑名单弹窗(对标老客户端 friend/FriendBlackListPopView.ts):从好友主面板「黑名单」按钮打开的二级弹窗。
    /// 黑名单列表(itemScroller/Content 克隆 _tpl_FriendBlackListItm)+ 空态(nullGroup,无数据时显示)+
    /// 关闭(btnClose → Close)。自带 btnClose 关闭返回好友面板。
    ///
    /// 降级:FriendModel(REQUEST_FRIEND_DATA / FRIEND_DATA_UPDATE / getFriendData(BLACKLIST))、
    /// FriendBlackListItm 列表项、LoopScrowViewMgr 均未移植 → 列表空(显 nullGroup)、_tpl_FriendBlackListItm 模板隐藏。
    /// btnClose 经 Hide 关闭返回好友面板。事件驱动窗口,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class FriendBlackListPopView : FriendBlackListPopViewBind
    {
        protected override void OnInit()
        {
            // 模板节点隐藏(GameObject 用 SetActive,绝不 HideNode)。
            if (_tpl_FriendBlackListItm != null) _tpl_FriendBlackListItm.SetActive(false);

            // 关闭按钮 → Hide(关闭返回好友面板)。
            BindClose(btnClose);
        }

        protected override void OnShow(object args)
        {
            // 老端 open_callback → REQUEST_FRIEND_DATA(BLACKLIST),FRIEND_DATA_UPDATE 回来 initData 铺列表 + nullGroup.visible=空。
            // FriendModel/协议未移植 → 列表空、显空态。
            if (nullGroup != null) nullGroup.gameObject.SetActive(true);
            GameLog.Info("Friend", "FriendBlackListPopView 打开 → 待对接 FriendModel(列表空/默认降级)");
        }

        private void BindClose(Component target)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, Hide);
        }
    }
}
