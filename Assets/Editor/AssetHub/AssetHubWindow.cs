using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shenxiao.Common.UI3D;
using Shenxiao.Editor.Laya3D;
using Shenxiao.Editor.LayaUI;
using Shenxiao.EditorTools.AddrSetup;
using Shenxiao.Framework.Res;
using UnityEditor;
using UnityEditor.AddressableAssets;
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
        private enum HubMode { Library, Assembly }

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
        private readonly AssetAssemblyPreview _assemblyPreview = new AssetAssemblyPreview();
        private AssetHubEffects.EffectInfo _effects;
        private string _effectsForId;
        private Vector2 _clipScroll;
        private HubMode _mode;
        private AssetAssemblyEntry _draftAssembly;
        private string _draftProfileId;
        private bool _draftHasSavedProfile;
        private bool _draftDirty;
        private string _assemblyActionName;
        private List<AssetAssemblySet> _assemblySets;
        private AssetAssemblySet _selectedAssemblySet;
        private Vector2 _assemblyListScroll;
        private Vector2 _assemblyDetailScroll;

        private static GUIStyle _rowStyle;
        private static GUIStyle RowStyle => _rowStyle ??= new GUIStyle(EditorStyles.label)
        { richText = true, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(4, 4, 0, 0) };

        [MenuItem("神霄/资产管理", priority = 2)]
        public static void Open()
        {
            var w = GetWindow<AssetHubWindow>("资产管理");
            w.minSize = new Vector2(900f, 520f);
        }

        /// <summary>独立入口:直达「特效」域(特效导入/管理/转换/预览都在这)。</summary>
        [MenuItem("神霄/资源/特效管理", priority = 22)]
        public static void OpenEffects()
        {
            var w = GetWindow<AssetHubWindow>("资产管理");
            w.minSize = new Vector2(900f, 520f);
            w.SelectDomainByName("特效");
        }

        /// <summary>按域名定位左栏(供独立菜单入口用)。</summary>
        public void SelectDomainByName(string name)
        {
            if (_domains == null) _domains = AssetHubDomains.Build();
            for (int i = 0; i < _domains.Count; i++)
            {
                if (_domains[i].Name == name) { _domainIndex = i; RefreshDomain(); break; }
            }
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
            _assemblyPreview.Dispose();
        }

        private void OnEditorUpdate()
        {
            if (_preview.Playing != null || _preview.HasParticles) Repaint(); // 动画/粒子播放期间持续重绘
            if (_mode != HubMode.Library) Repaint();
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
            _draftAssembly = null;
            _draftProfileId = null;
            _draftDirty = false;
            _assemblyPreview.SetModel(null);
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
            if (!EnsureConvertCanBeVerified("Laya3D 资源转换")) return;
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
                    bool ok;
                    if (e.Kind == AssetKind.Effect)
                    {
                        ok = LayaEffectImporter.Convert(e.LhPath).Ok;
                    }
                    else
                    {
                        // mirrorX=false(v4):几何镜像路径有蒙皮 bug 已撤回,
                        // 与老客户端的朝向差异由 UIModelStage 渲染层水平翻转补偿
                        ok = Laya3DImporter.Convert(e.LhPath, LanisFor(e), mirrorX: false, _materialMode).Ok;
                    }
                    if (!ok) failed.Add($"{e.Id} {e.DisplayName}");
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

        private void ResetRuntimeEditableEntries(List<AssetEntry> targets)
        {
            targets = targets.Where(e => e.Kind == AssetKind.Effect).ToList();
            if (targets.Count == 0) return;
            if (!EnsureConvertCanBeVerified("重置 Runtime Editable")) return;

            var failed = new List<string>();
            try
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    AssetEntry e = targets[i];
                    if (EditorUtility.DisplayCancelableProgressBar("重置 Runtime Editable",
                            $"({i + 1}/{targets.Count}) {System.IO.Path.GetFileNameWithoutExtension(e.LhPath)} {e.DisplayName}",
                            (float)i / targets.Count))
                        break;

                    if (!LayaEffectImporter.ResetRuntimeEditable(e.LhPath).Ok)
                        failed.Add($"{e.Id} {e.DisplayName}");
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
            _preview.SetPrefab(null);

            string msg = failed.Count == 0
                ? $"完成 {targets.Count} 个 Runtime Editable 重置。"
                : $"完成 {targets.Count - failed.Count}/{targets.Count},失败(看 Console):\n" + string.Join("\n", failed);
            EditorUtility.DisplayDialog("Runtime Editable 重置结果", msg, "好");
        }

        private static bool EnsureConvertCanBeVerified(string title)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(title,
                    "请先停止 Play Mode 再转换资源。运行时已经加载的 Addressables 资产和材质实例不会被 AssetDatabase.Refresh 刷新。",
                    "OK");
                return false;
            }

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return true;

            int index = settings.ActivePlayModeDataBuilderIndex;
            var builder = settings.GetDataBuilder(index);
            string builderName = builder != null ? builder.Name : "<none>";
            string builderType = builder != null ? builder.GetType().Name : "<none>";
            if (builderType == "BuildScriptFastMode") return true;

            string message =
                "当前 Addressables Play Mode 是 '" + builderName + "'。\n\n" +
                "转换/分组后如果要立刻在 Editor 里看效果，请切到 'Use Asset Database (fastest)'；" +
                "如果继续使用当前模式，必须重新 Build Addressables，否则运行时会继续加载旧 bundle。\n\n" +
                "是否继续转换？";
            return EditorUtility.DisplayDialog(title, message, "继续转换", "取消");
        }

        // ================= UI =================

        private void OnGUI()
        {
            DrawModeToolbar();
            if (_mode == HubMode.Library)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawDomainPane();
                    DrawListPane();
                    DrawDetailPane();
                }
                return;
            }
            DrawAssemblyEditor();
        }

        private void DrawModeToolbar()
        {
            string[] labels = { "资源库", "装配编辑" };
            HubMode next = (HubMode)GUILayout.Toolbar((int)_mode, labels, GUILayout.Height(26f));
            if (next == _mode) return;
            _mode = next;
            _preview.SetPrefab(null);
            if (_mode == HubMode.Assembly)
                EnsureAssemblySetSelected();
        }

        private void DrawAssemblyEditor()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawAssemblyListPane();
                DrawAssemblyPreviewPane();
                DrawAssemblyDetailPane();
            }
        }

        private void DrawAssemblyListPane()
        {
            EnsureAssemblyCatalog();
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(300f)))
            {
                EditorGUILayout.LabelField("模型套装", EditorStyles.boldLabel);
                if (_assemblySets == null || _assemblySets.Count == 0)
                {
                    EditorGUILayout.HelpBox("没有从 ConfigLogin.CreateRole 推导到默认套装。", MessageType.Warning);
                    return;
                }

                _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
                string needle = (_search ?? string.Empty).ToLowerInvariant();
                List<AssetAssemblySet> shown = _assemblySets
                    .Where(s => string.IsNullOrEmpty(needle)
                        || s.ProfileId.ToLowerInvariant().Contains(needle)
                        || s.Label.ToLowerInvariant().Contains(needle)
                        || s.BusinessKey.Contains(needle)
                        || s.ActionGroup.Contains(needle))
                    .ToList();

                EditorGUILayout.LabelField($"角色默认套装 {shown.Count}/{_assemblySets.Count}", EditorStyles.miniLabel);
                _assemblyListScroll = EditorGUILayout.BeginScrollView(_assemblyListScroll);
                foreach (IGrouping<string, AssetAssemblySet> group in shown.GroupBy(s => s.ActionGroup).OrderBy(g => g.Key))
                {
                    string careerName = CareerName(group.First().Career);
                    EditorGUILayout.LabelField(group.Key + " " + careerName + "动作组", EditorStyles.miniBoldLabel);
                    foreach (AssetAssemblySet set in group)
                    {
                        bool hasProfile = AssetAssemblyProfileStore.Load(set.ProfileId) != null;
                        string label = $"{(hasProfile ? "■" : "□")} {set.BusinessKey}  {set.Label}  role_res={set.RoleRes}";
                        Rect row = GUILayoutUtility.GetRect(10f, 22f, GUILayout.ExpandWidth(true));
                        if (_selectedAssemblySet == set)
                            EditorGUI.DrawRect(row, new Color(0.24f, 0.49f, 0.91f, 0.35f));
                        if (GUI.Button(row, label, RowStyle) && _selectedAssemblySet != set)
                        {
                            if (!CanSwitchAssemblySelection()) continue;
                            _selectedAssemblySet = set;
                            _selected = set.Entry;
                            GUI.FocusControl(null);
                            LoadAssemblyDraft(set);
                        }
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void EnsureAssemblyCatalog()
        {
            if (_assemblySets != null) return;
            _assemblySets = AssetAssemblyCatalog.BuildDefaultRoleSets();
        }

        private void EnsureAssemblySetSelected()
        {
            EnsureAssemblyCatalog();
            if (_selectedAssemblySet != null || _assemblySets == null || _assemblySets.Count == 0) return;
            _selectedAssemblySet = _assemblySets.FirstOrDefault(s => s.BusinessKey == "1@1") ?? _assemblySets[0];
            _selected = _selectedAssemblySet.Entry;
            LoadAssemblyDraft(_selectedAssemblySet);
        }

        private static string CareerName(int career)
        {
            if (career >= 0 && career < AssetHubDomains.CAREER_NAMES.Length)
                return AssetHubDomains.CAREER_NAMES[career];
            return "?";
        }

        private void DrawAssemblyPreviewPane()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(360f)))
            {
                EditorGUILayout.LabelField("预览", EditorStyles.boldLabel);
                if (_draftAssembly == null)
                {
                    EditorGUILayout.HelpBox("左边选一个模型套装。", MessageType.Info);
                    return;
                }

                EnsureAssemblyCollections(_draftAssembly);
                EditorGUILayout.LabelField(_draftAssembly.Label ?? _draftProfileId, EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField("动作场景", _draftAssembly.DefaultActionContext ?? "", EditorStyles.miniLabel);
                string[] actions = _draftAssembly.Actions.Keys.OrderBy(k => k).ToArray();
                if (actions.Length > 0)
                {
                    int index = System.Array.IndexOf(actions, _assemblyActionName);
                    if (index < 0) index = 0;
                    int next = EditorGUILayout.Popup("动作", index, actions);
                    if (next != index || string.IsNullOrEmpty(_assemblyActionName))
                    {
                        _assemblyActionName = actions[next];
                        PlayAssemblyPreviewAction();
                    }
                    if (GUILayout.Button("播放当前动作", GUILayout.Height(24f)))
                        PlayAssemblyPreviewAction();
                }
                else
                {
                    EditorGUILayout.HelpBox("当前动作场景没有业务动作槽。", MessageType.Warning);
                }

                Rect previewRect = GUILayoutUtility.GetRect(220f, 300f, GUILayout.ExpandWidth(true));
                _assemblyPreview.OnGUI(previewRect);

                List<string> issues = ValidateAssembly(_draftAssembly);
                if (issues.Count == 0)
                {
                    EditorGUILayout.HelpBox("校验通过:模型、动作、特效资源和挂点存在。", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox(string.Join("\n", issues.Take(8)) +
                        (issues.Count > 8 ? $"\n... 还有 {issues.Count - 8} 项" : ""), MessageType.Warning);
                }
            }
        }

        private void DrawAssemblyDetailPane()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                if (_selected == null || _draftAssembly == null)
                {
                    EditorGUILayout.HelpBox("左边选一个模型套装。", MessageType.Info);
                    return;
                }

                _assemblyDetailScroll = EditorGUILayout.BeginScrollView(_assemblyDetailScroll);
                EditorGUILayout.LabelField($"{_draftProfileId}  {_draftAssembly.Label}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("方案状态", _draftHasSavedProfile ? "已保存覆盖方案" : "默认推导(未保存)");
                if (_draftDirty) EditorGUILayout.HelpBox("当前有未保存修改。", MessageType.Warning);

                DrawAssemblyModelSection();
                DrawAssemblyActionsSection();
                DrawAssemblyEffectsSection("常驻特效", _draftAssembly.AlwaysEffects, null);
                DrawAssemblyActionEffectsSection();
                EditorGUILayout.EndScrollView();

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(!_draftDirty))
                    {
                        if (GUILayout.Button("保存方案", GUILayout.Height(28f))) SaveAssemblyDraft();
                    }
                    if (GUILayout.Button("还原默认", GUILayout.Height(28f))
                        && EditorUtility.DisplayDialog("还原默认", "丢弃当前草稿,按老配置/转换产物重新推导。继续?", "还原", "取消"))
                    {
                        _draftAssembly = BuildDefaultAssembly(_selectedAssemblySet);
                        _draftDirty = true;
                        SelectFirstActionIfNeeded();
                        RefreshAssemblyPreview();
                    }
                    using (new EditorGUI.DisabledScope(!_draftHasSavedProfile))
                    {
                        if (GUILayout.Button("删除覆盖", GUILayout.Height(28f))
                            && EditorUtility.DisplayDialog("删除覆盖", _draftProfileId + "\n删除后运行时回退旧规则。", "删除", "取消"))
                        {
                            AssetAssemblyProfileStore.Delete(_draftProfileId);
                            AssetAssemblyProfiles.Invalidate();
                            _draftHasSavedProfile = false;
                            _draftDirty = false;
                        }
                    }
                }
            }
        }

        private void DrawAssemblyModelSection()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("套装信息", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("业务Key", _draftAssembly.BusinessKey ?? "", GUILayout.Width(170f));
                EditorGUILayout.LabelField("动作组", _draftAssembly.ActionGroup ?? "");
            }
            EditorGUILayout.LabelField("动作场景", _draftAssembly.DefaultActionContext ?? "");
            if (GUILayout.Button("替换套装(模型+动作组+特效)", GUILayout.Height(24f)))
                PickAssemblySetReplacement();

            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField("模型", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.SelectableLabel(_draftAssembly.Model ?? "", EditorStyles.textField, GUILayout.Height(20f));
                if (GUILayout.Button("替换", GUILayout.Width(56f)))
                {
                    AssetAssemblyPickerWindow.Open("替换模型", AssetAssemblyPickerWindow.ScanModels(), c =>
                    {
                        _draftAssembly.Model = c.Key;
                        MarkAssemblyDirty();
                    });
                }
                if (GUILayout.Button("定位", GUILayout.Width(56f)))
                    PingKey(_draftAssembly.Model, ".prefab");
            }
            EditorGUILayout.LabelField("武器", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.SelectableLabel(_draftAssembly.Weapon ?? "", EditorStyles.textField, GUILayout.Height(20f));
                if (GUILayout.Button("定位", GUILayout.Width(56f)))
                    PingKey(_draftAssembly.Weapon, ".prefab");
            }
        }

        private void DrawAssemblyActionsSection()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("业务动作槽", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("来源: ConfigModelAni.role." + (_draftAssembly.DefaultActionContext ?? ""), EditorStyles.miniLabel);
            EnsureAssemblyCollections(_draftAssembly);
            if (_draftAssembly.Actions.Count == 0)
            {
                EditorGUILayout.HelpBox("当前动作场景没有业务动作。", MessageType.Warning);
                return;
            }

            foreach (string action in _draftAssembly.Actions.Keys.ToList())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool current = _assemblyActionName == action;
                    if (GUILayout.Toggle(current, action, "Button", GUILayout.Width(86f), GUILayout.Height(22f)) && !current)
                    {
                        _assemblyActionName = action;
                        PlayAssemblyPreviewAction();
                    }
                    EditorGUILayout.SelectableLabel(_draftAssembly.Actions[action], EditorStyles.textField, GUILayout.Height(20f));
                    if (GUILayout.Button("替换", GUILayout.Width(50f)))
                        PickAction(action);
                    if (GUILayout.Button("预览", GUILayout.Width(50f)))
                    {
                        _assemblyActionName = action;
                        PlayAssemblyPreviewAction();
                    }
                    if (GUILayout.Button("默认", GUILayout.Width(44f)))
                    {
                        _draftAssembly.Actions[action] = DefaultActionKey(_draftAssembly.ActionGroup, action);
                        _assemblyActionName = action;
                        MarkAssemblyDirty();
                    }
                }
            }
        }

        private void DrawAssemblyEffectsSection(string title, List<AssetEffectBinding> list, string actionName)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            if (list == null) return;
            for (int i = 0; i < list.Count; i++)
            {
                AssetEffectBinding binding = list[i];
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.BeginChangeCheck();
                        string bone = EditorGUILayout.TextField(binding.Bone ?? "", GUILayout.Width(92f));
                        if (EditorGUI.EndChangeCheck())
                        {
                            binding.Bone = bone;
                            MarkAssemblyDirty();
                        }
                        if (GUILayout.Button("骨骼", GUILayout.Width(46f))) PickBone(binding);
                        EditorGUILayout.SelectableLabel(binding.ResolveEffectKey(), EditorStyles.textField, GUILayout.Height(20f));
                        if (GUILayout.Button("替换", GUILayout.Width(50f))) PickEffect(binding);
                        if (GUILayout.Button("删", GUILayout.Width(34f)))
                        {
                            list.RemoveAt(i);
                            MarkAssemblyDirty();
                            break;
                        }
                    }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("target=" + (string.IsNullOrEmpty(binding.Target) ? "model" : binding.Target),
                            EditorStyles.miniLabel, GUILayout.Width(90f));
                        EditorGUILayout.LabelField(binding.Source ?? "", EditorStyles.miniLabel, GUILayout.Width(180f));
                        Vector3 scale = binding.ResolveScale();
                        EditorGUI.BeginChangeCheck();
                        scale = EditorGUILayout.Vector3Field("Scale", scale);
                        if (EditorGUI.EndChangeCheck())
                        {
                            binding.Scale = new[] { scale.x, scale.y, scale.z };
                            MarkAssemblyDirty();
                        }
                    }
                }
            }
            if (GUILayout.Button("添加" + title, GUILayout.Height(22f)))
            {
                AssetAssemblyPickerWindow.Open("添加" + title, AssetAssemblyPickerWindow.ScanEffects(), c =>
                {
                    list.Add(MakeBinding("root", c.Key, title));
                    if (actionName != null) _assemblyActionName = actionName;
                    MarkAssemblyDirty();
                });
            }
        }

        private void DrawAssemblyActionEffectsSection()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("动作特效", EditorStyles.boldLabel);
            EnsureAssemblyCollections(_draftAssembly);
            foreach (string action in _draftAssembly.Actions.Keys.OrderBy(k => k).ToList())
            {
                if (!_draftAssembly.ActionEffects.TryGetValue(action, out List<AssetEffectBinding> list))
                {
                    list = new List<AssetEffectBinding>();
                    _draftAssembly.ActionEffects[action] = list;
                }
                EditorGUILayout.LabelField(action, EditorStyles.miniBoldLabel);
                DrawAssemblyEffectsSection("动作特效", list, action);
            }
        }

        private bool CanSwitchAssemblySelection()
        {
            if (!_draftDirty) return true;
            return EditorUtility.DisplayDialog("未保存的装配方案",
                "当前装配修改还没保存,切换条目会丢弃草稿。继续?", "切换", "取消");
        }

        private void LoadAssemblyDraft(AssetAssemblySet set)
        {
            if (set == null) return;
            _draftProfileId = set.ProfileId;
            AssetAssemblyEntry saved = AssetAssemblyProfileStore.Load(_draftProfileId);
            _draftHasSavedProfile = saved != null;
            _draftAssembly = saved != null
                ? AssetAssemblyProfileStore.Clone(saved)
                : BuildDefaultAssembly(set);
            ApplySetHeader(set, _draftAssembly);
            EnsureAssemblyCollections(_draftAssembly);
            EnsureBusinessActionSlots(set, _draftAssembly);
            _draftDirty = false;
            SelectFirstActionIfNeeded();
            RefreshAssemblyPreview();
        }

        private AssetAssemblyEntry BuildDefaultAssembly(AssetAssemblySet set)
        {
            var profile = new AssetAssemblyEntry
            {
                Id = set.ProfileId,
                Label = set.Label,
                BusinessKey = set.BusinessKey,
                ActionGroup = set.ActionGroup,
                Model = set.ModelKey,
                Weapon = set.WeaponKey,
                DefaultActionContext = set.DefaultActionContext,
                Actions = new Dictionary<string, string>(),
                AlwaysEffects = new List<AssetEffectBinding>(),
                ActionEffects = new Dictionary<string, List<AssetEffectBinding>>(),
            };

            foreach (string action in set.DefaultActions())
            {
                if (string.IsNullOrEmpty(action)) continue;
                profile.Actions[action] = DefaultActionKey(set.ActionGroup, action);
                profile.ActionEffects[action] = new List<AssetEffectBinding>();
            }

            AssetHubEffects.EffectInfo fx = AssetHubEffects.Query(set.Entry);
            profile.AlwaysEffects.AddRange(ConvertEffectRefs(fx.CreateEffects, "ConfigLogin.CreateRole.Effect", "model"));
            profile.AlwaysEffects.AddRange(ConvertEffectRefs(fx.Always, "SceneObjectParticle.Body.always", "model"));
            profile.AlwaysEffects.AddRange(ConvertEffectRefs(fx.SetWeapon, "SceneObjectParticle.Weapon.always", "weapon"));
            foreach (KeyValuePair<string, List<AssetHubEffects.EffectRef>> kv in fx.Actions)
            {
                if (!profile.Actions.ContainsKey(kv.Key)) continue;
                profile.ActionEffects[kv.Key] = ConvertEffectRefs(kv.Value, "SceneObjectParticle.Body.action", "model");
            }
            return profile;
        }

        private static void ApplySetHeader(AssetAssemblySet set, AssetAssemblyEntry entry)
        {
            if (set == null || entry == null) return;
            entry.Id = set.ProfileId;
            if (string.IsNullOrEmpty(entry.Label)) entry.Label = set.Label;
            if (string.IsNullOrEmpty(entry.BusinessKey)) entry.BusinessKey = set.BusinessKey;
            if (string.IsNullOrEmpty(entry.ActionGroup)) entry.ActionGroup = set.ActionGroup;
            if (string.IsNullOrEmpty(entry.DefaultActionContext)) entry.DefaultActionContext = set.DefaultActionContext;
            if (string.IsNullOrEmpty(entry.Model)) entry.Model = set.ModelKey;
            if (string.IsNullOrEmpty(entry.Weapon)) entry.Weapon = set.WeaponKey;
        }

        private static void EnsureBusinessActionSlots(AssetAssemblySet set, AssetAssemblyEntry entry)
        {
            if (set == null || entry == null) return;
            var wanted = new HashSet<string>(set.DefaultActions());
            foreach (string action in entry.Actions.Keys.ToList())
            {
                if (!wanted.Contains(action))
                {
                    entry.Actions.Remove(action);
                    entry.ActionEffects.Remove(action);
                }
            }
            foreach (string action in wanted)
            {
                if (!entry.Actions.ContainsKey(action))
                    entry.Actions[action] = DefaultActionKey(set.ActionGroup, action);
                if (!entry.ActionEffects.ContainsKey(action))
                    entry.ActionEffects[action] = new List<AssetEffectBinding>();
            }
        }

        private static string DefaultActionKey(string actionGroup, string action)
        {
            return $"object/role/action/{actionGroup}/{action}";
        }

        private static List<AssetEffectBinding> ConvertEffectRefs(IEnumerable<AssetHubEffects.EffectRef> refs,
            string source, string target)
        {
            var list = new List<AssetEffectBinding>();
            if (refs == null) return list;
            foreach (AssetHubEffects.EffectRef r in refs)
            {
                string key = ConvertedEffectKey(r.RelPath);
                if (string.IsNullOrEmpty(key)) continue;
                AssetEffectBinding binding = MakeBinding(r.Bone, key, source, target);
                list.Add(binding);
            }
            return list;
        }

        private static AssetEffectBinding MakeBinding(string bone, string effectKey, string source,
            string target = "model")
        {
            ParseEffectKey(effectKey, out string effectDir, out string effectName);
            return new AssetEffectBinding
            {
                Bone = bone,
                Effect = effectKey,
                EffectDir = effectDir,
                EffectName = effectName,
                Scale = new[] { 1f, 1f, 1f },
                Source = source,
                Target = string.IsNullOrEmpty(target) ? "model" : target,
            };
        }

        private static string ConvertedEffectKey(string relPath)
        {
            string key = ResourcePath.Normalize(relPath);
            if (key.StartsWith("resource/")) key = key.Substring("resource/".Length);
            string name = Path.GetFileName(key);
            return string.IsNullOrEmpty(name) ? key : key + "/" + name;
        }

        private static void ParseEffectKey(string effectKey, out string effectDir, out string effectName)
        {
            effectDir = "";
            effectName = "";
            string key = ResourcePath.Normalize(effectKey);
            string[] parts = key.Split('/');
            if (parts.Length < 4 || parts[0] != "effect" || parts[1] != "objs") return;
            effectDir = parts[2];
            effectName = parts[3];
        }

        private void EnsureAssemblyCollections(AssetAssemblyEntry entry)
        {
            if (entry == null) return;
            if (entry.Actions == null) entry.Actions = new Dictionary<string, string>();
            if (entry.AlwaysEffects == null) entry.AlwaysEffects = new List<AssetEffectBinding>();
            if (entry.ActionEffects == null) entry.ActionEffects = new Dictionary<string, List<AssetEffectBinding>>();
        }

        private void SelectFirstActionIfNeeded()
        {
            if (_draftAssembly?.Actions == null || _draftAssembly.Actions.Count == 0)
            {
                _assemblyActionName = null;
                return;
            }
            if (string.IsNullOrEmpty(_assemblyActionName) || !_draftAssembly.Actions.ContainsKey(_assemblyActionName))
                _assemblyActionName = _draftAssembly.Actions.Keys.OrderBy(k => k).First();
        }

        private void RefreshAssemblyPreview()
        {
            if (_draftAssembly == null) return;
            EnsureAssemblyCollections(_draftAssembly);
            _assemblyPreview.SetModel(_draftAssembly.Model);
            _assemblyPreview.SetWeapon(_draftAssembly.Weapon);
            _assemblyPreview.ApplyAlwaysEffects(_draftAssembly.AlwaysEffects);
            SelectFirstActionIfNeeded();
            PlayAssemblyPreviewAction();
        }

        private void PlayAssemblyPreviewAction()
        {
            if (_draftAssembly == null || string.IsNullOrEmpty(_assemblyActionName)) return;
            EnsureAssemblyCollections(_draftAssembly);
            _draftAssembly.Actions.TryGetValue(_assemblyActionName, out string actionKey);
            _draftAssembly.ActionEffects.TryGetValue(_assemblyActionName, out List<AssetEffectBinding> effects);
            _assemblyPreview.PlayAction(_assemblyActionName, actionKey, effects);
        }

        private void MarkAssemblyDirty()
        {
            _draftDirty = true;
            RefreshAssemblyPreview();
        }

        private void PickAssemblySetReplacement()
        {
            EnsureAssemblyCatalog();
            if (_assemblySets == null || _assemblySets.Count == 0 || _selectedAssemblySet == null) return;
            List<AssetAssemblyCandidate> candidates = _assemblySets
                .Select(s => new AssetAssemblyCandidate
                {
                    Name = s.BusinessKey + " " + s.Label + " [" + s.ActionGroup + "]",
                    Key = s.ProfileId,
                    AssetPath = s.Entry?.PrefabPath,
                })
                .ToList();
            AssetAssemblyPickerWindow.Open("替换套装", candidates, c =>
            {
                AssetAssemblySet replacementSet = _assemblySets.FirstOrDefault(s => s.ProfileId == c.Key);
                if (replacementSet == null) return;
                if (_draftAssembly != null
                    && !string.IsNullOrEmpty(_draftAssembly.ActionGroup)
                    && _draftAssembly.ActionGroup != replacementSet.ActionGroup
                    && !EditorUtility.DisplayDialog("动作组变化",
                        "当前动作组 " + _draftAssembly.ActionGroup + " 将替换为 "
                        + replacementSet.ActionGroup + "。\n骨骼不兼容会在校验区显示。",
                        "继续替换", "取消"))
                {
                    return;
                }

                string currentId = _draftProfileId;
                string currentLabel = _draftAssembly?.Label;
                string currentBusinessKey = _draftAssembly?.BusinessKey;
                AssetAssemblyEntry next = BuildDefaultAssembly(replacementSet);
                next.Id = currentId;
                next.Label = string.IsNullOrEmpty(currentLabel) ? next.Label : currentLabel + " <- " + replacementSet.Label;
                next.BusinessKey = string.IsNullOrEmpty(currentBusinessKey) ? next.BusinessKey : currentBusinessKey;
                _draftAssembly = next;
                _draftDirty = true;
                SelectFirstActionIfNeeded();
                RefreshAssemblyPreview();
            });
        }

        private void PickAction(string action)
        {
            AssetAssemblyPickerWindow.Open("替换动作 - " + action,
                AssetAssemblyPickerWindow.ScanActions(_draftAssembly?.ActionGroup), c =>
            {
                _draftAssembly.Actions[action] = c.Key;
                _assemblyActionName = action;
                MarkAssemblyDirty();
            });
        }

        private void PickEffect(AssetEffectBinding binding)
        {
            AssetAssemblyPickerWindow.Open("替换特效", AssetAssemblyPickerWindow.ScanEffects(), c =>
            {
                AssetEffectBinding next = MakeBinding(binding.Bone, c.Key, binding.Source, binding.Target);
                binding.Effect = next.Effect;
                binding.EffectDir = next.EffectDir;
                binding.EffectName = next.EffectName;
                MarkAssemblyDirty();
            });
        }

        private void PickBone(AssetEffectBinding binding)
        {
            List<AssetAssemblyCandidate> bones = BoneCandidates(binding);
            if (bones.Count == 0)
            {
                EditorUtility.DisplayDialog("选择骨骼", "当前模型没有可读取的骨骼。", "好");
                return;
            }
            AssetAssemblyPickerWindow.Open("选择骨骼", bones, c =>
            {
                binding.Bone = c.Key;
                MarkAssemblyDirty();
            });
        }

        private List<AssetAssemblyCandidate> BoneCandidates(AssetEffectBinding binding)
        {
            var list = new List<AssetAssemblyCandidate>();
            if (_draftAssembly == null) return list;
            string target = binding == null || string.IsNullOrEmpty(binding.Target) ? "model" : binding.Target;
            string key = target == "weapon" ? _draftAssembly.Weapon : _draftAssembly.Model;
            if (string.IsNullOrEmpty(key)) return list;
            string path = AssetAssemblyProfileStore.AssetPathOfKey(key, ".prefab");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return list;
            CollectBones(prefab.transform, "", path, list);
            return list;
        }

        private static void CollectBones(Transform t, string prefix, string assetPath, List<AssetAssemblyCandidate> output)
        {
            string label = string.IsNullOrEmpty(prefix) ? t.name : prefix + "/" + t.name;
            output.Add(new AssetAssemblyCandidate { Name = label, Key = t.name, AssetPath = assetPath });
            for (int i = 0; i < t.childCount; i++)
                CollectBones(t.GetChild(i), label, assetPath, output);
        }

        private List<string> ValidateAssembly(AssetAssemblyEntry entry)
        {
            var issues = new List<string>();
            if (entry == null) return issues;
            if (_selectedAssemblySet != null
                && !string.IsNullOrEmpty(entry.ActionGroup)
                && entry.ActionGroup != _selectedAssemblySet.ActionGroup)
            {
                issues.Add("⚠ 动作组已从默认 " + _selectedAssemblySet.ActionGroup + " 改为 " + entry.ActionGroup);
            }
            string modelPath = AssetAssemblyProfileStore.AssetPathOfKey(entry.Model, ".prefab");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (prefab == null) issues.Add("❌ 模型资源不存在: " + entry.Model);
            string weaponPath = AssetAssemblyProfileStore.AssetPathOfKey(entry.Weapon, ".prefab");
            GameObject weaponPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(weaponPath);
            if (!string.IsNullOrEmpty(entry.Weapon) && weaponPrefab == null)
                issues.Add("❌ 武器资源不存在: " + entry.Weapon);

            if (entry.Actions != null)
            {
                foreach (KeyValuePair<string, string> kv in entry.Actions)
                {
                    string path = AssetAssemblyProfileStore.AssetPathOfKey(kv.Value, ".anim");
                    if (!File.Exists(path)) issues.Add("❌ 动作不存在: " + kv.Key + " -> " + kv.Value);
                }
            }

            ValidateBindings("常驻", entry.AlwaysEffects, prefab, weaponPrefab, issues);
            if (entry.ActionEffects != null)
            {
                foreach (KeyValuePair<string, List<AssetEffectBinding>> kv in entry.ActionEffects)
                    ValidateBindings("动作 " + kv.Key, kv.Value, prefab, weaponPrefab, issues);
            }
            return issues;
        }

        private static void ValidateBindings(string label, List<AssetEffectBinding> bindings,
            GameObject modelPrefab, GameObject weaponPrefab, List<string> issues)
        {
            if (bindings == null) return;
            foreach (AssetEffectBinding binding in bindings)
            {
                if (binding == null) continue;
                string effectKey = binding.ResolveEffectKey();
                if (string.IsNullOrEmpty(effectKey))
                {
                    issues.Add("❌ " + label + " 特效路径为空");
                    continue;
                }
                string path = AssetAssemblyProfileStore.AssetPathOfKey(effectKey, ".prefab");
                if (!File.Exists(path)) issues.Add("❌ " + label + " 特效不存在: " + effectKey);
                string target = string.IsNullOrEmpty(binding.Target) ? "model" : binding.Target;
                GameObject boneHost = target == "weapon" ? weaponPrefab : modelPrefab;
                if (boneHost != null
                    && !string.IsNullOrEmpty(binding.Bone)
                    && RoleModelAssembler.FindBone(boneHost.transform, binding.Bone) == null)
                {
                    issues.Add("❌ " + label + " 挂点不存在(" + target + "): " + binding.Bone);
                }
            }
        }

        private void SaveAssemblyDraft()
        {
            ApplySetHeader(_selectedAssemblySet, _draftAssembly);
            EnsureAssemblyCollections(_draftAssembly);
            EnsureBusinessActionSlots(_selectedAssemblySet, _draftAssembly);
            AssetAssemblyProfileStore.Save(_draftProfileId, _draftAssembly);
            if (LayaUISettings.AutoGroupAfterConvert)
            {
                try { AddressableSetup.AutoGroupAll(); }
                catch (System.Exception e) { Debug.LogWarning("[AssetAssembly] Addressable 分组失败: " + e.Message); }
            }
            AssetAssemblyProfiles.Invalidate();
            _draftDirty = false;
            _draftHasSavedProfile = true;
            Debug.Log("[AssetAssembly] 保存装配方案: " + _draftProfileId);
        }

        private static void PingKey(string key, string ext)
        {
            string path = AssetAssemblyProfileStore.AssetPathOfKey(key, ext);
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (obj == null) return;
            EditorGUIUtility.PingObject(obj);
            Selection.activeObject = obj;
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
                            if (_mode != HubMode.Library && !CanSwitchAssemblySelection()) continue;
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
                var shownPending = shown.Where(e =>
                    StatusOf(e) == EntryStatus.NotConverted || StatusOf(e) == EntryStatus.Stale).ToList();
                var shownResettableEffects = shown.Where(e => e.Kind == AssetKind.Effect
                    && (StatusOf(e) == EntryStatus.Converted || StatusOf(e) == EntryStatus.Stale)).ToList();

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
                        && EditorUtility.DisplayDialog("全部重转", $"重转 {all.Count} 个资源,产物覆盖。继续?", "转", "算了"))
                        ConvertEntries(all);
                }
                if (!string.IsNullOrWhiteSpace(_search))
                {
                    using (new EditorGUI.DisabledScope(shownPending.Count == 0))
                    {
                        if (GUILayout.Button($"转换当前筛选缺失+过期({shownPending.Count})", GUILayout.Height(22f)))
                            ConvertEntries(shownPending);
                    }
                    using (new EditorGUI.DisabledScope(shownResettableEffects.Count == 0))
                    {
                        if (GUILayout.Button($"重置当前筛选Runtime({shownResettableEffects.Count})", GUILayout.Height(22f))
                            && EditorUtility.DisplayDialog("重置当前筛选 Runtime Editable",
                                $"将覆盖当前筛选的 {shownResettableEffects.Count} 个特效运行时可编辑层。继续?",
                                "重置", "取消"))
                            ResetRuntimeEditableEntries(shownResettableEffects);
                    }
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

        // 新模型(美术成品)交付状态缓存(GetPartArtStatus 要给几 MB 的 prefab 算 MD5,不能每帧调)
        private readonly Dictionary<string, string> _artPartStatus = new Dictionary<string, string>();
        // 挂点体检缓存(要载 prefab 数节点,不能每帧调);key=交付夹名,值=缺失挂点列表
        private readonly Dictionary<string, string[]> _mountCache = new Dictionary<string, string[]>();
        private bool _previewNewModel; // 预览窗显示新模型(而不是老转换产物)
        private bool _oldClipsFoldout;   // 有新模型线时,原始动作长列表默认收起(防糊满详情页)
        private string _oldClipsEntryId; // 换条目时重置折叠状态

        /// <summary>
        /// 条目 → 新模型(美术成品)交付映射:OutDir 的 object/{module}/ 定模块,Id 尾段数字定 id,
        /// 交付夹 = {module}_{id}(美术工程 Role/role_1213、Head/head_1213、Weapon/weapon_1200…)。
        /// 返回 null = 该域没有新模型交付线(特效/怪物等待列装,在 ArtPrefabImporter.PartTopDirs 加)。
        /// </summary>
        private static (string module, string folder, string id)? ArtPartOf(AssetEntry e)
        {
            if (e == null || e.Kind != AssetKind.Model || string.IsNullOrEmpty(e.OutDir)) return null;
            const string prefix = "Assets/GameRes/object/";
            if (!e.OutDir.StartsWith(prefix)) return null;
            string rest = e.OutDir.Substring(prefix.Length);
            int slash = rest.IndexOf('/');
            if (slash <= 0) return null;
            string module = rest.Substring(0, slash);
            if (!EditorTools.ArtImport.ArtPrefabImporter.IsPartModule(module)) return null;
            // Id:"1213"(时装/默认装)或 "model_head_1213"/"model_weapon_r_1200"(部件域)→ 取尾段数字
            int idStart = e.Id.Length;
            while (idStart > 0 && char.IsDigit(e.Id[idStart - 1])) idStart--;
            if (idStart == e.Id.Length) return null; // 尾段无数字,对不上交付夹
            string id = e.Id.Substring(idStart);
            return (module, $"{module}_{id}", id);
        }

        /// <summary>新模型预览用的 prefab:优先 idle(待机),其次 create3,再退第一个动作。</summary>
        private static string FirstNewModelPrefab((string module, string folder, string id) part)
        {
            string dir = $"Assets/GameRes/object/{part.module}/{part.folder}";
            if (!Directory.Exists(dir)) return null;
            foreach (string act in new[] { "idle", "create3" })
            {
                string p = $"{dir}/{part.id}@{act}.prefab";
                if (File.Exists(p)) return p;
            }
            return Directory.GetFiles(dir, "*@*.prefab", SearchOption.TopDirectoryOnly)
                .Select(p => p.Replace('\\', '/')).OrderBy(p => p).FirstOrDefault();
        }

        /// <summary>
        /// 新模型替换区(对标 cyzc 物品图标管理:一行一条、原始/新都能定位、槽位直接选资产):
        /// 每个动作一行——原始列(有无+定位老 clip)、新模型槽位(ObjectField 直接拖/选 prefab)、
        /// 定位/还原按钮。**没有全局开关**:某动作槽位有值就用新、空就用原始,全部按
        /// model_replacement.json 自动选择(运行时 RoleModelAssembler 同一份配置)。
        /// [更新导入]从美术工程搬文件并按 {id}@动作 自动填槽;[全部还原]清空本模型全部配置。
        /// </summary>
        private void DrawArtPartSection(AssetEntry e, (string module, string folder, string id) part)
        {
            string targetDir = $"Assets/GameRes/object/{part.module}/{part.folder}";
            string[] prefabs = Directory.Exists(targetDir)
                ? Directory.GetFiles(targetDir, "*@*.prefab", SearchOption.TopDirectoryOnly)
                    .Select(p => p.Replace('\\', '/')).OrderBy(p => p).ToArray()
                : new string[0];
            var newAvail = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // 动作→已导入 prefab 路径
            foreach (string p in prefabs)
            {
                string s = Path.GetFileNameWithoutExtension(p);
                int at = s.IndexOf('@');
                if (at >= 0 && at < s.Length - 1) newAvail[s.Substring(at + 1)] = p;
            }
            if (!_artPartStatus.TryGetValue(part.folder, out string artStatus))
            {
                artStatus = EditorTools.ArtImport.ArtPrefabImporter.GetPartArtStatus(part.module, part.folder);
                _artPartStatus[part.folder] = artStatus;
            }
            Dictionary<string, string> configured = ModelReplacementStore.GetActions(part.module, part.id);
            string[] oldActions = OldActionNames(e, part.module);
            var oldSet = new HashSet<string>(oldActions, StringComparer.OrdinalIgnoreCase);
            string oldAnimDir = OldActionAnimDir(e, part.module);

            // 行集合 = 原始 ∪ 已导入新 ∪ 已配置,逐条列出:有新货/已配置的排前面
            var rows = oldActions
                .Union(newAvail.Keys, StringComparer.OrdinalIgnoreCase)
                .Union(configured.Keys, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(a => configured.ContainsKey(a) || newAvail.ContainsKey(a))
                .ThenBy(a => a, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // —— 标题行:统计 + 两侧状态 ——
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("新模型替换", EditorStyles.boldLabel, GUILayout.Width(80f));
                    var badge = new GUIStyle(EditorStyles.miniBoldLabel);
                    badge.normal.textColor = configured.Count > 0
                        ? new Color(0.3f, 0.85f, 0.4f) : new Color(0.75f, 0.75f, 0.75f);
                    GUILayout.Label($"已配置 {configured.Count}/{rows.Length} 个动作", badge, GUILayout.Width(120f));
                    GUILayout.FlexibleSpace();
                    GUILayout.Label($"美术工程:{artStatus} | 工程内新资源:{(prefabs.Length > 0 ? prefabs.Length + " 动作" : "未导入")}",
                        EditorStyles.miniLabel);
                }

                // —— 挂点体检(角色本体,一行) ——
                if (part.module == "role" && prefabs.Length > 0)
                {
                    if (!_mountCache.TryGetValue(part.folder, out string[] missing))
                    {
                        missing = EditorTools.ArtImport.ArtPrefabImporter.MissingRoleMounts(prefabs[0]);
                        _mountCache[part.folder] = missing;
                    }
                    if (missing.Length == 0)
                        EditorGUILayout.LabelField("挂点  ✓ head / rhand / root 齐", EditorStyles.miniLabel);
                    else
                        EditorGUILayout.HelpBox(
                            $"挂点缺 [{string.Join(",", missing)}]:美术工程跑菜单[交付/补挂点]后点下方[更新导入]。",
                            MessageType.Warning);
                }

                // —— 逐行:动作 | 原始(可定位) | 新模型槽位(可选可定位) | 还原 | 生效 ——
                EditorGUILayout.Space(2f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("动作", EditorStyles.miniBoldLabel, GUILayout.Width(92f));
                    GUILayout.Label("原始", EditorStyles.miniBoldLabel, GUILayout.Width(40f));
                    GUILayout.Label("新模型(拖入/选择 prefab)", EditorStyles.miniBoldLabel, GUILayout.MinWidth(150f));
                    GUILayout.Label("", GUILayout.Width(40f)); // 定位
                    GUILayout.Label("", GUILayout.Width(40f)); // 还原
                    GUILayout.Label("生效", EditorStyles.miniBoldLabel, GUILayout.Width(40f));
                }
                foreach (string action in rows)
                {
                    bool hasCfg = configured.TryGetValue(action, out string cfgKey);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label(action, GUILayout.Width(92f));

                        // 原始列:有=可点定位老 clip;无=占位
                        if (oldSet.Contains(action) && oldAnimDir != null)
                        {
                            if (GUILayout.Button("有", EditorStyles.miniButton, GUILayout.Width(40f)))
                            {
                                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{oldAnimDir}/{action}.anim");
                                if (clip != null) EditorGUIUtility.PingObject(clip);
                            }
                        }
                        else
                        {
                            GUILayout.Label("—", GUILayout.Width(40f));
                        }

                        // 新模型槽位:显示已配置的 prefab,可直接拖/选;清空=还原该动作
                        GameObject cur = hasCfg
                            ? AssetDatabase.LoadAssetAtPath<GameObject>(ModelReplacementStore.KeyToPrefabPath(cfgKey))
                            : null;
                        GameObject picked = (GameObject)EditorGUILayout.ObjectField(
                            cur, typeof(GameObject), false, GUILayout.MinWidth(150f));
                        if (picked != cur)
                        {
                            if (picked == null)
                            {
                                ModelReplacementStore.RemoveAction(part.module, part.id, action);
                            }
                            else if (PrefabUtility.GetPrefabAssetType(picked) != PrefabAssetType.Regular
                                     && PrefabUtility.GetPrefabAssetType(picked) != PrefabAssetType.Variant)
                            {
                                // 裸 FBX/模型资产没有 Timeline/渲染档案,不能当动作件(实锤:用户拖过 models/create3.fbx)
                                EditorUtility.DisplayDialog("不是 prefab",
                                    "要选 {id}@动作 的 prefab(蓝色方块图标),不能选 FBX/模型资产。", "好");
                            }
                            else
                            {
                                string key = ModelReplacementStore.PathToKey(AssetDatabase.GetAssetPath(picked));
                                if (key == null)
                                    EditorUtility.DisplayDialog("不在 GameRes 内",
                                        "新模型 prefab 必须在 Assets/GameRes/ 下(要能进 Addressables)。", "好");
                                else
                                    ModelReplacementStore.SetAction(part.module, part.id, action, key);
                            }
                        }

                        using (new EditorGUI.DisabledScope(cur == null))
                        {
                            if (GUILayout.Button("定位", EditorStyles.miniButton, GUILayout.Width(40f)))
                                EditorGUIUtility.PingObject(cur);
                        }
                        using (new EditorGUI.DisabledScope(!hasCfg))
                        {
                            if (GUILayout.Button("还原", EditorStyles.miniButton, GUILayout.Width(40f)))
                                ModelReplacementStore.RemoveAction(part.module, part.id, action);
                        }

                        var eff = new GUIStyle(EditorStyles.miniLabel);
                        if (hasCfg) eff.normal.textColor = new Color(0.3f, 0.85f, 0.4f);
                        GUILayout.Label(hasCfg ? "新" : "原始", eff, GUILayout.Width(40f));
                    }
                }

                // —— 打包提示(一行小字;打包线按同一份清单剔除,不双打) ——
                int oldCovered = oldActions.Count(a => configured.ContainsKey(a));
                string packHint = configured.Count == 0
                    ? "打包:未配置任何动作 → 全用原始,剔除新资源"
                    : oldCovered >= oldActions.Length
                        ? "打包:原始动作已全被新覆盖 → 可剔除原始资源"
                        : $"打包:已配置 {configured.Count} 动作,其余 {oldActions.Length - oldCovered} 个用原始 → 两份都留";
                GUILayout.Label(packHint + "(记录:model_replacement.json)", EditorStyles.miniLabel);

                // —— 操作行 ——
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.enabled = artStatus != "未交付";
                    if (GUILayout.Button(prefabs.Length > 0 ? "更新导入" : "导入新模型",
                            GUILayout.Width(90f), GUILayout.Height(22f)))
                    {
                        bool ok = EditorTools.ArtImport.ArtPrefabImporter.ImportPart(part.module, part.folder, out string summary);
                        InvalidatePartCaches(part.folder);
                        if (ok)
                        {
                            int filled = AutoFillFromImported(part);
                            ShowNotification(new GUIContent($"{summary};自动配置 {filled} 个动作"));
                        }
                        else EditorUtility.DisplayDialog("导入失败", summary, "好");
                        GUIUtility.ExitGUI();
                    }
                    GUI.enabled = prefabs.Length > 0;
                    if (GUILayout.Button("自动配全部", GUILayout.Width(80f), GUILayout.Height(22f)))
                    {
                        int filled = AutoFillFromImported(part);
                        ShowNotification(new GUIContent($"按已导入资源自动配置 {filled} 个动作"));
                    }
                    GUI.enabled = configured.Count > 0;
                    if (GUILayout.Button("全部还原", GUILayout.Width(70f), GUILayout.Height(22f))
                        && EditorUtility.DisplayDialog("全部还原",
                            $"清空 {part.folder} 的全部动作配置,恢复原始模型?\n(新资源保留在工程,打包按记录剔除)", "还原", "取消"))
                    {
                        ModelReplacementStore.ClearEntry(part.module, part.id);
                        GUIUtility.ExitGUI();
                    }
                    GUI.enabled = prefabs.Length > 0;
                    _previewNewModel = GUILayout.Toggle(_previewNewModel && prefabs.Length > 0,
                        "预览新模型", "Button", GUILayout.Width(76f), GUILayout.Height(22f));
                    GUI.enabled = true;
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("刷新", GUILayout.Width(44f), GUILayout.Height(22f)))
                        InvalidatePartCaches(part.folder);
                }
            }
        }

        /// <summary>按已导入的 {id}@动作 prefab 自动填全部动作槽(已有配置的动作原样覆盖为导入件)。</summary>
        private int AutoFillFromImported((string module, string folder, string id) part)
        {
            string targetDir = $"Assets/GameRes/object/{part.module}/{part.folder}";
            if (!Directory.Exists(targetDir)) return 0;
            int filled = 0;
            foreach (string p in Directory.GetFiles(targetDir, "*@*.prefab", SearchOption.TopDirectoryOnly))
            {
                string s = Path.GetFileNameWithoutExtension(p);
                int at = s.IndexOf('@');
                if (at < 0 || at >= s.Length - 1) continue;
                string key = ModelReplacementStore.PathToKey(p.Replace('\\', '/'));
                if (key == null) continue;
                ModelReplacementStore.SetAction(part.module, part.id, s.Substring(at + 1).ToLowerInvariant(), key);
                filled++;
            }
            return filled;
        }

        private void InvalidatePartCaches(string folder)
        {
            _artPartStatus.Remove(folder);
            _mountCache.Remove(folder);
        }

        /// <summary>原始动作 .anim 所在的 GameRes 目录(定位老 clip 用);没有返回 null。</summary>
        private static string OldActionAnimDir(AssetEntry e, string module)
        {
            if (string.IsNullOrEmpty(e.ActionDir)) return null;
            string dirName = Path.GetFileName(e.ActionDir.TrimEnd('/', '\\'));
            string converted = $"Assets/GameRes/object/{module}/action/{dirName}";
            return Directory.Exists(converted) ? converted : null;
        }

        /// <summary>
        /// 原始(老端)动作名列表:优先 GameRes 里已转换的 .anim(运行时真正可用的);
        /// 一个都没转换时退回老客户端源 .lani 清单(展示"原始有这个动作"用)。按条目 ActionDir 定目录。
        /// </summary>
        private static string[] OldActionNames(AssetEntry e, string module)
        {
            if (string.IsNullOrEmpty(e.ActionDir)) return new string[0];
            string dirName = Path.GetFileName(e.ActionDir.TrimEnd('/', '\\'));
            string converted = $"Assets/GameRes/object/{module}/action/{dirName}";
            if (Directory.Exists(converted))
            {
                string[] anims = Directory.GetFiles(converted, "*.anim", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileNameWithoutExtension).OrderBy(n => n).ToArray();
                if (anims.Length > 0) return anims;
            }
            if (Directory.Exists(e.ActionDir))
            {
                // 老 .lani 名形如 idle-idle.lani / create2-create2.lani,取 '-' 前段
                return Directory.GetFiles(e.ActionDir, "*.lani", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileNameWithoutExtension)
                    .Select(n => { int i = n.IndexOf('-'); return i > 0 ? n.Substring(0, i) : n; })
                    .Distinct().OrderBy(n => n).ToArray();
            }
            return new string[0];
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
                // 先装载预览实例:动作列表(DrawClipsSection)读 _preview.Clips,需与当前条目同步。
                // 勾了[预览新模型]则加载新美术成品(Timeline 驱动,此预览窗不播动作,可看模型/粒子)
                string previewPath = s == EntryStatus.Converted || s == EntryStatus.Stale ? e.PrefabPath : null;
                (string module, string folder, string id)? artPart = ArtPartOf(e);
                if (_previewNewModel && artPart != null)
                {
                    string np = FirstNewModelPrefab(artPart.Value);
                    if (np != null) previewPath = np;
                }
                _preview.SetPrefab(previewPath);

                _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
                EditorGUILayout.LabelField($"{Path.GetFileNameWithoutExtension(e.LhPath)}  {e.DisplayName}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("状态", $"{AssetHubDomains.StatusIcon(s)} {AssetHubDomains.StatusText(s)}");
                string statusDetail = AssetHubDomains.StatusDetail(e, s);
                if (!string.IsNullOrEmpty(statusDetail))
                    EditorGUILayout.HelpBox(statusDetail, s == EntryStatus.Converted ? MessageType.Info : MessageType.Warning);
                if (e.Career > 0)
                    EditorGUILayout.LabelField("职业/性别",
                        $"{AssetHubDomains.CAREER_NAMES[Mathf.Clamp(e.Career, 0, 4)]} / {(e.Sex == 2 ? "女" : "男")}");
                if (!string.IsNullOrEmpty(e.Note))
                    EditorGUILayout.LabelField("配置来源", e.Note, EditorStyles.wordWrappedLabel);
                EditorGUILayout.LabelField("源", e.LhPath, EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField("产物", e.PrefabPath, EditorStyles.wordWrappedMiniLabel);

                // 新模型(美术成品)对照替换:时装/默认装/头饰/武器/翅膀/背饰 模型条目通用
                if (artPart != null)
                    DrawArtPartSection(e, artPart.Value);

                if (e.Kind == AssetKind.Model)
                {
                    if (artPart != null)
                    {
                        // 有新模型线的条目:原始动作长列表默认收起(90+ 行糊满详情页;要预览再展开)
                        if (_oldClipsEntryId != e.Id)
                        {
                            _oldClipsEntryId = e.Id;
                            _oldClipsFoldout = false;
                        }
                        _oldClipsFoldout = EditorGUILayout.Foldout(_oldClipsFoldout, "原始模型动作预览", true);
                        if (_oldClipsFoldout) DrawClipsSection(e, s);
                    }
                    else
                    {
                        DrawClipsSection(e, s);
                    }
                    DrawAlwaysEffects(e);
                }
                else if (e.Kind == AssetKind.Effect)
                {
                    DrawEffectStructure(e, s);
                }

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);
                if (e.Kind == AssetKind.Model)
                {
                    DrawLaniChoice(e);
                    _materialMode = (Laya3DImporter.MaterialMode)EditorGUILayout.EnumPopup(
                        new GUIContent("材质模式", "Unlit=对标老客户端贴图直出(默认);Lit=受场景光照"), _materialMode);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    bool canConvert = s != EntryStatus.SourceMissing && s != EntryStatus.SourceLfs;
                    using (new EditorGUI.DisabledScope(!canConvert))
                    {
                        if (GUILayout.Button(s == EntryStatus.Converted || s == EntryStatus.Stale ? "重转" : "转换", GUILayout.Height(26f)))
                            ConvertEntries(new List<AssetEntry> { e });
                        if (e.Kind == AssetKind.Effect
                            && GUILayout.Button("重置Runtime", GUILayout.Height(26f))
                            && EditorUtility.DisplayDialog("重置 Runtime Editable",
                                e.PrefabPath + "\n\n将覆盖该特效的运行时可编辑层。继续?", "重置", "取消"))
                            ResetRuntimeEditableEntries(new List<AssetEntry> { e });
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
            EditorGUILayout.LabelField("  (特效 .lh=Laya 粒子,Unity 侧统一走 LayaEffectImporter 转换)", EditorStyles.miniLabel);
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

        /// <summary>
        /// 特效结构速览(只读解析,不产资产):列出粒子节点数 / 网格 / 拖尾,
        /// LFS 占位时明确提示。让用户转换前先确认能读懂这个特效文件。
        /// </summary>
        private void DrawEffectStructure(AssetEntry e, EntryStatus s)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("特效结构(只读速览)", EditorStyles.boldLabel);
            if (s == EntryStatus.SourceMissing) { EditorGUILayout.LabelField("  源文件缺失", EditorStyles.miniLabel); return; }
            if (s == EntryStatus.SourceLfs)
            {
                EditorGUILayout.HelpBox("源是 LFS 占位(130字节):在本机 git lfs pull 后才能解析/转换。", MessageType.Warning);
                return;
            }
            (int particles, int meshes, int trails, string err) = LayaEffectImporter.Inspect(e.LhPath);
            if (err != null) { EditorGUILayout.HelpBox("解析失败:" + err, MessageType.Error); return; }
            EditorGUILayout.LabelField($"  粒子节点 {particles} / 静态网格 {meshes} / 拖尾 {trails}", EditorStyles.miniLabel);
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
