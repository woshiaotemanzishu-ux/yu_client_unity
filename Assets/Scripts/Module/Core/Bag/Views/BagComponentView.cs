using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.Proto;
using Shenxiao.Common.Tips;
using Shenxiao.Common.UI3D;
using Shenxiao.Generated.UI.Bag;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Equip;
using Shenxiao.Module.Core.FirstRecharge;
using Shenxiao.Module.Core.Game;
using Shenxiao.Module.Core.Guard;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Resonance;
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
        private const float ModelScale = 0.78f;
        private const int LockedTailCells = 18;
        private const int LegacyColumns = 6;
        private const int OneKeyWearRedMaxLevel = 210;

        private BagItemRenderer _itemTemplate;
        // 虚拟化格子池:只实例化「可视行数 + 缓冲」个 cell,滚动/数据更新时原地重绑数据,
        // 不再按背包容量整表 Instantiate(200 格背包一帧 400 次实例化 = 打开即卡死的元凶)。
        private readonly List<BagItemRenderer> _cellPool = new List<BagItemRenderer>();
        private BagGoods[] _slots = System.Array.Empty<BagGoods>();
        private int _slotCount;
        private int _unlockedSlotCount;
        private int _cols = 1;
        private int _firstVisibleRow = -1;
        private bool _scrollHooked;
        private readonly List<BagEquipmentIcon> _equipmentSlots = new List<BagEquipmentIcon>();
        private FightingShowSmallItem _fightingItem;
        private bool _subscribed;
        private int _modelRequestId;
        private int _conditionalVisualRequestId;
        private int _guardType1 = 1;
        private int _guardType2 = 2;

        protected override void OnInit()
        {
            HideReds();
            HideTemplates();
            BindButtons();
            ConfigureOwnedRedProviders();
        }

        public void SetItemTemplate(BagItemRenderer template) => _itemTemplate = template;

        protected override void OnShow(object args)
        {
            BagFlow.ApplyWindowTitlePresentation(0);
            Subscribe();
            BuildEquipmentSlots();
            RefreshEquipmentSlots();
            BuildGrid();
            EnsureFightingItem();
            RefreshRoleInfo();
            RefreshConditionalBlocks();
            RefreshPageReds();
            ShowRoleModel();
            _ = RefreshAfterGoodsConfigAsync();
        }

        protected override void OnHide()
        {
            Unsubscribe();
            StopGridScroll();
            _modelRequestId++;
            _conditionalVisualRequestId++;
            UIModelStage.Clear();
        }

        protected override void OnDispose()
        {
            Unsubscribe();
            _modelRequestId++;
            _conditionalVisualRequestId++;
            UIModelStage.Clear();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            _modelRequestId++;
            _conditionalVisualRequestId++;
            UIModelStage.Clear();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On(GlobalEvent.EVT_BAG_UPDATE, BuildGrid);
            EventDispatcher.On(GlobalEvent.EVT_EQUIPMENT_UPDATE, RefreshEquipmentSlots);
            EventDispatcher.On(GlobalEvent.EVT_EQUIP_SUIT_UPDATE, RefreshEquipmentSlots);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.On(GlobalEvent.EVT_FIRST_RECHARGE_UPDATE, RefreshConditionalBlocks);
            EventDispatcher.On(GlobalEvent.EVT_BAG_UPDATE, RefreshPageReds);
            EventDispatcher.On(GlobalEvent.EVT_BAG_UPDATE, RefreshConditionalBlocks);
            EventDispatcher.On(GlobalEvent.EVT_EQUIPMENT_UPDATE, RefreshPageReds);
            BagMainRedStateProvider.Instance.Changed += ApplyPageRedSnapshot;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off(GlobalEvent.EVT_BAG_UPDATE, BuildGrid);
            EventDispatcher.Off(GlobalEvent.EVT_EQUIPMENT_UPDATE, RefreshEquipmentSlots);
            EventDispatcher.Off(GlobalEvent.EVT_EQUIP_SUIT_UPDATE, RefreshEquipmentSlots);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_FIRST_RECHARGE_UPDATE, RefreshConditionalBlocks);
            EventDispatcher.Off(GlobalEvent.EVT_BAG_UPDATE, RefreshPageReds);
            EventDispatcher.Off(GlobalEvent.EVT_BAG_UPDATE, RefreshConditionalBlocks);
            EventDispatcher.Off(GlobalEvent.EVT_EQUIPMENT_UPDATE, RefreshPageReds);
            BagMainRedStateProvider.Instance.Changed -= ApplyPageRedSnapshot;
            _subscribed = false;
        }

        private void OnRoleInfoUpdate()
        {
            RefreshRoleInfo();
            RefreshConditionalBlocks();
            RefreshPageReds();
            if (gameObject.activeInHierarchy) ShowRoleModel();
        }

        /// <summary>对标老端 BagComponentView：守护与龙珠是条件块，未开放时不生成可见入口。</summary>
        private void RefreshConditionalBlocks()
        {
            bool guardOpen = FuncOpenConfig.CheckFuncOpenState("GuardMainView");
            bool dragonBallOpen = !PlatformModel.IsAlpha
                                  && FuncOpenConfig.CheckFuncOpenState("DragonBallView");
            if (_gp_guard1 != null) _gp_guard1.gameObject.SetActive(guardOpen);
            if (_gp_guard2 != null) _gp_guard2.gameObject.SetActive(guardOpen);
            if (_gp_dragonball != null) _gp_dragonball.gameObject.SetActive(dragonBallOpen);
            _ = RefreshConditionalVisualsAsync(guardOpen, dragonBallOpen);
        }

        private async Task RefreshConditionalVisualsAsync(bool guardOpen, bool dragonBallOpen)
        {
            int requestId = ++_conditionalVisualRequestId;
            ResolveGuardPresentation(out int type1, out bool active1, out int type2, out bool active2);
            _guardType1 = type1;
            _guardType2 = type2;

            if (guardOpen)
            {
                if (_btn_guard1 != null)
                {
                    _btn_guard1.enabled = true;
                    _btn_guard1.color = active1 ? Color.white : new Color(0.45f, 0.45f, 0.45f, 1f);
                    await ResManager.SetImageAsync(_btn_guard1, GameResPath.GetIcon("bag", "guard" + type1), false, false);
                }
                if (requestId != _conditionalVisualRequestId || !IsShown) return;
                if (_btn_guard2 != null)
                {
                    _btn_guard2.enabled = true;
                    _btn_guard2.color = active2 ? Color.white : new Color(0.45f, 0.45f, 0.45f, 1f);
                    await ResManager.SetImageAsync(_btn_guard2, GameResPath.GetIcon("bag", "guard" + type2), false, false);
                }
            }
            if (requestId != _conditionalVisualRequestId || !IsShown) return;

            if (dragonBallOpen && _btn_dragonball != null)
            {
                bool firstRechargeDone = FirstRechargeModel.Instance.IsDoneFirstRecharge();
                _btn_dragonball.enabled = true;
                _btn_dragonball.color = firstRechargeDone
                    ? Color.white
                    : new Color(0.45f, 0.45f, 0.45f, 1f);
                await ResManager.SetImageAsync(_btn_dragonball,
                    GameResPath.GetIcon("bag", "dragon_ball"), false, false);
            }
        }

        private static void ResolveGuardPresentation(
            out int type1, out bool active1, out int type2, out bool active2)
        {
            type1 = 1;
            type2 = 2;
            active1 = false;
            active2 = false;
            IReadOnlyList<GuardModel.Circle> circles = GuardModel.Instance.Circles;
            for (int i = 0; i < circles.Count; i++)
            {
                GuardModel.Circle circle = circles[i];
                if (circle == null || circle.Status != 1) continue;
                if (circle.Level == 1 || circle.Level == 3)
                {
                    type1 = circle.Level;
                    active1 = true;
                }
                else if (circle.Level == 2 || circle.Level == 4)
                {
                    type2 = circle.Level;
                    active2 = true;
                }
            }

            if (BagModel.Instance.GetTypeGoodsNum(38040055) > 0)
            {
                type1 = 3;
                active1 = false;
            }
            if (BagModel.Instance.GetTypeGoodsNum(38040059) > 0)
            {
                type2 = 4;
                active2 = false;
            }
        }

        /// <summary>登录时 15010 可能早于 config_goods；配置到齐后重绑一次，恢复真实图标和 equip_type 槽位。</summary>
        private async Task RefreshAfterGoodsConfigAsync()
        {
            await Task.WhenAll(GoodsModel.EnsureLoaded(), ResonanceConfigs.EnsureLoaded(), FuncOpenConfig.EnsureLoaded());
            if (!IsShown) return;
            RefreshEquipmentSlots();
            BuildGrid();
            RefreshConditionalBlocks();
            RefreshPageReds();
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
                ClotheChartletId = figure.ClotheChartletId,
                HeadRes = figure.HeadModelId > 0 ? figure.HeadModelId : (defaults != null ? defaults.HeadRes : 0),
                HeadChartletId = figure.HeadChartletId,
                WeaponRes = figure.WeaponModelId > 0 ? figure.WeaponModelId : (defaults != null ? defaults.WeaponRes : 0),
                WingId = figure.WingId,
                BackOrnamentId = figure.BackOrnamentId,
                Actions = LoginConfigs.RoleUIActions("BagComponentView"),
            };
        }

        private void BuildGrid()
        {
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

            _cols = Mathf.Max(1, Mathf.FloorToInt((viewW + Gap) / (Cell + Gap)));
            _unlockedSlotCount = ResolveSlotCount(goods);
            _slotCount = _unlockedSlotCount + LockedTailCells - _unlockedSlotCount % LegacyColumns;
            _slots = BuildSlots(goods, _unlockedSlotCount);

            int rows = Mathf.CeilToInt(_slotCount / (float)_cols);
            content.sizeDelta = new Vector2(content.sizeDelta.x, rows * (Cell + Gap) + Gap);
            EnsureCellPool();
            HookScroll();
            RefreshVisibleCells(force: true);

            GameLog.Info("Bag", "grid slots={0} goods={1} cols={2} rows={3} pool={4} hasData={5}",
                _slotCount, goods.Count, _cols, rows, _cellPool.Count, BagModel.Instance.HasData);
        }

        /// <summary>池容量 = 可视行数 + 2 行缓冲(只增不减;视口尺寸未就绪时按 640 兜底)。</summary>
        private void EnsureCellPool()
        {
            float viewH = bag_con.viewport != null ? bag_con.viewport.rect.height : 0f;
            if (viewH <= 1f) viewH = 640f;
            int poolRows = Mathf.CeilToInt(viewH / (Cell + Gap)) + 2;
            int need = poolRows * _cols;

            RectTransform content = bag_con.content;
            while (_cellPool.Count < need)
            {
                GameObject cellGo = Instantiate(_itemTemplate.gameObject, content);
                cellGo.name = "BagCell_" + _cellPool.Count;
                var rt = (RectTransform)cellGo.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                ApplyCellScale(rt);
                _cellPool.Add(cellGo.GetComponent<BagItemRenderer>());
            }
        }

        private void HookScroll()
        {
            if (_scrollHooked || bag_con == null) return;
            _scrollHooked = true;
            bag_con.onValueChanged.AddListener(_ => RefreshVisibleCells(force: false));
        }

        private void StopGridScroll()
        {
            if (bag_con == null) return;
            bag_con.StopMovement();
            bag_con.velocity = Vector2.zero;
        }

        /// <summary>把池内 cell 绑到当前可视窗口对应的槽位;首行未变时整帧 early-out(对齐 SceneMapView 瓦片池思路)。</summary>
        private void RefreshVisibleCells(bool force)
        {
            if (bag_con == null || bag_con.content == null || _cellPool.Count == 0) return;

            float scrollY = Mathf.Max(0f, bag_con.content.anchoredPosition.y);
            int totalRows = Mathf.CeilToInt(_slotCount / (float)_cols);
            int poolRows = _cellPool.Count / Mathf.Max(1, _cols);
            int firstRow = Mathf.FloorToInt(scrollY / (Cell + Gap));
            firstRow = Mathf.Clamp(firstRow, 0, Mathf.Max(0, totalRows - poolRows));
            if (!force && firstRow == _firstVisibleRow) return;
            _firstVisibleRow = firstRow;

            for (int i = 0; i < _cellPool.Count; i++)
            {
                BagItemRenderer cell = _cellPool[i];
                if (cell == null) continue;
                int col = i % _cols;
                int rowOffset = i / _cols;
                int slotIndex = (firstRow + rowOffset) * _cols + col;
                if (slotIndex >= _slotCount)
                {
                    cell.gameObject.SetActive(false);
                    continue;
                }

                var rt = (RectTransform)cell.transform;
                rt.anchoredPosition = new Vector2(col * (Cell + Gap), -(firstRow + rowOffset) * (Cell + Gap));
                cell.gameObject.SetActive(true);
                if (slotIndex >= _unlockedSlotCount)
                {
                    int initialCount = slotIndex - _unlockedSlotCount + 1;
                    cell.SetData(new BagItemData
                    {
                        Locked = true,
                        Click = () => BagFlow.OpenSub("ExpandBagView", new ExpandBagView.Presentation
                        {
                            BagPos = BagModel.POS_BAG,
                            InitialCount = initialCount,
                        }),
                    });
                    continue;
                }

                BagGoods vo = _slots[slotIndex];
                cell.SetData(vo != null ? new BagItemData { TypeId = vo.TypeId, Count = vo.GoodsNum, Goods = vo } : null);
            }
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
                GameObject go = Instantiate(_tpl_BagEquipmentIcon, parent);
                go.name = "BagEquipmentIcon_" + pos;
                go.SetActive(true);

                BagEquipmentIcon icon = go.GetComponent<BagEquipmentIcon>();
                if (icon != null)
                {
                    icon.Show();
                    icon.SetEquipPosition(pos);
                    _equipmentSlots.Add(icon);
                }
            }
            GameLog.Info("Bag", "equipment slots built: {0}", _equipmentSlots.Count);
        }

        private void RefreshEquipmentSlots()
        {
            for (int i = 0; i < _equipmentSlots.Count; i++)
                _equipmentSlots[i]?.SetData(BagModel.Instance.GetEquipmentAt(i + 1));
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

        /// <summary>
        /// 只注册 Bag 自身可由当前权威快照推导的三类红点。普通使用冷却、守护、龙珠及三个特殊装备页
        /// 均依赖外部模块状态，保持 unknown，禁止把“暂时无消费者”猜成 false 后扩散到 MainUI。
        /// </summary>
        private static void ConfigureOwnedRedProviders()
        {
            BagMainRedStateProvider.Instance.ConfigureOwnedProviders(
                null, HasOneKeyUseRed, HasSmeltRed, HasOneKeyWearRed,
                null, null, null);
        }

        private void RefreshPageReds()
        {
            ApplyPageRedSnapshot(BagMainRedStateProvider.Instance.Refresh());
        }

        private void ApplyPageRedSnapshot(BagMainRedStateProvider.Snapshot snapshot)
        {
            SetNodeActive(useRed, snapshot.OneKeyUse);
            SetNodeActive(smeltRed, snapshot.Smelt);
            SetNodeActive(red_quick, snapshot.OneKeyWear);

            // 这些节点没有 Bag 域内的权威状态源；维持隐藏，直到对应模块显式注入。
            HideNode(suitRed);
            HideNode(guard1_red);
            HideNode(guard2_red);
            HideNode(dragonball_red);
        }

        private static bool HasOneKeyUseRed()
        {
            int roleLevel = RoleModel.Instance.Level;
            foreach (BagGoods goods in BagModel.Instance.BagGoodsList)
            {
                if (goods == null || goods.GoodsNum <= 0) continue;
                GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(goods.TypeId);
                if (basic != null && basic.UseOneKey != 0 && roleLevel >= basic.Level) return true;
            }
            return false;
        }

        private static bool HasSmeltRed()
        {
            BagModel model = BagModel.Instance;
            int maxCell = model.MaxCell;
            if (maxCell <= 0 || maxCell - model.BagGoodsList.Count > 30) return false;

            foreach (BagGoods goods in model.BagGoodsList)
            {
                if (goods == null || goods.GoodsNum <= 0) continue;
                GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(goods.TypeId);
                GoodsModel.EquipAttr equip = GoodsModel.GetEquipAttr(goods.TypeId);
                if (basic == null || equip == null || basic.Color > 4) continue;
                if (equip.Star != 0 || equip.Stage > 99 || basic.EquipType == 7 || basic.EquipType == 9) continue;

                BagGoods worn = basic.EquipType > 0 ? model.GetEquipmentAt(basic.EquipType) : null;
                if (worn == null || worn.Rating >= goods.Rating) return true;
            }
            return false;
        }

        private static bool HasOneKeyWearRed()
        {
            RoleModel role = RoleModel.Instance;
            return role.Level < OneKeyWearRedMaxLevel && HasOneKeyWearCandidate();
        }

        private static bool HasOneKeyWearCandidate()
        {
            RoleModel role = RoleModel.Instance;
            BagModel model = BagModel.Instance;
            if (!model.HasEquipmentData) return false;

            int roleTurn = role.Figure != null ? role.Figure.turn : 0;
            foreach (BagGoods goods in model.BagGoodsList)
            {
                if (goods == null || goods.GoodsNum <= 0) continue;
                GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(goods.TypeId);
                if (basic == null || !GoodsModel.IsEquip(goods.TypeId) || basic.EquipType <= 0) continue;
                if (basic.CareerId != 0 && basic.CareerId != role.Career) continue;
                if (basic.Sex != 0 && basic.Sex != role.Sex) continue;
                if (basic.Level > role.Level || basic.Turn > roleTurn) continue;

                BagGoods worn = model.GetEquipmentAt(basic.EquipType);
                if (worn == null || goods.Rating > worn.Rating) return true;
            }
            return false;
        }

        private void HideTemplates()
        {
            if (_tpl_BagEquipmentIcon != null) _tpl_BagEquipmentIcon.SetActive(false);
            if (_tpl_FightingShowSmallItem != null) _tpl_FightingShowSmallItem.SetActive(false);
        }

        private void BindButtons()
        {
            BindAction(onekeyBtn, TryManualOneKeyWear, "one key equip");
            BindAction(smeltBtn, OpenSmelt, "smelt");
            BindAction(expandBtn, () => BagFlow.OpenSub("ExpandBagView", BagModel.POS_BAG), "expand bag");
            BindAction(redequipBtn, () => ResonanceFlow.Open(), "resonance");
            BindToggle(useBtn, "OneKeyUseView", "one key use");
            BindAction(_btn_guard1, () => OpenGuard(_guardType1), "guard 1");
            BindAction(_btn_guard2, () => OpenGuard(_guardType2), "guard 2");
            BindAction(_btn_dragonball, OpenDragonBall, "dragon ball");
        }

        private static void TryManualOneKeyWear()
        {
            if (!GoodsModel.IsLoaded || !BagModel.Instance.HasEquipmentData || HasOneKeyWearCandidate())
            {
                EquipAutoWear.TryManualWear();
                return;
            }

            TipsManager.Confirm(
                "当前没有更好装备可替换。\n击杀各种大妖可获得极品装备，是否立刻前往？",
                () => GameLog.Warn("Bag",
                    "One-key wear destination blocked: target=BossEnterView, index=Field, code=BOSS_FIELD_ROUTE_MISSING"),
                null,
                "前往");
        }

        private static void OpenGuard(int guardType)
        {
            const string route = "GuardMainView";
            if (!MainUIRouter.IsRegistered(route))
            {
                GameLog.Warn("Bag", "Guard route blocked: target={0}, requestedType={1}, code=GUARD_ROUTE_STATE_PROVIDER",
                    route, guardType);
                return;
            }
            GameLog.Info("Bag", "open guard target={0}, requestedType={1}", route, guardType);
            MainUIRouter.Open(route);
        }

        private static void OpenDragonBall()
        {
            if (!FirstRechargeModel.Instance.IsDoneFirstRecharge())
            {
                MainUIRouter.Open("recharge");
                return;
            }

            const string route = "DragonBallView";
            if (!MainUIRouter.IsRegistered(route))
            {
                GameLog.Warn("Bag", "DragonBall route blocked: target={0}, code=DRAGONBALL_ROUTE_STATE_PROVIDER", route);
                return;
            }
            MainUIRouter.Open(route);
        }

        private static void OpenSmelt()
        {
            if (!FuncOpenConfig.CheckFuncOpenState("BagSmeltView"))
            {
                TipsManager.Toast("熔炼功能尚未开放");
                return;
            }
            BagFlow.ToggleSub("BagSmeltView");
        }

        private void BindToggle(Component target, string viewType, string label)
        {
            Image img = PrepareClickSurface(target);
            if (img == null) return;
            UIUtil.AddClick(img, () =>
            {
                GameLog.Info("Bag", "click bag button [{0}] -> toggle {1}", label, viewType);
                BagFlow.ToggleSub(viewType);
            });
        }

        private void BindAction(Component target, System.Action action, string label)
        {
            Image img = PrepareClickSurface(target);
            if (img == null) return;
            UIUtil.AddClick(img, () =>
            {
                GameLog.Info("Bag", "click bag button [{0}]", label);
                action?.Invoke();
            });
        }

        /// <summary>复合按钮根为唯一命中面；所有图标/文字装饰 Graphic 都关闭 Raycast。</summary>
        private static Image PrepareClickSurface(Component target)
        {
            if (target == null) return null;
            GameObject go = target.gameObject;
            foreach (Graphic graphic in go.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            Image image = go.GetComponent<Image>();
            if (image == null)
            {
                image = go.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0f);
            }
            image.raycastTarget = true;
            return image;
        }

        private static void DisableClickSurface(Component target)
        {
            if (target == null) return;
            foreach (Graphic graphic in target.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }

        private static void SetNodeActive(Component c, bool active)
        {
            if (c != null) c.gameObject.SetActive(active);
        }
    }
}
