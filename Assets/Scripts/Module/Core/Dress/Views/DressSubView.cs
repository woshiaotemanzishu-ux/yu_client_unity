using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Dress;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Role;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Dress
{
    /// <summary>装扮内容区：列表选择、预览、属性、材料与按钮状态。</summary>
    public sealed class DressSubView : DressSubViewBind
    {
        private GameObject _itemTemplate;
        private GameObject _proTemplate;
        private GameObject _skillTemplate;
        private readonly List<DressItem> _items = new List<DressItem>();
        private readonly List<DressProItem> _pros = new List<DressProItem>();
        private readonly List<DressSkillItem> _skills = new List<DressSkillItem>();
        private readonly Dictionary<byte, uint> _selectedByType = new Dictionary<byte, uint>();
        private readonly HashSet<ulong> _requestedInactivePower = new HashSet<ulong>();
        private byte _type = DressView.BubbleType;
        private uint _selectedId;
        private int _refreshVersion;
        private TMP_Text _powerLabel;
        private bool _confirming;
        private byte _confirmType;
        private uint _confirmId;
        private int _confirmLevel;
        private int _confirmGoodsId;
        private long _confirmNeed;
        private long _confirmHave;

        public byte Type => _type;
        public uint SelectedId => _selectedId;
        public int VisibleItemCount => _items.Count;

        public void Configure(GameObject itemTemplate, GameObject proTemplate, GameObject skillTemplate)
        {
            _itemTemplate = itemTemplate;
            _proTemplate = proTemplate;
            _skillTemplate = skillTemplate;
        }

        protected override void OnInit()
        {
            if (activite_btn != null) UIUtil.AddClick(activite_btn, OnActivateOrUpgrade);
            if (use_btn != null) UIUtil.AddClick(use_btn, OnUseOrTakeOff);
            if (model_img != null) model_img.raycastTarget = false;
            _powerLabel = _gp_fight != null ? _gp_fight.Find("dress_power_label")?.GetComponent<TMP_Text>() : null;
            DressController.Instance.TransactionStateChanged += OnTransactionStateChanged;
        }

        protected override void OnShow(object args)
        {
            Refresh();
        }

        protected override void OnHide()
        {
            ClearConfirm();
        }

        protected override void OnDispose()
        {
            DressController.Instance.TransactionStateChanged -= OnTransactionStateChanged;
        }

        public void SetType(byte type)
        {
            if (type != DressView.BubbleType && type != DressView.PhotoType && type != DressView.HeadType)
                type = DressView.BubbleType;
            if (_selectedId != 0) _selectedByType[_type] = _selectedId;
            _type = type;
            Refresh();
        }

        public void Refresh()
        {
            _ = RefreshAsync(++_refreshVersion);
        }

        private async Task RefreshAsync(int version)
        {
            await DressConfigs.EnsureLoaded();
            await GoodsModel.EnsureLoaded();
            if (this == null || version != _refreshVersion) return;

            IReadOnlyList<DressConfigs.Row> rows = DressConfigs.GetDisplayRows(_type);
            DressModel.Instance.TryGet(_type, out DressModel.Snapshot snapshot);
            uint selected = _selectedByType.TryGetValue(_type, out uint remembered) ? remembered : 0;
            if (!Contains(rows, selected)) selected = snapshot != null && Contains(rows, snapshot.UsedDressId) ? snapshot.UsedDressId : 0;
            if (selected == 0 && rows.Count > 0) selected = rows[0].Id;
            _selectedId = selected;
            _selectedByType[_type] = selected;

            BuildItems(rows, snapshot);
            RefreshSelected(snapshot);
        }

        private void BuildItems(IReadOnlyList<DressConfigs.Row> rows, DressModel.Snapshot snapshot)
        {
            ClearItems();
            if (_itemTemplate == null || scroll == null) return;
            Transform parent = scroll.content != null ? scroll.content : scroll.transform;
            for (int i = 0; i < rows.Count; i++)
            {
                DressConfigs.Row row = rows[i];
                GameObject go = Instantiate(_itemTemplate, parent);
                go.name = "DressItem_" + row.Type + "_" + row.Id;
                go.SetActive(false);
                DressItem item = go.GetComponent<DressItem>();
                if (item == null) { DestroyRuntimeObject(go.transform); continue; }
                DressModel.Entry entry = FindEntry(snapshot, row.Id);
                bool worn = snapshot != null && snapshot.UsedDressId == row.Id;
                item.Show();
                item.SetData(row, row.Id == _selectedId, entry, worn, () => Select(row.Id));
                _items.Add(item);
            }
            if (parent is RectTransform rect) LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }

        private void Select(uint id)
        {
            if (_selectedId == id) return;
            _selectedId = id;
            _selectedByType[_type] = id;
            for (int i = 0; i < _items.Count; i++) _items[i].SetSelected(_items[i].DressId == id);
            DressModel.Instance.TryGet(_type, out DressModel.Snapshot snapshot);
            RefreshSelected(snapshot);
        }

        private void RefreshSelected(DressModel.Snapshot snapshot)
        {
            DressModel.Entry entry = FindEntry(snapshot, _selectedId);
            int level = entry?.DressLevel ?? 0;
            DressConfigs.Row first = DressConfigs.GetRow(_type, _selectedId, 1);
            DressConfigs.Row current = entry != null ? DressConfigs.GetRow(_type, _selectedId, Math.Max(1, level)) : null;
            DressConfigs.Row next = DressConfigs.GetRow(_type, _selectedId, entry != null ? level + 1 : 1);
            DressConfigs.Row display = current ?? first;
            if (display == null) return;

            if (dress_name != null) dress_name.text = "Lv." + level + " " + display.Name;
            if (pro_title != null) pro_title.text = entry != null ? "升级增加属性" : "激活增加属性";
            if (active_title != null) active_title.text = entry != null ? "升级条件" : "激活条件";
            SetPower(entry);
            SetProperties(current, next);
            SetSkills(display, level);
            SetButtons(display, entry, next, snapshot);
            ShowPreview(display);
        }

        private void SetPower(DressModel.Entry entry)
        {
            if (_powerLabel == null) return;
            if (entry != null)
            {
                _powerLabel.text = "战力" + entry.CurrentPower;
                return;
            }
            if (DressModel.Instance.TryGetInactivePower(_type, _selectedId, out DressModel.InactivePowerSnapshot snapshot))
            {
                _powerLabel.text = "战力" + snapshot.ActivePower;
                return;
            }
            _powerLabel.text = "战力--";
            ulong key = ((ulong)_type << 32) | _selectedId;
            if (_selectedId != 0 && _requestedInactivePower.Add(key))
                DressController.Instance.RequestInactivePower(_type, _selectedId);
        }

        private void SetProperties(DressConfigs.Row current, DressConfigs.Row next)
        {
            ClearPros();
            if (_proTemplate == null || Content11 == null) return;
            IReadOnlyList<DressConfigs.AttrValue> now = DressConfigs.GetAttrs(current);
            IReadOnlyList<DressConfigs.AttrValue> after = DressConfigs.GetAttrs(next);
            var order = new List<int>();
            var nowMap = new Dictionary<int, long>();
            var nextMap = new Dictionary<int, long>();
            for (int i = 0; i < now.Count; i++) { nowMap[now[i].Id] = now[i].Value; if (!order.Contains(now[i].Id)) order.Add(now[i].Id); }
            for (int i = 0; i < after.Count; i++) { nextMap[after[i].Id] = after[i].Value; if (!order.Contains(after[i].Id)) order.Add(after[i].Id); }

            for (int i = 0; i < order.Count; i++)
            {
                int attr = order[i];
                GameObject go = Instantiate(_proTemplate, Content11);
                go.name = "DressProItem_" + attr;
                go.SetActive(false);
                DressProItem item = go.GetComponent<DressProItem>();
                if (item == null) { DestroyRuntimeObject(go.transform); continue; }
                item.Show();
                item.SetData(attr, nowMap.TryGetValue(attr, out long nv) ? nv : 0L, nextMap.TryGetValue(attr, out long av) ? av : 0L);
                _pros.Add(item);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(Content11);
        }

        private void SetButtons(DressConfigs.Row display, DressModel.Entry entry, DressConfigs.Row next, DressModel.Snapshot snapshot)
        {
            int turn = DressConfigs.GetTurnCondition(display);
            bool active = entry != null;
            bool worn = active && snapshot != null && snapshot.UsedDressId == display.Id;
            if (active_desc != null) active_desc.text = !active && turn > 0 ? turn + "转可激活" : "";
            if (use_btn != null) use_btn.gameObject.SetActive(active);
            if (use_btn_label != null) use_btn_label.text = worn ? "卸下" : "使用";

            bool showAction = active ? next != null : turn <= 0;
            if (activite_btn != null) activite_btn.gameObject.SetActive(showAction);
            if (activite_btn_label != null) activite_btn_label.text = active ? "升级" : "激活";
            if (top_level != null) top_level.gameObject.SetActive(active && next == null);
            if (upgrade_dot != null) upgrade_dot.gameObject.SetActive(false);
            if (active_btn_img != null) active_btn_img.color = next == null && active ? new Color32(156, 156, 156, 255) : Color.white;

            DressConfigs.CostValue cost = DressConfigs.GetFirstCost(next ?? display);
            bool hasCost = cost != null && cost.TypeId > 0;
            if (cost_con != null) cost_con.gameObject.SetActive(hasCost);
            if (mat_num != null)
            {
                if (!hasCost) mat_num.text = "";
                else
                {
                    var mapped = GoodsModel.GetMappingTypeId(cost.Type, cost.TypeId);
                    int goodsId = mapped.goodsId;
                    long have = BagModel.Instance.GetTypeGoodsNum(goodsId);
                    mat_num.text = have + "/" + cost.Num;
                    mat_num.color = have >= cost.Num ? new Color32(25, 174, 74, 255) : new Color32(255, 79, 80, 255);
                }
            }

            bool pending = DressController.Instance.IsTransactionPending || _confirming;
            Button activateButton = activite_btn != null ? activite_btn.GetComponent<Button>() : null;
            Button useButton = use_btn != null ? use_btn.GetComponent<Button>() : null;
            if (activateButton != null) activateButton.interactable = !pending && showAction;
            if (useButton != null) useButton.interactable = !pending && active;
            if (pending && active_btn_img != null) active_btn_img.color = new Color32(156, 156, 156, 255);
        }

        private void SetSkills(DressConfigs.Row display, int currentLevel)
        {
            ClearSkills();
            IReadOnlyList<DressConfigs.Row> rows = DressConfigs.GetSkillRows(display.Type, display.Id);
            if (skill_group != null) skill_group.gameObject.SetActive(rows.Count > 0);
            if (_skillTemplate == null || Content1 == null) return;
            for (int i = 0; i < rows.Count; i++)
            {
                DressConfigs.Row row = rows[i];
                GameObject go = Instantiate(_skillTemplate, Content1);
                go.name = "DressSkillItem_" + row.Skill;
                go.SetActive(false);
                DressSkillItem item = go.GetComponent<DressSkillItem>();
                if (item == null) { DestroyRuntimeObject(go.transform); continue; }
                item.Show();
                item.SetData(row.Skill, row.Level, currentLevel);
                _skills.Add(item);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(Content1);
        }

        private async void ShowPreview(DressConfigs.Row row)
        {
            if (model_img == null || row == null) return;
            uint requested = row.Id;
            byte requestedType = row.Type;
            string path = "";
            if (row.Type == DressView.HeadType)
            {
                int career = RoleModel.Instance.Career > 0 ? RoleModel.Instance.Career : 1;
                string icon = DressConfigs.GetHeadIcon(row, career);
                if (!string.IsNullOrEmpty(icon)) path = GameResPath.GetHeadPath(icon);
            }
            else if (row.Type == DressView.BubbleType)
            {
                path = GameResPath.GetIcon("chat", "1_" + row.Id + "_0");
            }
            else if (row.Type == DressView.PhotoType)
            {
                path = GameResPath.GetHeadPath("2_" + row.Id);
            }

            model_img.gameObject.SetActive(false);
            bool ok = !string.IsNullOrEmpty(path) && await ResManager.SetImageAsync(model_img, path, nativeSize: false);
            if (this == null || _selectedId != requested || _type != requestedType) return;
            model_img.preserveAspect = true;
            model_img.gameObject.SetActive(ok);
        }

        private void OnActivateOrUpgrade()
        {
            if (_confirming || DressController.Instance.IsTransactionPending || _selectedId == 0) return;
            DressModel.Instance.TryGet(_type, out DressModel.Snapshot snapshot);
            DressModel.Entry entry = FindEntry(snapshot, _selectedId);
            int level = entry?.DressLevel ?? 0;
            DressConfigs.Row target = DressConfigs.GetRow(_type, _selectedId, level + 1);
            if (target == null)
            {
                TipsManager.Toast(entry != null ? "已升至最高等级" : "装扮配置不存在");
                return;
            }
            int turn = DressConfigs.GetTurnCondition(target);
            int currentTurn = RoleModel.Instance.Figure != null ? RoleModel.Instance.Figure.turn : 0;
            if (turn > currentTurn)
            {
                TipsManager.Toast(turn + "转可激活");
                return;
            }

            DressConfigs.CostValue cost = DressConfigs.GetFirstCost(target);
            int goodsId = 0;
            long need = 0;
            long have = 0;
            if (cost != null && cost.TypeId > 0 && cost.Num > 0)
            {
                goodsId = GoodsModel.GetMappingTypeId(cost.Type, cost.TypeId).goodsId;
                need = cost.Num;
                have = BagModel.Instance.GetTypeGoodsNum(goodsId);
                if (have < need)
                {
                    TipsManager.Toast("激活/升级材料不足");
                    Refresh();
                    return;
                }
            }

            _confirming = true;
            _confirmType = _type;
            _confirmId = _selectedId;
            _confirmLevel = level;
            _confirmGoodsId = goodsId;
            _confirmNeed = need;
            _confirmHave = have;
            Refresh();
            string action = entry == null ? "激活" : "升级";
            string costText = goodsId > 0 ? "，消耗材料 " + need : string.Empty;
            TipsManager.Confirm("是否" + action + "该装扮" + costText + "？", ConfirmActivateOrUpgrade, CancelActivateOrUpgrade);
        }

        private void OnUseOrTakeOff()
        {
            if (_confirming || DressController.Instance.IsTransactionPending || _selectedId == 0) return;
            if (!DressModel.Instance.TryGet(_type, out DressModel.Snapshot snapshot)
                || FindEntry(snapshot, _selectedId) == null)
            {
                TipsManager.Toast("请先激活该装扮");
                return;
            }
            bool worn = snapshot.UsedDressId == _selectedId;
            bool sent = worn
                ? DressController.Instance.TakeOff(_type, _selectedId)
                : DressController.Instance.Use(_type, _selectedId);
            if (!sent) TipsManager.Toast("装扮操作正在处理中");
        }

        private void ConfirmActivateOrUpgrade()
        {
            if (!_confirming || DressController.Instance.IsTransactionPending) return;
            DressModel.Instance.TryGet(_confirmType, out DressModel.Snapshot snapshot);
            DressModel.Entry entry = FindEntry(snapshot, _confirmId);
            int currentLevel = entry?.DressLevel ?? 0;
            DressConfigs.Row target = DressConfigs.GetRow(_confirmType, _confirmId, currentLevel + 1);
            long currentHave = _confirmGoodsId > 0 ? BagModel.Instance.GetTypeGoodsNum(_confirmGoodsId) : 0;
            bool valid = _type == _confirmType && _selectedId == _confirmId && currentLevel == _confirmLevel
                && target != null && currentHave == _confirmHave && currentHave >= _confirmNeed;
            byte type = _confirmType;
            uint id = _confirmId;
            ClearConfirm();
            if (!valid)
            {
                TipsManager.Toast("装扮状态或材料已变化，请重新确认");
                Refresh();
                return;
            }
            if (!DressController.Instance.ActivateOrUpgrade(type, id))
                TipsManager.Toast("装扮操作正在处理中");
            Refresh();
        }

        private void CancelActivateOrUpgrade()
        {
            ClearConfirm();
            Refresh();
        }

        private void ClearConfirm()
        {
            _confirming = false;
            _confirmType = 0;
            _confirmId = 0;
            _confirmLevel = 0;
            _confirmGoodsId = 0;
            _confirmNeed = 0;
            _confirmHave = 0;
        }

        private void OnTransactionStateChanged()
        {
            if (this != null && IsShown) Refresh();
        }

        private void ClearItems()
        {
            for (int i = 0; i < _items.Count; i++) DestroyRuntimeObject(_items[i]);
            _items.Clear();
        }

        private void ClearPros()
        {
            for (int i = 0; i < _pros.Count; i++) DestroyRuntimeObject(_pros[i]);
            _pros.Clear();
        }

        private void ClearSkills()
        {
            for (int i = 0; i < _skills.Count; i++) DestroyRuntimeObject(_skills[i]);
            _skills.Clear();
        }

        private static void DestroyRuntimeObject(Component component)
        {
            if (component == null) return;
            if (Application.isPlaying) Destroy(component.gameObject);
            else DestroyImmediate(component.gameObject);
        }

        private static DressModel.Entry FindEntry(DressModel.Snapshot snapshot, uint id)
        {
            if (snapshot == null) return null;
            for (int i = 0; i < snapshot.Entries.Count; i++) if (snapshot.Entries[i].DressId == id) return snapshot.Entries[i];
            return null;
        }

        private static bool Contains(IReadOnlyList<DressConfigs.Row> rows, uint id)
        {
            if (id == 0) return false;
            for (int i = 0; i < rows.Count; i++) if (rows[i].Id == id) return true;
            return false;
        }
    }
}
