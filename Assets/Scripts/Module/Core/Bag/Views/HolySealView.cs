using System;
using System.Collections.Generic;
using Shenxiao.Generated.UI.HolySeal;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Composite;
using Shenxiao.Module.Core.HolySeal;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 影骸战衣主页面。复用现有 HolySealView/HolySealEquipItem Prefab，数据只来自
    /// 15010(pos30/31) 与 65401/07 权威回包；按钮打开现有 HolySealModule 子窗。
    /// </summary>
    public sealed class HolySealView : HolySealViewBind
    {
        private const int EquipCount = 11;
        private const int BagColumns = 6;
        private const int BagSlotCount = 200;
        private const int BagPoolRows = 6;
        private const float BagCellStep = 99f;

        private static readonly Vector2[] EquipPositions =
        {
            new Vector2(479f, -117f), new Vector2(348f, -41f), new Vector2(199f, -68f),
            new Vector2(101f, -184f), new Vector2(101f, -335f), new Vector2(199f, -452f),
            new Vector2(374f, -202f), new Vector2(254f, -324f), new Vector2(374f, -444f),
            new Vector2(494f, -324f), new Vector2(374f, -324f)
        };

        // 老端 HolySealView.GetEquipTypeByPos：展示槽序号 -> config_goods.subtype。
        private static readonly int[] EquipTypes = { 6, 5, 4, 3, 2, 1, 7, 8, 9, 10, 11 };

        private sealed class Cell
        {
            public HolySealEquipItemBind Bind;
            public BagGoods Goods;
            public int EquipType;
            public bool IsBagSlot;
        }

        private readonly List<Cell> _equipCells = new List<Cell>(EquipCount);
        private readonly List<Cell> _bagCells = new List<Cell>();
        private IReadOnlyList<BagGoods> _bagGoods = Array.Empty<BagGoods>();
        private int _bagDisplayCount;
        private int _firstBagRow = -1;
        private bool _bagScrollHooked;
        private bool _subscribed;

        protected override void OnInit()
        {
            SetActive(_img_stren_red, false);
            SetActive(_img_preview_red, false);
            SetActive(_img_decompose_red, false);
            SetActive(_img_composite_red, false);
            SetActive(_img_soul_red, false);
            if (_tpl_HolySealEquipItem != null) _tpl_HolySealEquipItem.SetActive(false);

            BindAction(_img_attr, () => BagFlow.OpenModuleSub("holySeal", "HolySealModule", "HolySealAttrView"));
            BindAction(_img_tips, () => InstructionFlow.Show(6541));
            BindAction(_gp_stren, () => BagFlow.OpenModuleSub("holySeal", "HolySealModule", "HolySealStrenView"));
            BindAction(_gp_preview, () => BagFlow.OpenModuleSub("holySeal", "HolySealModule", "HolySealSuitPreviewView"));
            BindAction(_gp_decompose, () => BagFlow.OpenModuleSub("holySeal", "HolySealModule", "HolySealDecomposeView"));
            BindAction(_gp_soul, () => BagFlow.OpenModuleSub("holySeal", "HolySealModule", "HolySoulView"));
            BindAction(_gp_composite, CompositeFlow.Open);
            // 旧端按活动状态路由 KfHolyArea(99)/HolyTerritory(88)。两条页面路线尚无 Unity View，
            // 保留按钮命中但不伪造成功；Bag v2 台账将该外部消费者标 blocked。
            BindAction(_btn_holy, () => GameLog.Warn("Bag", "影骸战衣主活动入口等待 KfHolyArea/HolyTerritory 页面实现"));
        }

        protected override void OnShow(object args)
        {
            BagFlow.ApplyWindowTitlePresentation(2);
            Subscribe();
            EnsureEquipCells();
            RefreshAll();

            if (!BagModel.Instance.HasContainerData(BagModel.POS_HOLY_SEAL_BAG))
                BagController.Instance.RequestContainer(BagModel.POS_HOLY_SEAL_BAG);
            if (!BagModel.Instance.HasContainerData(BagModel.POS_HOLY_SEAL))
                BagController.Instance.RequestContainer(BagModel.POS_HOLY_SEAL);
            if (!HolySealModel.Instance.HasEquipSnapshot)
                HolySealController.Instance.RequestStartup();
            if (!HolySealModel.Instance.HasRating)
                HolySealController.Instance.RequestRating();
        }

        protected override void OnHide()
        {
            if (_list_bag != null)
            {
                _list_bag.StopMovement();
                _list_bag.velocity = Vector2.zero;
            }
            Unsubscribe();
        }
        protected override void OnDispose()
        {
            Unsubscribe();
            if (_bagScrollHooked && _list_bag != null)
            {
                _list_bag.onValueChanged.RemoveListener(OnBagScroll);
                _bagScrollHooked = false;
            }
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            BagModel.Instance.ContainerChanged += OnContainerChanged;
            HolySealModel.Instance.Changed += RefreshAll;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            BagModel.Instance.ContainerChanged -= OnContainerChanged;
            HolySealModel.Instance.Changed -= RefreshAll;
            _subscribed = false;
        }

        private void OnContainerChanged(int pos)
        {
            if (pos == BagModel.POS_HOLY_SEAL_BAG || pos == BagModel.POS_HOLY_SEAL) RefreshAll();
        }

        private void RefreshAll()
        {
            if (_lb_fighting != null)
                _lb_fighting.text = HolySealModel.Instance.HasRating
                    ? HolySealModel.Instance.TotalRating.ToString()
                    : "0";
            RefreshEquip();
            RefreshBag();
        }

        private void EnsureEquipCells()
        {
            if (_tpl_HolySealEquipItem == null || _gp_equips == null || _equipCells.Count > 0) return;
            for (int i = 0; i < EquipCount; i++)
            {
                Cell cell = CreateCell(_gp_equips, "HolySealEquipSlot_" + (i + 1));
                if (cell == null) break;
                cell.EquipType = EquipTypes[i];
                ((RectTransform)cell.Bind.transform).anchoredPosition = EquipPositions[i];
                _equipCells.Add(cell);
            }
        }

        private void RefreshEquip()
        {
            EnsureEquipCells();
            IReadOnlyList<BagGoods> equipped = BagModel.Instance.GetContainer(BagModel.POS_HOLY_SEAL);
            for (int i = 0; i < _equipCells.Count; i++)
            {
                BagGoods match = null;
                int subtype = EquipTypes[i];
                for (int j = 0; j < equipped.Count; j++)
                {
                    BagGoods candidate = equipped[j];
                    GoodsModel.GoodsBasic basic = candidate != null
                        ? GoodsModel.GetGoodsBasicByTypeId(candidate.TypeId)
                        : null;
                    if (candidate != null && (basic?.Subtype == subtype || candidate.Cell == subtype))
                    {
                        match = candidate;
                        break;
                    }
                }
                SetCell(_equipCells[i], match);
            }
        }

        private void RefreshBag()
        {
            if (_list_bag == null || _list_bag.content == null || _tpl_HolySealEquipItem == null) return;
            _bagGoods = BagModel.Instance.GetContainer(BagModel.POS_HOLY_SEAL_BAG);
            _bagDisplayCount = Mathf.Max(BagSlotCount, _bagGoods.Count);
            int poolCount = Mathf.Min(_bagDisplayCount, BagColumns * BagPoolRows);
            while (_bagCells.Count < poolCount)
            {
                Cell cell = CreateCell(_list_bag.content, "HolySealBagCellPool_" + _bagCells.Count);
                if (cell == null) break;
                cell.IsBagSlot = true;
                _bagCells.Add(cell);
            }

            int rows = Mathf.CeilToInt(_bagDisplayCount / (float)BagColumns);
            _list_bag.content.sizeDelta = new Vector2(_list_bag.content.sizeDelta.x, Mathf.Max(BagCellStep, rows * BagCellStep));
            if (!_bagScrollHooked)
            {
                _list_bag.onValueChanged.AddListener(OnBagScroll);
                _bagScrollHooked = true;
            }
            _firstBagRow = -1;
            RenderBagWindow(true);
        }

        private void OnBagScroll(Vector2 _) => RenderBagWindow(false);

        private void RenderBagWindow(bool force)
        {
            if (_list_bag == null || _list_bag.content == null || _bagCells.Count == 0) return;
            int totalRows = Mathf.CeilToInt(_bagDisplayCount / (float)BagColumns);
            int firstRow = Mathf.Max(0, Mathf.FloorToInt(Mathf.Max(0f, _list_bag.content.anchoredPosition.y) / BagCellStep) - 1);
            firstRow = Mathf.Min(firstRow, Mathf.Max(0, totalRows - BagPoolRows));
            if (!force && firstRow == _firstBagRow) return;
            _firstBagRow = firstRow;

            for (int i = 0; i < _bagCells.Count; i++)
            {
                int slotIndex = firstRow * BagColumns + i;
                bool active = slotIndex < _bagDisplayCount;
                Cell cell = _bagCells[i];
                cell.Bind.gameObject.SetActive(active);
                if (!active) continue;
                RectTransform rt = (RectTransform)cell.Bind.transform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
                rt.localScale = Vector3.one * 0.86f;
                rt.anchoredPosition = new Vector2(slotIndex % BagColumns * BagCellStep, -(slotIndex / BagColumns) * BagCellStep);
                SetCell(cell, slotIndex < _bagGoods.Count ? _bagGoods[slotIndex] : null);
            }
        }

        private Cell CreateCell(Transform parent, string name)
        {
            GameObject go = Instantiate(_tpl_HolySealEquipItem, parent, false);
            go.name = name;
            go.SetActive(true);
            HolySealEquipItemBind bind = go.GetComponent<HolySealEquipItemBind>();
            if (bind == null)
            {
                GameLog.Error("Bag", "HolySealEquipItem Prefab 缺 Bind: {0}", name);
                Destroy(go);
                return null;
            }
            bind.Show();
            var cell = new Cell { Bind = bind };
            Image click = PrepareClickSurface(bind._gp_con);
            if (click != null) UIUtil.AddClick(click, () => OpenCellDetails(cell));
            return cell;
        }

        private static void OpenCellDetails(Cell cell)
        {
            if (cell?.Goods == null) return;
            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(cell.Goods.TypeId);
            if (basic != null && basic.Type == 60)
            {
                // HolySealToolTips lives in the frozen Common module. Keep the specialized identity
                // instead of routing type 60 through generic ItemTips/15201 by mistake.
                GameLog.Warn("Bag", "HolySealToolTips requires the shared Common consumer; generic equipment details are intentionally not used");
                return;
            }
            ItemTipsView.Show(cell.Goods);
        }

        private static async void SetCell(Cell cell, BagGoods goods)
        {
            if (cell?.Bind == null) return;
            cell.Goods = goods;
            bool has = goods != null && goods.TypeId > 0;
            bool emptyEquip = !has && cell.EquipType > 0;
            bool emptyBag = !has && cell.IsBagSlot;
            SetActive(cell.Bind._img_bg, has || emptyEquip || emptyBag);
            SetActive(cell.Bind._img_icon, has || emptyEquip);
            SetActive(cell.Bind._gp_stage, has && goods.EquipStage > 0);
            if (cell.Bind._img_bg != null) cell.Bind._img_bg.enabled = has || emptyEquip || emptyBag;
            if (cell.Bind._img_icon != null) cell.Bind._img_icon.enabled = has || emptyEquip;
            if (!has)
            {
                if (emptyEquip)
                {
                    int group = cell.EquipType < 7 || cell.EquipType == 11 ? 1 : 2;
                    await ResManager.SetImageAsync(cell.Bind._img_bg,
                        GameResPath.GetIcon("holySeal", "bg_" + group + "_0"), false, false);
                    if (cell.Goods != goods) return;
                    await ResManager.SetImageAsync(cell.Bind._img_icon,
                        GameResPath.GetIcon("holySeal", "base_" + cell.EquipType), false, false);
                }
                else if (emptyBag)
                {
                    await ResManager.SetImageAsync(cell.Bind._img_bg,
                        GameResPath.GetIcon("common", "com_goods_plate_0"), false, false);
                }
                return;
            }

            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(goods.TypeId);
            if (basic == null) return;
            if (cell.Bind._lb_stage != null) cell.Bind._lb_stage.text = goods.EquipStage > 0 ? goods.EquipStage + "阶" : string.Empty;
            if (cell.Bind._img_bg != null)
            {
                string background = cell.EquipType > 0
                    ? GameResPath.GetIcon("holySeal", "bg_" + (cell.EquipType < 7 || cell.EquipType == 11 ? 1 : 2)
                        + "_" + GoodsModel.GetDisplayColor(goods.TypeId))
                    : GameResPath.GetIcon("common", "com_goods_plate_" + GoodsModel.GetDisplayColor(goods.TypeId));
                await ResManager.SetImageAsync(cell.Bind._img_bg, background, false, false);
            }
            if (cell.Goods != goods) return;
            if (cell.Bind._img_icon != null)
                await ResManager.SetImageAsync(cell.Bind._img_icon, GameResPath.GetGoodsIconPath(basic.Icon), false, false);
        }

        private static void BindAction(Component target, Action action)
        {
            Image image = PrepareClickSurface(target);
            if (image != null) UIUtil.AddClick(image, action);
        }

        private static Image PrepareClickSurface(Component target)
        {
            if (target == null) return null;
            foreach (Graphic graphic in target.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            Image image = target.GetComponent<Image>();
            if (image == null)
            {
                image = target.gameObject.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0f);
            }
            image.raycastTarget = true;
            return image;
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null) component.gameObject.SetActive(active);
        }
    }
}
