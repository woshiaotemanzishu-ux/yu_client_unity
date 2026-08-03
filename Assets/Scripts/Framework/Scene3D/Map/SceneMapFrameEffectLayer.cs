using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Framework.Scene3D.Map
{
    /// <summary>加载并显示 Electron 保存的地图图集帧动画实例。</summary>
    public static class SceneMapFrameEffectLayer
    {
        [Serializable]
        private sealed class ManifestData
        {
            public int version = 0;
            public int[] maps = Array.Empty<int>();
        }

        [Serializable]
        private sealed class EffectDocument
        {
            public int version = 0;
            public int mapResId = 0;
            public EffectData[] effects = Array.Empty<EffectData>();
        }

        [Serializable]
        private sealed class EffectData
        {
            public string id = "";
            public string kind = "";
            public string assetId = "";
            public string name = "";
            public float x = 0f;
            public float y = 0f;
            public float scaleX = 1f;
            public float scaleY = 1f;
            public float rotation = 0f;
            public float alpha = 1f;
            public string layer = "map_front";
            public bool randomStart = false;
        }

        private sealed class LoadedClip
        {
            public MapFrameAnimationClipAsset Asset;
            public Sprite[] Sprites;
        }

        private static int _version;
        private static GameObject _backRoot;
        private static GameObject _frontRoot;
        private static MapFrameEffectPlayer _player;
        private static readonly List<LoadedClip> _loadedClips = new List<LoadedClip>();

        public static async Task ShowAsync(int mapResId, Transform mapRoot, Transform tileRoot)
        {
            int version = ++_version;
            ClearVisuals();
            if (mapResId <= 0 || mapRoot == null) return;

            string manifestPath = GameResPath.GetSceneMapEffectsManifest();
#if UNITY_EDITOR
            // 当前工程常用 Existing Build 模式；刚同步的资源尚未重建 catalog 时，LoadAsync 会走
            // ResManager 的 AssetDatabase 兜底。先判本地存在，避免无特效工程把可选 manifest 记成错误。
            if (UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/GameRes/" + manifestPath) == null)
                return;
#endif
            TextAsset manifestAsset = await ResManager.LoadAsync<TextAsset>(manifestPath);
            if (version != _version)
            {
                if (manifestAsset != null) ResManager.Release(manifestAsset);
                return;
            }
            if (manifestAsset == null) return;

            ManifestData manifest = ParseJson<ManifestData>(manifestAsset.text, "manifest");
            ResManager.Release(manifestAsset);
            if (manifest == null || !ContainsMap(manifest.maps, mapResId)) return;

            TextAsset documentAsset = await ResManager.LoadAsync<TextAsset>(
                GameResPath.GetSceneMapEffects(mapResId));
            if (version != _version)
            {
                if (documentAsset != null) ResManager.Release(documentAsset);
                return;
            }
            if (documentAsset == null) return;

            EffectDocument document = ParseJson<EffectDocument>(documentAsset.text, "mapResId=" + mapResId);
            ResManager.Release(documentAsset);
            if (document == null || document.version != 2 || document.mapResId != mapResId
                || document.effects == null || document.effects.Length == 0)
            {
                return;
            }

            var clipsById = new Dictionary<string, LoadedClip>(StringComparer.Ordinal);
            for (int i = 0; i < document.effects.Length; i++)
            {
                EffectData effect = document.effects[i];
                if (effect == null || effect.kind != "frame_animation" || !IsSafeAssetId(effect.assetId)
                    || clipsById.ContainsKey(effect.assetId))
                {
                    continue;
                }

                MapFrameAnimationClipAsset clipAsset = await ResManager.LoadAsync<MapFrameAnimationClipAsset>(
                    GameResPath.GetMapFrameAnimationClip(effect.assetId));
                if (version != _version)
                {
                    if (clipAsset != null) ResManager.Release(clipAsset);
                    ReleaseClips(clipsById.Values);
                    return;
                }
                if (clipAsset == null) continue;

                Sprite[] sprites = clipAsset.CreateSprites();
                if (sprites.Length == 0)
                {
                    GameLog.Error("SceneMap", "map frame clip invalid assetId={0}", effect.assetId);
                    ResManager.Release(clipAsset);
                    continue;
                }
                clipsById.Add(effect.assetId, new LoadedClip { Asset = clipAsset, Sprites = sprites });
            }

            if (version != _version)
            {
                ReleaseClips(clipsById.Values);
                return;
            }
            if (clipsById.Count == 0) return;

            CreateRoots(mapRoot, tileRoot);
            if (_player == null)
            {
                ReleaseClips(clipsById.Values);
                return;
            }

            foreach (KeyValuePair<string, LoadedClip> pair in clipsById) _loadedClips.Add(pair.Value);

            int created = 0;
            for (int i = 0; i < document.effects.Length; i++)
            {
                EffectData effect = document.effects[i];
                if (effect == null || effect.kind != "frame_animation"
                    || !clipsById.TryGetValue(effect.assetId, out LoadedClip loaded))
                {
                    continue;
                }

                Transform parent = effect.layer == "map_back" ? _backRoot.transform : _frontRoot.transform;
                Image image = CreateImage(effect, loaded.Asset, parent);
                _player.Register(effect.assetId, loaded.Sprites, loaded.Asset.Fps, loaded.Asset.Loop,
                    image, effect.randomStart);
                created++;
            }

            GameLog.Info("SceneMap", "map frame effects ready: mapResId={0} instances={1} assets={2}",
                mapResId, created, clipsById.Count);
        }

        public static void Clear()
        {
            ++_version;
            ClearVisuals();
        }

        private static T ParseJson<T>(string json, string context) where T : class
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch (ArgumentException e)
            {
                GameLog.Error("SceneMap", "map frame json invalid {0}: {1}", context, e.Message);
                return null;
            }
        }

        private static bool ContainsMap(int[] maps, int mapResId)
        {
            if (maps == null) return false;
            for (int i = 0; i < maps.Length; i++)
            {
                if (maps[i] == mapResId) return true;
            }
            return false;
        }

        private static bool IsSafeAssetId(string assetId)
        {
            if (string.IsNullOrEmpty(assetId) || assetId.Length > 64
                || assetId[0] < 'a' || assetId[0] > 'z')
            {
                return false;
            }
            for (int i = 1; i < assetId.Length; i++)
            {
                char c = assetId[i];
                if ((c < 'a' || c > 'z') && (c < '0' || c > '9') && c != '_') return false;
            }
            return true;
        }

        private static void CreateRoots(Transform mapRoot, Transform tileRoot)
        {
            _backRoot = CreateRoot("MapFrameEffectsBack", mapRoot);
            _frontRoot = CreateRoot("MapFrameEffectsFront", mapRoot);

            if (tileRoot != null) _backRoot.transform.SetSiblingIndex(tileRoot.GetSiblingIndex());
            _frontRoot.transform.SetAsLastSibling();
            _player = _frontRoot.AddComponent<MapFrameEffectPlayer>();
        }

        private static GameObject CreateRoot(string name, Transform parent)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)root.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            return root;
        }

        private static Image CreateImage(EffectData effect, MapFrameAnimationClipAsset clip, Transform parent)
        {
            string objectName = string.IsNullOrEmpty(effect.id) ? effect.assetId : effect.id;
            var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);

            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(clip.PivotX, 1f - clip.PivotY);
            rt.anchoredPosition = new Vector2(effect.x, -effect.y);
            rt.sizeDelta = new Vector2(clip.FrameWidth, clip.FrameHeight);
            rt.localScale = new Vector3(effect.scaleX, effect.scaleY, 1f);
            rt.localEulerAngles = new Vector3(0f, 0f, -effect.rotation);

            Image image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(effect.alpha));
            return image;
        }

        private static void ClearVisuals()
        {
            if (_player != null) _player.Clear();
            _player = null;

            DestroyObject(_backRoot);
            DestroyObject(_frontRoot);
            _backRoot = null;
            _frontRoot = null;

            ReleaseClips(_loadedClips);
            _loadedClips.Clear();
        }

        private static void ReleaseClips(IEnumerable<LoadedClip> clips)
        {
            foreach (LoadedClip clip in clips)
            {
                if (clip == null) continue;
                MapFrameAnimationClipAsset.DestroySprites(clip.Sprites);
                if (clip.Asset != null) ResManager.Release(clip.Asset);
            }
        }

        private static void DestroyObject(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(obj);
            else UnityEngine.Object.DestroyImmediate(obj);
        }
    }
}
