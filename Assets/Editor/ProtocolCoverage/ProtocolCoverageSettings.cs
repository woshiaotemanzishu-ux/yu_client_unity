using System.IO;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.ProtocolCoverage
{
    /// <summary>
    /// 协议覆盖率核验器的跨仓路径设置。照抄 Assets/Editor/LayaUI/LayaUISettings.cs:29-37 的
    /// ClientRoot 范式(EditorPrefs 按项目哈希隔离 + 默认取本仓库同级目录),新增同形状的 ServerRoot。
    /// 默认值不依赖 EditorPrefs 就能跑——这是 batchmode/CI 首次运行的生命线。
    /// </summary>
    public static class ProtocolCoverageSettings
    {
        private const string KEY_CLIENT_ROOT = "Shenxiao.ProtocolCoverage.ClientRoot";
        private const string KEY_SERVER_ROOT = "Shenxiao.ProtocolCoverage.ServerRoot";

        public const string BASELINE_PATH = "Schemas/ProtocolCoverage/baseline.json";
        public const string KILLLIST_PATH = "Schemas/ProtocolCoverage/killlist.json";
        public const string HARD_NEGATIVE_CONSTRAINTS_PATH =
            "Schemas/ProtocolCoverage/hard_negative_constraints.json";
        public const string REPORT_ROOT = "Reports/ProtocolCoverage";

        private static string ProjectKey(string key)
        {
            return key + ":" + Application.dataPath.GetHashCode();
        }

        private static string FindRepositoryRoot(string repositoryName, string markerPath)
        {
            DirectoryInfo cursor = new DirectoryInfo(Path.GetFullPath(Path.Combine(Application.dataPath, "..")));
            for (int i = 0; cursor != null && i < 6; i++, cursor = cursor.Parent)
            {
                string candidate = Path.Combine(cursor.FullName, repositoryName);
                if (File.Exists(Path.Combine(candidate, markerPath))
                    || Directory.Exists(Path.Combine(candidate, markerPath)))
                {
                    return candidate;
                }
            }

            // 保留原默认形状，让 Validate* 输出可诊断的精确失败路径。
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", repositoryName));
        }

        /// <summary>yu_client(老端 TS,只读事实源)仓库根,默认 ../../yu_client。</summary>
        public static string ClientRoot
        {
            get
            {
                string def = FindRepositoryRoot("yu_client", Path.Combine("cdn", "resource", "game"));
                return EditorPrefs.GetString(ProjectKey(KEY_CLIENT_ROOT), def);
            }
            set { EditorPrefs.SetString(ProjectKey(KEY_CLIENT_ROOT), value); }
        }

        /// <summary>yu_server(Erlang,只读事实源)仓库根,默认 ../../yu_server。</summary>
        public static string ServerRoot
        {
            get
            {
                string def = FindRepositoryRoot("yu_server", Path.Combine("src", "server", "mod_server.erl"));
                return EditorPrefs.GetString(ProjectKey(KEY_SERVER_ROOT), def);
            }
            set { EditorPrefs.SetString(ProjectKey(KEY_SERVER_ROOT), value); }
        }

        /// <summary>老端 TS 源码根(RegisterProtocal/handler 扫描目标)。</summary>
        public static string OldClientSrcRoot => Path.Combine(ClientRoot, "h5", "src");

        /// <summary>协议全集定义(wire 表)。</summary>
        public static string ClientProtocolJsonPath =>
            Path.Combine(ClientRoot, "cdn", "resource", "config", "client", "ClientProtocol.json");

        /// <summary>服务端族级路由表(mod_server.erl 的 "NNN" -> pp_xxx; 分发)。</summary>
        public static string ModServerErlPath => Path.Combine(ServerRoot, "src", "server", "mod_server.erl");

        /// <summary>Unity 侧协议常量定义(仅用于 D 段静态双注册扫描按符号解析数字;
        /// 已注册集合的权威来源是运行时反射,不是这份源码扫描——见 ProtocolCoverageScanner 顶部注释)。</summary>
        public static string ProtoConstsPath => "Assets/Scripts/Framework/Net/Proto.cs";

        public static string UnityScriptsRoot => "Assets/Scripts";

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

        public static bool ValidateServerRoot(out string error)
        {
            error = null;
            if (!File.Exists(ModServerErlPath))
            {
                error = "yu_server 路径不对(找不到 src/server/mod_server.erl): " + ServerRoot;
                return false;
            }
            return true;
        }
    }
}
