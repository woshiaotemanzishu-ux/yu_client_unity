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
        private const string BaseAwardItemPath = "Assets/Prefabs/UI/Common/BaseAwardItem.prefab";
        private const string FightingShowSmallItemPath = "Assets/Prefabs/UI/Common/FightingShowSmallItem.prefab";
        private const string TeaseViewPath = "Assets/Prefabs/UI/Baby/BabyTeaseView.prefab";
        private const string TeaseSceneKey = "baby/BabyTeaseView";
        private static readonly string[] EquipSceneKeys =
        {
            "baby/BabyEquipFuncView", "baby/BabyEquipView", "baby/BabyEquipTabItem", "baby/BabyEquipSubItem", "baby/BabyEquipIcon",
        };
        private static readonly string[] EquipPrefabPaths =
        {
            "Assets/Prefabs/UI/Baby/BabyEquipFuncView.prefab", "Assets/Prefabs/UI/Baby/BabyEquipView.prefab",
            "Assets/Prefabs/UI/Baby/BabyEquipTabItem.prefab", "Assets/Prefabs/UI/Baby/BabyEquipSubItem.prefab",
            "Assets/Prefabs/UI/Baby/BabyEquipIcon.prefab",
        };
        private static readonly string[] LikeSceneKeys =
        {
            "baby/BabyLikeView", "baby/BabyLikeItem", "baby/BabyBelikeView", "baby/BabyBelikeItem", "baby/BabyLikeReward",
        };
        private static readonly string[] ImprintSceneKeys =
        {
            "baby/BabyImprintView", "baby/BabyAddImprintView", "baby/BabyImprintItem", "baby/BabyAddImprintItem",
        };
        private static readonly string[] ImprintPrefabPaths =
        {
            "Assets/Prefabs/UI/Baby/BabyImprintView.prefab", "Assets/Prefabs/UI/Baby/BabyAddImprintView.prefab",
            "Assets/Prefabs/UI/Baby/BabyImprintItem.prefab", "Assets/Prefabs/UI/Baby/BabyAddImprintItem.prefab",
        };
        private const string ImprintButtonSkinPath = "Assets/GameRes/resource/game/common/texture/com_rect_btn12.png";
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
            // Orphan scenes are converted once via GenerateImprintStatic; incremental passes only restore bindings/templates.
            return Verify() && UpgradeImprintStatic();
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
            if (!Fill(BaseAwardItemPath) || !Fill(LikeViewPath) || !Fill(LikeItemPath) || !Fill(BelikeViewPath)
                || !Fill(BelikeItemPath) || !Fill(LikeRewardPath)) return false;
            if (!EnsureTemplates(LikeViewPath, LikeItemPath, LikeRewardPath)
                || !EnsureTemplates(BelikeViewPath, BelikeItemPath)) return false;
            if (!EnsureNestedTemplate(LikeRewardPath, "__Templates", BaseAwardItemPath)
                || !EnsureNestedTemplate(LikeViewPath, "__Templates/BabyLikeReward/__Templates", BaseAwardItemPath)) return false;
            return VerifyLikeStatic();
        }

        /// <summary>
        /// First pass for the four orphan imprint scenes. This only converts/generates Bind sources;
        /// run <see cref="UpgradeImprintStatic"/> after Unity has compiled those sources.
        /// </summary>
        public static bool GenerateImprintStatic()
        {
            for (int i = 0; i < ImprintSceneKeys.Length; i++) LayaSceneConverter.ConvertSingle(ImprintSceneKeys[i]);
            return true;
        }

        /// <summary>Post-compilation imprint pass: fill Bind references and restore disabled list templates.</summary>
        public static bool UpgradeImprintStatic()
        {
            if (!Fill(BaseAwardItemPath)) return false;
            for (int i = 0; i < ImprintPrefabPaths.Length; i++) if (!Fill(ImprintPrefabPaths[i])) return false;
            if (!EnsureTemplates(ImprintPrefabPaths[0], ImprintPrefabPaths[2])
                || !EnsureTemplates(ImprintPrefabPaths[1], ImprintPrefabPaths[3])
                || !EnsureNestedTemplate(ImprintPrefabPaths[2], "__Templates", BaseAwardItemPath)
                || !EnsureNestedTemplate(ImprintPrefabPaths[3], "__Templates", BaseAwardItemPath)) return false;
            return VerifyImprintStatic();
        }

        /// <summary>Read-only acceptance for orphan imprint prefabs; no business component or button is required here.</summary>
        public static bool VerifyImprintStatic()
        {
            bool ok = CheckGeneratedBindByName(ImprintPrefabPaths[0], "BabyImprintView", "BabyImprintViewBind");
            ok &= CheckNamedNodes(ImprintPrefabPaths[0], "itemScroller", "Content1", "_Scroller1", "Content", "skillLb",
                "stageGp", "_Scroller2", "Content11", "impBtn", "successLb", "impRed", "activeImg", "targetGp", "effectGp", "failImg");
            ok &= CheckGeneratedBindByName(ImprintPrefabPaths[1], "BabyAddImprintView", "BabyAddImprintViewBind");
            ok &= CheckNamedNodes(ImprintPrefabPaths[1], "_Scroller1", "Content", "nothingLb");
            ok &= CheckGeneratedBindByName(ImprintPrefabPaths[2], "BabyImprintItem", "BabyImprintItemBind");
            ok &= CheckNamedNodes(ImprintPrefabPaths[2], "itemGp", "addImg", "numLb");
            ok &= CheckGeneratedBindByName(ImprintPrefabPaths[3], "BabyAddImprintItem", "BabyAddImprintItemBind");
            ok &= CheckNamedNodes(ImprintPrefabPaths[3], "clickBg", "itemGp", "nameLb", "probabilityLb");
            ok &= CheckTemplatePath(ImprintPrefabPaths[0], "__Templates/BabyImprintItem");
            ok &= CheckTemplatePath(ImprintPrefabPaths[1], "__Templates/BabyAddImprintItem");
            ok &= CheckTemplatePath(ImprintPrefabPaths[2], "__Templates/BaseAwardItem");
            ok &= CheckTemplatePath(ImprintPrefabPaths[3], "__Templates/BaseAwardItem");
            if (AssetDatabase.LoadAssetAtPath<Sprite>(ImprintButtonSkinPath) == null)
            {
                Debug.LogError("[UiCreator] Baby imprint button skin missing " + ImprintButtonSkinPath);
                ok = false;
            }
            Debug.Log("[UiCreator] Baby imprint static verification " + (ok ? "OK" : "FAILED"));
            return ok;
        }

        public static bool GenerateTeaseStatic()
        {
            LayaSceneConverter.ConvertSingle(TeaseSceneKey);
            return true;
        }

        public static bool UpgradeTeaseStatic()
        {
            return Fill(TeaseViewPath) && VerifyTeaseStatic();
        }

        public static bool VerifyTeaseStatic()
        {
            bool ok = CheckGeneratedBindByName(TeaseViewPath, "BabyTeaseView", "BabyTeaseViewBind");
            ok &= CheckNamedNodes(TeaseViewPath, "modelGp", "teaseEffectGp", "closeBtn", "teaseBtn", "nameLb", "sayLb");
            Debug.Log("[UiCreator] Baby tease static verification " + (ok ? "OK" : "FAILED"));
            return ok;
        }

        public static bool GenerateEquipStatic()
        {
            for (int i = 0; i < EquipSceneKeys.Length; i++) LayaSceneConverter.ConvertSingle(EquipSceneKeys[i]);
            return true;
        }

        public static bool UpgradeEquipStatic()
        {
            for (int i = 0; i < EquipPrefabPaths.Length; i++) if (!Fill(EquipPrefabPaths[i])) return false;
            if (!EnsureNestedTemplate(EquipPrefabPaths[4], "__Templates", BaseAwardItemPath)
                || !EnsureNestedTemplate(EquipPrefabPaths[3], "__Templates", BaseAwardItemPath)
                || !EnsureNestedTemplate(EquipPrefabPaths[1], "__Templates", EquipPrefabPaths[4])
                || !EnsureNestedTemplate(EquipPrefabPaths[1], "__Templates", EquipPrefabPaths[3])
                || !EnsureNestedTemplate(EquipPrefabPaths[1], "__Templates", FightingShowSmallItemPath)
                || !EnsureNestedTemplate(EquipPrefabPaths[0], "__Templates", EquipPrefabPaths[1])) return false;
            return VerifyEquipStatic();
        }

        public static bool VerifyEquipStatic()
        {
            bool ok = CheckGeneratedBindByName(EquipPrefabPaths[0], "BabyEquipFuncView", "BabyEquipFuncViewBind");
            ok &= CheckNamedNodes(EquipPrefabPaths[0], "viewGp", "TabList", "closeBtn");
            ok &= CheckGeneratedBindByName(EquipPrefabPaths[1], "BabyEquipView", "BabyEquipViewBind");
            ok &= CheckNamedNodes(EquipPrefabPaths[1], "modelGp", "fight", "forgeBtn", "imprintBtn", "_Scroller1", "Content");
            ok &= CheckGeneratedBindByName(EquipPrefabPaths[2], "BabyEquipTabItem", "BabyEquipTabItemBind");
            ok &= CheckNamedNodes(EquipPrefabPaths[2], "labelDisplay", "reddot");
            ok &= CheckGeneratedBindByName(EquipPrefabPaths[3], "BabyEquipSubItem", "BabyEquipSubItemBind");
            ok &= CheckNamedNodes(EquipPrefabPaths[3], "selectImg", "equipGp", "redImg");
            ok &= CheckGeneratedBindByName(EquipPrefabPaths[4], "BabyEquipIcon", "BabyEquipIconBind");
            ok &= CheckNamedNodes(EquipPrefabPaths[4], "itemGp", "defaultImg", "addImg", "effectGp");
            ok &= CheckBusinessView<BabyEquipIcon>(AssetDatabase.LoadAssetAtPath<GameObject>(EquipPrefabPaths[4]), "BabyEquipIcon");
            ok &= CheckBusinessView<BabyEquipSubItem>(AssetDatabase.LoadAssetAtPath<GameObject>(EquipPrefabPaths[3]), "BabyEquipSubItem");
            ok &= CheckBusinessView<BabyEquipView>(AssetDatabase.LoadAssetAtPath<GameObject>(EquipPrefabPaths[1]), "BabyEquipView");
            ok &= CheckBusinessView<BabyEquipFuncView>(AssetDatabase.LoadAssetAtPath<GameObject>(EquipPrefabPaths[0]), "BabyEquipFuncView");
            GameObject icon = AssetDatabase.LoadAssetAtPath<GameObject>(EquipPrefabPaths[4]);
            GameObject subItem = AssetDatabase.LoadAssetAtPath<GameObject>(EquipPrefabPaths[3]);
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(EquipPrefabPaths[1]);
            GameObject func = AssetDatabase.LoadAssetAtPath<GameObject>(EquipPrefabPaths[0]);
            ok &= CheckTemplate<Shenxiao.Module.Core.Common.BaseAwardItem>(icon != null ? icon.transform.Find("__Templates/BaseAwardItem")?.gameObject : null, "BabyEquipIcon.BaseAwardItem");
            ok &= CheckTemplate<Shenxiao.Module.Core.Common.BaseAwardItem>(subItem != null ? subItem.transform.Find("__Templates/BaseAwardItem")?.gameObject : null, "BabyEquipSubItem.BaseAwardItem");
            ok &= CheckTemplate<BabyEquipIcon>(view != null ? view.transform.Find("__Templates/BabyEquipIcon")?.gameObject : null, "BabyEquipView.BabyEquipIcon");
            ok &= CheckTemplate<BabyEquipSubItem>(view != null ? view.transform.Find("__Templates/BabyEquipSubItem")?.gameObject : null, "BabyEquipView.BabyEquipSubItem");
            ok &= CheckTemplate<Shenxiao.Module.Core.Common.BaseAwardItem>(view != null ? view.transform.Find("__Templates/BabyEquipSubItem/__Templates/BaseAwardItem")?.gameObject : null, "BabyEquipView.BabyEquipSubItem.BaseAwardItem");
            ok &= CheckTemplate<BabyEquipView>(func != null ? func.transform.Find("__Templates/BabyEquipView")?.gameObject : null, "BabyEquipFuncView.BabyEquipView");
            ok &= CheckTemplate<Shenxiao.Module.Core.Common.FightingShowSmallItem>(view != null ? view.transform.Find("__Templates/FightingShowSmallItem")?.gameObject : null, "BabyEquipView.FightingShowSmallItem");
            ok &= CheckBusinessView<Shenxiao.Module.Core.Common.FightingShowSmallItem>(view, "BabyEquipView.FightingShowSmallItem");
            Debug.Log("[UiCreator] Baby equip static verification " + (ok ? "OK" : "FAILED"));
            return ok;
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

        /// <summary>CLI first pass: converts the four imprint prefabs and emits Bind sources, then exits for compilation.</summary>
        public static void GenerateImprintStaticBatch()
        {
            try
            {
                bool ok = GenerateImprintStatic();
                Debug.Log("[UiCreator] BabyBindUpgrader.GenerateImprintStaticBatch " + (ok ? "OK (compile then run UpgradeImprintStaticBatch)" : "FAILED"));
                EditorApplication.Exit(ok ? 0 : 1);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[UiCreator] BabyBindUpgrader.GenerateImprintStaticBatch exception: " + e);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>CLI post-compilation pass: fills and verifies the four imprint prefabs.</summary>
        public static void UpgradeImprintStaticBatch()
        {
            try
            {
                bool ok = UpgradeImprintStatic();
                Debug.Log("[UiCreator] BabyBindUpgrader.UpgradeImprintStaticBatch " + (ok ? "OK" : "FAILED"));
                EditorApplication.Exit(ok ? 0 : 1);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[UiCreator] BabyBindUpgrader.UpgradeImprintStaticBatch exception: " + e);
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
            ok &= CheckBusinessView<BabyBelikeView>(AssetDatabase.LoadAssetAtPath<GameObject>(BelikeViewPath), "BabyBelikeView");
            ok &= CheckBusinessView<BabyBelikeItem>(AssetDatabase.LoadAssetAtPath<GameObject>(BelikeItemPath), "BabyBelikeItem");
            ok &= CheckBusinessView<BabyLikeReward>(AssetDatabase.LoadAssetAtPath<GameObject>(LikeRewardPath), "BabyLikeReward");
            ok &= CheckBusinessView<Shenxiao.Module.Core.Common.BaseAwardItem>(AssetDatabase.LoadAssetAtPath<GameObject>(LikeRewardPath), "BabyLikeReward.BaseAwardItem");
            GameObject likeView = AssetDatabase.LoadAssetAtPath<GameObject>(LikeViewPath);
            Transform likeItemTemplate = likeView != null ? likeView.transform.Find("__Templates/BabyLikeItem") : null;
            ok &= CheckTemplate<BabyLikeItem>(likeItemTemplate != null ? likeItemTemplate.gameObject : null, "BabyLikeView.BabyLikeItem template");
            Transform likeRewardTemplate = likeView != null ? likeView.transform.Find("__Templates/BabyLikeReward") : null;
            ok &= CheckTemplate<BabyLikeReward>(likeRewardTemplate != null ? likeRewardTemplate.gameObject : null, "BabyLikeView.BabyLikeReward template");
            GameObject rewardSource = AssetDatabase.LoadAssetAtPath<GameObject>(LikeRewardPath);
            Transform awardTemplate = rewardSource != null ? rewardSource.transform.Find("__Templates/BaseAwardItem") : null;
            ok &= CheckTemplate<Shenxiao.Module.Core.Common.BaseAwardItem>(awardTemplate != null ? awardTemplate.gameObject : null, "BabyLikeReward.BaseAwardItem template");
            Transform nestedAwardTemplate = likeView != null ? likeView.transform.Find("__Templates/BabyLikeReward/__Templates/BaseAwardItem") : null;
            ok &= CheckTemplate<Shenxiao.Module.Core.Common.BaseAwardItem>(nestedAwardTemplate != null ? nestedAwardTemplate.gameObject : null, "BabyLikeView.BabyLikeReward.BaseAwardItem template");
            GameObject belikeView = AssetDatabase.LoadAssetAtPath<GameObject>(BelikeViewPath);
            Transform belikeItemTemplate = belikeView != null ? belikeView.transform.Find("__Templates/BabyBelikeItem") : null;
            ok &= CheckTemplate<BabyBelikeItem>(belikeItemTemplate != null ? belikeItemTemplate.gameObject : null, "BabyBelikeView.BabyBelikeItem template");
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
                        GameObject clone = PrefabUtility.InstantiatePrefab(source, templates) as GameObject;
                        if (clone == null) return false;
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

        private static bool EnsureNestedTemplate(string prefabPath, string parentPath, string sourcePath)
        {
            GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefab == null) return false;
            try
            {
                GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
                if (source == null) return false;
                bool changed = false;
                Transform parent = prefab.transform;
                string[] parts = parentPath.Split('/');
                for (int i = 0; i < parts.Length; i++)
                {
                    Transform child = parent.Find(parts[i]);
                    if (child == null)
                    {
                        child = new GameObject(parts[i], typeof(RectTransform)).transform;
                        child.SetParent(parent, false);
                        changed = true;
                    }
                    if (parts[i] == "__Templates" && child.gameObject.activeSelf)
                    {
                        child.gameObject.SetActive(false);
                        changed = true;
                    }
                    parent = child;
                }
                Transform existing = parent.Find(source.name);
                if (existing == null)
                {
                    GameObject clone = PrefabUtility.InstantiatePrefab(source, parent) as GameObject;
                    if (clone == null) return false;
                    clone.name = source.name;
                    clone.SetActive(false);
                    changed = true;
                }
                else if (existing.gameObject.activeSelf)
                {
                    existing.gameObject.SetActive(false);
                    changed = true;
                }
                if (changed) PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
                return true;
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
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

        private static bool CheckGeneratedBindByName(string prefabPath, string rootName, string bindName)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null || prefab.transform.name != rootName)
            {
                Debug.LogError("[UiCreator] Baby static prefab/root missing " + prefabPath);
                return false;
            }
            Component bind = prefab.GetComponent(bindName);
            if (bind == null)
            {
                Debug.LogError("[UiCreator] Baby generated Bind missing " + bindName + "(" + prefabPath + ")");
                return false;
            }
            bool ok = true;
            foreach (FieldInfo field in bind.GetType().GetFields(BindFields))
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
