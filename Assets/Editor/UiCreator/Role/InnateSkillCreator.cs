using TMPro;
using Shenxiao.Generated.UI.InnateSkill;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RoleViews = Shenxiao.Module.Core.Role;

namespace Shenxiao.Editor.UiCreator.Role
{
    /// <summary>
    /// 天赋技能页(InnateSkillView)装配器——技能成长线轮3 3b 单。
    ///
    /// 背景:role 模块此前一轮 LayaUI 批量转换已经把 innateSkill 目录下全部 6 个 .scene(InnateSkillView/
    /// InnateListItem/InnateSkillItem/InnateTypeItemRenderer/InnateUpInfoItem/InnateUpCondItem)几何 + Bind
    /// 忠实烤出来了(节点/贴图/文字皆真实,详见 3b 单权威几何侦察报告 r3b_innate_geometry.md),但全部被当
    /// "死重模板"塞在 EquipmentView 的 __Templates 容器里
    /// (EquipmentViewBind._tpl_InnateSkillView 等 6 个字段),从未被真正接入任何 RoleFlow tab——
    /// 这就是为什么"天赋"过去打不开:根本没有独立的顶层内容视图。
    ///
    /// 本 Creator **不重建几何**(baked 数据已对,勿与 MainUIRelive 那种从零 new 节点的 Creator 混淆),
    /// 分两个阶段,重跑安全(修复式幂等,见 Generate 注释):
    ///
    /// 【阶段A:提升装配】(仅当 RoleModule 顶层还没有 InnateSkillView 时跑一次)
    ///   1) 把 InnateSkillView 从 EquipmentView/__Templates 提升为 RoleModule 顶层内容视图
    ///      (与 EquipmentView/SkillInitiativeSubItem/SkillPassiveItem/SkillShowView 同级,供
    ///      <see cref="Shenxiao.Module.Core.Role.RoleFlow"/> 按名字 Find + reparent 接管)。
    ///   2) InnateListItem 挪进 _Scroller1.content(与 _gp_skill 同级)常驻可见(老端 new
    ///      InnateListItem(this._Scroller1));InnateSkillItem 挪进它的 _gp_item 当隐藏克隆模板
    ///      (技能树 item 是运行时数据驱动创建,Creator 只建挂载点 + 一个隐藏模板,坐标见
    ///      <see cref="Shenxiao.Module.Core.Skill.SkillUIConfigs.GetInnateSlots"/>)。
    ///   3) _Scroller2.content 加 HorizontalLayoutGroup(spaceX=20)+ 4 份真实 InnateTypeItemRenderer
    ///      实例(攻击/防守/通用/绝对,类型恒 5/6/7/8 横排;原模板保留为隐藏备份,不重复占位)。
    ///   4) InnateUpInfoItem 挪进 _gp_up_level 常驻;它的 _gp_up_cond 下再挂 InnateUpCondItem 隐藏模板
    ///      (条件行同样运行时数据驱动)。
    ///
    /// 【阶段B:修复】(每次 Generate 都跑,幂等)
    ///   5) 运行时组件补挂:烤入管线当年只挂了 *Bind 基类(业务子类当时还不存在),GetComponentInChildren
    ///      &lt;InnateSkillView&gt; 等会判空。用【脚本替换法】(SerializedObject.m_Script 换成业务子类的
    ///      MonoScript)原地升级——子类继承全部序列化字段,烤入的节点引用一个不丢;组件 fileID 不变,
    ///      EquipmentViewBind._tpl_* 等外部引用也不失效。已是子类 → 跳过(幂等)。
    ///   6) 背景贴图纠正:老端代码路径 role/texture/uijn_001.jpg 是错的(报告已核实),改绑
    ///      resource/game/role/other/uijn_001.jpg。
    ///   7) InnateInfoItem 手工建树入 _gp_info——**这一个例外**:老端该 .scene 没被上一轮流水线捕获过
    ///      (__Templates 里找不到它,也没有生成的 Bind 类),按几何报告数值手工搭(详见 BuildInnateInfoItem),
    ///      字段直接赋值给 <see cref="Shenxiao.Module.Core.Role.InnateInfoItem"/>(手工赋值惯例,
    ///      非常规 Bind 回填路径)。检测到缺组件的残树(历史坏产物)→ 删了重建。
    ///
    /// 废弃节点(_img_002/_Image2/_gp_effect 隐藏,InnateListItem._Image44、InnateUpInfoItem 的
    /// _Image11/_Image2/_Image3/_img_limit 隐藏)**保留不删**:它们已挂在自动生成的 *Bind("由 LayaUI 转换器
    /// 自动生成,不要手改")字段上,删节点会让对应 EnsureBound 报空引用错误且违反该文件的生成契约;保持
    /// inactive、View 不引用即达到"不建/不用"的实际效果。_Image4 经运行时快照核实其实处于 active 且贴了真实
    /// bg_49.png(与几何报告"隐藏废弃"矛盾,判定报告有误——按快照为准保留可见,不做处理)。
    ///
    /// 重跑前提:EquipmentView.HideTemplates() 已同步删掉对这 6 个 _tpl_Innate* 字段的 HideNode 调用
    /// (它们的宿主关系已转移,继续 HideNode 会把 InnateListItem/InnateUpInfoItem 这两个"应常驻可见"的节点
    /// 误一次性隐藏且再也没人重新点亮,详见 EquipmentView.cs 注释)——若被回退,天赋页会打开成空壳,先查那处。
    /// </summary>
    public static class InnateSkillCreator
    {
        private const string RoleModulePath = "Assets/Prefabs/UI/Role/RoleModule.prefab";

        // 老端代码 GetOutsideImageSprite(this._img_bg, "resource/game/role/texture/uijn_001.jpg") 是错误路径
        // (报告核实:Unity 侧真实存在的是 resource/game/role/other/uijn_001.jpg,同目录还有个 uijn_001.png 别混)。
        // 注意 UiCreatorKit.TrySetSprite 的根是 Assets/GameRes/,所以这里必须带 resource/game/ 前缀。
        private const string BgSpriteRelPath = "resource/game/role/other/uijn_001.jpg";

        // Manual-Prefab takeover: keep this repair block disabled unless explicitly re-enabled.
        #if false
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "Role",
                Name = "InnateSkillView(天赋技能页)",
                Note = "把已烤入 RoleModule.prefab/EquipmentView/__Templates 的 InnateSkillView 子树提升为顶层" +
                       "内容视图(与 SkillPassiveItem 同级),补挂运行时组件 + 纠正背景图 + 装配技能树/类型tab/升级面板挂载点 + 补建 InnateInfoItem",
                Order = 90,
                Generate = Generate,
                PrefabPath = RoleModulePath,
            });
        }

        #endif
        [MenuItem("Tools/UiCreator/Role/InnateSkillView Creator")]
        public static void Generate()
        {
            GameObject inst = PrefabUtility.LoadPrefabContents(RoleModulePath);
            if (inst == null)
            {
                Debug.LogError("[UiCreator] 找不到 " + RoleModulePath);
                return;
            }
            Transform root = inst.transform;

            // 修复式幂等:顶层已有 InnateSkillView(已提升过)→ 不重复搬树,但仍走【阶段B】组件补挂+贴图纠正。
            // (历史坑:早期版本这里是"直接跳过整个流程",导致 prefab 卡死在「已提升但缺运行时组件」状态永远修不上。)
            Transform innateViewT = root.Find("InnateSkillView");
            if (innateViewT == null)
            {
                innateViewT = PromoteAndAssemble(root);
                if (innateViewT == null)
                {
                    PrefabUtility.UnloadPrefabContents(inst);
                    return;
                }
            }
            else
            {
                Debug.Log("[UiCreator] RoleModule 顶层已有 InnateSkillView(已提升过)→ 修复模式:只补挂组件/纠正贴图,不重复搬树");
            }

            // 【阶段B:修复】组件补挂 + 贴图纠正(幂等,重跑安全)
            RoleViews.InnateSkillView view = EnsureRuntimeComponents(innateViewT);
            if (view == null)
            {
                Debug.LogError("[UiCreator] InnateSkillView 运行时组件升级失败,停止保存(prefab 未改动)");
                PrefabUtility.UnloadPrefabContents(inst);
                return;
            }
            FixTextures(view);
            EnsureInnateInfoItem(view);
            EnsureInnateInfoViewport(view);

            PrefabUtility.SaveAsPrefabAsset(inst, RoleModulePath);
            PrefabUtility.UnloadPrefabContents(inst);
            GameObject saved = AssetDatabase.LoadAssetAtPath<GameObject>(RoleModulePath);
            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] RoleModule.prefab 已更新: InnateSkillView 顶层内容视图 + 运行时组件齐备" +
                      "(RoleFlow tab index7「天赋」;真机包前记得跑一次 Addressable 自动分组)");
        }

        /// <summary>
        /// 批处理入口(供 -executeMethod 调用):
        ///   Unity.exe -batchmode -projectPath . -executeMethod
        ///     Shenxiao.Editor.UiCreator.Role.InnateSkillCreator.GenerateBatch -logFile Temp/innateskill_creator.log
        /// 成功判据 = 顶层 InnateSkillView 节点存在【且】挂着运行时 InnateSkillView 组件(不是只有 Bind)→ Exit(0);否则 Exit(1)。
        /// </summary>
        public static void GenerateBatch()
        {
            try
            {
                Generate();
                GameObject saved = AssetDatabase.LoadAssetAtPath<GameObject>(RoleModulePath);
                Transform t = saved != null ? saved.transform.Find("InnateSkillView") : null;
                bool ok = t != null && t.GetComponent<RoleViews.InnateSkillView>() != null;
                Debug.Log("[UiCreator] InnateSkillCreator.GenerateBatch " + (ok ? "OK " : "FAILED ") + RoleModulePath);
                EditorApplication.Exit(ok ? 0 : 1);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[UiCreator] InnateSkillCreator.GenerateBatch 异常: " + e);
                EditorApplication.Exit(1);
            }
        }

        // ==================================================================== 阶段A:提升装配(仅首跑)

        /// <summary>把 __Templates 里的 InnateSkillView 子树提升为顶层内容视图并装配各挂载点。
        /// 成功返回提升后的 InnateSkillView Transform,失败(缺模板等)返回 null(已打日志)。</summary>
        private static Transform PromoteAndAssemble(Transform root)
        {
            Transform templates = root.Find("EquipmentView/__Templates");
            Transform innateViewT = templates != null ? templates.Find("InnateSkillView") : null;
            if (templates == null || innateViewT == null)
            {
                Debug.LogError("[UiCreator] 找不到 EquipmentView/__Templates/InnateSkillView" +
                    "(需先有 role 模块 LayaUI 转换基线烤出该子树)");
                return null;
            }

            var viewBind = innateViewT.GetComponent<InnateSkillViewBind>();
            Transform listItemT = templates.Find("InnateListItem");
            Transform skillItemT = templates.Find("InnateSkillItem");
            Transform typeItemT = templates.Find("InnateTypeItemRenderer");
            Transform upInfoT = templates.Find("InnateUpInfoItem");
            Transform upCondT = templates.Find("InnateUpCondItem");
            if (viewBind == null || listItemT == null || skillItemT == null || typeItemT == null
                || upInfoT == null || upCondT == null)
            {
                Debug.LogError("[UiCreator] InnateSkillView/__Templates 下缺必要子模板或 Bind 组件,停止生成");
                return null;
            }

            var listBind = listItemT.GetComponent<InnateListItemBind>();
            var upInfoBind = upInfoT.GetComponent<InnateUpInfoItemBind>();

            // 1) 提升 InnateSkillView 为 RoleModule 顶层内容视图
            innateViewT.SetParent(root, false);
            innateViewT.name = "InnateSkillView";

            // 2) InnateListItem → _Scroller1.content(与 _gp_skill 同级,常驻可见)
            if (viewBind._Scroller1 != null && viewBind._Scroller1.content != null)
            {
                var listRt = (RectTransform)listItemT;
                listRt.SetParent(viewBind._Scroller1.content, false);
                PlaceTopLeft(listRt, 0f, 0f, 720f, 637f); // 637=type5 默认高度(报告 content_size[0])
                listItemT.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogError("[UiCreator] InnateSkillView._Scroller1/content 缺失,InnateListItem 挂载失败");
            }

            if (listBind != null)
            {
                // 默认展示 type5(攻击)分支的连线装饰,其余互斥分支隐藏(运行时按选中 type 切换)
                SetActiveIfExists(listBind._gp_skill5, true);
                SetActiveIfExists(listBind._gp_skill6, false);
                SetActiveIfExists(listBind._gp_skill7, false);
                SetActiveIfExists(listBind._gp_skill8, false);

                // 3) InnateSkillItem → _gp_item 下隐藏模板(唯一真内容挂载点)
                if (listBind._gp_item != null)
                {
                    var skillItemRt = (RectTransform)skillItemT;
                    skillItemRt.SetParent(listBind._gp_item, false);
                    skillItemT.gameObject.SetActive(false);
                }
                else
                {
                    Debug.LogError("[UiCreator] InnateListItem._gp_item 缺失,InnateSkillItem 模板挂载失败");
                }
            }

            // 4) 类型 tab:_Scroller2.content 加 HorizontalLayoutGroup(spaceX=20)+ 4 份真实实例
            if (viewBind._Scroller2 != null && viewBind._Scroller2.content != null)
            {
                RectTransform content2 = viewBind._Scroller2.content;
                HorizontalLayoutGroup hlg = content2.GetComponent<HorizontalLayoutGroup>();
                if (hlg == null) hlg = content2.gameObject.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 20f;
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;

                var typeItemRt = (RectTransform)typeItemT;
                typeItemRt.SetParent(content2, false);
                typeItemT.name = "InnateTypeItemRenderer_Template";
                typeItemT.gameObject.SetActive(false); // 原模板留作隐藏备份(EquipmentViewBind._tpl_InnateTypeItemRenderer 引用不失效),不进可见列表

                for (int i = 0; i < 4; i++)
                {
                    var clone = Object.Instantiate(typeItemT.gameObject, content2);
                    clone.name = "TypeTab_" + i;
                    clone.SetActive(true);
                }
            }
            else
            {
                Debug.LogError("[UiCreator] InnateSkillView._Scroller2/content 缺失,类型 tab 挂载失败");
            }

            // 5) InnateUpInfoItem → _gp_up_level 常驻子节点
            if (viewBind._gp_up_level != null)
            {
                var upRt = (RectTransform)upInfoT;
                upRt.SetParent(viewBind._gp_up_level, false);
                AnchorTopLeft(upRt, 0f, 0f);
                upInfoT.gameObject.SetActive(true);

                // 6) InnateUpCondItem → _gp_up_cond 下隐藏模板
                if (upInfoBind != null && upInfoBind._gp_up_cond != null)
                {
                    var upCondRt = (RectTransform)upCondT;
                    upCondRt.SetParent(upInfoBind._gp_up_cond, false);
                    upCondT.gameObject.SetActive(false);
                }
                else
                {
                    Debug.LogError("[UiCreator] InnateUpInfoItem._gp_up_cond 缺失,InnateUpCondItem 模板挂载失败");
                }
            }
            else
            {
                Debug.LogError("[UiCreator] InnateSkillView._gp_up_level 缺失,InnateUpInfoItem 挂载失败");
            }

            return innateViewT;
        }

        // ==================================================================== 阶段B:修复(每次都跑,幂等)

        /// <summary>
        /// 运行时组件补挂:把烤入管线只挂了 *Bind 基类的节点原地升级成业务子类
        /// (脚本替换法,序列化字段/外部引用全保留;已是子类的直接跳过)。返回根 InnateSkillView(失败 null)。
        /// </summary>
        private static RoleViews.InnateSkillView EnsureRuntimeComponents(Transform innateViewT)
        {
            // 根节点:InnateSkillViewBind → InnateSkillView
            RoleViews.InnateSkillView view =
                UpgradeBind<RoleViews.InnateSkillView, InnateSkillViewBind>(innateViewT.gameObject, "InnateSkillView 根");
            if (view == null) return null;

            // 列表节点 + 技能树隐藏模板
            if (view._Scroller1 != null && view._Scroller1.content != null)
            {
                InnateListItemBind listBind = view._Scroller1.content.GetComponentInChildren<InnateListItemBind>(true);
                if (listBind != null)
                {
                    RoleViews.InnateListItem list =
                        UpgradeBind<RoleViews.InnateListItem, InnateListItemBind>(listBind.gameObject, "InnateListItem");
                    if (list != null && list._gp_item != null)
                    {
                        InnateSkillItemBind itemBind = list._gp_item.GetComponentInChildren<InnateSkillItemBind>(true);
                        if (itemBind != null)
                            UpgradeBind<RoleViews.InnateSkillItem, InnateSkillItemBind>(itemBind.gameObject, "InnateSkillItem 模板");
                        else
                            Debug.LogError("[UiCreator] _gp_item 下找不到 InnateSkillItem 模板(阶段A产物被人为改动?)");
                    }
                }
                else
                {
                    Debug.LogError("[UiCreator] _Scroller1.content 下找不到 InnateListItem(阶段A产物被人为改动?)");
                }
            }

            // 类型 tab:4 份实例 + 1 份隐藏模板备份,全部升级
            if (view._Scroller2 != null && view._Scroller2.content != null)
            {
                InnateTypeItemRendererBind[] tabs = view._Scroller2.content.GetComponentsInChildren<InnateTypeItemRendererBind>(true);
                if (tabs.Length == 0)
                    Debug.LogError("[UiCreator] _Scroller2.content 下找不到任何 InnateTypeItemRenderer 实例(阶段A产物被人为改动?)");
                foreach (InnateTypeItemRendererBind tab in tabs)
                    UpgradeBind<RoleViews.InnateTypeItemRenderer, InnateTypeItemRendererBind>(
                        tab.gameObject, "InnateTypeItemRenderer(" + tab.gameObject.name + ")");
            }

            // 升级面板 + 条件行隐藏模板
            if (view._gp_up_level != null)
            {
                InnateUpInfoItemBind upBind = view._gp_up_level.GetComponentInChildren<InnateUpInfoItemBind>(true);
                if (upBind != null)
                {
                    RoleViews.InnateUpInfoItem up =
                        UpgradeBind<RoleViews.InnateUpInfoItem, InnateUpInfoItemBind>(upBind.gameObject, "InnateUpInfoItem");
                    if (up != null && up._gp_up_cond != null)
                    {
                        InnateUpCondItemBind condBind = up._gp_up_cond.GetComponentInChildren<InnateUpCondItemBind>(true);
                        if (condBind != null)
                            UpgradeBind<RoleViews.InnateUpCondItem, InnateUpCondItemBind>(condBind.gameObject, "InnateUpCondItem 模板");
                        else
                            Debug.LogError("[UiCreator] _gp_up_cond 下找不到 InnateUpCondItem 模板(阶段A产物被人为改动?)");
                    }
                }
                else
                {
                    Debug.LogError("[UiCreator] _gp_up_level 下找不到 InnateUpInfoItem(阶段A产物被人为改动?)");
                }
            }

            return view;
        }

        /// <summary>
        /// 把节点上已挂的 TBind 基类组件原地升级为 TRuntime 业务子类(SerializedObject.m_Script 替换):
        /// 子类继承全部序列化字段 → 烤入的节点引用一个不丢;组件 fileID 不变 → 外部对该组件/GameObject 的引用不失效。
        /// 已是 TRuntime → 直接返回(幂等)。找不到 TBind / MonoScript → 报错返回 null。
        /// </summary>
        private static TRuntime UpgradeBind<TRuntime, TBind>(GameObject go, string label)
            where TBind : MonoBehaviour
            where TRuntime : TBind
        {
            TBind bind = go.GetComponent<TBind>();
            if (bind == null)
            {
                Debug.LogError("[UiCreator] " + label + " 节点缺 " + typeof(TBind).Name + " 组件,无法升级(烤入基线损坏?)");
                return null;
            }
            if (bind is TRuntime ready) return ready; // 已挂运行时子类 → 幂等跳过

            MonoScript script = ScriptOf<TRuntime>();
            if (script == null)
            {
                Debug.LogError("[UiCreator] 找不到 " + typeof(TRuntime).FullName + " 的 MonoScript(类名/文件名不一致?)");
                return null;
            }

            var so = new SerializedObject(bind);
            SerializedProperty scriptProp = so.FindProperty("m_Script");
            scriptProp.objectReferenceValue = script;
            so.ApplyModifiedProperties();

            var upgraded = go.GetComponent<TRuntime>();
            Debug.Log("[UiCreator] " + label + ": " + typeof(TBind).Name + " → " + typeof(TRuntime).Name + " 组件升级完成");
            return upgraded;
        }

        /// <summary>取某 MonoBehaviour 类型的 MonoScript(临时探针法,规避 FindAssets 字符串误配)。</summary>
        private static MonoScript ScriptOf<T>() where T : MonoBehaviour
        {
            var tmp = new GameObject("__script_probe") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                return MonoScript.FromMonoBehaviour(tmp.AddComponent<T>());
            }
            finally
            {
                Object.DestroyImmediate(tmp);
            }
        }

        /// <summary>贴图纠正(幂等,重跑覆盖):主背景 uijn_001.jpg(纠正老端错误路径)。</summary>
        private static void FixTextures(RoleViews.InnateSkillView view)
        {
            if (view._img_bg != null)
                UiCreatorKit.TrySetSprite(view._img_bg, BgSpriteRelPath, UiCreatorKit.Palette.Bg);
        }

        /// <summary>InnateInfoItem 保障:_gp_info 下没有(或只有缺组件的历史坏产物)→ 重建。</summary>
        private static void EnsureInnateInfoItem(RoleViews.InnateSkillView view)
        {
            if (view._gp_info == null)
            {
                Debug.LogError("[UiCreator] InnateSkillView._gp_info 缺失,InnateInfoItem 建树失败");
                return;
            }

            RoleViews.InnateInfoItem existed = view._gp_info.GetComponentInChildren<RoleViews.InnateInfoItem>(true);
            if (existed != null)
            {
                // 已有完好组件 → 幂等跳过重建,但重刷两张静态贴图(防"组件在但贴图落占位色"的历史中间态)
                if (existed.Frame != null)
                    UiCreatorKit.TrySetSprite(existed.Frame, "resource/game/innateSkill/texture/uirw_043c.png", UiCreatorKit.Palette.Panel);
                if (existed.Mask != null)
                    UiCreatorKit.TrySetSprite(existed.Mask, "resource/game/common/texture/ui_circle_mask.png", UiCreatorKit.Palette.Panel);
                return;
            }

            // 历史坏产物:节点在但组件丢(早期版本跑在运行时类未编译进 Unity 之前)→ 删残树重建
            Transform stale = view._gp_info.Find("InnateInfoItem");
            if (stale != null)
            {
                Debug.Log("[UiCreator] _gp_info 下发现缺组件的 InnateInfoItem 残树 → 删除重建");
                Object.DestroyImmediate(stale.gameObject);
            }
            BuildInnateInfoItem(view._gp_info);
        }

        private static void EnsureInnateInfoViewport(RoleViews.InnateSkillView view)
        {
            RoleViews.InnateInfoItem info = view != null && view._gp_info != null
                ? view._gp_info.GetComponentInChildren<RoleViews.InnateInfoItem>(true)
                : null;
            if (info == null) return;

            RectTransform viewport = info.transform.Find("_gp_dec") as RectTransform;
            if (viewport == null) return;

            VerticalLayoutGroup staleLayout = viewport.GetComponent<VerticalLayoutGroup>();
            if (staleLayout != null) Object.DestroyImmediate(staleLayout);
            if (viewport.GetComponent<RectMask2D>() == null)
                viewport.gameObject.AddComponent<RectMask2D>();

            ScrollRect scroll = viewport.GetComponent<ScrollRect>();
            if (scroll == null) scroll = viewport.gameObject.AddComponent<ScrollRect>();

            RectTransform content = viewport.Find("Content") as RectTransform;
            if (content == null) content = UiCreatorKit.NewNode("Content", viewport);
            PlaceTopLeft(content, 0f, 0f, 280f, 0f);

            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            if (layout == null) layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 2f;

            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.scrollSensitivity = 20f;
            info.DecContainer = content;
        }

        /// <summary>手工建 InnateInfoItem 子树(几何数值照几何报告 §1 InnateInfoItem 小节;
        /// 圆形遮罩用真图 ui_circle_mask.png 叠加,非 Unity Mask 组件,对标已烤好的 InnateSkillItem 同类手法)。
        /// 贴图路径注意带 resource/game/ 前缀(UiCreatorKit.TrySetSprite 根是 Assets/GameRes/)。</summary>
        private static void BuildInnateInfoItem(RectTransform gpInfo)
        {
            RectTransform infoRoot = UiCreatorKit.NewNode("InnateInfoItem", gpInfo);
            PlaceTopLeft(infoRoot, 0f, 0f, 355f, 169f);
            var comp = infoRoot.gameObject.AddComponent<RoleViews.InnateInfoItem>();

            RectTransform gpScr = UiCreatorKit.NewNode("_gp_scr", infoRoot);
            PlaceTopLeft(gpScr, 0f, 0f, 355f, 100f);

            Image frame = UiCreatorKit.NewImage("_Image1", gpScr);
            PlaceTopLeft(frame.rectTransform, 7.6f, -2.4f, 95f, 95f);
            UiCreatorKit.TrySetSprite(frame, "resource/game/innateSkill/texture/uirw_043c.png", UiCreatorKit.Palette.Panel);

            Image icon = UiCreatorKit.NewImage("_img_icon", gpScr);
            PlaceTopLeft(icon.rectTransform, 18.9f, 8.2f, 73f, 75f);

            Image mask = UiCreatorKit.NewImage("_img_mask", gpScr);
            PlaceTopLeft(mask.rectTransform, 19f, 8f, 73f, 75f);
            UiCreatorKit.TrySetSprite(mask, "resource/game/common/texture/ui_circle_mask.png", UiCreatorKit.Palette.Panel);
            // ui_circle_mask.png 是供裁剪组件读取的黑色遮罩源，不是可见前景图。
            // 当前详情技能图本身已经带透明圆形边缘；未挂 Unity Mask 时必须关闭该 Image，
            // 否则它作为后绘制的同级节点会把真实技能图完全盖成黑块。
            mask.enabled = false;

            TextMeshProUGUI lbName = UiCreatorKit.NewText("_lb_name", gpScr, "");
            PlaceTopLeft(lbName.rectTransform, 113f, 16f, 220f, 28f);
            lbName.fontSize = 20f;
            lbName.color = HexColor("#d15e00");
            lbName.fontStyle = FontStyles.Bold;
            lbName.alignment = TextAlignmentOptions.Left;

            TextMeshProUGUI lbLv = UiCreatorKit.NewText("_lb_lv", gpScr, "");
            PlaceTopLeft(lbLv.rectTransform, 114f, 55f, 220f, 26f);
            lbLv.fontSize = 20f;
            lbLv.color = HexColor("#663915");
            lbLv.alignment = TextAlignmentOptions.Left;

            RectTransform gpDec = UiCreatorKit.NewNode("_gp_dec", infoRoot);
            PlaceTopLeft(gpDec, 15f, 80f, 310f, 85f);
            VerticalLayoutGroup vlg = gpDec.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 2f;

            comp.Icon = icon;
            comp.Mask = mask;
            comp.Frame = frame;
            comp.NameLabel = lbName;
            comp.LevelLabel = lbLv;
            comp.DecContainer = gpDec;
        }

        // ==================================================================== 小工具

        private static void PlaceTopLeft(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, -y);
        }

        private static void AnchorTopLeft(RectTransform rt, float x, float y)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
        }

        private static void SetActiveIfExists(RectTransform rt, bool active)
        {
            if (rt != null) rt.gameObject.SetActive(active);
        }

        private static Color HexColor(string hex) => ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.white;
    }
}
