using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using UnityEngine;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 装备模块编排:六标签窗(对标老端 EquipView extends BaseWindowComponent;MainFunc.Equip(=4) → OPEN_VIEW "EquipView")。
    /// **走 BaseWindowSkinView 地基,且示范「跨模块标签内容」**:各标签内容可来自不同 module 预制体(老端 viewClassModuleDic 同义)。
    ///
    /// 老端 tabStrList=[天殒淬炉/神兵淬炼/骸珀镶嵌/吞天洗魄/神屠九炼/不朽圣骸];内容分散在 equip / jewel / equipRefinement / equipArmor 等模块。
    /// Unity 已写:淬炉/淬炼/洗魄(equip,tab0/1/3)+ 神屠九炼(equipRefinement,tab4)→ 这 4 标签开放(默认 tab0);镶嵌(jewel inline)/圣骸(equipArmor)未写 → disabled。
    /// 打开时按需加载各标签所属内容预制体(去重),点标签把对应内容 reparent 进窗框内容区 _gp_item_con(懒加载缓存)。
    /// 入口 <see cref="EquipBootstrap"/>(MainUIRouter "equip");子窗(淬炉宗师等)经 <see cref="OpenSub"/>;再点图标 <see cref="Toggle"/> 关闭。
    /// </summary>
    public static class EquipFlow
    {
        private const string FRAME_MODULE = "common";
        private const string FRAME_PREFAB = "BaseWindowSkin";
        private const string PRIMARY_MODULE = "equip";
        private const string PRIMARY_PREFAB = "EquipModule";

        // 老端 EquipView.viewClassList(标签索引 → 内容视图类名)
        private static readonly string[] TabContent =
        {
            "EquipStrenView", "EquipSmeltView", "EquipJewelView",
            "EquipWashView", "EquipRefinementView", "EquipArmorView"
        };
        private static readonly string[] TabLabels =
        {
            "天殒淬炉", "神兵淬炼", "骸珀镶嵌",
            "吞天洗魄", "神屠九炼", "不朽圣骸"
        };
        private static readonly string[] TabBackgrounds =
        {
            "ui_role_bg5.jpg", "ui_role_bg5.jpg", "ui_forge_bg.jpg",
            "ui_forge_bg2.jpg", "ui_forge_bg1.jpg", "ui_role_bg5.jpg"
        };
        // 各标签内容所属 module/prefab(跨模块);未写的标签随便填(disabled 不加载)
        private static readonly string[] TabModule =
        {
            "equip", "equip", "jewel", "equip", "equipRefinement", "equipArmor"
        };
        private static readonly string[] TabPrefab =
        {
            "EquipModule", "EquipModule", "JewelModule", "EquipModule", "EquipRefinementModule", "EquipArmorModule"
        };
        // 该标签内容是否已在 Unity 写好(写好才开放;其余 disabled,写完置 true 即开)
        // tab2 骸珀镶嵌(EquipJewelView,自动循环 轮4 下半/4b):Jewel 手写 View 已补齐,开放。
        private static readonly bool[] TabEnabled = { true, true, true, true, true, true };
        private const int DefaultTab = 0;

        private static GameObject _frameRoot;
        // 内容源根:prefab 名 → 实例(去重,多标签可共用一个 prefab)
        private static readonly Dictionary<string, GameObject> _contentRoots = new Dictionary<string, GameObject>();
        private static readonly HashSet<BaseView> _attachedViews = new HashSet<BaseView>();
        private static readonly HashSet<BaseView> _openSubViews = new HashSet<BaseView>();
        private static BaseWindowSkinView _window;
        private static bool _loading;
        private static bool _windowEventSubscribed;

        public static void Toggle()
        {
            if (_window != null && _window.IsShown) { Close(); return; }
            _ = OpenAsync();
        }

        public static void Open() => _ = OpenAsync();

        public static void Close()
        {
            HideModuleViews();
            if (_window != null) _window.Hide();
        }

        /// <summary>打开装备模块内子窗(淬炉宗师 EquipStrenMasterView、洗魄选材 EquipWashGoodsView、宝石背包
        /// EquipJewelBagView…),在所有已加载内容源里按 View 子类名查找并 Show(args)。args 透传给
        /// BaseView.Show(object),供需要打开上下文的弹窗(如 EquipJewelBagView.Context)使用;无需上下文的
        /// 子窗传 null 与既有调用完全等价(向后兼容,原调用点无需改)。未移植 → 日志降级。</summary>
        public static void OpenSub(string viewTypeName, object args = null)
        {
            if (_contentRoots.Count == 0)
            {
                GameLog.Warn("Equip", "OpenSub({0}) 时装备模块未打开", viewTypeName);
                return;
            }
            foreach (GameObject root in _contentRoots.Values)
            {
                if (root == null) continue;
                foreach (BaseView v in root.GetComponentsInChildren<BaseView>(true))
                {
                    if (v.GetType().Name == viewTypeName)
                    {
                        // 子窗仍挂在各自模块根下；共享窗框与模块根同处 Window 层。
                        // BaseView.Show 只能在模块根内部置顶，因此先提升所属模块根，避免“已 Show 但在窗框后方”。
                        root.transform.SetAsLastSibling();
                        v.Show(args);
                        _openSubViews.Add(v);
                        return;
                    }
                }
            }
            GameLog.Info("Equip", "装备子窗 [{0}] 未移植 View,待对接", viewTypeName);
        }

        /// <summary>按业务 View 身份关闭装备模块子窗，确保切页/关页走 BaseView.Hide 生命周期。</summary>
        public static void CloseSub(string viewTypeName)
        {
            if (string.IsNullOrEmpty(viewTypeName) || _openSubViews.Count == 0) return;
            var closed = new List<BaseView>();
            foreach (BaseView view in _openSubViews)
            {
                if (view == null || view.GetType().Name != viewTypeName) continue;
                if (view.IsShown) view.Hide();
                closed.Add(view);
            }
            foreach (BaseView view in closed) _openSubViews.Remove(view);
        }

        internal static bool TryGetStrengthTemplates(out EquipStrenItem itemTemplate,
            out GameObject equipmentTemplate, out GameObject fightingTemplate)
        {
            itemTemplate = null;
            equipmentTemplate = null;
            fightingTemplate = null;
            foreach (BaseView attached in _attachedViews)
            {
                if (!(attached is EquipStrenView strength)) continue;
                itemTemplate = strength.ItemTemplate;
                equipmentTemplate = strength.EquipmentTemplate;
                fightingTemplate = strength.FightingTemplate;
                return itemTemplate != null && equipmentTemplate != null;
            }

            if (_contentRoots.TryGetValue(PRIMARY_PREFAB, out GameObject root) && root != null)
            {
                EquipStrenView strength = root.GetComponentInChildren<EquipStrenView>(true);
                if (strength != null)
                {
                    itemTemplate = strength.ItemTemplate;
                    equipmentTemplate = strength.EquipmentTemplate;
                    fightingTemplate = strength.FightingTemplate;
                }
            }
            return itemTemplate != null && equipmentTemplate != null;
        }

        internal static EquipJewelItem GetJewelItemTemplate()
        {
            // EquipJewelView 被移入共享窗框后，其私有 __Templates 也随页面离开 JewelModule 根。
            foreach (BaseView attached in _attachedViews)
            {
                if (!(attached is EquipJewelView jewel)) continue;
                foreach (EquipJewelItem candidate in jewel.GetComponentsInChildren<EquipJewelItem>(true))
                {
                    Transform parent = candidate.transform.parent;
                    if (parent != null && parent.name == "__Templates") return candidate;
                }
            }
            if (!_contentRoots.TryGetValue("JewelModule", out GameObject root) || root == null) return null;
            foreach (EquipJewelItem candidate in root.GetComponentsInChildren<EquipJewelItem>(true))
            {
                Transform parent = candidate.transform.parent;
                if (parent != null && parent.name == "__Templates") return candidate;
            }
            return null;
        }

        internal static EquipWashItem GetWashItemTemplate()
        {
            // 九炼页被移入共享窗框后仍要让洗魄 warm 重开取得同一私有模板。
            foreach (BaseView attached in _attachedViews)
            {
                if (!(attached is EquipRefinementView refinement)) continue;
                foreach (EquipWashItem candidate in refinement.GetComponentsInChildren<EquipWashItem>(true))
                {
                    Transform parent = candidate.transform.parent;
                    if (parent != null && parent.name == "__Templates") return candidate;
                }
            }
            if (!_contentRoots.TryGetValue("EquipRefinementModule", out GameObject root) || root == null) return null;
            foreach (EquipWashItem candidate in root.GetComponentsInChildren<EquipWashItem>(true))
            {
                Transform parent = candidate.transform.parent;
                if (parent != null && parent.name == "__Templates") return candidate;
            }
            return null;
        }

        private static async Task OpenAsync()
        {
            if (_frameRoot != null)
            {
                if (_window != null)
                {
                    _window.Show();
                    int reopenTab = _window.CurrentIndex >= 0 ? _window.CurrentIndex : DefaultTab;
                    _window.Configure(BuildTabSpecs(), reopenTab);
                }
                return;
            }

            if (_loading) return;
            _loading = true;
            try
            {
                await FuncOpenConfig.EnsureLoaded();

                // 共用窗框与四个内容源互不依赖，必须同批并行实例化；旧实现逐个 await 会把首开延迟相加。
                Transform windowLayer = ViewManager.GetLayer(UILayer.Window);
                string frameKey = GameResPath.GetUIPrefab(FRAME_MODULE, FRAME_PREFAB);
                Task<GameObject> frameTask = ResManager.InstantiateAsync(frameKey, windowLayer);
                var needPrefab = new Dictionary<string, string> { { PRIMARY_PREFAB, PRIMARY_MODULE } };
                for (int i = 0; i < TabContent.Length; i++)
                {
                    if (TabEnabled[i] && !needPrefab.ContainsKey(TabPrefab[i])) needPrefab[TabPrefab[i]] = TabModule[i];
                }
                var contentTasks = new Dictionary<string, Task<GameObject>>(needPrefab.Count);
                var allTasks = new List<Task<GameObject>>(needPrefab.Count + 1) { frameTask };
                foreach (KeyValuePair<string, string> kv in needPrefab)
                {
                    string key = GameResPath.GetUIPrefab(kv.Value, kv.Key);
                    Task<GameObject> task = ResManager.InstantiateAsync(key, windowLayer);
                    contentTasks[kv.Key] = task;
                    allTasks.Add(task);
                }

                await Task.WhenAll(allTasks);
                _frameRoot = frameTask.Result;
                foreach (KeyValuePair<string, Task<GameObject>> kv in contentTasks)
                {
                    GameObject root = kv.Value.Result;
                    if (root == null) continue;
                    root.name = kv.Key;
                    foreach (Transform child in root.transform) child.gameObject.SetActive(false);
                    _contentRoots[kv.Key] = root;
                }
            }
            catch (Exception ex)
            {
                GameLog.Error("Equip", "装备六标签窗并行加载失败: {0}", ex.Message);
                return;
            }
            finally
            {
                _loading = false;
            }

            if (_frameRoot == null || !_contentRoots.ContainsKey(PRIMARY_PREFAB))
            {
                GameLog.Error("Equip", "装备六标签窗加载失败(frame 或主内容源缺失)");
                return;
            }
            _frameRoot.name = FRAME_PREFAB;

            _window = _frameRoot.GetComponent<BaseWindowSkinView>();
            if (_window == null) _window = _frameRoot.GetComponentInChildren<BaseWindowSkinView>(true);
            if (_window == null)
            {
                GameLog.Warn("Equip", "BaseWindowSkin 缺 BaseWindowSkinView(重跑 common 流水线回填)");
                return;
            }
            _window.SetReturnAction(Close);
            if (!_windowEventSubscribed)
            {
                EventDispatcher.On(GlobalEvent.EVT_BASE_WINDOW_CLOSED, OnBaseWindowClosed);
                _windowEventSubscribed = true;
            }

            List<TabSpec> specs = BuildTabSpecs();

            _window.Show();
            _window.Configure(specs, DefaultTab);
            GameLog.Info("Equip", "装备六标签窗打开(BaseWindowSkinView,默认 tab{0} 天殒淬炉)", DefaultTab);
        }

        private static List<TabSpec> BuildTabSpecs()
        {
            var specs = new List<TabSpec>(TabContent.Length);
            for (int i = 0; i < TabContent.Length; i++)
            {
                string viewName = TabContent[i];
                string prefabName = TabPrefab[i];
                // 老端 BaseWindowComponent 会先按 ConfigFuncOpenCondition 过滤候选页签；未开放项不创建按钮。
                bool enabled = TabEnabled[i] && FuncOpenConfig.CheckFuncOpenState(viewName);
                specs.Add(new TabSpec
                {
                    Enabled = enabled,
                    Label = TabLabels[i],
                    TitleImagePath = GameResPath.GetIcon("equip", "uidz_001"),
                    BackgroundImagePath = GameResPath.GetBigBgPath(TabBackgrounds[i]),
                    ContentFactory = enabled ? (Func<RectTransform, BaseView>)(parent => ReparentFrom(prefabName, viewName, parent)) : null,
                });
            }
            return specs;
        }

        /// <summary>从指定内容源 prefab 里把名为 viewName 的内容视图 reparent 进窗框内容区(保留原始布局),返回其 BaseView。</summary>
        private static BaseView ReparentFrom(string prefabName, string viewName, RectTransform parent)
        {
            if (!_contentRoots.TryGetValue(prefabName, out GameObject root) || root == null)
            {
                GameLog.Warn("Equip", "内容源 [{0}] 未加载(标签 {1})", prefabName, viewName);
                return null;
            }
            Transform t = root.transform.Find(viewName);
            if (t == null)
            {
                GameLog.Warn("Equip", "内容视图 [{0}] 不在 {1} 顶层", viewName, prefabName);
                return null;
            }
            t.SetParent(parent, false);
            t.gameObject.SetActive(true);
            BaseView view = t.GetComponent<BaseView>();
            if (view != null) _attachedViews.Add(view);
            return view;
        }

        /// <summary>
        /// BaseWindowSkin 只负责隐藏共享窗框；Equip 的分页和独立子窗分散在四个内容源根中，
        /// 必须逐个走 BaseView.Hide，才能解绑事件并清掉弹窗，而不是只靠父节点 inactiveInHierarchy。
        /// </summary>
        private static void HideModuleViews()
        {
            foreach (BaseView view in _openSubViews)
            {
                if (view != null && view.IsShown) view.Hide();
            }
            _openSubViews.Clear();

            foreach (BaseView view in _attachedViews)
            {
                if (view != null && view.IsShown) view.Hide();
            }

            foreach (GameObject root in _contentRoots.Values)
            {
                if (root == null) continue;
                foreach (BaseView view in root.GetComponentsInChildren<BaseView>(true))
                {
                    if (view != null && view.IsShown) view.Hide();
                }
            }
        }

        private static void OnBaseWindowClosed()
        {
            // 兼容被 BaseWindowManager.ShowExclusive 直接隐藏的路径；返回按钮/Toggle 则已先走 Close。
            if (_window != null && !_window.IsShown) HideModuleViews();
        }

        internal static void Reset()
        {
            HideModuleViews();
            if (_windowEventSubscribed)
            {
                EventDispatcher.Off(GlobalEvent.EVT_BASE_WINDOW_CLOSED, OnBaseWindowClosed);
                _windowEventSubscribed = false;
            }
            if (_frameRoot != null) ResManager.ReleaseInstance(_frameRoot);
            foreach (GameObject root in _contentRoots.Values)
            {
                if (root != null) ResManager.ReleaseInstance(root);
            }
            _contentRoots.Clear();
            _attachedViews.Clear();
            _openSubViews.Clear();
            _frameRoot = null;
            _window = null;
            _loading = false;
        }
    }
}
