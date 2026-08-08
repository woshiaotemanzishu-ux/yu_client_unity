using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Suit;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Equip;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Resonance
{
    /// <summary>
    /// SuitModule 的数据/事件 presenter。所有视觉节点和模板均来自可编辑 Prefab；本类只填数据、绑定点击、
    /// 克隆 Prefab 模板并管理协议/特效生命周期。
    /// </summary>
    public sealed class ResonancePresenter
    {
        private const string CombatPrefab = "FightingShowSmallItem";
        private readonly EquipSuitMianViewBind _view;
        private readonly List<GameObject> _mainClones = new List<GameObject>();
        private readonly List<GameObject> _popupClones = new List<GameObject>();
        private readonly List<GameObject> _mainAddressables = new List<GameObject>();
        private readonly List<GameObject> _popupAddressables = new List<GameObject>();

        private EquipSuitPreviewTipsBind _preview;
        private EquipSuitReturnViewBind _return;
        private GameObject _previewMask;
        private GameObject _returnMask;
        private bool _popupBound;
        private bool _subscribed;
        private int _tabIndex;
        private byte _selectedPosition;
        private int _attrIndex;
        private bool _confirming;
        private int _confirmTab;
        private byte _confirmPosition;
        private string _confirmFingerprint = string.Empty;
        private string _returnFingerprint = string.Empty;
        private int _returnTab;
        private byte _returnPosition;
        private int _loadEpoch;
        private int _renderEpoch;
        private int _effectEpoch;
        private Image _upClickImage;
        private Image _returnClickImage;
        private UIEffectStage.Handle _currentEffect;
        private UIEffectStage.Handle _nextEffect;
        private UIEffectStage.Handle _previewEffect;
        private UIEffectStage.Handle _successEffect;

        public ResonancePresenter(EquipSuitMianViewBind view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            BindMainActions();
        }

        public void Configure(EquipSuitPreviewTipsBind preview, EquipSuitReturnViewBind returnView,
            GameObject previewMask, GameObject returnMask)
        {
            _preview = preview;
            _return = returnView;
            _previewMask = previewMask;
            _returnMask = returnMask;
            BindPopupActions();
        }

        public void Show()
        {
            Subscribe();
            ClosePopups();
            EquipReadController.Instance.RequestSuitInfo();
            BagController.Instance.RequestContainer(BagModel.POS_EQUIP);
            BagController.Instance.RequestContainer(BagModel.POS_BAG);
            int epoch = ++_loadEpoch;
            _ = LoadAndRefresh(epoch);
            Refresh();
        }

        public void Hide()
        {
            Unsubscribe();
            ++_loadEpoch;
            _confirming = false;
            _confirmFingerprint = string.Empty;
            ClosePopups();
            ClearMainVisuals();
            ClearEffects();
        }

        public void Dispose()
        {
            Hide();
            _popupBound = false;
        }

        public void SetTab(int index)
        {
            int next = Mathf.Clamp(index, 0, ResonanceConfigs.Tabs.Length - 1);
            bool changed = next != _tabIndex;
            _tabIndex = next;
            if (changed)
            {
                _selectedPosition = 0;
                _attrIndex = 0;
                _confirming = false;
                ClosePopups();
            }
            Refresh(resetScroll: changed);
        }

        public void ClosePopups()
        {
            ClosePreview();
            CloseReturn();
        }

        private async Task LoadAndRefresh(int epoch)
        {
            await ResonanceConfigs.EnsureLoaded();
            if (epoch != _loadEpoch || !_view.IsShown) return;
            Refresh(resetScroll: true);
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            EventDispatcher.On(GlobalEvent.EVT_EQUIP_SUIT_UPDATE, OnSuitUpdated);
            EventDispatcher.On<EquipReadController.SuitOperationResult>(
                GlobalEvent.EVT_EQUIP_SUIT_OPERATION_RESULT, OnOperationResult);
            EventDispatcher.On(GlobalEvent.EVT_EQUIP_SUIT_RETURN_PREVIEW, OnReturnPreviewUpdated);
            EventDispatcher.On(GlobalEvent.EVT_BAG_UPDATE, OnInventoryUpdated);
            EventDispatcher.On(GlobalEvent.EVT_EQUIPMENT_UPDATE, OnInventoryUpdated);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnInventoryUpdated);
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            EventDispatcher.Off(GlobalEvent.EVT_EQUIP_SUIT_UPDATE, OnSuitUpdated);
            EventDispatcher.Off<EquipReadController.SuitOperationResult>(
                GlobalEvent.EVT_EQUIP_SUIT_OPERATION_RESULT, OnOperationResult);
            EventDispatcher.Off(GlobalEvent.EVT_EQUIP_SUIT_RETURN_PREVIEW, OnReturnPreviewUpdated);
            EventDispatcher.Off(GlobalEvent.EVT_BAG_UPDATE, OnInventoryUpdated);
            EventDispatcher.Off(GlobalEvent.EVT_EQUIPMENT_UPDATE, OnInventoryUpdated);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnInventoryUpdated);
        }

        private void OnSuitUpdated()
        {
            if (_confirming) ClearBuildConfirm();
            Refresh();
            if (_return != null && _return.IsShown) ValidateOpenReturnOrClose();
        }

        private void OnInventoryUpdated()
        {
            if (_confirming) ClearBuildConfirm();
            Refresh();
            if (_return != null && _return.IsShown) ValidateOpenReturnOrClose();
        }

        private void OnReturnPreviewUpdated()
        {
            if (_return != null && _return.IsShown) RenderReturnRewards();
        }

        private void OnOperationResult(EquipReadController.SuitOperationResult result)
        {
            if (result == null) return;
            ClearBuildConfirm();
            bool playBuildSuccess = false;
            if (result.Success)
            {
                if (result.Protocol == Proto.EQUIP_SUIT_BUILD)
                {
                    TipsManager.Toast("共鸣打造成功");
                    playBuildSuccess = true;
                }
                else if (result.Protocol == Proto.EQUIP_SUIT_RETURN)
                {
                    CloseReturn();
                    TipsManager.Toast(result.WasRequested ? "共鸣回退成功" : "装备变更触发共鸣回退，返还物已发放");
                }
            }
            else if (result.WasRequested && result.ErrorCode == -1)
            {
                TipsManager.Toast("共鸣请求超时，请重试");
            }
            Refresh();
            if (playBuildSuccess) _ = PlaySuccessEffect();
        }

        private void Refresh(bool resetScroll = false)
        {
            if (!_view.IsShown) return;
            int epoch = ++_renderEpoch;
            ClearMainClonesOnly();
            ClearPersistentEffects();

            if (!ResonanceConfigs.IsLoaded || !EquipReadModel.Instance.HasSuitInfo)
            {
                SetLoadingState();
                return;
            }

            EnsureSelectedPosition();
            if (_selectedPosition == 0)
            {
                SetLoadingState("当前页没有可用装备部位");
                return;
            }

            ResonanceConfigs.BuildPreview selected = ResonanceConfigs.Preview(
                _tabIndex, _selectedPosition, EquipReadController.Instance.SuitOperationPending);
            RenderPositions(epoch);
            RenderSelectedEquipment(selected, epoch);
            RenderCosts(selected, epoch);
            RenderAttributes(selected, epoch);
            RenderActionState(selected);
            RefreshPersistentEffects(selected, epoch);
            if (resetScroll) ResetScrolls();
        }

        private void EnsureSelectedPosition()
        {
            ResonanceConfigs.TabDefinition tab = ResonanceConfigs.GetTab(_tabIndex);
            IReadOnlyList<byte> positions = ResonanceConfigs.GetPositions(tab.SuitType);
            bool valid = false;
            for (int i = 0; i < positions.Count; i++) if (positions[i] == _selectedPosition) { valid = true; break; }
            if (valid) return;

            _selectedPosition = positions.Count > 0 ? positions[0] : (byte)0;
            for (int i = 0; i < positions.Count; i++)
            {
                ResonanceConfigs.BuildPreview preview = ResonanceConfigs.Preview(_tabIndex, positions[i]);
                if (!preview.CanBuild) continue;
                _selectedPosition = positions[i];
                break;
            }
            ResetAttributeIndexToCurrent();
        }

        private void RenderPositions(int epoch)
        {
            if (_view.posList == null || _view.posList.content == null || _view._tpl_EquipSuitPosItem == null) return;
            ResonanceConfigs.TabDefinition tab = ResonanceConfigs.GetTab(_tabIndex);
            IReadOnlyList<byte> positions = ResonanceConfigs.GetPositions(tab.SuitType);
            for (int i = 0; i < positions.Count; i++)
            {
                byte position = positions[i];
                GameObject go = CloneTemplate(_view._tpl_EquipSuitPosItem, _view.posList.content, _mainClones,
                    "ResonancePosition_" + position);
                EquipSuitPosItemBind bind = go != null ? go.GetComponent<EquipSuitPosItemBind>() : null;
                if (bind == null) continue;
                bind.Show();
                BagGoods equipment = BagModel.Instance.GetEquipmentAt(position);
                ushort level = ResonanceConfigs.GetCurrentLevel(tab.SuitType, tab.SubType, position);
                ResonanceConfigs.SuitItem item = ResonanceConfigs.GetSuitItem(tab.SuitType, tab.SubType, level);
                if (bind.nameLab != null)
                {
                    bind.nameLab.gameObject.SetActive(equipment != null);
                    if (equipment != null)
                    {
                        bind.nameLab.text = CleanGoodsName(GoodsModel.GetGoodsName(equipment.TypeId));
                        Color qualityColor;
                        if (ColorUtility.TryParseHtmlString(
                                EquipmentTipsConfig.GetDarkColor(GoodsModel.GetDisplayColor(equipment.TypeId)),
                                out qualityColor))
                            bind.nameLab.color = qualityColor;
                    }
                }
                ResonanceConfigs.BuildPreview preview = ResonanceConfigs.Preview(_tabIndex, position);
                if (bind.descLab != null)
                {
                    bind.descLab.gameObject.SetActive(equipment != null);
                    if (equipment != null)
                    {
                        bind.descLab.text = level > 0
                            ? (item?.Name ?? ("共鸣 " + level.ToString(CultureInfo.InvariantCulture)))
                            : (preview.CanBuild ? "可打造" : ResonanceConfigs.GetBlockText(preview));
                        bind.descLab.color = preview.CanBuild || level > 0
                            ? new Color32(0x0a, 0x95, 0x3e, 0xff)
                            : new Color32(0xd1, 0x18, 0x18, 0xff);
                    }
                }
                if (bind.numLab != null)
                {
                    bool showLevelCount = equipment != null && level > 0 && item != null;
                    bind.numLab.gameObject.SetActive(showLevelCount);
                    if (showLevelCount)
                    {
                        int activeCount = ResonanceConfigs.GetActiveCount(tab.SuitType, tab.SubType, level);
                        bind.numLab.text = activeCount.ToString(CultureInfo.InvariantCulture) + "/"
                            + item.MaxCount.ToString(CultureInfo.InvariantCulture);
                    }
                }
                if (bind.selectImg != null) bind.selectImg.gameObject.SetActive(position == _selectedPosition);
                if (bind.defImg != null) bind.defImg.gameObject.SetActive(position != _selectedPosition);
                if (bind.bgImg != null) bind.bgImg.gameObject.SetActive(equipment == null);
                if (bind.iconImg != null) bind.iconImg.gameObject.SetActive(equipment == null);
                if (bind.iconBox != null) bind.iconBox.gameObject.SetActive(equipment != null);
                if (bind.redImg != null) bind.redImg.gameObject.SetActive(IsRecommendedBuild(preview));
                if (equipment != null && bind.itemBox != null && bind.iconBox != null)
                {
                    EquipmentItem positionCell = AttachEquipmentCell(
                        _view._tpl_EquipmentItem, bind.iconBox, equipment, true, _mainClones);
                    byte effectTier = ResonanceConfigs.GetPositionEffectTier(position, equipment);
                    if (positionCell != null && effectTier > 0)
                        positionCell.SetSuitEffect(GetEffectName(tab.SuitType, effectTier), effectTier);
                    // 老端 EquipmentItem 挂在 iconBox；itemBox 是覆盖整张 112x200 卡片的唯一点击面。
                    ConfigureDisplayOnlyCell(positionCell, 77f / 127f);
                    BindPositionClick(bind.itemBox, () => SelectPosition(position));
                }
                else
                {
                    // 老端空部位点击只提示先穿装备，不允许把空位切成打造目标。
                    BindPositionClick(bind.itemBox, () => TipsManager.Toast("请先穿戴装备"));
                }
            }
            ForceLayout(_view.posList.content);
        }

        private void SelectPosition(byte position)
        {
            if (_selectedPosition == position) return;
            _selectedPosition = position;
            ResetAttributeIndexToCurrent();
            _confirming = false;
            ClosePopups();
            Refresh();
        }

        private void RenderSelectedEquipment(ResonanceConfigs.BuildPreview preview, int epoch)
        {
            string goodsName = preview.Equipment != null
                ? CleanGoodsName(GoodsModel.GetGoodsName(preview.Equipment.TypeId)) : "未穿戴装备";
            if (_view.nameSLab != null)
            {
                string currentName = preview.CurrentItem?.Name ?? string.Empty;
                _view.nameSLab.text = string.IsNullOrEmpty(currentName)
                    ? goodsName
                    : ColorText(currentName, GetStageColor(preview.Tab)) + " " + goodsName;
            }
            if (_view.nameXLab != null)
            {
                _view.nameXLab.text = preview.IsMax
                    ? ColorText("已满阶", "#00FFEA")
                    : ColorText(preview.NextItem?.Name ?? "下一阶", "#00FFEA") + " "
                        + ColorText(goodsName, "#FEFEB2");
            }
            if (preview.Equipment != null)
            {
                if (_view.iconSBox != null)
                {
                    EquipmentItem current = AttachEquipmentCell(
                        _view._tpl_EquipmentItem, _view.iconSBox, preview.Equipment, true, _mainClones);
                    ConfigureDisplayOnlyCell(current, 85f / 127f);
                }
                if (_view.iconXBox != null && !preview.IsMax)
                {
                    EquipmentItem next = AttachEquipmentCell(
                        _view._tpl_EquipmentItem, _view.iconXBox, preview.Equipment, true, _mainClones);
                    ConfigureDisplayOnlyCell(next, 85f / 127f);
                }
            }

            bool showDescription = !preview.IsMax;
            if (_view.descBox != null) _view.descBox.gameObject.SetActive(showDescription);
            if (_view.descHtml != null)
            {
                bool showCostCaption = preview.Block == ResonanceConfigs.BuildBlock.None
                    || preview.Block == ResonanceConfigs.BuildBlock.MaterialNotEnough
                    || preview.Block == ResonanceConfigs.BuildBlock.OperationPending;
                _view.descHtml.text = showCostCaption
                    ? "打造消耗"
                    : (preview.Block == ResonanceConfigs.BuildBlock.EquipmentCondition && preview.NextMake != null
                        ? BuildEquipmentConditionText(preview.NextMake)
                        : ResonanceConfigs.GetBlockText(preview));
                RectTransform descRect = _view.descHtml.rectTransform;
                if (descRect != null)
                {
                    Vector2 position = descRect.anchoredPosition;
                    position.x = showCostCaption ? 165f : 65f;
                    descRect.anchoredPosition = position;
                    Vector2 size = descRect.sizeDelta;
                    size.x = showCostCaption ? 120f : 320f;
                    descRect.sizeDelta = size;
                }
            }
            if (_view.previewBox != null)
            {
                bool canPreview = preview.Tab.SuitType == 1 && preview.Equipment != null
                    && preview.MaxReachableLevel > preview.CurrentLevel + 1;
                _view.previewBox.gameObject.SetActive(canPreview);
            }
            if (_view._btn_back != null)
                _view._btn_back.gameObject.SetActive(preview.CurrentLevel > 0 && preview.Equipment != null);
            if (_view.maxImg != null) _view.maxImg.gameObject.SetActive(preview.IsMax);
            if (_view.giftIcon != null)
            {
                // 当前 PushGift 仅有全局礼包集合，没有 eGongMing 类型切片/购买页；不可把任意礼包伪装成共鸣礼包。
                _view.giftIcon.gameObject.SetActive(false);
            }
        }

        private void RenderCosts(ResonanceConfigs.BuildPreview preview, int epoch)
        {
            if (_view.costList == null || _view.costList.content == null || _view._tpl_EquipSuitCostItem == null) return;
            IReadOnlyList<ResonanceConfigs.CostItem> costs = preview.Costs;
            for (int i = 0; i < costs.Count; i++)
            {
                ResonanceConfigs.CostItem cost = costs[i];
                GameObject go = CloneTemplate(_view._tpl_EquipSuitCostItem, _view.costList.content, _mainClones,
                    "ResonanceCost_" + cost.TypeId);
                EquipSuitCostItemBind bind = go != null ? go.GetComponent<EquipSuitCostItemBind>() : null;
                if (bind == null) continue;
                bind.Show();
                if (bind.num_text != null)
                {
                    bind.num_text.text = GoodsModel.FormatCountNum(cost.Have) + "/" + GoodsModel.FormatCountNum(cost.Need);
                    bind.num_text.color = cost.Enough
                        ? new Color32(0, 250, 100, 255) : new Color32(250, 77, 77, 255);
                    bind.num_text.fontSize = 18f;
                    bind.num_text.fontStyle = FontStyles.Normal;
                    bind.num_text.outlineColor = Color.black;
                    bind.num_text.outlineWidth = 0.18f;
                }
                if (bind.lockImg != null) bind.lockImg.gameObject.SetActive(false);
                GameObject template = bind._tpl_EquipmentItem != null ? bind._tpl_EquipmentItem : _view._tpl_EquipmentItem;
                if (bind._group_item != null)
                {
                    // 老端 EquipmentItem 本身不显示需求数量，唯一数字层是 num_text 的“拥有/需要”。
                    // 传 need 会让通用格再绘一遍数量，与 num_text 重叠。
                    EquipmentItem material = AttachEquipmentCell(
                        template, bind._group_item, cost.TypeId, 1, false, _mainClones);
                    if (material != null)
                    {
                        long have = Math.Max(cost.Have, 1L);
                        material.SetClickCallBack(() => ItemTipsView.Show(cost.TypeId, have));
                    }
                }
            }
            ForceLayout(_view.costList.content);
        }

        private void RenderAttributes(ResonanceConfigs.BuildPreview preview, int epoch)
        {
            IReadOnlyList<ResonanceConfigs.SuitItem> items = ResonanceConfigs.GetSuitItems(
                preview.Tab.SuitType, preview.Tab.SubType);
            if (items.Count == 0) return;
            _attrIndex = Mathf.Clamp(_attrIndex, 0, items.Count - 1);
            ResonanceConfigs.SuitItem shown = items[_attrIndex];
            int activeCount = ResonanceConfigs.GetActiveCount(preview.Tab.SuitType, preview.Tab.SubType, shown.Level);
            if (_view.nameLab != null) _view.nameLab.text = shown.Name;
            if (_view.barLab != null) _view.barLab.text = "(" + activeCount + "/" + shown.MaxCount + ")";
            if (_view.lImg != null) SetImageEnabled(_view.lImg, _attrIndex > 0);
            if (_view.rImg != null) SetImageEnabled(_view.rImg, _attrIndex < items.Count - 1);

            EquipReadModel.SuitPowerSnapshot power = null;
            bool hasPower = EquipReadModel.Instance.TryGetSuitPower(
                preview.Position, preview.Tab.SubType, shown.Level, out power);
            if (!hasPower)
                EquipReadController.Instance.RequestSuitPower(preview.Position, preview.Tab.SubType, shown.Level);

            if (_view.atrsList == null || _view.atrsList.content == null || _view._tpl_EquipNewSuitAttrItem == null) return;
            for (int i = 0; i < shown.Tiers.Count; i++)
            {
                ResonanceConfigs.AttrTier tier = shown.Tiers[i];
                GameObject go = CloneTemplate(_view._tpl_EquipNewSuitAttrItem, _view.atrsList.content, _mainClones,
                    "ResonanceAttr_" + tier.Count);
                EquipNewSuitAttrItemBind bind = go != null ? go.GetComponent<EquipNewSuitAttrItemBind>() : null;
                if (bind == null) continue;
                bind.Show();
                bool active = activeCount >= tier.Count;
                Color32 attrColor = active
                    ? new Color32(10, 149, 62, 255) : new Color32(103, 103, 103, 255);
                if (bind.numLab != null)
                {
                    bind.numLab.text = tier.Count + "件";
                    bind.numLab.color = attrColor;
                }
                if (bind.attrHtml != null)
                {
                    bind.attrHtml.text = FormatAttributes(tier.Attributes);
                    bind.attrHtml.color = attrColor;
                    bind.attrHtml.fontSize = 20f;
                    bind.attrHtml.lineSpacing = 6f;
                    RectTransform attrRect = bind.attrHtml.rectTransform;
                    if (attrRect != null)
                    {
                        Vector2 size = attrRect.sizeDelta;
                        size.x = 200f;
                        attrRect.sizeDelta = size;
                    }
                }
                ulong combat = FindCombat(power, tier.Count);
                if (bind.combatBox != null && hasPower)
                    _ = AttachCombatAsync(bind.combatBox, combat, epoch, _mainAddressables);
            }
            ForceLayout(_view.atrsList.content);
        }

        private void RenderActionState(ResonanceConfigs.BuildPreview preview)
        {
            bool canBuild = preview.CanBuild && !_confirming;
            if (_upClickImage != null)
                _upClickImage.color = canBuild ? Color.white : new Color32(150, 150, 150, 255);
            if (_view.redImg != null) _view.redImg.gameObject.SetActive(IsRecommendedBuild(preview));
        }

        private void SetLoadingState(string text = "共鸣数据加载中")
        {
            if (_view.nameLab != null) _view.nameLab.text = text;
            if (_view.barLab != null) _view.barLab.text = string.Empty;
            if (_view.nameSLab != null) _view.nameSLab.text = string.Empty;
            if (_view.nameXLab != null) _view.nameXLab.text = string.Empty;
            if (_view.descHtml != null) _view.descHtml.text = text;
            if (_view.redImg != null) _view.redImg.gameObject.SetActive(false);
            if (_view.giftIcon != null) _view.giftIcon.gameObject.SetActive(false);
        }

        private void ResetAttributeIndexToCurrent()
        {
            ResonanceConfigs.TabDefinition tab = ResonanceConfigs.GetTab(_tabIndex);
            ushort current = ResonanceConfigs.GetCurrentLevel(tab.SuitType, tab.SubType, _selectedPosition);
            IReadOnlyList<ResonanceConfigs.SuitItem> items = ResonanceConfigs.GetSuitItems(tab.SuitType, tab.SubType);
            _attrIndex = 0;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Level != current) continue;
                _attrIndex = i;
                break;
            }
        }

        private void BrowseAttributes(int direction)
        {
            ResonanceConfigs.TabDefinition tab = ResonanceConfigs.GetTab(_tabIndex);
            IReadOnlyList<ResonanceConfigs.SuitItem> items = ResonanceConfigs.GetSuitItems(tab.SuitType, tab.SubType);
            int next = _attrIndex + direction;
            if (next < 0)
            {
                TipsManager.Toast("已经是最低阶");
                return;
            }
            if (next >= items.Count)
            {
                TipsManager.Toast("已经是最高阶");
                return;
            }
            _attrIndex = next;
            Refresh();
        }

        private void OnBuildClick()
        {
            if (_confirming || EquipReadController.Instance.SuitOperationPending) return;
            ResonanceConfigs.BuildPreview preview = ResonanceConfigs.Preview(_tabIndex, _selectedPosition);
            if (!preview.CanBuild)
            {
                TipsManager.Toast(ResonanceConfigs.GetBlockText(preview));
                Refresh();
                return;
            }

            _confirming = true;
            _confirmTab = _tabIndex;
            _confirmPosition = _selectedPosition;
            _confirmFingerprint = preview.Fingerprint;
            string message = BuildConfirmText(preview);
            if (!IsRecommendedBuild(preview))
                message += "\n当前操作可能使同阶套装件数下降，建议成组打造。是否继续？";
            Refresh();
            TipsManager.Confirm(message, ConfirmBuild, CancelBuild);
        }

        private void ConfirmBuild()
        {
            if (!_confirming || EquipReadController.Instance.SuitOperationPending) return;
            ResonanceConfigs.BuildPreview preview = ResonanceConfigs.Preview(_confirmTab, _confirmPosition);
            bool valid = _tabIndex == _confirmTab && _selectedPosition == _confirmPosition
                && preview.CanBuild && preview.Fingerprint == _confirmFingerprint;
            byte makeType = preview.Tab.SubType;
            byte position = preview.Position;
            ClearBuildConfirm();
            if (!valid)
            {
                TipsManager.Toast("装备、材料或共鸣状态已变化，请重新确认");
                Refresh();
                return;
            }
            if (!EquipReadController.Instance.TryRequestSuitBuild(makeType, position))
                TipsManager.Toast("已有共鸣操作处理中");
            Refresh();
        }

        private void CancelBuild()
        {
            ClearBuildConfirm();
            Refresh();
        }

        private void ClearBuildConfirm()
        {
            _confirming = false;
            _confirmTab = 0;
            _confirmPosition = 0;
            _confirmFingerprint = string.Empty;
        }

        private static string BuildConfirmText(ResonanceConfigs.BuildPreview preview)
        {
            var builder = new StringBuilder("是否消耗以下材料进行共鸣打造？");
            for (int i = 0; i < preview.Costs.Count; i++)
            {
                ResonanceConfigs.CostItem cost = preview.Costs[i];
                string name = GoodsModel.GetGoodsName(cost.TypeId);
                builder.Append('\n').Append(string.IsNullOrEmpty(name) ? cost.TypeId.ToString() : name)
                    .Append('×').Append(cost.Need);
            }
            return builder.ToString();
        }

        private bool IsRecommendedBuild(ResonanceConfigs.BuildPreview preview)
        {
            if (preview == null || !preview.CanBuild) return false;
            if (preview.CurrentLevel == 0) return true;
            ResonanceConfigs.SuitItem current = ResonanceConfigs.GetSuitItem(
                preview.Tab.SuitType, preview.Tab.SubType, preview.CurrentLevel);
            int currentCount = ResonanceConfigs.GetActiveCount(
                preview.Tab.SuitType, preview.Tab.SubType, preview.CurrentLevel);
            if (current?.Tiers != null && current.Tiers.Count > 0 && currentCount < current.Tiers[0].Count) return true;
            int nextCount = ResonanceConfigs.GetActiveCount(
                preview.Tab.SuitType, preview.Tab.SubType, unchecked((ushort)(preview.CurrentLevel + 1))) + 1;
            return nextCount % 2 == 0;
        }

        private void ShowPreview()
        {
            ResonanceConfigs.BuildPreview state = ResonanceConfigs.Preview(_tabIndex, _selectedPosition);
            if (_preview == null || _previewMask == null || state.Equipment == null
                || state.MaxReachableLevel <= state.CurrentLevel + 1)
            {
                TipsManager.Toast("当前装备没有更高阶特效可预览");
                return;
            }
            CloseReturn();
            ClearPopupVisuals(closeViews: false);
            _previewMask.transform.SetAsLastSibling();
            _previewMask.SetActive(true);
            _preview.Show();
            if (_preview.descLab != null)
            {
                ResonanceConfigs.SuitItem item = ResonanceConfigs.GetSuitItem(
                    state.Tab.SuitType, state.Tab.SubType, state.MaxReachableLevel);
                _preview.descLab.text = "打造至" + (item?.Name ?? (state.MaxReachableLevel + "阶")) + "可激活炫酷特效";
            }
            if (_preview.iconBox != null)
                AttachEquipmentCell(_preview._tpl_EquipmentItem ?? _view._tpl_EquipmentItem,
                    _preview.iconBox, state.Equipment, true, _popupClones);
            if (_preview.effBox != null)
            {
                _preview.effBox.gameObject.SetActive(true);
                _ = AttachPreviewEffect(GetEffectName(state.Tab), _preview.effBox, GetEffectScale(state.Tab.SubType));
            }
        }

        private void ClosePreview()
        {
            _previewEffect?.Dispose();
            _previewEffect = null;
            if (_preview != null && _preview.IsShown) _preview.Hide();
            if (_previewMask != null) _previewMask.SetActive(false);
            ClearPopupCollections(_popupClones, _popupAddressables);
        }

        private void ShowReturn()
        {
            ResonanceConfigs.BuildPreview state = ResonanceConfigs.Preview(_tabIndex, _selectedPosition);
            if (_return == null || _returnMask == null || state.CurrentLevel == 0 || state.Equipment == null)
            {
                TipsManager.Toast("当前部位没有可回退的共鸣");
                return;
            }
            ClosePreview();
            ClearPopupCollections(_popupClones, _popupAddressables);
            _returnTab = _tabIndex;
            _returnPosition = _selectedPosition;
            _returnFingerprint = ResonanceConfigs.BuildReturnFingerprint(_returnTab, _returnPosition);
            _returnMask.transform.SetAsLastSibling();
            _returnMask.SetActive(true);
            _return.Show();

            ResonanceConfigs.SuitItem target = ResonanceConfigs.GetSuitItem(
                state.Tab.SuitType, state.Tab.SubType, unchecked((ushort)Math.Max(state.CurrentLevel - 1, 0)));
            if (_return._lb_name != null) _return._lb_name.text = CleanGoodsName(GoodsModel.GetGoodsName(state.Equipment.TypeId));
            if (_return._lb_desc != null)
                _return._lb_desc.text = "共鸣回退至" + (target?.Name ?? "无") + "，并100%返还本级打造材料，是否确定？";
            int count = ResonanceConfigs.GetActiveCount(state.Tab.SuitType, state.Tab.SubType, state.CurrentLevel);
            if (_return._lb_tips != null)
                _return._lb_tips.text = count > 0 ? count + "件加成生效中" : "暂无加成效果";
            if (_return._gp_award != null)
                AttachEquipmentCell(_return._tpl_EquipmentItem ?? _view._tpl_EquipmentItem,
                    _return._gp_award, state.Equipment, true, _popupClones);
            if (_return._Label2 != null) _return._Label2.text = "返还材料加载中";
            SetReturnPendingVisual(false);
            EquipReadController.Instance.RequestSuitReturnPreview(state.Tab.SubType, state.Position);
            RenderReturnRewards();
        }

        private void RenderReturnRewards()
        {
            if (_return == null || !_return.IsShown) return;
            ClearReturnRewardClones();
            ResonanceConfigs.TabDefinition tab = ResonanceConfigs.GetTab(_returnTab);
            if (!EquipReadModel.Instance.TryGetReturnPreview(_returnPosition, tab.SubType,
                    out EquipReadModel.SuitReturnPreview snapshot))
            {
                if (_return._Label2 != null) _return._Label2.text = "返还材料加载中";
                return;
            }
            if (_return._Label2 != null) _return._Label2.text = snapshot.Rewards.Count > 0 ? "返还材料" : "本级无返还材料";
            for (int i = 0; i < snapshot.Rewards.Count; i++)
            {
                EquipReadModel.RewardEntry reward = snapshot.Rewards[i];
                (int typeId, int locked) = GoodsModel.GetMappingTypeId(reward.Type, unchecked((int)reward.Id));
                if (typeId <= 0) typeId = unchecked((int)reward.Id);
                RectTransform parent = _return.Content != null ? _return.Content : _return._gp_award;
                EquipmentItem cell = AttachEquipmentCell(_return._tpl_EquipmentItem ?? _view._tpl_EquipmentItem,
                    parent, typeId, reward.Num, locked != 0 || RewardIsBound(reward), _popupClones);
                if (cell != null) cell.gameObject.name = "ReturnReward_" + i;
            }
            ForceLayout(_return.Content);
        }

        private void OnReturnConfirm()
        {
            if (_return == null || !_return.IsShown || EquipReadController.Instance.SuitOperationPending) return;
            string current = ResonanceConfigs.BuildReturnFingerprint(_returnTab, _returnPosition);
            ResonanceConfigs.TabDefinition tab = ResonanceConfigs.GetTab(_returnTab);
            ushort level = ResonanceConfigs.GetCurrentLevel(tab.SuitType, tab.SubType, _returnPosition);
            if (current != _returnFingerprint || level == 0)
            {
                TipsManager.Toast("装备或共鸣状态已变化，请重新打开回退确认");
                CloseReturn();
                Refresh();
                return;
            }
            if (!EquipReadController.Instance.TryRequestSuitReturn(tab.SubType, _returnPosition))
            {
                TipsManager.Toast("已有共鸣操作处理中");
                return;
            }
            SetReturnPendingVisual(true);
            Refresh();
        }

        private void ValidateOpenReturnOrClose()
        {
            if (_return == null || !_return.IsShown) return;
            if (ResonanceConfigs.BuildReturnFingerprint(_returnTab, _returnPosition) == _returnFingerprint) return;
            CloseReturn();
        }

        private void CloseReturn()
        {
            if (_return != null && _return.IsShown) _return.Hide();
            if (_returnMask != null) _returnMask.SetActive(false);
            _returnFingerprint = string.Empty;
            _returnPosition = 0;
            ClearPopupCollections(_popupClones, _popupAddressables);
        }

        private void SetReturnPendingVisual(bool pending)
        {
            if (_returnClickImage != null)
                _returnClickImage.color = pending ? new Color32(150, 150, 150, 255) : Color.white;
            if (_return != null && _return._Label4 != null)
                _return._Label4.text = pending ? "处理中" : "回退";
        }

        private void BindMainActions()
        {
            _upClickImage = BindClick(_view.upBtn, OnBuildClick);
            BindClick(_view._btn_back, ShowReturn);
            BindClick(_view.previewBox, ShowPreview);
            // 老端 InstructionType.EquipSuit=1524；453 是人物属性说明，不能串用。
            BindClick(_view.infoBox, () => InstructionFlow.Show(1524));
            BindImage(_view.lImg, () => BrowseAttributes(-1));
            BindImage(_view.rImg, () => BrowseAttributes(1));
        }

        private void BindPopupActions()
        {
            if (_popupBound || _preview == null || _return == null || _previewMask == null || _returnMask == null) return;
            _popupBound = true;
            // 两个生成 Bind 在独立程序集；用公开生命周期做一次初始化，再恢复隐藏态。
            if (!_preview.IsInitialized) _preview.Show();
            if (_preview.IsShown) _preview.Hide();
            if (!_return.IsInitialized) _return.Show();
            if (_return.IsShown) _return.Hide();
            BindClick(_preview.closeBtn, ClosePreview);
            BindImage(FindClickImage(_previewMask.transform as RectTransform), ClosePreview);
            BindClick(_return._gp_cancel, CloseReturn);
            _returnClickImage = BindClick(_return._gp_return, OnReturnConfirm);
            BindImage(_return._img_close, CloseReturn);
            BindImage(FindClickImage(_returnMask.transform as RectTransform), CloseReturn);
            _preview.gameObject.SetActive(false);
            _return.gameObject.SetActive(false);
            _previewMask.SetActive(false);
            _returnMask.SetActive(false);
        }

        private void RefreshPersistentEffects(ResonanceConfigs.BuildPreview preview, int renderEpoch)
        {
            int effectEpoch = ++_effectEpoch;
            string effectName = GetEffectName(preview.Tab);
            Vector3 scale = GetEffectScale(preview.Tab.SubType);
            if (_view.effBox1 != null)
            {
                bool show = preview.CurrentLevel > 0 && preview.Equipment != null;
                _view.effBox1.gameObject.SetActive(show);
                if (show) _ = AttachPersistentEffect(effectName, _view.effBox1, scale, true, effectEpoch, renderEpoch);
            }
            if (_view.effBox2 != null)
            {
                bool show = preview.NextMake != null && preview.Equipment != null
                    && ResonanceConfigs.MeetsEquipmentCondition(preview.Equipment, preview.NextMake, out _);
                _view.effBox2.gameObject.SetActive(show);
                if (show) _ = AttachPersistentEffect(effectName, _view.effBox2, scale, false, effectEpoch, renderEpoch);
            }
        }

        private async Task AttachPersistentEffect(string name, RectTransform host, Vector3 scale,
            bool current, int effectEpoch, int renderEpoch)
        {
            UIEffectStage.Handle handle = await UIEffectStage.AddAsync(name, host, Vector2.zero, scale);
            if (effectEpoch != _effectEpoch || renderEpoch != _renderEpoch || !_view.IsShown)
            {
                handle?.Dispose();
                return;
            }
            if (current) _currentEffect = handle;
            else _nextEffect = handle;
        }

        private async Task AttachPreviewEffect(string name, RectTransform host, Vector3 scale)
        {
            int epoch = ++_effectEpoch;
            UIEffectStage.Handle handle = await UIEffectStage.AddAsync(name, host, Vector2.zero, scale);
            if (epoch != _effectEpoch || _preview == null || !_preview.IsShown)
            {
                handle?.Dispose();
                return;
            }
            _previewEffect = handle;
        }

        private async Task PlaySuccessEffect()
        {
            if (_view._group_eff == null || !_view.IsShown) return;
            _successEffect?.Dispose();
            _successEffect = null;
            _view._group_eff.gameObject.SetActive(true);
            int epoch = ++_effectEpoch;
            UIEffectStage.Handle handle = await UIEffectStage.AddAsync(
                "ui_gongmingchenggong", _view._group_eff, Vector2.zero, Vector3.one);
            if (epoch != _effectEpoch || !_view.IsShown)
            {
                handle?.Dispose();
                return;
            }
            _successEffect = handle;
            int duration = handle != null
                ? Mathf.CeilToInt(Mathf.Max(1f, handle.LongestLegacyAnimationSeconds + 0.1f) * 1000f) : 1200;
            await TimeUtil.Delay(duration);
            if (epoch != _effectEpoch || _successEffect != handle) return;
            handle?.Dispose();
            _successEffect = null;
            if (_view._group_eff != null) _view._group_eff.gameObject.SetActive(false);
        }

        private void ClearPersistentEffects()
        {
            _effectEpoch++;
            _currentEffect?.Dispose();
            _nextEffect?.Dispose();
            _currentEffect = null;
            _nextEffect = null;
        }

        private void ClearEffects()
        {
            ClearPersistentEffects();
            _previewEffect?.Dispose();
            _successEffect?.Dispose();
            _previewEffect = null;
            _successEffect = null;
            if (_view._group_eff != null) _view._group_eff.gameObject.SetActive(false);
        }

        private async Task AttachCombatAsync(RectTransform parent, ulong combat, int epoch, List<GameObject> collection)
        {
            GameObject go = await ResManager.InstantiateAsync(
                GameResPath.GetUIPrefab("common", CombatPrefab), parent);
            if (go == null) return;
            if (epoch != _renderEpoch || !_view.IsShown)
            {
                ResManager.ReleaseInstance(go);
                return;
            }
            collection.Add(go);
            FightingShowSmallItem item = go.GetComponent<FightingShowSmallItem>();
            if (item == null) item = go.GetComponentInChildren<FightingShowSmallItem>(true);
            if (item != null)
            {
                item.Show();
                item.SetFighting(combat > long.MaxValue ? long.MaxValue : unchecked((long)combat));
                item.SetFightingUp(0);
            }
        }

        private static ulong FindCombat(EquipReadModel.SuitPowerSnapshot snapshot, int count)
        {
            if (snapshot?.Entries == null) return 0;
            for (int i = 0; i < snapshot.Entries.Count; i++)
                if (snapshot.Entries[i].Num == count) return snapshot.Entries[i].Combat;
            return 0;
        }

        private static string FormatAttributes(IReadOnlyList<ResonanceConfigs.AttrValue> attrs)
        {
            if (attrs == null || attrs.Count == 0) return string.Empty;
            var builder = new StringBuilder();
            for (int i = 0; i < attrs.Count; i++)
            {
                ResonanceConfigs.AttrValue attr = attrs[i];
                string name = GoodsModel.GetAttrName(attr.AttrId);
                if (string.IsNullOrEmpty(name)) name = "属性" + attr.AttrId;
                if (builder.Length > 0) builder.Append('\n');
                builder.Append(name).Append('：').Append(GoodsModel.FormatAttrValue(attr.AttrId, attr.Value));
            }
            return builder.ToString();
        }

        private static string ColorText(string text, string color)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return "<color=" + color + ">" + text + "</color>";
        }

        private static string GetStageColor(ResonanceConfigs.TabDefinition tab)
        {
            if (tab.SuitType == 2) return "#F5B328";
            switch (tab.SubType)
            {
                case 1: return "#1D85DD";
                case 2: return "#C41DDD";
                case 3: return "#FF604C";
                default: return "#00FFEA";
            }
        }

        private static string BuildEquipmentConditionText(ResonanceConfigs.MakeItem make)
        {
            return "穿戴" + ColorText(ChineseNumber(make.NeedStage) + "阶", "#3CAD66")
                + ColorText(GetColorName(make.NeedColor), "#CD9222")
                + ColorText(ChineseNumber(make.NeedStar) + "星", "#CD9222") + "装备可打造";
        }

        private static string GetColorName(int color)
        {
            switch (color)
            {
                case 0: return "白色";
                case 1: return "绿色";
                case 2: return "蓝色";
                case 3: return "紫色";
                case 4: return "橙色";
                case 5: return "红色";
                case 6: return "暗金色";
                case 7: return "粉色";
                default: return "无色";
            }
        }

        private static string ChineseNumber(int value)
        {
            string[] digits = { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九", "十" };
            if (value >= 0 && value <= 10) return digits[value];
            if (value < 20) return "十" + digits[value - 10];
            if (value < 100)
            {
                int tens = value / 10;
                int ones = value % 10;
                return digits[tens] + "十" + (ones == 0 ? string.Empty : digits[ones]);
            }
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string GetEffectName(ResonanceConfigs.TabDefinition tab)
            => GetEffectName(tab.SuitType, tab.SubType);

        private static string GetEffectName(byte suitType, byte subType)
            => ResonanceConfigs.GetEffectName(suitType, subType);

        private static Vector3 GetEffectScale(byte subType)
        {
            // 页面中央当前/下一阶与预览属于页面展示链，不是 EquipmentItem 槽位流光。
            // 旧端 scale=10 不能机械复制到当前转换资源/页面宿主；用户截图已证明会生成错误贴身框。
            switch (subType)
            {
                case 2: return new Vector3(1.2f, 1f, 1f);
                case 3: return Vector3.one * 1.2f;
                default: return Vector3.one;
            }
        }

        private EquipmentItem AttachEquipmentCell(GameObject template, RectTransform parent, BagGoods equipment,
            bool equipped, List<GameObject> collection)
        {
            if (equipment == null) return null;
            EquipmentItem item = AttachEquipmentCell(template, parent, equipment.TypeId,
                Math.Max(equipment.GoodsNum, 1L), false, collection);
            if (item != null)
            {
                // 同账号老 H5 的四个共鸣页签都把已穿装备绘制为 plate_4；
                // equipment.Color 仍保留配置品质（当前为 7）参与打造门槛，不能为视觉对齐改写业务值。
                item.SetDisplayColor(4);
                item.SetClickCallBack(() =>
                {
                    if (equipped) ItemTipsView.ShowEquipped(equipment);
                    else ItemTipsView.Show(equipment.TypeId, Math.Max(equipment.GoodsNum, 1L));
                });
            }
            return item;
        }

        private static EquipmentItem AttachEquipmentCell(GameObject template, RectTransform parent, int typeId,
            long count, bool locked, List<GameObject> collection)
        {
            if (template == null || parent == null || typeId <= 0) return null;
            GameObject go = CloneTemplate(template, parent, collection, "EquipmentItem_" + typeId);
            EquipmentItem item = go != null ? go.GetComponent<EquipmentItem>() : null;
            if (item == null && go != null) item = go.GetComponentInChildren<EquipmentItem>(true);
            if (item == null) return null;
            RectTransform rect = item.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.localRotation = Quaternion.identity;
            }
            item.Show();
            item.SetData(typeId, count, locked, false);
            item.SetScale(0.68f);
            ConfigureEquipmentDecorations(item, typeId);
            item.SetClickCallBack(() => ItemTipsView.Show(typeId, count));
            return item;
        }

        private static GameObject CloneTemplate(GameObject template, Transform parent, List<GameObject> collection, string name)
        {
            if (template == null || parent == null) return null;
            GameObject go = UnityEngine.Object.Instantiate(template, parent, false);
            go.name = name;
            go.SetActive(true);
            collection.Add(go);
            return go;
        }

        private void ClearMainVisuals()
        {
            ++_renderEpoch;
            ClearMainClonesOnly();
        }

        private void ClearMainClonesOnly()
        {
            ClearPopupCollections(_mainClones, _mainAddressables);
        }

        private void ClearPopupVisuals(bool closeViews)
        {
            _previewEffect?.Dispose();
            _previewEffect = null;
            if (closeViews)
            {
                if (_preview != null && _preview.IsShown) _preview.Hide();
                if (_return != null && _return.IsShown) _return.Hide();
            }
            ClearPopupCollections(_popupClones, _popupAddressables);
        }

        private void ClearReturnRewardClones()
        {
            for (int i = _popupClones.Count - 1; i >= 0; i--)
            {
                GameObject go = _popupClones[i];
                if (go == null || !go.name.StartsWith("ReturnReward_", StringComparison.Ordinal)) continue;
                _popupClones.RemoveAt(i);
                DestroyClone(go);
            }
        }

        private static void ClearPopupCollections(List<GameObject> clones, List<GameObject> addressables)
        {
            for (int i = 0; i < clones.Count; i++) if (clones[i] != null) DestroyClone(clones[i]);
            clones.Clear();
            for (int i = 0; i < addressables.Count; i++) if (addressables[i] != null) ResManager.ReleaseInstance(addressables[i]);
            addressables.Clear();
        }

        private static void DestroyClone(GameObject go)
        {
            if (go == null) return;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // PointerClick 内同步 Refresh 时，DestroyImmediate 会在 ExecuteHierarchy 退栈前
                // 销毁当前命中链。先退场、下一 Editor tick 再销毁，等价于 Player 的帧末 Destroy。
                go.SetActive(false);
                GameObject pending = go;
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (pending != null) UnityEngine.Object.DestroyImmediate(pending);
                };
                return;
            }
#endif
            UnityEngine.Object.Destroy(go);
        }

        private void ResetScrolls()
        {
            ResetScroll(_view.posList, true);
            ResetScroll(_view.atrsList, false);
            ResetScroll(_view.costList, false);
        }

        private static void ResetScroll(ScrollRect scroll, bool vertical)
        {
            if (scroll == null) return;
            scroll.StopMovement();
            if (vertical) scroll.verticalNormalizedPosition = 1f;
            else scroll.horizontalNormalizedPosition = 0f;
        }

        private static void ForceLayout(RectTransform content)
        {
            if (content == null) return;
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }

        private static void SetImageEnabled(Image image, bool enabled)
        {
            if (image == null) return;
            image.color = enabled ? Color.white : new Color32(105, 105, 105, 255);
        }

        private static Image BindClick(RectTransform root, Action action)
        {
            Image image = FindClickImage(root);
            BindImage(image, action);
            return image;
        }

        private static Image FindClickImage(RectTransform root)
        {
            if (root == null) return null;
            Image own = root.GetComponent<Image>();
            if (own != null) return own;
            return root.GetComponentInChildren<Image>(true);
        }

        private static void BindImage(Image image, Action action)
        {
            if (image == null || action == null) return;
            image.raycastTarget = true;
            UIUtil.AddClick(image, action);
        }

        private static void ConfigureDisplayOnlyCell(EquipmentItem item, float scale)
        {
            if (item == null) return;
            item.SetScale(scale);
            item.SetClickCallBack(null);
            if (item.click_group == null) return;
            Image clickImage = item.click_group.GetComponent<Image>();
            if (clickImage == null) clickImage = item.click_group.GetComponentInChildren<Image>(true);
            if (clickImage == null) return;
            UIUtil.ClearClicks(clickImage);
            clickImage.raycastTarget = false;
        }

        private static void BindPositionClick(RectTransform root, Action action)
        {
            if (root == null || action == null) return;
            Image own = root.GetComponent<Image>();
            if (own == null)
            {
                own = root.gameObject.AddComponent<Image>();
                own.color = Color.clear;
            }
            foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = graphic == own;
            UIUtil.ClearClicks(own);
            own.raycastTarget = true;
            UIUtil.AddClick(own, action);
        }

        private static void ConfigureEquipmentDecorations(EquipmentItem item, int typeId)
        {
            if (item == null) return;
            GoodsModel.EquipAttr attr = GoodsModel.GetEquipAttr(typeId);
            int stage = attr?.Stage ?? 0;
            int stars = Mathf.Clamp(attr?.Star ?? 0, 0, 4);
            if (item.grade != null)
            {
                item.grade.gameObject.SetActive(stage > 0);
                item.grade.text = stage > 0
                    ? stage.ToString(CultureInfo.InvariantCulture) + "阶" : string.Empty;
            }
            if (item._img_grade_bg != null) item._img_grade_bg.gameObject.SetActive(false);
            if (item.star_group != null) item.star_group.gameObject.SetActive(stars > 0);
            Image[] starImages = { item.star_0, item.star_1, item.star_2, item.star_3 };
            for (int i = 0; i < starImages.Length; i++)
                if (starImages[i] != null) starImages[i].gameObject.SetActive(i < stars);
        }

        private static bool RewardIsBound(EquipReadModel.RewardEntry reward)
        {
            return reward != null && reward.Type == 101
                && !string.IsNullOrEmpty(reward.AttrList)
                && reward.AttrList.IndexOf("bind", StringComparison.OrdinalIgnoreCase) >= 0
                && reward.AttrList.IndexOf("1", StringComparison.Ordinal) >= 0;
        }

        private static string CleanGoodsName(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("(段)", string.Empty).Replace("（段）", string.Empty);
        }
    }
}
