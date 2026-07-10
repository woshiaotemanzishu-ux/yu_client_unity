using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.MainUI;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.UiCreator.MainUI
{
    /// <summary>
    /// 复活倒计时窗(MainUIReliveView)独立 prefab 生成器 —— 从 HudOverlayCombat.prefab【抽取】。
    ///
    /// 背景:<see cref="HudOverlayCombatCreator"/> 的 BuildRelive 早就把 MainUIReliveView 的完整几何/贴图/
    /// Bind 建对了(720x600 居中,对标老端 mainUI/MainUIReliveView.json centerX=0/centerY=0;
    /// _img_bg/_img_bg2/_lb_left_count/_lb_des2/_lb_des 五个节点几何已按 LayaRectMath 公式人工换算核对),
    /// 但它只是 HudOverlayCombat 这个「六合一战斗弹层」bundle 里默认 inactive 的一个子节点,从没落地成
    /// <see cref="Shenxiao.Module.Core.MainUI.MainUIFlow"/>.ShowReliveAsync 真正会加载的独立 prefab
    /// (GameResPath.GetUIPrefab("mainUI","MainUIReliveView") = prefabs/ui/mainui/mainuireliveview)——
    /// 该方法此前只能 InstantiateAsync 失败打 Warn,复活窗实际打不开。
    ///
    /// 本 Creator 不重建树(避免跟 BuildRelive 的几何各改一半、日后失步),而是实例化 bundle → 完全解包 →
    /// 摘出 MainUIReliveView 子树 → 直接把它自己存成独立 prefab(手法对标 HudFuncOpenCreator 从
    /// FunctionOpenModule.prefab 摘 FunctionOpenIcon 子树)。
    ///
    /// 落盘位置与 CollectBarView.prefab / FightingUpView.prefab 同级(Assets/Prefabs/UI/MainUI/ 根下,
    /// 不进 Regions/ 子目录——这三个都是 MainUIFlow 按精确文件名单独 InstantiateAsync 的独立弹窗,
    /// 不是并入 MainUIModule 总装的区域)。MainUIFlow.ShowReliveAsync 用 go.GetComponent&lt;MainUIReliveView&gt;()
    /// 直接取 prefab 根组件(不是 GetComponentInChildren),所以根节点本身必须挂 MainUIReliveView,
    /// 不能再包一层容器——抽取时把 MainUIReliveView 所在的 RectTransform 直接摘成新 prefab 的根。
    ///
    /// 节点对照(沿用 HudOverlayCombatCreator 已定的语义化命名,未改名):
    ///   _img_bg -> ReviveBackgroundImage / _img_bg2 -> ReviveTitleBannerImage /
    ///   _lb_left_count -> ReviveCountdownNumberLabel / _lb_des2 -> ReviveDeathMessageLabel /
    ///   _lb_des -> ReviveCountdownHintLabel。
    ///
    /// 重跑前提:HudOverlayCombat.prefab 还在(它同时还托管着 HiterBigBlood/DropProgress/
    /// FlowerEffect/Effect/EffectPartnerSkill 五个未接线/待总装子视图,抽取后原样保留,不动 bundle 本体——
    /// bundle 内继续留一份 inactive 的 MainUIReliveView 副本,属于已知的「捆包死重」,与
    /// HudOverlayPopupsCreator 文档记载的 Buff/FightMode/IconBox 同类情况一致,不在本次改动范围;
    /// 若要清理,去改 HudOverlayCombatCreator.GenerateBundle 停建 BuildRelive,与本文件无关)。
    /// </summary>
    public static class MainUIReliveCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/MainUI/MainUIReliveView.prefab";
        private const string BundlePrefabPath = "Assets/Prefabs/UI/MainUI/Overlays/HudOverlayCombat.prefab";

        private static GameObject _previewInstance;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "MainUI",
                Name = "MainUIReliveView(复活倒计时窗·独立)",
                Note = "从 HudOverlayCombat.prefab 抽取 MainUIReliveView 子树,补齐 MainUIFlow.ShowReliveAsync 缺失的独立 prefab",
                Order = 73,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });
        }

        public static void Generate()
        {
            GameObject bundleAsset = AssetDatabase.LoadAssetAtPath<GameObject>(BundlePrefabPath);
            if (bundleAsset == null)
            {
                Debug.LogError("[UiCreator] 找不到 " + BundlePrefabPath +
                                ",无法抽取 MainUIReliveView(先跑「HudOverlayCombat(战斗弹层×6)」生成该 bundle)");
                return;
            }

            // 实例化 → 完全解包(否则不能把子树从 prefab 实例里摘出来)→ 找 MainUIReliveView 业务组件。
            var bundleInst = (GameObject)PrefabUtility.InstantiatePrefab(bundleAsset);
            PrefabUtility.UnpackPrefabInstance(bundleInst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            MainUIReliveView view = bundleInst.GetComponentInChildren<MainUIReliveView>(true);
            if (view == null)
            {
                Object.DestroyImmediate(bundleInst);
                Debug.LogError("[UiCreator] HudOverlayCombat.prefab 里没有 MainUIReliveView 业务组件(需先重跑该 bundle 生成)");
                return;
            }

            var root = (RectTransform)view.transform;
            root.SetParent(null, false);     // 摘成游离根:prefab 根即 MainUIReliveView 本体(对标 FightingUpView.prefab 的直挂法)
            root.gameObject.SetActive(true); // bundle 里默认 inactive,独立 prefab 需要点亮(运行时仍由 ShowReliveAsync/Show() 再管一次)
            Object.DestroyImmediate(bundleInst); // 摘完子树,bundle 其余五个子视图不进本 prefab

            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] MainUIReliveView.prefab 已生成: " + PrefabPath +
                      "(从 HudOverlayCombat.prefab 抽取,几何/贴图/Bind 原样;MainUIFlow.ShowReliveAsync 现在能正常加载;" +
                      "真机包前记得跑一次 Addressable 自动分组)");
        }

        /// <summary>
        /// 批处理入口(供 -executeMethod 调用,不依赖 [MenuItem]/交互面板):
        ///   Unity.exe -batchmode -projectPath . -executeMethod
        ///     Shenxiao.Editor.UiCreator.MainUI.MainUIReliveCreator.GenerateBatch -logFile Temp/mainui_relive_creator.log
        /// 生成成功且 prefab 资产确实落盘 → Exit(0);否则 Exit(1)。
        /// </summary>
        public static void GenerateBatch()
        {
            try
            {
                Generate();
                bool ok = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
                Debug.Log("[UiCreator] MainUIReliveCreator.GenerateBatch " + (ok ? "OK " : "FAILED ") + PrefabPath);
                EditorApplication.Exit(ok ? 0 : 1);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[UiCreator] MainUIReliveCreator.GenerateBatch 异常: " + e);
                EditorApplication.Exit(1);
            }
        }

        public static void Preview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 MainUIReliveView",
                    "请先进入 Play 模式(游戏已起、mainUI addressable 可用)再点预览。\n\n" +
                    "预览走 MainUIFlow.ShowReliveAsync 同一条加载路径(GetUIPrefab(\"mainUI\",\"MainUIReliveView\") + " +
                    "InstantiateAsync + Show()),仅用于看结构/倒计时表现,不发真实复活协议。",
                    "好");
                return;
            }
            _ = PreviewAsync();
        }

        private static async Task PreviewAsync()
        {
            if (_previewInstance != null)
            {
                Object.Destroy(_previewInstance);
                _previewInstance = null;
            }

            string key = GameResPath.GetUIPrefab("mainUI", "MainUIReliveView");
            GameObject go = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
            if (go == null)
            {
                Debug.LogWarning("[UiCreator] MainUIReliveView 预览加载失败: " + key + "(检查 addressable/是否已生成)");
                return;
            }
            MainUIReliveView view = go.GetComponent<MainUIReliveView>();
            if (view == null)
            {
                Debug.LogWarning("[UiCreator] MainUIReliveView 预览缺组件(重跑生成)");
                Object.Destroy(go);
                return;
            }
            _previewInstance = go;
            view.Show(); // 无参:走本地默认倒计时(_defaultReliveSeconds=5,对标老端 GetReliveDuration()=5)
        }
    }
}
