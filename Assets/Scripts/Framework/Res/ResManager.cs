using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Shenxiao.Framework.Util;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Shenxiao.Framework.Res
{
    /// <summary>
    /// Addressables wrapper. All runtime resource loading MUST go through this class.
    /// Synchronous loading is not supported (WebGL constraint).
    /// </summary>
    public static class ResManager
    {
        private static readonly Dictionary<UnityEngine.Object, AsyncOperationHandle> _assetHandles
            = new Dictionary<UnityEngine.Object, AsyncOperationHandle>();

        private static readonly Dictionary<GameObject, AsyncOperationHandle<GameObject>> _instanceHandles
            = new Dictionary<GameObject, AsyncOperationHandle<GameObject>>();

        // key 存在性缓存:同一 catalog 版本内 key→location 映射是固定的,所以一次查到就缓存,
        // 避免每次加载都额外多跑一次 LoadResourceLocationsAsync(那是一整次按帧推进的异步往返)。
        // catalog 重载会改变映射,届时由 ResVersionManager 调 InvalidateKeyCache() 清空。
        private static readonly Dictionary<string, bool> _keyExistsCache = new Dictionary<string, bool>();

#if UNITY_EDITOR
        /// <summary>批处理 CLI 验证(-batchmode -executeMethod)专用:Addressables 的操作系统在 batch 域不推进,
        /// LoadResourceLocationsAsync 永不完成 → KeyExists 挂死。置 true 让 LoadAsync/LoadOptionalAsync 先走
        /// AssetDatabase 编辑器兜底(命中即返回,不触 Addressables);运行时/交互编辑器默认 false,行为不变。</summary>
        public static bool EditorPreferFallback;

        private static readonly HashSet<GameObject> _editorFallbackInstances = new HashSet<GameObject>();

        // 编辑器兜底的「key→工程内资产路径」缓存:命中即免去一次 AssetDatabase.FindAssets 全工程扫描。
        // 只缓存命中(路径非空);未命中不缓存——同一会话里 TryImport* 可能稍后把散图补进来。
        private static readonly Dictionary<string, string> _editorAssetPathCache = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> _editorPrefabPathCache = new Dictionary<string, string>();
#endif

        /// <summary>
        /// Load an asset asynchronously by Addressable key.
        /// </summary>
        public static async Task<T> LoadAsync<T>(string addrKey) where T : UnityEngine.Object
        {
            string key = ResourcePath.Normalize(addrKey);
#if UNITY_EDITOR
            T convertedEffect = LoadEditorConvertedEffectSource<T>(key);
            if (convertedEffect != null) return convertedEffect;
            if (EditorPreferFallback)
            {
                T pre = LoadEditorAssetFallback<T>(key);
                if (pre == null && (typeof(T) == typeof(Sprite) || typeof(T) == typeof(Texture2D))
                    && TryImportLooseImageFromClient(key))
                {
                    pre = LoadEditorAssetFallback<T>(key);
                }
                if (pre != null) return pre;
            }
#endif
            if (!await KeyExists(key))
            {
#if UNITY_EDITOR
                T fallback = LoadEditorAssetFallback<T>(key);
                if (fallback == null && (typeof(T) == typeof(Sprite) || typeof(T) == typeof(Texture2D))
                    && TryImportLooseImageFromClient(key))
                {
                    fallback = LoadEditorAssetFallback<T>(key);
                }
                if (fallback == null && (typeof(T) == typeof(Sprite) || typeof(T) == typeof(Texture2D))
                    && TryImportSceneMapJxrTileFromClient(key))
                {
                    fallback = LoadEditorAssetFallback<T>(key);
                }
                if (fallback == null && typeof(T) == typeof(TextAsset)
                    && TryImportSceneMapBytesFromClient(key))
                {
                    fallback = LoadEditorAssetFallback<T>(key);
                }
                if (fallback != null)
                {
                    GameLog.Warn("Res", "editor asset fallback key={0}(未进 Addressables 组,记得跑 自动分组)", key);
                    return fallback;
                }
#endif
                GameLog.Error("Res", "load failed key={0} type={1}(key 不在 Addressables,跑 神霄/资源/Addressable 自动分组)", key, typeof(T).Name);
                return null;
            }
            var handle = Addressables.LoadAssetAsync<T>(key);
            await AddressablesAwaiter.Wait(handle);

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                GameLog.Error("Res", "load failed key={0} type={1}", key, typeof(T).Name);
                return null;
            }

            T asset = handle.Result;
            if (asset != null) _assetHandles[asset] = handle;
            return asset;
        }

        /// <summary>
        /// Try loading an optional asset without logging missing/unconverted keys.
        /// </summary>
        public static async Task<T> LoadOptionalAsync<T>(string addrKey) where T : UnityEngine.Object
        {
            string key = ResourcePath.Normalize(addrKey);
#if UNITY_EDITOR
            T convertedEffect = LoadEditorConvertedEffectSource<T>(key);
            if (convertedEffect != null) return convertedEffect;
            if (EditorPreferFallback)
            {
                T pre = LoadEditorAssetFallback<T>(key);
                if (pre != null) return pre;
            }
#endif
            if (!await KeyExists(key, typeof(T))) return null;

            var handle = Addressables.LoadAssetAsync<T>(key);
            await AddressablesAwaiter.Wait(handle);
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Addressables.Release(handle);
                return null;
            }

            T asset = handle.Result;
            if (asset != null) _assetHandles[asset] = handle;
            return asset;
        }

        /// <summary>
        /// Instantiate a prefab asynchronously by Addressable key.
        /// </summary>
        public static async Task<GameObject> InstantiateAsync(string addrKey, Transform parent = null)
        {
            string key = ResourcePath.Normalize(addrKey);
#if UNITY_EDITOR
            GameObject convertedEffect = LoadEditorConvertedEffectSource<GameObject>(key);
            if (convertedEffect != null)
            {
                GameObject effectGo = UnityEngine.Object.Instantiate(convertedEffect, parent);
                _editorFallbackInstances.Add(effectGo);
                return effectGo;
            }

            GameObject editorPrefab = LoadEditorPrefabFallback(key);
            if (editorPrefab != null)
            {
                GameObject editorGo = UnityEngine.Object.Instantiate(editorPrefab, parent);
                _editorFallbackInstances.Add(editorGo);
                return editorGo;
            }
#endif
            // 先查 key 是否登记,避免 Addressables 对无效 key 在控制台抛 InvalidKeyException
            if (!await KeyExists(key))
            {
#if UNITY_EDITOR
                GameObject fallbackPrefab = LoadEditorPrefabFallback(key);
                if (fallbackPrefab != null)
                {
                    GameObject fb = UnityEngine.Object.Instantiate(fallbackPrefab, parent);
                    _editorFallbackInstances.Add(fb);
                    GameLog.Warn("Res", "editor prefab fallback key={0}(未进 Addressables 组,记得跑 自动分组)", key);
                    return fb;
                }
#endif
                GameLog.Error("Res", "instantiate failed key={0}(key 不在 Addressables)", key);
                return null;
            }

            var handle = Addressables.InstantiateAsync(key, parent);
            await AddressablesAwaiter.Wait(handle);

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
#if UNITY_EDITOR
                // 编辑器兜底:资源还没进 Addressables 组时直接实例化工程内 prefab。
                // 兜底成功只提示 Warn(去跑 神霄/资源/Addressable 自动分组),不刷 Error。
                GameObject prefab = LoadEditorPrefabFallback(key);
                if (prefab != null)
                {
                    GameObject fallbackGo = UnityEngine.Object.Instantiate(prefab, parent);
                    _editorFallbackInstances.Add(fallbackGo);
                    GameLog.Warn("Res", "editor prefab fallback key={0}(未进 Addressables 组,记得跑 自动分组)", key);
                    return fallbackGo;
                }
#endif
                GameLog.Error("Res", "instantiate failed key={0}", key);
                return null;
            }

            GameObject go = handle.Result;
            if (go != null) _instanceHandles[go] = handle;
            return go;
        }

        /// <summary>key 是否已登记进 Addressables(查 location,不触发加载、不抛异常)。</summary>
        public static Task<bool> KeyExistsAsync(string addrKey)
        {
            return KeyExists(ResourcePath.Normalize(addrKey));
        }

        /// <summary>key exists in Addressables with an asset type assignable to T.</summary>
        public static Task<bool> KeyExistsAsync<T>(string addrKey) where T : UnityEngine.Object
        {
            return KeyExists(ResourcePath.Normalize(addrKey), typeof(T));
        }

        /// <summary>key 是否已登记进 Addressables(查 location,不触发加载、不抛异常)。</summary>
        private static async Task<bool> KeyExists(string key)
        {
            return await KeyExists(key, null);
        }

        private static async Task<bool> KeyExists(string key, Type type)
        {
#if UNITY_EDITOR
            if (type != null)
            {
                // 编辑器下类型化存在性退化为无类型查询,统一走无类型缓存项,避免重复往返。
                return await KeyExists(key, null);
            }
#endif
            string cacheKey = type == null ? key : key + "|" + type.FullName;
            if (_keyExistsCache.TryGetValue(cacheKey, out bool cached)) return cached;

            var locHandle = type == null
                ? Addressables.LoadResourceLocationsAsync(key)
                : Addressables.LoadResourceLocationsAsync(key, type);
            await AddressablesAwaiter.Wait(locHandle);
            bool exists = locHandle.Status == AsyncOperationStatus.Succeeded
                          && locHandle.Result != null && locHandle.Result.Count > 0;
            Addressables.Release(locHandle);
            _keyExistsCache[cacheKey] = exists;
            return exists;
        }

        /// <summary>
        /// catalog 重载后调用:key→location 映射可能整体变化,清空存在性缓存以免读到旧版本结果。
        /// </summary>
        public static void InvalidateKeyCache()
        {
            _keyExistsCache.Clear();
        }

        /// <summary>
        /// Release an asset loaded via LoadAsync.
        /// </summary>
        public static void Release(UnityEngine.Object asset)
        {
            if (asset == null) return;
            if (_assetHandles.TryGetValue(asset, out var handle))
            {
                _assetHandles.Remove(asset);
                Addressables.Release(handle);
            }
        }

        /// <summary>
        /// Release a GameObject instantiated via InstantiateAsync.
        /// </summary>
        public static void ReleaseInstance(GameObject go)
        {
            if (go == null) return;
#if UNITY_EDITOR
            if (_editorFallbackInstances.Remove(go))
            {
                // 编辑期兜底实例:Play 用 Destroy,Edit 模式(无 Play 的渲染/截图 harness)必须 DestroyImmediate(Destroy 在 edit mode 抛错)。
                if (Application.isPlaying) UnityEngine.Object.Destroy(go);
                else UnityEngine.Object.DestroyImmediate(go);
                return;
            }
#endif
            if (_instanceHandles.TryGetValue(go, out var handle))
            {
                _instanceHandles.Remove(go);
                Addressables.ReleaseInstance(handle);
            }
            else
            {
                Addressables.ReleaseInstance(go);
            }
        }

        /// <summary>
        /// Preload a label group.
        /// </summary>
        public static async Task PreloadGroup(string label)
        {
            var handle = Addressables.DownloadDependenciesAsync(label);
            await AddressablesAwaiter.Wait(handle);
            Addressables.Release(handle);
        }

        /// <summary>
        /// Get total download size for given keys. 未登记的 key 自动跳过(一条 Warn),不抛异常。
        /// </summary>
        public static async Task<long> GetDownloadSize(IEnumerable<string> keys)
        {
            List<string> valid = await FilterExistingKeys(keys);
            if (valid.Count == 0) return 0;
            var handle = Addressables.GetDownloadSizeAsync((IEnumerable<object>)valid);
            await AddressablesAwaiter.Wait(handle);
            long size = handle.Result;
            Addressables.Release(handle);
            return size;
        }

        /// <summary>
        /// Download dependencies for given keys with progress callback. 未登记的 key 自动跳过。
        /// </summary>
        public static async Task DownloadAsync(IEnumerable<string> keys, Action<float> onProgress)
        {
            List<string> valid = await FilterExistingKeys(keys);
            if (valid.Count == 0) { onProgress?.Invoke(1f); return; }
            var handle = Addressables.DownloadDependenciesAsync((IEnumerable<object>)valid, Addressables.MergeMode.Union);
            while (!handle.IsDone)
            {
                onProgress?.Invoke(handle.PercentComplete);
                await Task.Yield();
            }
            onProgress?.Invoke(1f);
            Addressables.Release(handle);
        }

        private static async Task<List<string>> FilterExistingKeys(IEnumerable<string> keys)
        {
            var valid = new List<string>();
            if (keys == null) return valid;

            // 并发探测:串行 await 每个 key 会按帧逐个推进;并发发起后统一 await,几帧内全处理完。
            var normalized = new List<string>();
            foreach (string raw in keys) normalized.Add(ResourcePath.Normalize(raw));
            var checks = new Task<bool>[normalized.Count];
            for (int i = 0; i < normalized.Count; i++) checks[i] = KeyExists(normalized[i]);
            bool[] results = await Task.WhenAll(checks);

            for (int i = 0; i < normalized.Count; i++)
            {
                if (results[i]) valid.Add(normalized[i]);
                else GameLog.Warn("Res", "预下载 key 未登记,跳过: {0}", normalized[i]);
            }
            return valid;
        }

        /// <summary>
        /// Dynamically assign an Image from a direct Laya skin path.
        /// Use SetLayaTextureAsync for old-client ResManager.SetTexture/SetOutsideImageSprite calls,
        /// because those APIs rewrite /texture/ to /other/ before loading.
        /// layaSkinPath 直接用 Laya 资源路径(如 resource/game/login/other/load_bg0.jpg)。
        /// coverScreen=true 复刻 Util.SetLargeScreenImageSize:等比放大盖满设计分辨率。
        /// nativeSize=false 复刻 Laya skin= 换肤:保留节点场景尺寸,只换图——
        /// 选中态换图/状态切换一律用 false,否则 SetNativeSize 会把节点撑回原图大小。
        /// </summary>
        public static async Task<bool> SetImageAsync(UnityEngine.UI.Image image, string layaSkinPath,
            bool coverScreen = false, bool nativeSize = true)
        {
            if (image == null || string.IsNullOrEmpty(layaSkinPath)) return false;
            Sprite sprite = await LoadAsync<Sprite>(layaSkinPath);
            if (sprite == null || image == null) return false;
            image.sprite = sprite;
            image.enabled = true;
            // 运行时赋图必可见:占位容器/可点击图标在 prefab 里被设为透明(alpha0,不画白块),赋图后须复位 alpha。
            if (image.color.a < 1f) { Color ic = image.color; ic.a = 1f; image.color = ic; }
            if (!coverScreen && !nativeSize) return true; // 保留场景尺寸,只换图
            if (coverScreen)
            {
                // 全屏底图:居中锚定 + 等比放大盖满(转换产物的 pivot 可能在左上,必须一并归位)
                RectTransform rt = image.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                Rect canvasRect = ((RectTransform)rt.GetComponentInParent<Canvas>().transform).rect;
                float scale = Mathf.Max(canvasRect.width / sprite.rect.width, canvasRect.height / sprite.rect.height);
                rt.sizeDelta = new Vector2(sprite.rect.width * scale, sprite.rect.height * scale);
            }
            else
            {
                image.SetNativeSize();
            }
            return true;
        }

        /// <summary>
        /// Match yu_client ResManager.SetTexture/SetOutsideImageSprite path semantics.
        /// Those APIs rewrite the first /texture/ segment to /other/ before loading.
        /// Direct Laya skin assignment and SetImageSpriteTrans should keep using SetImageAsync.
        /// </summary>
        public static Task<bool> SetLayaTextureAsync(UnityEngine.UI.Image image, string layaSkinPath,
            bool coverScreen = false, bool nativeSize = true)
        {
            return SetImageAsync(image, ToLayaSetTexturePath(layaSkinPath), coverScreen, nativeSize);
        }

        public static string ToLayaSetTexturePath(string layaSkinPath)
        {
            if (string.IsNullOrEmpty(layaSkinPath)) return layaSkinPath;
            string path = layaSkinPath.Replace('\\', '/');
            const string textureSegment = "/texture/";
            int index = path.IndexOf(textureSegment, StringComparison.Ordinal);
            if (index < 0) return path;
            return path.Substring(0, index) + "/other/" + path.Substring(index + textureSegment.Length);
        }

#if UNITY_EDITOR
        private static T LoadEditorConvertedEffectSource<T>(string key) where T : UnityEngine.Object
        {
            if (typeof(T) != typeof(GameObject)) return null;
            if (!key.StartsWith("effect/objs/", StringComparison.Ordinal)) return null;
            return LoadEditorAssetFallback<T>(key);
        }

        private static GameObject LoadEditorPrefabFallback(string key)
        {
            string fileName = Path.GetFileName(key);
            if (string.IsNullOrEmpty(fileName)) return null;

            if (_editorPrefabPathCache.TryGetValue(key, out string cachedPath))
            {
                GameObject cachedAsset = string.IsNullOrEmpty(cachedPath)
                    ? null : AssetDatabase.LoadAssetAtPath<GameObject>(cachedPath);
                if (cachedAsset != null) return cachedAsset;
                _editorPrefabPathCache.Remove(key); // 路径失效(资产被移动/删除/重导):清掉并重扫
            }

            string[] searchFolders = { "Assets/GameRes", "Assets/Prefabs", "Assets/_App" };
            string[] guids = AssetDatabase.FindAssets(fileName + " t:Prefab", searchFolders);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (ResourcePath.Normalize(MakeEditorAddress(path)) != key) continue;
                _editorPrefabPathCache[key] = path;
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            return null;
        }

        private static string MakeEditorAddress(string assetPath)
        {
            string rel;
            if (assetPath.StartsWith("Assets/GameRes/")) rel = assetPath.Substring("Assets/GameRes/".Length);
            else if (assetPath.StartsWith("Assets/_App/")) rel = assetPath.Substring("Assets/_App/".Length);
            else rel = assetPath.Substring("Assets/".Length);

            string ext = Path.GetExtension(rel);
            if (!string.IsNullOrEmpty(ext)) rel = rel.Substring(0, rel.Length - ext.Length);
            return rel.Replace('\\', '/').ToLowerInvariant();
        }

        /// <summary>
        /// 编辑器兜底再兜一层:GameRes 缺图时从 yu_client 散图镜像(h5/laya/assets → cdn)按需拷入并
        /// 导成 Sprite。解决「运行时引用的图从没被任何 UI 模块导入过」这一类问题(如头像 head/texture)。
        /// 拷入后仍提示跑 Addressable 分组,打真机包前必须分组登记。
        /// </summary>
        private static bool TryImportLooseImageFromClient(string key)
        {
            // ClientRoot 取值与 LayaUISettings.ProjectKey 保持一致(框架层不依赖 Editor 程序集,内联读取)
            string def = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "yu_client"));
            string clientRoot = UnityEditor.EditorPrefs.GetString(
                "Shenxiao.LayaUI.ClientRoot:" + Application.dataPath.GetHashCode(), def);
            string[] roots = { Path.Combine(clientRoot, "h5", "laya", "assets"), Path.Combine(clientRoot, "cdn") };
            string[] exts = { ".png", ".jpg" };
            foreach (string root in roots)
            {
                foreach (string ext in exts)
                {
                    string src = Path.Combine(root, key + ext);
                    if (!File.Exists(src)) continue;
                    // git LFS 占位文件不算有图(需要在本机 git lfs pull)
                    var info = new FileInfo(src);
                    if (info.Length < 300)
                    {
                        using var sr = new StreamReader(src);
                        char[] head = new char[30];
                        int n = sr.Read(head, 0, head.Length);
                        if (new string(head, 0, n).StartsWith("version https://git-lfs")) continue;
                    }
                    string assetPath = "Assets/GameRes/" + key + ext;
                    Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
                    File.Copy(src, assetPath, true);
                    AssetDatabase.ImportAsset(assetPath);
                    if (AssetImporter.GetAtPath(assetPath) is TextureImporter ti
                        && ti.textureType != TextureImporterType.Sprite)
                    {
                        ti.textureType = TextureImporterType.Sprite;
                        ti.SaveAndReimport();
                    }
                    GameLog.Warn("Res", "editor 兜底:已从 yu_client 导入散图 {0}{1}", key, ext);
                    return true;
                }
            }
            return false;
        }

        /// <summary>编辑器兜底加载任意资产(Sprite/Texture 等),按文件名+规范化地址匹配 GameRes/_App。</summary>
        private static bool TryImportSceneMapBytesFromClient(string key)
        {
            if (!key.StartsWith("resource/game/scene/map/", StringComparison.Ordinal)) return false;

            string def = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "yu_client"));
            string clientRoot = UnityEditor.EditorPrefs.GetString(
                "Shenxiao.LayaUI.ClientRoot:" + Application.dataPath.GetHashCode(), def);
            string src = Path.Combine(clientRoot, "cdn", key + ".bytes");
            if (!File.Exists(src)) return false;

            string assetPath = "Assets/GameRes/" + key + ".bytes";
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
            File.Copy(src, assetPath, true);
            AssetDatabase.ImportAsset(assetPath);
            GameLog.Warn("Res", "editor fallback imported scene map bytes from yu_client: {0}", key);
            return true;
        }

        private static bool TryImportSceneMapJxrTileFromClient(string key)
        {
            if (!key.StartsWith("resource/game/scene/map/", StringComparison.Ordinal)) return false;
            string fileName = Path.GetFileName(key);
            if (string.IsNullOrEmpty(fileName) || fileName.Length != 4) return false;

            string def = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "yu_client"));
            string clientRoot = UnityEditor.EditorPrefs.GetString(
                "Shenxiao.LayaUI.ClientRoot:" + Application.dataPath.GetHashCode(), def);
            string src = Path.Combine(clientRoot, "cdn", key + ".jxr");
            if (!File.Exists(src)) return false;

            byte[] bytes = File.ReadAllBytes(src);
            if (bytes.Length < 3 || bytes[0] != 0xFF || bytes[1] != 0xD8 || bytes[2] != 0xFF)
            {
                GameLog.Warn("Res", "scene map .jxr is not JPEG data, skip editor import: {0}", key);
                return false;
            }

            string assetPath = "Assets/GameRes/" + key + ".jpg";
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
            File.WriteAllBytes(assetPath, bytes);
            AssetDatabase.ImportAsset(assetPath);
            if (AssetImporter.GetAtPath(assetPath) is TextureImporter ti)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.mipmapEnabled = false;
                ti.npotScale = TextureImporterNPOTScale.None;
                ti.SaveAndReimport();
            }
            GameLog.Warn("Res", "editor fallback imported scene map .jxr tile as jpg from yu_client: {0}", key);
            return true;
        }

        private static T LoadEditorAssetFallback<T>(string key) where T : UnityEngine.Object
        {
            string fileName = Path.GetFileName(key);
            if (string.IsNullOrEmpty(fileName)) return null;

            if (_editorAssetPathCache.TryGetValue(key, out string cachedPath))
            {
                T cachedAsset = string.IsNullOrEmpty(cachedPath) ? null : AssetDatabase.LoadAssetAtPath<T>(cachedPath);
                if (cachedAsset != null) return cachedAsset;
                _editorAssetPathCache.Remove(key); // 路径失效(资产被移动/删除/重导):清掉并重扫
            }

            string[] searchFolders = { "Assets/GameRes", "Assets/_App" };
            string[] guids = AssetDatabase.FindAssets(fileName, searchFolders);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (ResourcePath.Normalize(MakeEditorAddress(path)) != key) continue;
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    // 缓存 key→路径(类型无关:路径只按 文件名+规范化地址 解析,类型在 LoadAssetAtPath<T> 处再应用)。
                    _editorAssetPathCache[key] = path;
                    return asset;
                }
            }
            return null;
        }
#endif
    }
}
