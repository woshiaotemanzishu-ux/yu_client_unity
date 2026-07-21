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
        private const string LikeViewPath = "Assets/Prefabs/UI/Baby/BabyLikeView.prefab";
        private const string LikeItemPath = "Assets/Prefabs/UI/Baby/BabyLikeItem.prefab";
        private const string BelikeViewPath = "Assets/Prefabs/UI/Baby/BabyBelikeView.prefab";
        private const string BelikeItemPath = "Assets/Prefabs/UI/Baby/BabyBelikeItem.prefab";
        private const string LikeRewardPath = "Assets/Prefabs/UI/Baby/BabyLikeReward.prefab";
        private static readonly string[] LikeSceneKeys =
        {
            "baby/BabyLikeView", "baby/BabyLikeItem", "baby/BabyBelikeView", "baby/BabyBelikeItem", "baby/BabyLikeReward",
        };
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

        /// <summary>
        /// Converts the five orphan baby-like scenes, fills their generated Bind components, and restores
        /// the local list-template relationships.  Kept separate from <see cref="Generate"/> because these
        /// scenes are intentionally not part of BabyModule's combined conversion manifest.
        /// </summary>
        public static bool GenerateLikeStatic()
        {
            for (int i = 0; i < LikeSceneKeys.Length; i++) LayaSceneConverter.ConvertSingle(LikeSceneKeys[i]);
            return UpgradeLikeStatic();
        }

        /// <summary>
        /// Reuses the already converted prefabs when only business subclasses or template bindings changed.
        /// This avoids regenerating every local file ID on each incremental migration round.
        /// </summary>
        public static bool UpgradeLikeStatic()
        {
            if (!Fill(LikeViewPath) || !Fill(LikeItemPath) || !Fill(BelikeViewPath)
                || !Fill(BelikeItemPath) || !Fill(LikeRewardPath)) return false;
            if (!EnsureTemplates(LikeViewPath, LikeItemPath, LikeRewardPath)
                || !EnsureTemplates(BelikeViewPath, BelikeItemPath)) return false;
            return VerifyLikeStatic();
        }

        /// <summary>Unity CLI entry point for the orphan baby-like static conversion.</summary>
        public static void GenerateLikeStaticBatch()
        {
            try
            {
                bool ok = GenerateLikeStatic();
                Debug.Log("[UiCreator] BabyBindUpgrader.GenerateLikeStaticBatch " + (ok ? "OK" : "FAILED"));
                EditorApplication.Exit(ok ? 0 : 1);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[UiCreator] BabyBindUpgrader.GenerateLikeStaticBatch exception: " + e);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Read-only acceptance for the five orphan prefabs and their local disabled templates.</summary>
        public static bool VerifyLikeStatic()
        {
            bool ok = true;
            ok &= CheckGeneratedBind<BabyLikeViewBind>(LikeViewPath, "BabyLikeView");
            ok &= CheckGeneratedBind<BabyLikeItemBind>(LikeItemPath, "BabyLikeItem");
            ok &= CheckGeneratedBind<BabyBelikeViewBind>(BelikeViewPath, "BabyBelikeView");
            ok &= CheckGeneratedBind<BabyBelikeItemBind>(BelikeItemPath, "BabyBelikeItem");
            ok &= CheckGeneratedBind<BabyLikeRewardBind>(LikeRewardPath, "BabyLikeReward");
            ok &= CheckNamedNodes(LikeViewPath, "bg1", "closeBtn", "_Scroller1", "Content1",
                "rewardScroller", "Content", "belikeBtn", "leftBtn", "rightBtn", "myRank", "mylike",
                "tipsLb", "noOneLb", "likeRed");
            ok &= CheckNamedNodes(LikeItemPath, "rankImg", "rankLb", "nameLb", "fightLb", "numLb");
            ok &= CheckNamedNodes(BelikeViewPath, "bg1", "closeBtn", "_Scroller1", "Content", "noOneLb");
            ok &= CheckNamedNodes(BelikeItemPath, "likeBtn", "lb");
            ok &= CheckNamedNodes(LikeRewardPath, "lb", "_Scroller1", "Content");
            ok &= CheckTemplatePath(LikeViewPath, "__Templates/BabyLikeItem");
            ok &= CheckTemplatePath(LikeViewPath, "__Templates/BabyLikeReward");
            ok &= CheckTemplatePath(BelikeViewPath, "__Templates/BabyBelikeItem");
            ok &= CheckBusinessView<BabyLikeView>(AssetDatabase.LoadAssetAtPath<GameObject>(LikeViewPath), "BabyLikeView");
            ok &= CheckBusinessView<BabyLikeItem>(AssetDatabase.LoadAssetAtPath<GameObject>(LikeItemPath), "BabyLikeItem");
            GameObject likeView = AssetDatabase.LoadAssetAtPath<GameObject>(LikeViewPath);
            Transform likeItemTemplate = likeView != null ? likeView.transform.Find("__Templates/BabyLikeItem") : null;
            ok &= CheckTemplate<BabyLikeItem>(likeItemTemplate != null ? likeItemTemplate.gameObject : null, "BabyLikeView.BabyLikeItem template");
            Debug.Log("[UiCreator] Baby like static verification " + (ok ? "OK" : "FAILED"));
            return ok;
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
            ok &= CheckBusinessView<BabyPropItem>(propItem, "BabyPropItem(独立源 prefab)");
            ok &= CheckBind<BabyCulTaskItemBind>(module, "BabyCulTaskItem(__Templates)");
            ok &= CheckBind<BabyIlluItemBind>(module, "BabyIlluItem(__Templates)");
            ok &= CheckBusinessView<BabyIlluItem>(module, "BabyIlluItem(__Templates)");
            ok &= CheckBind<BabyPropItemBind>(propItem, "BabyPropItem(独立源 prefab)");

            BabyCultivateViewBind cultivate = module.GetComponentInChildren<BabyCultivateViewBind>(true);
            BabyIllusionViewBind illusion = module.GetComponentInChildren<BabyIllusionViewBind>(true);
            ok &= CheckTemplate<BabyCulTaskItemBind>(cultivate != null ? cultivate._tpl_BabyCulTaskItem : null,
                "BabyCultivateView._tpl_BabyCulTaskItem");
            ok &= CheckTemplate<BabyPropItemBind>(cultivate != null ? cultivate._tpl_BabyPropItem : null,
                "BabyCultivateView._tpl_BabyPropItem");
            ok &= CheckTemplate<BabyPropItem>(cultivate != null ? cultivate._tpl_BabyPropItem : null,
                "BabyCultivateView._tpl_BabyPropItem business");
            ok &= CheckTemplate<BabyIlluItemBind>(illusion != null ? illusion._tpl_BabyIlluItem : null,
                "BabyIllusionView._tpl_BabyIlluItem");
            ok &= CheckTemplate<BabyIlluItem>(illusion != null ? illusion._tpl_BabyIlluItem : null,
                "BabyIllusionView._tpl_BabyIlluItem business");
            ok &= CheckTemplate<BabyPropItemBind>(illusion != null ? illusion._tpl_BabyPropItem : null,
                "BabyIllusionView._tpl_BabyPropItem");
            ok &= CheckTemplate<BabyPropItem>(illusion != null ? illusion._tpl_BabyPropItem : null,
                "BabyIllusionView._tpl_BabyPropItem business");
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

        private static bool EnsureTemplates(string viewPath, params string[] itemPaths)
        {
            GameObject view = PrefabUtility.LoadPrefabContents(viewPath);
            if (view == null)
            {
                Debug.LogError("[UiCreator] Baby static prefab missing " + viewPath);
                return false;
            }
            try
            {
                Transform templates = view.transform.Find("__Templates");
                if (templates == null)
                {
                    templates = new GameObject("__Templates", typeof(RectTransform)).transform;
                    templates.SetParent(view.transform, false);
                }
                templates.gameObject.SetActive(false);
                for (int i = 0; i < itemPaths.Length; i++)
                {
                    GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(itemPaths[i]);
                    if (source == null)
                    {
                        Debug.LogError("[UiCreator] Baby static template source missing " + itemPaths[i]);
                        return false;
                    }
                    Transform existing = templates.Find(source.name);
                    if (existing == null)
                    {
                        GameObject clone = Object.Instantiate(source, templates, false);
                        clone.name = source.name;
                        clone.SetActive(false);
                    }
                    else existing.gameObject.SetActive(false);
                }
                PrefabUtility.SaveAsPrefabAsset(view, viewPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(view);
            }
        }

        private static bool CheckGeneratedBind<T>(string prefabPath, string rootName) where T : Component
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null || prefab.transform.name != rootName)
            {
                Debug.LogError("[UiCreator] Baby static prefab/root missing " + prefabPath);
                return false;
            }
            T bind = prefab.GetComponent<T>();
            if (bind == null)
            {
                Debug.LogError("[UiCreator] Baby generated Bind missing " + typeof(T).Name + "(" + prefabPath + ")");
                return false;
            }
            bool ok = true;
            foreach (FieldInfo field in typeof(T).GetFields(BindFields))
            {
                if (field.GetValue(bind) != null) continue;
                Debug.LogError("[UiCreator] Baby generated Bind field missing " + bind.GetType().Name + "." + field.Name);
                ok = false;
            }
            return ok;
        }

        private static bool CheckNamedNodes(string prefabPath, params string[] names)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return false;
            Transform[] nodes = prefab.GetComponentsInChildren<Transform>(true);
            bool ok = true;
            for (int i = 0; i < names.Length; i++)
            {
                bool found = false;
                for (int j = 0; j < nodes.Length; j++)
                {
                    if (nodes[j].name != names[i]) continue;
                    found = true;
                    break;
                }
                if (found) continue;
                Debug.LogError("[UiCreator] Baby static node missing " + prefabPath + "/" + names[i]);
                ok = false;
            }
            return ok;
        }

        private static bool CheckTemplatePath(string viewPath, string path)
        {
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(viewPath);
            Transform template = view != null ? view.transform.Find(path) : null;
            if (template != null && !template.gameObject.activeSelf) return true;
            Debug.LogError("[UiCreator] Baby static template missing or active " + viewPath + "/" + path);
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
