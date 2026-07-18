using Shenxiao.Editor.LayaUI;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.UiCreator.Fashion
{
    /// <summary>
    /// FashionModule.prefab 运行时组件升级器(第21轮 PA,照 Friend/FriendBindUpgrader.cs、
    /// Equip/JewelBindUpgrader.cs 范式)。
    ///
    /// 背景:FashionModule.prefab 早已烤好(9 个 Bind 已挂进 prefab 且字段已回填),缺的只是
    /// "把 Bind 换成本轮写的业务子类"这一步——单阶段(无需像 FriendModule 那样先嫁接):
    /// 直接跑 LayaBindFiller.FillPrefab,把 FashionMainView/FashionColorItem/FashionItem/
    /// FashionAttrItem 从 {Name}Bind 升级为 Shenxiao.Module.Core.Fashion.{Name}(按节点名 + 反射找
    /// Bind 类的唯一业务子类,同一算法给 __Templates 内嵌模板也升级)。
    /// FashionLevelView/FashionSuitView(41305/41313-15,第二刀范围)本轮未写业务子类,跑完仍是
    /// 基类 Bind,不受影响、不报错(FindSingleSubclass 找不到子类时按老规矩用基类回填)。
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
                Note = "把 FashionMainView/FashionColorItem/FashionItem/FashionAttrItem 从生成的 Bind 基类" +
                       "升级为第21轮 PA 写的业务子类(LayaBindFiller.FillPrefab,幂等可重跑)",
                Order = 97,
                Generate = () => Generate(),
                PrefabPath = ModulePath,
            });
        }

        /// <summary>单阶段:升级回填(幂等可重跑)。成功返回 true。</summary>
        public static bool Generate()
        {
            if (!LayaBindFiller.FillPrefab(ModulePath))
            {
                Debug.LogError("[UiCreator] LayaBindFiller.FillPrefab(" + ModulePath + ") 失败(看 Console 前面的警告)");
                return false;
            }
            return Verify();
        }

        /// <summary>验证 4 个本轮新写的业务子类真实挂上(FashionLevelView/FashionSuitView 不在验证范围内——本轮未写)。</summary>
        private static bool Verify()
        {
            GameObject saved = AssetDatabase.LoadAssetAtPath<GameObject>(ModulePath);
            if (saved == null)
            {
                Debug.LogError("[UiCreator] 验证失败:" + ModulePath + " 加载不到");
                return false;
            }
            bool ok = true;
            ok &= Check<Shenxiao.Module.Core.Fashion.FashionMainView>(saved, "FashionMainView");
            ok &= Check<Shenxiao.Module.Core.Fashion.FashionColorItem>(saved, "FashionColorItem");
            ok &= Check<Shenxiao.Module.Core.Fashion.FashionItem>(saved, "FashionItem(__Templates 内)");
            ok &= Check<Shenxiao.Module.Core.Fashion.FashionAttrItem>(saved, "FashionAttrItem(__Templates 内)");
            Debug.Log("[UiCreator] FashionBindUpgrader 验证 " + (ok ? "OK" : "FAILED") + " " + ModulePath);
            return ok;
        }

        private static bool Check<T>(GameObject root, string label) where T : Component
        {
            if (root.GetComponentInChildren<T>(true) != null) return true;
            Debug.LogError("[UiCreator] 缺运行时组件 " + typeof(T).Name + "(" + label + ")");
            return false;
        }

        /// <summary>
        /// 批处理入口(供 -executeMethod 调用):
        ///   Unity.exe -batchmode -projectPath . -executeMethod
        ///     Shenxiao.Editor.UiCreator.Fashion.FashionBindUpgrader.GenerateBatch -logFile Temp/fashion_bind_upgrader.log
        /// 成功判据 = 4 个业务子类真实在挂 → Exit(0);否则 Exit(1)。
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
                Debug.LogError("[UiCreator] FashionBindUpgrader.GenerateBatch 异常: " + e);
                EditorApplication.Exit(1);
            }
        }
    }
}
