using System.Collections.Generic;
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
                icon.SetData(position, entry); _items.Add(go);
            }
        }

        private static BabyEquipEntry Find(BabyEquipInfo equip, int position) { if (equip != null) for (int i = 0; i < equip.EquipList.Count; i++) if (equip.EquipList[i].PositionId == position) return equip.EquipList[i]; return null; }
        private void CacheNodes() { if (_template != null) return; foreach (Transform node in GetComponentsInChildren<Transform>(true)) { if (node.name == "nameLb") _nameLb = node.GetComponent<TextMeshProUGUI>(); else if (node.name == "fight") _fight = node; else if (node.name == "leftGp") _leftGp = node; else if (node.name == "rightGp") _rightGp = node; else if (node.name == "BabyEquipIcon" && node.parent != null && node.parent.name == "__Templates") _template = node.gameObject; else if (node.name == "FightingShowSmallItem" && node.parent != null && node.parent.name == "__Templates") _fightTemplate = node.gameObject; } }
        private void ClearItems() { for (int i = 0; i < _items.Count; i++) DestroyItem(_items[i]); _items.Clear(); }
        private static void DestroyItem(GameObject go) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
    }
}
