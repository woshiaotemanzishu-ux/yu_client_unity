using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.EquipRefinement;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 神屠九炼：真实穿戴装备横列、中心装备详情、极品/当前/下一阶九炼属性、材料消耗和说明层。
    /// 写事务只由玩家点击且通过真实装备、详情、配置与材料预检后发送 15255。
    /// </summary>
    public sealed class EquipRefinementView : EquipRefinementViewBind
    {
        private const int RefinementMinStage = 9;
        private readonly List<EquipWashItem> _equipmentItems = new List<EquipWashItem>(10);
        private int _selectedEquipType;
        private long _selectedGoodsId;
        private bool _subscribed;
        private int _lifecycleEpoch;
        private BaseAwardItem _costItem;
        private EquipRefiAttrsItemBind _extraAttrsItem;
        private EquipRefiAttrsItemBind _currentAttrsItem;
        private EquipRefiAttrsItemBind _nextAttrsItem;

        protected override void OnInit()
        {
            SetNode(_img_red, false);
            SetNode(_img_max, false);
            SetNode(_gp_instrcution, false);
            if (_tpl_EquipWashItem != null) _tpl_EquipWashItem.SetActive(false);
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
            if (_tpl_EquipRefiAttrsItem != null) _tpl_EquipRefiAttrsItem.SetActive(false);
            BindClick(_btn_refi, OnRefineClick);
            BindClick(insBtn, ToggleInstruction);
            BindClick(_Image5, () => SetNode(_gp_instrcution, false));
            BindClick(_img_equip, ShowSelectedTips);
        }

        protected override void OnShow(object args)
        {
            _selectedEquipType = 0;
            _selectedGoodsId = 0;
            ++_lifecycleEpoch;
            Subscribe();
            BuildRuntimeItems();
            RefreshEquipmentItems();
            if (!BagModel.Instance.HasEquipmentData) EquipWearController.Instance.RequestWornList();
            _ = EnsureConfigAndRefreshAsync(_lifecycleEpoch);
        }

        protected override void OnHide()
        {
            ++_lifecycleEpoch;
            Unsubscribe();
            foreach (EquipWashItem item in _equipmentItems)
                if (item != null && item.IsShown) item.Hide();
            HideAttrItem(_extraAttrsItem);
            HideAttrItem(_currentAttrsItem);
            HideAttrItem(_nextAttrsItem);
            if (_costItem != null && _costItem.IsShown) _costItem.Hide();
            SetNode(_gp_instrcution, false);
            if (_Scroller1 != null)
            {
                _Scroller1.StopMovement();
                _Scroller1.horizontalNormalizedPosition = 0f;
            }
        }

        protected override void OnDispose()
        {
            Unsubscribe();
            _equipmentItems.Clear();
            _costItem = null;
            _extraAttrsItem = null;
            _currentAttrsItem = null;
            _nextAttrsItem = null;
            base.OnDispose();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On(GlobalEvent.EVT_EQUIPMENT_UPDATE, RefreshEquipmentItems);
            EventDispatcher.On(GlobalEvent.EVT_BAG_UPDATE, RefreshEquipmentItems);
            EventDispatcher.On<long>(GlobalEvent.EVT_GOODS_DETAIL_UPDATE, OnGoodsDetailUpdated);
            EventDispatcher.On(GlobalEvent.EVT_EQUIP_REFINEMENT_UPDATE, OnRefinementUpdated);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off(GlobalEvent.EVT_EQUIPMENT_UPDATE, RefreshEquipmentItems);
            EventDispatcher.Off(GlobalEvent.EVT_BAG_UPDATE, RefreshEquipmentItems);
            EventDispatcher.Off<long>(GlobalEvent.EVT_GOODS_DETAIL_UPDATE, OnGoodsDetailUpdated);
            EventDispatcher.Off(GlobalEvent.EVT_EQUIP_REFINEMENT_UPDATE, OnRefinementUpdated);
            _subscribed = false;
        }

        private async Task EnsureConfigAndRefreshAsync(int epoch)
        {
            await EquipConfigs.EnsureLoaded();
            if (this == null || !IsShown || epoch != _lifecycleEpoch) return;
            RefreshEquipmentItems();
        }

        private void BuildRuntimeItems()
        {
            EquipWashItem template = EquipFlow.GetWashItemTemplate();
            if (_equipmentItems.Count == 0 && template != null && Content != null)
            {
                for (int equipType = 1; equipType <= 10; equipType++)
                {
                    GameObject go = Instantiate(template.gameObject, Content, false);
                    go.name = "EquipRefinementItem_Runtime_" + equipType;
                    go.SetActive(false);
                    EquipWashItem item = go.GetComponent<EquipWashItem>();
                    if (item == null) { Destroy(go); continue; }
                    item.Show();
                    item.SetEquipPos(equipType);
                    item.SetSelectCallback(SelectEquipment);
                    _equipmentItems.Add(item);
                }
            }
            else
            {
                foreach (EquipWashItem item in _equipmentItems)
                    if (item != null && !item.IsShown) item.Show();
            }

            _extraAttrsItem = EnsureAttrItem(_extraAttrsItem, _gp_extra_attr, "EquipRefiAttrs_Extra_Runtime");
            _currentAttrsItem = EnsureAttrItem(_currentAttrsItem, _gp_now_lv, "EquipRefiAttrs_Current_Runtime");
            _nextAttrsItem = EnsureAttrItem(_nextAttrsItem, _gp_next_lv, "EquipRefiAttrs_Next_Runtime");
            if (_costItem == null && _tpl_BaseAwardItem != null && _gp_cost_goods != null)
            {
                GameObject go = Instantiate(_tpl_BaseAwardItem, _gp_cost_goods, false);
                go.name = "EquipRefinementCost_Runtime";
                go.SetActive(false);
                _costItem = go.GetComponent<BaseAwardItem>();
                if (_costItem != null) _costItem.SetScale(81f / 127f);
                else Destroy(go);
            }
        }

        private EquipRefiAttrsItemBind EnsureAttrItem(EquipRefiAttrsItemBind current, Transform parent, string name)
        {
            if (current != null || _tpl_EquipRefiAttrsItem == null || parent == null) return current;
            GameObject go = Instantiate(_tpl_EquipRefiAttrsItem, parent, false);
            go.name = name;
            go.SetActive(false);
            EquipRefiAttrsItemBind item = go.GetComponent<EquipRefiAttrsItemBind>();
            if (item == null) { Destroy(go); return null; }
            item.Show();
            return item;
        }

        private void RefreshEquipmentItems()
        {
            BuildRuntimeItems();
            int firstWorn = 0;
            bool selectionStillValid = false;
            for (int equipType = 1; equipType <= 10; equipType++)
            {
                BagGoods worn = EquipAutoWear.GetWorn(equipType);
                if (worn == null) continue;
                if (firstWorn == 0) firstWorn = equipType;
                if (equipType == _selectedEquipType && worn.GoodsId == _selectedGoodsId) selectionStillValid = true;
            }

            foreach (EquipWashItem item in _equipmentItems)
            {
                int equipType = item.EquipType;
                BagGoods worn = EquipAutoWear.GetWorn(equipType);
                GoodsDetailVo detail = worn != null ? GoodsDynamicModel.Instance.Peek(worn.GoodsId) : null;
                item.SetUnlocked(true, 0);
                item.SetData(equipType, worn?.GoodsId ?? 0, worn?.TypeId ?? 0, detail?.Division ?? 0,
                    _tpl_BaseAwardItem, detail);
                item.SetRefinementLevel(detail?.RefinementLv ?? 0);
            }

            if (!selectionStillValid)
            {
                BagGoods first = firstWorn > 0 ? EquipAutoWear.GetWorn(firstWorn) : null;
                _selectedEquipType = first != null ? firstWorn : 0;
                _selectedGoodsId = first?.GoodsId ?? 0;
            }
            RefreshSelection();
            RefreshSelectedEquipment();
        }

        private void SelectEquipment(int equipType, long goodsId)
        {
            _selectedEquipType = equipType > 0 && goodsId > 0 ? equipType : 0;
            _selectedGoodsId = _selectedEquipType > 0 ? goodsId : 0;
            RefreshSelection();
            RefreshSelectedEquipment();
        }

        private void RefreshSelection()
        {
            foreach (EquipWashItem item in _equipmentItems)
                item.SetSelect(item.EquipType == _selectedEquipType);
        }

        private void RefreshSelectedEquipment()
        {
            BagGoods worn = _selectedEquipType > 0 ? EquipAutoWear.GetWorn(_selectedEquipType) : null;
            if (_img_equip != null)
            {
                _img_equip.gameObject.SetActive(worn != null);
                if (worn != null)
                {
                    GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(worn.TypeId);
                    if (basic != null && !string.IsNullOrEmpty(basic.Icon))
                        _ = ResManager.SetImageAsync(_img_equip, GameResPath.GetGoodsIconPath(basic.Icon), false, false);
                }
            }
            if (worn == null)
            {
                ApplyDetail(null);
                return;
            }

            GoodsDetailVo cached = GoodsDynamicModel.Instance.Peek(worn.GoodsId);
            if (cached != null) ApplyDetail(cached);
            else
            {
                int epoch = _lifecycleEpoch;
                long goodsId = worn.GoodsId;
                GoodsDynamicModel.Instance.RequestDetail(goodsId, detail =>
                {
                    if (this == null || !IsShown || epoch != _lifecycleEpoch || goodsId != _selectedGoodsId) return;
                    ApplyDetail(detail);
                    RefreshEquipmentRow(detail);
                });
            }
        }

        private void ApplyDetail(GoodsDetailVo detail)
        {
            BagGoods worn = _selectedEquipType > 0 ? EquipAutoWear.GetWorn(_selectedEquipType) : null;
            GoodsModel.EquipAttr equipAttr = worn != null ? GoodsModel.GetEquipAttr(worn.TypeId) : null;
            bool hasEligibleAttr = false;
            if (detail?.ExtraAttrs != null)
            {
                foreach (EquipExtraAttr attr in detail.ExtraAttrs)
                    if (attr.Color >= 4) { hasEligibleAttr = true; break; }
            }
            bool stageReady = equipAttr != null && equipAttr.Stage >= RefinementMinStage;
            bool canRefine = detail != null && stageReady && hasEligibleAttr;
            int level = detail?.RefinementLv ?? 0;
            EquipConfigs.RefinementLevel currentCfg = default;
            bool hasCurrentCfg = level > 0 && EquipConfigs.TryGetRefinementLevel(_selectedEquipType, level, out currentCfg);
            bool hasNextCfg = EquipConfigs.TryGetRefinementLevel(_selectedEquipType, level + 1, out EquipConfigs.RefinementLevel nextCfg);

            SetNode(_gp_notice, !canRefine);
            SetNode(_gp_attr, canRefine);
            SetNode(_gp_cost, canRefine && hasNextCfg);
            SetNode(_btn_refi, canRefine && hasNextCfg);
            SetNode(_gp_arrow, canRefine && hasNextCfg);
            SetNode(_gp_next_lv, canRefine && hasNextCfg);
            SetNode(_img_max, canRefine && !hasNextCfg && EquipConfigs.IsLoaded);

            if (!canRefine)
            {
                if (lb_notice != null)
                {
                    if (worn == null || detail == null) lb_notice.text = "装备详情加载中";
                    else if (!stageReady) lb_notice.text = "当前装备低于9阶无法进行九炼\n请更换高阶装备";
                    else lb_notice.text = "当前装备品质较低无法进行九炼\n请更换更高品质装备";
                }
                SetNode(_img_red, false);
                return;
            }

            RenderAttrItem(_extraAttrsItem, "极品属性", detail.ExtraAttrs, 0);
            RenderAttrItem(_currentAttrsItem, "九炼+" + level, detail.ExtraAttrs,
                hasCurrentCfg ? currentCfg.Promote : 0);
            if (hasNextCfg)
            {
                RenderAttrItem(_nextAttrsItem, "九炼+" + (level + 1), detail.ExtraAttrs, nextCfg.Promote);
                if (_bit_add0 != null) _bit_add0.text = "+" + FormatPromote(hasCurrentCfg ? currentCfg.Promote : 0);
                if (_bit_add != null) _bit_add.text = "+" + FormatPromote(nextCfg.Promote);
                RefreshCost(nextCfg);
            }
            else
            {
                HideAttrItem(_nextAttrsItem);
                if (_bit_add0 != null) _bit_add0.text = hasCurrentCfg ? ("+" + FormatPromote(currentCfg.Promote)) : "配置未就绪";
                if (_bit_add != null) _bit_add.text = string.Empty;
                if (_lb_cost_num != null) _lb_cost_num.text = "配置未就绪";
                SetNode(_img_red, false);
            }
        }

        private void RenderAttrItem(EquipRefiAttrsItemBind item, string title, List<EquipExtraAttr> attrs, int promote)
        {
            if (item == null) return;
            if (!item.IsShown) item.Show();
            if (item._lb_lv != null) item._lb_lv.text = title;
            if (item._lb_attrs == null) return;
            var text = new StringBuilder();
            if (attrs != null)
            {
                foreach (EquipExtraAttr attr in attrs)
                {
                    if (attr.Color < 4) continue;
                    if (text.Length > 0) text.Append('\n');
                    string name = GoodsModel.GetAttrName(attr.AttrId);
                    if (promote <= 0)
                        text.Append(name).Append(" +").Append(GoodsModel.FormatAttrValue(attr.AttrId, attr.AttrVal));
                    else
                        text.Append(name).Append(" 神炼+").Append(FormatBonus(attr.AttrVal, promote));
                }
            }
            item._lb_attrs.text = text.Length > 0 ? text.ToString() : "暂无可九炼属性";
        }

        private void RefreshCost(EquipConfigs.RefinementLevel cfg)
        {
            long have = BagModel.Instance.GetTypeGoodsNum(cfg.MaterialTypeId);
            bool enough = have >= cfg.NeedNum;
            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(cfg.MaterialTypeId);
            if (_lb_cost_name != null) _lb_cost_name.text = basic?.Name ?? ("物品" + cfg.MaterialTypeId);
            if (_lb_cost_num != null)
            {
                _lb_cost_num.text = have + "/" + cfg.NeedNum;
                _lb_cost_num.color = enough ? new Color32(29, 182, 65, 255) : new Color32(255, 79, 80, 255);
            }
            SetNode(_img_red, enough);
            if (_costItem != null)
            {
                if (!_costItem.IsShown) _costItem.Show();
                _costItem.SetData(cfg.MaterialTypeId, cfg.NeedNum);
            }
        }

        private void RefreshEquipmentRow(GoodsDetailVo detail)
        {
            foreach (EquipWashItem item in _equipmentItems)
                if (item.EquipType == _selectedEquipType) item.SetRefinementLevel(detail?.RefinementLv ?? 0);
        }

        private void OnGoodsDetailUpdated(long goodsId)
        {
            if (goodsId != _selectedGoodsId) return;
            GoodsDetailVo detail = GoodsDynamicModel.Instance.Peek(goodsId);
            ApplyDetail(detail);
            RefreshEquipmentRow(detail);
        }

        private void OnRefinementUpdated()
        {
            GoodsDetailVo detail = _selectedGoodsId > 0 ? GoodsDynamicModel.Instance.Peek(_selectedGoodsId) : null;
            ApplyDetail(detail);
            RefreshEquipmentRow(detail);
        }

        private void OnRefineClick()
        {
            if (_selectedGoodsId <= 0 || _selectedEquipType <= 0)
            {
                TipsManager.Toast("请先选择已穿戴装备");
                return;
            }
            GoodsDetailVo detail = GoodsDynamicModel.Instance.Peek(_selectedGoodsId);
            int nextLevel = (detail?.RefinementLv ?? 0) + 1;
            if (detail == null || !EquipConfigs.TryGetRefinementLevel(_selectedEquipType, nextLevel, out EquipConfigs.RefinementLevel cfg))
            {
                TipsManager.Toast("九炼配置未就绪");
                return;
            }
            long have = BagModel.Instance.GetTypeGoodsNum(cfg.MaterialTypeId);
            if (have < cfg.NeedNum)
            {
                TipsManager.Toast("九炼材料不足");
                return;
            }
            EquipRefinementController.Instance.Refine(_selectedGoodsId);
        }

        private void ShowSelectedTips()
        {
            BagGoods worn = _selectedEquipType > 0 ? EquipAutoWear.GetWorn(_selectedEquipType) : null;
            if (worn != null) ItemTipsView.ShowEquipped(worn);
        }

        private void ToggleInstruction()
        {
            if (_gp_instrcution == null) return;
            _gp_instrcution.gameObject.SetActive(!_gp_instrcution.gameObject.activeSelf);
        }

        private static string FormatPromote(int promote) =>
            (promote / 100f).ToString("0.##", CultureInfo.InvariantCulture) + "%";

        private static string FormatBonus(long attrValue, int promote) =>
            (EquipConfigs.CalculateRefinementBonus(attrValue, promote) / 100f)
            .ToString("0.##", CultureInfo.InvariantCulture) + "%";

        private static void HideAttrItem(EquipRefiAttrsItemBind item)
        {
            if (item != null && item.IsShown) item.Hide();
        }

        private static void SetNode(Component component, bool visible)
        {
            if (component != null) component.gameObject.SetActive(visible);
        }

        private static void BindClick(Component target, System.Action onClick)
        {
            if (target == null || onClick == null) return;
            Image image = target as Image;
            if (image == null) image = target.GetComponentInChildren<Image>(true);
            if (image == null) return;
            image.raycastTarget = true;
            UIUtil.AddClick(image, onClick);
        }
    }
}
