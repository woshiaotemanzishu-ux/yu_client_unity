using System;
using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Generated.UI.Common;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Common
{
    /// <summary>一个标签页的配置:是否开放 + 内容工厂(把内容视图挂到内容区并返回其 BaseView)+ 可选 选中/未选中 sprite。</summary>
    public sealed class TabSpec
    {
        public bool Enabled = true;
        /// <summary>首次选中该页时调用:把内容视图 reparent / 实例化到给定内容区(_gp_item_con)下,返回其 BaseView(可空=空页)。结果会被缓存。</summary>
        public Func<RectTransform, BaseView> ContentFactory;
        public string Label;
        public string TitleImagePath;
        public string BackgroundImagePath;
        public string UpImagePath;
        public string DownImagePath;
        public Sprite UpSprite;
        public Sprite DownSprite;
    }

    /// <summary>
    /// 六标签窗框架视图(对标老端 BaseWindowComponent + 共享皮肤 BaseWindowSkin)。**地基**:各分页大窗
    /// (DailyView/装备/合成/九霄劫魄/宠物/角色…)= 实例化 BaseWindowSkin 预制体 + <see cref="Configure"/> 各自标签,**不再各写窗框**。
    ///
    /// 数据驱动:Configure(specs, defaultIndex) → 克隆 _tpl_TabButtonTwoSkin 进 _list_tab_con(标签条)→ 点击切换
    /// _gp_item_con(内容区)里的内容视图(**懒加载 + 缓存**,照抄老端 ChangeView)。关闭走 _img_return;说明 _img_instruction 降级日志。
    ///
    /// 我管「结构」(克隆标签/切页/懒加载/关闭);标签 sprite、标签条 layout 等「效果」由美术/用户在预制体上配。
    /// </summary>
    public sealed class BaseWindowSkinView : BaseWindowSkinBind
    {
        private readonly List<TabButtonTwoSkin> _tabs = new List<TabButtonTwoSkin>();
        private readonly List<int> _tabIndices = new List<int>();
        private readonly Dictionary<int, BaseView> _contentCache = new Dictionary<int, BaseView>();
        private IList<TabSpec> _specs;
        private int _current = -1;
        private bool _windowOpenRaised;

        protected override void OnInit()
        {
            if (_tpl_TabButtonTwoSkin != null) _tpl_TabButtonTwoSkin.SetActive(false);
            if (_img_return0 != null)
            {
                _img_return0.raycastTarget = true;
                UIUtil.AddClick(_img_return0, Hide);
            }
            if (_img_instruction != null)
            {
                _img_instruction.raycastTarget = true;
                UIUtil.AddClick(_img_instruction, () => GameLog.Info("Window", "点击[说明] → 待对接 InstructionType"));
            }
            if (_img_red != null) _img_red.gameObject.SetActive(false);
        }

        /// <summary>配置标签页 + 默认页索引。在窗口 Show() 之后调用(此时 OnInit 已跑)。</summary>
        protected override void OnShow(object args)
        {
            if (_windowOpenRaised) return;
            _windowOpenRaised = true;
            EventDispatcher.Emit(GlobalEvent.EVT_BASE_WINDOW_OPENED);
        }

        protected override void OnHide()
        {
            RaiseWindowClosed();
        }

        private void OnDestroy()
        {
            RaiseWindowClosed();
        }

        private void RaiseWindowClosed()
        {
            if (!_windowOpenRaised) return;
            _windowOpenRaised = false;
            EventDispatcher.Emit(GlobalEvent.EVT_BASE_WINDOW_CLOSED);
        }

        public void Configure(IList<TabSpec> specs, int defaultIndex = 0)
        {
            _specs = specs;
            BuildTabs();
            SelectTab(ClampDefault(defaultIndex));
        }

        // ---- 共享内容模式(多标签复用同一内容视图,如商城 11 标签共用 ShopCommonView 仅切 shop_type)----
        // 支持 overrides:个别标签用专属视图(如商城「抢购」= ShopVieView),其余共用 sharedContent。
        private Func<RectTransform, BaseView> _sharedFactory;
        private Action<int> _onSharedTab;
        private Func<int, bool> _sharedEnabled;
        private BaseView _sharedContent;
        private int _sharedCount;
        private string[] _sharedLabels;
        private string[] _sharedTitleImages;
        private string _sharedBackground;
        private Dictionary<int, Func<RectTransform, BaseView>> _overrideFactory;
        private readonly Dictionary<int, BaseView> _overrideCache = new Dictionary<int, BaseView>();

        /// <summary>共享内容模式:tabCount 个标签共用一个内容视图(contentFactory 懒建一次入 _gp_item_con),点标签调 onTabSelected(index) 重喂数据。
        /// overrides:个别标签用专属视图(index→工厂),命中时改显该专属视图(不调 onTabSelected)。可选 isEnabled 判定标签可点。
        /// labels:标签文字(对标老端 tabStrList);titleImages:每页标题图(对标 titleList);backgroundImage:窗底大图(对标 bg_list,null=默认 ui_bg_1)。</summary>
        public void ConfigureShared(int tabCount, Func<RectTransform, BaseView> contentFactory, Action<int> onTabSelected, int defaultIndex = 0, Func<int, bool> isEnabled = null, Dictionary<int, Func<RectTransform, BaseView>> overrides = null,
            string[] labels = null, string[] titleImages = null, string backgroundImage = null)
        {
            _sharedCount = tabCount;
            _sharedFactory = contentFactory;
            _onSharedTab = onTabSelected;
            _sharedEnabled = isEnabled;
            _overrideFactory = overrides;
            _sharedLabels = labels;
            _sharedTitleImages = titleImages;
            _sharedBackground = backgroundImage;
            BuildSharedTabs();
            int def = defaultIndex;
            if (isEnabled != null && (def < 0 || def >= tabCount || !isEnabled(def)))
            {
                def = -1;
                for (int i = 0; i < tabCount; i++) if (isEnabled(i)) { def = i; break; }
            }
            SelectShared(def);
        }

        private void BuildSharedTabs()
        {
            foreach (TabButtonTwoSkin t in _tabs) if (t != null) Destroy(t.gameObject);
            _tabs.Clear();
            _tabIndices.Clear();
            if (_tpl_TabButtonTwoSkin == null) return;
            RectTransform parent = TabParent();
            if (parent == null) return;
            for (int i = 0; i < _sharedCount; i++)
            {
                if (_sharedEnabled != null && !_sharedEnabled(i)) continue;
                GameObject go = Instantiate(_tpl_TabButtonTwoSkin, parent);
                go.SetActive(true);
                TabButtonTwoSkin tab = go.GetComponent<TabButtonTwoSkin>();
                if (tab == null) { Destroy(go); continue; }
                tab.SetData(i, SelectShared, _sharedLabels != null && i < _sharedLabels.Length ? _sharedLabels[i] : null);
                LayoutTab(go, _tabs.Count);
                _tabs.Add(tab);
                _tabIndices.Add(i);
            }
        }

        public void SelectShared(int index)
        {
            if (index < 0 || index >= _sharedCount) return;
            if (_sharedEnabled != null && !_sharedEnabled(index)) { GameLog.Info("Window", "标签[{0}]未开放", index); return; }

            bool isOverride = _overrideFactory != null && _overrideFactory.ContainsKey(index);
            if (isOverride)
            {
                if (!_overrideCache.TryGetValue(index, out BaseView ov))
                {
                    ov = _overrideFactory[index] != null ? _overrideFactory[index].Invoke(_gp_item_con) : null;
                    _overrideCache[index] = ov;
                }
                if (_sharedContent != null) _sharedContent.Hide();
                foreach (KeyValuePair<int, BaseView> kv in _overrideCache)
                    if (kv.Value != null) { if (kv.Key == index) kv.Value.Show(); else kv.Value.Hide(); }
            }
            else
            {
                foreach (BaseView ov in _overrideCache.Values) if (ov != null) ov.Hide();
                if (_sharedContent == null && _sharedFactory != null) _sharedContent = _sharedFactory.Invoke(_gp_item_con);
                if (_sharedContent != null) _sharedContent.Show();
                _onSharedTab?.Invoke(index);
            }

            ApplyTitle(_sharedTitleImages != null && index < _sharedTitleImages.Length ? _sharedTitleImages[index] : null);
            ApplyBackground(_sharedBackground);
            for (int i = 0; i < _tabs.Count; i++) if (_tabs[i] != null) _tabs[i].SetSelected(_tabIndices[i] == index);
            _current = index;
        }

        /// <summary>设置某标签红点(由各窗的红点逻辑调用)。</summary>
        public void SetTabRed(int index, bool on)
        {
            for (int i = 0; i < _tabIndices.Count; i++)
            {
                if (_tabIndices[i] == index && i < _tabs.Count && _tabs[i] != null)
                {
                    _tabs[i].SetRed(on);
                    return;
                }
            }
        }

        public int CurrentIndex => _current;

        private int ClampDefault(int idx)
        {
            if (_specs == null || _specs.Count == 0) return -1;
            if (idx >= 0 && idx < _specs.Count && _specs[idx].Enabled) return idx;
            for (int i = 0; i < _specs.Count; i++) if (_specs[i].Enabled) return i;
            return -1;
        }

        private RectTransform TabParent()
        {
            if (_list_tab_con == null) return null;
            return _list_tab_con.content != null ? _list_tab_con.content : (RectTransform)_list_tab_con.transform;
        }

        private void BuildTabs()
        {
            foreach (TabButtonTwoSkin t in _tabs) if (t != null) Destroy(t.gameObject);
            _tabs.Clear();
            _tabIndices.Clear();

            if (_specs == null || _tpl_TabButtonTwoSkin == null) return;
            RectTransform parent = TabParent();
            if (parent == null) return;

            for (int i = 0; i < _specs.Count; i++)
            {
                if (!_specs[i].Enabled) continue;
                GameObject go = Instantiate(_tpl_TabButtonTwoSkin, parent);
                go.SetActive(true);
                TabButtonTwoSkin tab = go.GetComponent<TabButtonTwoSkin>();
                if (tab == null) { Destroy(go); continue; }
                tab.SetData(
                    i,
                    SelectTab,
                    _specs[i].Label,
                    _specs[i].UpImagePath,
                    _specs[i].DownImagePath,
                    _specs[i].UpSprite,
                    _specs[i].DownSprite);
                LayoutTab(go, _tabs.Count);
                _tabs.Add(tab);
                _tabIndices.Add(i);
            }
        }

        public void SelectTab(int index)
        {
            if (_specs == null || index < 0 || index >= _specs.Count) return;
            if (!_specs[index].Enabled)
            {
                GameLog.Info("Window", "标签[{0}]未开放(内容未移植/未达条件)", index);
                return;
            }

            if (!_contentCache.TryGetValue(index, out BaseView content))
            {
                content = _specs[index].ContentFactory != null ? _specs[index].ContentFactory.Invoke(_gp_item_con) : null;
                _contentCache[index] = content;
            }
            ApplyTitle(_specs[index].TitleImagePath);
            ApplyBackground(_specs[index].BackgroundImagePath);

            foreach (KeyValuePair<int, BaseView> kv in _contentCache)
            {
                if (kv.Value == null) continue;
                if (kv.Key == index) kv.Value.Show(); else kv.Value.Hide();
            }

            for (int i = 0; i < _tabs.Count; i++) if (_tabs[i] != null) _tabs[i].SetSelected(_tabIndices[i] == index);
            _current = index;
        }

        private static void LayoutTab(GameObject go, int visualIndex)
        {
            RectTransform rt = go != null ? go.transform as RectTransform : null;
            if (rt == null) return;

            const float width = 150f;
            const float height = 90f;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(width * visualIndex, 0f);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

            RectTransform parent = rt.parent as RectTransform;
            if (parent == null) return;

            float contentWidth = width * (visualIndex + 1);
            if (parent.rect.width < contentWidth)
                parent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentWidth);
            if (parent.rect.height < height)
                parent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        private void ApplyTitle(string titleImagePath)
        {
            if (_img_title == null || string.IsNullOrEmpty(titleImagePath)) return;
            _ = ResManager.SetImageAsync(_img_title, titleImagePath, nativeSize: false);
        }

        private void ApplyBackground(string backgroundImagePath)
        {
            if (_img_bg == null) return;

            string path = string.IsNullOrEmpty(backgroundImagePath)
                ? GameResPath.GetBigBgPath("ui_bg_1.jpg")
                : backgroundImagePath;
            _ = ResManager.SetImageAsync(_img_bg, path, nativeSize: false);
        }
    }
}
