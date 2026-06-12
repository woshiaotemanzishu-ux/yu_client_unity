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

#if UNITY_EDITOR
        private static readonly HashSet<GameObject> _editorFallbackInstances = new HashSet<GameObject>();
#endif

        /// <summary>
        /// Load an asset asynchronously by Addressable key.
        /// </summary>
        public static async Task<T> LoadAsync<T>(string addrKey) where T : UnityEngine.Object
        {
            string key = ResourcePath.Normalize(addrKey);
            if (!await KeyExists(key))
            {
#if UNITY_EDITOR
                T fallback = LoadEditorAssetFallback<T>(key);
                if (fallback == null && (typeof(T) == typeof(Sprite) || typeof(T) == typeof(Texture2D))
                    && TryImportLooseImageFromClient(key))
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
            await handle.Task;

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
        /// Instantiate a prefab asynchronously by Addressable key.
        /// </summary>
        public static async Task<GameObject> InstantiateAsync(string addrKey, Transform parent = null)
        {
            string key = ResourcePath.Normalize(addrKey);
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
            await handle.Task;

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
        private static async Task<bool> KeyExists(string key)
        {
            var locHandle = Addressables.LoadResourceLocationsAsync(key);
            await locHandle.Task;
            bool exists = locHandle.Status == AsyncOperationStatus.Succeeded
                          && locHandle.Result != null && locHandle.Result.Count > 0;
            Addressables.Release(locHandle);
            return exists;
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
                UnityEngine.Object.Destroy(go);
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
            await handle.Task;
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
            await handle.Task;
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
            foreach (string raw in keys)
            {
                string key = ResourcePath.Normalize(raw);
                if (await KeyExists(key)) valid.Add(key);
                else GameLog.Warn("Res", "预下载 key 未登记,跳过: {0}", key);
            }
            return valid;
        }

        /// <summary>
        /// 动态给 Image 赋图,对标 Laya 的 ResManager.SetTexture(业务运行时换图统一走这里)。
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

#if UNITY_EDITOR
        private static GameObject LoadEditorPrefabFallback(string key)
        {
            string fileName = Path.GetFileName(key);
            if (string.IsNullOrEmpty(fileName)) return null;

            string[] searchFolders = { "Assets/Prefabs", "Assets/_App" };
            string[] guids = AssetDatabase.FindAssets(fileName + " t:Prefab", searchFolders);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (ResourcePath.Normalize(MakeEditorAddress(path)) != key) continue;
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
        private static T LoadEditorAssetFallback<T>(string key) where T : UnityEngine.Object
        {
            string fileName = Path.GetFileName(key);
            if (string.IsNullOrEmpty(fileName)) return null;

            string[] searchFolders = { "Assets/GameRes", "Assets/_App" };
            string[] guids = AssetDatabase.FindAssets(fileName, searchFolders);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (ResourcePath.Normalize(MakeEditorAddress(path)) != key) continue;
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null) return asset;
            }
            return null;
        }
#endif
    }
}
