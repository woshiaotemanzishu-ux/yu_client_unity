using Shenxiao.Editor.LayaUI;
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

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "Fashion",
                Name = "FashionModule(时装 Bind 升级)",
                Note = "将 FashionModule 的 9 个生成 Bind 组件升级为业务子类" +
                       "（LayaBindFiller.FillPrefab，幂等可重跑）",
                Order = 97,
                Generate = () => Generate(),
                PrefabPath = ModulePath,
            });
        }

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
    }
}
