using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Pet;
using Shenxiao.Module.Core.Skill;
using Shenxiao.Module.Core.Tasks;
using UnityEngine;

namespace Shenxiao.Module.Core.Role
{
    /// <summary>
    /// 人物模块窗口编排。一级页签严格对标老端 RoleView：
    /// 人物 / 垂神翼影 / 古法符相 / 殒锋天刃 / 玄穹云披。
    /// 技能、时装、称号等属于人物页二级入口，不进入底部一级页签。
    /// </summary>
    public static class RoleFlow
    {
        private const string ContentModule = "role";
        private const string ContentPrefab = "RoleModule";
        private const string FrameModule = "common";
        private const string FramePrefab = "BaseWindowSkin";
        private const string OutwardModule = "pet";
        private const string OutwardPrefab = "PetModule";
        private const int DefaultTab = 0;

        private static readonly string[] TabContent =
        {
            "EquipmentView",
            "WingsComponentView",
            "ArtifactComponentView",
            "HolyDeviceComponentView",
            "BackOrnamentComponentView",
        };

        private static readonly int[] TabOutwardTypeId = { 0, 3, 4, 5, 12 };

        private static readonly string[] TabTitles =
        {
            GameResPath.GetIcon("role", "title_name"),
            GameResPath.GetIcon("pet", "ui_yuyi"),
            GameResPath.GetIcon("pet", "ui_yushou"),
            GameResPath.GetIcon("pet", "ui_shenbin"),
            GameResPath.GetIcon("pet", "ui_beishi"),
        };

        private static readonly string[] TabBackgrounds =
        {
            GameResPath.GetBigBgPath("ui_role_new_bg_1.jpg"),
            GameResPath.GetBigBgPath("uiwg_008a.jpg"),
            GameResPath.GetBigBgPath("uiwg_008a.jpg"),
            GameResPath.GetBigBgPath("uiwg_008a.jpg"),
            GameResPath.GetBigBgPath("uiwg_008a.jpg"),
        };

        private static readonly string[] TabLabels =
        {
            "人物",
            "垂神翼影",
            "古法符相",
            "殒锋天刃",
            "玄穹云披",
        };

        private static readonly string[] TabFuncOpenViews =
        {
            null,
            "WingsComponentView",
            "ArtifactComponentView",
            "HolyDeviceComponentView",
            "BackOrnamentComponentView",
        };

        private static GameObject _frameRoot;
        private static GameObject _contentRoot;
        private static BaseWindowSkinView _window;
        private static GameObject _skillFrameRoot;
        private static BaseWindowSkinView _skillWindow;
        private static bool _loading;
        private static bool _skillLoading;
        private static int _requestedTab = -1;
        private static int _skillReturnTab;
        private static BaseView _activeSubView;
        private static BaseView _returnView;

        private static readonly Dictionary<int, GameObject> OutwardRoots =
            new Dictionary<int, GameObject>();
        private static readonly Dictionary<int, OutWardBaseView> OutwardViews =
            new Dictionary<int, OutWardBaseView>();

        public static void Toggle()
        {
            if ((_window != null && _window.IsShown) || (_skillWindow != null && _skillWindow.IsShown))
            {
                Close();
                return;
            }
            _ = OpenAsync();
        }

        public static void Open() => _ = OpenAsync();

        public static void SelectTab(int index)
        {
            if (index < 0 || index >= TabContent.Length) return;
            _requestedTab = index;
            if (_window == null || _loading)
            {
                _ = OpenAsync();
                return;
            }

            _window.Show();
            _window.SelectTab(index);
            _requestedTab = -1;
        }

        public static void Close()
        {
            HideSubView(false);
            if (_skillWindow != null) _skillWindow.Hide();
            if (_window != null) _window.Hide();
        }

        /// <summary>打开老端 SkillSubView 等价页：独立公共窗口壳 + 主动/被动/天赋三页签。</summary>
        public static void OpenSkill() => _ = OpenSkillAsync();

        private static void HandleReturn()
        {
            if (HideSubView(true)) return;
            if (_window != null) _window.Hide();
        }

        private static void ReturnFromSkill()
        {
            if (_skillWindow != null) _skillWindow.Hide();
            if (_window == null) return;

            _window.SetReturnAction(HandleReturn);
            _window.Show();
            _window.Configure(BuildTabSpecs(BuildEnabledTabs()), _skillReturnTab);
        }

        private static BaseView FindShownPrimaryView()
        {
            if (_frameRoot == null) return null;
            foreach (BaseView view in _frameRoot.GetComponentsInChildren<BaseView>(true))
            {
                for (int i = 0; i < TabContent.Length; i++)
                {
                    if (view.GetType().Name == TabContent[i] && view.IsShown)
                        return view;
                }
            }
            return null;
        }

        private static void OpenSubView(BaseView target)
        {
            if (target == null) return;
            if (_activeSubView != null && _activeSubView != target)
                _activeSubView.Hide();
            if (_activeSubView != target)
            {
                _returnView = FindShownPrimaryView();
                _returnView?.Hide();
                _activeSubView = target;
            }
            target.Show();
        }

        private static bool HideSubView(bool restoreParent)
        {
            if (_activeSubView == null)
            {
                _returnView = null;
                return false;
            }

            _activeSubView.Hide();
            BaseView parent = _returnView;
            _activeSubView = null;
            _returnView = null;
            if (restoreParent) parent?.Show();
            return true;
        }

        public static void OpenSub(string viewTypeName)
        {
            if (string.IsNullOrEmpty(viewTypeName)) return;
            if (_contentRoot != null)
            {
                foreach (BaseView view in _contentRoot.GetComponentsInChildren<BaseView>(true))
                {
                    if (view.GetType().Name != viewTypeName) continue;
                    OpenSubView(view);
                    return;
                }
            }
            if (_frameRoot != null)
            {
                foreach (BaseView view in _frameRoot.GetComponentsInChildren<BaseView>(true))
                {
                    if (view.GetType().Name != viewTypeName) continue;
                    OpenSubView(view);
                    return;
                }
            }
            GameLog.Info("Role", "人物二级窗口 [{0}] 尚未接入", viewTypeName);
        }

        private static async Task OpenSkillAsync()
        {
            if (_skillLoading) return;
            _skillLoading = true;
            try
            {
                if (_contentRoot == null || _window == null)
                    await OpenAsync();
                if (_contentRoot == null || _window == null) return;

                _skillReturnTab = _window.CurrentIndex >= 0 ? _window.CurrentIndex : DefaultTab;
                if (_skillFrameRoot == null || _skillWindow == null)
                {
                    string frameKey = GameResPath.GetUIPrefab(FrameModule, FramePrefab);
                    _skillFrameRoot = await ResManager.InstantiateAsync(
                        frameKey, ViewManager.GetLayer(UILayer.Window));
                    if (_skillFrameRoot == null)
                    {
                        GameLog.Error("Role", "技能窗口公共壳加载失败: {0}", frameKey);
                        return;
                    }
                    _skillFrameRoot.name = "RoleSkillWindow";
                    _skillWindow = _skillFrameRoot.GetComponent<BaseWindowSkinView>()
                        ?? _skillFrameRoot.GetComponentInChildren<BaseWindowSkinView>(true);
                    if (_skillWindow == null)
                    {
                        GameLog.Error("Role", "技能窗口公共壳缺 BaseWindowSkinView");
                        return;
                    }
                }

                await SkillConfigs.EnsureLoaded();
                await SkillUIConfigs.EnsureLoaded();
                await SkillPassiveConfigs.EnsureLoaded();
                await TaskConfigs.EnsureLoaded();
                _skillWindow.SetReturnAction(ReturnFromSkill);
                _skillWindow.Configure(BuildSkillTabSpecs(), 0);
                HideSubView(false);
                _window.Hide();
                _skillWindow.Show();
            }
            finally
            {
                _skillLoading = false;
            }
        }

        private static List<TabSpec> BuildSkillTabSpecs()
        {
            string title = GameResPath.GetIcon("role", "title_name");
            string background = GameResPath.GetIconJpgOtherPath("role", "uijn_001");
            return new List<TabSpec>
            {
                new TabSpec
                {
                    Enabled = true,
                    Label = "主动技能",
                    TitleImagePath = title,
                    BackgroundImagePath = background,
                    ContentFactory = parent => ReparentSkillContent("SkillInitiativeSubItem", parent),
                },
                new TabSpec
                {
                    Enabled = true,
                    Label = "被动技能",
                    TitleImagePath = title,
                    BackgroundImagePath = background,
                    ContentFactory = parent => ReparentSkillContent("SkillPassiveSubItem", parent),
                },
                new TabSpec
                {
                    Enabled = true,
                    Label = "天赋",
                    TitleImagePath = title,
                    BackgroundImagePath = background,
                    OpenCheck = () => (RoleModel.Instance.Figure?.turn ?? 0) >= 4,
                    LockedToast = "角色达到4转后开启",
                    ContentFactory = parent => ReparentSkillContent("InnateSkillView", parent),
                },
            };
        }

        private static BaseView ReparentSkillContent(string viewName, RectTransform parent)
        {
            if (_contentRoot == null) return null;
            Transform node = _contentRoot.transform.Find(viewName);
            if (node == null)
            {
                GameLog.Warn("Role", "RoleModule 顶层缺技能内容页 {0}", viewName);
                return null;
            }
            node.SetParent(parent, false);
            node.gameObject.SetActive(true);
            return node.GetComponent<BaseView>();
        }

        private static async Task OpenAsync()
        {
            if (_loading) return;
            if (_frameRoot != null && _window != null)
            {
                _loading = true;
                try
                {
                    HideSubView(false);
                    _window.SetReturnAction(HandleReturn);
                    await FuncOpenConfig.EnsureLoaded();
                    bool[] enabledTabs = BuildEnabledTabs();
                    await PreloadOutwardTabsAsync(enabledTabs);
                    int reopenTab = _requestedTab >= 0
                        ? _requestedTab
                        : (_window.CurrentIndex >= 0 ? _window.CurrentIndex : DefaultTab);
                    _requestedTab = -1;
                    _window.Show();

                    // 每次重开都按最新任务进度重建可见页签，并重走当前页 OnShow。
                    // 共享 UIModelStage 即使已被别的页面清空，也会在这里重新装配人物。
                    _window.Configure(BuildTabSpecs(enabledTabs), reopenTab);
                }
                finally
                {
                    _loading = false;
                }
                return;
            }

            _loading = true;
            try
            {
                string frameKey = GameResPath.GetUIPrefab(FrameModule, FramePrefab);
                string contentKey = GameResPath.GetUIPrefab(ContentModule, ContentPrefab);
                _frameRoot = await ResManager.InstantiateAsync(
                    frameKey, ViewManager.GetLayer(UILayer.Window));
                _contentRoot = await ResManager.InstantiateAsync(
                    contentKey, ViewManager.GetLayer(UILayer.Window));
                if (_frameRoot == null || _contentRoot == null)
                {
                    GameLog.Error("Role", "人物窗口加载失败 frame={0} content={1}", frameKey, contentKey);
                    return;
                }

                _frameRoot.name = FramePrefab;
                _contentRoot.name = ContentPrefab;
                foreach (Transform child in _contentRoot.transform)
                    child.gameObject.SetActive(false);

                _window = _frameRoot.GetComponent<BaseWindowSkinView>()
                    ?? _frameRoot.GetComponentInChildren<BaseWindowSkinView>(true);
                if (_window == null)
                {
                    GameLog.Error("Role", "BaseWindowSkin 缺 BaseWindowSkinView");
                    return;
                }

                _window.SetReturnAction(HandleReturn);
                await FuncOpenConfig.EnsureLoaded();
                bool[] enabledTabs = BuildEnabledTabs();
                await PreloadOutwardTabsAsync(enabledTabs);
                List<TabSpec> specs = BuildTabSpecs(enabledTabs);

                int initialTab = _requestedTab >= 0 ? _requestedTab : DefaultTab;
                _requestedTab = -1;
                _window.Show();
                _window.Configure(specs, initialTab);
                GameLog.Info("Role", "人物窗口打开，一级页签={0}，初始页={1}",
                    specs.FindAll(x => x.Enabled).Count, initialTab);
            }
            finally
            {
                _loading = false;
            }
        }

        private static List<TabSpec> BuildTabSpecs(bool[] enabledTabs)
        {
            var specs = new List<TabSpec>(TabContent.Length);
            for (int i = 0; i < TabContent.Length; i++)
            {
                int tabIndex = i;
                string viewName = TabContent[i];
                int outwardTypeId = TabOutwardTypeId[i];
                bool enabled = enabledTabs != null && i < enabledTabs.Length && enabledTabs[i];
                Func<RectTransform, BaseView> factory = null;
                if (enabled)
                {
                    factory = outwardTypeId > 0
                        ? (Func<RectTransform, BaseView>)(parent =>
                            ReparentOutwardTab(tabIndex, parent))
                        : (parent => ReparentContent(viewName, parent));
                }

                specs.Add(new TabSpec
                {
                    Enabled = enabled,
                    Label = TabLabels[i],
                    TitleImagePath = TabTitles[i],
                    BackgroundImagePath = TabBackgrounds[i],
                    ContentFactory = factory,
                });
            }
            return specs;
        }

        private static bool[] BuildEnabledTabs()
        {
            var enabled = new bool[TabContent.Length];
            enabled[0] = true;
            for (int i = 1; i < enabled.Length; i++)
                enabled[i] = FuncOpenConfig.CheckFuncOpenState(TabFuncOpenViews[i]);
            return enabled;
        }

        private static BaseView ReparentContent(string viewName, RectTransform parent)
        {
            if (_contentRoot == null) return null;
            Transform node = _contentRoot.transform.Find(viewName);
            if (node == null)
            {
                GameLog.Warn("Role", "RoleModule 顶层缺内容页 {0}", viewName);
                return null;
            }
            node.SetParent(parent, false);
            node.gameObject.SetActive(true);
            return node.GetComponent<BaseView>();
        }

        private static async Task PreloadOutwardTabsAsync(bool[] enabledTabs)
        {
            string key = GameResPath.GetUIPrefab(OutwardModule, OutwardPrefab);
            for (int i = 1; i < TabOutwardTypeId.Length; i++)
            {
                if (enabledTabs == null || i >= enabledTabs.Length || !enabledTabs[i]) continue;
                if (OutwardViews.ContainsKey(i)) continue;

                GameObject root = await ResManager.InstantiateAsync(
                    key, ViewManager.GetLayer(UILayer.Window));
                if (root == null)
                {
                    GameLog.Warn("Role", "人物页签 {0} 加载 OutWardBaseView 失败", TabContent[i]);
                    continue;
                }
                root.name = "OutWard_" + TabContent[i];
                root.SetActive(false);
                OutWardBaseView view = root.GetComponentInChildren<OutWardBaseView>(true);
                if (view == null)
                {
                    GameLog.Warn("Role", "PetModule 缺 OutWardBaseView");
                    ResManager.ReleaseInstance(root);
                    continue;
                }

                view.SetType(TabOutwardTypeId[i]);
                OutwardRoots[i] = root;
                OutwardViews[i] = view;
            }
        }

        private static BaseView ReparentOutwardTab(int tabIndex, RectTransform parent)
        {
            if (!OutwardViews.TryGetValue(tabIndex, out OutWardBaseView view) || view == null)
            {
                GameLog.Warn("Role", "人物外观页签 {0} 尚未加载", tabIndex);
                return null;
            }
            view.transform.SetParent(parent, false);
            view.gameObject.SetActive(true);
            return view;
        }

        internal static void Reset()
        {
            HideSubView(false);
            _window?.SetReturnAction(null);
            _skillWindow?.SetReturnAction(null);
            if (_frameRoot != null) ResManager.ReleaseInstance(_frameRoot);
            if (_skillFrameRoot != null) ResManager.ReleaseInstance(_skillFrameRoot);
            if (_contentRoot != null) ResManager.ReleaseInstance(_contentRoot);
            foreach (GameObject root in OutwardRoots.Values)
                if (root != null) ResManager.ReleaseInstance(root);
            OutwardRoots.Clear();
            OutwardViews.Clear();
            _frameRoot = null;
            _contentRoot = null;
            _window = null;
            _skillFrameRoot = null;
            _skillWindow = null;
            _loading = false;
            _skillLoading = false;
            _requestedTab = -1;
            _skillReturnTab = DefaultTab;
            _activeSubView = null;
            _returnView = null;
        }
    }
}
