using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.LayaUI
{
    /// <summary>
    /// LayaUI 转换器入口窗口:神霄/LayaUI/转换器。
    /// 流程:①确认 yu_client 路径与字体 ②生成模板 ③转换模块 ④编译后回填 Bind。
    /// </summary>
    public class LayaUIWindow : EditorWindow
    {
        private string _module = "login";
        private string _singleKey = "login/LoginView";
        private TMP_FontAsset _font;

        [MenuItem("神霄/LayaUI/转换器", priority = 10)]
        public static void Open()
        {
            LayaUIWindow w = GetWindow<LayaUIWindow>("LayaUI 转换器");
            w.minSize = new Vector2(420f, 320f);
        }

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(LayaUISettings.FontAssetPath))
            {
                _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LayaUISettings.FontAssetPath);
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("1. 环境", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("yu_client 路径", LayaUISettings.ClientRoot);
                if (GUILayout.Button("选...", GUILayout.Width(50f)))
                {
                    string p = EditorUtility.OpenFolderPanel("选 yu_client 仓库根目录", LayaUISettings.ClientRoot, "");
                    if (!string.IsNullOrEmpty(p)) LayaUISettings.ClientRoot = p;
                }
            }
            string err;
            if (!LayaUISettings.ValidateClientRoot(out err))
            {
                EditorGUILayout.HelpBox(err, MessageType.Error);
            }

            TMP_FontAsset newFont = (TMP_FontAsset)EditorGUILayout.ObjectField("默认中文字体(TMP)", _font, typeof(TMP_FontAsset), false);
            if (newFont != _font)
            {
                _font = newFont;
                LayaUISettings.FontAssetPath = _font != null ? AssetDatabase.GetAssetPath(_font) : "";
            }
            if (_font == null)
            {
                EditorGUILayout.HelpBox("没配置中文 TMP 字体,中文会显示成方块。把字体 ttf 放进 Assets/GameRes/Fonts," +
                                        "用 Window/TextMeshPro/Font Asset Creator 生成后拖到这里。", MessageType.Warning);
            }

            GUILayout.Space(8f);
            GUILayout.Label("2. 模板(样式都在模板 prefab 上调,改完重转即生效)", EditorStyles.boldLabel);
            if (GUILayout.Button("生成 / 补齐 UI 模板"))
            {
                LayaUITemplates.BuildAll();
            }

            GUILayout.Space(8f);
            GUILayout.Label("3. 转换", EditorStyles.boldLabel);
            _module = EditorGUILayout.TextField("模块名(如 login)", _module);
            if (GUILayout.Button("转换模块 → 合并成大 prefab(推荐)"))
            {
                LayaSceneConverter.ConvertModuleCombined(_module.Trim());
            }
            EditorGUILayout.HelpBox("合并模式:整个模块一个 prefab,各窗口是子节点(默认只激活第一个)。" +
                                    "想拆成几个大 Panel,编辑 " + LayaUIGroups.CONFIG_PATH + " 后重转。", MessageType.None);
            if (GUILayout.Button("转换模块 → 每窗口一个 prefab"))
            {
                LayaSceneConverter.ConvertModule(_module.Trim());
            }
            _singleKey = EditorGUILayout.TextField("单个 scene key", _singleKey);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("在合并 prefab 内重转该窗口"))
                {
                    LayaSceneConverter.ReconvertWindowInGroup(_singleKey.Trim());
                }
                if (GUILayout.Button("转成独立 prefab"))
                {
                    LayaSceneConverter.ConvertSingle(_singleKey.Trim());
                }
            }

            GUILayout.Space(8f);
            GUILayout.Label("4. Bind(等编译转完一轮后再点)", EditorStyles.boldLabel);
            if (GUILayout.Button("回填 Bind 引用(按上面的模块名)"))
            {
                LayaBindFiller.FillModule(_module.Trim());
            }

            GUILayout.Space(8f);
            if (GUILayout.Button("打开转换报告目录"))
            {
                string dir = Path.GetFullPath(LayaUISettings.REPORT_ROOT);
                Directory.CreateDirectory(dir);
                EditorUtility.RevealInFinder(dir);
            }
        }
    }
}
