using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.LayaUI
{
    /// <summary>
    /// LayaUI 转换器(神霄/LayaUI/转换器):
    /// Tab 分类 + 模块按钮(中英对照,来自 Schemas/LayaUI/module_names_cn.json),
    /// 点按钮 = 一键流水线(散图→转换→编译后自动回填→分组→报告)。
    /// 按钮右侧:⚠N = 上次转换缺图数;验收勾 = 标记后重转弹确认。
    /// </summary>
    public class LayaUIWindow : EditorWindow
    {
        private const string NAMES_PATH = "Schemas/LayaUI/module_names_cn.json";

        private class ModuleEntry
        {
            public string Module;
            public string Cn;
            public string Tab;
        }

        private readonly Dictionary<string, List<ModuleEntry>> _byTab = new Dictionary<string, List<ModuleEntry>>();
        private readonly List<string> _tabs = new List<string>();
        private int _tabIndex;
        private string _search = "";
        private Vector2 _scroll;
        private bool _showSettings;
        private bool _showAdvanced;
        private TMP_FontAsset _font;
        private string _singleKey = "login/LoginView";

        [MenuItem("神霄/LayaUI/转换器", priority = 10)]
        public static void Open()
        {
            LayaUIWindow w = GetWindow<LayaUIWindow>("LayaUI 转换器");
            w.minSize = new Vector2(460f, 420f);
        }

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(LayaUISettings.FontAssetPath))
            {
                _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LayaUISettings.FontAssetPath);
            }
            LoadModules();
        }

        private void LoadModules()
        {
            _byTab.Clear();
            _tabs.Clear();
            if (!File.Exists(NAMES_PATH))
            {
                Debug.LogError("[LayaUI] 缺 " + NAMES_PATH);
                return;
            }
            JObject names = JObject.Parse(File.ReadAllText(NAMES_PATH));
            foreach (KeyValuePair<string, JToken> kv in names)
            {
                if (kv.Key.StartsWith("_")) continue;
                var obj = kv.Value as JObject;
                if (obj == null) continue;
                var entry = new ModuleEntry
                {
                    Module = kv.Key,
                    Cn = (string)obj["cn"] ?? kv.Key,
                    Tab = (string)obj["tab"] ?? "其他",
                };
                if (!_byTab.TryGetValue(entry.Tab, out List<ModuleEntry> list))
                {
                    list = new List<ModuleEntry>();
                    _byTab[entry.Tab] = list;
                    _tabs.Add(entry.Tab);
                }
                list.Add(entry);
            }
            foreach (List<ModuleEntry> list in _byTab.Values)
            {
                list.Sort((a, b) => string.CompareOrdinal(a.Module, b.Module));
            }
        }

        private void OnGUI()
        {
            DrawSettings();
            EditorGUILayout.Space(4f);

            using (new EditorGUILayout.HorizontalScope())
            {
                _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
                if (GUILayout.Button("刷新", GUILayout.Width(44f))) LoadModules();
            }

            if (_tabs.Count == 0)
            {
                EditorGUILayout.HelpBox("没有读到模块分类(" + NAMES_PATH + ")", MessageType.Warning);
                return;
            }

            bool searching = !string.IsNullOrEmpty(_search);
            if (!searching)
            {
                _tabIndex = GUILayout.Toolbar(Mathf.Clamp(_tabIndex, 0, _tabs.Count - 1), _tabs.ToArray());
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (searching)
            {
                foreach (string tab in _tabs)
                {
                    foreach (ModuleEntry e in _byTab[tab])
                    {
                        if (e.Module.Contains(_search) || e.Cn.Contains(_search)) DrawModuleRow(e);
                    }
                }
            }
            else
            {
                foreach (ModuleEntry e in _byTab[_tabs[_tabIndex]]) DrawModuleRow(e);
            }
            EditorGUILayout.EndScrollView();

            DrawAdvanced();
        }

        private void DrawModuleRow(ModuleEntry e)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                int missing = LayaUIPipeline.GetLastMissingCount(e.Module);
                bool converted = Directory.Exists(LayaUISettings.PREFAB_ROOT + "/" +
                    (char.ToUpperInvariant(e.Module[0]) + e.Module.Substring(1)));
                string state = !converted ? "" : missing > 0 ? "  ⚠" + missing : "  ✓";

                if (GUILayout.Button(e.Cn + "  (" + e.Module + ")" + state, GUILayout.Height(22f)))
                {
                    LayaUIPipeline.RunModule(e.Module);
                }

                bool accepted = LayaUIAcceptance.IsAccepted(e.Module);
                bool toggled = GUILayout.Toggle(accepted, new GUIContent("验收", "验收后重转会弹确认"), GUILayout.Width(48f));
                if (toggled != accepted) LayaUIAcceptance.SetAccepted(e.Module, toggled);
            }
        }

        private void DrawSettings()
        {
            _showSettings = EditorGUILayout.Foldout(_showSettings, "设置(yu_client 目录 / 中文字体 / 分组)", true);
            if (!_showSettings) return;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("yu_client", LayaUISettings.ClientRoot);
                if (GUILayout.Button("选...", GUILayout.Width(44f)))
                {
                    string p = EditorUtility.OpenFolderPanel("选 yu_client 仓库根目录", LayaUISettings.ClientRoot, "");
                    if (!string.IsNullOrEmpty(p)) LayaUISettings.ClientRoot = p;
                }
            }
            string err;
            if (!LayaUISettings.ValidateClientRoot(out err)) EditorGUILayout.HelpBox(err, MessageType.Error);

            TMP_FontAsset newFont = (TMP_FontAsset)EditorGUILayout.ObjectField("中文字体(TMP)", _font, typeof(TMP_FontAsset), false);
            if (newFont != _font)
            {
                _font = newFont;
                LayaUISettings.FontAssetPath = _font != null ? AssetDatabase.GetAssetPath(_font) : "";
            }
            if (_font == null)
            {
                EditorGUILayout.HelpBox("未配置中文 TMP 字体,中文会显示方块。", MessageType.Warning);
            }
            LayaUISettings.AutoGroupAfterConvert =
                EditorGUILayout.ToggleLeft("转换后自动 Addressable 分组", LayaUISettings.AutoGroupAfterConvert);
        }

        private void DrawAdvanced()
        {
            _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "高级(单窗口 / 预览 / 报告)", true);
            if (!_showAdvanced) return;

            _singleKey = EditorGUILayout.TextField("scene key", _singleKey);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("组内重转该窗口")) LayaSceneConverter.ReconvertWindowInGroup(_singleKey.Trim());
                if (GUILayout.Button("转成独立 prefab")) LayaSceneConverter.ConvertSingle(_singleKey.Trim());
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("预览场景(模块名取 scene key 前缀)"))
                {
                    string module = _singleKey.Contains("/") ? _singleKey.Substring(0, _singleKey.IndexOf('/')) : _singleKey;
                    LayaUIPreview.OpenWithPrefab(module.Trim());
                }
                if (GUILayout.Button("打开报告目录"))
                {
                    Directory.CreateDirectory(LayaUISettings.REPORT_ROOT);
                    EditorUtility.RevealInFinder(Path.GetFullPath(LayaUISettings.REPORT_ROOT));
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("重建 UI 模板")) LayaUITemplates.BuildAll();
                if (GUILayout.Button("手动回填 Bind(模块名取前缀)"))
                {
                    string module = _singleKey.Contains("/") ? _singleKey.Substring(0, _singleKey.IndexOf('/')) : _singleKey;
                    LayaBindFiller.FillModule(module.Trim());
                }
            }
        }
    }
}
