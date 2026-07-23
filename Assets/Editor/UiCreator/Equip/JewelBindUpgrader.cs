using Shenxiao.Editor.LayaUI;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.UiCreator.Equip
{
    /// <summary>
    /// JewelModule.prefab 运行时组件升级器(自动循环 轮4 下半/4b 收尾)。
    ///
    /// 背景(核查实录):烤入管线给 JewelModule.prefab 只烤了 3 个窗口(EquipJewelBagView/EquipJewelCraveView/
    /// EquipJewelMasterView,顶层)+ 3 个模板(EquipJewelBagItem/EquipJewelCraveAttItem/EquipJewelCraveSubItem,
    /// 各窗口 __Templates 下),全部只挂基类 *Bind;而**主页签 EquipJewelView 子树(连同 EquipJewelItem 模板)
    /// 根本不在 JewelModule.prefab 里**——按 guid 全仓库搜证,它们被烤进了 CommonModule.prefab/__Templates 当
    /// 死模板(与轮3 InnateSkillView 被烤进 RoleModule/EquipmentView/__Templates 同款管线行为)。
    ///
    /// 两阶段(照 InnateSkillCreator「提升+修复」模式,但回填复用流水线现成工具不重写):
    /// 【阶段A·嫁接,仅首跑】JewelModule 顶层无 EquipJewelView → 从 CommonModule.prefab/__Templates 克隆
    ///   EquipJewelView 子树进 JewelModule 顶层(EquipFlow.ReparentFrom 按顶层名查找,必须顶层),并把
    ///   EquipJewelItem 模板一并克隆到 EquipJewelView/__Templates 下(消费者与模板同 prefab,后续铺格直接用);
    ///   **CommonModule.prefab 只读不改**。
    /// 【阶段B·升级回填,每次】复用 <see cref="LayaBindFiller.FillPrefab"/>(「LayaUI 转换器 > 高级 > 全量回填 Bind」
    ///   的单 prefab 版,EquipModule.prefab 的 EquipStrenView 等此前轮次正是经它升级的):对 prefab 内每个窗口/
    ///   模板做 基类Bind→唯一业务子类 升级(Destroy+Add)+ 按 manifest 节点名重新回填序列化引用。幂等:已是
    ///   子类则跳过升级、引用未漂移则不改。EquipJewelCraveAttItem 无业务子类(4b 判定其属性对比列表依赖未移植
    ///   配置表,刻意未写)→ FindSingleSubclass 找不到子类,保持基类,合法跳过。
    ///
    /// 成功判据(GenerateBatch)= JewelModule.prefab 里 7 个已写运行时子类全部真实在挂:
    /// EquipJewelView/EquipJewelBagView/EquipJewelCraveView/EquipJewelMasterView(窗口)+
    /// EquipJewelBagItem/EquipJewelCraveSubItem/EquipJewelItem(模板)。
    /// </summary>
    public static class JewelBindUpgrader
    {
        private const string JewelModulePath = "Assets/Prefabs/UI/Jewel/JewelModule.prefab";
        private const string CommonModulePath = "Assets/Prefabs/UI/Common/CommonModule.prefab";

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "Equip",
                Name = "JewelModule(骸珀镶嵌 Bind 升级)",
                Note = "从 CommonModule/__Templates 嫁接 EquipJewelView 子树 + EquipJewelItem 模板进 JewelModule 顶层," +
                       "再经 LayaBindFiller.FillPrefab 把 7 个 Bind 升级为运行时业务子类(CommonModule 只读不改)",
                Order = 95,
                Generate = () => Generate(),
                PrefabPath = JewelModulePath,
            });
        }

        /// <summary>两阶段执行(嫁接仅首跑;升级回填幂等可重跑)。成功返回 true。</summary>
        public static bool Generate()
        {
            // 【阶段A】嫁接(仅当 JewelModule 顶层还没有 EquipJewelView)
            GameObject jewelRoot = PrefabUtility.LoadPrefabContents(JewelModulePath);
            if (jewelRoot == null)
            {
                Debug.LogError("[UiCreator] 找不到 " + JewelModulePath);
                return false;
            }
            try
            {
                if (jewelRoot.transform.Find("EquipJewelView") == null)
                {
                    if (!GraftFromCommon(jewelRoot.transform)) return false;
                    PrefabUtility.SaveAsPrefabAsset(jewelRoot, JewelModulePath);
                    Debug.Log("[UiCreator] JewelModule 嫁接完成:EquipJewelView 子树 + EquipJewelItem 模板已入顶层");
                }
                else
                {
                    Debug.Log("[UiCreator] JewelModule 顶层已有 EquipJewelView(已嫁接过)→ 只跑升级回填");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(jewelRoot);
            }

            // 【阶段B】升级回填(复用流水线回填工具,幂等)
            if (!LayaBindFiller.FillPrefab(JewelModulePath))
            {
                Debug.LogError("[UiCreator] LayaBindFiller.FillPrefab(" + JewelModulePath + ") 失败(看 Console 前面的警告)");
                return false;
            }

            return Verify();
        }

        /// <summary>从 CommonModule.prefab(只读)克隆 EquipJewelView 子树到 JewelModule 顶层 +
        /// EquipJewelItem 模板到 EquipJewelView/__Templates。</summary>
        private static bool GraftFromCommon(Transform jewelRoot)
        {
            GameObject commonAsset = AssetDatabase.LoadAssetAtPath<GameObject>(CommonModulePath);
            if (commonAsset == null)
            {
                Debug.LogError("[UiCreator] 找不到 " + CommonModulePath);
                return false;
            }

            Transform srcView = FindDeep(commonAsset.transform, "EquipJewelView");
            if (srcView == null)
            {
                Debug.LogError("[UiCreator] CommonModule.prefab 里没有 EquipJewelView 模板(烤入产物变动?先查 __Templates)");
                return false;
            }

            GameObject viewClone = Object.Instantiate(srcView.gameObject, jewelRoot, false);
            viewClone.name = "EquipJewelView";   // 去掉 "(Clone)" 后缀,EquipFlow.ReparentFrom 按名查找
            viewClone.SetActive(true);           // 与兄弟窗口一致(EquipFlow 打开时统一 SetActive(false) 管理)

            // EquipJewelItem 模板 → EquipJewelView/__Templates(消费者与模板同 prefab;克隆源在 CommonModule
            // 的 __Templates,若烤入产物已把它挂在 EquipJewelView 子树内,克隆即已带上,跳过)
            if (FindDeep(viewClone.transform, "EquipJewelItem") == null)
            {
                Transform srcItem = FindDeep(commonAsset.transform, "EquipJewelItem");
                if (srcItem != null)
                {
                    Transform tplRoot = viewClone.transform.Find("__Templates");
                    if (tplRoot == null)
                    {
                        var tplGo = new GameObject("__Templates", typeof(RectTransform));
                        tplRoot = tplGo.transform;
                        tplRoot.SetParent(viewClone.transform, false);
                    }
                    GameObject itemClone = Object.Instantiate(srcItem.gameObject, tplRoot, false);
                    itemClone.name = "EquipJewelItem";
                    itemClone.SetActive(false);   // 模板惯例:默认隐藏,业务克隆后再激活
                }
                else
                {
                    // 不视为失败:EquipJewelItem 4b 里本就"类已写好、暂无实例被创建"(见 EquipJewelItem.cs 注释)
                    Debug.LogWarning("[UiCreator] CommonModule.prefab 里没找到 EquipJewelItem 模板,跳过(镶嵌槽铺格留待后续)");
                }
            }
            return true;
        }

        /// <summary>验证 7 个运行时子类真实在挂(节点在而组件不是业务子类 → 失败并指名)。</summary>
        private static bool Verify()
        {
            GameObject saved = AssetDatabase.LoadAssetAtPath<GameObject>(JewelModulePath);
            if (saved == null)
            {
                Debug.LogError("[UiCreator] 验证失败:" + JewelModulePath + " 加载不到");
                return false;
            }

            bool ok = true;
            ok &= Check<Shenxiao.Module.Core.Equip.EquipJewelView>(saved, "EquipJewelView");
            ok &= Check<Shenxiao.Module.Core.Equip.EquipJewelBagView>(saved, "EquipJewelBagView");
            ok &= Check<Shenxiao.Module.Core.Equip.EquipJewelCraveView>(saved, "EquipJewelCraveView");
            ok &= Check<Shenxiao.Module.Core.Equip.EquipJewelMasterView>(saved, "EquipJewelMasterView");
            ok &= Check<Shenxiao.Module.Core.Equip.EquipJewelBagItem>(saved, "EquipJewelBagItem");
            ok &= Check<Shenxiao.Module.Core.Equip.EquipJewelCraveSubItem>(saved, "EquipJewelCraveSubItem");
            ok &= Check<Shenxiao.Module.Core.Equip.EquipJewelItem>(saved, "EquipJewelItem");
            Debug.Log("[UiCreator] JewelBindUpgrader 验证 " + (ok ? "OK" : "FAILED") + " " + JewelModulePath);
            return ok;
        }

        private static bool Check<T>(GameObject root, string label) where T : Component
        {
            if (root.GetComponentInChildren<T>(true) != null) return true;
            Debug.LogError("[UiCreator] JewelModule.prefab 缺运行时组件 " + typeof(T).Name + "(" + label + ")");
            return false;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        /// <summary>
        /// 批处理入口(供 -executeMethod 调用):
        ///   Unity.exe -batchmode -projectPath . -executeMethod
        ///     Shenxiao.Editor.UiCreator.Equip.JewelBindUpgrader.GenerateBatch -logFile Temp/jewel_bind_upgrader.log
        /// 成功判据 = 7 个运行时子类全部真实在 JewelModule.prefab 里(含 EquipJewelView)→ Exit(0);否则 Exit(1)。
        /// </summary>
        public static void GenerateBatch()
        {
            try
            {
                bool ok = Generate();
                Debug.Log("[UiCreator] JewelBindUpgrader.GenerateBatch " + (ok ? "OK " : "FAILED ") + JewelModulePath);
                EditorApplication.Exit(ok ? 0 : 1);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[UiCreator] JewelBindUpgrader.GenerateBatch 异常: " + e);
                EditorApplication.Exit(1);
            }
        }
    }
}
