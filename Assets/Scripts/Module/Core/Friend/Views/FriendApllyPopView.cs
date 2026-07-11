using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Generated.UI.Friend;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using UnityEngine;

namespace Shenxiao.Module.Core.Friend
{
    /// <summary>
    /// 好友申请弹窗(对标老客户端 friend/FriendApllyPopView.ts):从好友主面板「好友申请」按钮打开的二级弹窗。
    /// 申请列表(FriendApllyPopItem)+ 一键通过(passBtn,14004 response=1)/一键拒绝(rejectBtn,response=0)+
    /// 空态(nullGroup)+ 关闭(btnClose 关闭返回好友面板)。列表非空校验对标老端"当前无好友申请!"。
    /// </summary>
    public sealed class FriendApllyPopView : FriendApllyPopViewBind
    {
        private readonly List<FriendApllyPopItem> _pool = new List<FriendApllyPopItem>();
        private bool _subscribed;

        protected override void OnInit()
        {
            if (_tpl_FriendApllyPopItem != null) _tpl_FriendApllyPopItem.SetActive(false);

            UIUtil.AddClick(btnClose, Hide);
            UIUtil.AddClick(passBtn, () => OnClickBatch(1));
            UIUtil.AddClick(rejectBtn, () => OnClickBatch(0));
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
            EventDispatcher.On(GlobalEvent.EVT_FRIEND_APPLY_UPDATE, RefreshList);
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            EventDispatcher.Off(GlobalEvent.EVT_FRIEND_APPLY_UPDATE, RefreshList);
        }

        private void RefreshList()
        {
            IReadOnlyList<FriendModel.ApplyVo> list = FriendModel.Instance.ApplyList;
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
            if (_tpl_FriendApllyPopItem == null || Content == null) return;
            while (_pool.Count < count)
            {
                GameObject go = Object.Instantiate(_tpl_FriendApllyPopItem, Content);
                go.SetActive(true);
                _pool.Add(go.GetComponent<FriendApllyPopItem>());
            }
        }

        private void OnClickBatch(int responseType)
        {
            if (FriendModel.Instance.ApplyList.Count <= 0)
            {
                TipsManager.Toast("当前无好友申请!");
                return;
            }
            FriendController.Instance.OneClickApply(responseType);
        }
    }
}
