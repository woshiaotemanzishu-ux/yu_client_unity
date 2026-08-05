using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.Prefs;
using Shenxiao.Framework.Res;
using UnityEngine;

namespace Shenxiao.Common.Audio
{
    /// <summary>
    /// 老客户端声音的统一运行时入口。资源地址保持 resource/sound/{type}/{name}，
    /// 音乐单路循环，音效使用可并发池；所有公开入口均可在未显式初始化时安全调用。
    /// </summary>
    public static class AudioManager
    {
        public enum Category { Music, Sfx, Voice }

        private const int InitialSfxSources = 12;
        private const int MaxSfxSources = 32;

        private sealed class PlaybackSlot
        {
            public AudioSource Source;
            public AudioClip Clip;
            public Category Category;
            public float LocalVolume;
            public float ReleaseAt;
            public int Generation;
            public bool Busy;
        }

        public sealed class PlaybackHandle
        {
            internal int SlotIndex = -1;
            internal int Generation;
            public bool IsValid => SlotIndex >= 0;
            public void Stop() => AudioManager.Stop(this);
        }

        private static readonly List<PlaybackSlot> Slots = new List<PlaybackSlot>(InitialSfxSources);
        private static AudioRuntime _runtime;
        private static AudioSource _music;
        private static AudioClip _musicClip;
        private static string _musicKey = "";
        private static int _musicEpoch;
        private static float _musicLocalVolume = 1f;
        private static float _musicVol = 1f;
        private static float _sfxVol = 1f;
        private static float _voiceVol = 1f;
        private static bool _quitting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap() => Init();

        public static void Init()
        {
            if (_quitting || _runtime != null) return;

            _runtime = UnityEngine.Object.FindFirstObjectByType<AudioRuntime>(FindObjectsInactive.Include);
            if (_runtime == null)
            {
                var root = new GameObject("AudioRoot");
                UnityEngine.Object.DontDestroyOnLoad(root);
                _runtime = root.AddComponent<AudioRuntime>();
            }

            _music = _runtime.GetComponent<AudioSource>();
            if (_music == null) _music = _runtime.gameObject.AddComponent<AudioSource>();
            ConfigureSource(_music);
            _music.loop = true;

            _musicVol = PrefsManager.GetFloat("setting.musicVolume", 1f);
            _sfxVol = PrefsManager.GetFloat("setting.sfxVolume", 1f);
            _voiceVol = 1f;

            while (Slots.Count < InitialSfxSources) CreateSlot();
            ApplyVolume();
        }

        public static void SetVolume(Category category, float value)
        {
            Init();
            value = Mathf.Clamp01(value);
            switch (category)
            {
                case Category.Music: _musicVol = value; break;
                case Category.Sfx: _sfxVol = value; break;
                case Category.Voice: _voiceVol = value; break;
            }
            ApplyVolume();
        }

        public static async Task PlayMusic(string addressKey, float localVolume = 1f)
        {
            Init();
            if (_music == null || string.IsNullOrWhiteSpace(addressKey)) return;

            string key = ResourcePath.Normalize(addressKey);
            _musicLocalVolume = Mathf.Clamp01(localVolume);
            if (_musicClip != null && string.Equals(_musicKey, key, StringComparison.Ordinal))
            {
                _music.volume = _musicVol * _musicLocalVolume;
                if (!_music.isPlaying) _music.Play();
                return;
            }

            int epoch = ++_musicEpoch;
            AudioClip clip = await ResManager.LoadAsync<AudioClip>(key);
            if (epoch != _musicEpoch || _music == null)
            {
                if (clip != null) ResManager.Release(clip);
                return;
            }
            if (clip == null)
            {
                Debug.LogWarning("[Audio] 音乐资源缺失: " + key);
                return;
            }

            ReleaseCurrentMusic();
            _musicClip = clip;
            _musicKey = key;
            _music.clip = clip;
            _music.loop = true;
            _music.volume = _musicVol * _musicLocalVolume;
            _music.Play();
        }

        public static async Task PlaySceneMusic(int sceneId, int sceneType)
        {
            LegacySoundConfig config = await LegacySoundConfig.LoadAsync();
            if (config == null) return;
            string name = config.ResolveScene(sceneId, sceneType);
            await PlayMusic(GameResPath.GetSoundPath("scene", name), config.ResolveSceneVolume(name));
        }

        public static async Task PlayLoginMusic(float localVolume = 0.1f)
        {
            LegacySoundConfig config = await LegacySoundConfig.LoadAsync();
            if (config == null) return;
            await PlayMusic(GameResPath.GetSoundPath("scene", config.LoginOrRole), localVolume);
        }

        public static Task<PlaybackHandle> PlaySfx(string addressKey, float volume = 1f, float delaySeconds = 0f)
            => PlaySfxInternal(addressKey, volume, delaySeconds, Category.Sfx);

        public static Task<PlaybackHandle> PlayUi(string name, float volume = 1f, float delaySeconds = 0f)
            => PlaySfxInternal(GameResPath.GetSoundPath("ui", name), volume, delaySeconds, Category.Sfx);

        public static Task<PlaybackHandle> PlayRole(string name, float volume = 1f, float delaySeconds = 0f)
            => PlaySfxInternal(GameResPath.GetSoundPath("role", name), volume, delaySeconds, Category.Voice);

        public static Task<PlaybackHandle> PlaySkill(string name, float volume = 1f, float delaySeconds = 0f)
            => PlaySfxInternal(GameResPath.GetSoundPath("skill", name), volume, delaySeconds, Category.Sfx);

        public static Task<PlaybackHandle> PlayNpc(string name, float volume = 1f, float delaySeconds = 0f)
            => PlaySfxInternal(GameResPath.GetSoundPath("npc", name), volume, delaySeconds, Category.Voice);

        public static Task<PlaybackHandle> PlayNoviceVoice(string name, float volume = 1f, float delaySeconds = 0f)
            => PlaySfxInternal(GameResPath.GetSoundPath("novice_voice", name), volume, delaySeconds, Category.Voice);

        public static Task<PlaybackHandle> PlayFightingVoice(int sex, int state)
        {
            string prefix = sex == 1 ? "Girl" : "Boy";
            string suffix = state == 1 ? "Lose" : state == 2 ? "Win1" : "Win2";
            return PlayRole(prefix + "_" + suffix);
        }

        public static void PauseMusic()
        {
            if (_music != null && _music.isPlaying) _music.Pause();
        }

        public static void ResumeMusic()
        {
            if (_music != null && _music.clip != null && !_music.isPlaying) _music.UnPause();
        }

        public static void StopMusic()
        {
            ++_musicEpoch;
            ReleaseCurrentMusic();
        }

        public static void Stop(PlaybackHandle handle)
        {
            if (handle == null || handle.SlotIndex < 0 || handle.SlotIndex >= Slots.Count) return;
            PlaybackSlot slot = Slots[handle.SlotIndex];
            if (slot.Generation != handle.Generation) return;
            ReleaseSlot(slot);
            handle.SlotIndex = -1;
        }

        public static void ClearAllSfx()
        {
            for (int i = 0; i < Slots.Count; i++) ReleaseSlot(Slots[i]);
        }

        internal static void Tick()
        {
            float now = Time.realtimeSinceStartup;
            for (int i = 0; i < Slots.Count; i++)
            {
                PlaybackSlot slot = Slots[i];
                if (slot.Busy && now >= slot.ReleaseAt) ReleaseSlot(slot);
            }
        }

        internal static void OnRuntimeDestroy(AudioRuntime runtime)
        {
            if (_runtime != runtime) return;
            ReleaseCurrentMusic();
            ClearAllSfx();
            _runtime = null;
            _music = null;
            _musicClip = null;
            _musicKey = "";
            Slots.Clear();
        }

        internal static void OnApplicationQuit() => _quitting = true;

        private static async Task<PlaybackHandle> PlaySfxInternal(string addressKey, float volume, float delaySeconds, Category category)
        {
            Init();
            if (_runtime == null || string.IsNullOrWhiteSpace(addressKey)) return new PlaybackHandle();

            string key = ResourcePath.Normalize(addressKey);
            AudioClip clip = await ResManager.LoadAsync<AudioClip>(key);
            if (clip == null || _runtime == null)
            {
                if (clip != null) ResManager.Release(clip);
                Debug.LogWarning("[Audio] 音效资源缺失: " + key);
                return new PlaybackHandle();
            }

            int index = AcquireSlot();
            PlaybackSlot slot = Slots[index];
            slot.Generation++;
            slot.Clip = clip;
            slot.Category = category;
            slot.LocalVolume = Mathf.Clamp01(volume);
            slot.Busy = true;
            slot.Source.clip = clip;
            slot.Source.loop = false;
            slot.Source.volume = EffectiveVolume(slot);
            delaySeconds = Mathf.Max(0f, delaySeconds);
            slot.ReleaseAt = Time.realtimeSinceStartup + delaySeconds + Mathf.Max(0.05f, clip.length) + 0.1f;
            if (delaySeconds > 0f) slot.Source.PlayDelayed(delaySeconds);
            else slot.Source.Play();
            return new PlaybackHandle { SlotIndex = index, Generation = slot.Generation };
        }

        private static int AcquireSlot()
        {
            float now = Time.realtimeSinceStartup;
            for (int i = 0; i < Slots.Count; i++)
            {
                PlaybackSlot slot = Slots[i];
                if (slot.Busy && now >= slot.ReleaseAt) ReleaseSlot(slot);
                if (!slot.Busy) return i;
            }

            if (Slots.Count < MaxSfxSources)
            {
                CreateSlot();
                return Slots.Count - 1;
            }

            int oldest = 0;
            for (int i = 1; i < Slots.Count; i++)
                if (Slots[i].ReleaseAt < Slots[oldest].ReleaseAt) oldest = i;
            ReleaseSlot(Slots[oldest]);
            return oldest;
        }

        private static void CreateSlot()
        {
            var go = new GameObject("Sfx_" + Slots.Count.ToString("00"));
            go.transform.SetParent(_runtime.transform, false);
            var source = go.AddComponent<AudioSource>();
            ConfigureSource(source);
            Slots.Add(new PlaybackSlot { Source = source, Generation = 1 });
        }

        private static void ConfigureSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.loop = false;
        }

        private static void ReleaseSlot(PlaybackSlot slot)
        {
            if (slot == null) return;
            if (slot.Source != null)
            {
                slot.Source.Stop();
                slot.Source.clip = null;
            }
            if (slot.Clip != null) ResManager.Release(slot.Clip);
            slot.Clip = null;
            slot.Busy = false;
            slot.ReleaseAt = 0f;
            slot.Generation++;
        }

        private static void ReleaseCurrentMusic()
        {
            if (_music != null)
            {
                _music.Stop();
                _music.clip = null;
            }
            if (_musicClip != null) ResManager.Release(_musicClip);
            _musicClip = null;
            _musicKey = "";
        }

        private static float EffectiveVolume(PlaybackSlot slot)
        {
            float category = slot.Category == Category.Voice ? _sfxVol * _voiceVol : _sfxVol;
            return category * slot.LocalVolume;
        }

        private static void ApplyVolume()
        {
            if (_music != null) _music.volume = _musicVol * _musicLocalVolume;
            for (int i = 0; i < Slots.Count; i++)
            {
                PlaybackSlot slot = Slots[i];
                if (slot.Source != null) slot.Source.volume = EffectiveVolume(slot);
            }
        }
    }
}
