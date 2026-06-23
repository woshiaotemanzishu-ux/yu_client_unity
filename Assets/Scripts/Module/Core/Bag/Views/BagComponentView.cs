using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.Proto;
using Shenxiao.Common.UI3D;
using Shenxiao.Generated.UI.Bag;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.Role;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// Main bag panel. It renders a fixed slot grid first, then overlays real BagModel items
    /// by their server cell index. Empty slots are part of the runtime UI and must stay visible
    /// even before 15010 bag data arrives.
    /// </summary>
    public sealed class BagComponentView : BagComponentViewBind
    {
        private const float Cell = 86f;
        private const float Gap = 10f;
        private const int DefaultVisibleCells = 24;
        private const int EquipmentSlotCount = 10;
        private const float EquipmentSlotStep = 113f;
        private const float ModelScale = 0.78f;

        private BagItemRenderer _itemTemplate;
        private readonly List<GameObject> _cells = new List<GameObject>();
        private readonly List<BagEquipmentIcon> _equipmentSlots = new List<BagEquipmentIcon>();
        private FightingShowSmallItem _fightingItem;
        private bool _subscribed;
        private int _modelRequestId;

        protected override void OnInit()
        {
            HideReds();
            HideTemplates();
            BindButtons();
        }

        public void SetItemTemplate(BagItemRenderer template) => _itemTemplate = template;

        protected override void OnShow(object args)
        {
            BuildEquipmentSlots();
            BuildGrid();
            EnsureFightingItem();
            RefreshRoleInfo();
            ShowRoleModel();
            Subscribe();
        }

        protected override void OnHide()
        {
            Unsubscribe();
            _modelRequestId++;
            UIModelStage.Clear();
        }

        protected override void OnDispose()
        {
            Unsubscribe();
            _modelRequestId++;
            UIModelStage.Clear();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            _modelRequestId++;
            UIModelStage.Clear();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On(GlobalEvent.EVT_BAG_UPDATE, BuildGrid);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off(GlobalEvent.EVT_BAG_UPDATE, BuildGrid);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            _subscribed = false;
        }

        private void OnRoleInfoUpdate()
        {
            RefreshRoleInfo();
            if (gameObject.activeInHierarchy) ShowRoleModel();
        }

        private void EnsureFightingItem()
        {
            if (_fightingItem != null || _tpl_FightingShowSmallItem == null) return;

            RectTransform parent = transform as RectTransform;
            if (parent == null) return;

            GameObject go = Instantiate(_tpl_FightingShowSmallItem, parent);
            go.name = "FightingShowSmallItem_Runtime";
            var rt = go.transform as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, -84f);
                rt.localScale = Vector3.one;
            }

            _fightingItem = go.GetComponent<FightingShowSmallItem>();
            go.SetActive(true);
        }

        private void RefreshRoleInfo()
        {
            RoleModel role = RoleModel.Instance;
            bool hasInfo = role.HasBaseInfo;
            if (nameLb != null)
            {
                nameLb.gameObject.SetActive(true);
                nameLb.text = hasInfo ? role.Name : string.Empty;
            }
            if (_fightingItem != null) _fightingItem.SetFighting(hasInfo ? role.CombatPower : 0L);
        }

        private async void ShowRoleModel()
        {
            int requestId = ++_modelRequestId;
            RoleModel role = RoleModel.Instance;
            RoleModelSpec spec = await BuildRoleModelSpecAsync(role);
            if (spec == null)
            {
                if (requestId == _modelRequestId) UIModelStage.Clear();
                return;
            }

            GameObject model = await RoleModelAssembler.BuildAsync(spec);
            if (model == null) return;
            if (requestId != _modelRequestId || this == null || !gameObject.activeInHierarchy)
            {
                Destroy(model);
                return;
            }

            int sex = role.Figure != null ? role.Figure.sex : 0;
            UIModelStage.ShowInstance(modelGp, model, ModelScale,
                LoginConfigs.GetModelPos("SelectRole", spec.Career, sex));
        }

        private static async Task<RoleModelSpec> BuildRoleModelSpecAsync(RoleModel model)
        {
            if (model == null || !model.HasBaseInfo || model.Figure == null) return null;

            await LoginConfigs.EnsureLoaded();
            FigureProto figure = model.Figure;
            int career = figure.career;
            int sex = figure.sex;
            LoginConfigs.CareerRes defaults = LoginConfigs.GetCreateRes(career, sex);
            int clothe = figure.ClotheModelId > 0 ? figure.ClotheModelId : (defaults != null ? defaults.RoleRes : 0);
            if (clothe <= 0) return null;

            return new RoleModelSpec
            {
                Career = career,
                ClotheRes = clothe,
                HeadRes = figure.HeadModelId > 0 ? figure.HeadModelId : (defaults != null ? defaults.HeadRes : 0),
                WeaponRes = figure.WeaponModelId > 0 ? figure.WeaponModelId : (defaults != null ? defaults.WeaponRes : 0),
                WingId = figure.WingId,
                BackOrnamentId = figure.BackOrnamentId,
                Actions = LoginConfigs.RoleUIActions("BagComponentView"),
            };
        }

        private void BuildGrid()
        {
            ClearCells();

            if (_itemTemplate == null)
            {
                GameLog.Warn("Bag", "Bag item template was not injected by BagFlow; grid cannot render.");
                return;
            }
            if (bag_con == null || bag_con.content == null)
            {
                GameLog.Warn("Bag", "bag_con/content missing; grid cannot render.");
                return;
            }

            RectTransform content = bag_con.content;
            List<BagGoods> goods = BagModel.Instance.BagGoodsList;
            float viewW = bag_con.viewport != null ? bag_con.viewport.rect.width : 580f;
            if (viewW <= 1f) viewW = 580f;

            int cols = Mathf.Max(1, Mathf.FloorToInt((viewW + Gap) / (Cell + Gap)));
            int slotCount = ResolveSlotCount(goods);
            BagGoods[] slots = BuildSlots(goods, slotCount);

            for (int i = 0; i < slotCount; i++)
            {
                GameObject cellGo = Instantiate(_itemTemplate.gameObject, content);
                cellGo.SetActive(true);

                var rt = (RectTransform)cellGo.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                ApplyCellScale(rt);
                int col = i % cols;
                int row = i / cols;
                rt.anchoredPosition = new Vector2(col * (Cell + Gap), -row * (Cell + Gap));

                BagGoods vo = slots[i];
                var renderer = cellGo.GetComponent<BagItemRenderer>();
                if (renderer != null)
                {
                    renderer.SetData(vo != null ? new BagItemData { TypeId = vo.TypeId, Count = vo.GoodsNum, Goods = vo } : null);
                }
                _cells.Add(cellGo);
            }

            int rows = Mathf.CeilToInt(slotCount / (float)cols);
            content.sizeDelta = new Vector2(content.sizeDelta.x, rows * (Cell + Gap) + Gap);
            GameLog.Info("Bag", "grid slots={0} goods={1} cols={2} rows={3} hasData={4}",
                slotCount, goods.Count, cols, rows, BagModel.Instance.HasData);
        }

        private static void ApplyCellScale(RectTransform rt)
        {
            float sourceWidth = rt.rect.width;
            if (sourceWidth <= 1f) sourceWidth = rt.sizeDelta.x;
            if (sourceWidth <= 1f) return;
            float scale = Cell / sourceWidth;
            rt.localScale = new Vector3(scale, scale, 1f);
        }

        private static int ResolveSlotCount(List<BagGoods> goods)
        {
            int slotCount = BagModel.Instance.HasData
                ? Mathf.Max(BagModel.Instance.MaxCell, goods != null ? goods.Count : 0)
                : DefaultVisibleCells;
            slotCount = Mathf.Max(slotCount, DefaultVisibleCells);

            if (goods != null)
            {
                for (int i = 0; i < goods.Count; i++)
                {
                    if (goods[i] != null && goods[i].Cell > slotCount) slotCount = goods[i].Cell;
                }
            }
            return slotCount;
        }

        private static BagGoods[] BuildSlots(List<BagGoods> goods, int slotCount)
        {
            var slots = new BagGoods[slotCount];
            if (goods == null || goods.Count == 0) return slots;

            var overflow = new List<BagGoods>();
            for (int i = 0; i < goods.Count; i++)
            {
                BagGoods vo = goods[i];
                int index = vo != null ? vo.Cell - 1 : -1;
                if (index >= 0 && index < slots.Length && slots[index] == null)
                {
                    slots[index] = vo;
                }
                else if (vo != null)
                {
                    overflow.Add(vo);
                }
            }

            int cursor = 0;
            for (int i = 0; i < overflow.Count; i++)
            {
                while (cursor < slots.Length && slots[cursor] != null) cursor++;
                if (cursor >= slots.Length) break;
                slots[cursor] = overflow[i];
            }
            return slots;
        }

        private void ClearCells()
        {
            for (int i = 0; i < _cells.Count; i++)
            {
                if (_cells[i] != null) Destroy(_cells[i]);
            }
            _cells.Clear();
        }

        private void BuildEquipmentSlots()
        {
            if (_equipmentSlots.Count > 0) return;
            if (_tpl_BagEquipmentIcon == null || leftGp == null || rightGp == null)
            {
                GameLog.Warn("Bag", "equipment slot template or containers missing.");
                return;
            }

            for (int pos = 1; pos <= EquipmentSlotCount; pos++)
            {
                RectTransform parent = pos % 2 == 0 ? rightGp : leftGp;
                int row = (pos - 1) / 2;
                GameObject go = Instantiate(_tpl_BagEquipmentIcon, parent);
                go.name = "BagEquipmentIcon_" + pos;
                go.SetActive(true);

                var rt = go.transform as RectTransform;
                if (rt != null)
                {
                    rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                    rt.anchoredPosition = new Vector2(0f, -row * EquipmentSlotStep);
                    rt.localScale = Vector3.one;
                }

                BagEquipmentIcon icon = go.GetComponent<BagEquipmentIcon>();
                if (icon != null)
                {
                    icon.Show();
                    icon.SetEmpty(true);
                    _equipmentSlots.Add(icon);
                }
            }
            GameLog.Info("Bag", "equipment slots built: {0}", _equipmentSlots.Count);
        }

        private void HideReds()
        {
            HideNode(suitRed);
            HideNode(guard1_red);
            HideNode(guard2_red);
            HideNode(dragonball_red);
            HideNode(red_quick);
            HideNode(useRed);
            HideNode(smeltRed);
        }

        private void HideTemplates()
        {
            if (_tpl_BagEquipmentIcon != null) _tpl_BagEquipmentIcon.SetActive(false);
            if (_tpl_FightingShowSmallItem != null) _tpl_FightingShowSmallItem.SetActive(false);
        }

        private void BindButtons()
        {
            BindToggle(onekeyBtn, "OneKeyUseView", "one key use");
            BindToggle(smeltBtn, "BagSmeltView", "smelt");
            BindToggle(expandBtn, "ExpandBagView", "expand bag");
            BindBtn(redequipBtn, "red equip");
            BindBtn(useBtn, "use");
            BindBtn(_btn_guard1, "guard 1");
            BindBtn(_btn_guard2, "guard 2");
            BindBtn(_btn_dragonball, "dragon ball");
        }

        private void BindToggle(Component target, string viewType, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () =>
            {
                GameLog.Info("Bag", "click bag button [{0}] -> toggle {1}", label, viewType);
                BagFlow.ToggleSub(viewType);
            });
        }

        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("Bag", "click bag button [{0}] -> TODO", label));
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
