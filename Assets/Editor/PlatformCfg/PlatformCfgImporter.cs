using System.IO;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Config;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.PlatformCfg
{
    /// <summary>
    /// 从 yu_client 的平台配置(cdn/platform/*.cfg,JSON)导入登录环境到 AppConfig.asset,
    /// 不手抄地址:gmApiUrl = url_account_path + login_php;plat_name 从文件名/字段推导。
    /// 换环境(测试/正式/渠道)= 选不同 cfg 重导,代码零改动。
    /// </summary>
    public static class PlatformCfgImporter
    {
        private const string APP_CONFIG_PATH = "Assets/_App/Configs/AppConfig.asset";

        [MenuItem("神霄/配置/从 yu_client 平台cfg 导入登录环境", priority = 40)]
        public static void Import()
        {
            string defaultDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "yu_client", "cdn", "platform"));
            string path = EditorUtility.OpenFilePanel("选平台配置(cdn/platform/*.cfg)",
                Directory.Exists(defaultDir) ? defaultDir : "", "cfg");
            if (string.IsNullOrEmpty(path)) return;

            JObject cfg;
            try
            {
                cfg = JObject.Parse(File.ReadAllText(path));
            }
            catch (System.Exception e)
            {
                Debug.LogError("[PlatformCfg] cfg 解析失败: " + e.Message);
                return;
            }

            AppConfig appConfig = AssetDatabase.LoadAssetAtPath<AppConfig>(APP_CONFIG_PATH);
            if (appConfig == null)
            {
                Debug.LogError("[PlatformCfg] 找不到 " + APP_CONFIG_PATH);
                return;
            }

            string accountPath = (string)cfg["url_account_path"] ?? "";
            string loginPhp = (string)cfg["login_php"] ?? "";
            if (string.IsNullOrEmpty(accountPath))
            {
                Debug.LogError("[PlatformCfg] cfg 里没有 url_account_path");
                return;
            }
            appConfig.gmApiUrl = accountPath.TrimEnd('/') + "/" + loginPhp.Trim('/') + "/";

            string platName = (string)cfg["plat_name"];
            if (string.IsNullOrEmpty(platName))
            {
                // 文件名约定: config_{plat_name}_P00xxxx.cfg
                string stem = Path.GetFileNameWithoutExtension(path);
                if (stem.StartsWith("config_")) stem = stem.Substring("config_".Length);
                int pIdx = stem.LastIndexOf("_P0", System.StringComparison.Ordinal);
                platName = pIdx > 0 ? stem.Substring(0, pIdx) : stem;
            }
            appConfig.platName = platName;

            EditorUtility.SetDirty(appConfig);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PlatformCfg] 已导入: gmApiUrl={appConfig.gmApiUrl} platName={appConfig.platName}(来源 {Path.GetFileName(path)})");
        }
    }
}
