using Shenxiao.Generated.UI.Friend;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Friend
{
    /// <summary>
    /// 加好友二级弹窗(对标老客户端 friend/FriendAddPopView.ts):从好友主面板「加好友」按钮打开,叠在主面板之上。
    /// 输入框(Placeholder/Text)输入玩家名称 → 搜索(searchBtn)按名查人、切换(changeBtn)换一批推荐、
    /// 搜索结果列表(itemScroller/Content 克隆 _tpl_FriendAddPopItem)、空态(nullGroup,无结果时显示)、
    /// 关闭(btnClose 返回好友面板)、清空搜索(btnCloseSearch 还原推荐)。
    ///
    /// 降级:FriendModel(推荐/搜索数据、REQUEST_RECOMMEND_DATA/CHECK_PLAYER 协议)、FriendAddPopItem 列表项均未移植 →
    /// 列表空(显 nullGroup、模板 _tpl_FriendAddPopItem 隐藏);searchBtn/changeBtn/btnCloseSearch 仅日志降级;
    /// btnClose 关闭返回好友面板(自带二级弹窗,经 BaseView.Hide 收起)。输入框保持可见、不动。
    /// </summary>
    public sealed class FriendAddPopView : FriendAddPopViewBind
    {
        protected override void OnInit()
        {
            // 列表模板项隐藏(GameObject 用 SetActive,不能 HideNode)。
            if (_tpl_FriendAddPopItem != null) _tpl_FriendAddPopItem.SetActive(false);

            // 关闭按钮 → 收起本弹窗,返回好友主面板。
            BindClose(btnClose);

            // 其余动作按钮 → 待对接 FriendModel,先日志降级。
            BindBtn(searchBtn, "搜索");
            BindBtn(changeBtn, "换一批");
            BindBtn(btnCloseSearch, "清空搜索");
        }

        protected override void OnShow(object args)
        {
            // 老端 LoadSuccess/initData → 请求推荐列表并铺 itemScroller;FriendModel/协议未移植 → 列表空、显空态。
            if (nullGroup != null) nullGroup.gameObject.SetActive(true);
            GameLog.Info("Friend", "FriendAddPopView 打开 → 待对接 FriendModel(列表空/默认降级)");
        }

        /// <summary>关闭按钮:取自身或子级 Image,接 Hide(收起弹窗返回好友面板)。</summary>
        private void BindClose(Component target)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, Hide);
        }

        /// <summary>动作按钮:取自身或子级 Image,接日志降级(待对接 FriendModel)。</summary>
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
