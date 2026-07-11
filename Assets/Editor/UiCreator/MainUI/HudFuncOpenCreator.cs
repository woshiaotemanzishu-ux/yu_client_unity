using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.FunctionOpen;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.UiCreator.MainUI
{
    /// <summary>
    /// 主界面「功能预告框」区域生成器 —— 从现网 FunctionOpenModule.prefab【抽取】FunctionOpenIcon 子树成区。
    ///
    /// 这是什么:老端 funcOpen 模块的主界面挂件 FunctionOpenIcon(功能开启预告):展示下一个即将解锁的功能
    /// (图标/3D模型 + "X级开启"金字 tips + 红点),≥2 个待开功能时 10 秒轮播,点击打开功能预告列表(Stage 2)。
    /// 位置(快照实测,与老端一致):右上 (570,313),148×141,贴在竞榜卡(117~311)正下方。
    ///
    /// 为什么是"抽取"而不是重建:FunctionOpenModule.prefab 是转换器+回填管线的产物,FunctionOpenIcon 子树的
    /// 几何/贴图/业务绑定都已正确;本 Creator 实例化该模块 → 完全解包 → 摘出 FunctionOpenIcon 子树 → 套上
    /// 有界 root 存为独立区域 prefab。改视觉去 HudFuncOpen.prefab 手调;若要重跑本 Creator,前提是
    /// FunctionOpenModule.prefab 还在(它同时还托管着未接线的 ListView/TipView/AutoView 弹窗结构)。
    ///
    ///   HudFuncOpen(root)            —— 有界:右上锚,右缘内缩 2、顶 313,148×141
    ///     FunctionOpenIcon(view)      —— Stretch 填满 root(业务类/子结构原样保留:click_box/bg/tips/icon_con/…)
    ///
    /// 运行时:并入 MainUIModule 总装,由 MainUIFlow.FirstPassViews 统一 Show(原 FunctionOpenFlow 单独挂载
    /// 已退役);显示/轮播/折叠逻辑全在 FunctionOpenIcon 业务类里,与之前一致。
    /// </summary>
    public static class HudFuncOpenCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/MainUI/Regions/HudFuncOpen.prefab";
        private const string ModulePrefabPath = "Assets/Prefabs/UI/FunctionOpen/FunctionOpenModule.prefab";

        // 有界 root:模块里 FunctionOpenIcon 节点为右上锚 pos(-150,-313) pivot(0,1) 148×141
        // → 面板占 (570..718, 313..454),即右缘内缩 2、顶 313。
        private const float RegionRightInset = 2f, RegionTop = 313f;
        private const float RegionW = 148f, RegionH = 141f;

        private static GameObject _previewInstance;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "MainUI",
                Name = "HudFuncOpen(功能预告框)",
                Note = "竞榜卡下方功能开启预告(FunctionOpenIcon),从 FunctionOpenModule.prefab 抽取子树成有界区",
                Order = 27,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });
        }

        public static void Generate()
        {
            GameObject moduleAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModulePrefabPath);
            if (moduleAsset == null)
            {
                Debug.LogError("[UiCreator] 找不到 " + ModulePrefabPath + ",无法抽取 FunctionOpenIcon(该模块 prefab 是转换器产物,若被删需从 git 找回)");
                return;
            }

            // 实例化 → 完全解包(否则不能把子树从 prefab 实例里摘出来)→ 找图标子树。
            var moduleInst = (GameObject)PrefabUtility.InstantiatePrefab(moduleAsset);
            PrefabUtility.UnpackPrefabInstance(moduleInst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            FunctionOpenIcon icon = moduleInst.GetComponentInChildren<FunctionOpenIcon>(true);
            if (icon == null)
            {
                Object.DestroyImmediate(moduleInst);
                Debug.LogError("[UiCreator] FunctionOpenModule.prefab 里没有 FunctionOpenIcon 业务组件(需先跑 functionOpen 回填)");
                return;
            }

            RectTransform root = UiCreatorKit.NewRoot("HudFuncOpen");
            AnchorTopRight(root, RegionRightInset, RegionTop, RegionW, RegionH);
            root.gameObject.SetActive(false);

            var iconRt = (RectTransform)icon.transform;
            iconRt.SetParent(root, false);
            UiCreatorKit.Stretch(iconRt); // 填满有界 root;子节点(click_box/bg/tips/…)相对坐标原样保留
            icon.gameObject.SetActive(true); // 模块里默认 inactive;区域 prefab 里点亮便于所见即所得(运行时归 MainUIFlow 统一管)
            Object.DestroyImmediate(moduleInst); // 摘完子树,模块实例其余部分(弹窗结构)不进本区

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] HudFuncOpen.prefab 已生成: " + PrefabPath +
                      "(FunctionOpenIcon 抽取自 FunctionOpenModule.prefab,几何/绑定原样;人工核对后并入 MainUIModule.prefab)");
        }

        /// <summary>右上锚定:锚点/轴心取父右上角,rightInset=距右缘、top=距顶。区域 root 专用(有界、不全屏)。</summary>
        private static void AnchorTopRight(RectTransform rt, float rightInset, float top, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(-rightInset, -top);
        }

        public static void Preview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 HudFuncOpen",
                    "请先进入 Play 模式(主界面已起、UI 层已初始化)再点预览。\n\n" +
                    "HudFuncOpen 是并入 MainUIModule 的区域子视图,不走 ViewManager.Open<T>();" +
                    "预览直接把最新 prefab 实例化到 Window 层并手动调用 view.Show(),仅用于看结构。",
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
                Debug.LogError("[UiCreator] 找不到 " + PrefabPath + ",请先点生成");
                return;
            }

            Transform parent = ViewManager.GetLayer(UILayer.Window);
            _previewInstance = Object.Instantiate(prefab, parent);
            var view = _previewInstance.GetComponentInChildren<FunctionOpenIcon>(true);
            if (view == null)
            {
                Debug.LogError("[UiCreator] HudFuncOpen 预览实例缺少 FunctionOpenIcon 组件");
                return;
            }
            view.Show();
        }
    }
}
