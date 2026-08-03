using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Shenxiao.Common.Tips;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Fashion;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.OutWard;
using Shenxiao.Module.Core.Role;
using TMPro;
using UnityEngine;

namespace Shenxiao.Module.Core.Fashion
{
    /// <summary>时装套装页：41313 快照展示、41314 两档激活、41315 升阶。</summary>
    public sealed class FashionSuitView : FashionSuitViewBind
    {
        private readonly List<FashionSuitTabItem> _tabs = new List<FashionSuitTabItem>();
        private readonly List<FashionSuitGoodsItem> _goods = new List<FashionSuitGoodsItem>();
        private GameObject _tabTemplate;
        private GameObject _goodsTemplate;
        private GameObject _awardTemplate;
        private BaseAwardItem _costItem;
        private FightingShowSmallItem _fightingItem;
        private int _suitId;
        private bool _subscribed;
        private Color _upgradeButtonColor = Color.white;
        private readonly UIModelStage _roleStage = new UIModelStage();
        private readonly UIModelStage _mountStage = new UIModelStage();
        private int _modelRequestId;
        private int _renderedSuitId;
        private bool _previewHasWeapon;
        private bool _previewHasWing;
        private bool _previewHasMount;
        private int _previewEffectCount;
        private int _wearConfirmSuitId;
        [SerializeField] private Vector2[] _tabPositions = Array.Empty<Vector2>();
        [SerializeField] private float[] _tabScales = Array.Empty<float>();

        public int SelectedSuitId => _suitId;
        public int RenderedSuitId => _renderedSuitId;
        public bool PreviewHasWeapon => _previewHasWeapon;
        public bool PreviewHasWing => _previewHasWing;
        public bool PreviewHasMount => _previewHasMount;
        public int PreviewEffectCount => _previewEffectCount;
        public bool IsModelPreviewReady => _renderedSuitId == _suitId && _renderedSuitId > 0;

        public void SetTemplates(GameObject tabTemplate, GameObject goodsTemplate, GameObject awardTemplate)
        {
            _tabTemplate = tabTemplate != null ? tabTemplate : _tabTemplate;
            _goodsTemplate = goodsTemplate != null ? goodsTemplate : _goodsTemplate;
            _awardTemplate = awardTemplate != null ? awardTemplate : _awardTemplate;
            if (_tabTemplate != null) _tabTemplate.SetActive(false);
            if (_goodsTemplate != null) _goodsTemplate.SetActive(false);
        }

        protected override void OnInit()
        {
            EnsureFightingItem();
            if (_img_high_active != null) UIUtil.AddClick(_img_high_active, () => Activate(FashionModel.SUIT_HIGH_ACTIVE_COUNT));
            if (_img_per_active != null) UIUtil.AddClick(_img_per_active, () => Activate(FashionModel.SUIT_PERFECT_ACTIVE_COUNT));
            if (_img_up != null)
            {
                _upgradeButtonColor = _img_up.color;
                UIUtil.AddClick(_img_up, Upgrade);
            }
            if (_img_change != null) UIUtil.AddClick(_img_change, ConfirmWearSuit);
            if (_img_changed != null) _img_changed.raycastTarget = false;
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            FashionController.Instance.RequestSuitInfo();
            _ = LoadThenRefresh();
        }

        protected override void OnHide()
        {
            Unsubscribe();
            ClearModelPreview();
        }

        protected override void OnDispose()
        {
            Unsubscribe();
            DisposeModelStages();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            DisposeModelStages();
        }

        private async System.Threading.Tasks.Task LoadThenRefresh()
        {
            await FashionConfigs.EnsureLoaded();
            await GoodsModel.EnsureLoaded();
            await OutWardConfigs.EnsureLoaded();
            await LoginConfigs.EnsureLoaded();
            if (this == null || !gameObject.activeInHierarchy) return;
            IReadOnlyList<FashionConfigs.SuitRow> cfgs = FashionConfigs.GetSuits();
            if (_suitId <= 0 && cfgs.Count > 0) _suitId = cfgs[0].Id;
            RequestConditionSnapshots(cfgs);
            Refresh();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            EventDispatcher.On(GlobalEvent.EVT_FASHION_UPDATE, OnUpdated);
            EventDispatcher.On(GlobalEvent.EVT_BAG_UPDATE, OnUpdated);
            EventDispatcher.On<int>(GlobalEvent.EVT_OUTWARD_ILLUSION_LIST_UPDATE, OnOutWardListUpdated);
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            EventDispatcher.Off(GlobalEvent.EVT_FASHION_UPDATE, OnUpdated);
            EventDispatcher.Off(GlobalEvent.EVT_BAG_UPDATE, OnUpdated);
            EventDispatcher.Off<int>(GlobalEvent.EVT_OUTWARD_ILLUSION_LIST_UPDATE, OnOutWardListUpdated);
        }

        private void OnUpdated()
        {
            if (this != null && gameObject.activeInHierarchy) Refresh();
        }

        private void OnOutWardListUpdated(int _) => OnUpdated();

        /// <summary>
        /// Type=2 的套装条件读取幻化激活列表，而不是培养主体的 16002 面板；
        /// 按涉及的子类型各拉一次 16006，回包后刷新升阶门控。
        /// </summary>
        private static void RequestConditionSnapshots(IReadOnlyList<FashionConfigs.SuitRow> cfgs)
        {
            var requested = new HashSet<int>();
            foreach (FashionConfigs.SuitRow suit in cfgs)
            {
                foreach (FashionConfigs.SuitCondition condition in suit.Conditions)
                {
                    if (condition.Type != 2 || condition.SubType <= 0 || !requested.Add(condition.SubType)) continue;
                    OutWardController.Instance.RequestIllusionList(condition.SubType);
                }
            }
        }

        private void Refresh()
        {
            if (!FashionConfigs.IsLoaded) return;
            IReadOnlyList<FashionConfigs.SuitRow> cfgs = FashionConfigs.GetSuits();
            RefreshTabs(cfgs);
            FashionConfigs.SuitRow cfg = FashionConfigs.GetSuit(_suitId);
            if (cfg == null)
            {
                ClearDetail();
                return;
            }
            FashionModel.SuitEntry data = FashionModel.Instance.GetSuit(_suitId);
            int activeNum = data?.ActiveNum ?? 0;
            int conformNum = data?.ConformNum ?? 0;
            int lv = data?.Lv ?? 0;
            bool active = lv > 0 || activeNum >= FashionModel.SUIT_PERFECT_ACTIVE_COUNT;

            EnsureFightingItem();
            if (_fightingItem != null)
            {
                _fightingItem.SetFighting(data?.Power ?? 0L);
                _fightingItem.SetFightingUp(data != null && data.NextPower > data.Power
                    ? data.NextPower - data.Power
                    : 0L);
            }

            if (_lb_name != null) _lb_name.text = string.Join("\n", cfg.Name.ToCharArray());
            if (_lb_stage != null) _lb_stage.text = lv + "阶";
            if (_img_active_state != null) _img_active_state.gameObject.SetActive(!active);
            if (_box_not_active != null) _box_not_active.gameObject.SetActive(!active);
            if (_box_active != null) _box_active.gameObject.SetActive(active);
            RefreshWearButton(cfg, active);
            RefreshConditions(cfg);
            if (active) RefreshActive(cfg, data);
            else RefreshNotActive(cfg, activeNum, conformNum);
            RefreshModelPreview(cfg);
        }

        private void EnsureFightingItem()
        {
            if (_fightingItem != null || _tpl_FightingShowSmallItem == null || _box_fighting == null) return;
            GameObject go = Instantiate(_tpl_FightingShowSmallItem, _box_fighting, false);
            go.name = "FightingShowSmallItem_Runtime";
            go.SetActive(true);
            _fightingItem = go.GetComponent<FightingShowSmallItem>();
        }

        private void RefreshTabs(IReadOnlyList<FashionConfigs.SuitRow> cfgs)
        {
            GameObject template = _tabTemplate != null ? _tabTemplate : _tpl_FashionSuitTabItem;
            Transform parent = _panel_tab != null ? (_panel_tab.content != null ? _panel_tab.content : _panel_tab.transform) : null;
            if (template == null || parent == null) return;
            while (_tabs.Count < cfgs.Count)
            {
                GameObject go = Instantiate(template, parent);
                go.SetActive(true);
                FashionSuitTabItem item = go.GetComponent<FashionSuitTabItem>();
                if (item == null) { Destroy(go); break; }
                _tabs.Add(item);
            }
            for (int i = 0; i < _tabs.Count; i++)
            {
                bool show = i < cfgs.Count;
                _tabs[i].gameObject.SetActive(show);
                if (!show) continue;
                FashionConfigs.SuitRow cfg = cfgs[i];
                FashionModel.SuitEntry data = FashionModel.Instance.GetSuit(cfg.Id);
                bool activationRed = data != null && ((data.ActiveNum < 2 && data.ConformNum >= 2)
                    || (data.ActiveNum < 4 && data.ConformNum >= 4));
                FashionConfigs.SuitStarRow next = data != null && data.Lv > 0
                    ? FashionConfigs.GetSuitStar(cfg.Id, data.Lv + 1)
                    : null;
                bool upgradeRed = next != null
                    && HasUpgradeConditions(cfg, next, out _, out _)
                    && HasCosts(next);
                bool red = activationRed || upgradeRed;
                int captured = cfg.Id;
                _tabs[i].SetSuitId(cfg.Id);
                _tabs[i].SetData(cfg.Name, cfg.Id == _suitId, red, () => Select(captured));
                if (_tabs[i].transform is RectTransform rt
                    && i < _tabPositions.Length && i < _tabScales.Length)
                {
                    rt.anchoredPosition = _tabPositions[i];
                    rt.localScale = Vector3.one * _tabScales[i];
                }
            }
        }

        private void Select(int suitId)
        {
            if (_suitId == suitId) return;
            _suitId = suitId;
            _wearConfirmSuitId = 0;
            Refresh();
        }

        private void RefreshWearButton(FashionConfigs.SuitRow cfg, bool active)
        {
            bool worn = active && IsWearingSuit(cfg);
            if (_img_change != null) _img_change.gameObject.SetActive(active && !worn);
            if (_img_changed != null) _img_changed.gameObject.SetActive(active && worn);
        }

        private static bool IsWearingSuit(FashionConfigs.SuitRow cfg)
        {
            if (cfg == null || cfg.Conditions.Count == 0) return false;
            foreach (FashionConfigs.SuitCondition condition in cfg.Conditions)
            {
                if (condition.Type == 1)
                {
                    FashionModel.PosInfo pos = FashionModel.Instance.GetPos(condition.SubType);
                    if (pos == null || pos.WearFashionId != condition.TypeId) return false;
                    continue;
                }
                if (condition.Type == 2)
                {
                    OutWardModel.IllusionListVo illusion = OutWardModel.Instance.GetIllusionList(condition.SubType);
                    if (illusion == null || illusion.IllusionId != condition.TypeId) return false;
                }
            }
            return true;
        }

        private void ConfirmWearSuit()
        {
            FashionConfigs.SuitRow cfg = FashionConfigs.GetSuit(_suitId);
            FashionModel.SuitEntry data = FashionModel.Instance.GetSuit(_suitId);
            bool active = data != null
                && (data.Lv > 0 || data.ActiveNum >= FashionModel.SUIT_PERFECT_ACTIVE_COUNT);
            if (cfg == null || !active)
            {
                TipsManager.Toast("请先激活套装");
                Refresh();
                return;
            }
            _wearConfirmSuitId = _suitId;
            TipsManager.Confirm("是否穿戴套装？", WearConfirmed, () => _wearConfirmSuitId = 0);
        }

        private void WearConfirmed()
        {
            int suitId = _wearConfirmSuitId;
            _wearConfirmSuitId = 0;
            if (suitId <= 0 || suitId != _suitId) return;

            FashionConfigs.SuitRow cfg = FashionConfigs.GetSuit(suitId);
            FashionModel.SuitEntry data = FashionModel.Instance.GetSuit(suitId);
            bool active = data != null
                && (data.Lv > 0 || data.ActiveNum >= FashionModel.SUIT_PERFECT_ACTIVE_COUNT);
            if (cfg == null || !active)
            {
                TipsManager.Toast("套装状态已变化，请重新确认");
                Refresh();
                return;
            }

            bool missingSnapshot = false;
            foreach (FashionConfigs.SuitCondition condition in cfg.Conditions)
            {
                if (condition.Type == 1)
                {
                    FashionModel.PosInfo pos = FashionModel.Instance.GetPos(condition.SubType);
                    if (pos == null || FashionModel.Instance.GetActive(condition.SubType, condition.TypeId) == null)
                    {
                        missingSnapshot = true;
                        FashionController.Instance.RequestInfoAll();
                        continue;
                    }
                    if (pos.WearFashionId != condition.TypeId)
                        FashionController.Instance.Wear(condition.SubType, condition.TypeId, 0);
                    continue;
                }

                if (condition.Type != 2) continue;
                OutWardModel.IllusionListVo illusion = OutWardModel.Instance.GetIllusionList(condition.SubType);
                bool activated = illusion?.FigureList != null
                    && illusion.FigureList.Any(figure => figure != null && figure.Id == condition.TypeId);
                if (!activated)
                {
                    missingSnapshot = true;
                    OutWardController.Instance.RequestIllusionList(condition.SubType);
                    continue;
                }
                if (illusion.IllusionId != condition.TypeId)
                    OutWardController.Instance.WearIllusion(condition.SubType, 2, condition.TypeId, 0);
            }

            if (missingSnapshot) TipsManager.Toast("套装状态同步中，请稍后重试");
            RefreshWearButton(cfg, active);
        }

        private async void RefreshModelPreview(FashionConfigs.SuitRow cfg)
        {
            if (cfg == null || _box_model == null || !gameObject.activeInHierarchy) return;
            int requestId = ++_modelRequestId;
            _renderedSuitId = 0;
            _previewHasWeapon = false;
            _previewHasWing = false;
            _previewHasMount = false;
            _previewEffectCount = 0;
            _roleStage.ClearStage();
            _mountStage.ClearStage();

            RoleModel role = RoleModel.Instance;
            int career = Mathf.Max(1, role.Career);
            int sex = role.Sex > 0 ? role.Sex : ((career == 2 || career == 4) ? 2 : 1);
            int clothe = 0;
            int head = 0;
            int weapon = 0;
            int wing = 0;
            int back = 0;
            int mount = 0;

            foreach (FashionConfigs.SuitCondition condition in cfg.Conditions)
            {
                if (condition.Type == 1)
                {
                    FashionConfigs.ModelRow row = FashionConfigs.GetModelRow(
                        condition.SubType, condition.TypeId, career, sex, 0);
                    if (row == null) continue;
                    if (condition.SubType == 1) clothe = row.ModelId;
                    else if (condition.SubType == 3) head = row.ModelId;
                    continue;
                }
                if (condition.Type != 2) continue;
                JObject rowFigure = OutWardConfigs.GetFigureRow(condition.SubType, condition.TypeId, career);
                int figureId = rowFigure?.Value<int?>("ride_figure") ?? 0;
                switch (condition.SubType)
                {
                    case 1: mount = figureId; break;
                    case 3: wing = figureId; break;
                    case 5: weapon = figureId; break;
                    case 12: back = figureId; break;
                }
            }

            if (clothe <= 0)
            {
                if (requestId == _modelRequestId) ClearModelPreview();
                return;
            }

            GameObject model = await RoleModelAssembler.BuildOldModelAsync(new RoleModelSpec
            {
                Career = career,
                ClotheRes = clothe,
                HeadRes = head,
                WeaponRes = weapon,
                WingId = wing,
                BackOrnamentId = back,
                Actions = LoginConfigs.RoleUIActions("FashionSuitView"),
            });
            if (requestId != _modelRequestId || this == null || !gameObject.activeInHierarchy)
            {
                if (model != null) Destroy(model);
                return;
            }
            if (model == null) return;

            _previewHasWeapon = weapon > 0 && model.GetComponentsInChildren<Transform>(true)
                .Any(node => node != null && node.name.StartsWith("model_weapon_r_", StringComparison.OrdinalIgnoreCase));
            _previewHasWing = wing > 0 && model.GetComponentsInChildren<Transform>(true)
                .Any(node => node != null && node.name.StartsWith("model_wing_", StringComparison.OrdinalIgnoreCase));
            _previewEffectCount = model.GetComponentsInChildren<Transform>(true)
                .Count(node => node != null && node.name.StartsWith("__fx_", StringComparison.Ordinal));

            _box_model.gameObject.SetActive(true);
            _roleStage.PlaceInstance(_box_model, model, 0.8f, new Vector2(0f, 0.95f), UIModelStage.MODEL_YAW);

            if (_box_horse != null) _box_horse.gameObject.SetActive(mount > 0);
            if (_box_sprite != null) _box_sprite.gameObject.SetActive(false);
            if (_box_shengqi != null) _box_shengqi.gameObject.SetActive(false);
            if (mount > 0 && _box_horse != null)
            {
                string resName = "model_mount_" + mount;
                GameObject mountPrefab = await ResManager.LoadAsync<GameObject>(
                    "object/mount/" + resName + "/" + resName);
                if (requestId != _modelRequestId || this == null || !gameObject.activeInHierarchy)
                {
                    if (mountPrefab != null) ResManager.Release(mountPrefab);
                    return;
                }
                if (mountPrefab != null)
                {
                    GameObject mountModel = Instantiate(mountPrefab);
                    LoadedAssetReleaser.Track(mountModel, mountPrefab);
                    await EffectBinder.AttachAlways(mountModel, "mount", mount.ToString());
                    if (requestId != _modelRequestId || this == null || !gameObject.activeInHierarchy)
                    {
                        Destroy(mountModel);
                        return;
                    }
                    _mountStage.PlaceInstance(_box_horse, mountModel,
                        0.5f * Mathf.Max(0.01f, cfg.Ratio), new Vector2(-4f, 1.6f), 160f);
                    _previewHasMount = true;
                    _previewEffectCount += mountModel.GetComponentsInChildren<Transform>(true)
                        .Count(node => node != null && node.name.StartsWith("__fx_", StringComparison.Ordinal));
                }
            }

            await System.Threading.Tasks.Task.Yield();
            if (requestId != _modelRequestId || this == null || !gameObject.activeInHierarchy) return;
            _roleStage.RenderStageNow();
            if (_previewHasMount) _mountStage.RenderStageNow();
            _renderedSuitId = cfg.Id;
        }

        private void ClearModelPreview()
        {
            ++_modelRequestId;
            _renderedSuitId = 0;
            _previewHasWeapon = false;
            _previewHasWing = false;
            _previewHasMount = false;
            _previewEffectCount = 0;
            _roleStage.ClearStage();
            _mountStage.ClearStage();
        }

        private void DisposeModelStages()
        {
            ++_modelRequestId;
            _roleStage.Dispose();
            _mountStage.Dispose();
        }

        private void RefreshConditions(FashionConfigs.SuitRow cfg)
        {
            GameObject template = _goodsTemplate != null ? _goodsTemplate : _tpl_FashionSuitGoodsItem;
            if (template == null || _box_item == null) return;
            while (_goods.Count < cfg.Conditions.Count)
            {
                GameObject go = Instantiate(template, _box_item);
                go.SetActive(true);
                FashionSuitGoodsItem item = go.GetComponent<FashionSuitGoodsItem>();
                if (item == null) { Destroy(go); break; }
                _goods.Add(item);
            }
            for (int i = 0; i < _goods.Count; i++)
            {
                bool show = i < cfg.Conditions.Count;
                _goods[i].gameObject.SetActive(show);
                if (!show) continue;
                FashionConfigs.SuitCondition cond = cfg.Conditions[i];
                _goods[i].SetData(cond, _awardTemplate != null ? _awardTemplate : _tpl_BaseAwardItem);
                if (_goods[i].transform is RectTransform rt) rt.anchoredPosition = new Vector2((cond.Slot - 1) * 110f, 0f);
            }
        }

        private void RefreshNotActive(FashionConfigs.SuitRow cfg, int activeNum, int conformNum)
        {
            if (_lb_high_value != null) _lb_high_value.text = "(" + conformNum + "/2)";
            if (_lb_per_value != null) _lb_per_value.text = "(" + conformNum + "/4)";
            if (_html_high_attr != null) _html_high_attr.text = AttrTierText(cfg, 2);
            if (_html_per_attr != null) _html_per_attr.text = AttrTierText(cfg, 4);
            if (_img_high_active != null) _img_high_active.gameObject.SetActive(activeNum < 2);
            if (_img_high_active_state != null) _img_high_active_state.gameObject.SetActive(activeNum >= 2);
            if (_img_high_active_red != null) _img_high_active_red.gameObject.SetActive(activeNum < 2 && conformNum >= 2);
            if (_img_per_active != null) _img_per_active.gameObject.SetActive(activeNum < 4);
            if (_img_per_active_state != null) _img_per_active_state.gameObject.SetActive(activeNum >= 4);
            if (_img_per_active_red != null) _img_per_active_red.gameObject.SetActive(activeNum < 4 && conformNum >= 4);
        }

        private void RefreshActive(FashionConfigs.SuitRow cfg, FashionModel.SuitEntry data)
        {
            int lv = Math.Max(1, data?.Lv ?? 1);
            FashionConfigs.SuitStarRow current = FashionConfigs.GetSuitStar(_suitId, lv);
            FashionConfigs.SuitStarRow next = FashionConfigs.GetSuitStar(_suitId, lv + 1);
            IReadOnlyList<FashionConfigs.AttrValue> attrs = lv == 1 ? FindTier(cfg, 4) : (current?.Attrs ?? Array.Empty<FashionConfigs.AttrValue>());
            IReadOnlyList<FashionConfigs.AttrValue> nextAttrs = next?.Attrs ?? Array.Empty<FashionConfigs.AttrValue>();
            TextMeshProUGUI[] labels = { _html_now_attr0, _html_now_attr1, _html_now_attr2, _html_now_attr3 };
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] == null) continue;
                if (i >= attrs.Count) { labels[i].text = string.Empty; continue; }
                FashionConfigs.AttrValue a = attrs[i];
                string name = GoodsModel.GetAttrName(a.AttrId);
                string extra = i < nextAttrs.Count ? ("  ↑" + GoodsModel.FormatAttrValue(a.AttrId, nextAttrs[i].Value - a.Value)) : string.Empty;
                labels[i].text = (string.IsNullOrEmpty(name) ? ("属性" + a.AttrId) : name) + " +" + GoodsModel.FormatAttrValue(a.AttrId, a.Value) + extra;
            }
            IReadOnlyList<FashionConfigs.SkillValue> skills = lv == 1 ? cfg.Skills : (current?.Skills ?? Array.Empty<FashionConfigs.SkillValue>());
            if (_html_skill_desc != null)
                _html_skill_desc.text = skills.Count > 0 ? ("技能 " + skills[0].SkillId + " Lv." + skills[0].Level) : string.Empty;

            bool max = next == null;
            bool conditionsMet = HasUpgradeConditions(cfg, next, out int conformCount, out int conditionCount);
            if (_img_max_stage != null) _img_max_stage.gameObject.SetActive(max);
            if (_img_up != null) _img_up.gameObject.SetActive(!max);
            if (_html_up_cond != null)
            {
                _html_up_cond.gameObject.SetActive(!max);
                _html_up_cond.text = UpgradeConditionText(next, conditionsMet, conformCount, conditionCount);
            }
            RefreshUpgradeCost(next);
            if (_img_up != null)
            {
                Color c = _upgradeButtonColor;
                _img_up.color = max || conditionsMet ? c : new Color(c.r * 0.55f, c.g * 0.55f, c.b * 0.55f, c.a);
            }
            if (_img_up_red != null) _img_up_red.gameObject.SetActive(!max && conditionsMet && HasCosts(next));
        }

        private void RefreshUpgradeCost(FashionConfigs.SuitStarRow next)
        {
            FashionConfigs.CostValue cost = next != null && next.Costs.Count > 0 ? next.Costs[0] : null;
            bool has = cost != null && cost.TypeId > 0 && cost.Num > 0;
            if (_box_goods_con != null) _box_goods_con.gameObject.SetActive(has);
            if (_html_goods_count != null) _html_goods_count.gameObject.SetActive(has);
            if (!has) return;
            long own = BagModel.Instance.GetTypeGoodsNum(cost.TypeId);
            if (_html_goods_count != null) _html_goods_count.text = own + "/" + cost.Num;
            GameObject template = _awardTemplate != null ? _awardTemplate : _tpl_BaseAwardItem;
            if (_costItem == null && template != null && _box_goods_con != null)
            {
                GameObject go = Instantiate(template, _box_goods_con);
                go.SetActive(true);
                _costItem = go.GetComponent<BaseAwardItem>();
                if (_costItem != null) _costItem.SetScale(0.45f);
            }
            if (_costItem != null) _costItem.SetData(cost.TypeId, cost.Num);
        }

        private static bool HasCosts(FashionConfigs.SuitStarRow next)
        {
            if (next == null || next.Costs.Count == 0) return true;
            foreach (FashionConfigs.CostValue cost in next.Costs)
                if (BagModel.Instance.GetTypeGoodsNum(cost.TypeId) < cost.Num) return false;
            return true;
        }

        /// <summary>
        /// 对标老端 GetSuitPosLv/GetSuitLvPosByVo 与服务端 check_upgrade：
        /// Conditions 的 Slot 先映射 SuitRow.Conditions，再按具体时装/幻化 ID 读取培养等级。
        /// </summary>
        private static bool HasUpgradeConditions(FashionConfigs.SuitRow suit, FashionConfigs.SuitStarRow next,
            out int conformCount, out int conditionCount)
        {
            conformCount = 0;
            conditionCount = next?.Conditions.Count ?? 0;
            if (next == null || suit == null) return false;
            foreach (FashionConfigs.SuitStageCondition required in next.Conditions)
            {
                FashionConfigs.SuitCondition target = FindSuitCondition(suit, required.Slot);
                if (GetSuitConditionLevel(target) >= required.RequiredLevel) conformCount++;
            }
            return conformCount >= conditionCount;
        }

        private static FashionConfigs.SuitCondition FindSuitCondition(FashionConfigs.SuitRow suit, int slot)
        {
            if (suit == null) return null;
            foreach (FashionConfigs.SuitCondition condition in suit.Conditions)
                if (condition.Slot == slot) return condition;
            return null;
        }

        private static int GetSuitConditionLevel(FashionConfigs.SuitCondition condition)
        {
            if (condition == null) return 0;
            if (condition.Type == 1)
            {
                FashionModel.FashionEntry fashion = FashionModel.Instance.GetActive(condition.SubType, condition.TypeId);
                return fashion?.GetStarLv(0) ?? 0;
            }
            if (condition.Type != 2) return 0;

            OutWardModel.IllusionListVo list = OutWardModel.Instance.GetIllusionList(condition.SubType);
            if (list?.FigureList == null) return 0;
            foreach (OutWardModel.FigureBriefVo figure in list.FigureList)
            {
                if (figure.Id != condition.TypeId) continue;
                // 服务端 ?STAGE_CONFIG=[坐骑,同修] 取 Star，其余幻化取 Stage。
                return condition.SubType == 1 || condition.SubType == 2 ? figure.Star : figure.Stage;
            }
            return 0;
        }

        private static string UpgradeConditionText(FashionConfigs.SuitStarRow next, bool met, int conformCount, int conditionCount)
        {
            if (next == null) return string.Empty;
            if (conditionCount <= 0) return next.Desc ?? string.Empty;
            string state = met ? "已达成" : "条件不足";
            string color = met ? "#388E3C" : "#D84343";
            return (next.Desc ?? string.Empty) + $" <color={color}>（{state} {conformCount}/{conditionCount}）</color>";
        }

        private void Activate(int count)
        {
            FashionModel.SuitEntry data = FashionModel.Instance.GetSuit(_suitId);
            if (data == null) { FashionController.Instance.RequestSuitInfo(); return; }
            if (data.ConformNum < count)
            {
                TipsManager.Toast("套装条件不足");
                return;
            }
            FashionController.Instance.ActivateSuit(_suitId, count);
        }

        private void Upgrade()
        {
            FashionModel.SuitEntry data = FashionModel.Instance.GetSuit(_suitId);
            if (data == null || data.Lv <= 0)
            {
                TipsManager.Toast("条件不足");
                if (data == null) FashionController.Instance.RequestSuitInfo();
                return;
            }
            FashionConfigs.SuitRow suit = FashionConfigs.GetSuit(_suitId);
            FashionConfigs.SuitStarRow next = FashionConfigs.GetSuitStar(_suitId, data.Lv + 1);
            if (next == null) { TipsManager.Toast("已满阶"); return; }
            if (!HasUpgradeConditions(suit, next, out _, out _)) { TipsManager.Toast("条件不足"); return; }
            if (!HasCosts(next)) { TipsManager.Toast("材料不足"); return; }
            FashionController.Instance.UpgradeSuit(_suitId);
        }

        private static IReadOnlyList<FashionConfigs.AttrValue> FindTier(FashionConfigs.SuitRow cfg, int activeCount)
        {
            foreach (FashionConfigs.SuitAttrTier tier in cfg.AttrTiers)
                if (tier.ActiveCount == activeCount) return tier.Attrs;
            return Array.Empty<FashionConfigs.AttrValue>();
        }

        private static string AttrTierText(FashionConfigs.SuitRow cfg, int activeCount)
        {
            IReadOnlyList<FashionConfigs.AttrValue> attrs = FindTier(cfg, activeCount);
            var lines = new List<string>();
            foreach (FashionConfigs.AttrValue a in attrs)
            {
                string name = GoodsModel.GetAttrName(a.AttrId);
                lines.Add((string.IsNullOrEmpty(name) ? ("属性" + a.AttrId) : name) + " +" + GoodsModel.FormatAttrValue(a.AttrId, a.Value));
            }
            return string.Join("\n", lines);
        }

        private void ClearDetail()
        {
            if (_lb_name != null) _lb_name.text = string.Empty;
            if (_lb_stage != null) _lb_stage.text = "0阶";
            if (_box_active != null) _box_active.gameObject.SetActive(false);
            if (_box_not_active != null) _box_not_active.gameObject.SetActive(false);
            if (_img_change != null) _img_change.gameObject.SetActive(false);
            if (_img_changed != null) _img_changed.gameObject.SetActive(false);
            if (_fightingItem != null)
            {
                _fightingItem.SetFighting(0L);
                _fightingItem.SetFightingUp(0L);
            }
        }
    }
}
