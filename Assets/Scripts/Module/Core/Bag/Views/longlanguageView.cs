using System;
using System.Collections.Generic;
using Shenxiao.Generated.UI.HolySeal;
using Shenxiao.Generated.UI.Longlanguage;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Composite;
using Shenxiao.Module.Core.Longlang;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 九天神祭主页面。复用现有 longlanguageView/longlangEquipItem/HolySealBagItem Prefab，
    /// 15010(pos43/44) 决定真实物品实例，62201/07 决定部位与战力。
    /// </summary>
    public sealed class longlanguageView : LonglanguageViewBind
    {
        private const int EquipCount = 11;
        private const int MoneyTypeId = 36255095;
        private const float BagCellStep = 99f;

        private sealed class BagCell
        {
            public HolySealBagItemBind Bind;
            public BagGoods Goods;
        }

        private sealed class EquipCell
        {
            public LonglangEquipItemBind Bind;
            public BagGoods Goods;
        }

        private readonly List<BagCell> _bagCells = new List<BagCell>();
        private readonly List<EquipCell> _equipCells = new List<EquipCell>(EquipCount);
        private FightingShowSmallItem _fightItem;
        private bool _subscribed;

        protected override void OnInit()
        {
            SetActive(red2, false);
            SetActive(red3, false);
            SetActive(red4, false);
            if (_tpl_HolySealBagItem != null) _tpl_HolySealBagItem.SetActive(false);
            if (_tpl_FightingShowSmallItem != null) _tpl_FightingShowSmallItem.SetActive(false);
            if (_tpl_DemonMainView != null) _tpl_DemonMainView.SetActive(false);
            if (_tpl_longlangEquipItem != null) _tpl_longlangEquipItem.SetActive(false);

            BindAction(btn1, () => BagFlow.OpenModuleSub("longlanguage", "LonglanguageModule", "longlangSuitView"));
            BindAction(btn2, () => BagFlow.OpenModuleSub("longlanguage", "LonglanguageModule", "longlangSBaseView"));
            BindAction(btn3, () => BagFlow.OpenModuleSub("longlanguage", "LonglanguageModule", "longlangStrView"));
            BindAction(btn4, CompositeFlow.Open);
            BindAction(shopBtn, () => GameLog.Warn("Bag", "九天神祭道痕兑换是外部Shop路线，等待Shop精确tab合同"));
            BindAction(propBtn, () => GameLog.Warn("Bag", "longlangAttrView在当前仓库无可编辑Prefab，Bag v2保持阻塞而非伪开"));
            BindAction(icon, () => ItemTipsView.Show(MoneyTypeId, BagModel.Instance.GetSpecialScore(MoneyTypeId)));

            EnsureFightItem();
            EnsureEquipCells();
        }

        protected override void OnShow(object args)
        {
            BagFlow.ApplyWindowTitlePresentation(4);
            Subscribe();
            EnsureFightItem();
            EnsureEquipCells();
            RefreshAll();

            if (!BagModel.Instance.HasContainerData(BagModel.POS_LONGLANG_BAG))
                BagController.Instance.RequestContainer(BagModel.POS_LONGLANG_BAG);
            if (!BagModel.Instance.HasContainerData(BagModel.POS_LONGLANG_EQUIP))
                BagController.Instance.RequestContainer(BagModel.POS_LONGLANG_EQUIP);
            if (!LonglangModel.Instance.HasEquipments)
                LonglangController.Instance.RequestStartup();
            if (!LonglangModel.Instance.HasRating)
                LonglangController.Instance.RequestRating();
        }

        protected override void OnHide() => Unsubscribe();
        protected override void OnDispose() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed) return;
            BagModel.Instance.ContainerChanged += OnContainerChanged;
            LonglangModel.Instance.Changed += RefreshAll;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            BagModel.Instance.ContainerChanged -= OnContainerChanged;
            LonglangModel.Instance.Changed -= RefreshAll;
            _subscribed = false;
        }

        private void OnContainerChanged(int pos)
        {
            if (pos == BagModel.POS_LONGLANG_BAG || pos == BagModel.POS_LONGLANG_EQUIP) RefreshAll();
        }

        private void RefreshAll()
        {
            EnsureFightItem();
            _fightItem?.SetFighting(LonglangModel.Instance.Rating?.Rating ?? 0);
            if (money != null) money.text = BagModel.Instance.GetSpecialScore(MoneyTypeId).ToString();
            RefreshMoneyIcon();
            RefreshEquip();
            RefreshBag();
        }

        private void EnsureFightItem()
        {
            if (_fightItem != null || _tpl_FightingShowSmallItem == null || fight == null) return;
            GameObject go = Instantiate(_tpl_FightingShowSmallItem, fight, false);
            go.name = "LonglangFight";
            go.SetActive(true);
            _fightItem = go.GetComponent<FightingShowSmallItem>();
            _fightItem?.Show();
        }

        private void EnsureEquipCells()
        {
            if (_equipCells.Count > 0 || _tpl_longlangEquipItem == null) return;
            RectTransform[] slots = { _gp_0, _gp_1, _gp_2, _gp_3, _gp_4, _gp_5, _gp_6, _gp_7, _gp_8, _gp_9, _gp_10 };
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;
                GameObject go = Instantiate(_tpl_longlangEquipItem, slots[i], false);
                go.name = "LonglangEquipSlot_" + (i + 1);
                go.SetActive(true);
                LonglangEquipItemBind bind = go.GetComponent<LonglangEquipItemBind>();
                if (bind == null)
                {
                    GameLog.Error("Bag", "longlangEquipItem Prefab 缺 Bind: pos={0}", i + 1);
                    Destroy(go);
                    continue;
                }
                bind.Show();
                var cell = new EquipCell { Bind = bind };
                Image click = PrepareClickSurface(bind._gp_con);
                if (click != null) UIUtil.AddClick(click, () => { if (cell.Goods != null) ItemTipsView.Show(cell.Goods); });
                _equipCells.Add(cell);
            }
        }

        private void RefreshEquip()
        {
            EnsureEquipCells();
            IReadOnlyList<BagGoods> container = BagModel.Instance.GetContainer(BagModel.POS_LONGLANG_EQUIP);
            for (int i = 0; i < _equipCells.Count; i++)
            {
                int position = i + 1;
                BagGoods match = null;
                if (LonglangModel.Instance.TryGetEquipment((byte)position, out LonglangModel.Equipment equipment))
                    match = FindGoods(container, equipment.GoodsId, position);
                if (match == null) match = FindGoods(container, 0, position);
                SetEquipCell(_equipCells[i], match);
            }
        }

        private void RefreshBag()
        {
            if (_Scroller1 == null || _Scroller1.content == null || _tpl_HolySealBagItem == null) return;
            IReadOnlyList<BagGoods> goods = BagModel.Instance.GetContainer(BagModel.POS_LONGLANG_BAG);
            while (_bagCells.Count < goods.Count)
            {
                GameObject go = Instantiate(_tpl_HolySealBagItem, _Scroller1.content, false);
                go.name = "LonglangBagCell_" + _bagCells.Count;
                go.SetActive(true);
                HolySealBagItemBind bind = go.GetComponent<HolySealBagItemBind>();
                if (bind == null)
                {
                    GameLog.Error("Bag", "longlanguage 内置 HolySealBagItem 缺 Bind");
                    Destroy(go);
                    break;
                }
                bind.Show();
                var cell = new BagCell { Bind = bind };
                Image click = PrepareClickSurface(bind._gp_con);
                if (click != null) UIUtil.AddClick(click, () => { if (cell.Goods != null) ItemTipsView.Show(cell.Goods); });
                _bagCells.Add(cell);
            }

            float width = _Scroller1.viewport != null ? _Scroller1.viewport.rect.width : _Scroller1.content.rect.width;
            int columns = Mathf.Max(1, Mathf.FloorToInt(Mathf.Max(BagCellStep, width) / BagCellStep));
            int rows = Mathf.CeilToInt(goods.Count / (float)columns);
            _Scroller1.content.sizeDelta = new Vector2(_Scroller1.content.sizeDelta.x, Mathf.Max(BagCellStep, rows * BagCellStep));
            for (int i = 0; i < _bagCells.Count; i++)
            {
                bool active = i < goods.Count;
                _bagCells[i].Bind.gameObject.SetActive(active);
                if (!active) continue;
                RectTransform rt = (RectTransform)_bagCells[i].Bind.transform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(i % columns * BagCellStep, -(i / columns) * BagCellStep);
                SetBagCell(_bagCells[i], goods[i]);
            }
        }

        private static BagGoods FindGoods(IReadOnlyList<BagGoods> goods, ulong goodsId, int position)
        {
            if (goods == null) return null;
            for (int i = 0; i < goods.Count; i++)
            {
                BagGoods value = goods[i];
                if (value == null) continue;
                if (goodsId > 0 && unchecked((ulong)value.GoodsId) == goodsId) return value;
                GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(value.TypeId);
                if (goodsId == 0 && (value.Cell == position || basic?.Subtype == position)) return value;
            }
            return null;
        }

        private static async void SetEquipCell(EquipCell cell, BagGoods goods)
        {
            if (cell?.Bind == null) return;
            cell.Goods = goods;
            bool has = goods != null && goods.TypeId > 0;
            SetActive(cell.Bind._img_icon, has);
            SetActive(cell.Bind._gp_stage, has && goods.EquipStage > 0);
            SetActive(cell.Bind.redImg, false);
            if (!has) return;
            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(goods.TypeId);
            if (basic == null) return;
            if (cell.Bind._lb_stage != null) cell.Bind._lb_stage.text = goods.EquipStage > 0 ? goods.EquipStage + "阶" : string.Empty;
            if (cell.Bind._img_icon != null)
                await ResManager.SetImageAsync(cell.Bind._img_icon, GameResPath.GetGoodsIconPath(basic.Icon), false, false);
        }

        private static async void SetBagCell(BagCell cell, BagGoods goods)
        {
            if (cell?.Bind == null) return;
            cell.Goods = goods;
            bool has = goods != null && goods.TypeId > 0;
            SetActive(cell.Bind._img_icon, has);
            SetActive(cell.Bind._img_bind, has && goods.Bind != 0);
            SetActive(cell.Bind._gp_stage, has && goods.EquipStage > 0);
            SetActive(cell.Bind._gp_effect, false);
            SetActive(cell.Bind._gp_effect2, false);
            SetActive(cell.Bind._img_up, false);
            if (cell.Bind._lb_count != null) cell.Bind._lb_count.text = has && goods.GoodsNum > 1 ? goods.GoodsNum.ToString() : string.Empty;
            if (!has) return;
            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(goods.TypeId);
            if (basic == null) return;
            if (cell.Bind._lb_stage != null) cell.Bind._lb_stage.text = goods.EquipStage > 0 ? goods.EquipStage + "阶" : string.Empty;
            if (cell.Bind._img_bg != null)
                await ResManager.SetImageAsync(cell.Bind._img_bg,
                    GameResPath.GetIcon("common", "com_goods_plate_" + GoodsModel.GetDisplayColor(goods.TypeId)), false, false);
            if (cell.Goods != goods) return;
            if (cell.Bind._img_icon != null)
                await ResManager.SetImageAsync(cell.Bind._img_icon, GameResPath.GetGoodsIconPath(basic.Icon), false, false);
        }

        private async void RefreshMoneyIcon()
        {
            if (icon == null) return;
            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(MoneyTypeId);
            if (basic != null) await ResManager.SetImageAsync(icon, GameResPath.GetGoodsIconPath(basic.Icon), false, false);
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
