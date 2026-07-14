using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Shenxiao.Common.Proto;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Scene3D.Map;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Dialogue;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Scene;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Shenxiao.Module.Core.Preload
{
    public enum LegacyPreloadStage
    {
        Boot,
        RoleSelection,
        GameStart,
        SceneMap,
    }

    public static class LegacyPreloadService
    {
        public delegate void ProgressHandler(LegacyPreloadStage stage, float progress, string hint);

        private const string PRELOAD_CONFIG = "configpreloadreslist";
        private const int WarmConcurrency = 8;
        private static readonly Dictionary<string, UnityEngine.Object> _retained =
            new Dictionary<string, UnityEngine.Object>();
        // 预热资产按阶段登记,阶段过期即整批归还(不登记则永久驻留:选角页全职业模型、
        // 每张到过的地图的首屏瓦片都会钉死在 wasm 堆里)。
        private static readonly Dictionary<LegacyPreloadStage, HashSet<string>> _retainedByStage =
            new Dictionary<LegacyPreloadStage, HashSet<string>>();
        private static readonly Dictionary<string, bool> _keyExists =
            new Dictionary<string, bool>();
        // 当前 SceneMap 桶预热对应的场景:同场景重进(首次进世界紧跟 12005/进出同图副本)跳过
        // 释放-重热返工——GameStart 刚预热好的首屏瓦片被释放再重载,白耗且拖慢首屏。
        private static int _retainedSceneId;

        private static bool _legacyLoaded;
        private static List<PreloadEntry> _legacyCommonEntries;

        public static event ProgressHandler ProgressChanged;

        public static async Task PreloadBootAsync(IEnumerable<string> appPreloadKeys, Action<float, string> progress = null)
        {
            var entries = new Dictionary<string, PreloadEntry>();
            // ⚠ 不要在 Boot 拉 GetLegacyCommonEntries():那份 ConfigPreloadResList.package_common_res(4453条)
            // 在老端是【打包进安装包免下载】的构建期清单(唯一消费者 PickUpPackageFile.js),不是启动下载清单!
            // 老端出登录页前只强制加载登录模块自身(几 MB);误把全清单塞 Boot 曾让登录前硬下 255 keys/102MB。
            // 该清单改为进游戏后后台预取(BackgroundPrefetchLegacyAsync),对齐"进包免下载→后台补齐"语义。
            AddManualBootEntries(entries);
            if (appPreloadKeys != null)
            {
                foreach (string key in appPreloadKeys)
                {
                    AddEntry(entries, key, PreloadAssetKind.Prefab);
                }
            }

            await RunStageAsync(LegacyPreloadStage.Boot, entries, progress);
        }

        /// <summary>
        /// 进游戏后台预取老端"进包免下载"清单(只下载进缓存,不预热实例化)。fire-and-forget,
        /// 失败静默(纯预热性质,业务按需加载自会兜底)。调用点:LoginFlow 收到 EVT_GAME_ENTERED 后。
        /// </summary>
        public static async Task BackgroundPrefetchLegacyAsync()
        {
            // 进世界即归还 Boot/选角阶段的预热引用:登录模块、全职业展示模型/视频进游戏后不再需要,
            // 在用的资产(如主角自己的模型)由使用方自己的引用计数保活,不受影响。
            ReleaseStageRetained(LegacyPreloadStage.Boot);
            ReleaseStageRetained(LegacyPreloadStage.RoleSelection);
            try
            {
                List<PreloadEntry> legacy = await GetLegacyCommonEntries();
                var entries = new Dictionary<string, PreloadEntry>();
                AddEntries(entries, legacy);
                List<PreloadEntry> valid = await FilterExistingAsync(entries.Values);
                if (valid.Count == 0) return;
                var keys = new List<string>(valid.Count);
                foreach (PreloadEntry e in valid) keys.Add(e.Key);
                long size = await ResManager.GetDownloadSize(keys);
                if (size <= 0) { GameLog.Info("Preload", "后台预取:清单已全部在缓存"); return; }
                GameLog.Info("Preload", "后台预取启动: {0} keys, {1} KB(进游戏后静默补齐;ResManager 分批下载压峰值)",
                    keys.Count, size / 1024);
                await ResManager.DownloadAsync(keys, null);
                GameLog.Info("Preload", "后台预取完成");
            }
            catch (Exception e)
            {
                GameLog.Info("Preload", "后台预取跳过: {0}", e.Message);
            }
        }

        /// <summary>归还某阶段预热持有的全部资产引用(引用计数-1,归零才真正卸载;在用资产不受影响)。</summary>
        public static void ReleaseStageRetained(LegacyPreloadStage stage)
        {
            if (!_retainedByStage.TryGetValue(stage, out HashSet<string> keys) || keys.Count == 0) return;
            int released = 0;
            foreach (string key in keys)
            {
                if (_retained.TryGetValue(key, out UnityEngine.Object asset))
                {
                    _retained.Remove(key);
                    ResManager.Release(asset);
                    released++;
                }
            }
            keys.Clear();
            if (stage == LegacyPreloadStage.SceneMap) _retainedSceneId = 0;
            if (released > 0) GameLog.Info("Preload", "released {0} retained assets of stage {1}", released, stage);
        }

        private static void Retain(LegacyPreloadStage stage, string key, UnityEngine.Object asset)
        {
            // 地图键(首屏瓦片/底图/寻路bytes)一律归 SceneMap 桶:首图经 GameStart 预热,
            // 不改桶则换图释放不到它,每到一张新图就永久多驻一屏瓦片。
            if (key.StartsWith("resource/game/scene/map/", StringComparison.Ordinal))
                stage = LegacyPreloadStage.SceneMap;
            _retained[key] = asset;
            if (!_retainedByStage.TryGetValue(stage, out HashSet<string> set))
            {
                set = new HashSet<string>();
                _retainedByStage[stage] = set;
            }
            set.Add(key);
        }

        public static async Task PreloadRoleSelectionAsync(Action<float, string> progress = null)
        {
            var entries = new Dictionary<string, PreloadEntry>();
            await LoginConfigs.EnsureLoaded();

            AddLoginRoleUiEntries(entries);
            List<LoginConfigs.CareerOption> options = LoginConfigs.CreateRoleOptions();
            for (int i = 0; i < options.Count; i++)
            {
                LoginConfigs.CareerOption option = options[i];
                AddCreateRoleOptionEntries(entries, option);
                LoginConfigs.CareerRes res = LoginConfigs.GetCreateRes(option.Career, option.Sex);
                if (res != null)
                {
                    // 创角页优先加载展示视频(整模 model_create_* 已废弃删除),与 RoleCreateView.TryShowVideo
                    // 的实际 key 对齐;不加则选职业时视频冷加载慢半拍。未交付视频的职业会被存在性过滤掉,无害。
                    string videoBase = $"object/role/video_create/{res.RoleRes}@";
                    AddEntry(entries, videoBase + "create2", PreloadAssetKind.Video);
                    AddEntry(entries, videoBase + "create3", PreloadAssetKind.Video);

                    await AddRoleModelSpecAsync(entries, new RoleModelSpec
                    {
                        Career = option.Career,
                        ClotheRes = res.RoleRes,
                        HeadRes = res.HeadRes,
                        WeaponRes = res.WeaponRes,
                        Actions = LoginConfigs.RoleUIActions("LoginCreateRoleView"),
                        AutoPlayActions = false,
                    });

                    foreach ((string bone, string name) in LoginConfigs.CreateRoleEffects(option.Career, option.Sex))
                    {
                        if (!string.IsNullOrEmpty(name))
                            AddEntry(entries, GameResPath.GetEffectPrefabPath("skills_effect", name), PreloadAssetKind.Prefab);
                    }
                }
            }

            IReadOnlyList<GameRoleInfo> roles = LoginModel.Instance.Roles;
            for (int i = 0; i < roles.Count; i++)
            {
                GameRoleInfo role = roles[i];
                string headIcon = LoginConfigs.HeadIconPath(role.Career, role.Turn);
                if (!string.IsNullOrEmpty(headIcon)) AddEntry(entries, headIcon, PreloadAssetKind.Sprite);

                LoginConfigs.CareerOption option = FindOption(options, role.Career);
                if (option == null) continue;
                LoginConfigs.CareerRes res = LoginConfigs.GetCreateRes(option.Career, option.Sex);
                if (res == null) continue;

                FigureProto figure = role.figure;
                await AddRoleModelSpecAsync(entries, new RoleModelSpec
                {
                    Career = option.Career,
                    ClotheRes = figure != null && figure.ClotheModelId > 0 ? figure.ClotheModelId : res.RoleRes,
                    HeadRes = figure != null && figure.HeadModelId > 0 ? figure.HeadModelId : res.HeadRes,
                    WeaponRes = figure != null && figure.WeaponModelId > 0 ? figure.WeaponModelId : res.WeaponRes,
                    WingId = figure != null ? figure.WingId : 0,
                    BackOrnamentId = figure != null ? figure.BackOrnamentId : 0,
                    Actions = LoginConfigs.RoleUIActions("LoginSelectRoleView"),
                });
            }

            await RunStageAsync(LegacyPreloadStage.RoleSelection, entries, progress);
        }

        public static async Task PreloadGameStartAsync(Action<float, string> progress = null)
        {
            var entries = new Dictionary<string, PreloadEntry>();
            AddEntry(entries, GameResPath.GetUIPrefab("mainUI", "MainUIModule"), PreloadAssetKind.Prefab);
            AddEntry(entries, GameResPath.GetClientConfigPath("configfunctionicon"), PreloadAssetKind.Text);
            AddEntry(entries, GameResPath.GetServerConfigPath("config_scene"), PreloadAssetKind.Text);
            AddEntry(entries, GameResPath.GetServerConfigPath("config_npc"), PreloadAssetKind.Text);
            AddEntry(entries, GameResPath.GetServerConfigPath("config_mon"), PreloadAssetKind.Text);
            AddEntry(entries, GameResPath.GetServerConfigPath("config_skill"), PreloadAssetKind.Text);
            AddEntry(entries, GameResPath.GetClientConfigPath("configskillui"), PreloadAssetKind.Text);

            await LoginConfigs.EnsureLoaded();
            await MainUIConfigs.EnsureLoaded();
            await MainUIConfigs.EnsureSceneLoaded();
            await NpcConfigs.EnsureLoaded();
            await MonsterConfigs.EnsureLoaded();

            RoleModel role = RoleModel.Instance;
            if (role.HasBaseInfo && role.Figure != null)
            {
                await AddRoleModelSpecAsync(entries, await BuildMainRoleSpecAsync(role));
                await AddSceneMapEntriesAsync(entries, role.SceneId, role.X, role.Y);
            }

            await RunStageAsync(LegacyPreloadStage.GameStart, entries, progress);
        }

        public static async Task PreloadSceneMapAsync(int sceneId, int focusX, int focusY, Action<float, string> progress = null)
        {
            // 同场景已预热(GameStart 或上一次 12005):跳过,不做释放-重热返工。
            if (sceneId == _retainedSceneId
                && _retainedByStage.TryGetValue(LegacyPreloadStage.SceneMap, out HashSet<string> held)
                && held.Count > 0)
            {
                Report(LegacyPreloadStage.SceneMap, progress, 1f, "场景资源已预热");
                return;
            }
            // 换图先归还上一张图的预热引用(首屏瓦片/底图/bytes);
            // 视图仍在展示的瓦片有自己的引用计数,释放预热引用不会把它们卸掉。
            ReleaseStageRetained(LegacyPreloadStage.SceneMap);
            var entries = new Dictionary<string, PreloadEntry>();
            await AddSceneMapEntriesAsync(entries, sceneId, focusX, focusY);
            await RunStageAsync(LegacyPreloadStage.SceneMap, entries, progress);
        }

        private static async Task RunStageAsync(LegacyPreloadStage stage,
            Dictionary<string, PreloadEntry> entries, Action<float, string> progress)
        {
            try
            {
                Report(stage, progress, 0f, "检查资源");
                List<PreloadEntry> valid = await FilterExistingAsync(entries.Values);
                if (valid.Count == 0)
                {
                    Report(stage, progress, 1f, "无可预加载资源");
                    return;
                }

                List<string> keys = new List<string>(valid.Count);
                for (int i = 0; i < valid.Count; i++)
                {
                    if (ShouldDownload(stage, valid[i])) keys.Add(valid[i].Key);
                }

                long size = keys.Count > 0 ? await ResManager.GetDownloadSize(keys) : 0;
                if (size > 0)
                {
                    GameLog.Info("Preload", "{0}: download {1} keys, {2} KB", stage, keys.Count, size / 1024);
                    await ResManager.DownloadAsync(keys, p => Report(stage, progress, 0.1f + p * 0.45f, "下载资源"));
                }
                else
                {
                    Report(stage, progress, 0.55f, "资源已缓存");
                }

                await WarmAsync(stage, valid, progress, 0.55f, 1f);
                Report(stage, progress, 1f, "加载完成");
            }
            catch (Exception e)
            {
                GameLog.Warn("Preload", "{0}: preload skipped by error: {1}", stage, e.Message);
                Report(stage, progress, 1f, "加载跳过");
            }
        }

        private static async Task<List<PreloadEntry>> FilterExistingAsync(IEnumerable<PreloadEntry> entries)
        {
            // 并发探测存在性:几百个 key 一个个 await 会卡几百帧(每次 LoadResourceLocationsAsync 都按帧推进,
            // 这正是启动慢的根因)。一次性并发发起、统一 await,Addressables 在少数几帧内全处理完;
            // 已缓存的直接取值、不发起异步。所有续体都在主线程协作执行,字典读写无需加锁。
            var list = new List<PreloadEntry>();
            foreach (PreloadEntry entry in entries)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.Key)) list.Add(entry);
            }

            var checks = new Task<bool>[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                string cacheKey = list[i].Kind + ":" + list[i].Key;
                checks[i] = _keyExists.TryGetValue(cacheKey, out bool known)
                    ? Task.FromResult(known)
                    : EntryExistsAsync(list[i]);
            }
            bool[] results = await Task.WhenAll(checks);

            var valid = new List<PreloadEntry>();
            int missing = 0;
            for (int i = 0; i < list.Count; i++)
            {
                _keyExists[list[i].Kind + ":" + list[i].Key] = results[i];
                if (results[i]) valid.Add(list[i]);
                else missing++;
            }
            if (missing > 0)
            {
                GameLog.Warn("Preload", "legacy preload skipped {0} missing/unconverted keys", missing);
            }
            return valid;
        }

        private static Task<bool> EntryExistsAsync(PreloadEntry entry)
        {
            switch (entry.Kind)
            {
                case PreloadAssetKind.Text:
                    return ResManager.KeyExistsAsync<TextAsset>(entry.Key);
                case PreloadAssetKind.Sprite:
                    return ResManager.KeyExistsAsync<Sprite>(entry.Key);
                case PreloadAssetKind.Prefab:
                    return ResManager.KeyExistsAsync<GameObject>(entry.Key);
                case PreloadAssetKind.Animation:
                    return ResManager.KeyExistsAsync<AnimationClip>(entry.Key);
                case PreloadAssetKind.Video:
                    return ResManager.KeyExistsAsync<UnityEngine.Video.VideoClip>(entry.Key);
                default:
                    return ResManager.KeyExistsAsync(entry.Key);
            }
        }

        private static async Task WarmAsync(LegacyPreloadStage stage, List<PreloadEntry> entries,
            Action<float, string> progress, float start, float end)
        {
            var toWarm = new List<PreloadEntry>();
            for (int i = 0; i < entries.Count; i++)
            {
                if (ShouldWarm(stage, entries[i])) toWarm.Add(entries[i]);
            }
            if (toWarm.Count == 0)
            {
                Report(stage, progress, end, "依赖已就绪");
                return;
            }

            // 有界并发预热:逐个串行 await 同样会卡上百帧;并发上限封顶,避免一帧涌入太多真实资源加载/解码尖刺。
            // next/done 的自增都在 await 之间的同步段完成,主线程协作调度下天然原子,无需加锁。
            int done = 0;
            int next = 0;

            async Task WarmWorkerAsync()
            {
                while (true)
                {
                    int i = next++;
                    if (i >= toWarm.Count) return;
                    await WarmOneAsync(stage, toWarm[i]);
                    done++;
                    float p = start + (end - start) * done / toWarm.Count;
                    Report(stage, progress, p, "预热资源");
                }
            }

            int concurrency = Mathf.Min(WarmConcurrency, toWarm.Count);
            var workers = new Task[concurrency];
            for (int w = 0; w < concurrency; w++) workers[w] = WarmWorkerAsync();
            await Task.WhenAll(workers);
        }

        private static bool ShouldWarm(LegacyPreloadStage stage, PreloadEntry entry)
        {
            if (entry == null || entry.Kind == PreloadAssetKind.DependencyOnly) return false;
            // 3D 模型/动作原先被无差别拦掉 → 预热对它们等于 no-op,创角/选角/进场模型必然冷加载
            // (="模型慢半拍"的根因)。RoleSelection/GameStart 阶段的 3D 条目正是下一屏要展示的模型,
            // 放行真正 LoadOptionalAsync 进内存(ResManager 资产缓存接住,视图侧加载时同步命中)。
            bool warm3D = stage == LegacyPreloadStage.RoleSelection || stage == LegacyPreloadStage.GameStart;
            if (entry.Kind == PreloadAssetKind.Animation) return warm3D;
            if (entry.Kind == PreloadAssetKind.Prefab && IsRuntime3DKey(entry.Key)) return warm3D;
            if (stage == LegacyPreloadStage.Boot
                && entry.Key.StartsWith("resource/game/scene/map/", StringComparison.Ordinal))
            {
                return false;
            }
            return true;
        }

        private static bool ShouldDownload(LegacyPreloadStage stage, PreloadEntry entry)
        {
            if (entry == null) return false;
#if UNITY_EDITOR
            if (Application.isEditor
                && (entry.Kind == PreloadAssetKind.Animation || IsRuntime3DKey(entry.Key)))
            {
                return false;
            }
#endif
            return true;
        }

        private static async Task WarmOneAsync(LegacyPreloadStage stage, PreloadEntry entry)
        {
            if (_retained.ContainsKey(entry.Key)) return;
            UnityEngine.Object asset = null;
            switch (entry.Kind)
            {
                case PreloadAssetKind.Text:
                    asset = await ResManager.LoadOptionalAsync<TextAsset>(entry.Key);
                    break;
                case PreloadAssetKind.Sprite:
                    asset = await ResManager.LoadOptionalAsync<Sprite>(entry.Key);
                    break;
                case PreloadAssetKind.Prefab:
                    asset = await ResManager.LoadOptionalAsync<GameObject>(entry.Key);
                    break;
                case PreloadAssetKind.Animation:
                    asset = await ResManager.LoadOptionalAsync<AnimationClip>(entry.Key);
                    break;
                case PreloadAssetKind.Video:
                    asset = await ResManager.LoadOptionalAsync<UnityEngine.Video.VideoClip>(entry.Key);
                    break;
            }

            if (asset != null) Retain(stage, entry.Key, asset);
        }

        private static void Report(LegacyPreloadStage stage, Action<float, string> progress, float p, string hint)
        {
            float clamped = Mathf.Clamp01(p);
            progress?.Invoke(clamped, hint);
            ProgressChanged?.Invoke(stage, clamped, hint);
        }

        private static async Task<List<PreloadEntry>> GetLegacyCommonEntries()
        {
            if (_legacyLoaded) return _legacyCommonEntries;
            _legacyLoaded = true;
            _legacyCommonEntries = new List<PreloadEntry>();

            string key = GameResPath.GetClientConfigPath(PRELOAD_CONFIG);
            TextAsset asset = await LoadLegacyManifestAsync(key);
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
            {
                GameLog.Warn("Preload", "legacy preload manifest missing: {0}(跑 神霄/配表/同步客户端配置)", key);
                return _legacyCommonEntries;
            }

            try
            {
                LegacyPreloadRoot root = JsonConvert.DeserializeObject<LegacyPreloadRoot>(asset.text);
                if (root?.PackageCommonRes == null) return _legacyCommonEntries;

                var entries = new Dictionary<string, PreloadEntry>();
                for (int i = 0; i < root.PackageCommonRes.Count; i++)
                {
                    AddLegacyPath(entries, root.PackageCommonRes[i]);
                }
                _legacyCommonEntries.AddRange(entries.Values);
                GameLog.Info("Preload", "legacy preload manifest loaded: {0} mapped keys", _legacyCommonEntries.Count);
            }
            catch (JsonException e)
            {
                GameLog.Error("Preload", "legacy preload manifest parse failed: {0}", e.Message);
            }

            return _legacyCommonEntries;
        }

        private static async Task<TextAsset> LoadLegacyManifestAsync(string key)
        {
            TextAsset asset = await ResManager.LoadOptionalAsync<TextAsset>(key);
#if UNITY_EDITOR
            if (asset == null)
            {
                asset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/GameRes/" + key + ".json");
            }
#endif
            return asset;
        }

        private static void AddManualBootEntries(Dictionary<string, PreloadEntry> entries)
        {
            AddEntry(entries, GameResPath.GetUIPrefab("login", "LoginModule"), PreloadAssetKind.Prefab);
            AddEntry(entries, GameResPath.GetClientConfigPath(PRELOAD_CONFIG), PreloadAssetKind.Text);
            AddEntry(entries, GameResPath.GetClientConfigPath("configlogin"), PreloadAssetKind.Text);
            AddEntry(entries, GameResPath.GetClientConfigPath("configmodelani"), PreloadAssetKind.Text);
            AddEntry(entries, GameResPath.GetClientConfigPath("configrandomname"), PreloadAssetKind.Text);
            AddEntry(entries, GameResPath.GetClientConfigPath("sceneobjectparticle"), PreloadAssetKind.Text);
            AddEntry(entries, GameResPath.GetClientConfigPath(AssetAssemblyProfiles.CONFIG_NAME), PreloadAssetKind.Text);
            AddEntry(entries, GameResPath.GetServerConfigPath("config_dress_up_cfg"), PreloadAssetKind.Text);
        }

        private static void AddLoginRoleUiEntries(Dictionary<string, PreloadEntry> entries)
        {
            AddEntry(entries, GameResPath.GetIcon("login", "ui_Login_02"), PreloadAssetKind.Sprite);
            AddEntry(entries, GameResPath.GetIcon("login", "ui_Login_03"), PreloadAssetKind.Sprite);
            AddEntry(entries, GameResPath.GetIcon("login", "ui_Login_04"), PreloadAssetKind.Sprite);
            AddEntry(entries, GameResPath.GetIcon("login", "ui_Login_05"), PreloadAssetKind.Sprite);
            AddEntry(entries, GameResPath.GetIcon("login", "ui_Login_06"), PreloadAssetKind.Sprite);
        }

        private static void AddCreateRoleOptionEntries(Dictionary<string, PreloadEntry> entries,
            LoginConfigs.CareerOption option)
        {
            if (option == null) return;
            if (!string.IsNullOrEmpty(option.SelectIcon))
                AddEntry(entries, GameResPath.GetIcon("login", option.SelectIcon), PreloadAssetKind.Sprite);
            if (!string.IsNullOrEmpty(option.UnselectIcon))
                AddEntry(entries, GameResPath.GetIcon("login", option.UnselectIcon), PreloadAssetKind.Sprite);
            if (!string.IsNullOrEmpty(option.Img1))
                AddEntry(entries, GameResPath.GetIconOtherPath("login", option.Img1), PreloadAssetKind.Sprite);
            if (!string.IsNullOrEmpty(option.Img2))
                AddEntry(entries, GameResPath.GetIconOtherPath("login", option.Img2), PreloadAssetKind.Sprite);
            if (!string.IsNullOrEmpty(option.Img3))
                AddEntry(entries, GameResPath.GetIconOtherPath("login", option.Img3), PreloadAssetKind.Sprite);
        }

        private static async Task<RoleModelSpec> BuildMainRoleSpecAsync(RoleModel role)
        {
            FigureProto figure = role.Figure;
            int career = figure.career;
            int sex = figure.sex;
            int clotheRes = figure.ClotheModelId;
            int weaponRes = figure.WeaponModelId;
            int headRes = figure.HeadModelId;

            if (clotheRes <= 0 || weaponRes <= 0 || headRes <= 0)
            {
                LoginConfigs.CareerRes defaults = LoginConfigs.GetCreateRes(career, sex);
                if (defaults != null)
                {
                    if (clotheRes <= 0) clotheRes = defaults.RoleRes;
                    if (weaponRes <= 0) weaponRes = defaults.WeaponRes;
                    if (headRes <= 0) headRes = defaults.HeadRes;
                }
            }

            return await Task.FromResult(new RoleModelSpec
            {
                Career = career,
                ClotheRes = clotheRes,
                WeaponRes = weaponRes,
                HeadRes = headRes,
                WingId = figure.WingId,
                BackOrnamentId = figure.BackOrnamentId,
                Actions = new[] { "idle", "run" },
            });
        }

        private static async Task AddRoleModelSpecAsync(Dictionary<string, PreloadEntry> entries, RoleModelSpec spec)
        {
            if (spec == null || spec.ClotheRes <= 0) return;
            AssetAssemblyEntry profile = await AssetAssemblyProfiles.GetAsync(AssetAssemblyProfiles.RoleProfileId(spec.ClotheRes));
            string modelKey = !string.IsNullOrEmpty(profile?.Model)
                ? profile.Model
                : RoleModelKey("role", "model_clothe_" + spec.ClotheRes);
            AddEntry(entries, modelKey, PreloadAssetKind.Prefab);

            if (spec.HeadRes > 0)
                AddEntry(entries, RoleModelKey("head", "model_head_" + spec.HeadRes), PreloadAssetKind.Prefab);
            if (spec.WeaponRes > 0)
                AddEntry(entries, RoleModelKey("weapon", "model_weapon_r_" + spec.WeaponRes), PreloadAssetKind.Prefab);
            if (spec.WingId > 0)
                AddEntry(entries, RoleModelKey("wing", "model_wing_" + spec.WingId), PreloadAssetKind.Prefab);
            if (spec.BackOrnamentId > 0)
                AddEntry(entries, RoleModelKey("back", "model_back_" + spec.BackOrnamentId), PreloadAssetKind.Prefab);

            if (spec.Actions != null)
            {
                for (int i = 0; i < spec.Actions.Length; i++)
                {
                    string action = spec.Actions[i];
                    if (string.IsNullOrEmpty(action)) continue;
                    string key = ResolveRoleActionKey(spec.Career, action, profile);
                    AddEntry(entries, key, PreloadAssetKind.Animation);
                }
            }

            AddProfileEffectEntries(entries, profile);
        }

        private static void AddProfileEffectEntries(Dictionary<string, PreloadEntry> entries, AssetAssemblyEntry profile)
        {
            if (profile == null) return;
            AddEffectBindings(entries, profile.AlwaysEffects);
            if (profile.ActionEffects == null) return;
            foreach (KeyValuePair<string, List<AssetEffectBinding>> kv in profile.ActionEffects)
            {
                AddEffectBindings(entries, kv.Value);
            }
        }

        private static void AddEffectBindings(Dictionary<string, PreloadEntry> entries,
            IEnumerable<AssetEffectBinding> bindings)
        {
            if (bindings == null) return;
            foreach (AssetEffectBinding binding in bindings)
            {
                string effectKey = binding?.ResolveEffectKey();
                if (!string.IsNullOrEmpty(effectKey)) AddEntry(entries, effectKey, PreloadAssetKind.Prefab);
            }
        }

        private static string RoleModelKey(string module, string name)
        {
            return "object/" + module + "/" + name + "/" + name;
        }

        private static string ResolveRoleActionKey(int career, string action, AssetAssemblyEntry profile)
        {
            if (profile?.Actions != null
                && profile.Actions.TryGetValue(action, out string key)
                && !string.IsNullOrEmpty(key))
            {
                return key;
            }
            string dir = (1000 + career * 100).ToString();
            return "object/role/action/" + dir + "/" + action;
        }

        private static async Task AddSceneMapEntriesAsync(Dictionary<string, PreloadEntry> entries,
            int sceneId, int focusX, int focusY)
        {
            if (sceneId <= 0) return;
            string dataKey = GameResPath.GetSceneMapData(sceneId);
            AddEntry(entries, dataKey, PreloadAssetKind.Text);

            TextAsset bytes = await ResManager.LoadAsync<TextAsset>(dataKey);
            if (bytes == null) return;
            _retainedSceneId = sceneId;
            string normalizedDataKey = ResourcePath.Normalize(dataKey);
            if (_retained.ContainsKey(normalizedDataKey))
            {
                // 该 LoadAsync 命中缓存已 +1 引用,而 Retain 不会重复登记:立即归还这次多余引用防泄漏
                ResManager.Release(bytes);
            }
            else
            {
                Retain(LegacyPreloadStage.SceneMap, normalizedDataKey, bytes);
            }

            SceneMapData data;
            try
            {
                data = MapDataParser.Parse(sceneId, bytes.bytes);
            }
            catch (Exception e)
            {
                GameLog.Warn("Preload", "map data parse failed sceneId={0}: {1}", sceneId, e.Message);
                return;
            }

            AddEntry(entries, GameResPath.GetSceneMapPreview(data.MapResId), PreloadAssetKind.Sprite);
            AddVisibleTileEntries(entries, data, focusX, focusY);
        }

        private static void AddVisibleTileEntries(Dictionary<string, PreloadEntry> entries,
            SceneMapData data, int focusX, int focusY)
        {
            if (data == null || data.TileSize <= 0) return;

            float stageWidth = Screen.width > 0 ? Screen.width : 720f;
            float stageHeight = Screen.height > 0 ? Screen.height : 1280f;
            float halfWidth = stageWidth * 0.5f;
            float halfHeight = stageHeight * 0.5f;
            float cameraX = ClampCameraX(data.MapWidth, focusX, halfWidth);
            float cameraY = ClampCameraY(data.MapHeight, focusY, halfHeight);

            int tileSize = data.TileSize;
            int poolCols = Application.isEditor ? 12 : Mathf.FloorToInt(stageWidth / tileSize) + 2;
            int poolRows = Mathf.FloorToInt(stageHeight / tileSize) + 1;
            poolCols = Mathf.Max(1, poolCols);
            poolRows = Mathf.Max(1, poolRows);

            int x = Mathf.CeilToInt(cameraX / tileSize);
            int y = Mathf.CeilToInt(cameraY / tileSize);
            int startCol = Mathf.CeilToInt(poolCols * 0.5f) - 1;
            int startRow = Mathf.CeilToInt(poolRows * 0.5f);
            if (cameraX % tileSize < 0.5f * tileSize) startCol++;
            if (cameraY % tileSize < 0.5f * tileSize) startRow++;
            startCol = x - startCol;
            startRow = y - startRow;

            int maxCol = Mathf.CeilToInt((float)data.MapWidth / data.TileSize);
            int maxRow = Mathf.CeilToInt((float)data.MapHeight / data.TileSize);
            int endCol = startCol + poolCols - 1;
            int endRow = startRow + poolRows - 1;
            for (int row = startRow; row <= endRow; row++)
            {
                if (row < 1 || row > maxRow) continue;
                for (int col = startCol; col <= endCol; col++)
                {
                    if (col < 1 || col > maxCol) continue;
                    AddEntry(entries, GameResPath.GetSceneMapTile(data.MapResId, row, col, ".jxr"), PreloadAssetKind.Sprite);
                }
            }
        }

        private static float ClampCameraX(int mapWidth, int focusX, float halfWidth)
        {
            if (mapWidth <= halfWidth * 2f) return halfWidth;
            return Mathf.Clamp(focusX, halfWidth, mapWidth - halfWidth);
        }

        private static float ClampCameraY(int mapHeight, int focusY, float halfHeight)
        {
            float cameraCenterY = halfHeight + SceneMapView.SceneLayerYOffset;
            if (mapHeight <= halfHeight * 2f) return cameraCenterY;
            return Mathf.Clamp(focusY, cameraCenterY, mapHeight - (halfHeight - SceneMapView.SceneLayerYOffset));
        }

        private static LoginConfigs.CareerOption FindOption(List<LoginConfigs.CareerOption> options, int career)
        {
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].Career == career) return options[i];
            }
            return null;
        }

        private static void AddEntries(Dictionary<string, PreloadEntry> entries, IEnumerable<PreloadEntry> add)
        {
            if (add == null) return;
            foreach (PreloadEntry entry in add)
            {
                if (entry != null) AddEntry(entries, entry.Key, entry.Kind);
            }
        }

        private static void AddLegacyPath(Dictionary<string, PreloadEntry> entries, string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return;
            string path = raw.Replace('\\', '/').Trim();
            string lower = path.ToLowerInvariant();

            if (IsLegacyRemoteUrl(lower) || IsLegacyBootStageExcludedPath(lower)) return;

            if (lower.EndsWith(".atlas", StringComparison.Ordinal) || lower.EndsWith("/texture.ktx", StringComparison.Ordinal))
            {
                AddLegacyAtlas(entries, path);
                return;
            }

            if (lower.EndsWith(".lh", StringComparison.Ordinal))
            {
                AddLegacyLh(entries, path);
                return;
            }

            if (lower.EndsWith(".lani", StringComparison.Ordinal))
            {
                AddLegacyLani(entries, path);
                return;
            }

            if (EndsWithAny(lower, ".png", ".jpg", ".ktx"))
            {
                AddEntry(entries, path, PreloadAssetKind.Sprite);
                return;
            }

            if (EndsWithAny(lower, ".json", ".bytes"))
            {
                AddEntry(entries, path, PreloadAssetKind.Text);
            }
        }

        private static bool IsLegacyRemoteUrl(string lower)
        {
            return lower.StartsWith("http://", StringComparison.Ordinal)
                   || lower.StartsWith("https://", StringComparison.Ordinal);
        }

        private static bool IsLegacyBootStageExcludedPath(string lower)
        {
            // Old common preload mixes runtime 3D/map assets into login boot.
            // Unity loads those later from stage-specific converted keys.
            return lower.StartsWith("resource/object/", StringComparison.Ordinal)
                   || lower.StartsWith("resource/effect/", StringComparison.Ordinal)
                   || lower.StartsWith("resource/default_mesh/", StringComparison.Ordinal)
                   || lower.StartsWith("resource/game/scene/map/", StringComparison.Ordinal);
        }

        private static bool IsRuntime3DKey(string key)
        {
            return key.StartsWith("object/", StringComparison.Ordinal)
                   || key.StartsWith("effect/", StringComparison.Ordinal);
        }

        private static void AddLegacyAtlas(Dictionary<string, PreloadEntry> entries, string raw)
        {
            string key = ResourcePath.Normalize(raw);
            string[] parts = key.Split('/');
            if (parts.Length < 4 || parts[0] != "resource" || parts[1] != "game") return;
            string module = parts[2];
            AddEntry(entries, "resource/game/" + module + "/" + module + "_texture", PreloadAssetKind.DependencyOnly);
        }

        private static void AddLegacyLh(Dictionary<string, PreloadEntry> entries, string raw)
        {
            string key = ResourcePath.Normalize(raw);
            string[] parts = key.Split('/');
            if (parts.Length >= 5 && parts[0] == "resource" && parts[1] == "object" && parts[3] == "objs")
            {
                string module = parts[2];
                string name = parts[4];
                AddEntry(entries, "object/" + module + "/" + name + "/" + name, PreloadAssetKind.Prefab);
                return;
            }

            if (parts.Length >= 5 && parts[0] == "resource" && parts[1] == "effect" && parts[2] == "objs")
            {
                string dir = parts[3];
                string name = parts[4];
                AddEntry(entries, "effect/objs/" + dir + "/" + name + "/" + name, PreloadAssetKind.Prefab);
            }
        }

        private static void AddLegacyLani(Dictionary<string, PreloadEntry> entries, string raw)
        {
            string key = ResourcePath.Normalize(raw);
            string[] parts = key.Split('/');
            if (parts.Length < 6 || parts[0] != "resource" || parts[1] != "object" || parts[3] != "action") return;
            string module = parts[2];
            string group = parts[4];
            string action = LegacyActionName(parts[5]);
            AddEntry(entries, "object/" + module + "/action/" + group + "/" + action, PreloadAssetKind.Animation);
        }

        private static string LegacyActionName(string fileName)
        {
            int dash = fileName.LastIndexOf('-');
            return dash >= 0 && dash < fileName.Length - 1 ? fileName.Substring(dash + 1) : fileName;
        }

        private static bool EndsWithAny(string value, params string[] suffixes)
        {
            for (int i = 0; i < suffixes.Length; i++)
            {
                if (value.EndsWith(suffixes[i], StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private static void AddEntry(Dictionary<string, PreloadEntry> entries, string key, PreloadAssetKind kind)
        {
            string normalized = ResourcePath.Normalize(key);
            if (string.IsNullOrEmpty(normalized)) return;
            if (entries.TryGetValue(normalized, out PreloadEntry old))
            {
                if (old.Kind == PreloadAssetKind.DependencyOnly && kind != PreloadAssetKind.DependencyOnly)
                    old.Kind = kind;
                return;
            }
            entries[normalized] = new PreloadEntry(normalized, kind);
        }

        private sealed class LegacyPreloadRoot
        {
            [JsonProperty("package_common_res")]
            public List<string> PackageCommonRes { get; set; }
        }

        private sealed class PreloadEntry
        {
            public readonly string Key;
            public PreloadAssetKind Kind;

            public PreloadEntry(string key, PreloadAssetKind kind)
            {
                Key = key;
                Kind = kind;
            }
        }

        private enum PreloadAssetKind
        {
            DependencyOnly,
            Text,
            Sprite,
            Prefab,
            Animation,
            Video,
        }
    }
}
