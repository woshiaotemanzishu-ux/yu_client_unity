using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shenxiao.Common.Audio;
using Shenxiao.Common.Tips;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Achv;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Achievement
{
    /// <summary>
    /// 人物页「成就」完整功能线。视觉结构来自 AchvModule.prefab，运行时仅装配配置、
    /// 409 协议权威状态、分类选择与共享 BaseAwardItem；不在代码中重建页面视觉树。
    /// </summary>
    public static class AchievementFlow
    {
        private const byte OverviewCategory = 1;
        private const int StageSuccessEffectDurationMs = 15000;

        private sealed class SubTabState
        {
            public ushort Id;
            public AchvTabSubBtnBind Bind;
        }

        private sealed class TopTabState
        {
            public AchievementConfigs.TypeRow Type;
            public AchvTabBtnBind Bind;
            public LayoutElement Layout;
            public float ExpandedWidth;
            public float CollapsedWidth;
            public readonly List<SubTabState> Subs = new List<SubTabState>();
        }

        private static readonly List<TopTabState> TopTabs = new List<TopTabState>();
        private static readonly List<GameObject> TotalRows = new List<GameObject>();
        private static readonly List<GameObject> DetailRows = new List<GameObject>();
        private static readonly List<GameObject> TypeCards = new List<GameObject>();
        private static readonly List<GameObject> AttributeRows = new List<GameObject>();
        private static readonly List<RewardFlyService.Handle> RewardFlyHandles =
            new List<RewardFlyService.Handle>();

        private static GameObject _frameRoot;
        private static GameObject _moduleRoot;
        private static BaseWindowSkinView _window;
        private static AchvMainViewBind _view;
        private static AchvSubItemBind _detailTemplate;
        private static AchvTabBarBind _tabBar;
        private static ScrollRect _overviewScroll;
        private static ScrollRect _detailScroll;
        private static Sprite _topSelected;
        private static Sprite _topUnselected;
        private static Sprite _subSelected;
        private static Sprite _subUnselected;
        private static GameObject _awardPrefab;
        private static bool _loading;
        private static bool _subscribed;
        private static bool _tabsBuilt;
        private static bool _stageClickBound;
        private static bool _initialSelectionResolved;
        private static int _selectedType = 1;
        private static ushort _selectedSubtype;
        private static UIEffectStage.Handle _stageSuccessEffect;
        private static int _stageSuccessEffectEpoch;
        private static uint _pendingRewardEntryId;
        private static Vector3 _pendingRewardSourceWorld;

        public static void Toggle()
        {
            if (_window != null && _window.IsShown) Close();
            else Open();
        }

        public static void Open() => _ = OpenAsync();

        public static void Close()
        {
            ClearStageSuccessEffect();
            ClearRewardFly();
            ClearPendingRewardSource();
            if (_window != null) _window.Hide();
        }

        private static async Task OpenAsync()
        {
            if (_loading) return;
            _loading = true;
            try
            {
                await Task.WhenAll(AchievementConfigs.EnsureLoaded(), GoodsModel.EnsureLoaded());
                if (!AchievementConfigs.IsLoaded)
                {
                    TipsManager.Toast("成就配置加载失败");
                    return;
                }

                AchievementController.Instance.Init();
                if (!await EnsureViewAsync()) return;
                Subscribe();

                _window.SetReturnAction(ReturnToRole);
                _window.Show();
                _window.Configure(new[]
                {
                    new TabSpec
                    {
                        Enabled = true,
                        Label = "成就",
                        TitleImagePath = GameResPath.GetIcon("achv", "uicj_030"),
                        BackgroundImagePath = GameResPath.GetBigBgPath("uicj_bg1.jpg"),
                        ContentFactory = ReparentView,
                    },
                }, 0);

                AchievementController.Instance.RequestStartup();
                if (!AchievementModel.Instance.TryGetCategory(OverviewCategory, out _))
                    AchievementController.Instance.RequestCategory(OverviewCategory);
                Render();
            }
            finally
            {
                _loading = false;
            }
        }

        private static async Task<bool> EnsureViewAsync()
        {
            if (_frameRoot != null && _moduleRoot != null && _window != null && _view != null)
                return true;

            Transform layer = ViewManager.GetLayer(UILayer.Window);
            Task<GameObject> frameTask = ResManager.InstantiateAsync(
                GameResPath.GetUIPrefab("common", "BaseWindowSkin"), layer);
            Task<GameObject> moduleTask = ResManager.InstantiateAsync(
                GameResPath.GetUIPrefab("achv", "AchvModule"), layer);
            Task<Sprite> topSelectedTask = ResManager.LoadAsync<Sprite>(
                GameResPath.GetIcon("achv", "uicj_026"));
            Task<Sprite> topUnselectedTask = ResManager.LoadAsync<Sprite>(
                GameResPath.GetIcon("achv", "uicj_027"));
            Task<Sprite> subSelectedTask = ResManager.LoadAsync<Sprite>(
                GameResPath.GetIcon("achv", "uicj_029"));
            Task<Sprite> subUnselectedTask = ResManager.LoadAsync<Sprite>(
                GameResPath.GetIcon("achv", "uicj_029b"));
            Task<GameObject> awardPrefabTask = ResManager.LoadAsync<GameObject>(
                GameResPath.GetUIPrefab("common", "BaseAwardItem"));
            await Task.WhenAll(frameTask, moduleTask, topSelectedTask, topUnselectedTask,
                subSelectedTask, subUnselectedTask, awardPrefabTask);

            _frameRoot = frameTask.Result;
            _moduleRoot = moduleTask.Result;
            _topSelected = topSelectedTask.Result;
            _topUnselected = topUnselectedTask.Result;
            _subSelected = subSelectedTask.Result;
            _subUnselected = subUnselectedTask.Result;
            _awardPrefab = awardPrefabTask.Result;
            if (_frameRoot == null || _moduleRoot == null)
            {
                GameLog.Error("Achievement", "成就窗口加载失败 frame={0} module={1}",
                    _frameRoot != null, _moduleRoot != null);
                ReleaseView();
                return false;
            }

            _frameRoot.name = "BaseWindowSkin(Achievement)";
            _moduleRoot.name = "AchvModule(Runtime)";
            _window = _frameRoot.GetComponent<BaseWindowSkinView>()
                ?? _frameRoot.GetComponentInChildren<BaseWindowSkinView>(true);
            _view = _moduleRoot.GetComponentInChildren<AchvMainViewBind>(true);
            _detailTemplate = _moduleRoot.GetComponentsInChildren<AchvSubItemBind>(true)
                .FirstOrDefault();
            if (_window == null || _view == null || _detailTemplate == null
                || _view._tpl_AchvTabBar == null || _view._tpl_AchvTabBtn == null
                || _view._tpl_AchvTabSubBtn == null || _view._tpl_AchvTotalItem == null
                || _view._tpl_AchvChildItem == null || _view._tpl_AchvPropItem == null)
            {
                GameLog.Error("Achievement", "AchvModule 缺窗口、主视图或运行时模板绑定");
                ReleaseView();
                return false;
            }

            foreach (BaseView child in _moduleRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (child != _view) child.gameObject.SetActive(false);
            }
            _view.gameObject.SetActive(false);
            _detailTemplate.gameObject.SetActive(false);
            _view._tpl_AchvTabBar.SetActive(false);
            _view._tpl_AchvTabBtn.SetActive(false);
            _view._tpl_AchvTabSubBtn.SetActive(false);
            _view._tpl_AchvTotalItem.SetActive(false);
            _view._tpl_AchvChildItem.SetActive(false);
            _view._tpl_AchvPropItem.SetActive(false);
            _moduleRoot.SetActive(true);
            return true;
        }

        private static BaseView ReparentView(RectTransform parent)
        {
            _view.transform.SetParent(parent, false);
            RectTransform rect = _view.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
            }
            _view.gameObject.SetActive(true);
            _view.Show();
            EnsureRuntimeStructure();
            return _view;
        }

        private static void EnsureRuntimeStructure()
        {
            if (_tabsBuilt) return;

            _overviewScroll = _view.Content1 != null && _view.Content1.content != null
                ? _view.Content1
                : ResolveNestedScroll(_view.achv_scroller, _view.totalGp);
            _detailScroll = ResolveNestedScroll(_view._Scroller3, _view.dispersionGp);
            if (!ValidateVerticalScroll(_overviewScroll, null, "总览列表")
                || !ValidateVerticalScroll(_detailScroll, null, "分类详情列表")
                || !ValidateVerticalScroll(_view._Scroller1, _view.Content11, "属性列表")
                || !ValidateHorizontalScroll(_view._Scroller2, _view.Content111, "分类进度列表"))
                return;

            GameObject barGo = UnityEngine.Object.Instantiate(
                _view._tpl_AchvTabBar, _view.Content1111, false);
            barGo.name = "AchvTabBar(Runtime)";
            _tabBar = barGo.GetComponent<AchvTabBarBind>();
            if (_tabBar == null)
            {
                GameLog.Error("Achievement", "AchvTabBar 模板缺 Bind");
                UnityEngine.Object.Destroy(barGo);
                return;
            }
            _tabBar.Show();
            if (!ValidateHorizontalScroll(_tabBar.scroll, _tabBar.Content, "顶部页签列表"))
            {
                UnityEngine.Object.Destroy(barGo);
                _tabBar = null;
                return;
            }
            _tabsBuilt = true;
            BuildTabs();
        }

        private static ScrollRect ResolveNestedScroll(ScrollRect outer, RectTransform scope)
        {
            if (outer != null && outer.enabled && outer.content != null) return outer;
            if (scope != null)
            {
                ScrollRect nested = scope.GetComponentsInChildren<ScrollRect>(true)
                    .FirstOrDefault(item => item != null && item != outer
                        && item.enabled && item.content != null);
                if (nested != null) return nested;
            }
            return outer;
        }

        private static bool ValidateVerticalScroll(ScrollRect scroll,
            RectTransform expectedContent, string label)
        {
            bool pass = scroll != null && scroll.enabled && scroll.viewport != null
                && scroll.content != null && !scroll.horizontal && scroll.vertical
                && scroll.movementType == ScrollRect.MovementType.Clamped
                && (expectedContent == null || scroll.content == expectedContent)
                && scroll.content.GetComponent<VerticalLayoutGroup>() != null
                && scroll.content.GetComponent<ContentSizeFitter>() != null;
            if (!pass)
                GameLog.Error("Achievement", "AchvModule.prefab 的{0}结构无效，拒绝运行时改写布局", label);
            return pass;
        }

        private static bool ValidateHorizontalScroll(ScrollRect scroll,
            RectTransform expectedContent, string label)
        {
            bool pass = scroll != null && scroll.enabled && scroll.viewport != null
                && scroll.content != null && scroll.horizontal && !scroll.vertical
                && scroll.movementType == ScrollRect.MovementType.Clamped
                && (expectedContent == null || scroll.content == expectedContent)
                && scroll.content.GetComponent<HorizontalLayoutGroup>() != null
                && scroll.content.GetComponent<ContentSizeFitter>() != null;
            if (!pass)
                GameLog.Error("Achievement", "AchvModule.prefab 的{0}结构无效，拒绝运行时改写布局", label);
            return pass;
        }

        private static void BuildTabs()
        {
            if (_tabBar == null || _tabBar.Content == null) return;
            TopTabs.Clear();
            IReadOnlyList<AchievementConfigs.TypeRow> types = AchievementConfigs.GetTypes();
            for (int i = 0; i < types.Count; i++)
            {
                AchievementConfigs.TypeRow type = types[i];
                GameObject go = UnityEngine.Object.Instantiate(
                    _view._tpl_AchvTabBtn, _tabBar.Content, false);
                go.name = "AchvTab_" + type.Id;
                AchvTabBtnBind bind = go.GetComponent<AchvTabBtnBind>();
                if (bind == null)
                {
                    UnityEngine.Object.Destroy(go);
                    continue;
                }
                bind.Show();
                DisableRaycasts(go);
                BindClick(bind.tab, () => SelectTop(type.Id));
                if (bind.tab_txt != null) bind.tab_txt.text = type.Name;
                LayoutElement element = go.GetComponent<LayoutElement>();
                if (element == null)
                {
                    GameLog.Error("Achievement", "AchvTabBtn 模板缺少 Prefab LayoutElement");
                    UnityEngine.Object.Destroy(go);
                    continue;
                }
                RectTransform tabRoot = bind.transform as RectTransform;
                var state = new TopTabState
                {
                    Type = type,
                    Bind = bind,
                    Layout = element,
                    ExpandedWidth = tabRoot != null ? tabRoot.rect.width : element.preferredWidth,
                    CollapsedWidth = bind.tab != null ? bind.tab.rect.width : element.preferredWidth,
                };
                TopTabs.Add(state);

                List<RectTransform> subSlots = bind.subCon == null
                    ? new List<RectTransform>()
                    : bind.subCon.Cast<Transform>()
                        .Select(item => item as RectTransform)
                        .Where(item => item != null && item.name.StartsWith("__SubSlot", StringComparison.Ordinal))
                        .OrderBy(item => item.GetSiblingIndex())
                        .ToList();

                for (int subIndex = 0; subIndex < type.Subtypes.Count; subIndex++)
                {
                    ushort subtype = type.Subtypes[subIndex];
                    if (!AchievementConfigs.TryGetSubtype(subtype, out AchievementConfigs.SubtypeRow row))
                        continue;
                    if (subIndex >= subSlots.Count)
                    {
                        GameLog.Error("Achievement", "AchvTabBtn 子页签槽位不足 type={0} index={1}",
                            type.Id, subIndex);
                        continue;
                    }
                    GameObject subGo = UnityEngine.Object.Instantiate(
                        _view._tpl_AchvTabSubBtn, subSlots[subIndex], false);
                    subGo.name = "AchvSubTab_" + subtype;
                    AchvTabSubBtnBind subBind = subGo.GetComponent<AchvTabSubBtnBind>();
                    if (subBind == null)
                    {
                        UnityEngine.Object.Destroy(subGo);
                        continue;
                    }
                    subBind.Show();
                    DisableRaycasts(subGo);
                    if (subBind.btn_text != null) subBind.btn_text.text = row.Name;
                    ushort captured = subtype;
                    BindClick(subBind.sub_conta, () => Select(type.Id, captured, true));
                    state.Subs.Add(new SubTabState { Id = subtype, Bind = subBind });
                }
            }
            RefreshTabs();
        }

        private static void SelectTop(int typeId)
        {
            TopTabState state = TopTabs.FirstOrDefault(item => item.Type.Id == typeId);
            if (state == null) return;
            if (state.Type.Subtypes.Count == 0)
            {
                Select(typeId, 0, true);
                return;
            }
            ushort subtype = state.Type.Subtypes.FirstOrDefault(HasClaimableSubtype);
            if (subtype == 0) subtype = state.Type.Subtypes[0];
            Select(typeId, subtype, true);
        }

        private static void Select(int typeId, ushort subtype, bool resetScroll)
        {
            _selectedType = typeId;
            _selectedSubtype = subtype;
            if (_view != null)
            {
                _view.totalGp.gameObject.SetActive(typeId == 1);
                _view.dispersionGp.gameObject.SetActive(typeId != 1);
            }
            RefreshTabs();
            if (typeId == 1) RenderOverview();
            else RenderDetail();
            if (!resetScroll) return;
            ScrollRect scroll = typeId == 1 ? _overviewScroll : _detailScroll;
            if (scroll != null)
            {
                scroll.StopMovement();
                scroll.verticalNormalizedPosition = 1f;
            }
        }

        private static void ResolveInitialSelection()
        {
            if (_initialSelectionResolved || !AchievementModel.Instance.HasAllStartupData
                || !AchievementModel.Instance.TryGetCategory(OverviewCategory, out _)) return;
            _initialSelectionResolved = true;
            TopTabState overview = TopTabs.FirstOrDefault(item => item.Type.Id == 1);
            if (overview != null && HasClaimableType(overview.Type))
            {
                _selectedType = 1;
                _selectedSubtype = 0;
                return;
            }
            foreach (TopTabState state in TopTabs)
            {
                if (state.Type.Id == 1) continue;
                ushort subtype = state.Type.Subtypes.FirstOrDefault(HasClaimableSubtype);
                if (subtype == 0) continue;
                _selectedType = state.Type.Id;
                _selectedSubtype = subtype;
                return;
            }
            _selectedType = 1;
            _selectedSubtype = 0;
        }

        private static void RefreshTabs()
        {
            for (int i = 0; i < TopTabs.Count; i++)
            {
                TopTabState state = TopTabs[i];
                bool selected = state.Type.Id == _selectedType;
                if (state.Bind.btn_img != null)
                    state.Bind.btn_img.sprite = selected ? _topSelected : _topUnselected;
                if (state.Bind.tab_txt != null)
                {
                    state.Bind.tab_txt.color = new Color(1f, 1f, 0.85f, 1f);
                    state.Bind.tab_txt.outlineColor = selected
                        ? new Color(0.65f, 0.22f, 0.06f, 1f)
                        : new Color(0.38f, 0.42f, 0.52f, 1f);
                }
                if (state.Bind.subCon != null) state.Bind.subCon.gameObject.SetActive(selected && state.Subs.Count > 0);
                if (state.Bind.red_dot != null) state.Bind.red_dot.gameObject.SetActive(HasClaimableType(state.Type));
                if (state.Layout != null)
                {
                    state.Layout.preferredWidth = selected && state.Subs.Count > 0
                        ? state.ExpandedWidth
                        : state.CollapsedWidth;
                }

                for (int j = 0; j < state.Subs.Count; j++)
                {
                    SubTabState sub = state.Subs[j];
                    bool subSelected = selected && sub.Id == _selectedSubtype;
                    if (sub.Bind.btn_state != null)
                        sub.Bind.btn_state.sprite = subSelected ? _subSelected : _subUnselected;
                    if (sub.Bind.btn_text != null)
                    {
                        sub.Bind.btn_text.color = subSelected
                            ? new Color(1f, 1f, 0.86f, 1f)
                            : new Color(1f, 0.99f, 0.73f, 1f);
                        sub.Bind.btn_text.outlineColor = subSelected
                            ? new Color(0.65f, 0.22f, 0.08f, 1f)
                            : new Color(0.21f, 0.27f, 0.47f, 1f);
                    }
                    if (sub.Bind.red_dot != null)
                        sub.Bind.red_dot.gameObject.SetActive(HasClaimableSubtype(sub.Id));
                }
            }
            Rebuild(_tabBar != null ? _tabBar.Content : null);
        }

        private static bool HasClaimableType(AchievementConfigs.TypeRow type)
        {
            if (type == null) return false;
            if (type.Id == 1)
                return AchievementModel.Instance.Rewards.Any(item => item.Status == 1)
                    || CategoryHasClaimable(OverviewCategory);
            return type.Subtypes.Any(HasClaimableSubtype);
        }

        private static bool HasClaimableSubtype(ushort subtype)
        {
            IReadOnlyList<AchievementConfigs.CategoryRow> categories = AchievementConfigs.GetCategories(subtype);
            for (int i = 0; i < categories.Count; i++)
                if (CategoryHasClaimable(categories[i].Category)) return true;
            return false;
        }

        private static bool CategoryHasClaimable(byte category)
        {
            IReadOnlyList<AchievementModel.Entry> entries = AchievementModel.Instance.Entries;
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].Category == category && entries[i].Status == 1) return true;
            if (AchievementModel.Instance.TryGetCategory(category, out IReadOnlyList<AchievementModel.Entry> full))
                for (int i = 0; i < full.Count; i++) if (full[i].Status == 1) return true;
            return false;
        }

        private static void Render()
        {
            if (_view == null || !_view.gameObject.activeInHierarchy) return;
            ResolveInitialSelection();
            _view.totalGp.gameObject.SetActive(_selectedType == 1);
            _view.dispersionGp.gameObject.SetActive(_selectedType != 1);
            RefreshTabs();
            if (_selectedType == 1) RenderOverview();
            else RenderDetail();
        }

        private static void RenderOverview()
        {
            if (_view == null) return;
            RenderStage();
            RenderTypeCards();
            ClearObjects(TotalRows);
            if (AchievementModel.Instance.TryGetCategory(
                    OverviewCategory, out IReadOnlyList<AchievementModel.Entry> entries))
            {
                foreach (AchievementModel.Entry entry in SortEntries(entries))
                    CreateTotalRow(entry);
            }
            Rebuild(_overviewScroll != null ? _overviewScroll.content : null);
        }

        private static void RenderStage()
        {
            AchievementModel model = AchievementModel.Instance;
            uint totalNow = 0;
            uint totalMax = 0;
            for (int i = 0; i < model.Types.Count; i++)
            {
                totalNow += model.Types[i].NowStar;
                totalMax += model.Types[i].TotalStar;
            }
            if (_view.totalLb != null) _view.totalLb.text = totalNow + "/" + totalMax;
            SetRadial(_view.roundImg, totalMax > 0 ? (float)totalNow / totalMax : 0f);

            int currentStage = model.CurrentStage;
            int nextStage = currentStage + 1;
            if (_view.lv_label != null) _view.lv_label.text = "Lv." + currentStage;
            bool hasNext = AchievementConfigs.TryGetStage(nextStage, out AchievementConfigs.StageRow next);
            uint currentStar = model.Star % 100U;
            bool canClaim = hasNext && next.RequiredStar > 0 && currentStar >= next.RequiredStar
                && !AchievementController.Instance.IsStageClaimPending;
            if (_view.exp_label != null)
                _view.exp_label.text = hasNext ? currentStar + "/" + next.RequiredStar : "已满级";
            RectTransform progressViewport = _view._pg_main_panel != null
                ? _view._pg_main_panel.transform as RectTransform
                : null;
            if (progressViewport != null)
            {
                float ratio = hasNext && next.RequiredStar > 0
                    ? Mathf.Clamp01((float)currentStar / next.RequiredStar)
                    : 1f;
                float maxWidth = _view._box_pg != null ? _view._box_pg.rect.width : 0f;
                progressViewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxWidth * ratio);
            }
            if (_view.active_btn != null) _view.active_btn.gameObject.SetActive(hasNext);
            if (_view._Image1 != null) UIGrayStyle.Apply(_view._Image1, !canClaim);
            if (_view.red_dot != null) _view.red_dot.gameObject.SetActive(canClaim);
            if (_view.labelDisplay != null) _view.labelDisplay.text = "升级";
            if (!_stageClickBound)
            {
                BindClick(_view.active_btn, ClaimStage);
                _stageClickBound = true;
            }
            RenderAttributes(currentStage, nextStage);
        }

        private static void RenderAttributes(int currentStage, int nextStage)
        {
            ClearObjects(AttributeRows);
            AchievementConfigs.TryGetStage(currentStage, out AchievementConfigs.StageRow current);
            AchievementConfigs.TryGetStage(nextStage, out AchievementConfigs.StageRow next);
            var ids = new List<int>();
            if (current != null)
                for (int i = 0; i < current.Attributes.Count; i++) ids.Add(current.Attributes[i].Id);
            if (next != null)
                for (int i = 0; i < next.Attributes.Count; i++)
                    if (!ids.Contains(next.Attributes[i].Id)) ids.Add(next.Attributes[i].Id);

            for (int i = 0; i < ids.Count; i++)
            {
                int id = ids[i];
                long cur = AttributeAmount(current, id);
                long upcoming = AttributeAmount(next, id);
                GameObject go = UnityEngine.Object.Instantiate(
                    _view._tpl_AchvPropItem, _view.Content11, false);
                go.name = "AchvAttr_" + id;
                AchvPropItemBind bind = go.GetComponent<AchvPropItemBind>();
                if (bind == null)
                {
                    UnityEngine.Object.Destroy(go);
                    continue;
                }
                bind.Show();
                string name = GoodsModel.GetAttrName(id);
                if (string.IsNullOrEmpty(name)) name = "属性" + id;
                if (bind.name_label != null) bind.name_label.text = name;
                if (bind.cur_value != null) bind.cur_value.text = GoodsModel.FormatAttrValue(id, cur);
                bool showNext = next != null;
                if (bind.arrow_img != null) bind.arrow_img.gameObject.SetActive(showNext);
                if (bind.name_label1 != null)
                {
                    bind.name_label1.gameObject.SetActive(showNext);
                    bind.name_label1.text = name;
                }
                if (bind.next_value != null)
                {
                    bind.next_value.gameObject.SetActive(showNext);
                    bind.next_value.text = GoodsModel.FormatAttrValue(id, upcoming);
                }
                AttributeRows.Add(go);
            }
            Rebuild(_view.Content11);
        }

        private static long AttributeAmount(AchievementConfigs.StageRow row, int id)
        {
            if (row == null) return 0L;
            for (int i = 0; i < row.Attributes.Count; i++)
                if (row.Attributes[i].Id == id) return row.Attributes[i].Value;
            return 0L;
        }

        private static void RenderTypeCards()
        {
            ClearObjects(TypeCards);
            IReadOnlyList<AchievementConfigs.TypeRow> types = AchievementConfigs.GetTypes();
            for (int i = 0; i < types.Count; i++)
            {
                AchievementConfigs.TypeRow type = types[i];
                if (type.Id == 1) continue;
                GameObject go = UnityEngine.Object.Instantiate(
                    _view._tpl_AchvChildItem, _view.Content111, false);
                go.name = "AchvTypeProgress_" + type.Id;
                AchvChildItemBind bind = go.GetComponent<AchvChildItemBind>();
                if (bind == null)
                {
                    UnityEngine.Object.Destroy(go);
                    continue;
                }
                bind.Show();
                if (bind.nameLb != null) bind.nameLb.text = type.Name;
                _ = ResManager.SetImageAsync(bind.typeImg,
                    GameResPath.GetIcon("achv", "uicj_0" + (14 + type.Id)), false, false);
                AchievementModel.TypeStar value = AchievementModel.Instance.Types
                    .FirstOrDefault(item => item.Type == type.Id);
                uint now = value?.NowStar ?? 0U;
                uint max = value?.TotalStar ?? 0U;
                if (bind.scheduleLb != null) bind.scheduleLb.text = now + "/" + max;
                RectTransform viewport = bind._pg_panal != null
                    ? bind._pg_panal.transform as RectTransform
                    : null;
                if (viewport != null)
                    viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                        bind._box_pg.rect.width * (max > 0 ? Mathf.Clamp01((float)now / max) : 0f));
                TypeCards.Add(go);
            }
            Rebuild(_view.Content111);
        }

        private static void CreateTotalRow(AchievementModel.Entry entry)
        {
            if (!AchievementConfigs.TryGetEntry(entry.Id, out AchievementConfigs.EntryRow config)) return;
            GameObject go = UnityEngine.Object.Instantiate(
                _view._tpl_AchvTotalItem, _overviewScroll.content, false);
            go.name = "AchvTotalItem_" + entry.Id;
            AchvTotalItemBind bind = go.GetComponent<AchvTotalItemBind>();
            if (bind == null)
            {
                UnityEngine.Object.Destroy(go);
                return;
            }
            bind.Show();
            if (bind.titleLb != null)
            {
                string title = AchievementConfigs.GetOverviewTitle(entry.Id);
                bind.titleLb.text = string.IsNullOrEmpty(title) ? config.Description : title;
            }
            if (bind.desLb != null)
                bind.desLb.text = config.Description + ProgressSuffix(entry, config);
            ApplyEntryStatus(bind.receiveBtn, bind.receivedImg, bind.unfinishLb, bind.reddot,
                entry.Status, () => ClaimEntry(entry, bind.rewardGp));
            if (config.Rewards.Count > 0)
                CreateAward(config.Rewards[0], bind.rewardGp, null, 0.44f);
            TotalRows.Add(go);
        }

        private static void RenderDetail()
        {
            ClearObjects(DetailRows);
            if (_detailScroll == null || _detailScroll.content == null || _selectedSubtype == 0) return;
            var categories = new HashSet<byte>(
                AchievementConfigs.GetCategories(_selectedSubtype).Select(item => item.Category));
            List<AchievementModel.Entry> entries = AchievementModel.Instance.Entries
                .Where(item => categories.Contains(item.Category))
                .ToList();
            foreach (AchievementModel.Entry entry in SortEntries(entries)) CreateDetailRow(entry);
            Rebuild(_detailScroll.content);
        }

        private static void CreateDetailRow(AchievementModel.Entry entry)
        {
            if (!AchievementConfigs.TryGetEntry(entry.Id, out AchievementConfigs.EntryRow config)) return;
            GameObject go = UnityEngine.Object.Instantiate(
                _detailTemplate.gameObject, _detailScroll.content, false);
            go.name = "AchvSubItem_" + entry.Id;
            AchvSubItemBind bind = go.GetComponent<AchvSubItemBind>();
            if (bind == null)
            {
                UnityEngine.Object.Destroy(go);
                return;
            }
            bind.Show();
            if (bind._tpl_BaseAwardItem != null) bind._tpl_BaseAwardItem.SetActive(false);
            if (bind.titleLb != null) bind.titleLb.text = config.Description;
            ulong target = Math.Max(1UL, config.Target);
            ulong shown = config.ShowProgress ? Math.Min(entry.Progress, target) : (entry.Status == 0 ? 0UL : 1UL);
            ulong shownTarget = config.ShowProgress ? target : 1UL;
            if (bind.expLb != null) bind.expLb.text = shown + "/" + shownTarget;
            SetRadial(bind._progress, shownTarget > 0 ? (float)((double)shown / shownTarget) : 0f);
            ApplyEntryStatus(bind.receiveBtn, bind.receivedImg, bind.unfinishLb, bind.reddot,
                entry.Status, () => ClaimEntry(entry, bind.receiveBtn));

            if (bind._Scroller1 != null)
            {
                if (!ValidateHorizontalScroll(bind._Scroller1, bind.Content, "详情奖励列表"))
                {
                    UnityEngine.Object.Destroy(go);
                    return;
                }
            }
            CreateAward(new AchievementConfigs.RewardTriple(0, 40, config.Star),
                bind.Content, bind._tpl_BaseAwardItem, 0.63f);
            for (int i = 0; i < config.Rewards.Count; i++)
                CreateAward(config.Rewards[i], bind.Content, bind._tpl_BaseAwardItem, 0.63f);
            Rebuild(bind.Content);
            DetailRows.Add(go);
        }

        private static void CreateAward(AchievementConfigs.RewardTriple reward,
            RectTransform parent, GameObject template, float scale)
        {
            if (parent == null || reward.Count <= 0) return;
            GameObject source = template != null ? template : _awardPrefab;
            GameObject go = source != null
                ? UnityEngine.Object.Instantiate(source, parent, false)
                : null;
            if (go == null)
            {
                GameLog.Warn("Achievement", "成就奖励槽缺共享 BaseAwardItem 模板 type={0} id={1}",
                    reward.Type, reward.TypeId);
                return;
            }
            go.name = "BaseAwardItem(Achievement)";
            BaseAwardItem item = go.GetComponent<BaseAwardItem>()
                ?? go.GetComponentInChildren<BaseAwardItem>(true);
            if (item == null)
            {
                UnityEngine.Object.Destroy(go);
                return;
            }
            item.Show();
            (int goodsId, int locked) = GoodsModel.GetMappingTypeId(reward.Type, reward.TypeId);
            item.SetData(goodsId, reward.Count, locked != 0);
            item.SetScale(scale);
        }

        private static string ProgressSuffix(AchievementModel.Entry entry,
            AchievementConfigs.EntryRow config)
        {
            ulong target = Math.Max(1UL, config.Target);
            ulong shown = config.ShowProgress
                ? Math.Min(entry.Progress, target)
                : (entry.Status == 0 ? 0UL : 1UL);
            return "（" + shown + "/" + (config.ShowProgress ? target : 1UL) + "）";
        }

        private static IEnumerable<AchievementModel.Entry> SortEntries(
            IEnumerable<AchievementModel.Entry> entries)
        {
            return entries.OrderBy(item => item.Status == 1 ? 0 : item.Status == 0 ? 1 : 2)
                .ThenByDescending(item => item.Id);
        }

        private static void ApplyEntryStatus(RectTransform receiveButton, Image received,
            TMPro.TextMeshProUGUI unfinished, Image red, byte status, Action claim)
        {
            bool claimable = status == 1 && !AchievementController.Instance.IsEntryClaimPending;
            if (receiveButton != null) receiveButton.gameObject.SetActive(status == 1);
            if (received != null) received.gameObject.SetActive(status == 2);
            if (unfinished != null) unfinished.gameObject.SetActive(status == 0);
            if (red != null) red.gameObject.SetActive(claimable);
            if (status == 1) BindClick(receiveButton, claim);
        }

        private static void ClaimStage()
        {
            if (!AchievementModel.Instance.HasStageData) return;
            uint stage = (uint)AchievementModel.Instance.CurrentStage + 1U;
            if (!AchievementConfigs.TryGetStage((int)stage, out AchievementConfigs.StageRow next)) return;
            uint currentStar = AchievementModel.Instance.Star % 100U;
            if (next.RequiredStar == 0 || currentStar < next.RequiredStar)
            {
                TipsManager.Toast("成就点不足");
                return;
            }
            AchievementController.Instance.RequestStageClaim(stage);
            RenderStage();
        }

        private static void ClaimEntry(AchievementModel.Entry entry, RectTransform source)
        {
            if (entry == null || entry.Status != 1) return;
            BagModel bag = BagModel.Instance;
            if (bag.HasData && bag.MaxCell - bag.BagGoodsList.Count < 5)
            {
                TipsManager.Confirm("背包空间不足，是否前往整理？", OpenBagForSorting,
                    null, "前往整理", "取消");
                return;
            }

            _pendingRewardEntryId = entry.Id;
            _pendingRewardSourceWorld = source != null
                ? source.TransformPoint(new Vector3(source.rect.xMin, source.rect.yMax, 0f))
                : Vector3.zero;
            if (!AchievementController.Instance.RequestEntryClaim(entry.Id, entry.Category))
            {
                ClearPendingRewardSource();
                return;
            }
            Render();
        }

        private static void Subscribe()
        {
            if (_subscribed) return;
            AchievementModel.Instance.Changed += OnChanged;
            AchievementModel.Instance.OperationCompleted += OnOperationCompleted;
            _subscribed = true;
        }

        private static void Unsubscribe()
        {
            if (!_subscribed) return;
            AchievementModel.Instance.Changed -= OnChanged;
            AchievementModel.Instance.OperationCompleted -= OnOperationCompleted;
            _subscribed = false;
        }

        private static void OnChanged() => Render();

        private static void OnOperationCompleted(AchievementModel.OperationResult result)
        {
            if (result == null) return;
            if (result.Success)
            {
                TipsManager.Toast(result.Kind == AchievementModel.OperationKind.StageClaim
                    ? "成就等级提升成功"
                    : "成就奖励领取成功");
                if (result.Kind == AchievementModel.OperationKind.StageClaim)
                {
                    PlayStageSuccessEffect();
                }
                else if (_pendingRewardEntryId == result.TargetId
                    && AchievementConfigs.TryGetEntry(result.TargetId,
                        out AchievementConfigs.EntryRow entry))
                {
                    PlayRewardFly(entry.Rewards, _pendingRewardSourceWorld);
                }
            }
            else
            {
                TipsManager.Toast("领取失败，错误码 " + result.ErrorCode);
            }
            if (result.Kind == AchievementModel.OperationKind.EntryClaim)
                ClearPendingRewardSource();
            Render();
        }

        private static void OpenBagForSorting()
        {
            Close();
            BagFlow.Open();
        }

        private static void PlayStageSuccessEffect()
        {
            int epoch = ++_stageSuccessEffectEpoch;
            UIEffectStage.Handle previous = _stageSuccessEffect;
            _stageSuccessEffect = null;
            previous?.Dispose();
            _ = PlayStageSuccessEffectAsync(epoch);
        }

        private static async Task PlayStageSuccessEffectAsync(int epoch)
        {
            UIEffectStage.Handle handle = null;
            try
            {
                RectTransform parent = ViewManager.GetLayer(UILayer.Top) as RectTransform;
                if (parent == null)
                {
                    GameLog.Warn("Achievement", "stage success effect skipped: Top layer missing");
                    return;
                }

                // 老端 PlayBigEffect 默认参数：MainUIEffectView(FightingUp 层)、pos=(0,2)、scale=1，
                // 外层 15 秒自动清理。Unity 的最高 Top 层承担同一全屏归属。
                handle = await UIEffectStage.AddAsync("ui_shengjitexiao", parent,
                    new Vector2(0f, 2f), Vector3.one);
                if (epoch != _stageSuccessEffectEpoch)
                {
                    handle?.Dispose();
                    return;
                }
                if (handle == null)
                {
                    GameLog.Warn("Achievement", "stage success effect failed: ui_shengjitexiao");
                    return;
                }
                _stageSuccessEffect = handle;
                await TimeUtil.Delay(StageSuccessEffectDurationMs);
            }
            catch (Exception ex)
            {
                GameLog.Warn("Achievement", "stage success effect failed: {0}", ex.Message);
            }
            finally
            {
                if (epoch == _stageSuccessEffectEpoch && ReferenceEquals(_stageSuccessEffect, handle))
                    _stageSuccessEffect = null;
                handle?.Dispose();
            }
        }

        private static void PlayRewardFly(
            IReadOnlyList<AchievementConfigs.RewardTriple> rewards, Vector3 sourceWorld)
        {
            for (int i = RewardFlyHandles.Count - 1; i >= 0; i--)
            {
                RewardFlyService.Handle old = RewardFlyHandles[i];
                if (old != null && !old.IsDisposed && !old.IsCompleted) continue;
                old?.Dispose();
                RewardFlyHandles.RemoveAt(i);
            }

            if (rewards == null || rewards.Count == 0) return;
            var values = new List<RewardFlyService.Reward>(rewards.Count);
            for (int i = 0; i < rewards.Count; i++)
            {
                AchievementConfigs.RewardTriple reward = rewards[i];
                values.Add(new RewardFlyService.Reward(reward.Type, reward.TypeId, reward.Count));
            }
            RewardFlyHandles.Add(RewardFlyService.Play(values, sourceWorld));
        }

        private static void ClearPendingRewardSource()
        {
            _pendingRewardEntryId = 0;
            _pendingRewardSourceWorld = Vector3.zero;
        }

        private static void ClearStageSuccessEffect()
        {
            _stageSuccessEffectEpoch++;
            UIEffectStage.Handle handle = _stageSuccessEffect;
            _stageSuccessEffect = null;
            handle?.Dispose();
        }

        private static void ClearRewardFly()
        {
            for (int i = 0; i < RewardFlyHandles.Count; i++)
                RewardFlyHandles[i]?.Dispose();
            RewardFlyHandles.Clear();
        }

        private static void ReturnToRole()
        {
            _ = AudioManager.PlayUi("openorclosebutton");
            Close();
            RoleFlow.Open();
        }

        private static void BindClick(RectTransform target, Action action)
        {
            if (target == null) return;
            Image image = target.GetComponent<Image>() ?? target.GetComponentInChildren<Image>(true);
            if (image == null) return;
            image.raycastTarget = true;
            UIUtil.AddClick(image, action);
        }

        private static void DisableRaycasts(GameObject root)
        {
            foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
        }

        private static void SetRadial(Image image, float ratio)
        {
            if (image == null) return;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = 2;
            image.fillClockwise = true;
            image.fillAmount = Mathf.Clamp01(ratio);
        }

        private static void Rebuild(RectTransform content)
        {
            if (content == null) return;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }

        private static void ClearObjects(List<GameObject> objects)
        {
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i] == null) continue;
                objects[i].SetActive(false);
                UnityEngine.Object.Destroy(objects[i]);
            }
            objects.Clear();
        }

        private static void ReleaseView()
        {
            Unsubscribe();
            ClearObjects(TotalRows);
            ClearObjects(DetailRows);
            ClearObjects(TypeCards);
            ClearObjects(AttributeRows);
            TopTabs.Clear();
            _window?.SetReturnAction(null);
            if (_frameRoot != null) ResManager.ReleaseInstance(_frameRoot);
            if (_moduleRoot != null) ResManager.ReleaseInstance(_moduleRoot);
            if (_topSelected != null) ResManager.Release(_topSelected);
            if (_topUnselected != null) ResManager.Release(_topUnselected);
            if (_subSelected != null) ResManager.Release(_subSelected);
            if (_subUnselected != null) ResManager.Release(_subUnselected);
            if (_awardPrefab != null) ResManager.Release(_awardPrefab);
            _frameRoot = null;
            _moduleRoot = null;
            _window = null;
            _view = null;
            _detailTemplate = null;
            _tabBar = null;
            _overviewScroll = null;
            _detailScroll = null;
            _topSelected = null;
            _topUnselected = null;
            _subSelected = null;
            _subUnselected = null;
            _awardPrefab = null;
            _tabsBuilt = false;
            _stageClickBound = false;
            _initialSelectionResolved = false;
            _selectedType = 1;
            _selectedSubtype = 0;
        }

        internal static void Reset()
        {
            Close();
            ClearStageSuccessEffect();
            ClearRewardFly();
            ClearPendingRewardSource();
            ReleaseView();
            AchievementController.Instance.Dispose();
            _loading = false;
        }
    }
}
