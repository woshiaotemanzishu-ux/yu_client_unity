using System;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Editor.UiCreator.Baby;
using Shenxiao.Generated.UI.Baby;
using Shenxiao.Module.Core.Baby;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>Baby UI 静态基线：回填后实例化两个源 prefab，验收窗口、Item Bind 与嵌套模板解析。</summary>
    public static class BabyUiCase
    {
        private const string ModulePath = "Assets/Prefabs/UI/Baby/BabyModule.prefab";
        private const string PropItemPath = "Assets/Prefabs/UI/Baby/BabyPropItem.prefab";
        private const BindingFlags BindFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;

        public static Task<int> Run()
        {
            try
            {
                bool upgraded = BabyBindUpgrader.Generate();
                bool prefab = upgraded && VerifyInstances();
                bool pass = upgraded && prefab;
                Debug.Log("CLIVERIFY babyui VERDICT upgraded=" + upgraded + " prefab=" + prefab + " pass=" + pass);
                return Task.FromResult(pass ? 0 : 3);
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY babyui EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        public static void RunBatch()
        {
            _ = RunBatchAsync();
        }

        private static async Task RunBatchAsync()
        {
            int code = await Run();
            Debug.Log("CLIVERIFY babyui EXIT " + code);
            EditorApplication.Exit(code);
        }

        private static bool VerifyInstances()
        {
            GameObject moduleAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModulePath);
            GameObject propItemAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PropItemPath);
            if (moduleAsset == null || propItemAsset == null) return false;

            GameObject module = UnityEngine.Object.Instantiate(moduleAsset);
            GameObject propItem = UnityEngine.Object.Instantiate(propItemAsset);
            try
            {
                bool pages = Check<GestateBabyViewBind>(module) && Check<BabyFamilyViewBind>(module)
                    && Check<BabyCultivateViewBind>(module) && Check<BabyChangedViewBind>(module)
                    && Check<BabyIllusionViewBind>(module);
                bool businessViews = module.GetComponentInChildren<GestateBabyView>(true) != null
                    && module.GetComponentInChildren<BabyCultivateView>(true) != null;
                bool items = Check<BabyCulTaskItemBind>(module) && Check<BabyIlluItemBind>(module)
                    && Check<BabyPropItemBind>(propItem);
                BabyCultivateViewBind cultivate = module.GetComponentInChildren<BabyCultivateViewBind>(true);
                BabyIllusionViewBind illusion = module.GetComponentInChildren<BabyIllusionViewBind>(true);
                bool templates = Has<BabyCulTaskItemBind>(cultivate != null ? cultivate._tpl_BabyCulTaskItem : null)
                    && Has<BabyPropItemBind>(cultivate != null ? cultivate._tpl_BabyPropItem : null)
                    && Has<BabyIlluItemBind>(illusion != null ? illusion._tpl_BabyIlluItem : null)
                    && Has<BabyPropItemBind>(illusion != null ? illusion._tpl_BabyPropItem : null);
                Debug.Log("CLIVERIFY babyui pages=" + pages + " businessViews=" + businessViews + " items=" + items + " templates=" + templates);
                return pages && businessViews && items && templates;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(module);
                UnityEngine.Object.DestroyImmediate(propItem);
            }
        }

        private static bool Check<T>(GameObject root) where T : Component
        {
            T bind = root.GetComponentInChildren<T>(true);
            if (bind == null) return false;
            foreach (FieldInfo field in typeof(T).GetFields(BindFields))
                if (field.GetValue(bind) == null) return false;
            return true;
        }

        private static bool Has<T>(GameObject template) where T : Component
        {
            return template != null && template.GetComponent<T>() != null;
        }
    }
}
