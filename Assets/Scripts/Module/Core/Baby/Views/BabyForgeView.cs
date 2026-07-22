using System.Collections.Generic;
using System.Text;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Baby;
using Shenxiao.Module.Core.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Baby
{
    /// <summary>Forge child panel. Only its lv/stage buttons may send 18219 after a locally validated confirmation.</summary>
    public sealed class BabyForgeView : BabyForgeViewBind
    {
        public enum ForgeMode { Empty, Level, Stage, Max }
        private Transform _levelGroup, _stageGroup, _maxGroup, _levelCosts, _stageCosts, _targetGroup, _effectGroup, _targetEffectGroup;
        private Image _levelExp;
        private Transform _levelBtn, _stageBtn;
        private TextMeshProUGUI _levelLabel, _nextLevelLabel, _levelExpLabel;
        private GameObject _awardTemplate;
        private readonly List<GameObject> _renderedCosts = new List<GameObject>();
        private bool _confirming, _pending, _buttonsBound;
        private int _stateVersion, _confirmVersion, _confirmPosition;
        private long _confirmGoodsId;
        private ForgeMode _confirmMode;
        private string _confirmFingerprint = string.Empty;

        public int PositionId { get; private set; }
        public ForgeMode Mode { get; private set; }
        public int RenderedCostCount => _renderedCosts.Count;
        public bool Pending => _pending;
        public bool LevelInteractable => _levelBtn != null && _levelBtn.GetComponent<Button>() != null && _levelBtn.GetComponent<Button>().interactable;
        public bool StageInteractable => _stageBtn != null && _stageBtn.GetComponent<Button>() != null && _stageBtn.GetComponent<Button>().interactable;
        public string LastConfirmText { get; private set; } = string.Empty;

        public void SetPositionId(int positionId) { PositionId = positionId >= 1 && positionId <= 6 ? positionId : 0; Refresh(); }

        public void Refresh()
        {
            _stateVersion++;
            CacheNodes(); ClearRenderedCosts(); SetMode(ForgeMode.Empty); HideEffects();
            BabyEquipEntry entry = FindEntry(PositionId);
            if (entry == null || entry.Id <= 0) { RefreshButtons(); return; }
            BabyEquipUpgradeConfigs.StrenCfg next = BabyEquipUpgradeConfigs.GetStren(entry.PositionId, entry.Stage, entry.StageLevel + 1);
            BabyEquipUpgradeConfigs.PreviewResult preview = BabyEquipUpgradeConfigs.Preview(entry);
            if (next != null)
            {
                SetMode(ForgeMode.Level);
                if (_levelLabel != null) _levelLabel.text = entry.StageLevel + "级";
                if (_nextLevelLabel != null) _nextLevelLabel.text = next.StageLevel + "级";
                if (_levelExpLabel != null) _levelExpLabel.text = entry.StageExp + "/" + next.PointCon;
                if (_levelExp != null) _levelExp.fillAmount = next.PointCon > 0 ? Mathf.Clamp01((float)entry.StageExp / next.PointCon) : 0f;
                RenderCosts(_levelCosts, preview);
            }
            else if (preview != null && preview.IsStageUpgrade) { SetMode(ForgeMode.Stage); RenderCosts(_stageCosts, preview); }
            else SetMode(ForgeMode.Max);
            RefreshButtons();
        }

        public void OnUpgradeResult()
        {
            _stateVersion++;
            ClearConfirmState();
            _pending = false;
            Refresh();
        }

        public void Clear()
        {
            _stateVersion++;
            CacheNodes(); PositionId = 0; ClearConfirmState(); _pending = false; LastConfirmText = string.Empty;
            ClearRenderedCosts(); SetMode(ForgeMode.Empty); HideEffects(); RefreshButtons();
            ClearButton(_levelBtn);
            ClearButton(_stageBtn);
            _buttonsBound = false;
        }

        protected override void OnHide() => Clear();
        protected override void OnDispose()
        {
            Clear();
        }

        private void TryUpgrade(ForgeMode expectedMode)
        {
            if (_confirming || _pending || Mode != expectedMode) return;
            BabyEquipEntry entry = FindEntry(PositionId);
            BabyEquipUpgradeConfigs.PreviewResult preview = entry != null && entry.Id > 0 ? BabyEquipUpgradeConfigs.Preview(entry) : null;
            if (!CanUpgrade(expectedMode, entry, preview)) { RefreshButtons(); return; }
            _confirming = true;
            _confirmVersion = _stateVersion;
            _confirmPosition = PositionId;
            _confirmGoodsId = entry.Id;
            _confirmMode = expectedMode;
            _confirmFingerprint = BuildFingerprint(preview);
            LastConfirmText = BuildConfirmText(preview);
            RefreshButtons();
            TipsManager.Confirm(LastConfirmText, ConfirmUpgrade, CancelUpgrade);
        }

        private void ConfirmUpgrade()
        {
            if (_pending || !_confirming) return;
            if (_confirmVersion != _stateVersion) { ClearConfirmState(); RefreshButtons(); return; }
            int position = _confirmPosition;
            BabyEquipEntry entry = FindEntry(position);
            BabyEquipUpgradeConfigs.PreviewResult preview = entry != null && entry.Id > 0 ? BabyEquipUpgradeConfigs.Preview(entry) : null;
            bool valid = PositionId == position && entry != null && entry.Id == _confirmGoodsId
                && MatchesUpgrade(_confirmMode, entry, preview) && BuildFingerprint(preview) == _confirmFingerprint;
            ClearConfirmState();
            if (!valid) { RefreshButtons(); return; }
            _pending = true;
            RefreshButtons();
            BabyController.Instance.RequestEquipUpgrade(position);
        }

        private void CancelUpgrade() { if (!_confirming) return; ClearConfirmState(); RefreshButtons(); }
        private void ClearConfirmState() { _confirming = false; _confirmVersion = 0; _confirmPosition = 0; _confirmGoodsId = 0; _confirmMode = ForgeMode.Empty; _confirmFingerprint = string.Empty; }
        private bool CanUpgrade(ForgeMode expected, BabyEquipEntry entry, BabyEquipUpgradeConfigs.PreviewResult preview)
            => !_confirming && !_pending && MatchesUpgrade(expected, entry, preview);
        private bool MatchesUpgrade(ForgeMode expected, BabyEquipEntry entry, BabyEquipUpgradeConfigs.PreviewResult preview)
            => entry != null && entry.Id > 0 && preview != null && preview.Enough
                && Mode == expected && ((expected == ForgeMode.Level && !preview.IsStageUpgrade) || (expected == ForgeMode.Stage && preview.IsStageUpgrade));
        private void RefreshButtons()
        {
            BabyEquipEntry entry = FindEntry(PositionId);
            BabyEquipUpgradeConfigs.PreviewResult preview = entry != null && entry.Id > 0 ? BabyEquipUpgradeConfigs.Preview(entry) : null;
            Button level = _levelBtn != null ? _levelBtn.GetComponent<Button>() : null;
            Button stage = _stageBtn != null ? _stageBtn.GetComponent<Button>() : null;
            if (level != null) level.interactable = CanUpgrade(ForgeMode.Level, entry, preview);
            if (stage != null) stage.interactable = CanUpgrade(ForgeMode.Stage, entry, preview);
        }

        private BabyEquipEntry FindEntry(int positionId) { BabyEquipInfo equip = BabyModel.Instance.Equip; if (equip == null || positionId == 0) return null; for (int i = 0; i < equip.EquipList.Count; i++) if (equip.EquipList[i].PositionId == positionId) return equip.EquipList[i]; return null; }
        private void RenderCosts(Transform parent, BabyEquipUpgradeConfigs.PreviewResult preview) { if (parent == null || _awardTemplate == null || preview == null) return; for (int i = 0; i < preview.Costs.Count; i++) { BabyEquipUpgradeConfigs.CostItem cost = preview.Costs[i]; if (cost == null || cost.TypeId <= 0 || cost.Num <= 0) continue; GameObject go = Instantiate(_awardTemplate, parent); go.SetActive(true); go.GetComponent<BaseAwardItem>()?.SetData(cost.TypeId, cost.Num); _renderedCosts.Add(go); } }
        private void SetMode(ForgeMode mode) { Mode = mode; if (_levelGroup != null) _levelGroup.gameObject.SetActive(mode == ForgeMode.Level); if (_stageGroup != null) _stageGroup.gameObject.SetActive(mode == ForgeMode.Stage); if (_maxGroup != null) _maxGroup.gameObject.SetActive(mode == ForgeMode.Max); }
        private void HideEffects() { if (_targetGroup != null) _targetGroup.gameObject.SetActive(false); if (_effectGroup != null) _effectGroup.gameObject.SetActive(false); if (_targetEffectGroup != null) _targetEffectGroup.gameObject.SetActive(false); }
        private void ClearRenderedCosts() { for (int i = 0; i < _renderedCosts.Count; i++) if (_renderedCosts[i] != null) { if (Application.isPlaying) Destroy(_renderedCosts[i]); else DestroyImmediate(_renderedCosts[i]); } _renderedCosts.Clear(); }
        private static string BuildConfirmText(BabyEquipUpgradeConfigs.PreviewResult preview) { var text = new StringBuilder(preview.IsStageUpgrade ? "是否消耗以下材料升阶宝宝装备？" : "是否消耗以下材料强化宝宝装备？"); for (int i = 0; i < preview.Costs.Count; i++) { BabyEquipUpgradeConfigs.CostItem cost = preview.Costs[i]; string name = GoodsModel.GetGoodsName(cost.TypeId); text.Append('\n').Append(string.IsNullOrEmpty(name) ? cost.TypeId.ToString() : name).Append('×').Append(cost.Num); } return text.ToString(); }
        private static string BuildFingerprint(BabyEquipUpgradeConfigs.PreviewResult preview) { if (preview == null) return string.Empty; var key = new StringBuilder().Append(preview.IsStageUpgrade ? 'S' : 'L').Append(':').Append(preview.RequiredExp); for (int i = 0; i < preview.Costs.Count; i++) { BabyEquipUpgradeConfigs.CostItem cost = preview.Costs[i]; key.Append('|').Append(cost.Type).Append(':').Append(cost.TypeId).Append(':').Append(cost.Num); } return key.ToString(); }
        private void CacheNodes()
        {
            if (_levelGroup != null && _stageGroup != null && _maxGroup != null && _awardTemplate != null && _levelBtn != null && _stageBtn != null && _buttonsBound) return;
            Transform[] nodes = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < nodes.Length; i++) { Transform node = nodes[i]; if (node.name == "lvGp") _levelGroup = node; else if (node.name == "stageGp") _stageGroup = node; else if (node.name == "maxStage") _maxGroup = node; else if (node.name == "targetGp") _targetGroup = node; else if (node.name == "effectGp") _effectGroup = node; else if (node.name == "targetEffectGp") _targetEffectGroup = node; }
            for (int i = 0; i < nodes.Length; i++) { Transform node = nodes[i]; if (node.name == "lvExpImg" && IsUnder(node, _levelGroup)) _levelExp = node.GetComponent<Image>(); else if (node.name == "lvLb" && IsUnder(node, _levelGroup)) _levelLabel = node.GetComponent<TextMeshProUGUI>(); else if (node.name == "nextlvLb" && IsUnder(node, _levelGroup)) _nextLevelLabel = node.GetComponent<TextMeshProUGUI>(); else if (node.name == "lvExpLb" && IsUnder(node, _levelGroup)) _levelExpLabel = node.GetComponent<TextMeshProUGUI>(); else if (node.name == "lvBtn" && IsUnder(node, _levelGroup)) _levelBtn = node; else if (node.name == "stageBtn" && IsUnder(node, _stageGroup)) _stageBtn = node; else if (node.name == "Content" && IsUnder(node, _levelGroup)) _levelCosts = node; else if (node.name == "Content1" && IsUnder(node, _stageGroup)) _stageCosts = node; else if (node.name == "BaseAwardItem" && node.parent != null && node.parent.name == "__Templates") _awardTemplate = node.gameObject; }
            BindButton(_levelBtn, () => TryUpgrade(ForgeMode.Level));
            BindButton(_stageBtn, () => TryUpgrade(ForgeMode.Stage));
            _buttonsBound = true;
        }
        private static void BindButton(Transform target, System.Action click) { if (target == null) return; ClearButton(target); UIUtil.AddClick(target, click); }
        private static void ClearButton(Transform target) { Button button = target != null ? target.GetComponent<Button>() : null; if (button != null) button.onClick.RemoveAllListeners(); }
        private static bool IsUnder(Transform node, Transform parent) => parent != null && node != null && (node == parent || node.IsChildOf(parent));
    }
}
