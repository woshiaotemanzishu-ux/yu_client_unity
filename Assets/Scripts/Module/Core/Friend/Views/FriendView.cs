using System.Collections.Generic;
using Shenxiao.Generated.UI.Friend;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Chat;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Friend
{
    /// <summary>
    /// 好友主界面(对标老客户端 friend/FriendView.ts):好友列表(itemScroller/Content 克隆 FriendListItem)+ 数量(numlabel)+
    /// 空态(nullGroup,无好友时显示)+ 加好友(btnAdd→FriendAddPopView)/好友申请(btnAplly→FriendApllyPopView)/
    /// 黑名单(btnBlacklist→FriendBlackListPopView)+ 申请红点(redDot)。
    ///
    /// 数据源 FriendModel.GetFriendData(TYPE_FRIEND)(14000 type=1,GAME_START 已自动拉取一次);
    /// 列表非虚拟化(好友上限50,规模小,直接整表 Instantiate/复用池即可,对标 BagItemRenderer 池化但无需虚拟滚动)。
    /// </summary>
    public sealed class FriendView : FriendViewBind
    {
        private readonly List<FriendListItem> _pool = new List<FriendListItem>();
        private bool _subscribed;

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
            Subscribe();
            RefreshList();
        }

        protected override void OnHide() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            EventDispatcher.On<int>(GlobalEvent.EVT_FRIEND_DATA_UPDATE, OnFriendDataUpdate);
            EventDispatcher.On<FriendModel.FriendVo>(GlobalEvent.EVT_FRIEND_ONLINE_UPDATE, OnFriendVoTouched);
            EventDispatcher.On<long, int>(GlobalEvent.EVT_FRIEND_INTIMACY_UPDATE, OnIntimacyUpdate);
            EventDispatcher.On(GlobalEvent.EVT_FRIEND_REDDOT_UPDATE, RefreshRedDot);
            EventDispatcher.On<long>(GlobalEvent.EVT_CHAT_PRIVATE_UPDATE, OnPrivateChatUpdate);
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            EventDispatcher.Off<int>(GlobalEvent.EVT_FRIEND_DATA_UPDATE, OnFriendDataUpdate);
            EventDispatcher.Off<FriendModel.FriendVo>(GlobalEvent.EVT_FRIEND_ONLINE_UPDATE, OnFriendVoTouched);
            EventDispatcher.Off<long, int>(GlobalEvent.EVT_FRIEND_INTIMACY_UPDATE, OnIntimacyUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_FRIEND_REDDOT_UPDATE, RefreshRedDot);
            EventDispatcher.Off<long>(GlobalEvent.EVT_CHAT_PRIVATE_UPDATE, OnPrivateChatUpdate);
        }

        private void OnFriendDataUpdate(int type)
        {
            if (type == FriendModel.TYPE_FRIEND) RefreshList();
        }

        private void OnFriendVoTouched(FriendModel.FriendVo vo) => RefreshList();
        private void OnIntimacyUpdate(long roleId, int intimacy) => RefreshList();
        private void OnPrivateChatUpdate(long targetId) => RefreshChatRedDots();

        private void RefreshList()
        {
            IReadOnlyList<FriendModel.FriendVo> list = FriendModel.Instance.GetFriendData(FriendModel.TYPE_FRIEND);
            EnsurePool(list.Count);
            for (int i = 0; i < _pool.Count; i++)
            {
                bool active = i < list.Count;
                _pool[i].gameObject.SetActive(active);
                if (active) _pool[i].SetData(list[i]);
            }
            if (nullGroup != null) nullGroup.gameObject.SetActive(list.Count == 0);
            if (numlabel != null) numlabel.text = FriendModel.Instance.GetOnlineDataNum(FriendModel.TYPE_FRIEND) + "/" + list.Count;
            RefreshRedDot();
        }

        private void RefreshChatRedDots()
        {
            foreach (FriendListItem item in _pool)
            {
                if (item.gameObject.activeSelf) item.RefreshChatRedDot();
            }
        }

        private void RefreshRedDot()
        {
            if (redDot != null) redDot.gameObject.SetActive(FriendModel.Instance.HaveNewApply);
        }

        private void EnsurePool(int count)
        {
            if (_tpl_FriendListItem == null || Content == null) return;
            while (_pool.Count < count)
            {
                GameObject go = Object.Instantiate(_tpl_FriendListItem, Content);
                go.SetActive(true);
                _pool.Add(go.GetComponent<FriendListItem>());
            }
        }

        /// <summary>按钮 → 打开好友模块内子窗(FriendFlow.OpenSub 按 View 子类名查找并叠在主面板上)。</summary>
        private void BindOpen(Component target, string viewType, string label)
        {
            if (target == null) return;
            UIUtil.AddClick(target, () =>
            {
                GameLog.Info("Friend", "点击[{0}] → 打开 {1}", label, viewType);
                FriendFlow.OpenSub(viewType);
            });
        }
    }
}
