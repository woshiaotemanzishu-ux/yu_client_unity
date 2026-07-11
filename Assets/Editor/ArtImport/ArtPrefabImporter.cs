using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Shenxiao.Common.UI3D;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.EditorTools.ArtImport
{
    /// <summary>
    /// 外部成品模型导入(美术工程 → 主工程,保 GUID 直搬)。
    ///
    /// 用法:菜单 [神霄/美术/导入外部成品模型] → 选另一个 Unity 工程里的 prefab(或整文件夹)
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

        // ---------------- 职业对照面板(左=老模型,右=新整模) ----------------

        private sealed class CareerRow
        {
            public string Name;           // 剑士
            public int Res;               // role_res(configlogin.json CreateRole.Res)
            public bool HasOld;           // object/role/model_clothe_{res}/ 在
            public string NewStatus;      // 未导入 / 仅create2 / create2+3
            public string ArtStatus;      // 美术工程:未交付 / 未导入 / 可更新 / 已是最新
            public string ImportedFolder; // Assets/GameRes/object/role/model_create_{res}
            public readonly List<string> ArtPrefabs = new List<string>();
        }

        private const string QuickSourcePrefsKey = "ArtImport.SourceProject";
        private const string LoginConfigPath = "Assets/GameRes/resource/config/client/configlogin.json";
        private string _quickSource;
        private List<CareerRow> _careers;

        private void OnEnable()
        {
            _quickSource = EditorPrefs.GetString(QuickSourcePrefsKey, "E:/Project/ArtsProject");
        }

        /// <summary>按 configlogin.json 的职业表建对照:老拼装 / 新整模(工程内) / 美术工程交付状态。</summary>
        private void ScanCareers()
        {
            _careers = new List<CareerRow>();
            if (!File.Exists(LoginConfigPath))
            {
                EditorUtility.DisplayDialog("缺配置", LoginConfigPath + " 不存在", "好");
                return;
            }
            Newtonsoft.Json.Linq.JObject cfg =
                Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(LoginConfigPath));
            Newtonsoft.Json.Linq.JToken createRole = cfg["CreateRole"];
            if (!(createRole?["UI"] is Newtonsoft.Json.Linq.JArray ui))
            {
                EditorUtility.DisplayDialog("配置异常", "configlogin.json 里没有 CreateRole.UI", "好");
                return;
            }
            foreach (Newtonsoft.Json.Linq.JToken o in ui)
            {
                int career = o.Value<int>("career");
                int sex = o.Value<int>("sex");
                int res = createRole["Res"]?[$"{career}@{sex}"]?.Value<int>("role_res") ?? 0;
                var row = new CareerRow { Name = o.Value<string>("name"), Res = res };
                if (res > 0)
                {
                    row.HasOld = AssetDatabase.IsValidFolder($"Assets/GameRes/object/role/model_clothe_{res}");
                    row.ImportedFolder = $"Assets/GameRes/object/role/model_create_{res}";
                    bool c2 = File.Exists(AbsOfProject($"{row.ImportedFolder}/{res}@create2.prefab"));
                    bool c3 = File.Exists(AbsOfProject($"{row.ImportedFolder}/{res}@create3.prefab"));
                    row.NewStatus = c2 ? (c3 ? "create2+3" : "仅create2") : "未导入";
                    ScanArtSide(row);
                }
                _careers.Add(row);
            }
        }

        private void ScanArtSide(CareerRow row)
        {
            row.ArtPrefabs.Clear();
            string folder = (_quickSource ?? "").Replace('\\', '/').TrimEnd('/') + $"/Assets/role_{row.Res}";
            if (!Directory.Exists(folder))
            {
                row.ArtStatus = "未交付";
                return;
            }
            bool anyNew = false, anyUpdate = false;
            foreach (string prefab in Directory.GetFiles(folder, "*@*.prefab", SearchOption.TopDirectoryOnly))
            {
                string abs = prefab.Replace('\\', '/');
                row.ArtPrefabs.Add(abs);
                string guid = ReadGuidOfMeta(abs + ".meta");
                string existing = guid != null ? AssetDatabase.GUIDToAssetPath(guid) : null;
                if (string.IsNullOrEmpty(existing)) anyNew = true;
                else if (!(HashEquals(abs, existing) && HashEquals(abs + ".meta", existing + ".meta"))) anyUpdate = true;
            }
            row.ArtStatus = row.ArtPrefabs.Count == 0 ? "未交付"
                : anyNew ? "未导入" : anyUpdate ? "可更新" : "已是最新";
        }

        private void DrawCareerPanel()
        {
            EditorGUILayout.LabelField("创角职业对照(左=老拼装,右=新整模;运行时有新用新、无新用老)", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            string src = EditorGUILayout.TextField("美术工程", _quickSource);
            if (src != _quickSource)
            {
                _quickSource = src;
                EditorPrefs.SetString(QuickSourcePrefsKey, src);
            }
            if (GUILayout.Button("扫描", GUILayout.Width(60f))) ScanCareers();
            EditorGUILayout.EndHorizontal();

            if (_careers == null) return;
            foreach (CareerRow row in _careers)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{row.Name}({(row.Res > 0 ? row.Res.ToString() : "无res")})", GUILayout.Width(96f));
                EditorGUILayout.LabelField(row.HasOld ? "老:拼装✓" : "老:缺!", GUILayout.Width(66f));
                EditorGUILayout.LabelField("新:" + (row.NewStatus ?? "-"), GUILayout.Width(96f));
                EditorGUILayout.LabelField("美术:" + (row.ArtStatus ?? "-"), GUILayout.Width(96f));

                GUI.enabled = row.ArtPrefabs.Count > 0;
                if (GUILayout.Button("导入/更新", GUILayout.Width(70f)))
                {
                    _sourcePrefabs.Clear();
                    _sourcePrefabs.AddRange(row.ArtPrefabs);
                    Plan plan = Scan();
                    if (plan.Files.Count > 0) Execute(plan);
                    _plan = null;
                    ScanCareers();
                    GUIUtility.ExitGUI(); // 布局中途弹了进度条/对话框,结束本帧 GUI 防布局错乱
                }
                GUI.enabled = row.NewStatus != null && row.NewStatus != "未导入";
                if (GUILayout.Button("重导FBX", GUILayout.Width(66f)))
                {
                    ReimportModels(row.ImportedFolder);
                    GUIUtility.ExitGUI();
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space();
        }

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

        [MenuItem("神霄/美术/导入外部成品模型(保GUID)", priority = 30)]
        public static void Open()
        {
            var win = GetWindow<ArtPrefabImporter>("美术成品导入");
            win.minSize = new Vector2(560f, 480f);
        }

        /// <summary>
        /// 外部面板(资产管理等)的一键入口:按 role_res 从美术工程导入/替换该角色的创角整模。
        /// 走与本窗口完全相同的管线(整文件夹+保GUID+FBX二次导入+档案注入+台账)。
        /// </summary>
        public static bool ImportRole(int res, out string summary)
        {
            var tool = CreateInstance<ArtPrefabImporter>();
            try
            {
                string folder = (tool._quickSource ?? "").Replace('\\', '/').TrimEnd('/') + $"/Assets/role_{res}";
                if (!Directory.Exists(folder))
                {
                    summary = $"美术工程未交付 role_{res}({folder})";
                    return false;
                }
                tool._sourcePrefabs.Clear();
                foreach (string prefab in Directory.GetFiles(folder, "*@*.prefab", SearchOption.TopDirectoryOnly))
                    tool._sourcePrefabs.Add(prefab.Replace('\\', '/'));
                if (tool._sourcePrefabs.Count == 0)
                {
                    summary = $"role_{res} 根目录下没有 xxx@动作.prefab";
                    return false;
                }
                Plan plan = tool.Scan();
                if (plan.Files.Count == 0)
                {
                    summary = "依赖闭包为空(警告见 Console)";
                    return false;
                }
                tool.Execute(plan);
                summary = $"role_{res}:新增 {plan.Files.Count(f => f.Action == FileAction.Add)}," +
                          $"替换 {plan.Files.Count(f => f.Action == FileAction.Replace)}," +
                          $"未变 {plan.Files.Count(f => f.Action == FileAction.SkipSame)}";
                return true;
            }
            finally
            {
                DestroyImmediate(tool);
            }
        }

        /// <summary>
        /// 美术工程交付状态:未交付 / 未导入 / 可更新 / 已是最新(哈希比对)。
        /// 有几 MB 的 prefab 要算 MD5——调用方自行缓存结果,别放 OnGUI 每帧调。
        /// </summary>
        public static string GetArtStatus(int res)
        {
            _md5Cache.Clear(); // 导入会替换工程内文件,别吃陈旧哈希
            string src = EditorPrefs.GetString(QuickSourcePrefsKey, "E:/Project/ArtsProject");
            string folder = src.Replace('\\', '/').TrimEnd('/') + $"/Assets/role_{res}";
            if (!Directory.Exists(folder)) return "未交付";
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
        /// 一键把创角整模的动画/位移相关事实吐到 Console(排查"位移不播/落点不对"用,输出发给程序):
        /// FBX 动画类型与 motion 节点、每个 clip 的根位移曲线(RootT/MotionT/根路径 m_LocalPosition)、
        /// prefab 上每个 Animator 的 applyRootMotion、蒙皮渲染器的 rootBone 与包围盒。
        /// </summary>
        [MenuItem("神霄/美术/诊断创角整模(输出Console)", priority = 31)]
        public static void DiagnoseCreatorModels()
        {
            var sb = new System.Text.StringBuilder("=== 创角整模诊断 ===\n");
            foreach (string dir in AssetDatabase.GetSubFolders("Assets/GameRes/object/role"))
            {
                if (!Path.GetFileName(dir).StartsWith("model_create_", StringComparison.Ordinal)) continue;
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

            DrawCareerPanel();

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
                "prefab 名形如 1300@create2 时落到 {模型目录}/model_create_1300/,否则用源文件夹名;\n" +
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
            // 每个源 prefab 所在文件夹 → 目标文件夹名(1300@create2 → model_create_1300)
            var folderMap = new Dictionary<string, string>(); // 源相对文件夹 -> 目标文件夹名
            foreach (string prefab in _sourcePrefabs)
            {
                string folderRel = Rel(plan.SourceAssetsRoot, Path.GetDirectoryName(prefab));
                string name = Path.GetFileNameWithoutExtension(prefab);
                Match m = Regex.Match(name, @"^(\d+)@");
                string target = m.Success
                    ? $"model_create_{m.Groups[1].Value}"
                    : Path.GetFileName(folderRel).ToLowerInvariant();
                folderMap[folderRel] = target;
            }

            var usedDst = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (PlannedFile f in closure)
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
                    string relAssets = Rel(plan.SourceAssetsRoot, f.Src);
                    string owner = folderMap.Keys
                        .Where(k => relAssets.StartsWith(k + "/", StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(k => k.Length)
                        .FirstOrDefault();
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

                // 角色目录根下的所有 prefab 都算"根 prefab"(挂档案+换材质),不只用户勾的那个——
                // create3 曾因只勾了 create2 而五次导入都没被接管,镜像/材质跟着漏(实锤教训)
                string srcDir = Path.GetDirectoryName(f.Src)?.Replace('\\', '/');
                bool isRootPrefab = f.Src.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) &&
                    _sourcePrefabs.Any(p => string.Equals(
                        Path.GetDirectoryName(p)?.Replace('\\', '/'), srcDir, StringComparison.OrdinalIgnoreCase));
                if (isRootPrefab) plan.RootPrefabDsts.Add(f.Dst);
                plan.Files.Add(f);
            }
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

                // 2.7 材质策略:身体材质保留 FBX 内嵌(=美术工程原样:Lit 材质+舞台补光,
                // 见 UIModelStage.StageLight)。顺手清理历史版本盲生成的 _gen_* 替身材质。
                bool cleaned = false;
                foreach (string folder in plan.RootPrefabDsts
                             .Select(p => Path.GetDirectoryName(p)?.Replace('\\', '/')).Distinct())
                {
                    cleaned |= CleanupGeneratedMaterials(folder);
                }
                if (cleaned) AssetDatabase.Refresh();

                // 2.8 PandaShader 特效材质 alpha 规范化:该 shader 混合因子是材质属性 [_Scr][_Dst],
                // alpha 通道跟着同因子写会污染展示台 RT 的覆盖度(加法特效把 alpha 加满,
                // 合成后把 UI 背景压暗)。规则:加法族(_Dst=One)不写 alpha;半透族写覆盖度。
                foreach (PlannedFile f in plan.Files
                             .Where(f => f.Dst.EndsWith(".mat", StringComparison.OrdinalIgnoreCase)))
                {
                    NormalizePandaAlpha(f.Dst);
                }
                AssetDatabase.SaveAssets();

                // 3. 渲染:独立 renderer 接进 RP Asset;prefab 根挂渲染档案
                int rendererIndex = -1;
                if (_renderMode == RenderMode.Dedicated)
                    rendererIndex = EnsureDedicatedRenderer(notes);
                if (_renderMode != RenderMode.None)
                {
                    foreach (string dst in plan.RootPrefabDsts.Distinct())
                    {
                        // 每个 prefab 用【自己动作的末帧】采样自己的落点/体量:即使 create2 与
                        // create3 的 FBX 导出单位错配(1213 实锤 2.54×),各自归一后都精确落在
                        // 原点、身高 2.33,切换依旧无缝——单位错配被自动中和,不阻塞在美术侧
                        (bool hasLanding, Vector3 landing, float scale) = SamplePrefabLanding(dst, notes);
                        string[] blendMats = AnalyzeBlendMaterials(dst, notes);
                        InjectProfile(dst, _renderMode == RenderMode.Dedicated, rendererIndex,
                            hasLanding, landing, scale, blendMats, notes);
                    }
                }

                // 4. Addressable + 台账
                if (_runAddressables)
                {
                    EditorUtility.DisplayProgressBar("导入", "Addressable 自动分组…", 0.95f);
                    AddrSetup.AddressableSetup.AutoGroupAll();
                }
                WriteLedger(plan, rendererIndex, notes);

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
            if (Path.GetFileName(f.Src).StartsWith("Pandavfx", StringComparison.OrdinalIgnoreCase) &&
                f.Src.EndsWith(".shader", StringComparison.OrdinalIgnoreCase))
            {
                // 自愈补丁:美术侧 shader 被换回原版时(实锤发生过,00:35 回滚→01:01 导入把主工程
                // 补丁覆盖没→特效全线异常),导入时自动补回 alpha 混合控制,永不再被回滚传染
                File.WriteAllText(dstAbs, EnsurePandaAlphaChannels(File.ReadAllText(f.Src)));
            }
            else if (isText && remap.Count > 0)
                File.WriteAllText(dstAbs, RemapGuids(File.ReadAllText(f.Src), remap));
            else
                File.Copy(f.Src, dstAbs, true);

            string dstMeta = dstAbs + ".meta";
            if (File.Exists(dstMeta)) File.SetAttributes(dstMeta, FileAttributes.Normal);
            string meta = File.ReadAllText(f.Src + ".meta");
            File.WriteAllText(dstMeta, remap.Count > 0 ? RemapGuids(meta, remap) : meta);
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
        /// PandaShader 材质的 alpha 混合规范化(shader 已加 [_ScrA][_DstA] 通道):
        /// 加法族(_Dst=One)不写 alpha(Zero/One),否则光团把 RT alpha 加满,预乘合成后压暗 UI 背景;
        /// 半透族(_Dst=OneMinusSrcAlpha)正常写覆盖度(One/OneMinusSrcAlpha),否则亮背景上发白。
        /// </summary>
        private static void NormalizePandaAlpha(string matPath)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null || mat.shader == null) return;
            if (!mat.shader.name.StartsWith("VFX/Pandavfx", StringComparison.OrdinalIgnoreCase)) return;
            if (!mat.HasProperty("_ScrA") || !mat.HasProperty("_DstA")) return;

            float dst = mat.HasProperty("_Dst")
                ? mat.GetFloat("_Dst") : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
            bool additive = Mathf.Approximately(dst, (float)UnityEngine.Rendering.BlendMode.One);
            mat.SetFloat("_ScrA", additive
                ? (float)UnityEngine.Rendering.BlendMode.Zero
                : (float)UnityEngine.Rendering.BlendMode.One);
            mat.SetFloat("_DstA", additive
                ? (float)UnityEngine.Rendering.BlendMode.One
                : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
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
        /// 末帧,BakeMesh 紧致盒量出脚底中心与身高。要点:
        /// ① 不能用静态包围盒猜——嵌套 FBX 的默认姿势是绑定姿势,和动画停放点不是一回事;
        /// ② 每个 prefab 采自己的动作(1213@create2 采 create2 末帧),而不是共用 create3——
        ///    这样 create2/create3 即使 FBX 单位错配(1213 实锤 2.54×)也各自归一,切换无缝。
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
            bool hasLanding, Vector3 landing, float landingScale, string[] blendMaterials, List<string> notes)
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

        private void WriteLedger(Plan plan, int rendererIndex, List<string> notes)
        {
            Ledger ledger = File.Exists(LedgerPath)
                ? JsonUtility.FromJson<Ledger>(File.ReadAllText(LedgerPath)) ?? new Ledger()
                : new Ledger();
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
                notes = notes,
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
            ledger.runs.Add(run);
            Directory.CreateDirectory(Path.GetDirectoryName(LedgerPath) ?? ".");
            File.WriteAllText(LedgerPath, JsonUtility.ToJson(ledger, true));
            AssetDatabase.ImportAsset(LedgerPath);
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
