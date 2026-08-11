using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.MainUI;
using TMPro;
using UnityEngine;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 背包模块编排:五标签窗(对标老端 BagView extends BaseWindowComponent)。**走 BaseWindowSkinView 地基 + 多内容源**(标签内容跨模块,= 老端 viewClassModuleDic)。
    ///
    /// 老端 tabStrList=[背包/仓库/影骸战衣/启示圣铠/九天神祭];内容分散在 bag / holySeal / revelation / longlanguage 模块。
    /// 五个标签都有当前可编辑主 Prefab；实际生成项与老端 InitTabList 一致，先走 ConfigFuncOpenCondition，
    /// 未开放的特殊标签不加载 Prefab、不生成 Tab。特殊装备页只在开放且被选中时拉自己的 15010 容器。
    /// **内容源两形态**:模块组 prefab(根=容器,视图为顶层子,如 BagModule)/ 独立视图 prefab(根即视图,shared-prefab 产物,如 HolySealView.prefab)。
    /// 加载时按需去重;点标签把对应内容 reparent 进窗框内容区 _gp_item_con(懒加载缓存)。
    /// 子窗(一键使用/熔炼/扩展…)经 <see cref="ToggleSub"/>/<see cref="OpenSub"/>(背包面板按钮触发,搜模块组源)。入口 <see cref="BagBootstrap"/>(MainUIRouter "bag");再点图标 <see cref="Toggle"/> 关闭。
    /// </summary>
    public static class BagFlow
    {
        private const string FRAME_MODULE = "common";
        private const string FRAME_PREFAB = "BaseWindowSkin";
        private const string PRIMARY_MODULE = "bag";
        private const string PRIMARY_PREFAB = "BagModule";

        // 老端 BagView.viewClassList(标签索引 → 内容视图类名)
        private static readonly string[] TabContent =
        {
            "BagComponentView", "WarehouseView", "HolySealView", "RevelationEquipView", "longlanguageView"
        };
        // 各标签内容所属 module/prefab;shared-prefab 视图的 prefab 名 = 视图名(独立 prefab),模块组用 <Module>Module
        private static readonly string[] TabModule =
        {
            "bag", "bag", "holySeal", "revelation", "longlanguage"
        };
        private static readonly string[] TabPrefab =
        {
            "BagModule", "BagModule", "HolySealView", "RevelationEquipView", "longlanguageView"
        };
        private static readonly string[] TabTitles =
        {
            GameResPath.GetIcon("bag", "title_name"),
            GameResPath.GetIcon("bag", "title_name"),
            GameResPath.GetIcon("bag", "uisy_002"),
            GameResPath.GetIcon("bag", "ui_Apocalypse_title"),
            GameResPath.GetIcon("bag", "ui_ly_title")
        };
        private static readonly string[] TabBackgrounds =
        {
            GameResPath.GetBigBgPath("ui_bg_1.jpg"),
            GameResPath.GetBigBgPath("ui_bg_1.jpg"),
            GameResPath.GetBigBgPath("ui_seal_bg.jpg"),
            GameResPath.GetBigBgPath("ui_Apocalypse_bg.jpg"),
            GameResPath.GetBigBgPath("ui_lybg.jpg")
        };
        private static readonly string[] TabLabels =
        {
            "\u80CC\u5305", "\u4ED3\u5E93", "\u5F71\u9AA8\u6218\u8863", "\u542F\u793A\u5723\u94E0", "\u4E5D\u5929\u795E\u796D"
        };
        private const int DefaultTab = 0;

        private static GameObject _frameRoot;
        // 内容源根:prefab 名 → 实例(去重)
        private static readonly Dictionary<string, GameObject> _contentRoots = new Dictionary<string, GameObject>();
        private static readonly HashSet<string> _moduleLoading = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static BaseWindowSkinView _window;
        private static TextMeshProUGUI _titleOverlay;
        private static bool[] _tabEnabledSnapshot;
        private static bool _windowCloseHooked;
        private static bool _loading;

        /// <summary>
        /// 对标老端 BaseWindowComponent.InitTabList：背包/仓库无开放表条目，始终生成；
        /// 影骸战衣、启示圣铠、九天神祭按各自 ViewName 的开服天/等级/任务条件过滤。
        /// </summary>
        private static bool IsTabEnabled(int index)
        {
            if (index < 0 || index >= TabContent.Length) return false;
            return index <= 1 || FuncOpenConfig.CheckFuncOpenState(TabContent[index]);
        }

        public static void Toggle()
        {
            if (_window != null && _window.IsShown) { Close(); return; }
            _ = OpenAsync();
        }

        public static void Open() => _ = OpenAsync();

        public static void Close()
        {
            HideContentViews();
            if (_window != null) _window.Hide();
        }

        /// <summary>
        /// 老端 BagView 会在影骸战衣、启示圣铠和九天神祭页用完整文字覆盖旧标题位图；
        /// 旧位图仍分别写着“影装”“天启”等简称，不能作为当前窗口身份。
        /// 内容页每次 Show 都同步一次，保证缓存页切换及 warm 重开不会残留上一页标题。
        /// </summary>
        internal static void ApplyWindowTitlePresentation(int tabIndex)
        {
            if (_window == null || _window._img_title == null) return;

            string titleText = tabIndex >= 2 && tabIndex < TabLabels.Length
                ? TabLabels[tabIndex]
                : null;
            if (string.IsNullOrEmpty(titleText))
            {
                if (_titleOverlay != null) _titleOverlay.gameObject.SetActive(false);
                _window._img_title.gameObject.SetActive(true);
                return;
            }

            RectTransform imageRect = _window._img_title.rectTransform;
            if (_titleOverlay != null && _titleOverlay.transform.parent != imageRect.parent)
            {
                UnityEngine.Object.Destroy(_titleOverlay.gameObject);
                _titleOverlay = null;
            }

            if (_titleOverlay == null)
            {
                var go = new GameObject("_bag_module_title_overlay", typeof(RectTransform));
                var rect = (RectTransform)go.transform;
                rect.SetParent(imageRect.parent, false);
                rect.anchorMin = imageRect.anchorMin;
                rect.anchorMax = imageRect.anchorMax;
                rect.pivot = imageRect.pivot;
                rect.anchoredPosition = imageRect.anchoredPosition;
                rect.sizeDelta = new Vector2(Mathf.Max(300f, imageRect.rect.width), Mathf.Max(44f, imageRect.rect.height));

                _titleOverlay = go.AddComponent<TextMeshProUGUI>();
                _titleOverlay.alignment = TextAlignmentOptions.Center;
                _titleOverlay.fontSize = 34f;
                _titleOverlay.fontStyle = FontStyles.Bold;
                _titleOverlay.color = new Color(1f, 0.972f, 0.906f, 1f);
                _titleOverlay.raycastTarget = false;
                foreach (TextMeshProUGUI text in _window.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if (text == _titleOverlay) continue;
                    _titleOverlay.font = text.font;
                    _titleOverlay.fontSharedMaterial = text.fontSharedMaterial;
                    break;
                }
            }

            _titleOverlay.text = titleText;
            _titleOverlay.gameObject.SetActive(true);
            _titleOverlay.transform.SetAsLastSibling();
            _window._img_title.gameObject.SetActive(false);
            if (_window._img_instruction != null && _window._img_instruction.gameObject.activeSelf)
                _window._img_instruction.transform.SetAsLastSibling();
        }

        /// <summary>切换背包模块内子窗(一键使用/熔炼/扩展…),已显则关、未显则开;在所有已加载内容源里按 View 子类名查找。</summary>
        public static void ToggleSub(string viewTypeName)
        {
            BaseView v = FindSub(viewTypeName);
            if (v == null) { GameLog.Info("Bag", "背包子窗 [{0}] 未移植 View,待对接", viewTypeName); return; }
            if (v.IsShown) v.Hide();
            else
            {
                RaiseSubViewRoot(v);
                v.Show();
            }
        }

        /// <summary>打开背包模块内子窗;在所有已加载内容源里按 View 子类名查找。未移植 → 日志降级。</summary>
        public static void OpenSub(string viewTypeName)
        {
            OpenSub(viewTypeName, null);
        }

        /// <summary>打开子窗并透传参数；扩容窗用它区分背包(pos=4)与仓库(pos=5)。</summary>
        public static void OpenSub(string viewTypeName, object args)
        {
            BaseView v = FindSub(viewTypeName);
            if (v == null) { GameLog.Info("Bag", "背包子窗 [{0}] 未移植 View,待对接", viewTypeName); return; }
            RaiseSubViewRoot(v);
            v.Show(args);
        }

        /// <summary>
        /// 打开特殊背包页所属的现有模块子窗。HolySealView/longlanguageView 是独立内容 Prefab，
        /// 它们的强化、预览、分解等子窗仍保存在各自 Module Prefab，不能把“按钮有点击日志”当成已接线。
        /// 本入口只做懒加载和真实 BaseView.Show，不复制视觉树；同一模块实例在 Bag 会话内复用。
        /// </summary>
        public static void OpenModuleSub(string module, string prefabName, string viewTypeName, object args = null)
        {
            _ = OpenModuleSubAsync(module, prefabName, viewTypeName, args);
        }

        private static async Task OpenModuleSubAsync(string module, string prefabName, string viewTypeName, object args)
        {
            if (string.IsNullOrEmpty(module) || string.IsNullOrEmpty(prefabName) || string.IsNullOrEmpty(viewTypeName))
                return;

            BaseView existing = FindSub(viewTypeName);
            if (existing != null)
            {
                RaiseSubViewRoot(existing);
                existing.Show(args);
                return;
            }

            string rootKey = module + "/" + prefabName;
            if (_moduleLoading.Contains(rootKey)) return;
            _moduleLoading.Add(rootKey);
            try
            {
                if (!_contentRoots.TryGetValue(rootKey, out GameObject root) || root == null)
                {
                    string address = GameResPath.GetUIPrefab(module, prefabName);
                    root = await MainUIRouteFallback.InstantiateOrShowAsync(
                        PRIMARY_MODULE, "Bag", address, ViewManager.GetLayer(UILayer.Window));
                    if (root == null)
                    {
                        GameLog.Error("Bag", "特殊装备子模块加载失败 module={0} prefab={1} view={2}", module, prefabName, viewTypeName);
                        return;
                    }
                    root.name = prefabName;
                    if (root.GetComponent<BaseView>() != null) root.SetActive(false);
                    else foreach (Transform child in root.transform) child.gameObject.SetActive(false);
                    _contentRoots[rootKey] = root;
                }

                BaseView view = FindSub(viewTypeName);
                if (view == null)
                {
                    GameLog.Error("Bag", "特殊装备子窗不存在 module={0} prefab={1} view={2}", module, prefabName, viewTypeName);
                    return;
                }
                RaiseSubViewRoot(view);
                view.Show(args);
            }
            catch (Exception e)
            {
                GameLog.Error("Bag", "特殊装备子窗打开异常 module={0} prefab={1} view={2} error={3}",
                    module, prefabName, viewTypeName, e.Message);
            }
            finally
            {
                _moduleLoading.Remove(rootKey);
            }
        }

        /// <summary>
        /// 老端 BagSmelt/OneKeyUse/ExpandBag 均在 Activity 层，必须整体高于背包 Window。
        /// 这些子窗仍属于 BagModule prefab；提升承载它们的模块根而不是把子节点拆出 prefab，
        /// 既保留序列化层级，也保证 BagFlow.Reset 能随模块根完整释放。
        /// </summary>
        private static void RaiseSubViewRoot(BaseView view)
        {
            if (view == null) return;
            Transform layer = ViewManager.GetLayer(view.Layer);
            if (layer == null) return;

            foreach (GameObject root in _contentRoots.Values)
            {
                if (root == null || (view.transform != root.transform && !view.transform.IsChildOf(root.transform)))
                    continue;
                if (root.transform.parent != layer) root.transform.SetParent(layer, false);
                root.transform.SetAsLastSibling();
                root.GetComponent<BagActivityModalLayout>()?.Show(view, view.Hide);
                return;
            }

            GameLog.Warn("Bag", "背包子窗 [{0}] 未找到所属模块根，无法提升到 {1} 层", view.GetType().Name, view.Layer);
        }

        internal static void NotifyActivitySubHidden(BaseView view)
        {
            if (view == null) return;
            foreach (GameObject root in _contentRoots.Values)
            {
                if (root == null || (view.transform != root.transform && !view.transform.IsChildOf(root.transform)))
                    continue;
                BagActivityModalLayout layout = root.GetComponent<BagActivityModalLayout>();
                if (layout == null) return;

                // SmeltPropView 叠在 BagSmeltView 上时，关闭上层后底层 Activity 仍保持打开。
                // 共享遮罩必须重新绑定到底层窗口，不能直接隐藏后让点击穿透到背包 Window。
                BaseView fallback = null;
                int sibling = int.MinValue;
                foreach (BaseView candidate in root.GetComponentsInChildren<BaseView>(true))
                {
                    if (candidate == null || candidate == view || !candidate.IsShown || candidate.Layer != UILayer.Popup)
                        continue;
                    int index = candidate.transform.GetSiblingIndex();
                    if (fallback == null || index > sibling)
                    {
                        fallback = candidate;
                        sibling = index;
                    }
                }
                if (fallback != null) layout.Show(fallback, fallback.Hide);
                else layout.Hide();
                return;
            }
        }

        private static BaseView FindSub(string viewTypeName)
        {
            if (_contentRoots.Count == 0)
            {
                GameLog.Warn("Bag", "OpenSub/ToggleSub({0}) 时背包模块未打开", viewTypeName);
                return null;
            }
            foreach (GameObject root in _contentRoots.Values)
            {
                if (root == null) continue;
                foreach (BaseView v in root.GetComponentsInChildren<BaseView>(true))
                {
                    string typeName = v.GetType().Name;
                    if (typeName == viewTypeName || typeName == viewTypeName + "Bind" || v.gameObject.name == viewTypeName)
                        return v;
                }
            }
            return null;
        }

        private static async Task OpenAsync()
        {
            if (_loading) return;
            if (_frameRoot != null)
            {
                bool[] current = SnapshotTabEnabled();
                if (!MatchesTabSnapshot(current))
                {
                    Reset();
                }
                else
                {
                    if (_window != null)
                    {
                        _window.Show();
                        _window.SelectTab(DefaultTab);
                    }
                    return;
                }
            }

            _loading = true;

            string frameKey = GameResPath.GetUIPrefab(FRAME_MODULE, FRAME_PREFAB);
            var tabEnabled = new bool[TabContent.Length];
            try
            {
            await FuncOpenConfig.EnsureLoaded();
            if (!FuncOpenConfig.IsLoaded)
                GameLog.Error("Bag", "ConfigFuncOpenCondition 未加载，特殊背包标签按公共缺表语义暂视为开放");
            for (int i = 0; i < tabEnabled.Length; i++) tabEnabled[i] = IsTabEnabled(i);

            _frameRoot = await MainUIRouteFallback.InstantiateOrShowAsync(PRIMARY_MODULE, "Bag", frameKey, ViewManager.GetLayer(UILayer.Window));

            var needPrefab = new Dictionary<string, string> { { PRIMARY_PREFAB, PRIMARY_MODULE } };
            for (int i = 0; i < TabContent.Length; i++)
            {
                if (tabEnabled[i] && !needPrefab.ContainsKey(TabPrefab[i])) needPrefab[TabPrefab[i]] = TabModule[i];
            }
            foreach (KeyValuePair<string, string> kv in needPrefab)
            {
                string key = GameResPath.GetUIPrefab(kv.Value, kv.Key);
                GameObject root = await MainUIRouteFallback.InstantiateOrShowAsync(PRIMARY_MODULE, "Bag", key, ViewManager.GetLayer(UILayer.Window));
                if (root == null) continue;
                root.name = kv.Key;
                // 两形态:根即视图(shared-prefab) → 隐根;模块组 → 隐各顶层子(保根 active 供 OpenSub)
                if (root.GetComponent<BaseView>() != null)
                {
                    root.SetActive(false);
                }
                else
                {
                    foreach (Transform c in root.transform) c.gameObject.SetActive(false);
                }
                _contentRoots[kv.Key] = root;
            }
            }
            catch (Exception e)
            {
                GameLog.Error("Bag", "Bag window load exception frame={0} error={1}", frameKey, e.Message);
                ShowPlaceholderAndReset();
                return;
            }
            finally
            {
                _loading = false;
            }

            if (_frameRoot == null || !_contentRoots.ContainsKey(PRIMARY_PREFAB))
            {
                GameLog.Error("Bag", "背包五标签窗加载失败(frame 或主内容源缺失)");
                ShowPlaceholderAndReset();
                return;
            }
            _frameRoot.name = FRAME_PREFAB;

            _window = _frameRoot.GetComponent<BaseWindowSkinView>();
            if (_window == null) _window = _frameRoot.GetComponentInChildren<BaseWindowSkinView>(true);
            if (_window == null)
            {
                GameLog.Warn("Bag", "BaseWindowSkin 缺 BaseWindowSkinView(重跑 common 流水线回填)");
                ShowPlaceholderAndReset();
                return;
            }

            var specs = new List<TabSpec>(TabContent.Length);
            for (int i = 0; i < TabContent.Length; i++)
            {
                string viewName = TabContent[i];
                string prefabName = TabPrefab[i];
                bool enabled = tabEnabled[i];
                specs.Add(new TabSpec
                {
                    Enabled = enabled,
                    Label = TabLabels[i],
                    TitleImagePath = TabTitles[i],
                    BackgroundImagePath = TabBackgrounds[i],
                    ContentFactory = enabled ? (Func<RectTransform, BaseView>)(parent => ReparentFrom(prefabName, viewName, parent)) : null,
                });
            }

            _window.Show();
            _window.Configure(specs, DefaultTab);
            _window.SetReturnAction(Close);
            _tabEnabledSnapshot = (bool[])tabEnabled.Clone();
            HookWindowClose();
            GameLog.Info("Bag", "背包五标签窗打开(BaseWindowSkinView,默认 tab{0} 背包)", DefaultTab);
        }

        private static bool[] SnapshotTabEnabled()
        {
            var result = new bool[TabContent.Length];
            for (int i = 0; i < result.Length; i++) result[i] = IsTabEnabled(i);
            return result;
        }

        private static bool MatchesTabSnapshot(bool[] current)
        {
            if (_tabEnabledSnapshot == null || current == null || _tabEnabledSnapshot.Length != current.Length)
                return false;
            for (int i = 0; i < current.Length; i++)
                if (_tabEnabledSnapshot[i] != current[i]) return false;
            return true;
        }

        private static void HookWindowClose()
        {
            if (_windowCloseHooked) return;
            EventDispatcher.On(GlobalEvent.EVT_BASE_WINDOW_CLOSED, OnAnyBaseWindowClosed);
            _windowCloseHooked = true;
        }

        private static void OnAnyBaseWindowClosed()
        {
            if (_window != null && !_window.IsShown) HideContentViews();
        }

        /// <summary>
        /// BaseWindowSkin 只控制窗框根显隐；Bag 的内容页与 Activity 子窗必须显式 Hide，
        /// 才能触发滚动停惯性、3D 清理和子窗遮罩回收。关闭后 warm 重开再从默认背包页 Show。
        /// </summary>
        private static void HideContentViews()
        {
            var views = new List<BaseView>();
            if (_window != null)
            {
                foreach (BaseView view in _window.GetComponentsInChildren<BaseView>(true))
                    if (view != null && view != _window && !views.Contains(view)) views.Add(view);
            }
            foreach (GameObject root in _contentRoots.Values)
            {
                if (root == null) continue;
                foreach (BaseView view in root.GetComponentsInChildren<BaseView>(true))
                    if (view != null && view != _window && !views.Contains(view)) views.Add(view);
            }
            for (int i = views.Count - 1; i >= 0; i--)
                if (views[i] != null && views[i].IsShown) views[i].Hide();

            foreach (GameObject root in _contentRoots.Values)
                if (root != null) root.GetComponent<BagActivityModalLayout>()?.Hide();
        }

        /// <summary>从内容源里把名为 viewName 的内容视图 reparent 进窗框内容区(根即视图 / 顶层子两形态),返回其 BaseView。</summary>
        private static BaseView ReparentFrom(string prefabName, string viewName, RectTransform parent)
        {
            if (!_contentRoots.TryGetValue(prefabName, out GameObject root) || root == null)
            {
                GameLog.Warn("Bag", "内容源 [{0}] 未加载(标签 {1})", prefabName, viewName);
                return null;
            }
            Transform t = root.GetComponent<BaseView>() != null ? root.transform : root.transform.Find(viewName);
            if (t == null)
            {
                GameLog.Warn("Bag", "内容视图 [{0}] 不在 {1}", viewName, prefabName);
                return null;
            }
            t.SetParent(parent, false);
            t.gameObject.SetActive(true);
            BaseView view = t.GetComponent<BaseView>();

            // 背包格渲染模板 bagItemRenderer 是模块组(BagModule)顶层兄弟、非视图 Bind 字段 →
            // 由 flow(已负责模块结构导航)注入,避免业务视图反向 transform.Find 兄弟节点。
            if (view is BagComponentView bagView)
            {
                Transform tpl = root.transform.Find("bagItemRenderer");
                BagItemRenderer rend = tpl != null ? tpl.GetComponent<BagItemRenderer>() : null;
                if (rend != null) bagView.SetItemTemplate(rend);
                else GameLog.Warn("Bag", "bagItemRenderer 模板未找到(BagModule 结构变动?)→ 背包格无法铺");
            }
            return view;
        }

        internal static void Reset()
        {
            if (_windowCloseHooked)
            {
                EventDispatcher.Off(GlobalEvent.EVT_BASE_WINDOW_CLOSED, OnAnyBaseWindowClosed);
                _windowCloseHooked = false;
            }
            if (_window != null) _window.SetReturnAction(null);
            if (_frameRoot != null) ResManager.ReleaseInstance(_frameRoot);
            foreach (GameObject root in _contentRoots.Values)
            {
                if (root != null) ResManager.ReleaseInstance(root);
            }
            _contentRoots.Clear();
            _moduleLoading.Clear();
            _frameRoot = null;
            _window = null;
            _titleOverlay = null;
            _tabEnabledSnapshot = null;
            _loading = false;
        }

        private static void ShowPlaceholderAndReset()
        {
            Reset();
            MainUIRoutePlaceholder.Show(PRIMARY_MODULE);
        }
    }
}
