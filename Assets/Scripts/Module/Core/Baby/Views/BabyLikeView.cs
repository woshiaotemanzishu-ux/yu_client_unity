using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Baby;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Baby
{
    [UIView("prefabs/ui/baby/babylikeview")]
    public sealed class BabyLikeView : BabyLikeViewBind
    {
        private bool _listening;
        private ScrollRect _scroller;
        private Transform _content;
        private TextMeshProUGUI _myRank;
        private TextMeshProUGUI _myLike;
        private TextMeshProUGUI _noOneLb;
        private GameObject _itemTemplate;
        private readonly List<GameObject> _items = new List<GameObject>();
        private Transform _rewardContent;
        private GameObject _rewardTemplate;
        private readonly List<GameObject> _rewardItems = new List<GameObject>();
        private Image _closeBtn;
        private Image _belikeBtn;

        protected override void OnInit()
        {
            CacheNodes();
            UIUtil.AddClick(_closeBtn, () => ViewManager.Close<BabyLikeView>());
            UIUtil.AddClick(_belikeBtn, () => _ = ViewManager.Open<BabyBelikeView>());
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            BabyController.Instance.RequestLikeRank();
            Refresh();
            if (!BabyPraiseConfigs.IsLoaded) _ = EnsureRewardsAndRefreshAsync();
        }

        protected override void OnHide()
        {
            Unsubscribe();
            ClearItems();
            ClearRewardItems();
        }

        protected override void OnDispose()
        {
            Unsubscribe();
            ClearItems();
            ClearRewardItems();
        }

        private void Subscribe()
        {
            if (_listening) return;
            _listening = true;
            EventDispatcher.On<int>(GlobalEvent.EVT_BABY_UPDATE, OnBabyUpdate);
        }

        private void Unsubscribe()
        {
            if (!_listening) return;
            _listening = false;
            EventDispatcher.Off<int>(GlobalEvent.EVT_BABY_UPDATE, OnBabyUpdate);
        }

        private void OnBabyUpdate(int command)
        {
            if (command == Proto.BABY_LIKE_RANK && gameObject.activeInHierarchy) Refresh();
        }

        private void Refresh()
        {
            CacheNodes();
            ClearItems();
            BabyPraiseRankInfo rank = BabyModel.Instance.PraiseRank;
            int count = rank != null ? rank.Entries.Count : 0;
            if (_noOneLb != null) _noOneLb.gameObject.SetActive(count == 0);
            int selfRank = 0;
            int selfPraise = 0;
            for (int i = 0; i < count; i++)
            {
                BabyPraiseRankEntry entry = rank.Entries[i];
                int displayRank = i + 1;
                if (entry != null && entry.RoleId == rank.RoleId) { selfRank = displayRank; selfPraise = entry.PraiseNum; }
                if (_itemTemplate == null || _content == null) continue;
                GameObject itemObject = Instantiate(_itemTemplate, _content);
                itemObject.SetActive(true);
                BabyLikeItem item = itemObject.GetComponent<BabyLikeItem>();
                if (item == null) { DestroyItem(itemObject); continue; }
                item.SetData(entry, displayRank);
                _items.Add(itemObject);
            }
            if (_myRank != null) _myRank.text = "我的排名:" + (selfRank > 0 ? selfRank.ToString() : "未上榜");
            if (_myLike != null) _myLike.text = "我的赞:" + selfPraise;
            RefreshRewards();
        }

        private void CacheNodes()
        {
            if (_content != null) return;
            _scroller = _Scroller1;
            if (_scroller != null) _content = _scroller.content;
            Transform[] nodes = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < nodes.Length; i++)
            {
                if (_content == null && nodes[i].name == "Content1") _content = nodes[i];
                if (_rewardContent == null && nodes[i].name == "Content" && nodes[i].parent != null && nodes[i].parent.name == "rewardScroller") _rewardContent = nodes[i];
                if (nodes[i].name == "myRank") _myRank = nodes[i].GetComponent<TextMeshProUGUI>();
                else if (nodes[i].name == "mylike") _myLike = nodes[i].GetComponent<TextMeshProUGUI>();
                else if (nodes[i].name == "noOneLb") _noOneLb = nodes[i].GetComponent<TextMeshProUGUI>();
                else if (nodes[i].name == "closeBtn") _closeBtn = nodes[i].GetComponent<Image>();
                else if (nodes[i].name == "belikeBtn") _belikeBtn = nodes[i].GetComponent<Image>();
                else if (nodes[i].name == "BabyLikeItem" && nodes[i].parent != null && nodes[i].parent.name == "__Templates") _itemTemplate = nodes[i].gameObject;
                else if (nodes[i].name == "BabyLikeReward" && nodes[i].parent != null && nodes[i].parent.name == "__Templates") _rewardTemplate = nodes[i].gameObject;
            }
        }

        private void ClearItems()
        {
            for (int i = 0; i < _items.Count; i++) DestroyItem(_items[i]);
            _items.Clear();
        }

        private void RefreshRewards()
        {
            ClearRewardItems();
            if (!BabyPraiseConfigs.IsLoaded || _rewardContent == null || _rewardTemplate == null) return;
            for (int i = 0; i < BabyPraiseConfigs.All.Count; i++)
            {
                GameObject go = Instantiate(_rewardTemplate, _rewardContent);
                go.SetActive(true);
                BabyLikeReward item = go.GetComponent<BabyLikeReward>();
                if (item == null) { DestroyItem(go); continue; }
                item.SetData(BabyPraiseConfigs.All[i]);
                _rewardItems.Add(go);
            }
        }

        private async Task EnsureRewardsAndRefreshAsync()
        {
            await BabyPraiseConfigs.EnsureLoaded();
            if (IsShown) RefreshRewards();
        }

        private void ClearRewardItems() { for (int i = 0; i < _rewardItems.Count; i++) DestroyItem(_rewardItems[i]); _rewardItems.Clear(); }

        private static void DestroyItem(GameObject item)
        {
            if (item == null) return;
            if (Application.isPlaying) Destroy(item);
            else DestroyImmediate(item);
        }
    }
}
