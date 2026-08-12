using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Pet;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Dress;
using Shenxiao.Module.Core.FunctionOpen;
using Shenxiao.Module.Core.OutWard;
using Shenxiao.Module.Core.Role;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Pet
{
    /// <summary>幻化正式生产子 View；Prefab 保存结构，本类只投影权威状态、绑定事件与调用既有协议。</summary>
    public sealed class IllusionBaseView : IllusionBaseViewBind
    {
        private readonly List<IllusionItemBind> _items = new List<IllusionItemBind>();
        private readonly List<IllusionPropItemBind> _attrs = new List<IllusionPropItemBind>();
        private readonly List<PetRoundItemBind> _skills = new List<PetRoundItemBind>();
        private OutWardBaseView _owner;
        private int _typeId;
        private int _selectedFigureId;
        private int _pendingDetailFigureId;
        private bool _subscribed;
        private UIModelStage _modelStage;
        private int _modelEpoch;
        private string _modelKey;
        private bool _modelLoading;
        private bool _modelPlaced;

        public int ModelEpoch => _modelEpoch;
        public string ModelKey => _modelKey ?? string.Empty;
        public bool ModelPlaced => _modelPlaced;
        public const int StageUpgradeGoodsId = 0;
        public const int IllusionGridColumns = 3;
        public const float IllusionGridItemHeight = 94f;
        public const float IllusionGridVerticalGap = 7f;

        public void Open(OutWardBaseView owner, int typeId)
        {
            _owner = owner;
            _typeId = typeId;
            Show(typeId);
        }

        protected override void OnInit()
        {
            BindClick(illusion_btn, WearSelected);
            BindClick(unillu_btn, UnwearSelected);
            BindClick(upstage_btn, ActivateOrStageSelected);
            BindClick(exp_upstage_btn, StageUpSelected);
            BindClick(resolve_btn, OpenStarSelected);
            if (illusion_scroller != null) illusion_scroller.onValueChanged.AddListener(OnIllusionScroll);
            HideTemplates();
        }

        protected override void OnShow(object args)
        {
            if (args is int typeId && typeId > 0) _typeId = typeId;
            Subscribe();
            OutWardController.Instance.RequestIllusionList(_typeId);
            RefreshAll();
        }

        protected override void OnHide()
        {
            _pendingDetailFigureId = 0;
            Unsubscribe();
            ClearDynamic();
            ClearModel();
        }

        protected override void OnDispose()
        {
            _pendingDetailFigureId = 0;
            Unsubscribe();
            ClearDynamic();
            DisposeModel();
            if (illusion_scroller != null) illusion_scroller.onValueChanged.RemoveListener(OnIllusionScroll);
        }

        public void Close()
        {
            OutWardBaseView owner = _owner;
            _owner = null;
            Hide();
            owner?.RestoreCapturedIllusion();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            EventDispatcher.On<int>(GlobalEvent.EVT_OUTWARD_ILLUSION_LIST_UPDATE, OnListUpdate);
            EventDispatcher.On<int, int>(GlobalEvent.EVT_OUTWARD_FIGURE_DETAIL_UPDATE, OnDetailUpdate);
            EventDispatcher.On<int>(GlobalEvent.EVT_OUTWARD_ILLUSION_WEAR, OnListUpdate);
            EventDispatcher.On<int, int>(GlobalEvent.EVT_OUTWARD_FIGURE_ACTIVATED, OnFigureUpdate);
            EventDispatcher.On<int, int>(GlobalEvent.EVT_OUTWARD_FIGURE_STAGE_UP, OnFigureUpdate);
            EventDispatcher.On<int, int>(GlobalEvent.EVT_OUTWARD_FIGURE_STAR_UP, OnFigureUpdate);
            EventDispatcher.On<int, int>(GlobalEvent.EVT_OUTWARD_FIGURE_EXPIRED, OnFigureUpdate);
            EventDispatcher.On<int, int>(GlobalEvent.EVT_OUTWARD_FIGHT_PREVIEW, OnFigureUpdate);
            EventDispatcher.On<int, int>(GlobalEvent.EVT_OUTWARD_STAR_FIGHT_PREVIEW, OnFigureUpdate);
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            EventDispatcher.Off<int>(GlobalEvent.EVT_OUTWARD_ILLUSION_LIST_UPDATE, OnListUpdate);
            EventDispatcher.Off<int, int>(GlobalEvent.EVT_OUTWARD_FIGURE_DETAIL_UPDATE, OnDetailUpdate);
            EventDispatcher.Off<int>(GlobalEvent.EVT_OUTWARD_ILLUSION_WEAR, OnListUpdate);
            EventDispatcher.Off<int, int>(GlobalEvent.EVT_OUTWARD_FIGURE_ACTIVATED, OnFigureUpdate);
            EventDispatcher.Off<int, int>(GlobalEvent.EVT_OUTWARD_FIGURE_STAGE_UP, OnFigureUpdate);
            EventDispatcher.Off<int, int>(GlobalEvent.EVT_OUTWARD_FIGURE_STAR_UP, OnFigureUpdate);
            EventDispatcher.Off<int, int>(GlobalEvent.EVT_OUTWARD_FIGURE_EXPIRED, OnFigureUpdate);
            EventDispatcher.Off<int, int>(GlobalEvent.EVT_OUTWARD_FIGHT_PREVIEW, OnFigureUpdate);
            EventDispatcher.Off<int, int>(GlobalEvent.EVT_OUTWARD_STAR_FIGHT_PREVIEW, OnFigureUpdate);
        }

        private void OnListUpdate(int typeId)
        {
            if (typeId == _typeId && IsShown) RefreshAll();
        }

        private void OnFigureUpdate(int typeId, int figureId)
        {
            if (typeId == _typeId && IsShown && (figureId == 0 || figureId == _selectedFigureId)) RefreshAll();
        }

        private void OnDetailUpdate(int typeId, int figureId)
        {
            if (typeId != _typeId) return;
            if (figureId == _pendingDetailFigureId) _pendingDetailFigureId = 0;
            if (IsShown && (figureId == 0 || figureId == _selectedFigureId)) RefreshAll();
        }

        private void RefreshAll()
        {
            int career = RoleModel.Instance.Career;
            int roleTurn = RoleModel.Instance.Figure?.turn ?? 0;
            OutWardModel.IllusionRedState projection = OutWardModel.Instance.GetIllusionRedState(
                _typeId, career, roleTurn, CountInBag);
            if (_selectedFigureId <= 0 || !Contains(projection.Figures, _selectedFigureId))
            {
                _selectedFigureId = projection.Figures.Count > 0 ? projection.Figures[0].FigureId : 0;
                _pendingDetailFigureId = 0;
            }
            RebuildList(projection.Figures);
            OutWardModel.IllusionFigureState selected = Find(projection.Figures, _selectedFigureId);
            EnsureSelectedDetail(selected);
            EnsureFightPreview(selected);
            RefreshSelected(selected);
            RefreshBottomHint(projection.Figures);
        }

        private void RebuildList(IReadOnlyList<OutWardModel.IllusionFigureState> states)
        {
            DestroyItems(_items);
            if (_tpl_IllusionItem == null || illusion_group == null) return;
            for (int i = 0; i < states.Count; i++)
            {
                OutWardModel.IllusionFigureState state = states[i];
                GameObject go = Instantiate(_tpl_IllusionItem, illusion_group, false);
                go.name = "IllusionItem_" + state.FigureId;
                IllusionItemBind item = go.GetComponent<IllusionItemBind>();
                if (item == null) continue;
                item.Show();
                if (item.res_name != null) item.res_name.text = state.Name;
                if (item.icon_stage != null) item.icon_stage.text = state.Stage > 0 ? state.Stage + "阶" : string.Empty;
                if (item.state_text != null) item.state_text.text = state.Current ? "使用中" : state.Activated ? "已激活" : "未激活";
                if (item.select_bg != null) item.select_bg.gameObject.SetActive(state.FigureId == _selectedFigureId);
                if (item.using_tip != null) item.using_tip.gameObject.SetActive(state.Current);
                if (item.red_dot != null) item.red_dot.gameObject.SetActive(state.CanActivate || state.CanStageUp || state.CanStarUp);
                int id = state.FigureId;
                BindClick(item.click_bg, () => SelectFigure(id));
                _items.Add(item);
            }
        }

        private void SelectFigure(int figureId)
        {
            if (figureId <= 0 || figureId == _selectedFigureId) return;
            _selectedFigureId = figureId;
            _pendingDetailFigureId = 0;
            RefreshAll();
        }

        private void EnsureSelectedDetail(OutWardModel.IllusionFigureState state)
        {
            if (!ShouldRequestActivatedDetail(state) || _pendingDetailFigureId == state.FigureId) return;
            _pendingDetailFigureId = state.FigureId;
            OutWardController.Instance.RequestFigureDetail(_typeId, state.FigureId);
        }

        private void EnsureFightPreview(OutWardModel.IllusionFigureState state)
        {
            if (state == null || state.Detail != null) return;
            OutWardModel.FightPreviewVo cached = OutWardModel.Instance.LastFightPreview;
            if (cached.TypeId == _typeId && cached.FigureId == state.FigureId) return;
            OutWardController.Instance.RequestFightPreview(_typeId, state.FigureId);
        }

        public static bool ShouldRequestActivatedDetail(OutWardModel.IllusionFigureState state)
            => state != null && state.Activated && state.Detail == null;

        public static int ResolveUnwearStage(OutWardModel.OutWardVo outward)
            => Math.Max(1, outward?.Stage ?? 1);

        public static bool UsesGoodsBasedStage(int typeId) => typeId == 3;

        public static bool SupportsStarEntry(int typeId) => typeId == 1 || typeId == 2;

        public static float ComputeIllusionGridHeight(int itemCount)
        {
            int rows = Math.Max(0, (itemCount + IllusionGridColumns - 1) / IllusionGridColumns);
            return rows <= 0 ? 0f : rows * IllusionGridItemHeight + (rows - 1) * IllusionGridVerticalGap;
        }

        private void RefreshSelected(OutWardModel.IllusionFigureState state)
        {
            bool present = state != null;
            RefreshModel(present ? state.ModelRes : 0);
            if (effect != null) effect.gameObject.SetActive(present);
            if (effect_group != null) effect_group.gameObject.SetActive(present);
            if (res_name != null) res_name.text = present ? state.Name : "暂无幻化";
            if (res_stage != null) res_stage.text = present && state.Stage > 0 ? state.Stage + "阶" : string.Empty;
            if (source_text != null) source_text.text = GetSourceReason(state);
            if (goods_quantity != null) goods_quantity.text = present
                ? state.ActivateOwnedNum + "/" + state.ActivateGoodsNum : string.Empty;
            if (goods_name != null) goods_name.text = present && state.ActivateGoodsId > 0
                ? GoodsModel.GetGoodsName((int)state.ActivateGoodsId) : string.Empty;
            SetActive(proview_bg, present && !state.Activated);
            SetActive(illu_group, present && state.Activated);
            SetActive(illusion_btn, present && state.Activated && !state.Current);
            SetActive(unillu_btn, present && state.Current);
            SetActive(upstage_group, present && (!state.Activated || state.HasNextStage));
            SetActive(use_goods_group, present && (!state.Activated || state.HasNextStage));
            // WingsIllusionView 沿基类默认 ui_type=0/upstage_type=0：goods-based stage，不能与经验组并显。
            SetActive(use_exp_group, !UsesGoodsBasedStage(_typeId) && present && state.Activated && state.HasNextStage);
            SetActive(upstage_btn, present && (!state.Activated || state.HasNextStage));
            // 激活仍校验激活物；已激活升阶即使材料不足也发16009，由服务端权威失败回包反馈。
            SetInteractable(upstage_btn, present && (!state.Activated ? state.CanActivate : state.HasNextStage));
            SetInteractable(exp_upstage_btn, false);
            if (upstage_lb != null) upstage_lb.text = present && !state.Activated ? "激活" : "升阶";
            SetActive(stage_reddot, present && (state.CanActivate || state.CanStageUp));
            SetActive(resolve_btn, present && SupportsStarEntry(_typeId)
                && state.Activated && state.HasStarSystem);
            SetActive(resolve_red, present && SupportsStarEntry(_typeId) && state.CanStarUp);
            RebuildAttributes(state);
            RebuildSkills(state);
        }

        private void RefreshModel(int showId)
        {
            if (res == null || showId <= 0 || !TryGetModelProfile(_typeId, out string module,
                    out string prefix, out string fallback))
            {
                ClearModel();
                return;
            }
            string address = BuildModelAddress(module, showId);
            if (_modelKey == address && (_modelLoading || _modelPlaced)) return;
            ClearModel(); // 换项先把旧实例失活/清台，异步旧请求由 epoch 拒绝回挂。
            _modelKey = address;
            _modelLoading = true;
            int epoch = _modelEpoch;
            _ = LoadModelAsync(epoch, address, module, prefix, fallback, showId);
        }

        private async Task LoadModelAsync(int epoch, string address, string module,
            string prefix, string fallback, int showId)
        {
            try
            {
                GameObject prefab = await ResManager.LoadAsync<GameObject>(address);
                await ClientOutWardPosConfigs.EnsureLoaded();
                if (!IsCurrentModel(epoch, address)) return;
                if (prefab == null)
                {
                    _modelLoading = false;
                    _modelKey = null;
                    GameLog.Warn("OutWard", "illusion model missing type={0} figure={1} address={2}",
                        _typeId, _selectedFigureId, address);
                    return;
                }
                GameObject instance = Instantiate(prefab);
                if (!IsCurrentModel(epoch, address))
                {
                    Destroy(instance);
                    return;
                }
                UiModelParameterConfigs.ModelParam parameter = ClientOutWardPosConfigs.Get(
                    prefix + "_" + showId, fallback);
                if (_modelStage == null) _modelStage = new UIModelStage();
                _modelStage.EnableDragRotate(true);
                res.gameObject.SetActive(true);
                _modelStage.PlaceInstance(res, instance, parameter.Scale, parameter.Position, parameter.Rotate);
                _modelPlaced = true;
                _modelLoading = false;
                _ = EffectBinder.AttachAlways(instance, module, showId.ToString());
                _ = PlayIdleAsync(instance, module, showId);
            }
            catch (Exception e)
            {
                if (!this || epoch != _modelEpoch || _modelKey != address) return;
                _modelLoading = false;
                _modelPlaced = false;
                _modelKey = null;
                _modelStage?.ClearStage();
                GameLog.Warn("OutWard", "illusion model load failed address={0} error={1}", address, e.Message);
            }
        }

        private bool IsCurrentModel(int epoch, string address)
            => this && IsShown && epoch == _modelEpoch && _modelKey == address && res != null;

        private static async Task PlayIdleAsync(GameObject instance, string module, int showId)
        {
            if (instance == null) return;
            const string action = "idle";
            Animation animation = instance.GetComponent<Animation>();
            if (animation != null && animation.GetClip(action) != null)
            {
                animation.Play(action);
                return;
            }
            AnimationClip clip = await ResManager.LoadAsync<AnimationClip>(
                "object/" + module + "/action/" + showId + "/" + action);
            if (instance == null || clip == null) return;
            if (animation == null) animation = instance.AddComponent<Animation>();
            if (animation.GetClip(action) == null) animation.AddClip(clip, action);
            animation.Play(action);
        }

        private static bool TryGetModelProfile(int typeId, out string module, out string prefix, out string fallback)
        {
            switch (typeId)
            {
                case 1: module = "mount"; prefix = "h"; fallback = "default_horse"; return true;
                case 2: module = "spirit"; prefix = "s"; fallback = "default_sprite"; return true;
                case 3: module = "wing"; prefix = "w"; fallback = "default_wing"; return true;
                case 4: module = "fabao"; prefix = "a"; fallback = "default_artifact"; return true;
                case 5: module = "weapon"; prefix = "d"; fallback = "default_weapon"; return true;
                case 12: module = "back"; prefix = "b"; fallback = "default_back_ornament"; return true;
                default: module = null; prefix = null; fallback = null; return false;
            }
        }

        private static string BuildModelAddress(string module, int showId)
        {
            string name = module == "weapon" ? "model_weapon_r_" + showId : "model_" + module + "_" + showId;
            return "object/" + module + "/" + name + "/" + name;
        }

        private void ClearModel()
        {
            _modelEpoch++;
            _modelKey = null;
            _modelLoading = false;
            _modelPlaced = false;
            _modelStage?.ClearStage();
            if (res != null) res.gameObject.SetActive(false);
        }

        private void DisposeModel()
        {
            _modelEpoch++;
            _modelKey = null;
            _modelLoading = false;
            _modelPlaced = false;
            if (_modelStage != null)
            {
                _modelStage.Dispose();
                _modelStage = null;
            }
            if (res != null) res.gameObject.SetActive(false);
        }

        private void WearSelected()
        {
            OutWardModel.IllusionFigureState state = CurrentState();
            if (state != null && state.Activated && !state.Current)
                OutWardController.Instance.WearIllusion(_typeId, 2, state.FigureId, 0);
        }

        private void UnwearSelected()
        {
            OutWardModel.IllusionFigureState state = CurrentState();
            if (state != null && state.Current)
                OutWardController.Instance.WearIllusion(_typeId, 1, 0,
                    ResolveUnwearStage(OutWardModel.Instance.Get(_typeId)));
        }

        private void ActivateOrStageSelected()
        {
            OutWardModel.IllusionFigureState state = CurrentState();
            if (state == null) return;
            if (state.CanActivate)
                OutWardController.Instance.ActivateFigure(_typeId, state.FigureId);
            else if (state.Activated && state.HasNextStage)
                OutWardController.Instance.UpgradeFigureStage(_typeId, state.FigureId, StageUpgradeGoodsId);
        }

        private void StageUpSelected()
        {
            OutWardModel.IllusionFigureState state = CurrentState();
            if (state != null && state.Activated && state.HasNextStage)
                OutWardController.Instance.UpgradeFigureStage(_typeId, state.FigureId, StageUpgradeGoodsId);
        }

        private void OpenStarSelected()
        {
            OutWardModel.IllusionFigureState state = CurrentState();
            if (state != null && SupportsStarEntry(_typeId) && state.HasStarSystem)
                OutWardController.Instance.RequestStarFightPreview(_typeId, state.FigureId);
        }

        /// <summary>供正式星级子页调用；当前翅膀(type=3)无星级系统时不会暴露入口。</summary>
        public void SubmitStarSelected()
        {
            OutWardModel.IllusionFigureState state = CurrentState();
            if (state != null && state.CanStarUp) OutWardController.Instance.UpgradeFigureStar(_typeId, state.FigureId);
        }

        private OutWardModel.IllusionFigureState CurrentState()
        {
            return OutWardModel.Instance.GetIllusionFigureState(_typeId, _selectedFigureId,
                RoleModel.Instance.Career, RoleModel.Instance.Figure?.turn ?? 0, CountInBag);
        }

        private void RebuildAttributes(OutWardModel.IllusionFigureState state)
        {
            DestroyItems(_attrs);
            if (_tpl_IllusionPropItem == null || prop_group == null || state == null) return;
            IReadOnlyList<OutWardModel.IllusionAttributeRowState> rows = OutWardModel.Instance
                .GetIllusionAttributeRows(_typeId, state.FigureId, state.Stage, state.Detail);
            for (int i = 0; i < rows.Count; i++)
            {
                GameObject go = Instantiate(_tpl_IllusionPropItem, prop_group, false);
                IllusionPropItemBind item = go.GetComponent<IllusionPropItemBind>();
                if (item == null) continue;
                item.Show();
                OutWardModel.IllusionAttributeRowState row = rows[i];
                if (item.prop_text != null) item.prop_text.text = row.Name + " " + row.CurrentText;
                if (item.next_text != null) item.next_text.text = row.NextText;
                if (item.up_arrow != null) item.up_arrow.gameObject.SetActive(!string.IsNullOrEmpty(row.NextText));
                _attrs.Add(item);
            }
        }

        private void RebuildSkills(OutWardModel.IllusionFigureState state)
        {
            DestroyItems(_skills);
            if (_tpl_PetRoundItem == null || skill_group == null || state == null) return;
            IReadOnlyList<OutWardModel.IllusionSkillRowState> rows = OutWardModel.Instance
                .GetIllusionSkillRows(_typeId, state.FigureId, state.Career, state.Stage, state.Detail);
            for (int i = 0; i < rows.Count; i++)
            {
                GameObject go = Instantiate(_tpl_PetRoundItem, skill_group, false);
                PetRoundItemBind item = go.GetComponent<PetRoundItemBind>();
                if (item == null) continue;
                item.Show();
                OutWardModel.IllusionSkillRowState row = rows[i];
                if (item.bottom_text != null) item.bottom_text.text = row.Name;
                if (item.skill_lv != null) item.skill_lv.text = row.Locked ? row.RequiredStage + "阶解锁" : string.Empty;
                if (item.icon_bg_mask != null) item.icon_bg_mask.gameObject.SetActive(row.Locked);
                if (item.red_dot != null) item.red_dot.gameObject.SetActive(false);
                if (item.icon != null && !string.IsNullOrEmpty(row.Icon))
                    _ = ResManager.SetImageAsync(item.icon, GameResPath.GetSkillIcon(row.Icon), nativeSize: false);
                if (item.click_group != null)
                {
                    int skillId = row.SkillId;
                    BindClick(item.click_group, () => DressSkillTipFlow.Show(skillId));
                }
                _skills.Add(item);
            }
        }

        private static string GetSourceReason(OutWardModel.IllusionFigureState state)
        {
            if (state == null || state.Activated) return string.Empty;
            if (!state.ConditionsMet) return state.ConditionBlockReason;
            if (state.ActivateGoodsId > 0 && state.ActivateOwnedNum < state.ActivateGoodsNum)
            {
                string name = GoodsModel.GetGoodsName((int)state.ActivateGoodsId);
                if (string.IsNullOrEmpty(name)) name = "激活材料";
                return "缺少" + name + "（" + state.ActivateOwnedNum + "/" + state.ActivateGoodsNum + "）";
            }
            return state.CanActivate ? string.Empty : "当前条件不足，暂不可激活";
        }

        private void OnIllusionScroll(Vector2 _)
        {
            int career = RoleModel.Instance.Career;
            int roleTurn = RoleModel.Instance.Figure?.turn ?? 0;
            RefreshBottomHint(OutWardModel.Instance.GetIllusionRedState(
                _typeId, career, roleTurn, CountInBag).Figures);
        }

        private void RefreshBottomHint(IReadOnlyList<OutWardModel.IllusionFigureState> states)
        {
            if (bottom_btn == null || illusion_scroller == null) return;
            float viewportHeight = ((RectTransform)illusion_scroller.transform).rect.height;
            float contentHeight = ComputeIllusionGridHeight(states?.Count ?? 0);
            bool below = contentHeight > viewportHeight + 1f
                && illusion_scroller.verticalNormalizedPosition > 0.001f;
            bottom_btn.gameObject.SetActive(below);
            bool redBelow = false;
            if (below && states != null)
            {
                int visibleRows = Math.Max(1, Mathf.FloorToInt(viewportHeight /
                    (IllusionGridItemHeight + IllusionGridVerticalGap)));
                int totalRows = Math.Max(1, (states.Count + IllusionGridColumns - 1) / IllusionGridColumns);
                int firstHidden = Math.Min(states.Count,
                    Math.Max(0, totalRows - visibleRows) * IllusionGridColumns);
                for (int i = firstHidden; i < states.Count; i++)
                    if (states[i].CanActivate || states[i].CanStageUp || states[i].CanStarUp)
                    { redBelow = true; break; }
            }
            SetActive(bottom_red, redBelow);
        }

        private void HideTemplates()
        {
            SetActive(_tpl_IllusionItem, false);
            SetActive(_tpl_IllusionPropItem, false);
            SetActive(_tpl_PetRoundItem, false);
        }

        private void ClearDynamic()
        {
            DestroyItems(_items);
            DestroyItems(_attrs);
            DestroyItems(_skills);
        }

        private static long CountInBag(int goodsTypeId)
        {
            long total = 0;
            foreach (Bag.BagGoods goods in Bag.BagModel.Instance.BagGoodsList)
                if (goods.TypeId == goodsTypeId) total += goods.GoodsNum;
            return total;
        }

        private static bool Contains(IReadOnlyList<OutWardModel.IllusionFigureState> states, int id)
            => Find(states, id) != null;

        private static OutWardModel.IllusionFigureState Find(IReadOnlyList<OutWardModel.IllusionFigureState> states, int id)
        {
            for (int i = 0; i < states.Count; i++) if (states[i].FigureId == id) return states[i];
            return null;
        }

        private static void BindClick(Component target, Action action)
        {
            if (target == null) return;
            Graphic graphic = target as Graphic ?? target.GetComponent<Graphic>();
            if (graphic != null) UIUtil.ClearClicks(graphic);
            UIUtil.AddClick(target, action);
        }

        private static void SetInteractable(Component target, bool value)
        {
            if (target == null) return;
            Button button = target.GetComponent<Button>();
            if (button != null) button.interactable = value;
            Graphic graphic = target as Graphic ?? target.GetComponent<Graphic>();
            if (graphic != null) graphic.raycastTarget = value;
        }

        private static void SetActive(Component target, bool value) { if (target != null) target.gameObject.SetActive(value); }
        private static void SetActive(GameObject target, bool value) { if (target != null) target.SetActive(value); }

        private static void DestroyItems<T>(List<T> items) where T : Component
        {
            for (int i = 0; i < items.Count; i++) if (items[i] != null) Destroy(items[i].gameObject);
            items.Clear();
        }
    }
}
