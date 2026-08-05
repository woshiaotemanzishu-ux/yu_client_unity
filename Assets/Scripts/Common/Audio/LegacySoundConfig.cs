using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using UnityEngine;

namespace Shenxiao.Common.Audio
{
    /// <summary>ConfigSound.json 的最小只读镜像，仅承载老端音乐选择与音量语义。</summary>
    internal sealed class LegacySoundConfig
    {
        private const string ConfigName = "configsound";
        private static LegacySoundConfig _instance;
        private static Task<LegacySoundConfig> _loadTask;

        private readonly Dictionary<int, string> _scene = new Dictionary<int, string>();
        private readonly Dictionary<int, string> _sceneType = new Dictionary<int, string>();
        private readonly Dictionary<string, float> _sceneVolume = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        public string DefaultScene { get; private set; } = "city";
        public string LoginOrRole { get; private set; } = "main";
        public float DefaultSceneVolume { get; private set; } = 0.3f;

        public static Task<LegacySoundConfig> LoadAsync()
        {
            if (_instance != null) return Task.FromResult(_instance);
            return _loadTask ?? (_loadTask = LoadCoreAsync());
        }

        public string ResolveScene(int sceneId, int sceneType)
        {
            if (_scene.TryGetValue(sceneId, out string exact) && !string.IsNullOrEmpty(exact)) return exact;
            if (_sceneType.TryGetValue(sceneType, out string byType) && !string.IsNullOrEmpty(byType)) return byType;
            return DefaultScene;
        }

        public float ResolveSceneVolume(string name)
            => !string.IsNullOrEmpty(name) && _sceneVolume.TryGetValue(name, out float value)
                ? Mathf.Clamp01(value)
                : Mathf.Clamp01(DefaultSceneVolume);

        private static async Task<LegacySoundConfig> LoadCoreAsync()
        {
            string key = GameResPath.GetClientConfigPath(ConfigName);
            TextAsset asset = await ResManager.LoadAsync<TextAsset>(key);
            if (asset == null)
            {
                Debug.LogError("[Audio] 缺少老端声音配置: " + key);
                _loadTask = null;
                return null;
            }

            try
            {
                JObject root = JObject.Parse(asset.text);
                var config = new LegacySoundConfig
                {
                    DefaultScene = root.Value<string>("DefaultScene") ?? "city",
                    LoginOrRole = root.Value<string>("LoginOrRole") ?? "main",
                    DefaultSceneVolume = root.Value<float?>("DefaultSceneVolume") ?? 0.3f,
                };
                ReadIntStringMap(root["Scene"] as JObject, config._scene);
                ReadIntStringMap(root["DefaultSceneType"] as JObject, config._sceneType);
                if (root["DefaultSceneTypeVolume"] is JObject volume)
                {
                    foreach (JProperty p in volume.Properties())
                        config._sceneVolume[p.Name] = p.Value.Value<float>();
                }
                _instance = config;
                return config;
            }
            catch (Exception ex)
            {
                Debug.LogError("[Audio] ConfigSound 解析失败: " + ex.Message);
                _loadTask = null;
                return null;
            }
            finally
            {
                ResManager.Release(asset);
            }
        }

        private static void ReadIntStringMap(JObject source, Dictionary<int, string> target)
        {
            if (source == null) return;
            foreach (JProperty p in source.Properties())
                if (int.TryParse(p.Name, out int id)) target[id] = p.Value.Value<string>() ?? "";
        }
    }
}
