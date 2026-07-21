using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Generated.UI.Baby;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Baby
{
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

        protected override void OnShow(object args)
        {
            Subscribe();
            BabyController.Instance.RequestLikeRank();
            Refresh();
        }

        protected override void OnHide()
        {
            Unsubscribe();
            ClearItems();
        }

        protected override void OnDispose()
        {
            Unsubscribe();
            ClearItems();
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
                if (nodes[i].name == "myRank") _myRank = nodes[i].GetComponent<TextMeshProUGUI>();
                else if (nodes[i].name == "mylike") _myLike = nodes[i].GetComponent<TextMeshProUGUI>();
                else if (nodes[i].name == "noOneLb") _noOneLb = nodes[i].GetComponent<TextMeshProUGUI>();
                else if (nodes[i].name == "BabyLikeItem" && nodes[i].parent != null && nodes[i].parent.name == "__Templates") _itemTemplate = nodes[i].gameObject;
            }
        }

        private void ClearItems()
        {
            for (int i = 0; i < _items.Count; i++) DestroyItem(_items[i]);
            _items.Clear();
        }

        private static void DestroyItem(GameObject item)
        {
            if (item == null) return;
            if (Application.isPlaying) Destroy(item);
            else DestroyImmediate(item);
        }
    }
}
