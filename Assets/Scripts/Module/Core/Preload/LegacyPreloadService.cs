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
        private static readonly Dictionary<string, bool> _keyExists =
            new Dictionary<string, bool>();

        private static bool _legacyLoaded;
        private static List<PreloadEntry> _legacyCommonEntries;

        public static event ProgressHandler ProgressChanged;

        public static async Task PreloadBootAsync(IEnumerable<string> appPreloadKeys, Action<float, string> progress = null)
        {
            var entries = new Dictionary<string, PreloadEntry>();
            AddEntries(entries, await GetLegacyCommonEntries());
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
                    await WarmOneAsync(toWarm[i]);
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
            if (entry.Kind == PreloadAssetKind.Animation) return false;
            if (entry.Kind == PreloadAssetKind.Prefab && IsRuntime3DKey(entry.Key)) return false;
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

        private static async Task WarmOneAsync(PreloadEntry entry)
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
            }

            if (asset != null) _retained[entry.Key] = asset;
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
            if (!_retained.ContainsKey(ResourcePath.Normalize(dataKey)))
                _retained[ResourcePath.Normalize(dataKey)] = bytes;

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
        }
    }
}
