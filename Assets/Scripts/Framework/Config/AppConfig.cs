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
        [Tooltip("Full URL of the Unity Addressables resource version API. Empty = use the built-in/local catalog.")]
        public string resourceVersionApiUrl = "";

        [Tooltip("Unity Addressables CDN base URL. This is not the old Laya raw resource CDN.")]
        public string addressablesCdnBaseUrl = "";

        [Tooltip("Optional Unity Addressables remote catalog URL. Empty = use the built-in/local catalog.")]
        public string addressablesCatalogUrl = "";

        [Tooltip("ASTC 内容变体 CDN 基址(ServerData-ASTC 的发布根)。设备不支持 DXT(手机 GPU)且支持 ASTC 时自动改用;留空=始终用主源。")]
        public string astcCdnBaseUrl = "";

        [Tooltip("Old-client raw resource CDN imported from yu_client platform cfg. Kept for comparison/tools, not used as Unity Addressables CDN.")]
        public string legacyResourceCdnBaseUrl = "";

        [Tooltip("运营公告 CDN 根地址，导自平台 cfg.url_cdn_path；10207 只通知客户端从此地址重查版本/正文。")]
        public string noticeCdnBaseUrl = "http://userpic.suyougame.com/";

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

        [Tooltip("运营公告平台归属键，导自平台 cfg.plat_belong，用于 belong 表筛选公告。")]
        public string platBelong = "test";

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
        public float heartbeatIntervalSec = 5f;

        [Header("Boot Loading")]
        [Tooltip("启动加载页要预下载的 Addressable key/label 列表(编辑器本地组为 0 下载,真机走 CDN)。")]
        public string[] preloadKeys = { "prefabs/ui/login/loginmodule" };

        [Header("Debug")]
        public bool enableEditorVerboseLog = true;

        [Tooltip("启动后自动用 devAccount 跑通整条登录链(HTTP登录→选服→连游戏服→收角色列表),链路冒烟用。")]
        public bool autoLoginSmokeTest = false;

        [Tooltip("Debug only. When autoLoginSmokeTest receives a real role list, immediately enter the first role.")]
        public bool autoEnterFirstRoleSmokeTest = false;

        [Tooltip("Round15 Combo副技能测试:启用后进游戏后自动驱动普攻并捕获副技能 damage>0(仅 smoke=1 时有效)。")]
        public bool enableRound15ComboTest = false;

        [Tooltip("Round18 连续击杀测试:启用后 combo 回包若目标 hp>0,自动继续驱动下一次普攻,循环至 hp==0(需 enableRound15ComboTest=1)。")]
        public bool enableRound18ContinuousKill = false;
    }
}
