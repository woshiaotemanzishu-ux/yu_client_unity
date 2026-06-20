using Shenxiao.Generated.UI.Friend;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Friend
{
    /// <summary>
    /// 好友申请弹窗(对标老客户端 friend/FriendApllyPopView.ts):从好友主面板「好友申请」按钮打开的二级弹窗。
    /// 申请列表(itemScroller/Content 克隆 _tpl_FriendApllyPopItem)+ 一键通过(passBtn)/一键拒绝(rejectBtn)+
    /// 空态(nullGroup,无申请时显示)+ 关闭(btnClose 关闭返回好友面板)。
    ///
    /// 降级:FriendModel(申请数据 GetapplyList、FriendEvent.ONECLICK_APPLY 协议)、FriendApllyPopItem 列表项、
    /// LoopScrowViewMgr 均未移植 → 列表空(显 nullGroup)、_tpl_FriendApllyPopItem 模板隐藏;
    /// passBtn/rejectBtn 仅日志降级(老端会按列表长度发一键通过/拒绝或提示「当前无好友申请」);
    /// btnClose → Hide(老端 Close,关闭返回好友面板)。
    /// </summary>
    public sealed class FriendApllyPopView : FriendApllyPopViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_FriendApllyPopItem != null) _tpl_FriendApllyPopItem.SetActive(false);

            BindClose(btnClose);
            BindBtn(passBtn, "通过");
            BindBtn(rejectBtn, "拒绝");
        }

        protected override void OnShow(object args)
        {
            // 老端 open → initData:GetapplyList 为空显空态、否则铺列表。FriendModel 未移植 → 列表空、显空态。
            if (nullGroup != null) nullGroup.gameObject.SetActive(true);
            GameLog.Info("Friend", "FriendApllyPopView 打开 → 待对接 FriendModel(列表空/默认降级)");
        }

        /// <summary>关闭按钮 → Hide(关闭返回好友面板)。</summary>
        private void BindClose(Component target)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, Hide);
        }

        /// <summary>动作按钮 → 日志降级(待对接 FriendModel 一键通过/拒绝协议)。</summary>
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
