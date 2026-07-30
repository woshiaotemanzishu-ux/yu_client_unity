using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Generated.UI.Bag;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 仓库双栏：上栏背包(pos=4)、下栏仓库(pos=5)。单击延时打开详情，双击在 300ms 内走 15003 存入/取出；
    /// 15010/15017/15018 均从 BagModel 的两个独立容器刷新。按钮语义逐项对标老端 WarehouseView.ts。
    /// </summary>
    public sealed class WarehouseView : WarehouseViewBind
    {
        private const int Columns = 6;
        private const float CellStep = 94f;
        private const float ItemScale = 0.72f;
        private const int MinBagCells = 24;
        private const int MinWarehouseCells = 18;
        private const double DoubleClickSeconds = 0.32d;

        private readonly List<BaseAwardItem> _bagCells = new List<BaseAwardItem>();
        private readonly List<BaseAwardItem> _warehouseCells = new List<BaseAwardItem>();
        private bool _subscribed;
        private int _clickEpoch;
        private long _lastGoodsId;
        private int _lastPos;
        private double _lastClickTime;

        protected override void OnInit()
        {
            if (suitRed != null) suitRed.gameObject.SetActive(false);
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);

            BindAction(expandBtn1, () => BagFlow.OpenSub("ExpandBagView", BagModel.POS_WAREHOUSE));
            BindAction(expandBtn2, () => BagFlow.OpenSub("ExpandBagView", BagModel.POS_BAG));
            BindAction(smeltBtn, () => BagFlow.OpenSub("BagSmeltView"));
            BindAction(useBtn, () => BagFlow.OpenSub("OneKeyUseView"));
            DisableClickSurface(redequipBtn);
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            RefreshAll();
            if (!BagModel.Instance.HasWarehouseData)
                BagController.Instance.RequestContainer(BagModel.POS_WAREHOUSE);
        }

        protected override void OnHide()
        {
            _clickEpoch++;
            Unsubscribe();
        }

        protected override void OnDispose()
        {
            _clickEpoch++;
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On(GlobalEvent.EVT_BAG_UPDATE, RefreshAll);
            EventDispatcher.On(GlobalEvent.EVT_WAREHOUSE_UPDATE, RefreshAll);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off(GlobalEvent.EVT_BAG_UPDATE, RefreshAll);
            EventDispatcher.Off(GlobalEvent.EVT_WAREHOUSE_UPDATE, RefreshAll);
            _subscribed = false;
        }

        private void RefreshAll()
        {
            // 老端 WarehouseView.ts 明确映射：_Scroller1=ware_scroll（上半仓库），
            // _Scroller2=goods_scroll（下半背包）。不可按字段编号猜顺序。
            RefreshContainer(_Scroller2, BagModel.Instance.BagGoodsList, BagModel.Instance.MaxCell,
                MinBagCells, BagModel.POS_BAG, _bagCells);
            RefreshContainer(_Scroller1, BagModel.Instance.WarehouseGoodsList,
                BagModel.Instance.GetMaxCell(BagModel.POS_WAREHOUSE), MinWarehouseCells,
                BagModel.POS_WAREHOUSE, _warehouseCells);
        }

        private void RefreshContainer(ScrollRect scroll, IReadOnlyList<BagGoods> goods, int maxCell,
            int minCells, int pos, List<BaseAwardItem> pool)
        {
            if (scroll == null || scroll.content == null || _tpl_BaseAwardItem == null) return;
            int slotCount = Mathf.Max(minCells, Mathf.Max(maxCell, goods?.Count ?? 0));
            if (goods != null)
            {
                for (int i = 0; i < goods.Count; i++)
                    if (goods[i] != null) slotCount = Mathf.Max(slotCount, goods[i].Cell);
            }

            EnsurePool(scroll.content, pool, slotCount, pos);
            BagGoods[] slots = BuildSlots(goods, slotCount);
            int rows = Mathf.CeilToInt(slotCount / (float)Columns);
            scroll.content.sizeDelta = new Vector2(scroll.content.sizeDelta.x, rows * CellStep);

            for (int i = 0; i < pool.Count; i++)
            {
                BaseAwardItem cell = pool[i];
                bool active = i < slotCount;
                cell.gameObject.SetActive(active);
                if (!active) continue;

                BagGoods vo = slots[i];
                if (vo == null)
                {
                    cell.SetClickCallBack(() => { });
                    cell.SetData(0, 0);
                    continue;
                }

                BagGoods captured = vo;
                cell.SetClickCallBack(() => OnGoodsClick(captured, pos));
                cell.SetData(vo.TypeId, vo.GoodsNum, vo.Bind != 0);
            }
        }

        private void EnsurePool(RectTransform content, List<BaseAwardItem> pool, int count, int pos)
        {
            while (pool.Count < count)
            {
                int index = pool.Count;
                GameObject go = Instantiate(_tpl_BaseAwardItem, content);
                go.name = (pos == BagModel.POS_WAREHOUSE ? "WarehouseCell_" : "WarehouseBagCell_") + index;
                go.SetActive(true);
                BaseAwardItem cell = go.GetComponent<BaseAwardItem>();
                if (cell == null)
                {
                    GameLog.Error("Bag", "Warehouse BaseAwardItem 模板缺组件: {0}", go.name);
                    Destroy(go);
                    return;
                }
                var rt = (RectTransform)go.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(index % Columns * CellStep, -(index / Columns) * CellStep);
                cell.SetScale(ItemScale);
                pool.Add(cell);
            }
        }

        private static BagGoods[] BuildSlots(IReadOnlyList<BagGoods> goods, int slotCount)
        {
            var slots = new BagGoods[slotCount];
            if (goods == null) return slots;
            var overflow = new List<BagGoods>();
            for (int i = 0; i < goods.Count; i++)
            {
                BagGoods vo = goods[i];
                int index = vo != null ? vo.Cell - 1 : -1;
                if (index >= 0 && index < slots.Length && slots[index] == null) slots[index] = vo;
                else if (vo != null) overflow.Add(vo);
            }
            int cursor = 0;
            for (int i = 0; i < overflow.Count; i++)
            {
                while (cursor < slots.Length && slots[cursor] != null) cursor++;
                if (cursor >= slots.Length) break;
                slots[cursor] = overflow[i];
            }
            return slots;
        }

        private void OnGoodsClick(BagGoods goods, int pos)
        {
            if (goods == null || goods.GoodsId <= 0) return;
            double now = Time.realtimeSinceStartupAsDouble;
            bool isDouble = goods.GoodsId == _lastGoodsId && pos == _lastPos && now - _lastClickTime <= DoubleClickSeconds;
            _lastGoodsId = goods.GoodsId;
            _lastPos = pos;
            _lastClickTime = now;
            int epoch = ++_clickEpoch;

            if (isDouble)
            {
                _lastGoodsId = 0;
                int to = pos == BagModel.POS_WAREHOUSE ? BagModel.POS_BAG : BagModel.POS_WAREHOUSE;
                BagController.Instance.MoveGoods(goods.GoodsId, pos, to);
                return;
            }
            _ = OpenTipsAfterDoubleClickWindow(goods, pos, epoch);
        }

        private async Task OpenTipsAfterDoubleClickWindow(BagGoods goods, int pos, int epoch)
        {
            await TimeUtil.Delay(330);
            if (epoch != _clickEpoch || !IsShown) return;
            ItemTipsView.ShowWarehouse(goods, pos == BagModel.POS_WAREHOUSE);
        }

        private static void BindAction(Component target, Action action)
        {
            Image image = PrepareClickSurface(target);
            if (image != null) UIUtil.AddClick(image, action);
        }

        private static Image PrepareClickSurface(Component target)
        {
            if (target == null) return null;
            GameObject go = target.gameObject;
            foreach (Graphic graphic in go.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            Image image = go.GetComponent<Image>();
            if (image == null)
            {
                image = go.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0f);
            }
            image.raycastTarget = true;
            return image;
        }

        private static void DisableClickSurface(Component target)
        {
            if (target == null) return;
            foreach (Graphic graphic in target.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
        }
    }
}
