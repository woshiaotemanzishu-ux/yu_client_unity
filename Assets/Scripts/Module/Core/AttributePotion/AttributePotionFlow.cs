using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shenxiao.Common.Audio;
using Shenxiao.Common.Tips;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.AttributePotion;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.AttributePotion
{
    /// <summary>
    /// 人物页属性药剂弹层。视觉结构完全取 AttributePotionModule.prefab；运行时只装配
    /// 4 档 tab、每档 4 条药剂数据、物品详情、21702 使用事务与首次使用引导。
    /// </summary>
    public static class AttributePotionFlow
    {
        private const string FeatureView = "attributePotionView";
        private const int GuideModule = 300;
        private const int GuideSubModule = 1;
        private const int GuideType = 1;
        private static readonly string[] TierNames = { "", "初级", "中级", "高级", "顶级" };
        private static readonly string[] DarkQualityColors =
        {
            "#663915", "#3cad66", "#5099dd", "#b55eec", "#e17547",
            "#ef4848", "#cd9222", "#f56ebd", "#8a8a8a",
        };
        private static readonly object GuideOwner = new object();
        private static readonly List<TabState> Tabs = new List<TabState>(4);
        private static readonly List<ItemState> Items = new List<ItemState>(4);
        private static readonly List<GameObject> AwardRoots = new List<GameObject>(4);

        private sealed class TabState
        {
            public byte Tier;
            public AttributePotionTabBind Bind;
        }

        private sealed class ItemState
        {
            public AttributePotionConfigs.Potion Potion;
            public AttributePotionItemBind Bind;
            public AttributePotionProgressBarBind Progress;
            public BaseAwardItem Award;
        }

        private static GameObject _moduleRoot;
        private static AttributePotionViewBind _view;
        private static Image _mask;
        private static ScrollRect _itemScroll;
        private static ScrollRect _tabScroll;
        private static Sprite _selectedTabSprite;
        private static Sprite _unselectedTabSprite;
        private static UIEffectStage.Handle _useEffect;
        private static RectTransform _useEffectHost;
        private static RectTransform _guideTarget;
        private static bool _loading;
        private static bool _subscribed;
        private static bool _guideCompletionSent;
        private static byte _selectedTier = 1;
        private static int _openEpoch;
        private static int _effectEpoch;

        public static void Open()
        {
            if (_loading) return;
            _ = OpenAsync(++_openEpoch);
        }

        public static void Close()
        {
            _openEpoch++;
            HideGuide();
            ClearUseEffect();
            if (_view != null && _view.IsShown) _view.Hide();
            if (_moduleRoot != null) _moduleRoot.SetActive(false);
            _selectedTier = 1;
        }

        public static bool HasAnyUsable()
        {
            if (!AttributePotionConfigs.IsLoaded) return false;
            for (byte tier = 1; tier <= 4; tier++)
            {
                IReadOnlyList<AttributePotionConfigs.Potion> rows = AttributePotionConfigs.GetPotions(tier);
                for (int i = 0; i < rows.Count; i++)
                    if (IsUsable(rows[i])) return true;
            }
            return false;
        }

        private static async Task OpenAsync(int epoch)
        {
            if (_loading) return;
            _loading = true;
            try
            {
                await Task.WhenAll(
                    AttributePotionConfigs.EnsureLoaded(),
                    GoodsModel.EnsureLoaded(),
                    FuncOpenConfig.EnsureLoaded());
                if (epoch != _openEpoch) return;
                if (!FuncOpenConfig.CheckFuncOpenState(FeatureView))
                {
                    TipsManager.Toast("完成主线任务【139】开启");
                    return;
                }

                AttributePotionController.Instance.Init();
                if (!await EnsureViewAsync()) return;
                if (epoch != _openEpoch) return;
                Subscribe();

                _moduleRoot.SetActive(true);
                _mask.gameObject.SetActive(true);
                _mask.transform.SetAsFirstSibling();
                _view.Show();
                _view.transform.SetAsLastSibling();

                _selectedTier = FindFirstUsableTier();
                SelectTier(_selectedTier);
            }
            finally
            {
                _loading = false;
            }
        }

        private static async Task<bool> EnsureViewAsync()
        {
            if (_moduleRoot != null && _view != null && Items.Count == 4 && Tabs.Count == 4) return true;

            string key = GameResPath.GetUIPrefab("attributePotion", "AttributePotionModule");
            _moduleRoot = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Popup));
            if (_moduleRoot == null)
            {
                GameLog.Error("AttributePotion", "AttributePotionModule load failed: {0}", key);
                return false;
            }
            _moduleRoot.name = "AttributePotionModule(Runtime)";
            // 首次冷加载期间先整体隐藏；页签图、四个物品格和绑定全部 ready 后再一次性揭开，
            // 避免只剩全屏遮罩或半成品列表的中间帧。
            _moduleRoot.SetActive(false);
            foreach (BaseView child in _moduleRoot.GetComponentsInChildren<BaseView>(true))
                child.gameObject.SetActive(false);

            _view = _moduleRoot.GetComponentInChildren<AttributePotionViewBind>(true);
            _mask = _moduleRoot.transform.Find("__ModalDim")?.GetComponent<Image>();
            if (_view == null || _mask == null)
            {
                GameLog.Error("AttributePotion", "AttributePotionModule missing view or prefab-owned __ModalDim");
                ReleaseModule();
                return false;
            }

            ScrollRect convertedListScroll = _view.Content != null
                ? _view.Content.GetComponentInChildren<ScrollRect>(true)
                : null;
            _itemScroll = convertedListScroll != null ? convertedListScroll : _view._Scroller1;
            if (_view._Scroller1 != null && convertedListScroll != null
                && convertedListScroll != _view._Scroller1)
            {
                // Laya Panel 内嵌 List 被转换器拆成了两层 ScrollRect。外层只保留裁剪，
                // 手势和内容移动统一交给满足 ScrollRect→Viewport→Content 的内层 List。
                _view._Scroller1.enabled = false;
                convertedListScroll.enabled = true;
            }
            _tabScroll = _view.Content1;
            if (_itemScroll == null || _itemScroll.content == null || _tabScroll == null || _tabScroll.content == null)
            {
                GameLog.Error("AttributePotion", "AttributePotionModule scroll structure incomplete");
                ReleaseModule();
                return false;
            }

            _view._tpl_attributePotionItem?.SetActive(false);
            _view._tpl_attributePotionProgressBar?.SetActive(false);
            _view._tpl_attributePotionTab?.SetActive(false);
            _unselectedTabSprite = _view._tpl_attributePotionTab != null
                ? _view._tpl_attributePotionTab.GetComponent<AttributePotionTabBind>()?._Image1?.sprite
                : null;
            _selectedTabSprite = await ResManager.LoadAsync<Sprite>(
                GameResPath.GetIcon("attributePotion", "uitc_006"));

            BindClose(_view._btn_close);
            _mask.raycastTarget = true;
            UIUtil.AddClick(_mask, CloseWithSound);
            if (!await BuildRuntimeAsync())
            {
                ReleaseModule();
                return false;
            }
            _moduleRoot.SetActive(false);
            return true;
        }

        private static async Task<bool> BuildRuntimeAsync()
        {
            if (_view._tpl_attributePotionTab == null
                || _view._tpl_attributePotionItem == null
                || _view._tpl_attributePotionProgressBar == null) return false;

            for (byte tier = 1; tier <= 4; tier++)
            {
                GameObject go = UnityEngine.Object.Instantiate(
                    _view._tpl_attributePotionTab, _tabScroll.content, false);
                go.name = "attributePotionTab_Runtime_" + tier;
                AttributePotionTabBind bind = go.GetComponent<AttributePotionTabBind>();
                if (bind == null) return false;
                bind.Show();
                foreach (Graphic graphic in go.GetComponentsInChildren<Graphic>(true))
                    graphic.raycastTarget = false;
                bind._Image1.raycastTarget = true;
                byte capturedTier = tier;
                UIUtil.AddClick(bind._Image1, () => SelectTier(capturedTier));
                Tabs.Add(new TabState { Tier = tier, Bind = bind });
            }

            var awardTasks = new List<Task<GameObject>>(4);
            for (int i = 0; i < 4; i++)
            {
                GameObject go = UnityEngine.Object.Instantiate(
                    _view._tpl_attributePotionItem, _itemScroll.content, false);
                go.name = "attributePotionItem_Runtime_" + i;
                AttributePotionItemBind bind = go.GetComponent<AttributePotionItemBind>();
                if (bind == null) return false;
                bind.Show();
                foreach (Graphic graphic in go.GetComponentsInChildren<Graphic>(true))
                    graphic.raycastTarget = false;

                GameObject progressGo = UnityEngine.Object.Instantiate(
                    _view._tpl_attributePotionProgressBar, bind.pbox, false);
                progressGo.name = "attributePotionProgressBar_Runtime";
                AttributePotionProgressBarBind progress = progressGo.GetComponent<AttributePotionProgressBarBind>();
                if (progress == null) return false;
                progress.Show();

                var state = new ItemState { Bind = bind, Progress = progress };
                Items.Add(state);
                if (bind._Image1 != null)
                {
                    bind._Image1.raycastTarget = true;
                    UIUtil.AddClick(bind._Image1, () => Use(state));
                }
                awardTasks.Add(ResManager.InstantiateAsync(
                    GameResPath.GetUIPrefab("common", "BaseAwardItem"), bind._gp_reward));
            }

            GameObject[] awards = await Task.WhenAll(awardTasks);
            for (int i = 0; i < awards.Length; i++)
            {
                GameObject awardRoot = awards[i];
                if (awardRoot == null) return false;
                awardRoot.name = "BaseAwardItem(AttributePotion_" + i + ")";
                AwardRoots.Add(awardRoot);
                RectTransform rect = awardRoot.transform as RectTransform;
                if (rect != null)
                {
                    rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 1f);
                    rect.anchoredPosition = Vector2.zero;
                    rect.localScale = Vector3.one;
                }
                Items[i].Award = awardRoot.GetComponent<BaseAwardItem>()
                    ?? awardRoot.GetComponentInChildren<BaseAwardItem>(true);
                if (Items[i].Award == null) return false;
                Items[i].Award.Show();
                // 老端 attributePotionItem.dataChanged 明确调用 SetScale(0.8)。父容器已有
                // 0.8 缩放，两级缩放共同得到截图中约 80px 的物品格。
                Items[i].Award.SetScale(0.8f);
            }
            return true;
        }

        private static void SelectTier(byte tier)
        {
            if (tier < 1 || tier > 4) tier = 1;
            _selectedTier = tier;
            if (!AttributePotionModel.Instance.HasLevel(tier))
                AttributePotionController.Instance.RequestLevel(tier);

            RenderTabs();
            RenderItems();
            RebuildRuntimeLayout(true);
        }

        private static void RebuildRuntimeLayout(bool resetItemToTop)
        {
            Canvas.ForceUpdateCanvases();
            if (_itemScroll != null && _itemScroll.content != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_itemScroll.content);
            if (_tabScroll != null && _tabScroll.content != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_tabScroll.content);
            Canvas.ForceUpdateCanvases();
            if (!resetItemToTop || _itemScroll == null) return;
            _itemScroll.StopMovement();
            _itemScroll.verticalNormalizedPosition = 1f;
        }

        private static void RenderTabs()
        {
            for (int i = 0; i < Tabs.Count; i++)
            {
                TabState state = Tabs[i];
                if (state.Bind._lb_name != null) state.Bind._lb_name.text = TierNames[state.Tier];
                if (state.Bind._Image1 != null)
                    state.Bind._Image1.sprite = state.Tier == _selectedTier && _selectedTabSprite != null
                        ? _selectedTabSprite
                        : _unselectedTabSprite;
                if (state.Bind._red_dot != null)
                    state.Bind._red_dot.gameObject.SetActive(TierHasUsable(state.Tier));
                if (state.Bind.iconDisplay != null) state.Bind.iconDisplay.gameObject.SetActive(false);
            }
        }

        private static void RenderItems()
        {
            List<AttributePotionConfigs.Potion> rows = AttributePotionConfigs.GetPotions(_selectedTier)
                .OrderByDescending(IsUsable)
                .ThenByDescending(x => x.GoodsId)
                .ToList();
            for (int i = 0; i < Items.Count; i++)
            {
                ItemState state = Items[i];
                bool visible = i < rows.Count;
                state.Bind.gameObject.SetActive(visible);
                if (!visible)
                {
                    state.Potion = null;
                    continue;
                }

                state.Potion = rows[i];
                RenderItem(state);
            }
            RefreshGuide();
        }

        private static void RenderItem(ItemState state)
        {
            AttributePotionConfigs.Potion potion = state.Potion;
            long bagCount = Math.Max(0L, BagModel.Instance.GetTypeGoodsNum(potion.GoodsId));
            state.Award.SetData(potion.GoodsId, bagCount);

            if (state.Bind._lb_name != null)
            {
                string name = GoodsModel.GetGoodsName(potion.GoodsId);
                state.Bind._lb_name.text = string.IsNullOrEmpty(name) ? potion.GoodsId.ToString() : name;
                state.Bind._lb_name.color = QualityColor(GoodsModel.GetColor(potion.GoodsId));
            }

            AttributePotionModel.Instance.TryGet(potion.Level, potion.GoodsId, out AttributePotionModel.Count count);
            if (state.Bind._lb_attr != null)
                state.Bind._lb_attr.text = BuildAttrText(potion, count?.CurrentCount ?? 0UL);

            bool hasLimit = AttributePotionConfigs.TryGetLimit(
                potion.GoodsId, RoleModel.Instance.Level, out AttributePotionConfigs.Limit limit);
            ulong current = count?.CurrentCount ?? 0UL;
            ulong maximum = hasLimit ? limit.AllTimes : 0UL;
            if (state.Progress.labelDisplay != null)
                state.Progress.labelDisplay.text = current + "/" + maximum;
            if (state.Progress.thumb != null)
            {
                float ratio = maximum > 0 ? Mathf.Clamp01((float)((double)current / maximum)) : 0f;
                state.Progress.thumb.rectTransform.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal, 200f * ratio);
            }

            bool usable = IsUsable(potion);
            if (state.Bind._red_dot != null) state.Bind._red_dot.gameObject.SetActive(usable);
            if (state.Bind.effBox != null && state.Bind.effBox != _useEffectHost)
                state.Bind.effBox.gameObject.SetActive(false);
        }

        private static string BuildAttrText(AttributePotionConfigs.Potion potion, ulong currentCount)
        {
            if (potion == null || potion.Attrs.Count == 0) return string.Empty;
            var parts = new List<string>(potion.Attrs.Count);
            for (int i = 0; i < potion.Attrs.Count; i++)
            {
                AttributePotionConfigs.Attr attr = potion.Attrs[i];
                string name = GoodsModel.GetAttrName(attr.Id);
                if (string.IsNullOrEmpty(name)) name = "属性" + attr.Id;
                decimal total = attr.Value * (decimal)currentCount;
                parts.Add(name + " + " + total.ToString("0"));
            }
            return string.Join(" ", parts) + " ";
        }

        private static Color QualityColor(int quality)
        {
            int index = Mathf.Clamp(quality, 0, DarkQualityColors.Length - 1);
            return ColorUtility.TryParseHtmlString(DarkQualityColors[index], out Color color)
                ? color
                : new Color(0.4f, 0.22f, 0.08f, 1f);
        }

        private static bool IsUsable(AttributePotionConfigs.Potion potion)
        {
            if (potion == null
                || !AttributePotionModel.Instance.TryGet(potion.Level, potion.GoodsId, out AttributePotionModel.Count count)
                || !AttributePotionConfigs.TryGetLimit(potion.GoodsId, RoleModel.Instance.Level, out AttributePotionConfigs.Limit limit))
                return false;
            return BagModel.Instance.GetTypeGoodsNum(potion.GoodsId) > 0
                && count.CurrentDayCount < limit.DayTimes
                && count.CurrentCount < limit.AllTimes;
        }

        private static bool TierHasUsable(byte tier)
        {
            IReadOnlyList<AttributePotionConfigs.Potion> rows = AttributePotionConfigs.GetPotions(tier);
            for (int i = 0; i < rows.Count; i++) if (IsUsable(rows[i])) return true;
            return false;
        }

        private static byte FindFirstUsableTier()
        {
            for (byte tier = 1; tier <= 4; tier++) if (TierHasUsable(tier)) return tier;
            return 1;
        }

        private static void Use(ItemState state)
        {
            if (state?.Potion == null) return;
            bool completesFirstGuide = Items.Count > 0
                && ReferenceEquals(state, Items[0])
                && BagModel.Instance.GetTypeGoodsNum(state.Potion.GoodsId) > 0
                && !_guideCompletionSent
                && RoleModel.Instance.GetLifelongCount(GuideModule, GuideSubModule, GuideType) == 0;
            if (completesFirstGuide)
            {
                _guideCompletionSent = true;
                HideGuide();
                RoleController.Instance.CompletePotionFirstUseGuide();
            }

            if (!AttributePotionController.Instance.TryRequestUse(state.Potion.GoodsId)) return;
            _ = PlayUseEffectAsync(state.Bind.effBox);
        }

        private static async Task PlayUseEffectAsync(RectTransform host)
        {
            if (host == null) return;
            int epoch = ++_effectEpoch;
            if (_useEffectHost != null) _useEffectHost.gameObject.SetActive(false);
            _useEffect?.Dispose();
            _useEffect = null;
            _useEffectHost = host;
            host.gameObject.SetActive(true);

            UIEffectStage.Handle handle = await UIEffectStage.AddAsync(
                "QiangHua", host, new Vector2(-1f, -1f), Vector3.one * 0.5f);
            if (epoch != _effectEpoch || _moduleRoot == null || !_moduleRoot.activeInHierarchy)
            {
                handle?.Dispose();
                return;
            }
            _useEffect = handle;
            int durationMs = handle != null
                ? Mathf.CeilToInt(Mathf.Max(0.8f, handle.LongestLegacyAnimationSeconds + 0.1f) * 1000f)
                : 1000;
            await TimeUtil.Delay(durationMs);
            if (epoch == _effectEpoch) ClearUseEffect();
        }

        private static void ClearUseEffect()
        {
            _effectEpoch++;
            _useEffect?.Dispose();
            _useEffect = null;
            if (_useEffectHost != null) _useEffectHost.gameObject.SetActive(false);
            _useEffectHost = null;
        }

        private static void RefreshGuide()
        {
            if (_moduleRoot == null || !_moduleRoot.activeInHierarchy
                || _guideCompletionSent
                || RoleModel.Instance.GetLifelongCount(GuideModule, GuideSubModule, GuideType) > 0
                || AttributePotionConfigs.Guide == null
                || Items.Count == 0 || Items[0].Potion == null)
            {
                HideGuide();
                return;
            }

            RectTransform target = Items[0].Bind._btn_use;
            if (target == null || _guideTarget == target) return;
            _guideTarget = target;
            AttributePotionConfigs.FirstGuide guide = AttributePotionConfigs.Guide;
            MainUIGuideManager.Instance.ShowMainUiFinger(GuideOwner, target, new ArrowData
            {
                Content = guide.Text,
                Direction = guide.Direction,
                SelectEffectScale = new Vector3(
                    guide.EffectScaleX, guide.EffectScaleY, guide.EffectScaleZ),
                Target = target,
            });
        }

        private static void HideGuide()
        {
            MainUIGuideManager.Instance.HideMainUiFinger(GuideOwner);
            _guideTarget = null;
        }

        private static void Subscribe()
        {
            if (_subscribed) return;
            AttributePotionModel.Instance.Changed += OnAuthoritativeDataChanged;
            EventDispatcher.On(GlobalEvent.EVT_BAG_UPDATE, OnVisualStateChanged);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnVisualStateChanged);
            EventDispatcher.On<int, int>(GlobalEvent.EVT_ROLE_LIFELONG_COUNT_UPDATE, OnLifelongChanged);
            _subscribed = true;
        }

        private static void Unsubscribe()
        {
            if (!_subscribed) return;
            AttributePotionModel.Instance.Changed -= OnAuthoritativeDataChanged;
            EventDispatcher.Off(GlobalEvent.EVT_BAG_UPDATE, OnVisualStateChanged);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnVisualStateChanged);
            EventDispatcher.Off<int, int>(GlobalEvent.EVT_ROLE_LIFELONG_COUNT_UPDATE, OnLifelongChanged);
            _subscribed = false;
        }

        private static void OnAuthoritativeDataChanged()
        {
            if (_moduleRoot == null || !_moduleRoot.activeInHierarchy) return;
            byte firstUsable = FindFirstUsableTier();
            if (TierHasUsable(firstUsable)) _selectedTier = firstUsable;
            RenderTabs();
            RenderItems();
        }

        private static void OnVisualStateChanged()
        {
            if (_moduleRoot == null || !_moduleRoot.activeInHierarchy) return;
            RenderTabs();
            RenderItems();
        }

        private static void OnLifelongChanged(int module, int subModule)
        {
            if (module != GuideModule || subModule != GuideSubModule) return;
            if (RoleModel.Instance.GetLifelongCount(GuideModule, GuideSubModule, GuideType) > 0)
                _guideCompletionSent = true;
            RefreshGuide();
        }

        private static void BindClose(Component target)
        {
            if (target == null) return;
            Image clickImage = target.GetComponent<Image>()
                ?? target.GetComponentInChildren<Image>(true);
            if (clickImage == null) return;
            clickImage.raycastTarget = true;
            UIUtil.AddClick(clickImage, CloseWithSound);
        }

        private static void CloseWithSound()
        {
            _ = AudioManager.PlayUi("openorclosebutton");
            Close();
        }

        private static void ReleaseModule()
        {
            HideGuide();
            ClearUseEffect();
            Unsubscribe();
            for (int i = 0; i < AwardRoots.Count; i++)
                if (AwardRoots[i] != null) ResManager.ReleaseInstance(AwardRoots[i]);
            AwardRoots.Clear();
            Tabs.Clear();
            Items.Clear();
            if (_selectedTabSprite != null) ResManager.Release(_selectedTabSprite);
            _selectedTabSprite = null;
            _unselectedTabSprite = null;
            if (_moduleRoot != null) ResManager.ReleaseInstance(_moduleRoot);
            _moduleRoot = null;
            _view = null;
            _mask = null;
            _itemScroll = null;
            _tabScroll = null;
        }

        internal static void Reset()
        {
            Close();
            ReleaseModule();
            _loading = false;
            _guideCompletionSent = false;
        }
    }
}
