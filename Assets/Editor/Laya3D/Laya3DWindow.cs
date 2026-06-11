using System.Collections.Generic;
using System.IO;
using Shenxiao.Editor.LayaUI;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.Laya3D
{
    /// <summary>
    /// Laya 3D 转换器(MVP):验证单模型 .lh/.lm/.lani → Unity 原生资产。
    /// 第一验收点 = model_clothe_1201(已知能渲染参考)渲染 + 待机动画。
    /// 验收通过后再做批量(配置表清单)与可视化资产管理(替换/增删)。
    /// </summary>
    public class Laya3DWindow : EditorWindow
    {
        private string _lhPath = "";
        private string _actionDir = "";
        private readonly List<string> _selectedLanis = new List<string>();
        private Vector2 _laniScroll;
        private bool _mirrorX;
        private Laya3DImporter.MaterialMode _materialMode = Laya3DImporter.MaterialMode.Unlit;
        private string[] _laniFiles = System.Array.Empty<string>();

        [MenuItem("神霄/Laya3D 转换器(MVP)", priority = 1)]
        public static void Open()
        {
            var w = GetWindow<Laya3DWindow>("Laya3D 转换器");
            w.minSize = new Vector2(520f, 420f);
        }

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(_lhPath))
            {
                _lhPath = Path.Combine(LayaUISettings.ClientRoot,
                    "cdn", "resource", "object", "role", "objs", "model_clothe_1201.lh");
            }
            if (string.IsNullOrEmpty(_actionDir))
            {
                _actionDir = Path.Combine(LayaUISettings.ClientRoot,
                    "cdn", "resource", "object", "role", "action", "1200");
                RefreshLanis();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "MVP:先验证一个模型(默认 model_clothe_1201,已知能渲染的参考)。\n" +
                "首渲若左右镜像/面片翻面 → 勾选 mirrorX 重转(坐标系开关,验收期定死后固化)。",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                _lhPath = EditorGUILayout.TextField(".lh 模型", _lhPath);
                if (GUILayout.Button("选...", GUILayout.Width(44f)))
                {
                    string p = EditorUtility.OpenFilePanel("选 .lh", Path.GetDirectoryName(_lhPath), "lh");
                    if (!string.IsNullOrEmpty(p)) _lhPath = p;
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _actionDir = EditorGUILayout.TextField("动作目录", _actionDir);
                if (GUILayout.Button("选...", GUILayout.Width(44f)))
                {
                    string p = EditorUtility.OpenFolderPanel("选动作目录(.lani)", _actionDir, "");
                    if (!string.IsNullOrEmpty(p)) { _actionDir = p; RefreshLanis(); }
                }
                if (GUILayout.Button("刷新", GUILayout.Width(44f))) RefreshLanis();
            }

            EditorGUILayout.LabelField($"动作({_selectedLanis.Count}/{_laniFiles.Length} 选中,MVP 建议只勾 1~2 个待机类)");
            _laniScroll = EditorGUILayout.BeginScrollView(_laniScroll, GUILayout.Height(160f));
            foreach (string f in _laniFiles)
            {
                bool on = _selectedLanis.Contains(f);
                bool now = EditorGUILayout.ToggleLeft(Path.GetFileName(f), on);
                if (now && !on) _selectedLanis.Add(f);
                else if (!now && on) _selectedLanis.Remove(f);
            }
            EditorGUILayout.EndScrollView();

            _mirrorX = EditorGUILayout.ToggleLeft("mirrorX(坐标系翻转,首渲镜像时切换)", _mirrorX);
            _materialMode = (Laya3DImporter.MaterialMode)EditorGUILayout.EnumPopup(
                new GUIContent("材质模式", "Unlit=对标老客户端(UnlitMaterial 贴图直出,不吃光照不发黑,默认);Lit=URP SimpleLit 受场景光照"), _materialMode);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("仅解析(干跑校验,不产资产)", GUILayout.Height(28f)))
                {
                    DryRun();
                }
                if (GUILayout.Button("转换生成 Prefab", GUILayout.Height(28f)))
                {
                    var result = Laya3DImporter.Convert(_lhPath, new List<string>(_selectedLanis), _mirrorX, _materialMode);
                    if (result.Ok)
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(result.PrefabPath);
                        EditorGUIUtility.PingObject(prefab);
                        Selection.activeObject = prefab;
                    }
                }
            }
        }

        private void RefreshLanis()
        {
            _selectedLanis.Clear();
            _laniFiles = Directory.Exists(_actionDir)
                ? Directory.GetFiles(_actionDir, "*.lani")
                : System.Array.Empty<string>();
            System.Array.Sort(_laniFiles);
            // 默认勾选名字里带 stand/idle 的待机动作
            foreach (string f in _laniFiles)
            {
                string n = Path.GetFileName(f).ToLowerInvariant();
                if (n.Contains("stand") || n.Contains("idle"))
                {
                    _selectedLanis.Add(f);
                    break;
                }
            }
        }

        private void DryRun()
        {
            try
            {
                LhDocument lh = LhDocument.Load(_lhPath);
                Debug.Log($"[Laya3D] .lh: root={lh.RootName} 骨骼={lh.Bones.Count} mesh={lh.MeshPath} 材质={lh.MaterialPaths.Count}");
                string lmPath = lh.ResolveAssetPath(lh.MeshPath);
                if (lmPath == null) { Debug.LogError("[Laya3D] .lm 找不到: " + lh.MeshPath); return; }
                LmMesh lm = LmParser.Parse(File.ReadAllBytes(lmPath));
                Debug.Log($"[Laya3D] .lm: 顶点={lm.VertexCount} 索引={lm.IndexData.Length} flag={lm.VertexFlag} " +
                          $"骨骼={lm.BoneNames.Count} 逆绑定={lm.InverseBindPoses.Count} 包围盒={lm.BoundsMin}~{lm.BoundsMax}");
                foreach (string laniPath in _selectedLanis)
                {
                    LaniClip clip = LaniParser.Parse(File.ReadAllBytes(laniPath));
                    Debug.Log($"[Laya3D] .lani {Path.GetFileName(laniPath)}: name={clip.Name} 时长={clip.Duration:0.##}s " +
                              $"帧率={clip.FrameRate} 轨={clip.Nodes.Count} 循环={clip.IsLooping}");
                }
                Debug.Log("[Laya3D] 干跑通过 ✅(数值合理即可点「转换生成 Prefab」)");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Laya3D] 干跑失败: " + e.Message + "\n" + e.StackTrace);
            }
        }
    }
}
