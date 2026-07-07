using System.Threading.Tasks;
using UnityEngine;
using Shenxiao.Common.Prefs;
using Shenxiao.Framework.Res;

namespace Shenxiao.Common.Audio
{
    /// <summary>
    /// Audio playback. Phase 0 skeleton: single music + multi sfx, per-category volume.
    /// </summary>
    public static class AudioManager
    {
        public enum Category { Music, Sfx, Voice }

        private static AudioSource _music;
        private static AudioSource _sfx;
        private static AudioSource _voice;

        private static float _musicVol = 1f, _sfxVol = 1f, _voiceVol = 1f;

        public static void Init()
        {
            var go = new GameObject("AudioRoot");
            Object.DontDestroyOnLoad(go);
            _music = go.AddComponent<AudioSource>();
            _music.loop = true;
            _sfx = go.AddComponent<AudioSource>();
            _voice = go.AddComponent<AudioSource>();
            // 本地音量镜像(设置面板滑条写入,服务器 10202 到达后会再覆盖同步)。无镜像时保持满音量。
            _musicVol = PrefsManager.GetFloat("setting.musicVolume", 1f);
            _sfxVol = PrefsManager.GetFloat("setting.sfxVolume", 1f);
            ApplyVolume();
        }

        public static void SetVolume(Category c, float v)
        {
            v = Mathf.Clamp01(v);
            switch (c)
            {
                case Category.Music: _musicVol = v; break;
                case Category.Sfx: _sfxVol = v; break;
                case Category.Voice: _voiceVol = v; break;
            }
            ApplyVolume();
        }

        public static async Task PlayMusic(string addrKey)
        {
            var clip = await ResManager.LoadAsync<AudioClip>(addrKey);
            if (clip == null || _music == null) return;
            _music.clip = clip;
            _music.Play();
        }

        public static async Task PlaySfx(string addrKey)
        {
            var clip = await ResManager.LoadAsync<AudioClip>(addrKey);
            if (clip == null || _sfx == null) return;
            _sfx.PlayOneShot(clip);
        }

        public static void StopMusic()
        {
            if (_music != null) _music.Stop();
        }

        private static void ApplyVolume()
        {
            if (_music != null) _music.volume = _musicVol;
            if (_sfx != null) _sfx.volume = _sfxVol;
            if (_voice != null) _voice.volume = _voiceVol;
        }
    }
}
