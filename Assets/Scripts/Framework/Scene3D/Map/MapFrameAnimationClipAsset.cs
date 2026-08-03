using System;
using UnityEngine;

namespace Shenxiao.Framework.Scene3D.Map
{
    [Serializable]
    public struct MapFrameAnimationFrame
    {
        public string Name;
        public int X;
        public int Y;
        public int Width;
        public int Height;

        public MapFrameAnimationFrame(string name, int x, int y, int width, int height)
        {
            Name = name;
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }

    /// <summary>Electron 图集帧动画在 Unity 中的共享运行时资产。</summary>
    public sealed class MapFrameAnimationClipAsset : ScriptableObject
    {
        [SerializeField] private string _assetId = "";
        [SerializeField] private string _displayName = "";
        [SerializeField] private Texture2D _texture;
        [SerializeField] private int _frameWidth = 1;
        [SerializeField] private int _frameHeight = 1;
        [SerializeField] private float _fps = 12f;
        [SerializeField] private bool _loop = true;
        [SerializeField] private float _pivotX = 0.5f;
        [SerializeField] private float _pivotY = 0.5f;
        [SerializeField] private MapFrameAnimationFrame[] _frames = Array.Empty<MapFrameAnimationFrame>();

        public string AssetId => _assetId;
        public string DisplayName => _displayName;
        public int FrameWidth => _frameWidth;
        public int FrameHeight => _frameHeight;
        public float Fps => _fps;
        public bool Loop => _loop;
        public float PivotX => _pivotX;
        public float PivotY => _pivotY;
        public int FrameCount => _frames == null ? 0 : _frames.Length;

        /// <summary>由地图帧动画 Editor 导入器更新；运行时业务不应调用。</summary>
        public void Configure(string assetId, string displayName, Texture2D texture,
            int frameWidth, int frameHeight, float fps, bool loop, float pivotX, float pivotY,
            MapFrameAnimationFrame[] frames)
        {
            _assetId = assetId ?? "";
            _displayName = displayName ?? "";
            _texture = texture;
            _frameWidth = Mathf.Max(1, frameWidth);
            _frameHeight = Mathf.Max(1, frameHeight);
            _fps = Mathf.Max(1f, fps);
            _loop = loop;
            _pivotX = Mathf.Clamp01(pivotX);
            _pivotY = Mathf.Clamp01(pivotY);
            _frames = frames ?? Array.Empty<MapFrameAnimationFrame>();
        }

        /// <summary>按中央资源库的顶部原点矩形创建共享 Sprite 数组。</summary>
        public Sprite[] CreateSprites()
        {
            if (_texture == null || _frames == null || _frames.Length == 0) return Array.Empty<Sprite>();

            var sprites = new Sprite[_frames.Length];
            for (int i = 0; i < _frames.Length; i++)
            {
                MapFrameAnimationFrame frame = _frames[i];
                if (frame.Width <= 0 || frame.Height <= 0 || frame.X < 0 || frame.Y < 0
                    || frame.X + frame.Width > _texture.width || frame.Y + frame.Height > _texture.height)
                {
                    DestroySprites(sprites);
                    return Array.Empty<Sprite>();
                }

                float unityY = _texture.height - frame.Y - frame.Height;
                Sprite sprite = Sprite.Create(
                    _texture,
                    new Rect(frame.X, unityY, frame.Width, frame.Height),
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0,
                    SpriteMeshType.FullRect);
                sprite.name = _assetId + "/" + frame.Name;
                sprites[i] = sprite;
            }
            return sprites;
        }

        public static void DestroySprites(Sprite[] sprites)
        {
            if (sprites == null) return;
            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite sprite = sprites[i];
                if (sprite == null) continue;
                if (Application.isPlaying) UnityEngine.Object.Destroy(sprite);
                else UnityEngine.Object.DestroyImmediate(sprite);
            }
        }
    }
}
