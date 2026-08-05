using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Shenxiao.EditorTools.Packaging
{
    /// <summary>
    /// 打包入口。顺序:③ 内容(Addressables→ServerData/[平台]) → ④ 壳(player,自动把本地组拷进 StreamingAssets)。
    /// 发布只需把 ServerData/[平台] 同步到静态服务器(先 bundle 后 catalog);壳不动。
    /// CLI: Unity.exe -batchmode -projectPath . -executeMethod Shenxiao.EditorTools.Packaging.PackagingBuild.BuildAllWebCli -logFile Temp/build.log
    /// </summary>
    public static class PackagingBuild
    {
        private const string WebOutputDir = "Builds/WebGL";
        private const string AndroidOutputApk = "Builds/Android/Shenxiao.apk";
        // 内容输出根(ASTC 手机变体构建时临时切到 ServerData-ASTC)
        private static string _serverDataRoot = "ServerData";

        // ===================== Web 菜单 =====================

        [MenuItem("神霄/打包/Web/① 构建内容(→ServerData-WebGL)", priority = 10)]
        public static void BuildContentWebMenu()
        {
            if (!RequireTarget(BuildTarget.WebGL)) return;
            BuildContent();
        }

        [MenuItem("神霄/打包/Web/② 构建壳(gzip·正式)", priority = 11)]
        public static void BuildWebShellGzipMenu() => BuildWebShell(development: false, brotli: false);

        [MenuItem("神霄/打包/Web/②b 构建壳(Dev·带内存归因)", priority = 12)]
        public static void BuildWebShellSmokeMenu() => BuildWebShell(development: true, brotli: false);

        [MenuItem("神霄/打包/Web/②c 构建壳(Brotli·须https)", priority = 13)]
        public static void BuildWebShellBrotliMenu() => BuildWebShell(development: false, brotli: true);

        [MenuItem("神霄/打包/Web/③ 一条龙(内容+壳)", priority = 14)]
        public static void BuildAllWebMenu()
        {
            if (!RequireTarget(BuildTarget.WebGL)) return;
            if (BuildContent()) BuildWebShell(development: false, brotli: false);
        }

        // ===================== 平台切换 =====================

        [MenuItem("神霄/打包/切换平台/切到 WebGL", priority = 100)]
        public static void SwitchToWebGL()
            => EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

        [MenuItem("神霄/打包/切换平台/切到 Android", priority = 101)]
        public static void SwitchToAndroid()
            => EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        /// <summary>菜单前置检查:激活平台不对就报错并提示切换入口(内容/壳跟随激活平台输出)。</summary>
        private static bool RequireTarget(BuildTarget target)
        {
            if (EditorUserBuildSettings.activeBuildTarget == target) return true;
            Debug.LogError($"[Packaging] 当前激活平台是 {EditorUserBuildSettings.activeBuildTarget},本操作需要 {target}——" +
                           "用 神霄/打包/切换平台 先切(切换会触发按平台重导入,首次约十几分钟)");
            return false;
        }

        /// <summary>CLI 兼容入口(内容构建,输出跟随激活平台)。</summary>
        public static void BuildContentMenu() => BuildContent();

        public static bool BuildContent()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) { Debug.LogError("[Packaging] Addressable settings not found"); return false; }

            // 构建前跑全量自动分组:新增文件(美术新视频/新模型等)自动登记进 Addressables +
            // 图集重烤 + pack 标签自愈(其他工具塞的无标签条目也被补上)。不跑这步,
            // "加了文件但没点自动分组菜单"的资源就会悄悄缺席远端包。
            Shenxiao.EditorTools.AddrSetup.AddressableSetup.AutoGroupAll();
            AssetDatabase.SaveAssets();

            if (!PackLabeler.Validate(settings, out var report))
            {
                Debug.LogError("[Packaging] 拆包校验未通过,中止构建。\n" + report);
                return false;
            }
            Debug.Log(report);

            var started = DateTime.Now;
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            if (!string.IsNullOrEmpty(result.Error))
            {
                Debug.LogError($"[Packaging] 内容构建失败: {result.Error}");
                return false;
            }

            string dir = Path.Combine(_serverDataRoot, EditorUserBuildSettings.activeBuildTarget.ToString());
            PublishRawVideos(dir);
            WriteBuildManifest(dir, result);
            long bytes = 0; int files = 0, bundles = 0;
            if (Directory.Exists(dir))
            {
                foreach (var f in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                {
                    files++; bytes += new FileInfo(f).Length;
                    if (f.EndsWith(".bundle", StringComparison.OrdinalIgnoreCase)) bundles++;
                }
            }
            Debug.Log($"[Packaging] 内容构建完成,耗时 {(DateTime.Now - started).TotalMinutes:F1} 分钟 → {dir}: " +
                      $"{files} 个文件 / {bytes / 1024 / 1024} MB(bundle {bundles} 个,含 catalog_live.bin/.hash)");
            return true;
        }

        /// <summary>
        /// WebGL 不支持 VideoClip 资产播放(仅 URL 流播),创角视频等裸 mp4 直接发布到
        /// ServerData/[平台]/video/ 供 {ResCdn.BaseUrl}/[平台]/video/xxx.mp4 直链。
        /// </summary>
        private static void PublishRawVideos(string serverDataDir)
        {
            const string src = "Assets/GameRes/object/role/video_create";
            if (!Directory.Exists(src)) return;
            string dst = Path.Combine(serverDataDir, "video");
            Directory.CreateDirectory(dst);
            int n = 0;
            foreach (var mp4 in Directory.EnumerateFiles(src, "*.mp4"))
            {
                string target = Path.Combine(dst, Path.GetFileName(mp4));
                // mp4 已含内容,按文件大小+时间跳过未变的(发布脚本以后按 hash 做增量)
                var s = new FileInfo(mp4); var t = new FileInfo(target);
                if (t.Exists && t.Length == s.Length && t.LastWriteTimeUtc >= s.LastWriteTimeUtc) continue;
                File.Copy(mp4, target, true);
                n++;
            }
            if (n > 0) Debug.Log($"[Packaging] 发布裸视频 {n} 个 → {dst}(WebGL URL 播法用)");
        }

        public static bool BuildWebShell(bool development, bool brotli)
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                Debug.LogError("[Packaging] 当前激活平台不是 WebGL,请先切平台再构建 Web 壳");
                return false;
            }

            // 自定义模板:官方 Default 底座 + sx-boot 加载层(引擎 0~85% + 游戏侧 BootOverlay 85~100%)。
            // ⚠ 必须是 Default 底座——自写全屏 canvas 页会让 TMP 输入框键盘失灵(2026-07-12 实证)。
            PlayerSettings.WebGL.template = "PROJECT:Shenxiao";

            // http 部署必须 gzip:Chrome/Firefox 仅在 https 下接受 Content-Encoding: br。
            // 注意 Development 构建会自动跳过压缩(产物无 .gz 后缀,体积巨大但零头痛)。
            PlayerSettings.WebGL.compressionFormat = brotli ? WebGLCompressionFormat.Brotli : WebGLCompressionFormat.Gzip;

            // IL2CPP 按体积优化:wasm 代码段 -20~30%(运行时略慢,Web 首包体积优先)。
            PlayerSettings.SetIl2CppCodeGeneration(UnityEditor.Build.NamedBuildTarget.WebGL,
                UnityEditor.Build.Il2CppCodeGeneration.OptimizeSize);

            var scenes = CollectScenes();
            if (scenes == null) return false;
            var opts = new BuildPlayerOptions
            {
                scenes = scenes,
                target = BuildTarget.WebGL,
                locationPathName = WebOutputDir,
                options = development ? BuildOptions.Development : BuildOptions.None,
            };

            // Addressables 设置里的 0 是 PreferencesValue，不是“关闭随 Player 构建”。
            // 若用户全局偏好为 true，所谓“只打壳”会暗中重建 ServerData/catalog，既慢又可能
            // 让壳与发布内容失配。Player 永远复用本工作区最近一次成套内容；内容只由 BuildContent 产生。
            AddressableAssetSettings addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetSettings.PlayerBuildOption previousPlayerBuildOption =
                addressableSettings != null
                    ? addressableSettings.BuildAddressablesWithPlayerBuild
                    : AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
            UnityEditor.Build.Reporting.BuildReport report;
            try
            {
                if (addressableSettings != null)
                    addressableSettings.BuildAddressablesWithPlayerBuild =
                        AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
                report = BuildPipeline.BuildPlayer(opts);
            }
            finally
            {
                if (addressableSettings != null)
                    addressableSettings.BuildAddressablesWithPlayerBuild = previousPlayerBuildOption;
            }
            bool ok = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
            var msg = $"[Packaging] Web 壳构建 {report.summary.result} → {WebOutputDir}, " +
                      $"{(long)report.summary.totalSize / 1024 / 1024} MB, 耗时 {report.summary.totalTime:hh\\:mm\\:ss} " +
                      $"({(development ? "Development" : "Release")}+{(brotli ? "Brotli(须https)" : "gzip")})";
            if (ok) Debug.Log(msg); else Debug.LogError(msg);
            return ok;
        }

        private static string[] CollectScenes()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                // Build Settings 里场景被禁用时批处理会去打"untitled scene"直接失败,兜底到启动场景。
                const string launch = "Assets/_App/Scenes/Launch.unity";
                if (!File.Exists(launch)) { Debug.LogError("[Packaging] Build Settings 无启用场景且找不到 " + launch); return null; }
                Debug.LogWarning("[Packaging] Build Settings 无启用场景,兜底使用 " + launch);
                scenes = new[] { launch };
            }
            return scenes;
        }

        // ===================== Android =====================

        /// <summary>流式小包的 CDN 基址:构建期临时注入 AppConfig、构完还原(Web 走 boot_config.json 不烧包,
        /// Android 无页面同源可拉,只能烧;换地址改这一处常量重打 APK)。</summary>
        private const string AndroidCdnBaseUrl = "http://223.109.142.26:89/cdn";
        /// <summary>整包的 CDN 令牌:运行时由 ResCdn 解析成 StreamingAssets 包内路径,零网络依赖。</summary>
        private const string StreamingCdnToken = "{streaming}/cdn";
        private const string StreamingContentDir = "Assets/StreamingAssets/cdn/Android";
        private const string AndroidFullOutputApk = "Builds/Android/Shenxiao_full.apk";

        [MenuItem("神霄/打包/Android/① 构建内容(→ServerData-Android)", priority = 50)]
        public static void BuildContentAndroidMenu()
        {
            if (!RequireTarget(BuildTarget.Android)) return;
            BuildContent();
        }

        [MenuItem("神霄/打包/Android/② 壳APK(流式小包·连CDN)", priority = 51)]
        public static void BuildAndroidShellReleaseMenu() => BuildAndroidShell(development: false);

        [MenuItem("神霄/打包/Android/②b 壳APK(流式小包·Dev调试)", priority = 52)]
        public static void BuildAndroidShellDevMenu() => BuildAndroidShell(development: true);

        [MenuItem("神霄/打包/Android/③ 整包APK(资源全内置·免服务器)", priority = 53)]
        public static void BuildAndroidFullMenu() => BuildAndroidFullApk(development: false);

        [MenuItem("神霄/打包/Android/④ 一条龙(内容+流式小包)", priority = 54)]
        public static void BuildAllAndroidMenu()
        {
            if (!RequireTarget(BuildTarget.Android)) return;
            if (BuildContent()) BuildAndroidShell(development: false);
        }

        [MenuItem("神霄/打包/Android/⑤ 一条龙(内容+整包)", priority = 55)]
        public static void BuildAllAndroidFullMenu()
        {
            if (!RequireTarget(BuildTarget.Android)) return;
            if (BuildContent()) BuildAndroidFullApk(development: false);
        }

        /// <summary>流式小包 APK(~65MB,资源走 CDN 按需下载落盘)。</summary>
        public static bool BuildAndroidShell(bool development)
            => BuildAndroidApk(development, AndroidCdnBaseUrl, AndroidOutputApk, "流式小包");

        /// <summary>整包 APK(内容全拷进 StreamingAssets,~950MB,零网络/免服务器):
        /// 复用最近一次 Android 内容构建的 ServerData/Android(没有就先跑菜单①);
        /// 构建期拷入 Assets/StreamingAssets/cdn/Android + 烧 {streaming}/cdn,构完自动清理还原。</summary>
        public static bool BuildAndroidFullApk(bool development)
        {
            if (!RequireTarget(BuildTarget.Android)) return false;
            string src = Path.Combine("ServerData", "Android");
            if (!File.Exists(Path.Combine(src, "catalog_live.bin")))
            {
                Debug.LogError("[Packaging] 整包需要先跑一次 Android 内容构建(神霄/打包/Android/①)");
                return false;
            }

            Debug.Log("[Packaging] 整包:拷贝内容进 StreamingAssets(约 900MB,稍等)…");
            if (Directory.Exists(StreamingContentDir)) Directory.Delete(StreamingContentDir, true);
            Directory.CreateDirectory(StreamingContentDir);
            int copied = 0;
            foreach (string f in Directory.EnumerateFiles(src, "*", SearchOption.AllDirectories))
            {
                string rel = Path.GetRelativePath(src, f);
                // 排除非运行时文件:发布清单;video/ 是 WebGL URL 流播的裸 mp4(Android 走 bundle 内 VideoClip)
                if (rel == "build_manifest.json" || rel.StartsWith("video", StringComparison.OrdinalIgnoreCase)) continue;
                string dst = Path.Combine(StreamingContentDir, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(dst));
                File.Copy(f, dst, true);
                copied++;
            }
            AssetDatabase.Refresh();
            Debug.Log($"[Packaging] 整包:已拷入 {copied} 个文件,开始出包");
            try
            {
                return BuildAndroidApk(development, StreamingCdnToken, AndroidFullOutputApk, "整包");
            }
            finally
            {
                if (Directory.Exists(StreamingContentDir)) Directory.Delete(StreamingContentDir, true);
                string meta = StreamingContentDir + ".meta";
                if (File.Exists(meta)) File.Delete(meta);
                // StreamingAssets/cdn 层级也清掉(若因此变空)
                string cdnDir = Path.GetDirectoryName(StreamingContentDir);
                if (Directory.Exists(cdnDir) && Directory.GetFileSystemEntries(cdnDir).Length == 0)
                {
                    Directory.Delete(cdnDir);
                    if (File.Exists(cdnDir + ".meta")) File.Delete(cdnDir + ".meta");
                }
                AssetDatabase.Refresh();
            }
        }

        /// <summary>Android 出包公共段:注入 CDN(构完还原)→ 包名校正 → BuildPlayer。</summary>
        private static bool BuildAndroidApk(bool development, string cdnBaseUrl, string outputPath, string tag)
        {
            if (!RequireTarget(BuildTarget.Android)) return false;

            var appConfig = AssetDatabase.LoadAssetAtPath<Shenxiao.Framework.Config.AppConfig>(
                "Assets/_App/Configs/AppConfig.asset");
            if (appConfig == null)
            {
                Debug.LogError("[Packaging] AppConfig.asset 缺失");
                return false;
            }
            string prevCdn = appConfig.addressablesCdnBaseUrl;
            appConfig.addressablesCdnBaseUrl = cdnBaseUrl;
            EditorUtility.SetDirty(appConfig);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Packaging] Android {tag} 注入 CDN: {cdnBaseUrl}(构建后还原为 {prevCdn})");
            try
            {
                // 包名仍是 URP 模板默认值时自动改正(com.UnityTechnologies.* 无法作为正式包名)
                string appId = PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android);
                if (string.IsNullOrEmpty(appId) || appId.Contains("UnityTechnologies") || appId.Contains("unity.template"))
                {
                    PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.shenxiao.game");
                    Debug.Log("[Packaging] Android 包名自动设为 com.shenxiao.game(原为模板默认值)");
                }

                var scenes = CollectScenes();
                if (scenes == null) return false;
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                var opts = new BuildPlayerOptions
                {
                    scenes = scenes,
                    target = BuildTarget.Android,
                    locationPathName = outputPath,
                    options = development ? BuildOptions.Development : BuildOptions.None,
                };

                var report = BuildPipeline.BuildPlayer(opts);
                bool ok = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
                long apkBytes = File.Exists(outputPath) ? new FileInfo(outputPath).Length : 0;
                var msg = $"[Packaging] Android {tag}构建 {report.summary.result} → {outputPath}, " +
                          $"APK {apkBytes / 1024 / 1024} MB, 耗时 {report.summary.totalTime:hh\\:mm\\:ss} " +
                          $"({(development ? "Development" : "Release")}·debug签名)";
                if (ok) Debug.Log(msg); else Debug.LogError(msg);
                return ok;
            }
            finally
            {
                appConfig.addressablesCdnBaseUrl = prevCdn;
                EditorUtility.SetDirty(appConfig);
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>CLI:Android 内容+流式小包一条龙(启动参数须带 -buildTarget Android)。退出码 0/2/3。</summary>
        public static void BuildAllAndroidCli()
        {
            if (!BuildContent()) { EditorApplication.Exit(2); return; }
            if (!BuildAndroidShell(development: false)) { EditorApplication.Exit(3); return; }
            EditorApplication.Exit(0);
        }

        /// <summary>CLI:Android 内容+整包一条龙(启动参数须带 -buildTarget Android)。退出码 0/2/3。</summary>
        public static void BuildAllAndroidFullCli()
        {
            if (!BuildContent()) { EditorApplication.Exit(2); return; }
            if (!BuildAndroidFullApk(development: false)) { EditorApplication.Exit(3); return; }
            EditorApplication.Exit(0);
        }

        // ===================== ASTC 手机内容变体 =====================

        /// <summary>手机内容变体(ASTC 纹理):输出 ServerData-ASTC/[平台],运行时由 AppConfig.astcCdnBaseUrl +
        /// 设备 DXT 探测自动选源。⚠切纹理子目标触发全量纹理重导(数小时,恢复 DXT 再来一遍),跑前先约时间。</summary>
        [MenuItem("神霄/打包/通用/ASTC内容变体(Web跑手机浏览器用·重导数小时)", priority = 201)]
        public static void BuildContentAstcMenu() => BuildContentAstc();

        public static bool BuildContentAstc()
        {
            var prevSubtarget = EditorUserBuildSettings.webGLBuildSubtarget;
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) { Debug.LogError("[Packaging] Addressable settings not found"); return false; }
            string profileId = settings.activeProfileId;
            string prevBuildPath = settings.profileSettings.GetValueByName(profileId, "Remote.BuildPath");
            try
            {
                EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.ASTC;
                settings.profileSettings.SetValue(profileId, "Remote.BuildPath", "ServerData-ASTC/[BuildTarget]");
                _serverDataRoot = "ServerData-ASTC";
                return BuildContent();
            }
            finally
            {
                _serverDataRoot = "ServerData";
                settings.profileSettings.SetValue(profileId, "Remote.BuildPath", prevBuildPath);
                EditorUserBuildSettings.webGLBuildSubtarget = prevSubtarget;
                AssetDatabase.SaveAssets();
            }
        }

        // ===================== 构建清单与 GC =====================

        /// <summary>记录本次构建引用的文件清单(发布 GC 依据):ServerData 只增不删,旧 bundle 靠它辨认。</summary>
        private static void WriteBuildManifest(string dir, AddressablesPlayerBuildResult result)
        {
            try
            {
                var files = new List<string>();
                string root = Path.GetFullPath(dir);
                foreach (string p in result.FileRegistry.GetFilePaths())
                {
                    if (string.IsNullOrEmpty(p)) continue;
                    string full = Path.GetFullPath(p);
                    if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) continue;
                    files.Add(full.Substring(root.Length + 1).Replace('\\', '/'));
                }
                files.Sort();
                var sb = new System.Text.StringBuilder();
                sb.Append("{\n  \"builtAt\": \"").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append("\",\n  \"files\": [\n");
                for (int i = 0; i < files.Count; i++)
                    sb.Append("    \"").Append(files[i]).Append(i == files.Count - 1 ? "\"\n" : "\",\n");
                sb.Append("  ]\n}\n");
                File.WriteAllText(Path.Combine(dir, "build_manifest.json"), sb.ToString());
                Debug.Log($"[Packaging] build_manifest.json 已写:{files.Count} 个文件在册");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Packaging] 写构建清单失败(不影响构建): " + e.Message);
            }
        }

        /// <summary>按 build_manifest.json 清理不再被当前 catalog 引用的旧 .bundle。
        /// 只删 .bundle;catalog/清单/video 永不动。⚠生产发布纪律:先发 bundle 再发 catalog,
        /// GC 要等所有在线客户端都拿到新 catalog 后再跑(测试环境随时跑)。</summary>
        [MenuItem("神霄/打包/通用/清理旧内容(按清单GC ServerData)", priority = 202)]
        public static void GcServerDataMenu()
            => GcServerData(Path.Combine("ServerData", EditorUserBuildSettings.activeBuildTarget.ToString()));

        public static void GcServerData(string dir)
        {
            string manifestPath = Path.Combine(dir, "build_manifest.json");
            if (!File.Exists(manifestPath))
            {
                Debug.LogError("[Packaging] 无 build_manifest.json(先跑一次内容构建生成清单)");
                return;
            }
            var keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (System.Text.RegularExpressions.Match m in
                     System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(manifestPath), "\"([^\"]+\\.bundle)\""))
                keep.Add(m.Groups[1].Value);
            string root = Path.GetFullPath(dir);
            long freedBytes = 0;
            int removed = 0;
            foreach (string f in Directory.EnumerateFiles(dir, "*.bundle", SearchOption.AllDirectories))
            {
                string rel = Path.GetFullPath(f).Substring(root.Length + 1).Replace('\\', '/');
                if (keep.Contains(rel)) continue;
                freedBytes += new FileInfo(f).Length;
                removed++;
                File.Delete(f);
            }
            Debug.Log($"[Packaging] ServerData GC: 删除旧 bundle {removed} 个,释放 {freedBytes / 1024 / 1024} MB(在册保留 {keep.Count} 个)");
        }

        /// <summary>CLI:仅重打 Web 壳(Release+gzip),内容不动。退出码 0=成功 3=失败。</summary>
        public static void BuildWebShellOnlyCli()
        {
            EditorApplication.Exit(BuildWebShell(development: false, brotli: false) ? 0 : 3);
        }

        /// <summary>CLI:内容+Web壳 一条龙(Release+gzip,http 可部署)。退出码 0=成功 2=内容失败 3=壳失败。</summary>
        public static void BuildAllWebCli()
        {
            if (!BuildContent()) { EditorApplication.Exit(2); return; }
            if (!BuildWebShell(development: false, brotli: false)) { EditorApplication.Exit(3); return; }
            EditorApplication.Exit(0);
        }

        /// <summary>CLI:配置迁移+自动分组+内容+Web壳 全链一条龙。退出码 0=成功 2=内容失败 3=壳失败 4=前置失败。</summary>
        public static void SetupAndBuildAllWebCli()
        {
            try
            {
                PackagingSetup.MigrateSettings();
                Shenxiao.EditorTools.AddrSetup.AddressableSetup.AutoGroupAll();
            }
            catch (Exception e)
            {
                Debug.LogError("[Packaging] 配置迁移/自动分组失败: " + e);
                EditorApplication.Exit(4);
                return;
            }
            BuildAllWebCli();
        }
    }
}
