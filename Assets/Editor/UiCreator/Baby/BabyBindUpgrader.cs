using System.Reflection;
using Shenxiao.Editor.LayaUI;
using Shenxiao.Generated.UI.Baby;
using Shenxiao.Module.Core.Baby;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.UiCreator.Baby
{
    /// <summary>
    /// Baby UI 的 Bind 回填入口。模块与独立 BabyPropItem 源 prefab 必须一起回填：
    /// 后者同时会被模块内的嵌套模板引用，不能只升级 BabyModule。
    /// </summary>
    public static class BabyBindUpgrader
    {
        private const string ModulePath = "Assets/Prefabs/UI/Baby/BabyModule.prefab";
        private const string PropItemPath = "Assets/Prefabs/UI/Baby/BabyPropItem.prefab";
        private const BindingFlags BindFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "Baby",
                Name = "BabyModule(Bind 回填)",
                Note = "回填 BabyModule 与独立 BabyPropItem，并校验窗口、列表模板及全部 Bind 字段",
                Order = 99,
                Generate = () => Generate(),
                PrefabPath = ModulePath,
            });
        }

        /// <summary>回填两个源 prefab，随后执行可重复的静态绑定验收。</summary>
        public static bool Generate()
        {
            if (!Fill(ModulePath) || !Fill(PropItemPath)) return false;
            return Verify();
        }

        /// <summary>供 CLI Case 和人工菜单复用的只读验收。</summary>
        public static bool Verify()
        {
            GameObject module = AssetDatabase.LoadAssetAtPath<GameObject>(ModulePath);
            GameObject propItem = AssetDatabase.LoadAssetAtPath<GameObject>(PropItemPath);
            if (module == null || propItem == null)
            {
                Debug.LogError("[UiCreator] Baby Bind 验证失败: prefab 加载不到 module=" + (module != null)
                    + " propItem=" + (propItem != null));
                return false;
            }

            bool ok = true;
            // 五个窗口 Bind，及三种实际会被列表模板实例化的 Item Bind。
            ok &= CheckBind<GestateBabyViewBind>(module, "GestateBabyView");
            ok &= CheckBind<BabyFamilyViewBind>(module, "BabyFamilyView");
            ok &= CheckBind<BabyCultivateViewBind>(module, "BabyCultivateView");
            ok &= CheckBind<BabyChangedViewBind>(module, "BabyChangedView");
            ok &= CheckBind<BabyIllusionViewBind>(module, "BabyIllusionView");
            ok &= CheckBusinessView<GestateBabyView>(module, "GestateBabyView");
            ok &= CheckBusinessView<BabyFamilyView>(module, "BabyFamilyView");
            ok &= CheckBusinessView<BabyCultivateView>(module, "BabyCultivateView");
            ok &= CheckBusinessView<BabyCulTaskItem>(module, "BabyCulTaskItem(__Templates)");
            ok &= CheckBusinessView<BabyIllusionView>(module, "BabyIllusionView");
            ok &= CheckBind<BabyCulTaskItemBind>(module, "BabyCulTaskItem(__Templates)");
            ok &= CheckBind<BabyIlluItemBind>(module, "BabyIlluItem(__Templates)");
            ok &= CheckBind<BabyPropItemBind>(propItem, "BabyPropItem(独立源 prefab)");

            BabyCultivateViewBind cultivate = module.GetComponentInChildren<BabyCultivateViewBind>(true);
            BabyIllusionViewBind illusion = module.GetComponentInChildren<BabyIllusionViewBind>(true);
            ok &= CheckTemplate<BabyCulTaskItemBind>(cultivate != null ? cultivate._tpl_BabyCulTaskItem : null,
                "BabyCultivateView._tpl_BabyCulTaskItem");
            ok &= CheckTemplate<BabyPropItemBind>(cultivate != null ? cultivate._tpl_BabyPropItem : null,
                "BabyCultivateView._tpl_BabyPropItem");
            ok &= CheckTemplate<BabyIlluItemBind>(illusion != null ? illusion._tpl_BabyIlluItem : null,
                "BabyIllusionView._tpl_BabyIlluItem");
            ok &= CheckTemplate<BabyPropItemBind>(illusion != null ? illusion._tpl_BabyPropItem : null,
                "BabyIllusionView._tpl_BabyPropItem");
            Debug.Log("[UiCreator] BabyBindUpgrader 验证 " + (ok ? "OK " : "FAILED ") + ModulePath);
            return ok;
        }

        /// <summary>供 -executeMethod 调用；成功判据见 <see cref="Verify"/>。</summary>
        public static void GenerateBatch()
        {
            try
            {
                bool ok = Generate();
                Debug.Log("[UiCreator] BabyBindUpgrader.GenerateBatch " + (ok ? "OK " : "FAILED ") + ModulePath);
                EditorApplication.Exit(ok ? 0 : 1);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[UiCreator] BabyBindUpgrader.GenerateBatch 异常: " + e);
                EditorApplication.Exit(1);
            }
        }

        private static bool Fill(string path)
        {
            if (LayaBindFiller.FillPrefab(path)) return true;
            Debug.LogError("[UiCreator] LayaBindFiller.FillPrefab(" + path + ") 失败(看 Console 前面的警告)");
            return false;
        }

        private static bool CheckBind<T>(GameObject root, string label) where T : Component
        {
            T bind = root.GetComponentInChildren<T>(true);
            if (bind == null)
            {
                Debug.LogError("[UiCreator] Baby 缺 Bind " + typeof(T).Name + "(" + label + ")");
                return false;
            }

            bool ok = true;
            foreach (FieldInfo field in typeof(T).GetFields(BindFields))
            {
                if (field.GetValue(bind) != null) continue;
                Debug.LogError("[UiCreator] Baby Bind 字段未回填 " + typeof(T).Name + "." + field.Name + "(" + label + ")");
                ok = false;
            }
            return ok;
        }

        private static bool CheckTemplate<T>(GameObject template, string label) where T : Component
        {
            if (template != null && template.GetComponent<T>() != null) return true;
            Debug.LogError("[UiCreator] Baby 嵌套模板未解析到 " + typeof(T).Name + "(" + label + ")");
            return false;
        }

        private static bool CheckBusinessView<T>(GameObject root, string label) where T : Component
        {
            if (root.GetComponentInChildren<T>(true) != null) return true;
            Debug.LogError("[UiCreator] Baby missing business view " + typeof(T).Name + "(" + label + ")");
            return false;
        }
    }
}
