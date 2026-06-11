using UnityEngine;

namespace Shenxiao.Framework.Config
{
    /// <summary>
    /// In-package boot config. The asset lives at Assets/_App/Configs/AppConfig.asset
    /// and is loaded synchronously at launch (Editor only sync; runtime via Addressables Local group).
    /// </summary>
    [CreateAssetMenu(fileName = "AppConfig", menuName = "Shenxiao/AppConfig", order = 0)]
    public class AppConfig : ScriptableObject
    {
        [Header("Environment")]
        [Tooltip("Environment tag passed to the resource version API.")]
        public string env = "dev";

        [Tooltip("Channel tag (appstore / official / inner ...)")]
        public string channel = "official";

        [Tooltip("Client app version, used by the resource version API.")]
        public string appVersion = "1.0.0";

        [Header("Resource Version API")]
        [Tooltip("Full URL of the resource version API. See ResVersionManager.")]
        public string resourceVersionApiUrl = "http://127.0.0.1:8090/api/resource_version";

        [Header("Game Server")]
        [Tooltip("Login / lobby server endpoint host (overridden by API in production).")]
        public string serverHost = "127.0.0.1";

        public int serverPort = 10000;

        [Header("GM API (yu_gm)")]
        [Tooltip("yu_gm 账号 API 基址 = 平台cfg 的 url_account_path + login_php,如 http://223.109.142.26:88/api/。可用菜单 神霄/配置/从 yu_client 平台cfg导入 一键填。")]
        public string gmApiUrl = "http://223.109.142.26:88/api/";

        [Tooltip("Shared secret used to sign GM API requests. Must match yu_gm Index::LOGIN_KEY.")]
        public string gmLoginKey = "#LMfJyNQUKhLVLmpJ%WBo4@k^VdTEB5m";

        [Tooltip("Platform name sent to GM API as 'site'. Mirrors Laya ClientConfig.plat_name.")]
        public string platName = "jzy_sh921_test";

        [Tooltip("Account used in dev to skip the OAuth/SDK step. Empty disables auto-login.")]
        public string devAccount = "unity_dev_001";

        [Header("UI Design")]
        [Tooltip("Reference resolution for Canvas Scaler. Match yu_client GameConfig (720x1280 portrait).")]
        public Vector2 designResolution = new Vector2(720f, 1280f);

        [Tooltip("Canvas Scaler match: 0=Width, 1=Height, 0.5=Balance. Laya 'fixedauto' = Expand mode (use 0.5 with Expand).")]
        [Range(0f, 1f)]
        public float canvasMatch = 0.5f;

        [Header("Game Net")]
        [Tooltip("心跳协议发送间隔(秒),<=0 关闭。")]
        public float heartbeatIntervalSec = 10f;

        [Header("Debug")]
        public bool enableEditorVerboseLog = true;

        [Tooltip("启动后自动用 devAccount 跑通整条登录链(HTTP登录→选服→连游戏服→收角色列表),链路冒烟用。")]
        public bool autoLoginSmokeTest = false;
    }
}
