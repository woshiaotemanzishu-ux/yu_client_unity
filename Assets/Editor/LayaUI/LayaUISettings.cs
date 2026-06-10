using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.LayaUI
{
    /// <summary>
    /// LayaUI 转换器的路径与字体设置。yu_client 仓库路径存 EditorPrefs(按项目隔离),
    /// 默认取本仓库同级目录的 ../yu_client。
    /// </summary>
    public static class LayaUISettings
    {
        private const string KEY_CLIENT_ROOT = "Shenxiao.LayaUI.ClientRoot";
        private const string KEY_FONT_PATH = "Shenxiao.LayaUI.FontPath";

        public const string MANIFEST_PATH = "Schemas/LayaUI/ui_manifest.json";
        public const string PREFAB_ROOT = "Assets/Prefabs/UI";
        public const string GAMERES_ROOT = "Assets/GameRes";
        public const string BIND_ROOT = "Assets/Scripts/Generated/UI";
        public const string TEMPLATE_ROOT = "Assets/Editor/LayaUI/Templates";
        public const string REPORT_ROOT = "Reports/LayaUI";

        private static string ProjectKey(string key)
        {
            return key + ":" + Application.dataPath.GetHashCode();
        }

        public static string ClientRoot
        {
            get
            {
                string def = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "yu_client"));
                return EditorPrefs.GetString(ProjectKey(KEY_CLIENT_ROOT), def);
            }
            set { EditorPrefs.SetString(ProjectKey(KEY_CLIENT_ROOT), value); }
        }

        /// <summary>h5/laya/assets 散图根(皮肤路径直接拼在它后面)。</summary>
        public static string LayaAssetsRoot { get { return Path.Combine(ClientRoot, "h5", "laya", "assets"); } }

        /// <summary>cdn/resource 根(运行时 scene json、图集、UIConfig.json)。</summary>
        public static string CdnResourceRoot { get { return Path.Combine(ClientRoot, "cdn", "resource"); } }

        public static string FontAssetPath
        {
            get { return EditorPrefs.GetString(ProjectKey(KEY_FONT_PATH), ""); }
            set { EditorPrefs.SetString(ProjectKey(KEY_FONT_PATH), value); }
        }

        /// <summary>取默认 TMP 字体;没配置就用 TMP 默认字体(中文会缺字,报告里会提示)。</summary>
        public static TMP_FontAsset LoadFont()
        {
            if (!string.IsNullOrEmpty(FontAssetPath))
            {
                TMP_FontAsset f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
                if (f != null) return f;
            }
            return TMP_Settings.defaultFontAsset;
        }

        public static bool ValidateClientRoot(out string error)
        {
            error = null;
            if (!Directory.Exists(Path.Combine(ClientRoot, "cdn", "resource", "game")))
            {
                error = "yu_client 路径不对(找不到 cdn/resource/game): " + ClientRoot;
                return false;
            }
            return true;
        }
    }
}
