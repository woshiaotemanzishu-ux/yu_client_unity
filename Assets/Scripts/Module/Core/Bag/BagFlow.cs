using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 背包模块编排:五标签窗(对标老端 BagView extends BaseWindowComponent)。**走 BaseWindowSkinView 地基 + 多内容源**(标签内容跨模块,= 老端 viewClassModuleDic)。
    ///
    /// 老端 tabStrList=[背包/仓库/影骸战衣/启示圣铠/九天神祭];内容分散在 bag / holySeal / revelation / longlanguage 模块。
    /// Unity 已写 背包/仓库(bag,tab0/1)+ 影骸战衣(holySeal,tab2)→ 开放;启示圣铠(revelation)/九天神祭(longlanguage)未写 → disabled。
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
            "\u80CC\u5305", "\u4ED3\u5E93", "\u5F71\u9AA8\u6218\u8863", "\u542F\u793A\u5723\u94E0", "\u4E5D\u5929\u795E\u7B26"
        };
        // 5/5 全开(背包/仓库/影骸战衣/启示圣铠/九天神祭);longlanguage 经 LayaBindFiller 大小写不敏感兜底后可正常挂载
        private static readonly bool[] TabEnabled = { true, true, false, false, false };
        private const int DefaultTab = 0;

        private static GameObject _frameRoot;
        // 内容源根:prefab 名 → 实例(去重)
        private static readonly Dictionary<string, GameObject> _contentRoots = new Dictionary<string, GameObject>();
        private static BaseWindowSkinView _window;
        private static bool _loading;

        public static void Toggle()
        {
            if (_window != null && _window.IsShown) { Close(); return; }
            _ = OpenAsync();
        }

        public static void Open() => _ = OpenAsync();

        public static void Close()
        {
            if (_window != null) _window.Hide();
        }

        /// <summary>切换背包模块内子窗(一键使用/熔炼/扩展…),已显则关、未显则开;在所有已加载内容源里按 View 子类名查找。</summary>
        public static void ToggleSub(string viewTypeName)
        {
            BaseView v = FindSub(viewTypeName);
            if (v == null) { GameLog.Info("Bag", "背包子窗 [{0}] 未移植 View,待对接", viewTypeName); return; }
            if (v.IsShown) v.Hide(); else v.Show();
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
            v.Show(args);
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
                    if (v.GetType().Name == viewTypeName) return v;
                }
            }
            return null;
        }

        private static async Task OpenAsync()
        {
            if (_frameRoot != null)
            {
                if (_window != null) _window.Show();
                return;
            }

            if (_loading) return;
            _loading = true;

            string frameKey = GameResPath.GetUIPrefab(FRAME_MODULE, FRAME_PREFAB);
            try
            {
            _frameRoot = await MainUIRouteFallback.InstantiateOrShowAsync(PRIMARY_MODULE, "Bag", frameKey, ViewManager.GetLayer(UILayer.Window));

            var needPrefab = new Dictionary<string, string> { { PRIMARY_PREFAB, PRIMARY_MODULE } };
            for (int i = 0; i < TabContent.Length; i++)
            {
                if (TabEnabled[i] && !needPrefab.ContainsKey(TabPrefab[i])) needPrefab[TabPrefab[i]] = TabModule[i];
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
                bool enabled = TabEnabled[i];
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
            GameLog.Info("Bag", "背包五标签窗打开(BaseWindowSkinView,默认 tab{0} 背包)", DefaultTab);
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
            if (_frameRoot != null) ResManager.ReleaseInstance(_frameRoot);
            foreach (GameObject root in _contentRoots.Values)
            {
                if (root != null) ResManager.ReleaseInstance(root);
            }
            _contentRoots.Clear();
            _frameRoot = null;
            _window = null;
            _loading = false;
        }

        private static void ShowPlaceholderAndReset()
        {
            Reset();
            MainUIRoutePlaceholder.Show(PRIMARY_MODULE);
        }
    }
}
