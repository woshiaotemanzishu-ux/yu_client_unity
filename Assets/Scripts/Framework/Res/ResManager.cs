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
            var handle = Addressables.InstantiateAsync(key, parent);
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                GameLog.Error("Res", "instantiate failed key={0}", key);
#if UNITY_EDITOR
                GameObject prefab = LoadEditorPrefabFallback(key);
                if (prefab != null)
                {
                    GameObject fallbackGo = UnityEngine.Object.Instantiate(prefab, parent);
                    _editorFallbackInstances.Add(fallbackGo);
                    GameLog.Warn("Res", "editor prefab fallback key={0}", key);
                    return fallbackGo;
                }
#endif
                return null;
            }

            GameObject go = handle.Result;
            if (go != null) _instanceHandles[go] = handle;
            return go;
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
        /// Get total download size for given keys.
        /// </summary>
        public static async Task<long> GetDownloadSize(IEnumerable<string> keys)
        {
            var handle = Addressables.GetDownloadSizeAsync((IEnumerable<object>)keys);
            await handle.Task;
            long size = handle.Result;
            Addressables.Release(handle);
            return size;
        }

        /// <summary>
        /// Download dependencies for given keys with progress callback.
        /// </summary>
        public static async Task DownloadAsync(IEnumerable<string> keys, Action<float> onProgress)
        {
            var handle = Addressables.DownloadDependenciesAsync((IEnumerable<object>)keys, Addressables.MergeMode.Union);
            while (!handle.IsDone)
            {
                onProgress?.Invoke(handle.PercentComplete);
                await Task.Yield();
            }
            onProgress?.Invoke(1f);
            Addressables.Release(handle);
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
#endif
    }
}
