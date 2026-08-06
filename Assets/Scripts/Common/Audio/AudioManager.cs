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
            public bool OwnsClipReference;
            public Category Category;
            public float LocalVolume;
            public float ReleaseAt;
            public int Generation;
            public bool Busy;
        }

        private readonly struct ClipLease
        {
            public ClipLease(AudioClip clip, bool ownsReference)
            {
                Clip = clip;
                OwnsReference = ownsReference;
            }

            public AudioClip Clip { get; }
            public bool OwnsReference { get; }
        }

        public sealed class PlaybackHandle
        {
            internal int SlotIndex = -1;
            internal int Generation;
            public bool IsValid => SlotIndex >= 0;
            public void Stop() => AudioManager.Stop(this);
        }

        private static readonly List<PlaybackSlot> Slots = new List<PlaybackSlot>(InitialSfxSources);
        // 只驻留启动阶段明确会用到的小闭包（当前场景、当前职业、当前技能和高频 UI），
        // 不允许把 310 个声音全量塞进内存。字典中的每个 clip 恰好持有一份 ResManager 引用。
        private static readonly Dictionary<string, AudioClip> ResidentClips =
            new Dictionary<string, AudioClip>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Task<AudioClip>> ResidentLoads =
            new Dictionary<string, Task<AudioClip>>(StringComparer.Ordinal);
        private static AudioRuntime _runtime;
        private static AudioSource _music;
        private static AudioClip _musicClip;
        private static bool _musicOwnsClipReference;
        private static string _musicKey = "";
        private static int _musicEpoch;
        private static int _residentEpoch;
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
            ClipLease lease = await GetPlaybackClipAsync(key);
            AudioClip clip = lease.Clip;
            if (epoch != _musicEpoch || _music == null)
            {
                if (clip != null && lease.OwnsReference) ResManager.Release(clip);
                return;
            }
            if (clip == null)
            {
                Debug.LogWarning("[Audio] 音乐资源缺失: " + key);
                return;
            }

            ReleaseCurrentMusic();
            _musicClip = clip;
            _musicOwnsClipReference = lease.OwnsReference;
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

        /// <summary>
        /// 在加载页内准备即将进入场景的唯一一首 BGM。若登录/选角已经在播同一地址则无需重复持有。
        /// </summary>
        public static async Task PreloadSceneMusic(int sceneId, int sceneType)
        {
            LegacySoundConfig config = await LegacySoundConfig.LoadAsync();
            if (config == null) return;
            string key = ResourcePath.Normalize(
                GameResPath.GetSoundPath("scene", config.ResolveScene(sceneId, sceneType)));
            if (_musicClip != null && string.Equals(_musicKey, key, StringComparison.Ordinal)) return;
            await PreloadAddressAsync(key);
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

        public static Task PreloadUi(params string[] names) => PreloadCategoryAsync("ui", names);
        public static Task PreloadRole(params string[] names) => PreloadCategoryAsync("role", names);
        public static Task PreloadSkill(params string[] names) => PreloadCategoryAsync("skill", names);

        public static string ResolveRoleShowVoice(int career)
        {
            switch (career)
            {
                case 1: return "boy_show1";
                case 2: return "girl_show1";
                case 3: return "boy_show2";
                case 4: return "girl_show2";
                default: return string.Empty;
            }
        }

        public static string ResolveFightingVoice(int sex, int state)
        {
            string prefix = sex == 1 ? "Girl" : "Boy";
            string suffix = state == 1 ? "Lose" : state == 2 ? "Win1" : "Win2";
            return prefix + "_" + suffix;
        }

        public static Task<PlaybackHandle> PlayFightingVoice(int sex, int state)
            => PlayRole(ResolveFightingVoice(sex, state));

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
            ++_musicEpoch;
            ++_residentEpoch;
            ReleaseCurrentMusic();
            ClearAllSfx();
            foreach (AudioClip clip in ResidentClips.Values)
                if (clip != null) ResManager.Release(clip);
            ResidentClips.Clear();
            ResidentLoads.Clear();
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
            ClipLease lease = await GetPlaybackClipAsync(key);
            AudioClip clip = lease.Clip;
            if (clip == null || _runtime == null)
            {
                if (clip != null && lease.OwnsReference) ResManager.Release(clip);
                Debug.LogWarning("[Audio] 音效资源缺失: " + key);
                return new PlaybackHandle();
            }

            int index = AcquireSlot();
            PlaybackSlot slot = Slots[index];
            slot.Generation++;
            slot.Clip = clip;
            slot.OwnsClipReference = lease.OwnsReference;
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
            if (slot.Clip != null && slot.OwnsClipReference) ResManager.Release(slot.Clip);
            slot.Clip = null;
            slot.OwnsClipReference = false;
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
            if (_musicClip != null && _musicOwnsClipReference) ResManager.Release(_musicClip);
            _musicClip = null;
            _musicOwnsClipReference = false;
            _musicKey = "";
        }

        private static Task PreloadCategoryAsync(string category, string[] names)
        {
            Init();
            if (_runtime == null || names == null || names.Length == 0) return Task.CompletedTask;

            var tasks = new List<Task>(names.Length);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < names.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(names[i])) continue;
                string key = ResourcePath.Normalize(GameResPath.GetSoundPath(category, names[i]));
                if (seen.Add(key)) tasks.Add(PreloadAddressAsync(key));
            }
            return tasks.Count == 0 ? Task.CompletedTask : Task.WhenAll(tasks);
        }

        private static Task<AudioClip> PreloadAddressAsync(string addressKey)
        {
            Init();
            if (_runtime == null || string.IsNullOrWhiteSpace(addressKey))
                return Task.FromResult<AudioClip>(null);

            string key = ResourcePath.Normalize(addressKey);
            if (ResidentClips.TryGetValue(key, out AudioClip resident) && resident != null)
                return Task.FromResult(resident);
            if (ResidentLoads.TryGetValue(key, out Task<AudioClip> pending)) return pending;

            var completion = new TaskCompletionSource<AudioClip>();
            ResidentLoads[key] = completion.Task;
            _ = CompleteResidentLoadAsync(key, _residentEpoch, completion);
            return completion.Task;
        }

        private static async Task CompleteResidentLoadAsync(string key, int epoch,
            TaskCompletionSource<AudioClip> completion)
        {
            AudioClip clip = null;
            try
            {
                clip = await ResManager.LoadAsync<AudioClip>(key);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Audio] 预热失败: " + key + " - " + e.Message);
            }

            if (epoch != _residentEpoch || _runtime == null)
            {
                if (clip != null) ResManager.Release(clip);
                clip = null;
            }
            else if (clip != null)
            {
                ResidentClips[key] = clip;
            }
            else
            {
                Debug.LogWarning("[Audio] 预热资源缺失: " + key);
            }

            if (ResidentLoads.TryGetValue(key, out Task<AudioClip> pending)
                && ReferenceEquals(pending, completion.Task))
            {
                ResidentLoads.Remove(key);
            }
            completion.TrySetResult(clip);
        }

        private static async Task<ClipLease> GetPlaybackClipAsync(string key)
        {
            if (ResidentClips.TryGetValue(key, out AudioClip resident) && resident != null)
                return new ClipLease(resident, false);
            if (ResidentLoads.TryGetValue(key, out Task<AudioClip> pending))
                return new ClipLease(await pending, false);
            return new ClipLease(await ResManager.LoadAsync<AudioClip>(key), true);
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
