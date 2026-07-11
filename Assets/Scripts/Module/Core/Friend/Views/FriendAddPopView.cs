using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Generated.UI.Friend;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;

namespace Shenxiao.Module.Core.Friend
{
    /// <summary>
    /// 加好友二级弹窗(对标老客户端 friend/FriendAddPopView.ts):从好友主面板「加好友」按钮打开,叠在主面板之上。
    /// 输入框输入玩家名称 → 搜索(searchBtn,14002)、切换(changeBtn,14001 换一批)、结果列表(FriendAddPopItem,
    /// 推荐为空且非搜索态时用假人填充避免空面板)、空态(nullGroup,仅搜索态下结果为空才显示)、
    /// 关闭(btnClose 返回好友面板)、清空搜索(btnCloseSearch 还原推荐)。
    /// </summary>
    public sealed class FriendAddPopView : FriendAddPopViewBind
    {
        private const string PlaceholderText = "输入玩家名称";
        private readonly List<FriendAddPopItem> _pool = new List<FriendAddPopItem>();
        private bool _subscribed;

        protected override void OnInit()
        {
            if (_tpl_FriendAddPopItem != null) _tpl_FriendAddPopItem.SetActive(false);
            if (btnCloseSearch != null) btnCloseSearch.gameObject.SetActive(false);

            UIUtil.AddClick(btnClose, Hide);
            UIUtil.AddClick(searchBtn, OnClickSearch);
            UIUtil.AddClick(changeBtn, OnClickChange);
            UIUtil.AddClick(btnCloseSearch, OnClickCloseSearch);
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            FriendController.Instance.RequestRecommend(0);
            RefreshList();
        }

        protected override void OnHide() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            EventDispatcher.On(GlobalEvent.EVT_FRIEND_RECOMMEND_UPDATE, RefreshList);
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            EventDispatcher.Off(GlobalEvent.EVT_FRIEND_RECOMMEND_UPDATE, RefreshList);
        }

        private void RefreshList()
        {
            List<FriendModel.RecommendVo> list = FriendModel.Instance.GetRecommendList();
            EnsurePool(list.Count);
            for (int i = 0; i < _pool.Count; i++)
            {
                bool active = i < list.Count;
                _pool[i].gameObject.SetActive(active);
                if (active) _pool[i].SetData(list[i]);
            }
            // 对标老端:list.length<=0 && _search 才显空态(假人填充保证非搜索态永不空)。
            if (nullGroup != null) nullGroup.gameObject.SetActive(list.Count == 0 && FriendModel.Instance.CheckPlayerMode);
        }

        private void EnsurePool(int count)
        {
            if (_tpl_FriendAddPopItem == null || Content == null) return;
            while (_pool.Count < count)
            {
                GameObject go = Object.Instantiate(_tpl_FriendAddPopItem, Content);
                go.SetActive(true);
                _pool.Add(go.GetComponent<FriendAddPopItem>());
            }
        }

        private void OnClickSearch()
        {
            string name = Placeholder != null ? Placeholder.text : "";
            if (string.IsNullOrEmpty(name))
            {
                TipsManager.Toast(PlaceholderText);
                return;
            }
            if (btnCloseSearch != null) btnCloseSearch.gameObject.SetActive(true);
            FriendController.Instance.SearchPlayer(name);
        }

        private void OnClickChange() => FriendController.Instance.RequestRecommend(1);

        private void OnClickCloseSearch()
        {
            if (Placeholder != null) Placeholder.text = "";
            if (btnCloseSearch != null) btnCloseSearch.gameObject.SetActive(false);
            FriendController.Instance.RequestRecommend(0);
        }
    }
}
