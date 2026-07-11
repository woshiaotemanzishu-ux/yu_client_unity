using System.Collections.Generic;
using Shenxiao.Generated.UI.Friend;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using UnityEngine;

namespace Shenxiao.Module.Core.Friend
{
    /// <summary>
    /// 黑名单弹窗(对标老客户端 friend/FriendBlackListPopView.ts):从好友主面板「黑名单」按钮打开的二级弹窗。
    /// 打开即请求 14000 type=3(对标老端 open_callback → REQUEST_FRIEND_DATA(BLACKLIST));
    /// 黑名单列表(FriendBlackListItm)+ 空态(nullGroup)+ 关闭(btnClose → Hide)。
    /// </summary>
    public sealed class FriendBlackListPopView : FriendBlackListPopViewBind
    {
        private readonly List<FriendBlackListItm> _pool = new List<FriendBlackListItm>();
        private bool _subscribed;

        protected override void OnInit()
        {
            if (_tpl_FriendBlackListItm != null) _tpl_FriendBlackListItm.SetActive(false);
            UIUtil.AddClick(btnClose, Hide);
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            FriendController.Instance.RequestFriendList(FriendModel.TYPE_BLACKLIST);
            RefreshList();
        }

        protected override void OnHide() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            EventDispatcher.On<int>(GlobalEvent.EVT_FRIEND_DATA_UPDATE, OnDataUpdate);
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            EventDispatcher.Off<int>(GlobalEvent.EVT_FRIEND_DATA_UPDATE, OnDataUpdate);
        }

        private void OnDataUpdate(int type)
        {
            if (type == FriendModel.TYPE_BLACKLIST) RefreshList();
        }

        private void RefreshList()
        {
            IReadOnlyList<FriendModel.FriendVo> list = FriendModel.Instance.GetFriendData(FriendModel.TYPE_BLACKLIST);
            EnsurePool(list.Count);
            for (int i = 0; i < _pool.Count; i++)
            {
                bool active = i < list.Count;
                _pool[i].gameObject.SetActive(active);
                if (active) _pool[i].SetData(list[i]);
            }
            if (nullGroup != null) nullGroup.gameObject.SetActive(list.Count == 0);
        }

        private void EnsurePool(int count)
        {
            if (_tpl_FriendBlackListItm == null || Content == null) return;
            while (_pool.Count < count)
            {
                GameObject go = Object.Instantiate(_tpl_FriendBlackListItm, Content);
                go.SetActive(true);
                _pool.Add(go.GetComponent<FriendBlackListItm>());
            }
        }
    }
}
