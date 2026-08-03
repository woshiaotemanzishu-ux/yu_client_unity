using Shenxiao.Editor.LayaUI;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Fashion;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.UiCreator.Fashion
{
    /// <summary>
    /// FashionModule.prefab 运行时组件升级器。
    ///
    /// FashionModule.prefab 已经挂好 9 个生成的 Bind 组件并回填字段；这里通过
    /// LayaBindFiller.FillPrefab 将它们升级为同名业务子类。范围包括第一刀的
    /// FashionMainView/FashionColorItem/FashionItem/FashionAttrItem，以及第二刀的
    /// FashionLevelView/FasBagItemRenderer/FashionSuitView/FashionSuitTabItem/
    /// FashionSuitGoodsItem。内嵌在 __Templates 下的模板也使用相同规则升级。
    /// </summary>
    public static class FashionBindUpgrader
    {
        private const string ModulePath = "Assets/Prefabs/UI/Fashion/FashionModule.prefab";

        // FashionModule 已由人工 Prefab 接管，不再进入批量重建注册表；本方法仅供显式修复旧产物。
        /// <summary>单阶段升级回填（幂等可重跑）。成功返回 true。</summary>
        public static bool Generate()
        {
            if (!LayaBindFiller.FillPrefab(ModulePath))
            {
                Debug.LogError("[UiCreator] LayaBindFiller.FillPrefab(" + ModulePath + ") 失败（见 Console 前面的警告）");
                return false;
            }
            return Verify();
        }

        /// <summary>验证 9 个时装业务子类均真实挂载到 prefab。</summary>
        private static bool Verify()
        {
            GameObject saved = AssetDatabase.LoadAssetAtPath<GameObject>(ModulePath);
            if (saved == null)
            {
                Debug.LogError("[UiCreator] 验证失败：" + ModulePath + " 加载不到");
                return false;
            }

            bool ok = true;
            ok &= Check<Shenxiao.Module.Core.Fashion.FashionMainView>(saved, "FashionMainView");
            ok &= Check<Shenxiao.Module.Core.Fashion.FashionColorItem>(saved, "FashionColorItem");
            ok &= Check<Shenxiao.Module.Core.Fashion.FashionItem>(saved, "FashionItem(__Templates 内)");
            ok &= Check<Shenxiao.Module.Core.Fashion.FashionAttrItem>(saved, "FashionAttrItem(__Templates 内)");
            ok &= Check<Shenxiao.Module.Core.Fashion.FashionLevelView>(saved, "FashionLevelView");
            ok &= Check<Shenxiao.Module.Core.Fashion.FasBagItemRenderer>(saved, "FasBagItemRenderer");
            ok &= Check<Shenxiao.Module.Core.Fashion.FashionSuitView>(saved, "FashionSuitView");
            ok &= Check<Shenxiao.Module.Core.Fashion.FashionSuitTabItem>(saved, "FashionSuitTabItem");
            ok &= Check<Shenxiao.Module.Core.Fashion.FashionSuitGoodsItem>(saved, "FashionSuitGoodsItem");
            Debug.Log("[UiCreator] FashionBindUpgrader 验证 " + (ok ? "OK" : "FAILED") + " " + ModulePath);
            return ok;
        }

        private static bool Check<T>(GameObject root, string label) where T : Component
        {
            if (root.GetComponentInChildren<T>(true) != null) return true;
            Debug.LogError("[UiCreator] 缺运行时组件 " + typeof(T).Name + "（" + label + "）");
            return false;
        }

        /// <summary>
        /// 批处理入口（供 -executeMethod 调用）：
        ///   Unity.exe -batchmode -projectPath . -executeMethod
        ///     Shenxiao.Editor.UiCreator.Fashion.FashionBindUpgrader.GenerateBatch -logFile Temp/fashion_bind_upgrader.log
        /// 成功判据 = 9 个业务子类真实挂载，成功 Exit(0)，否则 Exit(1)。
        /// </summary>
        public static void GenerateBatch()
        {
            try
            {
                bool ok = Generate();
                Debug.Log("[UiCreator] FashionBindUpgrader.GenerateBatch " + (ok ? "OK " : "FAILED ") + ModulePath);
                EditorApplication.Exit(ok ? 0 : 1);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[UiCreator] FashionBindUpgrader.GenerateBatch 异常：" + e);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>把老端 FashionMainView 的空容器 _box_fight(x=307,y=25,w=250,h=52)
        /// 与 FightingShowSmallItem 直接保存进当前 Prefab；不重转模块，不改其它视觉节点。</summary>
        public static bool PatchPowerAnchor()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(ModulePath);
            try
            {
                FashionMainView view = root.GetComponentInChildren<FashionMainView>(true);
                if (view == null) return false;
                Transform existing = view.transform.Find("_box_fight");
                RectTransform anchor;
                if (existing == null)
                {
                    var go = new GameObject("_box_fight", typeof(RectTransform));
                    anchor = (RectTransform)go.transform;
                    anchor.SetParent(view.transform, false);
                    anchor.anchorMin = anchor.anchorMax = new Vector2(0f, 1f);
                    anchor.pivot = new Vector2(0f, 1f);
                    anchor.anchoredPosition = new Vector2(307f, -25f);
                    anchor.sizeDelta = new Vector2(250f, 52f);
                }
                else anchor = existing as RectTransform;

                FightingShowSmallItem item = anchor != null ? anchor.GetComponentInChildren<FightingShowSmallItem>(true) : null;
                if (item == null && anchor != null)
                {
                    GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Common/FightingShowSmallItem.prefab");
                    GameObject instance = template != null ? PrefabUtility.InstantiatePrefab(template, anchor) as GameObject : null;
                    if (instance == null) return false;
                    instance.name = "FightingShowSmallItem";
                    RectTransform rect = instance.transform as RectTransform;
                    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
                    rect.pivot = new Vector2(0.5f, 1f);
                    rect.anchoredPosition = Vector2.zero;
                    rect.localScale = Vector3.one;
                    item = instance.GetComponent<FightingShowSmallItem>();
                }

                PrefabUtility.SaveAsPrefabAsset(root, ModulePath);
                return item != null;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
                AssetDatabase.SaveAssets();
            }
        }

        public static void PatchPowerAnchorBatch()
        {
            try { EditorApplication.Exit(PatchPowerAnchor() ? 0 : 1); }
            catch (System.Exception exception) { Debug.LogError(exception); EditorApplication.Exit(1); }
        }
    }
}
