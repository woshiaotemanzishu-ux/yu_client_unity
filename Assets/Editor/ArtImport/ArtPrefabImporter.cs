using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Shenxiao.Common.UI3D;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;

namespace Shenxiao.EditorTools.ArtImport
{
    /// <summary>
    /// 外部成品模型导入(美术工程 → 主工程,保 GUID 直搬)。
    ///
    /// 用法:资产管理选中目标条目后选择 Art 项目内的模型目录;底层仍保留任意外部 prefab 导入能力
    /// → [扫描] 看依赖闭包与新增/替换分类 → [导入]。
    ///
    /// 原理:两边引擎/URP/Timeline 版本一致(交付规范约定),把 prefab 的 GUID 依赖闭包连同
    /// .meta 原样拷入主工程,全部引用自动接上——不像 Laya 转换需要重建任何东西。
    /// 附带四件事:
    ///   ① 台账:每次导入记录哪些文件新增/替换/跳过,写 ArtImportLedger.json,可追溯;
    ///   ② 渲染档案:prefab 根挂 ArtModelRenderProfile,UIModelStage 上台时按档案给展示相机
    ///      切独立 ArtFx renderer + 强制 Depth/Opaque Texture(PandaShader 软粒子/扭曲依赖,
    ///      Mobile RP 默认关)——老模型不带档案,渲染路径零改动;
    ///   ③ 贴图压缩:平台格式覆盖(ASTC6x6/BC7)+ 尺寸封顶,超大无压缩 TGA 可无损转 PNG;
    ///   ④ 去重:同内容贴图只留一份,其余 GUID 重定向(美术工程里 VFXtexturePack 每个角色目录
    ///      重复一份,不去重会 ×N 膨胀)。
    /// </summary>
    public sealed class ArtPrefabImporter : EditorWindow
    {
        private const string LedgerPath = "Assets/Editor/ArtImport/ArtImportLedger.json";
        private const string RendererAssetPath = "Assets/Settings/ArtFx_Renderer.asset";
        private const string RendererTemplatePath = "Assets/Settings/Mobile_Renderer.asset";
        private const string RoleAssemblyProfileFile = "role_assembly_profile.json";

        private static readonly Regex GuidRegex = new Regex(@"guid:\s*([0-9a-f]{32})", RegexOptions.Compiled);

        /// <summary>会内嵌 GUID 引用的文本资产(闭包解析 + 去重重写都以此判定)。</summary>
        private static readonly HashSet<string> TextExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".prefab", ".mat", ".playable", ".anim", ".controller", ".asset", ".unity", ".json",
            ".shadergraph", ".shadersubgraph",
        };

        /// <summary>代码/程序集不搬——美术工程的脚本进主工程只会撞类名。</summary>
        private static readonly HashSet<string> ForbiddenExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".cs", ".asmdef", ".asmref", ".dll",
        };

        private enum RenderMode
        {
            Dedicated,     // 独立 ArtFx renderer + 强制 Depth/Opaque(推荐)
            SharedDefault, // 共用默认 renderer,仅强制 Depth/Opaque
            None,          // 不挂渲染档案
        }

        private enum FileAction { Add, Replace, SkipSame, DedupDropped }

        private sealed class PlannedFile
        {
            public string Guid;
            public string Src;        // 源绝对路径
            public string Dst;        // 主工程内 Assets/ 相对路径
            public FileAction Action;
            public string DedupToGuid;
            public long Bytes;
        }

        private sealed class Plan
        {
            public string SourceAssetsRoot;
            public readonly List<PlannedFile> Files = new List<PlannedFile>();
            public readonly List<string> RootPrefabDsts = new List<string>();
            public readonly List<string> RelocatedAssetPaths = new List<string>();
            public readonly List<string> Warnings = new List<string>();
            public readonly Dictionary<string, string> GuidRemap = new Dictionary<string, string>();
        }

        // ---------------- 台账(JsonUtility 可序列化) ----------------

        [Serializable]
        private class LedgerFile
        {
            public string guid;
            public string src;
            public string dst;
            public string action;
            public long bytes;
        }

        [Serializable]
        private class LedgerRun
        {
            public string time;
            public string sourceProject;
            public string renderMode;
            public int rendererIndex;
            public int added;
            public int replaced;
            public int skippedSame;
            public int deduped;
            public List<LedgerFile> files = new List<LedgerFile>();
            public List<string> notes = new List<string>();
        }

        [Serializable]
        private class Ledger
        {
            public List<LedgerRun> runs = new List<LedgerRun>();
        }

        [Serializable]
        private sealed class RoleAssemblyProfileData
        {
            public int version = 1;
            public string skeletonTemplate = "";
            public string canonicalAction = "idle";
            public float attachmentSpaceScale = 1f;
        }

        // ---------------- 泛化部件导入(2026-07-11:创角整模已退役改视频,导入线泛化到任意部件) ----------------
        //
        // 交付规范(与美术约定):{美术工程}/Assets/{TopDir}/{module}_{id}/{任意前缀}@{动作}.prefab
        //   Role/role_1213、Head/head_1213、Weapon/weapon_1200…;动作名取 prefab 名 '@' 后缀(小写)。
        // 目标:Assets/GameRes/object/{module}/{module}_{id}/{id}@{动作}.prefab(根 prefab 统一改名,
        //   前缀随意→键位确定;文件改名不动 .meta 内容,GUID/引用不受影响)。

        private const string QuickSourcePrefsKey = "ArtImport.SourceProject";
        private const string DEFAULT_ART_PROJECT_ROOT = "E:/GitProject/ArtsProject";

        /// <summary>
        /// 旧美术工程副本。2026-07 美术模板迁到 E:/GitProject/ArtsProject 并建了 git,旧路径不再维护。
        /// 2026-07-18 实锤事故:美术在新工程调好握点保存,主工程一键导入却拿的是这份 07-17 旧副本
        /// (rhand 仍是 -0.14/0.03/0.02),表现为"美术改了、导进来没生效"。见下方自动迁移。
        /// </summary>
        private const string LEGACY_ART_PROJECT_ROOT = "E:/Project/ArtsProject";

        private string _quickSource;

        /// <summary>部件模块 ↔ 美术工程顶层目录。加新部件类型(坐骑/怪物…)在这里列装。</summary>
        private static readonly Dictionary<string, string> PartTopDirs = new Dictionary<string, string>
        {
            { "role", "Role" }, { "head", "Head" }, { "weapon", "Weapon" },
            { "wing", "Wing" }, { "back", "Back" },
        };

        /// <summary>角色本体必须带的挂点(精确小写;head_mount 含头骨绑定姿态逆矩阵)。</summary>
        private static readonly string[] RoleMountNodes = { "head_mount", "rhand", "wing", "root" };

        public static bool IsPartModule(string module) => PartTopDirs.ContainsKey(module);

        /// <summary>
        /// 当前美术 Unity 项目根目录，由 EditorPrefs 持久保存。
        /// 若存档仍指向已停止维护的旧副本(LEGACY_ART_PROJECT_ROOT)且新工程可用，一次性自动切换——
        /// 旧副本文件夹还在磁盘上，靠"存在性"判断抓不出来，只能按路径点名。
        /// </summary>
        public static string ArtProjectRoot
        {
            get
            {
                string stored = EditorPrefs.GetString(QuickSourcePrefsKey, DEFAULT_ART_PROJECT_ROOT)
                    .Replace('\\', '/').TrimEnd('/');
                if (string.Equals(stored, LEGACY_ART_PROJECT_ROOT, StringComparison.OrdinalIgnoreCase)
                    && Directory.Exists(DEFAULT_ART_PROJECT_ROOT + "/Assets"))
                {
                    EditorPrefs.SetString(QuickSourcePrefsKey, DEFAULT_ART_PROJECT_ROOT);
                    Debug.LogWarning($"[ArtImport] 美术工程源指向已停维护的旧副本 {LEGACY_ART_PROJECT_ROOT}，" +
                                     $"已自动切到 {DEFAULT_ART_PROJECT_ROOT}；请重新导入一次受影响的部件。");
                    return DEFAULT_ART_PROJECT_ROOT;
                }
                return stored;
            }
        }

        /// <summary>保存美术 Unity 项目根目录；允许用户选到 Assets 目录并自动归一到项目根。</summary>
        public static bool TrySetArtProjectRoot(string projectRoot, out string error)
        {
            string normalized = (projectRoot ?? "").Replace('\\', '/').TrimEnd('/');
            if (string.IsNullOrEmpty(normalized))
            {
                error = "没有选择 Art 项目目录。";
                return false;
            }

            if (string.Equals(Path.GetFileName(normalized), "Assets", StringComparison.OrdinalIgnoreCase))
            {
                string parent = Path.GetDirectoryName(normalized)?.Replace('\\', '/');
                if (!string.IsNullOrEmpty(parent)) normalized = parent.TrimEnd('/');
            }

            if (!Directory.Exists($"{normalized}/Assets") || !Directory.Exists($"{normalized}/ProjectSettings"))
            {
                error = $"{normalized}\n不是 Unity 项目根目录(需要同时包含 Assets 和 ProjectSettings)。";
                return false;
            }

            EditorPrefs.SetString(QuickSourcePrefsKey, normalized);
            error = null;
            return true;
        }

        /// <summary>返回指定部件在美术项目中的目录选择起点。</summary>
        public static string GetPartPickerRoot(string module) => IsPartModule(module)
            ? $"{ArtProjectRoot}/Assets/{PartTopDirs[module]}"
            : $"{ArtProjectRoot}/Assets";

        private static string PartSourceFolder(string module, string folderName) =>
            $"{GetPartPickerRoot(module)}/{folderName}";

        private void OnEnable()
        {
            _quickSource = ArtProjectRoot; // 走带旧副本迁移的取值，别再直读 EditorPrefs
        }

        /// <summary>
        /// 美术工程部件交付状态:未交付 / 未导入 / 可更新 / 已是最新(哈希比对)。
        /// 有几 MB 的 prefab 要算 MD5——调用方自行缓存结果,别放 OnGUI 每帧调。
        /// </summary>
        public static string GetPartArtStatus(string module, string folderName)
        {
            if (!IsPartModule(module)) return "未交付";
            return GetPartArtStatusForFolder(module, PartSourceFolder(module, folderName));
        }

        /// <summary>按用户选择的具体模型目录检查美术交付状态。</summary>
        public static string GetPartArtStatusForFolder(string module, string sourceFolder)
        {
            if (!IsPartModule(module)) return "未交付";
            _md5Cache.Clear(); // 导入会替换工程内文件,别吃陈旧哈希
            if (string.IsNullOrEmpty(sourceFolder)) return "未选择目录";
            string folder = sourceFolder.Replace('\\', '/').TrimEnd('/');
            if (!Directory.Exists(folder)) return "目录不存在";
            bool anyNew = false, anyUpdate = false;
            int count = 0;
            foreach (string prefab in Directory.GetFiles(folder, "*@*.prefab", SearchOption.TopDirectoryOnly))
            {
                count++;
                string abs = prefab.Replace('\\', '/');
                string guid = ReadGuidOfMeta(abs + ".meta");
                string existing = guid != null ? AssetDatabase.GUIDToAssetPath(guid) : null;
                if (string.IsNullOrEmpty(existing)) anyNew = true;
                else if (!(HashEquals(abs, existing) && HashEquals(abs + ".meta", existing + ".meta"))) anyUpdate = true;
            }
            return count == 0 ? "未交付" : anyNew ? "未导入" : anyUpdate ? "可更新" : "已是最新";
        }

        /// <summary>
        /// 角色本体挂点体检:返回缺失的挂点节点名(head_mount/rhand/wing/root,精确小写)。
        /// 缺 head_mount=静态模型空间头部附件无法跟头、缺 rhand=武器挂不上、缺 wing=翅膀挂不上、
        /// 缺 root=技能特效兜底挂错位。
        /// </summary>
        public static string[] MissingRoleMounts(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return RoleMountNodes;
            var names = new HashSet<string>(
                prefab.GetComponentsInChildren<Transform>(true).Select(t => t.name));
            return RoleMountNodes.Where(n => !names.Contains(n)).ToArray();
        }

        /// <summary>
        /// 挂点问题只影响头饰/武器/翅膀/背饰/特效，不影响身体 Timeline 播放。完整检查结果进入导入台账供美术修，
        /// 但不再作为 idle/create3 是否写入替换清单的硬门槛。
        /// </summary>
        private static string[] InspectRoleMountStructure(string path)
        {
            var issues = new List<string>();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return new[] { path + " 无法加载，不能检查挂点" };

            Transform[] all = prefab.GetComponentsInChildren<Transform>(true);
            Transform[] heads = all.Where(t => t.name == "head_mount").ToArray();
            Transform[] hands = all.Where(t => t.name == "rhand").ToArray();
            Transform[] wings = all.Where(t => t.name == "wing").ToArray();
            Transform[] roots = all.Where(t => t.name == "root").ToArray();
            if (heads.Length != 1) issues.Add($"head_mount 数量={heads.Length}，必须为 1");
            if (hands.Length != 1) issues.Add($"rhand 数量={hands.Length}，必须为 1");
            if (wings.Length != 1) issues.Add($"wing 数量={wings.Length}，必须为 1");
            if (roots.Length != 1) issues.Add($"root 数量={roots.Length}，必须为 1");

            if (hands.Length == 1)
            {
                Transform hand = hands[0];
                string[] handHosts = { "Bip001 R Hand", "Bip001_R_Hand", "R Hand", "RightHand" };
                if (hand.parent == null || !handHosts.Any(n =>
                        string.Equals(hand.parent.name, n, StringComparison.OrdinalIgnoreCase)))
                    issues.Add("rhand 不在实际右手骨下");
                if (!Nearly(hand.localScale, Vector3.one, 0.0001f))
                    issues.Add("rhand scale 必须为 1");
            }

            if (wings.Length == 1)
            {
                Transform wing = wings[0];
                string[] wingHosts = { "Bip001 Spine1", "Bip001_Spine1", "Spine1", "UpperChest", "Chest" };
                if (wing.parent == null || !wingHosts.Any(n =>
                        string.Equals(wing.parent.name, n, StringComparison.OrdinalIgnoreCase)))
                    issues.Add("wing 不在 Spine1/胸骨下");
                if (!Nearly(wing.localScale, Vector3.one, 0.0001f))
                    issues.Add("wing scale 必须为 1");
            }

            if (heads.Length == 1)
            {
                Transform head = heads[0];
                // 2026-07-16 与 Art 工程 MountPointPatcher 同步:补偿以蒙皮 bindposes 为事实源。
                if (TryGetHeadBindLocal(prefab.transform, head.parent, out Matrix4x4 expected))
                {
                    Vector3 expScale = expected.lossyScale;
                    if (expected.determinant < 0f) expScale.x = -expScale.x;
                    if ((head.localPosition - expected.GetPosition()).magnitude > 0.005f
                        || Quaternion.Angle(head.localRotation, expected.rotation) > 0.1f
                        || (head.localScale - expScale).magnitude > 0.005f)
                        issues.Add("head_mount 没有绑定姿态逆矩阵补偿(vs bindposes 真值)");
                }
                else
                {
                    Matrix4x4 relative = prefab.transform.worldToLocalMatrix * head.localToWorldMatrix;
                    if (relative.GetPosition().magnitude > 0.0001f
                        || Quaternion.Angle(relative.rotation, Quaternion.identity) > 0.05f
                        || (relative.lossyScale - Vector3.one).magnitude > 0.001f)
                        issues.Add("head_mount 没有绑定姿态逆矩阵补偿");
                }
            }
            return issues.ToArray();
        }

        /// <summary>
        /// 主工程导入后的第二道硬检查。角色本体只把“动作能否实际播放”作为启用门槛；
        /// head_mount/rhand/wing/root 属于部件挂接能力，缺失时继续启用身体动作并在导入台账中明确告警，
        /// 不能因为一把武器暂时挂不上，就让已经可播放的 idle/create3 整体保持未配置状态。
        /// </summary>
        public static string[] ValidateImportedPartStructure(string module, IEnumerable<string> prefabPaths)
        {
            var issues = new List<string>();
            foreach (string path in prefabPaths ?? Enumerable.Empty<string>())
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    issues.Add(path + " 无法加载");
                    continue;
                }
                try
                {
                    switch (module)
                    {
                        case "role": ValidateRoleStructure(path, prefab); break;
                        case "head": ValidateHeadStructure(path, prefab); break;
                        case "weapon": ValidateWeaponStructure(path, prefab); break;
                        case "wing": ValidateWingStructure(path, prefab); break;
                        case "back": ValidateBackStructure(path, prefab); break;
                    }
                }
                catch (InvalidOperationException e) { issues.Add(e.Message); }
            }
            return issues.ToArray();
        }

        private static void ValidateRoleStructure(string path, GameObject prefab)
        {
            PlayableDirector director = prefab.GetComponentInChildren<PlayableDirector>(true);
            if (director == null || director.playableAsset == null)
                throw new InvalidOperationException(path + " 缺 PlayableDirector 或 playable");
            Animator animator = prefab.GetComponentInChildren<Animator>(true);
            if (animator == null)
                throw new InvalidOperationException(path + " 缺 Animator");
            bool animatorBound = director.playableAsset.outputs.Any(output =>
                director.GetGenericBinding(output.sourceObject) is Animator);
            if (!animatorBound)
                throw new InvalidOperationException(path + " Timeline AnimationTrack 没有绑定 Animator");
        }

        /// <summary>bindpose = W_bone⁻¹ × W_smr ⇒ W_bone(bind) = W_smr × bindpose⁻¹;期望补偿 L = W_bone(bind)⁻¹ × W_root。</summary>
        private static bool TryGetHeadBindLocal(Transform roleRoot, Transform host, out Matrix4x4 bindLocal)
        {
            bindLocal = Matrix4x4.identity;
            if (host == null) return false;
            foreach (SkinnedMeshRenderer smr in roleRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.sharedMesh == null) continue;
                Transform[] bones = smr.bones;
                Matrix4x4[] bindposes = smr.sharedMesh.bindposes;
                for (int i = 0; i < bones.Length && i < bindposes.Length; i++)
                {
                    if (bones[i] != host) continue;
                    Matrix4x4 headBindWorld = smr.transform.localToWorldMatrix * bindposes[i].inverse;
                    bindLocal = headBindWorld.inverse * roleRoot.localToWorldMatrix;
                    return true;
                }
            }
            return false;
        }

        private static void ValidateHeadStructure(string path, GameObject prefab)
        {
            RequireUnit(path + " prefab 根", prefab.transform);
            Transform locator = FindDirect(prefab.transform, "head_attach");
            Transform content = FindDirect(prefab.transform, "head_content");
            if (locator == null || content == null)
                throw new InvalidOperationException(path + " 缺根直属 head_attach/head_content");
            RequireUnit(path + " head_attach", locator);
            if (prefab.GetComponentsInChildren<Transform>(true).Count(t => t.name == "head_attach") != 1)
                throw new InvalidOperationException(path + " 必须有且只有一个 head_attach");
        }

        private static void ValidateWeaponStructure(string path, GameObject prefab)
        {
            RequireUnit(path + " prefab 根", prefab.transform);
            Transform locator = FindDirect(prefab.transform, "weapon_attach");
            Transform content = FindDirect(prefab.transform, "weapon_content");
            if (locator == null || content == null)
                throw new InvalidOperationException(path + " 缺根直属 weapon_attach/weapon_content");
            if (!Nearly(locator.localScale, Vector3.one, 0.0001f))
                throw new InvalidOperationException(path + " weapon_attach scale 必须为 1");
            RequireUnit(path + " weapon_content", content);
            if (prefab.GetComponentsInChildren<Transform>(true).Count(t => t.name == "weapon_attach") != 1)
                throw new InvalidOperationException(path + " 必须有且只有一个 weapon_attach");

            Transform source = content.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "Bone_wq_r");
            if (source != null)
            {
                Matrix4x4 relative = prefab.transform.worldToLocalMatrix * source.localToWorldMatrix;
                if (!Nearly(locator.localPosition, relative.GetPosition(), 0.0001f)
                    || Quaternion.Angle(locator.localRotation, relative.rotation) > 0.01f)
                    throw new InvalidOperationException(path + " weapon_attach 未对齐 Bone_wq_r 的握点和轴向");
            }
        }

        private static void ValidateWingStructure(string path, GameObject prefab)
        {
            RequireUnit(path + " prefab 根", prefab.transform);
            Transform locator = FindDirect(prefab.transform, "wing_attach");
            Transform content = FindDirect(prefab.transform, "wing_content");
            if (locator == null || content == null)
                throw new InvalidOperationException(path + " 缺根直属 wing_attach/wing_content");
            if (!Nearly(locator.localScale, Vector3.one, 0.0001f))
                throw new InvalidOperationException(path + " wing_attach scale 必须为 1");
            RequireUnit(path + " wing_content", content);
            if (prefab.GetComponentsInChildren<Transform>(true).Count(t => t.name == "wing_attach") != 1)
                throw new InvalidOperationException(path + " 必须有且只有一个 wing_attach");
        }

        private static void ValidateBackStructure(string path, GameObject prefab)
        {
            RequireUnit(path + " prefab 根", prefab.transform);
            Transform locator = FindDirect(prefab.transform, "back_attach");
            Transform content = FindDirect(prefab.transform, "back_content");
            if (locator == null || content == null)
                throw new InvalidOperationException(path + " 缺根直属 back_attach/back_content");
            if (!Nearly(locator.localScale, Vector3.one, 0.0001f))
                throw new InvalidOperationException(path + " back_attach scale 必须为 1");
            RequireUnit(path + " back_content", content);
            if (prefab.GetComponentsInChildren<Transform>(true).Count(t => t.name == "back_attach") != 1)
                throw new InvalidOperationException(path + " 必须有且只有一个 back_attach");
        }

        private static Transform SingleNamed(string path, Transform[] all, string name)
        {
            Transform[] found = all.Where(t => t.name == name).ToArray();
            if (found.Length != 1)
                throw new InvalidOperationException(path + $" {name} 数量={found.Length}，必须为 1");
            return found[0];
        }

        private static Transform FindDirect(Transform root, string name)
        {
            for (int i = 0; i < root.childCount; i++)
                if (root.GetChild(i).name == name) return root.GetChild(i);
            return null;
        }

        private static void RequireUnit(string label, Transform transform)
        {
            if (!Nearly(transform.localPosition, Vector3.zero, 0.0001f)
                || Quaternion.Angle(transform.localRotation, Quaternion.identity) > 0.01f
                || !Nearly(transform.localScale, Vector3.one, 0.0001f))
                throw new InvalidOperationException(label + " 必须为 position0/rotation0/scale1");
        }

        private static bool Nearly(Vector3 a, Vector3 b, float tolerance) =>
            Mathf.Abs(a.x - b.x) <= tolerance && Mathf.Abs(a.y - b.y) <= tolerance
            && Mathf.Abs(a.z - b.z) <= tolerance;

        /// <summary>
        /// 强制重导目录下所有模型:FBX 内嵌材质的贴图是"导入时按名搜索"解析的,首轮批量导入里
        /// FBX 若先于贴图导入,搜索落空=白模;贴图就位后重导一次即恢复。
        /// </summary>
        private static void ReimportModels(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return;
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { folder });
            foreach (string guid in guids)
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
            Debug.Log($"[ArtImport] 重导 {guids.Length} 个模型:{folder}");
        }

        // ---------------- 窗口状态 ----------------

        private readonly List<string> _sourcePrefabs = new List<string>();
        private string _targetBase = "Assets/GameRes/object/role";
        private string _sharedBase = "Assets/GameRes/object/artshared";
        private RenderMode _renderMode = RenderMode.Dedicated;
        private bool _importWholeFolder = true;
        private bool _runAddressables = true;
        private Plan _plan;
        private Vector2 _scroll;
        private string _lastDir = "";

        // ---- ImportPart(泛化部件一键导入)专用;手动窗口路径不设,保持原行为 ----
        private string _forcedTargetFolder; // 目标夹名固定(role_1213/head_1213/weapon_1200),不再按源夹名猜
        private string _partId;             // 根 prefab 统一改名 {id}@{动作}.prefab(head@idle → 1213@idle)
        private bool _sampleLanding = true; // 头饰/武器不采落点(2.33 身高归一只对角色本体有意义)
        private bool _checkRoleMounts;      // 角色本体导入后做挂点体检(head_mount/rhand/root),结果进台账

        // 可见入口统一在资产管理的当前模型条目;通用窗口只保留程序调用能力。
        public static void Open()
        {
            var win = GetWindow<ArtPrefabImporter>("美术成品导入");
            win.minSize = new Vector2(560f, 480f);
        }

        /// <summary>
        /// 外部面板(资产管理[替换新模型])的一键入口:按 交付规范 从美术工程导入/替换一个部件整夹。
        /// 走与本窗口完全相同的管线(整文件夹+保GUID+FBX二次导入+档案注入+台账),外加:
        /// 目标夹名固定为 {module}_{id}、根 prefab 统一改名 {id}@{动作}.prefab;
        /// module=role 时采落点+挂点体检,其余部件不采落点(身高归一只对角色本体有意义)。
        /// </summary>
        public static bool ImportPart(string module, string folderName, out string summary)
        {
            if (!IsPartModule(module))
            {
                summary = $"未知部件模块 {module}(可选:{string.Join("/", PartTopDirs.Keys)})";
                return false;
            }
            return ImportPart(module, folderName, PartSourceFolder(module, folderName), out summary);
        }

        /// <summary>从用户选择的美术模型目录导入全部动作，并固定替换到指定部件条目的目标目录。</summary>
        public static bool ImportPart(string module, string folderName, string sourceFolder, out string summary)
        {
            if (!IsPartModule(module))
            {
                summary = $"未知部件模块 {module}(可选:{string.Join("/", PartTopDirs.Keys)})";
                return false;
            }
            string folder = (sourceFolder ?? "").Replace('\\', '/').TrimEnd('/');
            if (!Directory.Exists(folder))
            {
                summary = string.IsNullOrEmpty(folder) ? "没有选择模型目录" : $"模型目录不存在:{folder}";
                return false;
            }
            string assetsRoot = $"{ArtProjectRoot}/Assets".TrimEnd('/');
            if (!folder.Equals(assetsRoot, StringComparison.OrdinalIgnoreCase)
                && !folder.StartsWith(assetsRoot + "/", StringComparison.OrdinalIgnoreCase))
            {
                summary = $"请选择当前 Art 项目 Assets 内的模型文件夹:\n{assetsRoot}\n\n当前选择:{folder}";
                return false;
            }
            var tool = CreateInstance<ArtPrefabImporter>();
            try
            {
                tool._sourcePrefabs.Clear();
                foreach (string prefab in Directory.GetFiles(folder, "*@*.prefab", SearchOption.TopDirectoryOnly))
                    tool._sourcePrefabs.Add(prefab.Replace('\\', '/'));
                if (tool._sourcePrefabs.Count == 0)
                {
                    summary = $"{folderName} 根目录下没有 xxx@动作.prefab";
                    return false;
                }

                tool._targetBase = $"Assets/GameRes/object/{module}";
                tool._forcedTargetFolder = folderName;
                int us = folderName.LastIndexOf('_');
                tool._partId = us >= 0 && us < folderName.Length - 1 ? folderName.Substring(us + 1) : folderName;
                tool._sampleLanding = module == "role";
                tool._checkRoleMounts = module == "role";

                Plan plan = tool.Scan();
                if (plan.Files.Count == 0)
                {
                    summary = "依赖闭包为空(警告见 Console)";
                    return false;
                }
                tool.Execute(plan);
                summary = $"{folderName}:新增 {plan.Files.Count(f => f.Action == FileAction.Add)}," +
                          $"替换 {plan.Files.Count(f => f.Action == FileAction.Replace)}," +
                          $"未变 {plan.Files.Count(f => f.Action == FileAction.SkipSame)}";
                string[] structureIssues = ValidateImportedPartStructure(module, plan.RootPrefabDsts);
                if (structureIssues.Length > 0)
                {
                    summary += ";模板结构检查失败:\n- " + string.Join("\n- ", structureIssues.Take(12));
                    return false; // 保留已导入资产供定位，但不让资产管理自动写入运行时替换配置。
                }
                summary += ";模板结构检查✓";
                return true;
            }
            finally
            {
                DestroyImmediate(tool);
            }
        }

        /// <summary>
        /// 一键把创角整模的动画/位移相关事实吐到 Console(排查"位移不播/落点不对"用,输出发给程序):
        /// FBX 动画类型与 motion 节点、每个 clip 的根位移曲线(RootT/MotionT/根路径 m_LocalPosition)、
        /// prefab 上每个 Animator 的 applyRootMotion、蒙皮渲染器的 rootBone 与包围盒。
        /// </summary>
        [MenuItem("神霄/美术/诊断新角色模型(输出Console)", priority = 31)]
        public static void DiagnoseCreatorModels()
        {
            var sb = new System.Text.StringBuilder("=== 新角色模型诊断(object/role/role_*) ===\n");
            foreach (string dir in AssetDatabase.GetSubFolders("Assets/GameRes/object/role"))
            {
                if (!Regex.IsMatch(Path.GetFileName(dir), @"^role_\d+$")) continue;
                sb.AppendLine($"—— {dir}");
                Vector3? create2End = null, create3End = null; // 顶层节点末帧,做落点一致性/单位判定

                foreach (string fbxPath in Directory.GetFiles(dir, "*.fbx", SearchOption.AllDirectories)
                             .Concat(Directory.GetFiles(dir, "*.FBX", SearchOption.AllDirectories))
                             .Select(p => p.Replace('\\', '/')).Distinct())
                {
                    if (!(AssetImporter.GetAtPath(fbxPath) is ModelImporter mi)) continue;
                    sb.AppendLine($"  [FBX] {Path.GetFileName(fbxPath)} animType={mi.animationType} " +
                                  $"motionNode='{mi.motionNodeName}' avatarSetup={mi.avatarSetup}");
                    foreach (UnityEngine.Object sub in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
                    {
                        if (!(sub is AnimationClip clip) || clip.name.StartsWith("__preview")) continue;
                        var bindings = UnityEditor.AnimationUtility.GetCurveBindings(clip);
                        int rootT = bindings.Count(b => b.propertyName.StartsWith("RootT") ||
                                                        b.propertyName.StartsWith("MotionT"));
                        var posPaths = bindings
                            .Where(b => b.propertyName.StartsWith("m_LocalPosition"))
                            .Select(b => b.path == "" ? "(根)" : b.path)
                            .Distinct().ToList();
                        sb.AppendLine($"    clip '{clip.name}' len={clip.length:F2}s legacy={clip.legacy} " +
                                      $"根位移曲线(RootT/MotionT)={rootT} 位置曲线路径数={posPaths.Count} " +
                                      $"前几个=[{string.Join(", ", posPaths.Take(6))}]");

                        // 顶层节点(路径最短的那个,如 "ride")的位移曲线逐轴统计:
                        // 位移到底有多大、往哪个轴走——沿 Z(镜头深度)的位移在正交展示台上不可见
                        if (clip.length > 0.5f && posPaths.Count > 0)
                        {
                            string top = posPaths.OrderBy(p => p.Length).First();
                            string topPath = top == "(根)" ? "" : top;
                            var lastValues = Vector3.zero;
                            for (int axisIndex = 0; axisIndex < 3; axisIndex++)
                            {
                                string axis = new[] { "x", "y", "z" }[axisIndex];
                                var eb = UnityEditor.EditorCurveBinding.FloatCurve(
                                    topPath, typeof(Transform), "m_LocalPosition." + axis);
                                AnimationCurve curve = UnityEditor.AnimationUtility.GetEditorCurve(clip, eb);
                                if (curve == null || curve.keys.Length == 0)
                                {
                                    sb.AppendLine($"      '{top}'.{axis}: 无曲线");
                                    continue;
                                }
                                float first = curve.keys[0].value, last = curve.keys[curve.keys.Length - 1].value;
                                float min = float.MaxValue, max = float.MinValue;
                                foreach (Keyframe k in curve.keys) { min = Mathf.Min(min, k.value); max = Mathf.Max(max, k.value); }
                                lastValues[axisIndex] = last;
                                sb.AppendLine($"      '{top}'.{axis}: 首={first:F2} 末={last:F2} 最小={min:F2} 最大={max:F2} 键数={curve.keys.Length}");
                            }
                            string lower = Path.GetFileName(fbxPath).ToLowerInvariant();
                            if (lower.Contains("create2")) create2End = lastValues;
                            else if (lower.Contains("create3")) create3End = lastValues;
                        }
                    }
                }

                // 落点一致性判定:约定=create2 末帧 == create3 停放。不一致的两大惯犯:
                // ① 单位错配 ~2.54×(英寸 vs 厘米,两个 FBX 导出单位不同,1213 实锤) ② 落点没对齐
                if (create2End.HasValue && create3End.HasValue)
                {
                    Vector3 a = create2End.Value, b = create3End.Value;
                    sb.AppendLine($"  [落点一致性] create2末帧={a} vs create3停放={b}");
                    float ry = Mathf.Abs(b.y) > 0.05f ? a.y / b.y : 0f;
                    float rz = Mathf.Abs(b.z) > 0.05f ? a.z / b.z : 0f;
                    if ((ry > 2.3f && ry < 2.8f) || (rz > 2.3f && rz < 2.8f))
                        sb.AppendLine("    ⚠⚠ 疑似单位错配 ~2.54×(英寸/厘米):create2.fbx 与 create3.fbx 的导出单位不一致!" +
                                      "美术需在 Max 里把两个 FBX 用同一单位设置重新导出(对照:末帧值应逐轴相等)");
                    else if ((a - b).magnitude > 0.5f)
                        sb.AppendLine($"    ⚠ 出场落点 ≠ 待机停放(差 {(a - b).magnitude:F2}):违反约定,切换会跳位,美术需对齐末帧");
                    else
                        sb.AppendLine("    ✓ 出场落点 = 待机停放,达标");
                }

                foreach (string prefabPath in Directory.GetFiles(dir, "*.prefab", SearchOption.TopDirectoryOnly)
                             .Select(p => p.Replace('\\', '/')))
                {
                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (go == null) continue;
                    sb.AppendLine($"  [Prefab] {Path.GetFileName(prefabPath)}");

                    // 落点达标检查(规则:待机脚底=世界原点,身高对齐老角色≈2.3;达标游戏零修正)
                    if (prefabPath.Contains("create3"))
                    {
                        Bounds b = default;
                        bool has = false;
                        foreach (SkinnedMeshRenderer smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        {
                            Transform space = smr.rootBone != null ? smr.rootBone : smr.transform;
                            Bounds wb = TransformBoundsEditor(space.localToWorldMatrix, smr.localBounds);
                            if (!has) { b = wb; has = true; } else b.Encapsulate(wb);
                        }
                        if (has)
                        {
                            var landing = new Vector3(b.center.x, b.min.y, b.center.z);
                            sb.AppendLine($"    [静态估算·仅参考] 落点≈{landing} 包围盒高≈{b.size.y:F2}" +
                                          "(精确落点由导入时 SampleAnimation 采样烤入档案,以导入日志的\"落点采样\"为准)");
                        }
                    }
                    foreach (Animator a in go.GetComponentsInChildren<Animator>(true))
                    {
                        // 绑定健康检查:动画曲线路径以 Animator 所在节点为根,顶层动画节点(如 ride)
                        // 必须是它的直接子级,否则整条链的曲线都绑不上
                        var childNames = new List<string>();
                        for (int i = 0; i < a.transform.childCount; i++)
                            childNames.Add(a.transform.GetChild(i).name);
                        bool hasRide = a.transform.Find("ride") != null;
                        sb.AppendLine($"    Animator@{a.gameObject.name} applyRootMotion={a.applyRootMotion} " +
                                      $"avatar={(a.avatar != null ? a.avatar.name : "无")} " +
                                      $"子级=[{string.Join(",", childNames)}] 有ride子级={hasRide}");
                    }
                    foreach (SkinnedMeshRenderer smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        sb.AppendLine($"    SMR@{smr.gameObject.name} rootBone={(smr.rootBone != null ? smr.rootBone.name : "无")} " +
                                      $"localBounds(c={smr.localBounds.center}, s={smr.localBounds.size})");
                }
            }
            // 同步写到工程根目录文件,方便直接取阅(不用手动从 Console 拷)
            string outPath = Path.GetFullPath("ArtImportDiagnose.log");
            File.WriteAllText(outPath, sb.ToString());
            sb.AppendLine($"(已写入 {outPath})");
            Debug.Log(sb.ToString());
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            // 一键部件替换入口在【神霄/资产管理】各模型条目详情页(走 ImportPart);
            // 本窗口保留手动模式:任意外部工程 prefab 的通用保GUID导入。
            EditorGUILayout.BeginHorizontal();
            string src = EditorGUILayout.TextField("美术工程(一键线源)", _quickSource);
            if (src != _quickSource)
            {
                _quickSource = src;
                EditorPrefs.SetString(QuickSourcePrefsKey, src);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("源(另一个 Unity 工程的 prefab)", EditorStyles.boldLabel);
            for (int i = _sourcePrefabs.Count - 1; i >= 0; i--)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(_sourcePrefabs[i], EditorStyles.miniLabel);
                if (GUILayout.Button("移除", GUILayout.Width(44f))) _sourcePrefabs.RemoveAt(i);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("添加 prefab…")) AddPrefabByPanel();
            if (GUILayout.Button("添加文件夹(收所有 prefab)…")) AddFolderByPanel();
            if (GUILayout.Button("清空", GUILayout.Width(60f)))
            {
                _sourcePrefabs.Clear();
                _plan = null;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("目标", EditorStyles.boldLabel);
            _targetBase = EditorGUILayout.TextField("模型目录", _targetBase);
            _sharedBase = EditorGUILayout.TextField("共享依赖目录", _sharedBase);
            _importWholeFolder = EditorGUILayout.ToggleLeft(
                "整文件夹导入(推荐:FBX 内嵌材质按名搜索的贴图不在 GUID 闭包里,漏了=白模)", _importWholeFolder);
            EditorGUILayout.HelpBox(
                "落夹规则:{模型目录}/{源文件夹名小写}/(交付规范 role_1213/head_1213/…,夹名即键位);\n" +
                "角色文件夹之外的依赖(Shared/PandaShader 等)按源相对路径落到共享目录,只存一份。",
                MessageType.None);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("渲染", EditorStyles.boldLabel);
            _renderMode = (RenderMode)EditorGUILayout.Popup("渲染模式", (int)_renderMode, new[]
            {
                "独立渲染器(推荐:新开一条渲染,不动老模型)",
                "共用默认渲染器(仅强制 Depth/Opaque Texture)",
                "不挂渲染档案",
            });

            EditorGUILayout.Space();
            // 贴图不做任何处理(压缩/转格式/去重都拿掉了,原样入库;体积问题另行统一处理)
            _runAddressables = EditorGUILayout.ToggleLeft("导入后跑 Addressable 自动分组", _runAddressables);

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = _sourcePrefabs.Count > 0;
            if (GUILayout.Button("1. 扫描(只读,不改工程)", GUILayout.Height(28f))) _plan = Scan();
            GUI.enabled = _plan != null && _plan.Files.Count > 0;
            if (GUILayout.Button("2. 导入", GUILayout.Height(28f)))
            {
                Execute(_plan);
                _plan = null;
            }
            GUI.enabled = true;
            if (GUILayout.Button("打开台账", GUILayout.Width(70f), GUILayout.Height(28f)))
            {
                if (File.Exists(LedgerPath)) EditorUtility.OpenWithDefaultApp(LedgerPath);
                else ShowNotification(new GUIContent("还没有台账(尚未导入过)"));
            }
            EditorGUILayout.EndHorizontal();

            if (_plan != null) DrawPlan(_plan);
            EditorGUILayout.EndScrollView();
        }

        private void DrawPlan(Plan plan)
        {
            EditorGUILayout.Space();
            int add = plan.Files.Count(f => f.Action == FileAction.Add);
            int replace = plan.Files.Count(f => f.Action == FileAction.Replace);
            int same = plan.Files.Count(f => f.Action == FileAction.SkipSame);
            int dedup = plan.Files.Count(f => f.Action == FileAction.DedupDropped);
            long copyBytes = plan.Files
                .Where(f => f.Action == FileAction.Add || f.Action == FileAction.Replace)
                .Sum(f => f.Bytes);
            EditorGUILayout.HelpBox(
                $"闭包 {plan.Files.Count} 个文件:新增 {add},替换 {replace},未变跳过 {same},去重丢弃 {dedup}\n" +
                $"实际拷贝 {copyBytes / (1024f * 1024f):F1} MB(压缩/转 PNG 在导入后进行)",
                plan.Warnings.Count > 0 ? MessageType.Warning : MessageType.Info);
            foreach (string w in plan.Warnings)
                EditorGUILayout.LabelField("⚠ " + w, EditorStyles.wordWrappedMiniLabel);
            foreach (PlannedFile f in plan.Files.OrderBy(f => f.Action).ThenBy(f => f.Dst))
                EditorGUILayout.LabelField($"[{ActionLabel(f.Action)}] {f.Dst}", EditorStyles.miniLabel);
        }

        private static string ActionLabel(FileAction a)
        {
            switch (a)
            {
                case FileAction.Add: return "新增";
                case FileAction.Replace: return "替换";
                case FileAction.SkipSame: return "未变";
                default: return "去重";
            }
        }

        // ---------------- 源选择 ----------------

        private void AddPrefabByPanel()
        {
            string p = EditorUtility.OpenFilePanel("选外部工程的 prefab", _lastDir, "prefab");
            if (string.IsNullOrEmpty(p)) return;
            _lastDir = Path.GetDirectoryName(p);
            AddSource(p);
        }

        private void AddFolderByPanel()
        {
            string dir = EditorUtility.OpenFolderPanel("选外部工程的文件夹(收其中所有 prefab)", _lastDir, "");
            if (string.IsNullOrEmpty(dir)) return;
            _lastDir = dir;
            foreach (string p in Directory.EnumerateFiles(dir, "*.prefab", SearchOption.AllDirectories))
                AddSource(p);
        }

        private void AddSource(string absPath)
        {
            string norm = absPath.Replace('\\', '/');
            if (FindAssetsRoot(norm) == null)
            {
                EditorUtility.DisplayDialog("不是 Unity 工程资产",
                    $"{norm}\n不在任何 Unity 工程的 Assets 目录下(找不到同级 ProjectSettings)。", "好");
                return;
            }
            if (norm.StartsWith(Application.dataPath.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("这是本工程的资产", "选的是主工程自己的 prefab,无需导入。", "好");
                return;
            }
            if (!_sourcePrefabs.Contains(norm)) _sourcePrefabs.Add(norm);
            _plan = null;
        }

        /// <summary>向上找 Assets 根:目录名为 Assets 且父级有 ProjectSettings 的那层。</summary>
        private static string FindAssetsRoot(string absPath)
        {
            var dir = new DirectoryInfo(Path.GetDirectoryName(absPath) ?? absPath);
            while (dir != null)
            {
                if (dir.Name == "Assets" && dir.Parent != null &&
                    Directory.Exists(Path.Combine(dir.Parent.FullName, "ProjectSettings")))
                    return dir.FullName.Replace('\\', '/');
                dir = dir.Parent;
            }
            return null;
        }

        // ---------------- 扫描:GUID 闭包 + 目标映射 + 分类 ----------------

        private Plan Scan()
        {
            var plan = new Plan();
            _md5Cache.Clear(); // 上次导入可能替换过工程内文件,哈希不能吃缓存
            try
            {
                var roots = _sourcePrefabs.Select(FindAssetsRoot).Distinct().ToList();
                if (roots.Count != 1)
                {
                    plan.Warnings.Add("所有源 prefab 必须来自同一个工程(检测到多个 Assets 根)。");
                    return plan;
                }
                plan.SourceAssetsRoot = roots[0];

                EditorUtility.DisplayProgressBar("扫描", "建源工程 GUID 索引…", 0.1f);
                Dictionary<string, string> guidToPath = BuildGuidIndex(plan.SourceAssetsRoot);

                EditorUtility.DisplayProgressBar("扫描", "解析依赖闭包…", 0.4f);
                List<PlannedFile> closure = CollectClosure(plan, guidToPath);

                EditorUtility.DisplayProgressBar("扫描", "映射目标路径…", 0.7f);
                MapTargets(plan, closure);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            return plan;
        }

        private static Dictionary<string, string> BuildGuidIndex(string assetsRoot)
        {
            var index = new Dictionary<string, string>();
            foreach (string meta in Directory.EnumerateFiles(assetsRoot, "*.meta", SearchOption.AllDirectories))
            {
                string guid = ReadGuidOfMeta(meta);
                if (guid == null) continue;
                string assetPath = meta.Substring(0, meta.Length - ".meta".Length);
                if (File.Exists(assetPath)) index[guid] = assetPath.Replace('\\', '/');
            }
            return index;
        }

        private static string ReadGuidOfMeta(string metaPath)
        {
            try
            {
                foreach (string line in File.ReadLines(metaPath).Take(5))
                {
                    Match m = GuidRegex.Match(line);
                    if (m.Success) return m.Groups[1].Value;
                }
            }
            catch { /* 单个 meta 读失败不阻塞整体 */ }
            return null;
        }

        private List<PlannedFile> CollectClosure(Plan plan, Dictionary<string, string> guidToPath)
        {
            var result = new List<PlannedFile>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var unresolved = new Dictionary<string, string>(); // guid -> 首个引用它的文件
            var queue = new Queue<string>(_sourcePrefabs);

            // 整文件夹导入:GUID 闭包看不见"按名引用"的资源——FBX 内嵌材质的贴图(如 1111 的
            // 服饰.tga/头.tga)是导入时按文件名搜索的,不进闭包就是白模。角色目录全量入队兜底。
            if (_importWholeFolder)
            {
                foreach (string folder in _sourcePrefabs
                             .Select(Path.GetDirectoryName).Where(d => d != null).Distinct())
                {
                    foreach (string file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
                    {
                        string norm = file.Replace('\\', '/');
                        if (norm.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
                        if (norm.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)) continue; // 展示场景不搬
                        queue.Enqueue(norm);
                    }
                }
            }

            while (queue.Count > 0)
            {
                string path = queue.Dequeue().Replace('\\', '/');
                if (!visited.Add(path)) continue;
                if (!File.Exists(path)) continue;

                string ext = Path.GetExtension(path);
                if (ForbiddenExts.Contains(ext))
                {
                    plan.Warnings.Add($"闭包里有代码/程序集,已跳过:{Rel(plan.SourceAssetsRoot, path)}");
                    continue;
                }

                string guid = ReadGuidOfMeta(path + ".meta");
                if (guid == null)
                {
                    plan.Warnings.Add($"缺 .meta,已跳过:{Rel(plan.SourceAssetsRoot, path)}");
                    continue;
                }

                var fi = new FileInfo(path);
                result.Add(new PlannedFile
                {
                    Guid = guid,
                    Src = path,
                    Bytes = fi.Length,
                });

                // 引用收集:文本资产解析内容;所有资产解析 .meta(FBX 的材质映射就在 meta 里)
                string refsText = TextExts.Contains(ext) ? SafeReadAllText(path) : string.Empty;
                refsText += SafeReadAllText(path + ".meta");
                foreach (Match m in GuidRegex.Matches(refsText))
                {
                    string g = m.Groups[1].Value;
                    if (guidToPath.TryGetValue(g, out string refPath))
                    {
                        if (!visited.Contains(refPath)) queue.Enqueue(refPath);
                    }
                    else if (!unresolved.ContainsKey(g))
                    {
                        unresolved[g] = Rel(plan.SourceAssetsRoot, path);
                    }
                }
            }

            // 源工程解析不到的 guid:包内/内置资产在主工程通常也有;真解析不到才是断引用风险
            foreach (KeyValuePair<string, string> kv in unresolved)
            {
                string inProject = AssetDatabase.GUIDToAssetPath(kv.Key);
                if (string.IsNullOrEmpty(inProject))
                    plan.Warnings.Add($"GUID {kv.Key} 在源工程与主工程都解析不到(引用自 {kv.Value}),导入后可能丢引用。");
            }
            return result;
        }

        private void MapTargets(Plan plan, List<PlannedFile> closure)
        {
            // 每个源 prefab 所在文件夹 → 目标文件夹名。ImportPart 固定为 {module}_{id};
            // 手动窗口按源文件夹名小写落夹(交付规范 role_1213/head_1213/…,夹名即键位)。
            var folderMap = new Dictionary<string, string>(); // 源相对文件夹 -> 目标文件夹名
            foreach (string prefab in _sourcePrefabs)
            {
                string folderRel = Rel(plan.SourceAssetsRoot, Path.GetDirectoryName(prefab));
                folderMap[folderRel] = _forcedTargetFolder ?? Path.GetFileName(folderRel).ToLowerInvariant();
            }

            var usedDst = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (PlannedFile f in closure)
            {
                string relAssets = Rel(plan.SourceAssetsRoot, f.Src);
                string owner = folderMap.Keys
                    .Where(k => relAssets.StartsWith(k + "/", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(k => k.Length)
                    .FirstOrDefault();

                // 角色目录根下的所有 prefab 都算"根 prefab"(挂档案+换材质),不只用户勾的那个——
                // create3 曾因只勾了 create2 而五次导入都没被接管,镜像/材质跟着漏(实锤教训)
                string srcDir = Path.GetDirectoryName(f.Src)?.Replace('\\', '/');
                bool isRootPrefab = f.Src.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) &&
                    _sourcePrefabs.Any(p => string.Equals(
                        Path.GetDirectoryName(p)?.Replace('\\', '/'), srcDir, StringComparison.OrdinalIgnoreCase));

                // ImportPart 的根 prefab 必须固定落到 {id}@{动作}.prefab。这里故意不按 GUID
                // 沿用旧路径:用户点的是“替换当前模型”,即使目标已有不同 GUID,也必须覆盖规范键位,
                // 不能悄悄生成 test_xxxxxx.prefab 让旧 test.prefab 继续被运行时加载。
                string canonicalRootDst = null;
                if (isRootPrefab && _partId != null)
                {
                    string sourceStem = Path.GetFileNameWithoutExtension(f.Src);
                    int at = sourceStem.IndexOf('@');
                    if (at >= 0 && at < sourceStem.Length - 1)
                    {
                        string action = sourceStem.Substring(at + 1).ToLowerInvariant();
                        canonicalRootDst = $"{_targetBase}/{_forcedTargetFolder}/{_partId}@{action}.prefab";
                    }
                }

                if (canonicalRootDst != null)
                {
                    MapForcedTarget(plan, f, canonicalRootDst, usedDst);
                }
                else if (_forcedTargetFolder != null && owner != null)
                {
                    // ImportPart 是整部件替换。部件目录内的依赖也必须落回源目录对应的规范路径；
                    // 不能因为上次交付重建了 meta/GUID，就保留旧文件并生成 idle_xxxxxx.playable。
                    string canonicalDst =
                        $"{_targetBase}/{folderMap[owner]}/{relAssets.Substring(owner.Length + 1)}";
                    MapForcedTarget(plan, f, canonicalDst, usedDst);
                }
                else
                {
                    string existing = AssetDatabase.GUIDToAssetPath(f.Guid);
                    if (!string.IsNullOrEmpty(existing) && existing.StartsWith("Assets/", StringComparison.Ordinal))
                    {
                        // 主工程已有同 GUID 资产:在原位替换/跳过,不另拷一份
                        f.Dst = existing;
                        f.Action = HashEquals(f.Src, existing) && HashEquals(f.Src + ".meta", existing + ".meta")
                            ? FileAction.SkipSame
                            : FileAction.Replace;
                    }
                    else
                    {
                        string dst = owner != null
                            ? $"{_targetBase}/{folderMap[owner]}/{relAssets.Substring(owner.Length + 1)}"
                            : $"{_sharedBase}/{relAssets}";
                        // 避撞:批内不同 GUID 落到同名路径,或磁盘上已有同名异 GUID 文件
                        if (!usedDst.Add(dst) || File.Exists(AbsOfProject(dst)))
                        {
                            string dir = Path.GetDirectoryName(dst)?.Replace('\\', '/');
                            string stem = Path.GetFileNameWithoutExtension(dst);
                            string ext = Path.GetExtension(dst);
                            dst = $"{dir}/{stem}_{f.Guid.Substring(0, 6)}{ext}";
                            usedDst.Add(dst);
                        }
                        f.Dst = dst;
                        f.Action = FileAction.Add;
                    }
                }
                if (isRootPrefab) plan.RootPrefabDsts.Add(f.Dst);
                plan.Files.Add(f);
            }
        }

        private static void MapForcedTarget(Plan plan, PlannedFile file, string destination,
            HashSet<string> usedDestinations)
        {
            if (!usedDestinations.Add(destination))
                plan.Warnings.Add($"部件内规范路径重名:{destination}(请清理美术源目录中的重复文件)");

            string existingByGuid = AssetDatabase.GUIDToAssetPath(file.Guid);
            if (!string.IsNullOrEmpty(existingByGuid)
                && !string.Equals(existingByGuid, destination, StringComparison.OrdinalIgnoreCase))
            {
                plan.RelocatedAssetPaths.Add(existingByGuid);
            }

            file.Dst = destination;
            file.Action = File.Exists(AbsOfProject(destination))
                ? (HashEquals(file.Src, destination)
                   && HashEquals(file.Src + ".meta", destination + ".meta")
                    ? FileAction.SkipSame
                    : FileAction.Replace)
                : FileAction.Add;
        }

        // ---------------- 导入执行 ----------------

        private void Execute(Plan plan)
        {
            var notes = new List<string>(plan.Warnings);
            List<PlannedFile> toCopy = plan.Files
                .Where(f => f.Action == FileAction.Add || f.Action == FileAction.Replace)
                .ToList();
            try
            {
                RemoveRelocatedAssets(plan, notes);

                // ImportPart 是整模型替换:同步根动作 prefab,删除目标目录里源目录已不存在的旧动作；
                // 部件内依赖若因历史 GUID 碰撞落过后缀路径,也迁回源目录对应的规范路径。
                SyncRootPrefabs(plan, notes);

                // 1. 落文件(文本资产顺手做去重 GUID 重写),一次 Refresh 全导入
                for (int i = 0; i < toCopy.Count; i++)
                {
                    PlannedFile f = toCopy[i];
                    EditorUtility.DisplayProgressBar("导入", f.Dst, (float)i / Mathf.Max(1, toCopy.Count));
                    CopyOne(f, plan.GuidRemap);
                }
                EditorUtility.DisplayProgressBar("导入", "AssetDatabase.Refresh…", 0.5f);
                AssetDatabase.Refresh();

                // 1.5 校验:落地的材质里每个 guid 引用必须能解析——shader 缺=紫模,贴图缺=白模,
                // 这类问题肉眼在 prefab 上看不出来,必须在这里点名到具体材质
                foreach (PlannedFile f in toCopy.Where(f =>
                             f.Dst.EndsWith(".mat", StringComparison.OrdinalIgnoreCase)))
                {
                    foreach (Match m in GuidRegex.Matches(SafeReadAllText(AbsOfProject(f.Dst))))
                    {
                        string g = m.Groups[1].Value;
                        if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(g)))
                            notes.Add($"材质引用缺失:{f.Dst} → guid {g}(shader/贴图没进闭包或源工程里就丢了)");
                    }
                }

                // (贴图不做任何处理:压缩/转格式/去重都已移除,原样入库,体积另行统一处理)

                // 2.6 FBX 二次导入:内嵌材质的贴图解析发生在 FBX 导入那一刻。两种悬空都要防:
                // ① 首轮批量导入 FBX 先于贴图 → 搜索落空(白模);
                // ② 本轮只替换了贴图(meta 变了会重导贴图)而 FBX 没拷 → FBX 旧产物里的贴图引用悬空,
                //    材质替换时读到 mainTexture=null(上次就是这样盲生成了无贴图材质)。
                // 所以对本次涉及的**全部**模型文件强制重导,不看拷贝状态。
                foreach (PlannedFile f in plan.Files.Where(f =>
                             f.Action != FileAction.DedupDropped &&
                             (f.Dst.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) ||
                              f.Dst.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))))
                {
                    EditorUtility.DisplayProgressBar("导入", "二次导入模型 " + f.Dst, 0.9f);
                    AssetDatabase.ImportAsset(f.Dst, ImportAssetOptions.ForceUpdate);
                }

                // role 只交 idle 时，程序侧生成标准 create3 动作件。创角链明确请求 create3，
                // 不能让业务代码把 idle 当 create3 硬播；生成独立 prefab + Timeline 后仍按标准动作清单接管。
                EnsureRoleCreate3Alias(plan, notes);

                // 2.7 材质策略:材质资产保留美术工程原样；运行时 ArtModelStager 只在实例上把
                // Standard/URP Lit 表面换成 URP Unlit，不再给 UI/场景模型补光。顺手清理历史版本的 _gen_* 材质。
                bool cleaned = false;
                foreach (string folder in plan.RootPrefabDsts
                             .Select(p => Path.GetDirectoryName(p)?.Replace('\\', '/')).Distinct())
                {
                    cleaned |= CleanupGeneratedMaterials(folder);
                }
                if (cleaned) AssetDatabase.Refresh();

                // 2.8 PandaShader 特效材质 alpha 规范化:该 shader 混合因子是材质属性 [_Scr][_Dst],
                // alpha 通道跟着同因子写会污染展示台 RT 的覆盖度(加法特效把 alpha 加满,
                // 合成后把 UI 背景压暗)。普通加法光效不写 alpha；但美术明确给半透明 Alpha 的
                // SkinnedMesh 结构层必须写覆盖度，否则整层进透明 RT 后 alpha 恒为 0（1005 wing-2 实锤）。
                HashSet<string> structuralAlphaMaterials = AnalyzeStructuralAlphaMaterials(
                    plan.RootPrefabDsts, notes);
                foreach (PlannedFile f in plan.Files
                             .Where(f => f.Dst.EndsWith(".mat", StringComparison.OrdinalIgnoreCase)))
                {
                    NormalizePandaAlpha(f.Dst,
                        structuralAlphaMaterials.Contains(f.Dst.Replace('\\', '/')));
                }
                AssetDatabase.SaveAssets();

                // 3. 渲染:独立 renderer 接进 RP Asset;prefab 根挂渲染档案
                int rendererIndex = -1;
                if (_renderMode == RenderMode.Dedicated)
                    rendererIndex = EnsureDedicatedRenderer(notes);
                if (_renderMode != RenderMode.None)
                {
                    string[] rootPrefabs = plan.RootPrefabDsts.Distinct().ToArray();
                    var landingSamples = new Dictionary<string, (bool hasLanding, Vector3 landing, float scale)>();
                    foreach (string dst in rootPrefabs)
                    {
                        landingSamples[dst] = _sampleLanding
                            ? SamplePrefabLanding(dst, notes)
                            : (false, Vector3.zero, 1f);
                    }

                    RoleAssemblyProfileData assemblyProfile = _sampleLanding
                        ? LoadRoleAssemblyProfile(rootPrefabs, landingSamples, notes)
                        : null;
                    float canonicalLandingScale = 1f;
                    if (assemblyProfile != null)
                    {
                        string canonicalPrefab = rootPrefabs.FirstOrDefault(path =>
                            string.Equals(ActionFromPrefab(path), assemblyProfile.canonicalAction,
                                StringComparison.OrdinalIgnoreCase));
                        canonicalLandingScale = landingSamples[canonicalPrefab].scale;
                    }

                    foreach (string dst in rootPrefabs)
                    {
                        (bool hasLanding, Vector3 landing, float sampledScale) = landingSamples[dst];
                        float landingScale = assemblyProfile != null ? canonicalLandingScale : sampledScale;
                        float attachmentSpaceScale = assemblyProfile != null
                            ? assemblyProfile.attachmentSpaceScale
                            : 1f;
                        if (assemblyProfile != null && hasLanding
                            && Mathf.Abs(sampledScale / canonicalLandingScale - 1f) > 0.03f)
                        {
                            notes.Add($"动作体量统一 {Path.GetFileName(dst)}:姿势采样 scale={sampledScale:F6}," +
                                      $"按 {assemblyProfile.canonicalAction} 固定为 {canonicalLandingScale:F6}");
                        }
                        string[] blendMats = AnalyzeBlendMaterials(dst, notes);
                        InjectProfile(dst, _renderMode == RenderMode.Dedicated, rendererIndex,
                            hasLanding, landing, landingScale, attachmentSpaceScale, blendMats, notes);
                    }
                }

                // 3.5 挂点体检(角色本体):必须带 head_mount/rhand/root 精确小写挂点,
                // 装配器(RoleModelAssembler.FindBone)按名精确匹配,缺=头饰/武器挂不上、特效挂错位
                if (_checkRoleMounts)
                {
                    foreach (string dst in plan.RootPrefabDsts.Distinct())
                    {
                        foreach (string issue in InspectRoleMountStructure(dst))
                            notes.Add($"挂点体检 {Path.GetFileName(dst)}:{issue}" +
                                      "（只影响部件挂接，身体动作已启用；美术工程跑[交付/补挂点]后重导）");
                    }
                }

                // 4. Addressable + 台账
                if (_runAddressables)
                {
                    EditorUtility.DisplayProgressBar("导入", "Addressable 自动分组…", 0.95f);
                    AddrSetup.AddressableSetup.AutoGroupAll();
                }
                QueueLedgerWrite(plan, rendererIndex, notes);

                int added = plan.Files.Count(f => f.Action == FileAction.Add);
                int replaced = plan.Files.Count(f => f.Action == FileAction.Replace);
                EditorUtility.DisplayDialog("导入完成",
                    $"新增 {added},替换 {replaced},未变 {plan.Files.Count(f => f.Action == FileAction.SkipSame)}," +
                    $"去重 {plan.Files.Count(f => f.Action == FileAction.DedupDropped)}\n" +
                    $"渲染模式:{_renderMode}(rendererIndex={rendererIndex})\n" +
                    $"台账:{LedgerPath}" +
                    (notes.Count > 0 ? $"\n注意事项 {notes.Count} 条,详见台账/Console" : ""), "好");
                foreach (string n in notes) Debug.LogWarning("[ArtImport] " + n);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void RemoveRelocatedAssets(Plan plan, List<string> notes)
        {
            var destinations = new HashSet<string>(
                plan.Files.Select(file => file.Dst), StringComparer.OrdinalIgnoreCase);
            foreach (string path in plan.RelocatedAssetPaths.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (destinations.Contains(path)) continue;
                if (!AssetDatabase.DeleteAsset(path))
                    throw new IOException("无法清理同 GUID 的旧导入路径:" + path);
                notes.Add("整部件替换:迁回规范路径并移除旧路径 " + path);
            }
        }

        /// <summary>
        /// 角色交付只有 idle 时，用同一段 Timeline 生成独立 create3 标准动作件。
        /// 两个 prefab/Timeline 各有自己的 GUID，运行时仍按动作名选择；以后美术补真 create3 时，
        /// SyncRootPrefabs 会先删掉这个程序别名，再由源交付完整替换。
        /// </summary>
        private void EnsureRoleCreate3Alias(Plan plan, List<string> notes)
        {
            if (!_checkRoleMounts || string.IsNullOrEmpty(_partId)) return;
            if (plan.RootPrefabDsts.Any(path =>
                    string.Equals(ActionOfPrefab(path), "create3", StringComparison.OrdinalIgnoreCase)))
                return;

            string idlePrefabPath = plan.RootPrefabDsts.FirstOrDefault(path =>
                string.Equals(ActionOfPrefab(path), "idle", StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(idlePrefabPath)
                || !File.Exists(AbsOfProject(idlePrefabPath)))
                return;

            GameObject idlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(idlePrefabPath);
            PlayableDirector idleDirector = idlePrefab != null
                ? idlePrefab.GetComponentInChildren<PlayableDirector>(true)
                : null;
            string idleTimelinePath = idleDirector?.playableAsset != null
                ? AssetDatabase.GetAssetPath(idleDirector.playableAsset)
                : null;
            if (string.IsNullOrEmpty(idleTimelinePath))
            {
                notes.Add($"{idlePrefabPath} 没有可复制的 Timeline，无法自动生成 create3");
                return;
            }

            string roleFolder = Path.GetDirectoryName(idlePrefabPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(roleFolder)) return;
            string create3PrefabPath = $"{roleFolder}/{_partId}@create3.prefab";
            string create3TimelineFolder = $"{roleFolder}/Timeline";
            string create3TimelinePath = $"{create3TimelineFolder}/create3.playable";

            if (!AssetDatabase.IsValidFolder(create3TimelineFolder))
            {
                Directory.CreateDirectory(AbsOfProject(create3TimelineFolder));
                AssetDatabase.Refresh();
            }
            if (AssetDatabase.LoadMainAssetAtPath(create3PrefabPath) != null)
                AssetDatabase.DeleteAsset(create3PrefabPath);
            if (AssetDatabase.LoadMainAssetAtPath(create3TimelinePath) != null)
                AssetDatabase.DeleteAsset(create3TimelinePath);

            if (!AssetDatabase.CopyAsset(idleTimelinePath, create3TimelinePath))
                throw new IOException($"复制 create3 Timeline 失败:{idleTimelinePath} → {create3TimelinePath}");
            PlayableAsset create3Timeline = AssetDatabase.LoadAssetAtPath<PlayableAsset>(create3TimelinePath);
            if (create3Timeline == null)
                throw new IOException("复制后的 create3 Timeline 无法加载:" + create3TimelinePath);
            create3Timeline.name = "create3";
            EditorUtility.SetDirty(create3Timeline);

            if (!AssetDatabase.CopyAsset(idlePrefabPath, create3PrefabPath))
                throw new IOException($"复制 create3 prefab 失败:{idlePrefabPath} → {create3PrefabPath}");
            GameObject root = PrefabUtility.LoadPrefabContents(create3PrefabPath);
            try
            {
                root.name = _partId + "@create3";
                PlayableDirector director = root.GetComponentInChildren<PlayableDirector>(true);
                Animator animator = root.GetComponentInChildren<Animator>(true);
                if (director == null || animator == null)
                    throw new InvalidOperationException(create3PrefabPath + " 缺 PlayableDirector/Animator");

                PlayableAsset oldTimeline = director.playableAsset;
                if (oldTimeline != null)
                {
                    foreach (PlayableBinding output in oldTimeline.outputs)
                        director.ClearGenericBinding(output.sourceObject);
                }
                director.playableAsset = create3Timeline;
                foreach (PlayableBinding output in create3Timeline.outputs)
                    director.SetGenericBinding(output.sourceObject, animator);

                PrefabUtility.SaveAsPrefabAsset(root, create3PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(create3TimelinePath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(create3PrefabPath, ImportAssetOptions.ForceUpdate);
            plan.RootPrefabDsts.Add(create3PrefabPath);
            notes.Add($"程序兼容:美术未交 create3，已由 idle 生成 {_partId}@create3.prefab（同动作内容）");
        }

        private static string ActionOfPrefab(string prefabPath)
        {
            string stem = Path.GetFileNameWithoutExtension(prefabPath);
            int at = stem.IndexOf('@');
            return at >= 0 && at < stem.Length - 1 ? stem.Substring(at + 1) : "";
        }

        private void SyncRootPrefabs(Plan plan, List<string> notes)
        {
            if (_partId == null || plan.RootPrefabDsts.Count == 0) return;

            var keep = new HashSet<string>(plan.RootPrefabDsts, StringComparer.OrdinalIgnoreCase);
            foreach (string folder in plan.RootPrefabDsts
                         .Select(p => Path.GetDirectoryName(p)?.Replace('\\', '/'))
                         .Where(p => !string.IsNullOrEmpty(p))
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!Directory.Exists(folder)) continue;
                foreach (string file in Directory.GetFiles(folder, "*.prefab", SearchOption.TopDirectoryOnly))
                {
                    string assetPath = file.Replace('\\', '/');
                    if (keep.Contains(assetPath) || !Path.GetFileName(assetPath).Contains("@")) continue;
                    if (AssetDatabase.DeleteAsset(assetPath))
                        notes.Add($"整模型替换:移除旧动作 {assetPath}");
                    else
                        notes.Add($"整模型替换:旧动作删除失败 {assetPath}");
                }
            }
        }

        private static void CopyOne(PlannedFile f, Dictionary<string, string> remap)
        {
            // 替换目标与源扩展名不同(典型:上次导入把 TGA 转成了 PNG,这次又想用原 TGA 替换回来):
            // 把目标迁移到源扩展名的新路径,旧文件+meta 删掉,GUID 靠 meta 原文续在新文件上,引用不断。
            string srcExt = Path.GetExtension(f.Src);
            if (!string.Equals(Path.GetExtension(f.Dst), srcExt, StringComparison.OrdinalIgnoreCase))
            {
                string oldAbs = AbsOfProject(f.Dst);
                if (File.Exists(oldAbs))
                {
                    File.SetAttributes(oldAbs, FileAttributes.Normal);
                    File.Delete(oldAbs);
                }
                if (File.Exists(oldAbs + ".meta"))
                {
                    File.SetAttributes(oldAbs + ".meta", FileAttributes.Normal);
                    File.Delete(oldAbs + ".meta");
                }
                f.Dst = Path.ChangeExtension(f.Dst, srcExt);
            }

            string dstAbs = AbsOfProject(f.Dst);
            Directory.CreateDirectory(Path.GetDirectoryName(dstAbs) ?? ".");
            if (File.Exists(dstAbs)) File.SetAttributes(dstAbs, FileAttributes.Normal);

            bool isText = TextExts.Contains(Path.GetExtension(f.Src));
            bool isPandaShader = Path.GetFileName(f.Src)
                                     .StartsWith("Pandavfx", StringComparison.OrdinalIgnoreCase)
                                 && f.Src.EndsWith(".shader", StringComparison.OrdinalIgnoreCase);
            if (isPandaShader)
            {
                // 自愈补丁:美术侧 shader 被换回原版时(实锤发生过,00:35 回滚→01:01 导入把主工程
                // 补丁覆盖没→特效全线异常),导入时自动补回 alpha 混合控制,永不再被回滚传染。
                // Shader 已被 Unity 编译后会以 mapped section 占用；内容相同绝不能重复打开写流，
                // 否则 Windows 返回 1224，连无关的角色/翅膀导入也会被共用 Shader 阻断。
                WriteTextIfChanged(dstAbs, EnsurePandaAlphaChannels(File.ReadAllText(f.Src)),
                    keepExistingWhenMapped: true);
            }
            else if (isText && remap.Count > 0)
                WriteTextIfChanged(dstAbs, RemapGuids(File.ReadAllText(f.Src), remap));
            else
                File.Copy(f.Src, dstAbs, true);

            string dstMeta = dstAbs + ".meta";
            if (File.Exists(dstMeta)) File.SetAttributes(dstMeta, FileAttributes.Normal);
            string meta = File.ReadAllText(f.Src + ".meta");
            WriteTextIfChanged(dstMeta, remap.Count > 0 ? RemapGuids(meta, remap) : meta);
        }

        /// <summary>
        /// Unity 对 Shader 等资源可能持有 Windows mapped section。先比较最终导入文本，内容相同就完全不碰文件；
        /// Panda Shader 若确有变化但当前会话仍被映射占用，保留主工程已加载版本并让本次模型导入继续，
        /// 不允许一个共用 Shader 把整套部件导入回滚。
        /// </summary>
        private static void WriteTextIfChanged(string path, string contents, bool keepExistingWhenMapped = false)
        {
            if (File.Exists(path) && string.Equals(File.ReadAllText(path), contents, StringComparison.Ordinal))
                return;
            try
            {
                File.WriteAllText(path, contents);
            }
            catch (IOException exception) when (keepExistingWhenMapped
                                                && File.Exists(path)
                                                && IsMappedFileLock(exception))
            {
                Debug.LogWarning($"[ArtImport] Unity 正在占用共用 Shader，保留主工程现有版本并继续导入:{path}" +
                                 "（如需升级 Shader，请退出 Unity 后单独同步）");
            }
        }

        private static bool IsMappedFileLock(IOException exception)
        {
            const int ErrorUserMappedFile = 1224;
            return (exception.HResult & 0xFFFF) == ErrorUserMappedFile
                   || exception.Message.IndexOf("1224", StringComparison.Ordinal) >= 0;
        }

        private static string RemapGuids(string text, Dictionary<string, string> remap)
        {
            foreach (KeyValuePair<string, string> kv in remap)
                text = text.Replace(kv.Key, kv.Value);
            return text;
        }

        /// <summary>
        /// PandaShader 必须带 alpha 通道混合控制([_ScrA][_DstA],默认 Zero/One=加法族不写 alpha):
        /// 没有它,特效会把 alpha 写进展示台 RT,预乘合成被污染=特效发白/压黑。
        /// 美术侧 shader 版本随时可能被换回原版,导入时无条件确保补丁在。
        /// </summary>
        private static string EnsurePandaAlphaChannels(string text)
        {
            if (text.Contains("_ScrA")) return text; // 已带补丁
            text = text.Replace(
                "[Enum(UnityEngine.Rendering.BlendMode)]_Dst(\"Dst\", Float) = 10",
                "[Enum(UnityEngine.Rendering.BlendMode)]_Dst(\"Dst\", Float) = 10\n" +
                "\t\t[Enum(UnityEngine.Rendering.BlendMode)]_ScrA(\"ScrA (alpha)\", Float) = 0\n" +
                "\t\t[Enum(UnityEngine.Rendering.BlendMode)]_DstA(\"DstA (alpha)\", Float) = 1");
            text = text.Replace("Blend [_Scr] [_Dst]", "Blend [_Scr] [_Dst], [_ScrA] [_DstA]");
            return text;
        }

        /// <summary>
        /// 确保独立 renderer 资产存在并接进所有 *_RPAsset 的 renderer list,返回其下标。
        /// 只"追加",不动 list 里原有项与默认下标——老模型渲染完全不受影响。
        /// </summary>
        private static int EnsureDedicatedRenderer(List<string> notes)
        {
            if (!File.Exists(RendererAssetPath))
            {
                if (!AssetDatabase.CopyAsset(RendererTemplatePath, RendererAssetPath))
                {
                    notes.Add($"创建 {RendererAssetPath} 失败(模板 {RendererTemplatePath} 不在?),独立渲染未生效。");
                    return -1;
                }
                AssetDatabase.ImportAsset(RendererAssetPath);
            }
            UnityEngine.Object rendererData = AssetDatabase.LoadMainAssetAtPath(RendererAssetPath);
            if (rendererData == null)
            {
                notes.Add($"载入 {RendererAssetPath} 失败,独立渲染未生效。");
                return -1;
            }

            int index = -1;
            foreach (string rpPath in Directory.GetFiles("Assets/Settings", "*_RPAsset.asset"))
            {
                string p = rpPath.Replace('\\', '/');
                UnityEngine.Object rp = AssetDatabase.LoadMainAssetAtPath(p);
                if (rp == null) continue;
                var so = new SerializedObject(rp);
                SerializedProperty list = so.FindProperty("m_RendererDataList");
                if (list == null || !list.isArray)
                {
                    notes.Add($"{p} 没有 m_RendererDataList(不是 URP RP Asset?),跳过。");
                    continue;
                }
                int found = -1;
                for (int i = 0; i < list.arraySize; i++)
                {
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue == rendererData) { found = i; break; }
                }
                if (found < 0)
                {
                    found = list.arraySize;
                    list.InsertArrayElementAtIndex(found);
                    list.GetArrayElementAtIndex(found).objectReferenceValue = rendererData;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(rp);
                }
                if (index < 0) index = found;
                else if (index != found)
                    notes.Add($"ArtFx renderer 在各 RP Asset 里的下标不一致({index} vs {found} @ {p})," +
                              "运行时按下标切换会错档——手动把两个 RP Asset 的 renderer list 对齐。");
            }
            AssetDatabase.SaveAssets();
            return index;
        }

        /// <summary>清理历史版本盲生成的 _gen_* 替身材质(材质策略已回归"保留 FBX 内嵌")。</summary>
        private static bool CleanupGeneratedMaterials(string roleFolder)
        {
            string abs = AbsOfProject($"{roleFolder}/Materials");
            if (!Directory.Exists(abs)) return false;
            bool any = false;
            foreach (string file in Directory.GetFiles(abs, "_gen_*.mat*"))
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
                any = true;
            }
            return any;
        }

        /// <summary>
        /// 找出“RGB 仍为加法、但美术明确给了半透明 Alpha”的蒙皮结构材质。
        /// 这类材质不是粒子光团：1005 的 wing-2 用 One/One 叠色，同时 MainColor.a=0.937；
        /// 若按普通加法族清空 alpha，透明模型 RT 中整层覆盖度恒为 0，最终看起来像漏挂了该层。
        /// </summary>
        private static HashSet<string> AnalyzeStructuralAlphaMaterials(
            IEnumerable<string> prefabPaths, List<string> notes)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string prefabPath in prefabPaths.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null) continue;
                foreach (SkinnedMeshRenderer renderer in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        if (material == null || material.shader == null ||
                            !material.shader.name.StartsWith("VFX/Pandavfx", StringComparison.OrdinalIgnoreCase) ||
                            !material.HasProperty("_Scr") || !material.HasProperty("_Dst") ||
                            !material.HasProperty("_MainColor"))
                            continue;
                        bool additive = Mathf.Approximately(material.GetFloat("_Scr"),
                                                (float)UnityEngine.Rendering.BlendMode.One) &&
                                        Mathf.Approximately(material.GetFloat("_Dst"),
                                                (float)UnityEngine.Rendering.BlendMode.One);
                        if (!additive || material.GetColor("_MainColor").a >= 0.999f) continue;
                        string materialPath = AssetDatabase.GetAssetPath(material).Replace('\\', '/');
                        if (string.IsNullOrEmpty(materialPath) || !result.Add(materialPath)) continue;
                        notes.Add($"加法蒙皮结构层保留 Alpha {Path.GetFileName(prefabPath)} → " +
                                  $"{renderer.name}/{material.name}");
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// PandaShader 材质的 alpha 混合规范化(shader 已加 [_ScrA][_DstA] 通道):
        /// 普通加法族(_Dst=One)不写 alpha(Zero/One),否则光团把 RT alpha 加满,预乘合成后压暗 UI 背景;
        /// 半透族和加法蒙皮结构层写覆盖度(One/OneMinusSrcAlpha)。RGB 混合因子始终保留美术原值。
        /// </summary>
        private static void NormalizePandaAlpha(string matPath, bool structuralAlpha)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null || mat.shader == null) return;
            if (!mat.shader.name.StartsWith("VFX/Pandavfx", StringComparison.OrdinalIgnoreCase)) return;
            if (!mat.HasProperty("_ScrA") || !mat.HasProperty("_DstA")) return;

            float dst = mat.HasProperty("_Dst")
                ? mat.GetFloat("_Dst") : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
            bool additive = Mathf.Approximately(dst, (float)UnityEngine.Rendering.BlendMode.One);
            bool writeCoverage = !additive || structuralAlpha;
            mat.SetFloat("_ScrA", writeCoverage
                ? (float)UnityEngine.Rendering.BlendMode.One
                : (float)UnityEngine.Rendering.BlendMode.Zero);
            mat.SetFloat("_DstA", writeCoverage
                ? (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha
                : (float)UnityEngine.Rendering.BlendMode.One);
            EditorUtility.SetDirty(mat);
        }

        private static Bounds TransformBoundsEditor(Matrix4x4 m, Bounds local)
        {
            Vector3 c = local.center, e = local.extents;
            var result = new Bounds(m.MultiplyPoint3x4(c), Vector3.zero);
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3(
                    c.x + ((i & 1) == 0 ? -e.x : e.x),
                    c.y + ((i & 2) == 0 ? -e.y : e.y),
                    c.z + ((i & 4) == 0 ? -e.z : e.z));
                result.Encapsulate(m.MultiplyPoint3x4(corner));
            }
            return result;
        }

        /// <summary>从 prefab 同目录 Timeline/{action}.playable 解出 AnimationTrack 真实引用的动画 clip(fileID+guid 精确匹配)。</summary>
        private static AnimationClip FindClipFromTimeline(string roleFolder, string action)
        {
            string abs = AbsOfProject($"{roleFolder}/Timeline/{action}.playable");
            if (!File.Exists(abs)) return null;
            foreach (Match m in Regex.Matches(File.ReadAllText(abs),
                         @"m_Clip: \{fileID: (-?\d+), guid: ([0-9a-f]{32})"))
            {
                long fileId = long.Parse(m.Groups[1].Value);
                string assetPath = AssetDatabase.GUIDToAssetPath(m.Groups[2].Value);
                if (string.IsNullOrEmpty(assetPath)) continue;
                foreach (UnityEngine.Object sub in AssetDatabase.LoadAllAssetsAtPath(assetPath))
                {
                    if (!(sub is AnimationClip c) || c.name.StartsWith("__preview")) continue;
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(c, out _, out long lid) && lid == fileId)
                        return c;
                }
            }
            return null;
        }

        /// <summary>兜底:按文件名找 role 目录下含动作名的 FBX 里的 clip(1300 这类总轨交付会落空)。</summary>
        private static AnimationClip FindClipByFileName(string roleFolder, string action)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { roleFolder }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (!Path.GetFileNameWithoutExtension(p).ToLowerInvariant().Contains(action)) continue;
                foreach (UnityEngine.Object sub in AssetDatabase.LoadAllAssetsAtPath(p))
                {
                    if (sub is AnimationClip c && !c.name.StartsWith("__preview")) return c;
                }
            }
            return null;
        }

        /// <summary>
        /// 落点/体量精确采样(按 prefab 自身):实例化后把【它自己的动作】用 SampleAnimation 拨到
        /// 末帧,BakeMesh 紧致盒量出脚底中心与姿势包围盒高度。要点:
        /// ① 不能用静态包围盒猜——嵌套 FBX 的默认姿势是绑定姿势,和动画停放点不是一回事;
        /// ② 每个动作仍独立采落点；体量 scale 默认沿用本动作，带 role_assembly_profile 的角色则
        ///    统一采用 canonicalAction，避免 death/跃起/披风等姿势包围盒把同一身体缩放成不同体型。
        /// </summary>
        private static (bool, Vector3, float) SamplePrefabLanding(string prefabPath, List<string> notes)
        {
            const float TARGET_HEIGHT = 2.33f; // 老拼装角色的世界身高标准

            string roleFolder = Path.GetDirectoryName(prefabPath)?.Replace('\\', '/');
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null || roleFolder == null) return (false, Vector3.zero, 1f);

            // 动作名 = prefab 名 '@' 后缀(1213@create2 → create2);没有 '@' 按 create3 待机算
            string stem = Path.GetFileNameWithoutExtension(prefabPath);
            int at = stem.IndexOf('@');
            string action = at >= 0 && at < stem.Length - 1 ? stem.Substring(at + 1).ToLowerInvariant() : "create3";

            // clip 发现:优先从 prefab 自己的 Timeline(.playable)里解真实引用——1300 的出场动画
            // 烤在 1300.fbx 总轨里(没有 create2.fbx),按文件名猜会落空→默认姿势→落点全错(实锤)
            AnimationClip clip = FindClipFromTimeline(roleFolder, action)
                                 ?? FindClipByFileName(roleFolder, action);

            GameObject inst = UnityEngine.Object.Instantiate(prefab);
            inst.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                Animator animator = inst.GetComponentInChildren<Animator>(true);
                GameObject sampleRoot = animator != null ? animator.gameObject : inst;
                if (clip != null)
                    clip.SampleAnimation(sampleRoot, Mathf.Max(0f, clip.length - 0.001f));
                else
                    notes.Add($"{prefabPath} 没找到 '{action}' 动画 clip,用默认姿势估落点(精度差)");

                Bounds bounds = default;
                bool has = false;
                foreach (SkinnedMeshRenderer smr in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var baked = new Mesh();
                    smr.BakeMesh(baked, true);
                    baked.RecalculateBounds();
                    Bounds b = TransformBoundsEditor(smr.transform.localToWorldMatrix, baked.bounds);
                    UnityEngine.Object.DestroyImmediate(baked);
                    if (!has) { bounds = b; has = true; } else bounds.Encapsulate(b);
                }
                if (!has || bounds.size.y < 0.01f)
                {
                    notes.Add($"{prefabPath} 量不到蒙皮包围盒,落点未烤入");
                    return (false, Vector3.zero, 1f);
                }

                // 锚点用骨骼,不用包围盒中心:包围盒被武器/披风/发饰污染,每个模型偏得各不相同
                // (实锤:三职业落点互不一致)。脚底=脚骨最低点,水平中心=骨盆;体量仍用包围盒高。
                Transform pelvis = null;
                float feetY = float.MaxValue;
                foreach (Transform t in inst.GetComponentsInChildren<Transform>(true))
                {
                    string n = t.name;
                    if (pelvis == null && (n == "Bip001" || n.EndsWith("Pelvis"))) pelvis = t;
                    if (n.Contains("Foot") && !n.Contains("Footsteps"))
                        feetY = Mathf.Min(feetY, t.position.y);
                }
                Vector3 landing = pelvis != null && feetY < float.MaxValue
                    ? new Vector3(pelvis.position.x, feetY, pelvis.position.z)
                    : new Vector3(bounds.center.x, bounds.min.y, bounds.center.z); // 无标准骨骼才退回包围盒
                float scale = TARGET_HEIGHT / bounds.size.y;
                string msg = $"落点采样 {Path.GetFileName(prefabPath)}(按 {action} 末帧" +
                             $"{(clip != null ? "" : ",无clip默认姿势")}" +
                             $"{(pelvis != null ? ",骨骼锚点" : ",包围盒锚点")}):landing={landing} " +
                             $"身高={bounds.size.y:F2} scale={scale:F2}";
                Debug.Log("[ArtImport] " + msg);
                notes.Add(msg); // 进台账,方便离线取证
                return (true, landing, scale);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(inst);
            }
        }

        private static RoleAssemblyProfileData LoadRoleAssemblyProfile(
            string[] rootPrefabs,
            Dictionary<string, (bool hasLanding, Vector3 landing, float scale)> landingSamples,
            List<string> notes)
        {
            if (rootPrefabs == null || rootPrefabs.Length == 0) return null;
            string folder = Path.GetDirectoryName(rootPrefabs[0])?.Replace('\\', '/');
            string profilePath = string.IsNullOrEmpty(folder) ? null : $"{folder}/{RoleAssemblyProfileFile}";
            if (string.IsNullOrEmpty(profilePath) || !File.Exists(profilePath))
            {
                float[] validScales = landingSamples.Values
                    .Where(sample => sample.hasLanding && sample.scale > 0.01f)
                    .Select(sample => sample.scale).ToArray();
                if (validScales.Length > 1 && validScales.Max() / validScales.Min() > 1.10f)
                {
                    notes.Add($"角色动作采样体量差异 {validScales.Max() / validScales.Min():F2}×，但缺少 " +
                              $"{RoleAssemblyProfileFile}；请确认是否为姿势包围盒误差或骨架单位差异");
                }
                return null;
            }

            try
            {
                RoleAssemblyProfileData profile = JsonUtility.FromJson<RoleAssemblyProfileData>(
                    File.ReadAllText(profilePath));
                if (profile == null || profile.version != 1
                    || string.IsNullOrWhiteSpace(profile.canonicalAction)
                    || profile.attachmentSpaceScale < 0.01f)
                {
                    notes.Add($"角色装配档案无效:{profilePath}(version=1、canonicalAction 非空、" +
                              "attachmentSpaceScale>=0.01)");
                    return null;
                }

                string canonicalPrefab = rootPrefabs.FirstOrDefault(path =>
                    string.Equals(ActionFromPrefab(path), profile.canonicalAction,
                        StringComparison.OrdinalIgnoreCase));
                if (canonicalPrefab == null || !landingSamples.TryGetValue(canonicalPrefab, out var canonical)
                    || !canonical.hasLanding || canonical.scale < 0.01f)
                {
                    notes.Add($"角色装配档案 canonicalAction={profile.canonicalAction} 没有有效采样:" +
                              profilePath);
                    return null;
                }

                notes.Add($"角色装配空间 {Path.GetFileName(folder)}:template={profile.skeletonTemplate}," +
                          $"canonical={profile.canonicalAction},landingScale={canonical.scale:F6}," +
                          $"attachmentSpaceScale={profile.attachmentSpaceScale:F6}");
                return profile;
            }
            catch (Exception e)
            {
                notes.Add($"读取角色装配档案失败({profilePath}):{e.Message}");
                return null;
            }
        }

        private static string ActionFromPrefab(string prefabPath)
        {
            string stem = Path.GetFileNameWithoutExtension(prefabPath);
            int at = stem.IndexOf('@');
            return at >= 0 && at < stem.Length - 1
                ? stem.Substring(at + 1).ToLowerInvariant()
                : "create3";
        }

        /// <summary>
        /// 分析 prefab 各身体材质贴图的 alpha 直方图,点名需要【半透渐变混合】的材质:
        /// alpha 有中间值(1~254 占比 >0.2%)=轻纱/雾状渐变 → 运行时设 Transparent;
        /// 只有 0/255 二值=缺口镂空 → Alpha Clipping 即可。TGA 直接读字节,PNG 走 LoadImage。
        /// </summary>
        private static string[] AnalyzeBlendMaterials(string prefabPath, List<string> notes)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return new string[0];
            var result = new HashSet<string>();
            var cache = new Dictionary<string, float>();
            foreach (Renderer r in prefab.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                foreach (Material m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    // 已经由美术 Shader/材质定义为 Transparent 的资源必须原样保留。尤其 Panda
                    // 使用独立颜色/Alpha Blend，不能仅凭贴图有渐变 alpha 就误标为待转换材质。
                    string renderType = m.GetTag("RenderType", false, string.Empty);
                    if (string.Equals(renderType, "Transparent", StringComparison.OrdinalIgnoreCase) ||
                        (m.HasProperty("_Surface") && m.GetFloat("_Surface") > 0.5f))
                        continue;
                    Texture tex = m.mainTexture;
                    if (tex == null && m.HasProperty("_BaseMap")) tex = m.GetTexture("_BaseMap");
                    if (tex == null) continue;
                    string texPath = AssetDatabase.GetAssetPath(tex);
                    if (string.IsNullOrEmpty(texPath)) continue;
                    if (!cache.TryGetValue(texPath, out float gradient))
                    {
                        gradient = GradientAlphaFraction(texPath);
                        cache[texPath] = gradient;
                    }
                    if (gradient > 0.002f && result.Add(m.name))
                        notes.Add($"半透渐变材质 {Path.GetFileName(prefabPath)} → \"{m.name}\"" +
                                  $"(贴图 {Path.GetFileName(texPath)} 渐变像素 {gradient:P1},运行时走 Transparent)");
                }
            }
            return result.ToArray();
        }

        /// <summary>贴图 alpha 中间值(1~254)像素占比;-1=格式不支持分析(jpg 无 alpha 等)。</summary>
        private static float GradientAlphaFraction(string assetPath)
        {
            try
            {
                string abs = AbsOfProject(assetPath);
                string ext = Path.GetExtension(assetPath).ToLowerInvariant();
                if (ext == ".tga")
                {
                    byte[] d = File.ReadAllBytes(abs);
                    if (d.Length < 18 || d[2] != 2 || d[16] != 32) return -1f; // 只认无压缩 32 位
                    int w = d[12] | d[13] << 8, h = d[14] | d[15] << 8;
                    int off = 18 + d[0];
                    long total = 0, mid = 0;
                    for (long i = 0; i < (long)w * h; i += 64)
                    {
                        byte a = d[off + i * 4 + 3];
                        total++;
                        if (a > 0 && a < 255) mid++;
                    }
                    return total > 0 ? (float)mid / total : -1f;
                }
                if (ext == ".png")
                {
                    var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!t.LoadImage(File.ReadAllBytes(abs))) { UnityEngine.Object.DestroyImmediate(t); return -1f; }
                    Color32[] px = t.GetPixels32();
                    UnityEngine.Object.DestroyImmediate(t);
                    long total = 0, mid = 0;
                    for (int i = 0; i < px.Length; i += 16)
                    {
                        byte a = px[i].a;
                        total++;
                        if (a > 0 && a < 255) mid++;
                    }
                    return total > 0 ? (float)mid / total : -1f;
                }
                return -1f; // jpg 等无 alpha
            }
            catch
            {
                return -1f;
            }
        }

        private static void InjectProfile(string prefabPath, bool dedicated, int rendererIndex,
            bool hasLanding, Vector3 landing, float landingScale, float attachmentSpaceScale,
            string[] blendMaterials, List<string> notes)
        {
            GameObject contents = null;
            try
            {
                contents = PrefabUtility.LoadPrefabContents(prefabPath);
                ArtModelRenderProfile p = contents.GetComponent<ArtModelRenderProfile>();
                if (p == null) p = contents.AddComponent<ArtModelRenderProfile>();
                p.useDedicatedRenderer = dedicated;
                p.rendererIndex = rendererIndex;
                p.forceDepthTexture = true;
                p.forceOpaqueTexture = true;
                p.hasLanding = hasLanding;
                p.landingOffset = landing;
                p.landingScale = landingScale;
                p.attachmentSpaceScale = attachmentSpaceScale;
                p.blendMaterials = blendMaterials;
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            }
            catch (Exception e)
            {
                notes.Add($"挂渲染档案失败({prefabPath}):{e.Message}");
            }
            finally
            {
                if (contents != null) PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static readonly List<LedgerRun> PendingLedgerRuns = new List<LedgerRun>();
        private static bool _ledgerWriteHooked;
        private static int _ledgerWriteAttempts;
        private static double _nextLedgerWriteTime;

        private void QueueLedgerWrite(Plan plan, int rendererIndex, List<string> notes)
        {
            var run = new LedgerRun
            {
                time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                sourceProject = plan.SourceAssetsRoot,
                renderMode = _renderMode.ToString(),
                rendererIndex = rendererIndex,
                added = plan.Files.Count(f => f.Action == FileAction.Add),
                replaced = plan.Files.Count(f => f.Action == FileAction.Replace),
                skippedSame = plan.Files.Count(f => f.Action == FileAction.SkipSame),
                deduped = plan.Files.Count(f => f.Action == FileAction.DedupDropped),
                notes = new List<string>(notes),
            };
            foreach (PlannedFile f in plan.Files)
            {
                run.files.Add(new LedgerFile
                {
                    guid = f.Guid,
                    src = Rel(plan.SourceAssetsRoot, f.Src),
                    dst = f.Action == FileAction.DedupDropped ? $"(去重→{f.DedupToGuid})" : f.Dst,
                    action = f.Action.ToString(),
                    bytes = f.Bytes,
                });
            }
            PendingLedgerRuns.Add(run);
            _ledgerWriteAttempts = 0;
            _nextLedgerWriteTime = EditorApplication.timeSinceStartup;
            if (_ledgerWriteHooked) return;
            _ledgerWriteHooked = true;
            EditorApplication.update += FlushPendingLedgerRuns;
        }

        private static void FlushPendingLedgerRuns()
        {
            if (PendingLedgerRuns.Count == 0)
            {
                StopLedgerWriter();
                return;
            }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating
                || EditorApplication.timeSinceStartup < _nextLedgerWriteTime)
                return;

            try
            {
                Ledger ledger = File.Exists(LedgerPath)
                    ? JsonUtility.FromJson<Ledger>(File.ReadAllText(LedgerPath)) ?? new Ledger()
                    : new Ledger();
                ledger.runs.AddRange(PendingLedgerRuns);
                Directory.CreateDirectory(Path.GetDirectoryName(LedgerPath) ?? ".");
                File.WriteAllText(LedgerPath, JsonUtility.ToJson(ledger, true));
                PendingLedgerRuns.Clear();
                StopLedgerWriter();
                try
                {
                    AssetDatabase.ImportAsset(LedgerPath, ImportAssetOptions.ForceSynchronousImport);
                }
                catch (Exception exception)
                {
                    Debug.LogException(new IOException(
                        $"台账已经写入，但 Unity 未能立即刷新资产:{LedgerPath}", exception));
                }
            }
            catch (IOException exception)
            {
                _ledgerWriteAttempts++;
                if (_ledgerWriteAttempts < 20)
                {
                    _nextLedgerWriteTime = EditorApplication.timeSinceStartup + 0.25d;
                    return;
                }
                Debug.LogException(new IOException(
                    $"美术导入已完成，但台账在 {_ledgerWriteAttempts} 次重试后仍无法写入:{LedgerPath}", exception));
                PendingLedgerRuns.Clear();
                StopLedgerWriter();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                PendingLedgerRuns.Clear();
                StopLedgerWriter();
            }
        }

        private static void StopLedgerWriter()
        {
            if (!_ledgerWriteHooked) return;
            EditorApplication.update -= FlushPendingLedgerRuns;
            _ledgerWriteHooked = false;
        }

        // ---------------- 小工具 ----------------

        private static string Rel(string root, string abs)
        {
            string a = abs.Replace('\\', '/');
            string r = root.Replace('\\', '/').TrimEnd('/');
            return a.StartsWith(r + "/", StringComparison.OrdinalIgnoreCase) ? a.Substring(r.Length + 1) : a;
        }

        private static string AbsOfProject(string assetsRelPath)
        {
            // Application.dataPath = <工程>/Assets;assetsRelPath 形如 Assets/xxx
            string projRoot = Path.GetDirectoryName(Application.dataPath) ?? ".";
            return Path.Combine(projRoot, assetsRelPath).Replace('\\', '/');
        }

        private static bool HashEquals(string absA, string projRelB)
        {
            string absB = AbsOfProject(projRelB);
            if (!File.Exists(absA) || !File.Exists(absB)) return false;
            return Md5File(absA) == Md5File(absB);
        }

        private static readonly Dictionary<string, string> _md5Cache = new Dictionary<string, string>();

        private static string Md5File(string absPath)
        {
            if (_md5Cache.TryGetValue(absPath, out string cached)) return cached;
            using (var md5 = MD5.Create())
            using (FileStream fs = File.OpenRead(absPath))
            {
                string hash = BitConverter.ToString(md5.ComputeHash(fs));
                _md5Cache[absPath] = hash;
                return hash;
            }
        }

        private static string SafeReadAllText(string path)
        {
            try { return File.Exists(path) ? File.ReadAllText(path) : string.Empty; }
            catch { return string.Empty; }
        }
    }
}
