using System.Collections.Generic;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Generated.UI.Baby;
using Shenxiao.Module.Core.Common;
using TMPro;
using UnityEngine;

namespace Shenxiao.Module.Core.Baby
{
    public sealed class BabyEquipView : BabyEquipViewBind
    {
        private TextMeshProUGUI _nameLb;
        private Transform _fight;
        private Transform _leftGp, _rightGp;
        private GameObject _template;
        private readonly List<GameObject> _items = new List<GameObject>();
        private GameObject _fightTemplate;
        private FightingShowSmallItem _fighting;
        private Transform _candidateContent;
        private GameObject _candidateTemplate;
        private readonly List<GameObject> _candidateItems = new List<GameObject>();
        private int _selectedPosition = 1;

        public void Refresh(BabyEquipInfo equip, BabyBasicInfo basic)
        {
            CacheNodes(); ClearItems();
            if (_nameLb != null) _nameLb.text = basic != null ? basic.BabyName ?? string.Empty : string.Empty;
            if (_fighting == null && _fight != null && _fightTemplate != null)
            {
                GameObject go = Instantiate(_fightTemplate, _fight);
                go.SetActive(true);
                _fighting = go.GetComponent<FightingShowSmallItem>();
            }
            if (_fighting != null) _fighting.SetFighting(equip != null ? equip.Power : 0);
            if (_template == null) return;
            for (int position = 1; position <= 6; position++)
            {
                Transform parent = position <= 3 ? _leftGp : _rightGp;
                if (parent == null) continue;
                GameObject go = Instantiate(_template, parent); go.SetActive(true);
                BabyEquipIcon icon = go.GetComponent<BabyEquipIcon>();
                if (icon == null) { DestroyItem(go); continue; }
                BabyEquipEntry entry = Find(equip, position);
                icon.SetData(position, entry, position == _selectedPosition, SelectPosition); _items.Add(go);
            }
            RefreshCandidates();
        }

        private void SelectPosition(int positionId)
        {
            if (_selectedPosition == positionId) return;
            _selectedPosition = positionId;
            for (int i = 0; i < _items.Count; i++)
            {
                BabyEquipIcon icon = _items[i] != null ? _items[i].GetComponent<BabyEquipIcon>() : null;
                if (icon != null) icon.SetSelected(icon.PositionId == _selectedPosition);
            }
            RefreshCandidates();
        }

        private void RefreshCandidates()
        {
            ClearCandidates();
            if (_candidateContent == null || _candidateTemplate == null) return;
            IReadOnlyList<BagGoods> goods = BagModel.Instance.GetContainer(BagModel.POS_BABY_BAG);
            int stage = BabyModel.Instance.Stage != null ? BabyModel.Instance.Stage.Stage : 0;
            int index = 0;
            for (int i = 0; i < goods.Count; i++)
            {
                BagGoods entry = goods[i];
                GoodsModel.GoodsBasic basic = entry != null ? GoodsModel.GetGoodsBasicByTypeId(entry.TypeId) : null;
                if (entry == null || entry.GoodsNum <= 0 || basic == null || basic.Type != 65 || !BabyEquipConfigs.CanWear(entry.TypeId, _selectedPosition, stage)) continue;
                GameObject go = Instantiate(_candidateTemplate, _candidateContent); go.SetActive(true);
                RectTransform rect = go.transform as RectTransform;
                if (rect != null)
                {
                    int col = index % 4;
                    int row = index / 4;
                    float width = GetCandidateWidth();
                    float step = 156f + (width - 4f * 156f) / 3f;
                    rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 1f);
                    rect.sizeDelta = new Vector2(156f, 208f);
                    rect.anchoredPosition = new Vector2(col * step, -row * 208f);
                }
                go.GetComponent<BabyEquipSubItem>()?.SetData(entry, OnCandidateClick);
                _candidateItems.Add(go); index++;
            }
            RectTransform content = _candidateContent as RectTransform;
            if (content != null)
            {
                content.anchorMin = content.anchorMax = new Vector2(0f, 1f);
                content.pivot = new Vector2(0f, 1f);
                content.anchoredPosition = Vector2.zero;
                content.sizeDelta = new Vector2(GetCandidateWidth(), ((index + 3) / 4) * 208f);
            }
        }

        private void OnCandidateClick(BagGoods goods)
        {
            if (goods == null) return;
            BagGoods current = BagModel.Instance.FindContainerGoods(BagModel.POS_BABY_BAG, goods.GoodsId);
            int stage = BabyModel.Instance.Stage != null ? BabyModel.Instance.Stage.Stage : 0;
            if (current == null || current.GoodsNum <= 0 || !BabyEquipConfigs.CanWear(current.TypeId, _selectedPosition, stage)) return;
            BabyController.Instance.RequestEquipWear(_selectedPosition, current.GoodsId);
        }

        private static BabyEquipEntry Find(BabyEquipInfo equip, int position) { if (equip != null) for (int i = 0; i < equip.EquipList.Count; i++) if (equip.EquipList[i].PositionId == position) return equip.EquipList[i]; return null; }
        private float GetCandidateWidth() { float width = _Scroller1 != null && _Scroller1.viewport != null ? _Scroller1.viewport.rect.width : 0f; return width > 0f ? width : 680f; }
        private void CacheNodes() { if (_Scroller1 != null && _Scroller1.content != null) { Transform inner = _Scroller1.content.Find("Content"); _candidateContent = inner != null ? inner : _Scroller1.content; if (inner != null) _Scroller1.content = inner as RectTransform; } if (_template != null && _candidateTemplate != null) return; foreach (Transform node in GetComponentsInChildren<Transform>(true)) { if (node.name == "nameLb" && node.parent == transform) _nameLb = node.GetComponent<TextMeshProUGUI>(); else if (node.name == "fight") _fight = node; else if (node.name == "leftGp") _leftGp = node; else if (node.name == "rightGp") _rightGp = node; else if (node.name == "BabyEquipIcon" && node.parent != null && node.parent.name == "__Templates") _template = node.gameObject; else if (node.name == "BabyEquipSubItem" && node.parent != null && node.parent.name == "__Templates") _candidateTemplate = node.gameObject; else if (node.name == "FightingShowSmallItem" && node.parent != null && node.parent.name == "__Templates") _fightTemplate = node.gameObject; } }
        private void ClearItems() { for (int i = 0; i < _items.Count; i++) DestroyItem(_items[i]); _items.Clear(); }
        private void ClearCandidates() { for (int i = 0; i < _candidateItems.Count; i++) DestroyItem(_candidateItems[i]); _candidateItems.Clear(); }
        private static void DestroyItem(GameObject go) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
    }
}
