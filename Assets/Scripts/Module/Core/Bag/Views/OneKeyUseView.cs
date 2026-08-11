using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Generated.UI.Bag;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Role;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>一键使用：按 config_goods.use_one_key 过滤，三个分类页签独立全选/取消，物品可逐个勾选。</summary>
    public sealed class OneKeyUseView : OneKeyUseViewBind
    {
        public override UILayer Layer => UILayer.Popup;

        private const int Columns = 6;
        private const float CellStep = 92f;
        private const float ItemScale = 0.72f;

        private sealed class Category
        {
            public string Name;
            public int Type;
            public bool Selected = true;
            public OneKeyUseTab Tab;
        }

        private readonly List<Category> _categories = new List<Category>
        {
            new Category { Name = "礼包", Type = 32 },
            new Category { Name = "经验书", Type = 37 },
            new Category { Name = "其他", Type = -1 },
        };
        private readonly List<BagGoods> _eligible = new List<BagGoods>();
        private readonly HashSet<long> _selected = new HashSet<long>();
        private readonly List<BaseAwardItem> _itemPool = new List<BaseAwardItem>();
        private bool _subscribed;
        private bool _hasShownOnce;
        private int _refreshEpoch;

        protected override void OnInit()
        {
            if (_tpl_OneKeyUseTab != null) _tpl_OneKeyUseTab.SetActive(false);
            BindBtn(closeBtn, Hide);
            BindBtn(useBtn, UseSelected);
            BuildTabs();
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            bool firstShow = !_hasShownOnce;
            _hasShownOnce = true;
            _ = RefreshAsync(firstShow, firstShow);
        }

        protected override void OnHide()
        {
            _refreshEpoch++;
            Unsubscribe();
            StopListScroll();
            BagFlow.NotifyActivitySubHidden(this);
        }

        protected override void OnDispose()
        {
            _refreshEpoch++;
            Unsubscribe();
            foreach (BaseAwardItem item in _itemPool)
                if (item != null) ResManager.ReleaseInstance(item.gameObject);
            _itemPool.Clear();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On(GlobalEvent.EVT_BAG_UPDATE, OnBagUpdate);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off(GlobalEvent.EVT_BAG_UPDATE, OnBagUpdate);
            _subscribed = false;
        }

        private void OnBagUpdate() => _ = RefreshAsync(true, false);

        private void BuildTabs()
        {
            if (_tpl_OneKeyUseTab == null || Content1 == null) return;
            for (int i = 0; i < _categories.Count; i++)
            {
                Category category = _categories[i];
                GameObject go = Instantiate(_tpl_OneKeyUseTab, Content1);
                go.name = "OneKeyUseTab_" + category.Name;
                go.SetActive(true);
                OneKeyUseTab tab = go.GetComponent<OneKeyUseTab>();
                if (tab == null) continue;
                tab.Show();
                tab.SetData(category.Name);
                tab.SetSelect(true);
                category.Tab = tab;
                Category captured = category;
                BindBtn(go.transform as RectTransform, () => ToggleCategory(captured));
            }
        }

        private async Task RefreshAsync(bool selectAllItems, bool resetCategories)
        {
            int epoch = ++_refreshEpoch;
            await GoodsModel.EnsureLoaded();
            if (epoch != _refreshEpoch || !IsShown) return;

            _eligible.Clear();
            int roleLevel = RoleModel.Instance.Level;
            foreach (BagGoods goods in BagModel.Instance.BagGoodsList)
            {
                GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(goods.TypeId);
                if (basic != null && basic.UseOneKey != 0 && roleLevel >= basic.Level)
                    _eligible.Add(goods);
            }

            if (resetCategories)
            {
                foreach (Category category in _categories)
                {
                    category.Selected = true;
                    category.Tab?.SetSelect(true);
                }
            }

            if (selectAllItems)
            {
                _selected.Clear();
                foreach (BagGoods goods in _eligible) _selected.Add(goods.GoodsId);
            }
            else
            {
                _selected.RemoveWhere(id => _eligible.FindIndex(g => g.GoodsId == id) < 0);
            }

            await EnsureItemPoolAsync(_eligible.Count, epoch);
            if (epoch != _refreshEpoch || !IsShown) return;
            RenderItems();
        }

        private async Task EnsureItemPoolAsync(int count, int epoch)
        {
            if (_Scroller2 == null || _Scroller2.content == null) return;
            string key = GameResPath.GetUIPrefab("common", "BaseAwardItem");
            while (_itemPool.Count < count)
            {
                GameObject go = await ResManager.InstantiateAsync(key, _Scroller2.content);
                if (epoch != _refreshEpoch)
                {
                    if (go != null) ResManager.ReleaseInstance(go);
                    return;
                }
                if (go == null) return;
                BaseAwardItem item = go.GetComponent<BaseAwardItem>();
                if (item == null)
                {
                    ResManager.ReleaseInstance(go);
                    return;
                }
                go.name = "OneKeyUseItem_" + _itemPool.Count;
                item.SetScale(ItemScale);
                _itemPool.Add(item);
            }
        }

        private void RenderItems()
        {
            if (_Scroller2 == null || _Scroller2.content == null) return;
            int rows = Mathf.CeilToInt(_eligible.Count / (float)Columns);
            _Scroller2.content.sizeDelta = new Vector2(_Scroller2.content.sizeDelta.x, rows * CellStep);
            for (int i = 0; i < _itemPool.Count; i++)
            {
                BaseAwardItem item = _itemPool[i];
                bool active = i < _eligible.Count;
                item.gameObject.SetActive(active);
                if (!active) continue;
                BagGoods goods = _eligible[i];
                var rt = (RectTransform)item.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(i % Columns * CellStep, -(i / Columns) * CellStep);
                BagGoods captured = goods;
                item.SetClickCallBack(() => ToggleItem(captured));
                item.SetData(goods.TypeId, goods.GoodsNum, goods.Bind != 0, _selected.Contains(goods.GoodsId));
            }
            if (nothingLb != null) nothingLb.gameObject.SetActive(_eligible.Count == 0);
        }

        private void StopListScroll()
        {
            if (_Scroller2 == null) return;
            _Scroller2.StopMovement();
            _Scroller2.velocity = Vector2.zero;
        }

        private void ToggleItem(BagGoods goods)
        {
            if (goods == null) return;
            if (!_selected.Add(goods.GoodsId)) _selected.Remove(goods.GoodsId);
            RenderItems();
        }

        private void ToggleCategory(Category category)
        {
            category.Selected = !category.Selected;
            category.Tab?.SetSelect(category.Selected);
            foreach (BagGoods goods in _eligible)
            {
                GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(goods.TypeId);
                if (!MatchesCategory(basic, category.Type)) continue;
                if (category.Selected) _selected.Add(goods.GoodsId);
                else _selected.Remove(goods.GoodsId);
            }
            RenderItems();
        }

        private static bool MatchesCategory(GoodsModel.GoodsBasic basic, int type)
        {
            if (basic == null) return false;
            if (type == -1) return basic.Type != 32 && basic.Type != 37;
            return basic.Type == type;
        }

        private void UseSelected()
        {
            int sent = 0;
            // 先冻结选择与数量，发包过程中模型更新不会改变本轮操作集合。
            var requests = new List<(long id, long num)>();
            foreach (BagGoods goods in _eligible)
                if (_selected.Contains(goods.GoodsId)) requests.Add((goods.GoodsId, goods.GoodsNum));
            foreach ((long id, long num) request in requests)
            {
                if (request.id <= 0 || request.num <= 0) continue;
                int useCount = request.num > int.MaxValue ? int.MaxValue : (int)request.num;
                BagController.Instance.UseGoods(request.id, useCount);
                sent++;
            }
            if (sent == 0) TipsManager.Toast("当前无可一键使用的道具");
        }

        private static void BindBtn(Component target, Action onClick)
        {
            if (target == null) return;
            GameObject go = target.gameObject;
            foreach (Graphic graphic in go.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            Image image = go.GetComponent<Image>();
            if (image == null)
            {
                image = go.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0f);
            }
            image.raycastTarget = true;
            UIUtil.AddClick(image, onClick);
        }
    }
}
