using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.EquipArmor;
using Shenxiao.Module.Core.Armor;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 不朽圣骸页：消费 14401 权威树和三张静态表，完整呈现阶段/类型/部位、属性、材料与红点；
    /// 打造按钮经本地预检、确认后二次校验和单飞保护发送 14402，本地绝不预扣材料或乐观置位。
    /// </summary>
    public sealed class EquipArmorView : EquipArmorViewBind
    {
        private static readonly Vector2[] MaterialPositions =
        {
            new Vector2(129f, 77f),
            new Vector2(1f, -165f),
            new Vector2(257f, -164f),
        };

        private readonly List<GameObject> _rendered = new List<GameObject>();
        private readonly List<GameObject> _awardInstances = new List<GameObject>();
        private Button _makeButton;
        private byte _selectedStage = 1;
        private byte _selectedType = 1;
        private byte _selectedPosition = 1;
        private bool _selectionInitialized;
        private bool _confirming;
        private bool _pending;
        private int _confirmModelVersion;
        private byte _confirmStage;
        private byte _confirmType;
        private byte _confirmPosition;
        private int _confirmEquipmentId;
        private string _confirmFingerprint = string.Empty;
        private int _renderVersion;
        private int _loadVersion;

        public bool Pending => _pending;
        public byte SelectedStage => _selectedStage;
        public byte SelectedType => _selectedType;
        public byte SelectedPosition => _selectedPosition;
        public string LastConfirmText { get; private set; } = string.Empty;

        protected override void OnInit()
        {
            if (_tpl_ArmorItem != null) _tpl_ArmorItem.SetActive(false);
            if (_tpl_ArmorTabItem != null) _tpl_ArmorTabItem.SetActive(false);
            if (_tpl_ArmorAttrItem != null) _tpl_ArmorAttrItem.SetActive(false);

            if (_lb_title != null) _lb_title.text = "不朽圣骸";
            if (_lb_title2 != null) _lb_title2.text = "圣骸属性";
            if (_lb_left != null) _lb_left.text = "荒陨圣骸";
            if (_lb_right != null) _lb_right.text = "天殒圣骸";

            _makeButton = BindContainerButton(_btn_make, OnMakeClick);
            BindContainerButton(leftBtn, () => SelectType(1));
            BindContainerButton(rightBtn, () => SelectType(2));
            BindImageButton(_btn_all, () => EquipFlow.OpenSub("ArmorAttrView"));

            SetRaycast(img_make, false);
            SetRaycast(_lb_make, false);
            SetRaycast(_btn_left, false);
            SetRaycast(_lb_left, false);
            SetRaycast(_btn_right, false);
            SetRaycast(_lb_right, false);
            SetRaycast(_img_red, false);
            SetRaycast(_img_red1, false);
            SetRaycast(_img_red2, false);
        }

        protected override void OnShow(object args)
        {
            EventDispatcher.On(GlobalEvent.EVT_ARMOR_UPDATED, OnArmorUpdated);
            EventDispatcher.On<uint>(GlobalEvent.EVT_ARMOR_MAKE_RESULT, OnMakeResult);
            EventDispatcher.On(GlobalEvent.EVT_BAG_UPDATE, OnInventoryOrRoleUpdated);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnInventoryOrRoleUpdated);
            _pending = false;
            ClearConfirmState();
            ArmorController.Instance.RequestInfo(0, 0);
            int token = ++_loadVersion;
            _ = LoadAndRefreshAsync(token);
            RefreshAll();
        }

        protected override void OnHide()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ARMOR_UPDATED, OnArmorUpdated);
            EventDispatcher.Off<uint>(GlobalEvent.EVT_ARMOR_MAKE_RESULT, OnMakeResult);
            EventDispatcher.Off(GlobalEvent.EVT_BAG_UPDATE, OnInventoryOrRoleUpdated);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnInventoryOrRoleUpdated);
            ++_loadVersion;
            _pending = false;
            ClearConfirmState();
            ClearRendered();
        }

        protected override void OnDispose()
        {
            ++_loadVersion;
            ClearRendered();
        }

        private async Task LoadAndRefreshAsync(int token)
        {
            await ArmorConfigs.EnsureLoaded();
            if (token != _loadVersion || !IsShown) return;
            RefreshAll();
        }

        private void OnArmorUpdated()
        {
            RefreshAll();
        }

        private void OnInventoryOrRoleUpdated()
        {
            if (_confirming) ClearConfirmState();
            RefreshAll();
        }

        private void OnMakeResult(uint code)
        {
            _pending = false;
            ClearConfirmState();
            if (code == 1)
            {
                TipsManager.Toast("圣骸打造成功");
                SelectFirstCandidate(preferMakeable: true);
            }
            RefreshAll();
        }

        private void RefreshAll()
        {
            ClearRendered();
            if (!ArmorConfigs.IsLoaded || !ArmorModel.Instance.HasData)
            {
                if (_lb_msg != null) _lb_msg.text = ArmorConfigs.IsLoaded ? "圣骸数据加载中" : "圣骸配置加载中";
                if (_lb_make != null) _lb_make.text = "加载中";
                if (_makeButton != null) _makeButton.interactable = false;
                HideRedDots();
                return;
            }

            EnsureSelection();
            RenderStages();
            RenderTypeHeader();
            RenderPositions();
            RenderSelected();
        }

        private void EnsureSelection()
        {
            if (!_selectionInitialized || ArmorModel.Instance.FindPosition(_selectedStage, _selectedType, _selectedPosition) == null)
                SelectFirstCandidate(preferMakeable: true);
        }

        private void SelectFirstCandidate(bool preferMakeable)
        {
            IReadOnlyList<ArmorModel.StageEntry> stages = ArmorModel.Instance.Stages;
            ArmorModel.PositionEntry fallbackPosition = null;
            byte fallbackStage = 0;
            byte fallbackType = 0;
            for (int i = 0; i < stages.Count; i++)
            {
                ArmorModel.StageEntry stage = stages[i];
                for (int j = 0; j < stage.Types.Count; j++)
                {
                    ArmorModel.TypeEntry type = stage.Types[j];
                    ArmorConfigs.SuitCfg suit = ArmorConfigs.GetSuit(stage.Stage, type.Type);
                    bool unlocked = suit != null && Shenxiao.Module.Core.Role.RoleModel.Instance.HasBaseInfo
                        && Shenxiao.Module.Core.Role.RoleModel.Instance.Level >= suit.OpenLevel;
                    for (int k = 0; k < type.Positions.Count; k++)
                    {
                        ArmorModel.PositionEntry position = type.Positions[k];
                        if (fallbackPosition == null && unlocked)
                        {
                            fallbackStage = stage.Stage;
                            fallbackType = type.Type;
                            fallbackPosition = position;
                        }
                        if (preferMakeable && ArmorConfigs.Preview(stage.Stage, type.Type, position.Position).CanMake)
                        {
                            ApplySelection(stage.Stage, type.Type, position.Position);
                            return;
                        }
                        if (unlocked && position.Status != 1 && fallbackPosition?.Status == 1)
                        {
                            fallbackStage = stage.Stage;
                            fallbackType = type.Type;
                            fallbackPosition = position;
                        }
                    }
                }
            }

            if (fallbackPosition != null) ApplySelection(fallbackStage, fallbackType, fallbackPosition.Position);
            else if (stages.Count > 0 && stages[0].Types.Count > 0 && stages[0].Types[0].Positions.Count > 0)
                ApplySelection(stages[0].Stage, stages[0].Types[0].Type, stages[0].Types[0].Positions[0].Position);
        }

        private void ApplySelection(byte stage, byte type, byte position)
        {
            _selectedStage = stage;
            _selectedType = type;
            _selectedPosition = position;
            _selectionInitialized = true;
        }

        private void SelectStage(byte stage)
        {
            ArmorModel.StageEntry entry = ArmorModel.Instance.FindStage(stage);
            if (entry == null) return;
            ArmorConfigs.SuitCfg suit = entry.Types.Count > 0 ? ArmorConfigs.GetSuit(stage, entry.Types[0].Type) : null;
            if (suit != null && Shenxiao.Module.Core.Role.RoleModel.Instance.Level < suit.OpenLevel)
            {
                TipsManager.Toast(suit.OpenLevel + "级开启");
                return;
            }
            byte type = ArmorModel.Instance.FindType(stage, _selectedType) != null ? _selectedType : (entry.Types.Count > 0 ? entry.Types[0].Type : (byte)1);
            ArmorModel.TypeEntry typeEntry = ArmorModel.Instance.FindType(stage, type);
            byte position = typeEntry != null && typeEntry.Positions.Count > 0 ? typeEntry.Positions[0].Position : (byte)1;
            ApplySelection(stage, type, position);
            ClearConfirmState();
            RefreshAll();
        }

        private void SelectType(byte type)
        {
            ArmorModel.TypeEntry entry = ArmorModel.Instance.FindType(_selectedStage, type);
            if (entry == null || entry.Positions.Count == 0) return;
            ApplySelection(_selectedStage, type, entry.Positions[0].Position);
            ClearConfirmState();
            RefreshAll();
        }

        private void SelectPosition(byte position)
        {
            if (ArmorModel.Instance.FindPosition(_selectedStage, _selectedType, position) == null) return;
            _selectedPosition = position;
            ClearConfirmState();
            RefreshAll();
        }

        private void RenderStages()
        {
            if (_gp_tabs == null || _gp_tabs.content == null || _tpl_ArmorTabItem == null) return;
            IReadOnlyList<ArmorModel.StageEntry> stages = ArmorModel.Instance.Stages;
            for (int i = 0; i < stages.Count; i++)
            {
                ArmorModel.StageEntry stage = stages[i];
                GameObject go = CloneTemplate(_tpl_ArmorTabItem, _gp_tabs.content, "ArmorTabItem_" + stage.Stage);
                ArmorTabItemBind bind = go != null ? go.GetComponent<ArmorTabItemBind>() : null;
                if (bind == null) continue;
                ArmorConfigs.SuitCfg suit = stage.Types.Count > 0 ? ArmorConfigs.GetSuit(stage.Stage, stage.Types[0].Type) : null;
                bool locked = suit != null && Shenxiao.Module.Core.Role.RoleModel.Instance.Level < suit.OpenLevel;
                if (bind._lb_name != null) bind._lb_name.text = locked ? (suit.OpenLevel + "级") : StageName(stage.Stage);
                SetActive(bind._img_final, ArmorModel.Instance.IsStageComplete(stage.Stage));
                SetActive(bind._img_red, !locked && HasStageRed(stage.Stage));
                if (bind._img_bg != null) bind._img_bg.color = stage.Stage == _selectedStage ? Color.white : new Color(0.68f, 0.68f, 0.68f, 1f);
                if (bind._lb_name != null) bind._lb_name.color = stage.Stage == _selectedStage ? new Color32(155, 87, 47, 255) : Color.white;
                DisableRaycasts(go);
                UIUtil.AddClick(bind.gp_con, () => SelectStage(stage.Stage));
            }
        }

        private void RenderTypeHeader()
        {
            bool left = _selectedType == 1;
            if (_lb_left != null) _lb_left.color = left ? new Color32(155, 87, 47, 255) : new Color32(215, 217, 237, 255);
            if (_lb_right != null) _lb_right.color = left ? new Color32(215, 217, 237, 255) : new Color32(155, 87, 47, 255);
            SetActive(_img_red1, HasTypeRed(_selectedStage, 1));
            SetActive(_img_red2, HasTypeRed(_selectedStage, 2));

            ArmorModel.TypeEntry type = ArmorModel.Instance.FindType(_selectedStage, _selectedType);
            int active = 0;
            if (type != null) for (int i = 0; i < type.Positions.Count; i++) if (type.Positions[i].Status == 1) active++;
            if (_lb_msg != null)
            {
                string name = _selectedType == 1 ? "荒陨圣骸" : "天殒圣骸";
                bool complete = type != null && type.Status == 1;
                _lb_msg.text = name + "套装 " + active + "/" + (type?.Positions.Count ?? 0)
                    + (complete ? " <color=#6CBF25>[已激活]</color>" : " <color=#898989>[未激活]</color>");
            }

            IReadOnlyList<ArmorConfigs.AttrItem> attrs = ArmorConfigs.GetSuitAttributes(_selectedStage, _selectedType);
            var leftText = new StringBuilder();
            var rightText = new StringBuilder();
            for (int i = 0; i < attrs.Count; i++)
            {
                ArmorConfigs.AttrItem attr = attrs[i];
                string line = AttrName(attr.AttrId) + ": <color=#D15E00>" + GoodsModel.FormatAttrValue(attr.AttrId, attr.Value) + "</color>";
                StringBuilder target = (i & 1) == 0 ? leftText : rightText;
                if (target.Length > 0) target.Append('\n');
                target.Append(line);
            }
            if (lb_left != null) lb_left.text = leftText.ToString();
            if (lb_right != null) lb_right.text = rightText.ToString();
        }

        private void RenderPositions()
        {
            if (_gp_equips == null || _tpl_ArmorItem == null) return;
            ArmorModel.TypeEntry type = ArmorModel.Instance.FindType(_selectedStage, _selectedType);
            if (type == null) return;
            for (int i = 0; i < type.Positions.Count; i++)
            {
                ArmorModel.PositionEntry position = type.Positions[i];
                GameObject go = CloneTemplate(_tpl_ArmorItem, _gp_equips, "ArmorItem_" + position.Position);
                ArmorItemBind bind = go != null ? go.GetComponent<ArmorItemBind>() : null;
                if (bind == null) continue;
                ArmorConfigs.EquipmentCfg cfg = ArmorConfigs.GetEquipment(unchecked((int)position.GTypeId));
                ArmorConfigs.PreviewResult preview = ArmorConfigs.Preview(_selectedStage, _selectedType, position.Position);
                if (bind._lb_tips != null) bind._lb_tips.text = cfg == null ? position.GTypeId.ToString() : StageName(cfg.Stage);
                SetActive(bind._img_final, position.Status == 1);
                SetActive(bind._img_select, position.Position == _selectedPosition);
                SetActive(bind._img_red, preview.CanMake);
                SetActive(bind._img_lock, preview.Block == ArmorConfigs.MakeBlock.LevelLocked || preview.Block == ArmorConfigs.MakeBlock.MissingPreviousStage);
                DisableRaycasts(go);
                UIUtil.AddClick(bind.gp_con, () => SelectPosition(position.Position));
                _ = AttachAwardAsync(bind._gp_item, unchecked((int)position.GTypeId), 1, 0.7f, _renderVersion);
            }
        }

        private void RenderSelected()
        {
            ArmorModel.PositionEntry position = ArmorModel.Instance.FindPosition(_selectedStage, _selectedType, _selectedPosition);
            if (position == null) return;
            ArmorConfigs.PreviewResult preview = ArmorConfigs.Preview(_selectedStage, _selectedType, _selectedPosition);
            bool made = position.Status == 1;
            if (_lb_make != null) _lb_make.text = _pending ? "处理中" : (made ? "已打造" : "打造");
            SetActive(_img_red, preview.CanMake && !_pending);
            if (_makeButton != null) _makeButton.interactable = preview.CanMake && !_confirming && !_pending;

            _ = AttachAwardAsync(_gp_curr, unchecked((int)position.GTypeId), 1, 0.78f, _renderVersion);
            RenderEquipmentAttributes(position, made);
            RenderMaterialSlots(preview);
        }

        private void RenderEquipmentAttributes(ArmorModel.PositionEntry position, bool made)
        {
            if (Content2 == null || _tpl_ArmorAttrItem == null) return;
            IReadOnlyList<ArmorConfigs.AttrItem> attrs = ArmorConfigs.GetEquipmentAttributes(unchecked((int)position.GTypeId));
            for (int i = 0; i < attrs.Count; i++)
            {
                ArmorConfigs.AttrItem data = attrs[i];
                GameObject go = CloneTemplate(_tpl_ArmorAttrItem, Content2, "ArmorAttr_" + data.AttrId);
                ArmorAttrItemBind bind = go != null ? go.GetComponent<ArmorAttrItemBind>() : null;
                if (bind == null) continue;
                if (bind.attr != null) bind.attr.text = AttrName(data.AttrId) + ": " + (made ? GoodsModel.FormatAttrValue(data.AttrId, data.Value) : "0");
                if (bind.up != null)
                {
                    bind.up.text = made ? string.Empty : ("+" + GoodsModel.FormatAttrValue(data.AttrId, data.Value));
                    bind.up.color = new Color32(10, 149, 62, 255);
                }
                DisableRaycasts(go);
            }
        }

        private void RenderMaterialSlots(ArmorConfigs.PreviewResult preview)
        {
            if (_gp_mat == null || _tpl_ArmorItem == null) return;
            for (int i = 0; i < MaterialPositions.Length; i++)
            {
                GameObject go = CloneTemplate(_tpl_ArmorItem, _gp_mat, "ArmorMaterial_" + i);
                ArmorItemBind bind = go != null ? go.GetComponent<ArmorItemBind>() : null;
                if (bind == null) continue;
                RectTransform rect = go.transform as RectTransform;
                if (rect != null) rect.anchoredPosition = MaterialPositions[i];
                bool has = preview != null && i < preview.DisplayCosts.Count;
                SetActive(bind._img_lock, !has);
                SetActive(bind._img_select, false);
                SetActive(bind._img_final, false);
                SetActive(bind._img_red, false);
                if (!has)
                {
                    if (bind._gp_item != null) bind._gp_item.gameObject.SetActive(false);
                    if (bind._lb_tips != null) bind._lb_tips.text = string.Empty;
                    DisableRaycasts(go);
                    continue;
                }

                ArmorConfigs.CostItem cost = preview.DisplayCosts[i];
                long have = cost.IsArmorState ? (ArmorConfigs.IsArmorStateAvailable(cost.TypeId) ? 1L : 0L) : BagModel.Instance.GetTypeGoodsNum(cost.TypeId);
                if (bind._lb_tips != null)
                {
                    bind._lb_tips.text = have + "/" + cost.Num;
                    bind._lb_tips.color = have >= cost.Num ? Color.white : new Color32(255, 71, 71, 255);
                }
                DisableRaycasts(go);
                _ = AttachAwardAsync(bind._gp_item, cost.TypeId, 1, 0.7f, _renderVersion);
            }
        }

        private void OnMakeClick()
        {
            if (_confirming || _pending) return;
            ArmorConfigs.PreviewResult preview = ArmorConfigs.Preview(_selectedStage, _selectedType, _selectedPosition);
            if (!preview.CanMake)
            {
                TipsManager.Toast(ArmorConfigs.GetBlockText(preview));
                RefreshAll();
                return;
            }

            _confirming = true;
            _confirmModelVersion = ArmorModel.Instance.Version;
            _confirmStage = _selectedStage;
            _confirmType = _selectedType;
            _confirmPosition = _selectedPosition;
            _confirmEquipmentId = preview.Equipment.Id;
            _confirmFingerprint = ArmorConfigs.BuildFingerprint(preview);
            LastConfirmText = BuildConfirmText(preview);
            if (_makeButton != null) _makeButton.interactable = false;
            TipsManager.Confirm(LastConfirmText, ConfirmMake, CancelMake);
        }

        private void ConfirmMake()
        {
            if (!_confirming || _pending) return;
            ArmorConfigs.PreviewResult preview = ArmorConfigs.Preview(_confirmStage, _confirmType, _confirmPosition);
            bool valid = _selectedStage == _confirmStage && _selectedType == _confirmType && _selectedPosition == _confirmPosition
                && ArmorModel.Instance.Version == _confirmModelVersion && preview.CanMake && preview.Equipment != null
                && preview.Equipment.Id == _confirmEquipmentId && ArmorConfigs.BuildFingerprint(preview) == _confirmFingerprint;
            byte stage = _confirmStage;
            byte type = _confirmType;
            byte position = _confirmPosition;
            ClearConfirmState();
            if (!valid)
            {
                TipsManager.Toast("圣骸状态或材料已变化，请重新确认");
                RefreshAll();
                return;
            }
            _pending = true;
            RefreshAll();
            ArmorController.Instance.RequestMake(stage, type, position);
        }

        private void CancelMake()
        {
            if (!_confirming) return;
            ClearConfirmState();
            RefreshAll();
        }

        private void ClearConfirmState()
        {
            _confirming = false;
            _confirmModelVersion = 0;
            _confirmStage = 0;
            _confirmType = 0;
            _confirmPosition = 0;
            _confirmEquipmentId = 0;
            _confirmFingerprint = string.Empty;
        }

        private static string BuildConfirmText(ArmorConfigs.PreviewResult preview)
        {
            var sb = new StringBuilder("是否消耗以下材料打造圣骸？");
            for (int i = 0; i < preview.RealCosts.Count; i++)
            {
                ArmorConfigs.CostItem cost = preview.RealCosts[i];
                string name = GoodsModel.GetGoodsName(cost.TypeId);
                sb.Append('\n').Append(string.IsNullOrEmpty(name) ? cost.TypeId.ToString() : name).Append('×').Append(cost.Num);
            }
            return sb.ToString();
        }

        private bool HasStageRed(byte stage)
        {
            ArmorModel.StageEntry entry = ArmorModel.Instance.FindStage(stage);
            if (entry == null) return false;
            for (int i = 0; i < entry.Types.Count; i++) if (HasTypeRed(stage, entry.Types[i].Type)) return true;
            return false;
        }

        private bool HasTypeRed(byte stage, byte type)
        {
            ArmorModel.TypeEntry entry = ArmorModel.Instance.FindType(stage, type);
            if (entry == null) return false;
            for (int i = 0; i < entry.Positions.Count; i++)
                if (ArmorConfigs.Preview(stage, type, entry.Positions[i].Position).CanMake) return true;
            return false;
        }

        private GameObject CloneTemplate(GameObject template, Transform parent, string name)
        {
            if (template == null || parent == null) return null;
            GameObject go = Instantiate(template, parent, false);
            go.name = name;
            go.SetActive(true);
            _rendered.Add(go);
            return go;
        }

        private async Task AttachAwardAsync(Transform parent, int typeId, long num, float scale, int token)
        {
            if (parent == null || typeId <= 0) return;
            string key = GameResPath.GetUIPrefab("common", "BaseAwardItem");
            GameObject go = await ResManager.InstantiateAsync(key, parent);
            if (go == null) return;
            if (token != _renderVersion || !IsShown || parent == null)
            {
                ResManager.ReleaseInstance(go);
                return;
            }
            go.name = "ArmorAward_" + typeId;
            go.SetActive(true);
            BaseAwardItem item = go.GetComponent<BaseAwardItem>();
            if (item == null) item = go.GetComponentInChildren<BaseAwardItem>(true);
            if (item != null)
            {
                item.SetData(typeId, num);
                item.SetScale(scale);
            }
            _awardInstances.Add(go);
        }

        private void ClearRendered()
        {
            _renderVersion++;
            for (int i = 0; i < _awardInstances.Count; i++)
                if (_awardInstances[i] != null) ResManager.ReleaseInstance(_awardInstances[i]);
            _awardInstances.Clear();
            for (int i = 0; i < _rendered.Count; i++)
            {
                GameObject go = _rendered[i];
                if (go == null) continue;
                if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
            }
            _rendered.Clear();
        }

        private void HideRedDots()
        {
            SetActive(_img_red, false);
            SetActive(_img_red1, false);
            SetActive(_img_red2, false);
        }

        private static Button BindContainerButton(Component target, Action action)
        {
            if (target == null) return null;
            DisableRaycasts(target.gameObject);
            UIUtil.AddClick(target, action);
            return target.GetComponent<Button>();
        }

        private static void BindImageButton(Image target, Action action)
        {
            if (target == null) return;
            foreach (Graphic graphic in target.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = graphic == target;
            UIUtil.ClearClicks(target);
            UIUtil.AddClick(target, action);
        }

        private static void DisableRaycasts(GameObject root)
        {
            if (root == null) return;
            foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        }

        private static void SetRaycast(Graphic graphic, bool value)
        {
            if (graphic != null) graphic.raycastTarget = value;
        }

        private static void SetActive(Component component, bool value)
        {
            if (component != null) component.gameObject.SetActive(value);
        }

        private static string AttrName(int attrId)
        {
            string name = GoodsModel.GetAttrName(attrId);
            return string.IsNullOrEmpty(name) ? ("属性" + attrId) : name;
        }

        private static string StageName(byte stage)
        {
            string[] names = { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九", "十" };
            return stage < names.Length ? (names[stage] + "阶") : (stage + "阶");
        }
    }
}
