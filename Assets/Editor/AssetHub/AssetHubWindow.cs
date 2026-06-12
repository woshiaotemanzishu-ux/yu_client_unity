using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shenxiao.Editor.Laya3D;
using Shenxiao.Editor.LayaUI;
using Shenxiao.EditorTools.AddrSetup;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.AssetHub
{
    /// <summary>
    /// 资产管理(Falcon 式三栏):左=资源域 / 中=配置表清单+状态+搜索 / 右=详情+操作+预览。
    /// 定位:配置表只读(清单真相源),工具管资源——转换/重转/批量/定位/删除/预览。
    /// 配表编辑走配表线(ConfigManager),不在此工具;新美术 FBX 线就绪后在详情页加「换源」入口。
    /// </summary>
    public class AssetHubWindow : EditorWindow
    {
        private List<AssetDomain> _domains;
        private int _domainIndex;
        private List<AssetEntry> _entries = new List<AssetEntry>();
        private readonly Dictionary<string, EntryStatus> _status = new Dictionary<string, EntryStatus>();
        private string _scanError;

        private string _search = "";
        private Vector2 _listScroll;
        private Vector2 _detailScroll;
        private AssetEntry _selected;

        // 动作选择:按条目 Id 记用户勾选;没勾过默认全部(clip 共享增量,不重复花钱)
        private readonly Dictionary<string, List<string>> _laniChoice = new Dictionary<string, List<string>>();
        private readonly Dictionary<string, string[]> _laniDirCache = new Dictionary<string, string[]>();
        private bool _laniFoldout;
        private Laya3DImporter.MaterialMode _materialMode = Laya3DImporter.MaterialMode.Unlit;

        private readonly AssetHubPreview _preview = new AssetHubPreview();
        private AssetHubEffects.EffectInfo _effects;
        private string _effectsForId;
        private Vector2 _clipScroll;

        private static GUIStyle _rowStyle;
        private static GUIStyle RowStyle => _rowStyle ??= new GUIStyle(EditorStyles.label)
        { richText = true, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(4, 4, 0, 0) };

        [MenuItem("神霄/资产管理", priority = 2)]
        public static void Open()
        {
            var w = GetWindow<AssetHubWindow>("资产管理");
            w.minSize = new Vector2(900f, 520f);
        }

        private void OnEnable()
        {
            _domains = AssetHubDomains.Build();
            RefreshDomain();
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            _preview.Dispose();
        }

        private void OnEditorUpdate()
        {
            if (_preview.Playing != null) Repaint(); // 动画播放期间持续重绘
        }

        private void OnFocus()
        {
            if (_domains != null) RefreshStatus();
        }

        // ================= 数据 =================

        private void RefreshDomain()
        {
            _entries.Clear();
            _selected = null;
            _scanError = null;
            _effectsForId = null;
            _preview.SetPrefab(null);
            AssetDomain d = _domains[_domainIndex];
            if (!d.Enabled) return;
            try { _entries = d.Scan(); }
            catch (System.Exception e) { _scanError = e.Message; return; }
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            _status.Clear();
            foreach (AssetEntry e in _entries) _status[e.Id] = AssetHubDomains.GetStatus(e);
        }

        private EntryStatus StatusOf(AssetEntry e)
        {
            return _status.TryGetValue(e.Id, out EntryStatus s) ? s : EntryStatus.SourceMissing;
        }

        private string[] LaniFiles(AssetEntry e)
        {
            if (!_laniDirCache.TryGetValue(e.ActionDir, out string[] files))
            {
                files = Directory.Exists(e.ActionDir)
                    ? Directory.GetFiles(e.ActionDir, "*.lani").OrderBy(f => f).ToArray()
                    : System.Array.Empty<string>();
                _laniDirCache[e.ActionDir] = files;
            }
            return files;
        }

        /// <summary>该条目要转哪些动作:用户勾过用勾选,否则默认全部
        /// (clip 共享存放、增量生成,转全不重复花钱;老客户端一个角色十几个动作都要)。</summary>
        private List<string> LanisFor(AssetEntry e)
        {
            if (_laniChoice.TryGetValue(e.Id, out List<string> picked) && picked.Count > 0)
                return picked;
            return new List<string>(LaniFiles(e));
        }

        // ================= 转换 =================

        private void ConvertEntries(List<AssetEntry> targets)
        {
            if (targets.Count == 0) return;
            var failed = new List<string>();
            try
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    AssetEntry e = targets[i];
                    if (EditorUtility.DisplayCancelableProgressBar("Laya3D 批量转换",
                            $"({i + 1}/{targets.Count}) {System.IO.Path.GetFileNameWithoutExtension(e.LhPath)} {e.DisplayName}",
                            (float)i / targets.Count))
                        break;
                    // mirrorX=false(v4):几何镜像路径有蒙皮 bug 已撤回,
                    // 与老客户端的朝向差异由 UIModelStage 渲染层水平翻转补偿
                    Laya3DImporter.Result r = Laya3DImporter.Convert(e.LhPath, LanisFor(e), mirrorX: false, _materialMode);
                    if (!r.Ok) failed.Add($"{e.Id} {e.DisplayName}");
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (LayaUISettings.AutoGroupAfterConvert)
            {
                try { AddressableSetup.AutoGroupAll(); }
                catch (System.Exception e) { Debug.LogWarning("[AssetHub] Addressable 分组失败: " + e.Message); }
            }
            RefreshStatus();
            _preview.SetPrefab(null); // 产物已重建,下次绘制重新加载

            string msg = failed.Count == 0
                ? $"完成 {targets.Count} 个。"
                : $"完成 {targets.Count - failed.Count}/{targets.Count},失败(看 Console):\n" + string.Join("\n", failed);
            EditorUtility.DisplayDialog("转换结果", msg, "好");
        }

        // ================= UI =================

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawDomainPane();
                DrawListPane();
                DrawDetailPane();
            }
        }

        private void DrawDomainPane()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(150f)))
            {
                EditorGUILayout.LabelField("资源域", EditorStyles.boldLabel);
                for (int i = 0; i < _domains.Count; i++)
                {
                    AssetDomain d = _domains[i];
                    using (new EditorGUI.DisabledScope(!d.Enabled))
                    {
                        bool on = GUILayout.Toggle(_domainIndex == i,
                            new GUIContent(d.Name, d.Enabled ? "" : d.DisabledNote), "Button", GUILayout.Height(26f));
                        if (on && _domainIndex != i)
                        {
                            _domainIndex = i;
                            RefreshDomain();
                        }
                    }
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("yu_client:", EditorStyles.miniLabel);
                EditorGUILayout.LabelField(LayaUISettings.ClientRoot, EditorStyles.wordWrappedMiniLabel);
                if (GUILayout.Button("改路径...", GUILayout.Height(20f)))
                {
                    string p = EditorUtility.OpenFolderPanel("选 yu_client 仓库根目录", LayaUISettings.ClientRoot, "");
                    if (!string.IsNullOrEmpty(p)) { LayaUISettings.ClientRoot = p; RefreshDomain(); }
                }
            }
        }

        private void DrawListPane()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(330f)))
            {
                if (_scanError != null)
                {
                    EditorGUILayout.HelpBox("清单扫描失败(检查 yu_client 路径):\n" + _scanError, MessageType.Error);
                    return;
                }
                if (!_domains[_domainIndex].Enabled)
                {
                    EditorGUILayout.HelpBox(_domains[_domainIndex].DisabledNote, MessageType.Info);
                    return;
                }

                _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
                List<AssetEntry> shown = string.IsNullOrEmpty(_search)
                    ? _entries
                    : _entries.Where(e => e.SearchText.Contains(_search.ToLowerInvariant())).ToList();

                int converted = _entries.Count(e => StatusOf(e) == EntryStatus.Converted);
                var pending = _entries.Where(e =>
                    StatusOf(e) == EntryStatus.NotConverted || StatusOf(e) == EntryStatus.Stale).ToList();
                EditorGUILayout.LabelField($"已转 {converted}/{_entries.Count},待转/过期 {pending.Count}", EditorStyles.miniLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(pending.Count == 0))
                    {
                        if (GUILayout.Button($"转换缺失+过期({pending.Count})", GUILayout.Height(24f)))
                            ConvertEntries(pending);
                    }
                    var all = _entries.Where(e =>
                        StatusOf(e) != EntryStatus.SourceMissing && StatusOf(e) != EntryStatus.SourceLfs).ToList();
                    if (GUILayout.Button($"全部重转({all.Count})", GUILayout.Height(24f))
                        && EditorUtility.DisplayDialog("全部重转", $"重转 {all.Count} 个模型,产物覆盖。继续?", "转", "算了"))
                        ConvertEntries(all);
                }

                _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
                foreach (AssetEntry e in shown)
                {
                    string careerTag = e.Career > 0
                        ? $"  <color=#888888>{AssetHubDomains.CAREER_NAMES[Mathf.Clamp(e.Career, 0, 4)]}</color>"
                        : "";
                    string label = $"{AssetHubDomains.StatusIcon(StatusOf(e))} {e.Id}  {e.DisplayName}{careerTag}";
                    Rect row = GUILayoutUtility.GetRect(10f, 22f, GUILayout.ExpandWidth(true));
                    if (_selected == e)
                        EditorGUI.DrawRect(row, new Color(0.24f, 0.49f, 0.91f, 0.35f));
                    if (GUI.Button(row, label, RowStyle) && _selected != e)
                    {
                        _selected = e;
                        _laniFoldout = false;
                        GUI.FocusControl(null);
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawDetailPane()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                if (_selected == null)
                {
                    EditorGUILayout.HelpBox("左边选一个条目。", MessageType.Info);
                    return;
                }
                AssetEntry e = _selected;
                EntryStatus s = StatusOf(e);

                _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
                EditorGUILayout.LabelField($"{Path.GetFileNameWithoutExtension(e.LhPath)}  {e.DisplayName}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("状态", $"{AssetHubDomains.StatusIcon(s)} {AssetHubDomains.StatusText(s)}");
                if (e.Career > 0)
                    EditorGUILayout.LabelField("职业/性别",
                        $"{AssetHubDomains.CAREER_NAMES[Mathf.Clamp(e.Career, 0, 4)]} / {(e.Sex == 2 ? "女" : "男")}");
                if (!string.IsNullOrEmpty(e.Note))
                    EditorGUILayout.LabelField("配置来源", e.Note, EditorStyles.wordWrappedLabel);
                EditorGUILayout.LabelField("源", e.LhPath, EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField("产物", e.PrefabPath, EditorStyles.wordWrappedMiniLabel);

                DrawClipsSection(e, s);
                DrawAlwaysEffects(e);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);
                DrawLaniChoice(e);
                _materialMode = (Laya3DImporter.MaterialMode)EditorGUILayout.EnumPopup(
                    new GUIContent("材质模式", "Unlit=对标老客户端贴图直出(默认);Lit=受场景光照"), _materialMode);

                using (new EditorGUILayout.HorizontalScope())
                {
                    bool canConvert = s != EntryStatus.SourceMissing && s != EntryStatus.SourceLfs;
                    using (new EditorGUI.DisabledScope(!canConvert))
                    {
                        if (GUILayout.Button(s == EntryStatus.Converted || s == EntryStatus.Stale ? "重转" : "转换", GUILayout.Height(26f)))
                            ConvertEntries(new List<AssetEntry> { e });
                    }
                    using (new EditorGUI.DisabledScope(s != EntryStatus.Converted && s != EntryStatus.Stale))
                    {
                        if (GUILayout.Button("定位产物", GUILayout.Height(26f)))
                        {
                            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(e.PrefabPath);
                            EditorGUIUtility.PingObject(prefab);
                            Selection.activeObject = prefab;
                        }
                        if (GUILayout.Button("删产物", GUILayout.Height(26f))
                            && EditorUtility.DisplayDialog("删除产物", e.OutDir + "\n整个目录删除(源不动,可随时重转)。", "删", "算了"))
                        {
                            AssetDatabase.DeleteAsset(e.OutDir);
                            RefreshStatus();
                            _preview.SetPrefab(null);
                        }
                    }
                    if (GUILayout.Button("源目录", GUILayout.Height(26f)))
                        EditorUtility.RevealInFinder(e.LhPath);
                }
                EditorGUILayout.EndScrollView();

                // 可播放预览(拖拽旋转/滚轮缩放;点上面的动作行即播)
                _preview.SetPrefab(s == EntryStatus.Converted || s == EntryStatus.Stale ? e.PrefabPath : null);
                Rect previewRect = GUILayoutUtility.GetRect(200f, 280f, GUILayout.ExpandWidth(true));
                _preview.OnGUI(previewRect);
            }
        }

        /// <summary>动作清单:产物里的 clip,点 ▶ 在下方预览台播放;✨n=该动作挂 n 条特效(悬停看明细)。</summary>
        private void DrawClipsSection(AssetEntry e, EntryStatus s)
        {
            if (s != EntryStatus.Converted && s != EntryStatus.Stale) return;
            AnimationClip[] clips = _preview.Clips;
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField($"动作({clips.Length})", EditorStyles.boldLabel);
            if (clips.Length == 0)
            {
                EditorGUILayout.LabelField("产物无动作(转换时未勾 .lani 或目录为空)", EditorStyles.miniLabel);
                return;
            }
            AssetHubEffects.EffectInfo fx = EffectsOf(e);
            _clipScroll = EditorGUILayout.BeginScrollView(_clipScroll,
                GUILayout.Height(Mathf.Min(clips.Length, 6) * 22f + 8f));
            foreach (AnimationClip clip in clips)
            {
                bool playing = _preview.Playing == clip;
                string badge = "";
                string tooltip = "";
                if (fx.Actions.TryGetValue(clip.name, out var list))
                {
                    badge = $"  ✨{list.Count}";
                    tooltip = string.Join("\n", list.Select(r =>
                        $"{AssetHubEffects.StateIcon(r.State)} {r.Bone} → {r.Name}"));
                }
                var content = new GUIContent($"{(playing ? "■" : "▶")} {clip.name}{badge}  ({clip.length:0.##}s)", tooltip);
                if (GUILayout.Button(content, playing ? EditorStyles.boldLabel : EditorStyles.label, GUILayout.Height(20f)))
                {
                    _preview.Play(playing ? null : clip);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        /// <summary>特效盘点:SceneObjectParticle(常驻/动作)+ ConfigLogin.CreateRole(创角/套装武器)。</summary>
        private void DrawAlwaysEffects(AssetEntry e)
        {
            AssetHubEffects.EffectInfo fx = EffectsOf(e);
            if (!fx.Supported) return;
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                $"特效映射(SceneObjectParticle.{fx.Section}:常驻 {fx.Always.Count} / 动作 {fx.ActionEffectTotal})",
                EditorStyles.boldLabel);
            if (fx.IsEmpty)
            {
                EditorGUILayout.LabelField(
                    "  该模型本体无特效记录(正常:Body 节只有少数时装有;展示线特效多在武器/翅膀/创角特效上)",
                    EditorStyles.wordWrappedMiniLabel);
                return;
            }
            foreach (AssetHubEffects.EffectRef r in fx.Always)
                EditorGUILayout.LabelField(
                    $"  常驻 {AssetHubEffects.StateIcon(r.State)} {r.Bone} → {r.Name}", EditorStyles.miniLabel);
            foreach (AssetHubEffects.EffectRef r in fx.CreateEffects)
                EditorGUILayout.LabelField(
                    $"  创角 {AssetHubEffects.StateIcon(r.State)} {r.Bone} → {r.Name}(skills_effect,创角页挂衣服骨骼)",
                    EditorStyles.miniLabel);
            foreach (AssetHubEffects.EffectRef r in fx.SetWeapon)
                EditorGUILayout.LabelField(
                    $"  套装武器 {AssetHubEffects.StateIcon(r.State)} {r.Bone} → {r.Name}(挂在武器模型上,见武器域)",
                    EditorStyles.miniLabel);
            if (fx.Variants.Count > 0)
                EditorGUILayout.LabelField("  变体键: " + string.Join(", ", fx.Variants), EditorStyles.miniLabel);
            EditorGUILayout.LabelField("  (特效 .lh=Laya 粒子,Unity 侧渲染待特效转换线;Electron 预览可直接看)", EditorStyles.miniLabel);
        }

        private AssetHubEffects.EffectInfo EffectsOf(AssetEntry e)
        {
            if (_effectsForId != e.Id || _effects == null)
            {
                _effects = AssetHubEffects.Query(e);
                _effectsForId = e.Id;
            }
            return _effects;
        }

        private void DrawLaniChoice(AssetEntry e)
        {
            List<string> current = LanisFor(e);
            string names = string.Join(",", current.Take(5).Select(Path.GetFileNameWithoutExtension));
            if (current.Count > 5) names += " …";
            _laniFoldout = EditorGUILayout.Foldout(_laniFoldout,
                $"转换动作({current.Count} 个:{names})", true);
            if (!_laniFoldout) return;

            string[] files = LaniFiles(e);
            if (files.Length == 0)
            {
                EditorGUILayout.HelpBox("动作目录无 .lani:" + e.ActionDir, MessageType.Warning);
                return;
            }
            if (!_laniChoice.TryGetValue(e.Id, out List<string> picked))
            {
                picked = new List<string>(current);
                _laniChoice[e.Id] = picked;
            }
            using (new EditorGUI.IndentLevelScope())
            {
                foreach (string f in files)
                {
                    bool on = picked.Contains(f);
                    bool now = EditorGUILayout.ToggleLeft(Path.GetFileName(f), on);
                    if (now && !on) picked.Add(f);
                    else if (!now && on) picked.Remove(f);
                }
            }
        }

    }
}
