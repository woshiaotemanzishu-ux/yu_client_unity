using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.MainUI;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.UiCreator.MainUI
{
    /// <summary>
    /// 主界面【总装】生成器:把 6 个 Region + 2 个 Overlay bundle(均为重构版 UICreator 产物)
    /// 以嵌套 prefab 方式装进一个根节点,覆盖存盘为运行时实际加载的
    /// Assets/Prefabs/UI/MainUI/MainUIModule.prefab(旧 Laya 转换器版本被替换,git 可回退)。
    ///
    /// - 子 prefab 缺失时先自动调用对应 Creator 补生成;补完仍缺则报错退出、不动现网 prefab。
    /// - 嵌套保持 prefab 连接:改任一 Region prefab,总装产物自动跟随。
    /// - CollectBarView / FightingUpView / ArrowComponent 三个运行时按名单独加载的 prefab
    ///   不进总装(MainUIFlow/MainUIGuideManager 走独立地址),由各自条目单独生成。
    /// - 运行时行为:MainUIFlow 实例化本 prefab 后收集全部 BaseView 统一关闭,再按
    ///   InitMainUI 顺序 Show 首批 9 个视图 —— 因此子节点顺序只是编辑器观感,不影响逻辑。
    /// </summary>
    public static class MainUIModuleCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/MainUI/MainUIModule.prefab";

        // 子 prefab 路径 + 缺失时的补生成动作;顺序 = prefab 子节点顺序。
        private static readonly (string path, string label, System.Action generate)[] Parts =
        {
            ("Assets/Prefabs/UI/MainUI/Regions/HudTop.prefab", "HudTop", HudTopCreator.Generate),
            ("Assets/Prefabs/UI/MainUI/Regions/HudActivity.prefab", "HudActivity", HudActivityCreator.Generate),
            ("Assets/Prefabs/UI/MainUI/Regions/HudTaskTeam.prefab", "HudTaskTeam", HudTaskTeamCreator.Generate),
            ("Assets/Prefabs/UI/MainUI/Regions/HudSkill.prefab", "HudSkill", HudSkillCreator.Generate),
            ("Assets/Prefabs/UI/MainUI/Regions/HudBottomBar.prefab", "HudBottomBar", HudBottomBarCreator.Generate),
            ("Assets/Prefabs/UI/MainUI/Regions/HudSecondary.prefab", "HudSecondary", HudSecondaryCreator.Generate),
            ("Assets/Prefabs/UI/MainUI/Overlays/HudOverlayCombat.prefab", "HudOverlayCombat", HudOverlayCombatCreator.GenerateBundle),
            ("Assets/Prefabs/UI/MainUI/Overlays/HudOverlayPopups.prefab", "HudOverlayPopups", HudOverlayPopupsCreator.GenerateOverlays),
        };

        /// <summary>MainUIFlow.FirstPassViews 同款首批显示顺序(预览用)。</summary>
        private static readonly System.Type[] FirstPassViews =
        {
            typeof(MainUITopViewBind),
            typeof(MainUIActivityViewBind),
            typeof(MainUISkillViewBind),
            typeof(MainUIChatViewBind),
            typeof(MainUISecondaryViewBind),
            typeof(MainUITaskTeamViewBind),
            typeof(MainUIDownViewBind),
            typeof(MainUIAutoBrushViewBind),
            typeof(UIJoyStickBind),
        };

        private static GameObject _previewInstance;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "MainUI",
                Name = "MainUIModule(总装·覆盖现网)",
                Note = "嵌套 6 Region + 2 Overlay 存为运行时加载的 MainUIModule.prefab;缺的子件先自动补生成",
                Order = 90,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });
        }

        public static void Generate()
        {
            // 第一遍:缺失的子 prefab 先补生成(逐个 try,单个失败不拦其它)。
            foreach (var part in Parts)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(part.path) != null) continue;
                Debug.Log("[UiCreator] 总装:缺 " + part.label + ",先补生成…");
                try { part.generate(); }
                catch (System.Exception e)
                {
                    Debug.LogError("[UiCreator] 总装:补生成 " + part.label + " 失败: " + e.Message);
                }
            }

            // 第二遍:全部就位才动现网 prefab,缺一个都不覆盖。
            var missing = new System.Collections.Generic.List<string>();
            foreach (var part in Parts)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(part.path) == null) missing.Add(part.label);
            }
            if (missing.Count > 0)
            {
                Debug.LogError("[UiCreator] 总装中止,以下子 prefab 仍缺失(现网 MainUIModule.prefab 未被改动): "
                    + string.Join(", ", missing));
                return;
            }

            RectTransform root = UiCreatorKit.NewRoot("MainUIModule");
            root.gameObject.SetActive(false);

            foreach (var part in Parts)
            {
                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(part.path);
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(asset, root);
                // 子 prefab 根都是全屏 Stretch;重挂父后把边距归零,避免继承实例化偏移。
                if (inst.transform is RectTransform rt
                    && rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one)
                {
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }
            }

            root.gameObject.SetActive(true);
            UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);
            Debug.Log("[UiCreator] 总装完成:8 个子模块 → " + PrefabPath + "(已覆盖旧转换器版本,git 可回退)");
        }

        public static void Preview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 MainUIModule",
                    "请先进入 Play 模式(UI 层已初始化)再点预览。\n\n" +
                    "预览会复刻 MainUIFlow 的做法:实例化总装 prefab → 关闭全部 BaseView → " +
                    "按 InitMainUI 顺序 Show 首批 9 个视图。弹层类视图保持关闭。",
                    "好");
                return;
            }

            if (_previewInstance != null)
            {
                Object.Destroy(_previewInstance);
                _previewInstance = null;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[UiCreator] 预览失败,找不到 " + PrefabPath + "(请先点生成)");
                return;
            }

            Transform layer = ViewManager.GetLayer(UILayer.Main);
            _previewInstance = Object.Instantiate(prefab, layer);
            _previewInstance.name = "MainUIModule(Preview)";

            BaseView[] views = _previewInstance.GetComponentsInChildren<BaseView>(true);
            foreach (BaseView v in views)
            {
                v.gameObject.SetActive(false);
            }
            foreach (System.Type t in FirstPassViews)
            {
                var view = (BaseView)_previewInstance.GetComponentInChildren(t, true);
                if (view != null) view.Show();
                else Debug.LogWarning("[UiCreator] 预览:总装里没找到 " + t.Name);
            }
            Selection.activeObject = _previewInstance;
        }
    }
}
