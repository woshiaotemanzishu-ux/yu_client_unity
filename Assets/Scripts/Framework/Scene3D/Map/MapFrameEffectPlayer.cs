using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Framework.Scene3D.Map
{
    /// <summary>同一地图内按 assetId 共享计时器的序列帧播放器。</summary>
    public sealed class MapFrameEffectPlayer : MonoBehaviour
    {
        private sealed class Instance
        {
            public Image Image;
            public int FrameOffset;
        }

        private sealed class Track
        {
            public Sprite[] Frames;
            public float Fps;
            public bool Loop;
            public float Elapsed;
            public int BaseFrame;
            public readonly List<Instance> Instances = new List<Instance>();
        }

        private readonly Dictionary<string, Track> _tracks = new Dictionary<string, Track>();

        public void Register(string assetId, Sprite[] frames, float fps, bool loop, Image image, bool randomStart)
        {
            if (string.IsNullOrEmpty(assetId) || frames == null || frames.Length == 0 || image == null) return;
            if (!_tracks.TryGetValue(assetId, out Track track))
            {
                track = new Track
                {
                    Frames = frames,
                    Fps = Mathf.Max(1f, fps),
                    Loop = loop,
                    Elapsed = 0f,
                    BaseFrame = 0,
                };
                _tracks.Add(assetId, track);
            }

            int offset = randomStart ? Random.Range(0, frames.Length) : 0;
            track.Instances.Add(new Instance { Image = image, FrameOffset = offset });
            image.sprite = frames[offset];
        }

        public void Clear()
        {
            foreach (KeyValuePair<string, Track> pair in _tracks)
            {
                List<Instance> instances = pair.Value.Instances;
                for (int i = 0; i < instances.Count; i++)
                {
                    if (instances[i].Image != null) instances[i].Image.sprite = null;
                }
            }
            _tracks.Clear();
            enabled = false;
        }

        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;
            foreach (KeyValuePair<string, Track> pair in _tracks)
            {
                Track track = pair.Value;
                int count = track.Frames.Length;
                if (count <= 1) continue;

                track.Elapsed += deltaTime;
                int nextFrame;
                if (track.Loop)
                {
                    float duration = count / track.Fps;
                    if (track.Elapsed >= duration) track.Elapsed %= duration;
                    nextFrame = Mathf.FloorToInt(track.Elapsed * track.Fps) % count;
                }
                else
                {
                    nextFrame = Mathf.Min(count - 1, Mathf.FloorToInt(track.Elapsed * track.Fps));
                }

                if (nextFrame == track.BaseFrame) continue;
                track.BaseFrame = nextFrame;
                Apply(track);
            }
        }

        private static void Apply(Track track)
        {
            int count = track.Frames.Length;
            for (int i = 0; i < track.Instances.Count; i++)
            {
                Instance instance = track.Instances[i];
                if (instance.Image == null) continue;
                int frame = track.BaseFrame + instance.FrameOffset;
                frame = track.Loop ? frame % count : Mathf.Min(count - 1, frame);
                instance.Image.sprite = track.Frames[frame];
            }
        }
    }
}
