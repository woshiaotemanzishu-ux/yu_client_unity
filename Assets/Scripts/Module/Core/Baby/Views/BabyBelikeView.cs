using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Baby;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Baby
{
    [UIView("prefabs/ui/baby/babybelikeview")]
    public sealed class BabyBelikeView : BabyBelikeViewBind
    {
        private bool _listening;
        private Transform _content;
        private TextMeshProUGUI _noOneLb;
        private GameObject _itemTemplate;
        private readonly List<GameObject> _items = new List<GameObject>();
        private Image _closeBtn;

        protected override void OnInit()
        {
            CacheNodes();
            UIUtil.AddClick(_closeBtn, () => ViewManager.Close<BabyBelikeView>());
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            BabyController.Instance.RequestLikeRecords();
            Refresh();
        }

        protected override void OnHide() { Unsubscribe(); ClearItems(); }
        protected override void OnDispose() { Unsubscribe(); ClearItems(); }

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
            if (command == Proto.BABY_LIKE_RECORDS && gameObject.activeInHierarchy) Refresh();
        }

        private void Refresh()
        {
            CacheNodes();
            ClearItems();
            BabyPraiseRecordsInfo records = BabyModel.Instance.PraiseRecords;
            int count = records != null ? records.Entries.Count : 0;
            if (_noOneLb != null) _noOneLb.gameObject.SetActive(count == 0);
            if (_content == null || _itemTemplate == null || records == null) return;
            for (int i = 0; i < records.Entries.Count; i++)
            {
                GameObject itemObject = Instantiate(_itemTemplate, _content);
                itemObject.SetActive(true);
                BabyBelikeItem item = itemObject.GetComponent<BabyBelikeItem>();
                if (item == null) { DestroyItem(itemObject); continue; }
                item.SetData(records.Entries[i]);
                _items.Add(itemObject);
            }
        }

        private void CacheNodes()
        {
            if (_content != null) return;
            if (_Scroller1 != null) _content = _Scroller1.content;
            Transform[] nodes = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < nodes.Length; i++)
            {
                if (_content == null && nodes[i].name == "Content") _content = nodes[i];
                if (nodes[i].name == "noOneLb") _noOneLb = nodes[i].GetComponent<TextMeshProUGUI>();
                else if (nodes[i].name == "closeBtn") _closeBtn = nodes[i].GetComponent<Image>();
                else if (nodes[i].name == "BabyBelikeItem" && nodes[i].parent != null && nodes[i].parent.name == "__Templates") _itemTemplate = nodes[i].gameObject;
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
            if (Application.isPlaying) Destroy(item); else DestroyImmediate(item);
        }
    }
}
