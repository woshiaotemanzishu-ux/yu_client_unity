using System.Collections.Generic;
using Shenxiao.Generated.UI.Baby;
using Shenxiao.Module.Core.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Baby
{
    /// <summary>Read-only Forge child-panel display skeleton.</summary>
    public sealed class BabyForgeView : BabyForgeViewBind
    {
        public enum ForgeMode { Empty, Level, Stage, Max }
        private Transform _levelGroup, _stageGroup, _maxGroup, _levelCosts, _stageCosts, _targetGroup, _effectGroup, _targetEffectGroup;
        private Image _levelExp;
        private TextMeshProUGUI _levelLabel, _nextLevelLabel, _levelExpLabel;
        private GameObject _awardTemplate;
        private readonly List<GameObject> _renderedCosts = new List<GameObject>();
        public int PositionId { get; private set; }
        public ForgeMode Mode { get; private set; }
        public int RenderedCostCount => _renderedCosts.Count;

        public void SetPositionId(int positionId) { PositionId = positionId >= 1 && positionId <= 6 ? positionId : 0; Refresh(); }
        public void Refresh()
        {
            CacheNodes(); ClearRenderedCosts(); SetMode(ForgeMode.Empty); HideEffects();
            BabyEquipEntry entry = FindEntry(PositionId);
            if (entry == null || entry.Id <= 0) return;
            BabyEquipUpgradeConfigs.StrenCfg next = BabyEquipUpgradeConfigs.GetStren(entry.PositionId, entry.Stage, entry.StageLevel + 1);
            BabyEquipUpgradeConfigs.PreviewResult preview = BabyEquipUpgradeConfigs.Preview(entry);
            if (next != null)
            {
                SetMode(ForgeMode.Level);
                if (_levelLabel != null) _levelLabel.text = entry.StageLevel + "\u7ea7";
                if (_nextLevelLabel != null) _nextLevelLabel.text = next.StageLevel + "\u7ea7";
                if (_levelExpLabel != null) _levelExpLabel.text = entry.StageExp + "/" + next.PointCon;
                if (_levelExp != null) _levelExp.fillAmount = next.PointCon > 0 ? Mathf.Clamp01((float)entry.StageExp / next.PointCon) : 0f;
                RenderCosts(_levelCosts, preview); return;
            }
            if (preview != null && preview.IsStageUpgrade) { SetMode(ForgeMode.Stage); RenderCosts(_stageCosts, preview); return; }
            SetMode(ForgeMode.Max);
        }
        public void Clear() { CacheNodes(); PositionId = 0; ClearRenderedCosts(); SetMode(ForgeMode.Empty); HideEffects(); }
        protected override void OnHide() => Clear();
        protected override void OnDispose() => Clear();
        private BabyEquipEntry FindEntry(int positionId) { BabyEquipInfo equip = BabyModel.Instance.Equip; if (equip == null || positionId == 0) return null; for (int i = 0; i < equip.EquipList.Count; i++) if (equip.EquipList[i].PositionId == positionId) return equip.EquipList[i]; return null; }
        private void RenderCosts(Transform parent, BabyEquipUpgradeConfigs.PreviewResult preview) { if (parent == null || _awardTemplate == null || preview == null) return; for (int i = 0; i < preview.Costs.Count; i++) { BabyEquipUpgradeConfigs.CostItem cost = preview.Costs[i]; if (cost == null || cost.TypeId <= 0 || cost.Num <= 0) continue; GameObject go = Instantiate(_awardTemplate, parent); go.SetActive(true); go.GetComponent<BaseAwardItem>()?.SetData(cost.TypeId, cost.Num); _renderedCosts.Add(go); } }
        private void SetMode(ForgeMode mode) { Mode = mode; if (_levelGroup != null) _levelGroup.gameObject.SetActive(mode == ForgeMode.Level); if (_stageGroup != null) _stageGroup.gameObject.SetActive(mode == ForgeMode.Stage); if (_maxGroup != null) _maxGroup.gameObject.SetActive(mode == ForgeMode.Max); }
        private void HideEffects() { if (_targetGroup != null) _targetGroup.gameObject.SetActive(false); if (_effectGroup != null) _effectGroup.gameObject.SetActive(false); if (_targetEffectGroup != null) _targetEffectGroup.gameObject.SetActive(false); }
        private void ClearRenderedCosts() { for (int i = 0; i < _renderedCosts.Count; i++) if (_renderedCosts[i] != null) { if (Application.isPlaying) Destroy(_renderedCosts[i]); else DestroyImmediate(_renderedCosts[i]); } _renderedCosts.Clear(); }
        private void CacheNodes()
        {
            if (_levelGroup != null && _stageGroup != null && _maxGroup != null && _awardTemplate != null) return;
            Transform[] nodes = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < nodes.Length; i++) { Transform node = nodes[i]; if (node.name == "lvGp") _levelGroup = node; else if (node.name == "stageGp") _stageGroup = node; else if (node.name == "maxStage") _maxGroup = node; else if (node.name == "targetGp") _targetGroup = node; else if (node.name == "effectGp") _effectGroup = node; else if (node.name == "targetEffectGp") _targetEffectGroup = node; }
            for (int i = 0; i < nodes.Length; i++) { Transform node = nodes[i]; if (node.name == "lvExpImg" && IsUnder(node, _levelGroup)) _levelExp = node.GetComponent<Image>(); else if (node.name == "lvLb" && IsUnder(node, _levelGroup)) _levelLabel = node.GetComponent<TextMeshProUGUI>(); else if (node.name == "nextlvLb" && IsUnder(node, _levelGroup)) _nextLevelLabel = node.GetComponent<TextMeshProUGUI>(); else if (node.name == "lvExpLb" && IsUnder(node, _levelGroup)) _levelExpLabel = node.GetComponent<TextMeshProUGUI>(); else if (node.name == "Content" && IsUnder(node, _levelGroup)) _levelCosts = node; else if (node.name == "Content1" && IsUnder(node, _stageGroup)) _stageCosts = node; else if (node.name == "BaseAwardItem" && node.parent != null && node.parent.name == "__Templates") _awardTemplate = node.gameObject; }
        }
        private static bool IsUnder(Transform node, Transform parent) => parent != null && node != null && (node == parent || node.IsChildOf(parent));
    }
}
