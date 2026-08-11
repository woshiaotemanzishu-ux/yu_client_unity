using System;
using System.Collections.Generic;
using Shenxiao.Generated.UI.Revelation;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Revelation;
using Shenxiao.Module.Core.Role;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 启示圣铠主页面。只消费 15010(pos40/41) 与既有 28606/28609 只读快照；
    /// 不从本页发送穿戴、卸下、聚灵、吞噬或升级事务。
    /// </summary>
    public sealed class RevelationEquipView : RevelationEquipViewBind
    {
        private const int EquipCount = 10;
        private const int BagColumns = 6;
        private const int BagPoolRows = 6;
        private const float BagCellStep = 96f;

        private static readonly Vector2[] EquipPositions =
        {
            new Vector2(87f, -267f), new Vector2(307f, -43f), new Vector2(307f, -487f),
            new Vector2(307f, -201f), new Vector2(307f, -330f), new Vector2(197f, -154f),
            new Vector2(417f, -154f), new Vector2(417f, -377f), new Vector2(197f, -377f),
            new Vector2(529f, -267f)
        };

        private sealed class EquipCell
        {
            public RevelationEquipItemBind Bind;
            public BagGoods Goods;
            public int Position;
        }

        private sealed class BagCell
        {
            public RevelationBagItemBind Bind;
            public BagGoods Goods;
        }

        private readonly List<EquipCell> _equipCells = new List<EquipCell>(EquipCount);
        private readonly List<BagCell> _bagCells = new List<BagCell>();
        private readonly List<BagGoods> _bagSnapshot = new List<BagGoods>();
        private FightingShowSmallItem _fightItem;
        private int _bagDisplayCount;
        private int _firstBagRow = -1;
        private bool _bagScrollHooked;
        private bool _subscribed;

        protected override void OnInit()
        {
            SetActive(comRed, false);
            SetActive(devourRed, false);
            if (_tpl_RevelationBagItem != null) _tpl_RevelationBagItem.SetActive(false);
            if (_tpl_FightingShowSmallItem != null) _tpl_FightingShowSmallItem.SetActive(false);
            if (_tpl_DemonMainView != null) _tpl_DemonMainView.SetActive(false);
            if (_tpl_longlanguageView != null) _tpl_longlanguageView.SetActive(false);
            if (_tpl_RevelationEquipItem != null) _tpl_RevelationEquipItem.SetActive(false);

            BindBlocked(suitBtn, "RevelationSuitView", "套装总览子窗 Prefab 尚未落地");
            BindBlocked(getBtn, "EternityMainView", "启示圣境 136 路由尚未落地");
            BindBlocked(comBtn, "CompositeRevelationView", "启示铠铸页仍由合成模块冻结");
            BindBlocked(soulBtn, "RevelationDevourView", "圣铠聚灵子窗 Prefab 尚未落地");
            BindBlocked(propBtn, "RevelationAttrView", "属性子窗 Prefab 尚未落地");
        }

        protected override void OnShow(object args)
        {
            BagFlow.ApplyWindowTitlePresentation(3);
            Subscribe();
            EnsureEquipCells();
            EnsureFightItem();
            RefreshAll();

            if (!BagModel.Instance.HasContainerData(BagModel.POS_REVELATION_EQUIP))
                BagController.Instance.RequestContainer(BagModel.POS_REVELATION_EQUIP);
            if (!BagModel.Instance.HasContainerData(BagModel.POS_REVELATION_BAG))
                BagController.Instance.RequestContainer(BagModel.POS_REVELATION_BAG);
            if (!RevelationModel.Instance.HasData)
                RevelationController.Instance.RequestStartup();
            else
                RevelationController.Instance.RequestPower();
        }

        protected override void OnHide()
        {
            if (gp_bag != null)
            {
                gp_bag.StopMovement();
                gp_bag.velocity = Vector2.zero;
            }
            Unsubscribe();
        }

        protected override void OnDispose()
        {
            Unsubscribe();
            if (_bagScrollHooked && gp_bag != null)
            {
                gp_bag.onValueChanged.RemoveListener(OnBagScroll);
                _bagScrollHooked = false;
            }
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            BagModel.Instance.ContainerChanged += OnContainerChanged;
            RevelationModel.Instance.Changed += RefreshAll;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            BagModel.Instance.ContainerChanged -= OnContainerChanged;
            RevelationModel.Instance.Changed -= RefreshAll;
            _subscribed = false;
        }

        private void OnContainerChanged(int pos)
        {
            if (pos == BagModel.POS_REVELATION_EQUIP || pos == BagModel.POS_REVELATION_BAG)
                RefreshAll();
        }

        private void RefreshAll()
        {
            RefreshEquip();
            RefreshBag();
            if (_fightItem != null) _fightItem.SetFighting((long)RevelationModel.Instance.Power);
        }

        private void EnsureFightItem()
        {
            if (_fightItem != null || _tpl_FightingShowSmallItem == null || gp_fight == null) return;
            GameObject go = Instantiate(_tpl_FightingShowSmallItem, gp_fight, false);
            go.name = "RevelationFighting_Runtime";
            go.SetActive(true);
            _fightItem = go.GetComponent<FightingShowSmallItem>();
            if (_fightItem != null) _fightItem.Show();
        }

        private void EnsureEquipCells()
        {
            if (_equipCells.Count > 0 || equipGp == null || _tpl_RevelationEquipItem == null) return;
            for (int i = 0; i < EquipCount; i++)
            {
                GameObject go = Instantiate(_tpl_RevelationEquipItem, equipGp, false);
                go.name = "RevelationEquipSlot_" + (i + 1);
                go.SetActive(true);
                RevelationEquipItemBind bind = go.GetComponent<RevelationEquipItemBind>();
                if (bind == null)
                {
                    GameLog.Error("Bag", "RevelationEquipItem missing bind at slot {0}", i + 1);
                    Destroy(go);
                    break;
                }
                bind.Show();
                RectTransform rt = (RectTransform)bind.transform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = EquipPositions[i];
                var cell = new EquipCell { Bind = bind, Position = i + 1 };
                Image click = PrepareClickSurface(bind._gp_con);
                if (click != null) UIUtil.AddClick(click, () => OpenEquipDetails(cell));
                _equipCells.Add(cell);
            }
        }

        private void RefreshEquip()
        {
            EnsureEquipCells();
            IReadOnlyList<BagGoods> equipped = BagModel.Instance.GetContainer(BagModel.POS_REVELATION_EQUIP);
            for (int i = 0; i < _equipCells.Count; i++)
            {
                BagGoods match = null;
                for (int j = 0; j < equipped.Count; j++)
                {
                    BagGoods candidate = equipped[j];
                    GoodsModel.GoodsBasic basic = candidate != null
                        ? GoodsModel.GetGoodsBasicByTypeId(candidate.TypeId)
                        : null;
                    if (candidate != null && (candidate.Cell == i + 1 || basic?.Subtype == i + 1))
                    {
                        match = candidate;
                        break;
                    }
                }
                SetEquipCell(_equipCells[i], match);
            }
        }

        private static async void SetEquipCell(EquipCell cell, BagGoods goods)
        {
            if (cell?.Bind == null) return;
            cell.Goods = goods;
            bool has = goods != null && goods.TypeId > 0;
            SetActive(cell.Bind._img_bg, has);
            SetActive(cell.Bind._img_icon, true);
            SetStarState(cell.Bind.star_group,
                new[] { cell.Bind.star_0, cell.Bind.star_1, cell.Bind.star_2, cell.Bind.star_3 },
                has ? goods.EquipStar : 0);
            if (cell.Bind._img_bg != null) cell.Bind._img_bg.enabled = has;
            if (cell.Bind._img_icon != null) cell.Bind._img_icon.enabled = true;

            string iconPath;
            if (!has)
            {
                iconPath = GameResPath.GetIcon("revelation", "ui_Apocalypse_zb" + cell.Position);
            }
            else
            {
                GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(goods.TypeId);
                if (basic == null) return;
                iconPath = GameResPath.GetGoodsIconPath(basic.Icon);
            }
            await ResManager.SetImageAsync(cell.Bind._img_icon, iconPath, false, false);
        }

        private void RefreshBag()
        {
            if (gp_bag == null || gp_bag.content == null || _tpl_RevelationBagItem == null) return;
            _bagSnapshot.Clear();
            IReadOnlyList<BagGoods> source = BagModel.Instance.GetContainer(BagModel.POS_REVELATION_BAG);
            for (int i = 0; i < source.Count; i++) if (source[i] != null) _bagSnapshot.Add(source[i]);
            _bagSnapshot.Sort(CompareBagGoods);

            int remainder = _bagSnapshot.Count % BagColumns;
            _bagDisplayCount = _bagSnapshot.Count + 24 - remainder;
            int poolCount = Mathf.Min(_bagDisplayCount, BagColumns * BagPoolRows);
            while (_bagCells.Count < poolCount)
            {
                GameObject go = Instantiate(_tpl_RevelationBagItem, gp_bag.content, false);
                go.name = "RevelationBagCellPool_" + _bagCells.Count;
                go.SetActive(true);
                RevelationBagItemBind bind = go.GetComponent<RevelationBagItemBind>();
                if (bind == null)
                {
                    GameLog.Error("Bag", "RevelationBagItem missing bind at pool {0}", _bagCells.Count);
                    Destroy(go);
                    break;
                }
                bind.Show();
                var cell = new BagCell { Bind = bind };
                Image click = PrepareClickSurface(bind._gp_con);
                if (click != null) UIUtil.AddClick(click, () => OpenBagDetails(cell));
                _bagCells.Add(cell);
            }

            int rows = Mathf.CeilToInt(_bagDisplayCount / (float)BagColumns);
            gp_bag.content.sizeDelta = new Vector2(gp_bag.content.sizeDelta.x, Mathf.Max(BagCellStep, rows * BagCellStep));
            if (!_bagScrollHooked)
            {
                gp_bag.onValueChanged.AddListener(OnBagScroll);
                _bagScrollHooked = true;
            }
            _firstBagRow = -1;
            RenderBagWindow(true);
        }

        private static int CompareBagGoods(BagGoods left, BagGoods right)
        {
            int byStar = right.EquipStar.CompareTo(left.EquipStar);
            if (byStar != 0) return byStar;
            int leftSubtype = GoodsModel.GetGoodsBasicByTypeId(left.TypeId)?.Subtype ?? 0;
            int rightSubtype = GoodsModel.GetGoodsBasicByTypeId(right.TypeId)?.Subtype ?? 0;
            int byPosition = leftSubtype.CompareTo(rightSubtype);
            return byPosition != 0 ? byPosition : right.TypeId.CompareTo(left.TypeId);
        }

        private void OnBagScroll(Vector2 _) => RenderBagWindow(false);

        private void RenderBagWindow(bool force)
        {
            if (gp_bag == null || gp_bag.content == null || _bagCells.Count == 0) return;
            int totalRows = Mathf.CeilToInt(_bagDisplayCount / (float)BagColumns);
            int firstRow = Mathf.Max(0, Mathf.FloorToInt(Mathf.Max(0f, gp_bag.content.anchoredPosition.y) / BagCellStep) - 1);
            firstRow = Mathf.Min(firstRow, Mathf.Max(0, totalRows - BagPoolRows));
            if (!force && firstRow == _firstBagRow) return;
            _firstBagRow = firstRow;

            for (int i = 0; i < _bagCells.Count; i++)
            {
                int slotIndex = firstRow * BagColumns + i;
                bool active = slotIndex < _bagDisplayCount;
                BagCell cell = _bagCells[i];
                cell.Bind.gameObject.SetActive(active);
                if (!active) continue;
                RectTransform rt = (RectTransform)cell.Bind.transform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
                rt.localScale = Vector3.one;
                rt.anchoredPosition = new Vector2(slotIndex % BagColumns * BagCellStep, -(slotIndex / BagColumns) * BagCellStep);
                SetBagCell(cell, slotIndex < _bagSnapshot.Count ? _bagSnapshot[slotIndex] : null);
            }
        }

        private static async void SetBagCell(BagCell cell, BagGoods goods)
        {
            if (cell?.Bind == null) return;
            cell.Goods = goods;
            bool has = goods != null && goods.TypeId > 0;
            SetActive(cell.Bind._img_bg, true);
            SetActive(cell.Bind._img_icon, has);
            SetActive(cell.Bind._img_bind, has && goods.Bind == 1);
            SetActive(cell.Bind._gp_effect, false);
            SetActive(cell.Bind.effect1, false);
            SetActive(cell.Bind.img_up, false);
            SetActive(cell.Bind.ban, false);
            if (cell.Bind._img_bg != null) cell.Bind._img_bg.enabled = true;
            if (cell.Bind._img_icon != null) cell.Bind._img_icon.enabled = has;
            if (cell.Bind._lb_count != null) cell.Bind._lb_count.text = has && goods.GoodsNum > 1 ? goods.GoodsNum.ToString() : string.Empty;
            SetStarState(cell.Bind.star_group,
                new[] { cell.Bind.star_0, cell.Bind.star_1, cell.Bind.star_2, cell.Bind.star_3 },
                has ? goods.EquipStar : 0);

            string plate = GameResPath.GetIcon("common", "com_goods_plate_" + (has ? GoodsModel.GetDisplayColor(goods.TypeId) : 0));
            await ResManager.SetImageAsync(cell.Bind._img_bg, plate, false, false);
            if (!has || cell.Goods != goods) return;
            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(goods.TypeId);
            if (basic == null) return;
            SetActive(cell.Bind.ban, RoleModel.Instance.Level < basic.Level);
            await ResManager.SetImageAsync(cell.Bind._img_icon, GameResPath.GetGoodsIconPath(basic.Icon), false, false);
        }

        private static void OpenEquipDetails(EquipCell cell)
        {
            if (cell?.Goods == null) return;
            GameLog.Warn("Bag", "RevelationToolTips(unload) is a frozen shared consumer; generic equipment details are intentionally not used");
        }

        private static void OpenBagDetails(BagCell cell)
        {
            if (cell?.Goods == null) return;
            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(cell.Goods.TypeId);
            if (basic != null && basic.Type == 72)
            {
                GameLog.Warn("Bag", "RevelationToolTips(replace) is a frozen shared consumer; generic equipment details are intentionally not used");
                return;
            }
            ItemTipsView.Show(cell.Goods);
        }

        private static void BindBlocked(Component target, string identity, string reason)
        {
            Image image = PrepareClickSurface(target);
            if (image != null) UIUtil.AddClick(image,
                () => GameLog.Warn("Bag", "Revelation destination [{0}] blocked: {1}", identity, reason));
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

        private static void SetStarState(Component group, Image[] stars, int starCount)
        {
            int count = Mathf.Clamp(starCount, 0, stars.Length);
            SetActive(group, count > 0);
            for (int i = 0; i < stars.Length; i++) SetActive(stars[i], i < count);
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null) component.gameObject.SetActive(active);
        }
    }
}
