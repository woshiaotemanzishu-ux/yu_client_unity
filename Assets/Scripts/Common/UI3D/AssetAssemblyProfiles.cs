using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Common.UI3D
{
    public sealed class AssetEffectBinding
    {
        [JsonProperty("bone")]
        public string Bone;

        // Legacy/full Addressable key, kept for runtime compatibility.
        [JsonProperty("effect")]
        public string Effect;

        [JsonProperty("effectDir")]
        public string EffectDir;

        [JsonProperty("effectName")]
        public string EffectName;

        [JsonProperty("time")]
        public float Time = 0f;

        [JsonProperty("position")]
        public float[] Position;

        [JsonProperty("rotation")]
        public float[] Rotation;

        [JsonProperty("scale")]
        public float[] Scale;

        [JsonProperty("color")]
        public float[] Color;

        [JsonProperty("source")]
        public string Source;

        [JsonProperty("target")]
        public string Target;

        public string ResolveEffectKey()
        {
            if (!string.IsNullOrEmpty(Effect)) return ResourcePath.Normalize(Effect);
            if (string.IsNullOrEmpty(EffectDir) || string.IsNullOrEmpty(EffectName)) return string.Empty;
            return ResourcePath.Normalize("effect/objs/" + EffectDir + "/" + EffectName + "/" + EffectName);
        }

        public Vector3 ResolvePosition()
        {
            return ToVector3(Position, Vector3.zero);
        }

        public Quaternion ResolveRotation()
        {
            return Quaternion.Euler(ToVector3(Rotation, Vector3.zero));
        }

        public Vector3 ResolveScale()
        {
            return ToVector3(Scale, Vector3.one);
        }

        private static Vector3 ToVector3(float[] values, Vector3 fallback)
        {
            if (values == null || values.Length == 0) return fallback;
            if (values.Length == 1) return new Vector3(values[0], values[0], values[0]);
            return new Vector3(
                values.Length > 0 ? values[0] : fallback.x,
                values.Length > 1 ? values[1] : fallback.y,
                values.Length > 2 ? values[2] : fallback.z);
        }
    }

    public sealed class AssetAssemblyEntry
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("label")]
        public string Label;

        [JsonProperty("businessKey")]
        public string BusinessKey;

        [JsonProperty("actionGroup")]
        public string ActionGroup;

        [JsonProperty("model")]
        public string Model;

        [JsonProperty("weapon")]
        public string Weapon;

        [JsonProperty("defaultActionContext")]
        public string DefaultActionContext;

        [JsonProperty("actions")]
        public Dictionary<string, string> Actions;

        [JsonProperty("alwaysEffects")]
        public List<AssetEffectBinding> AlwaysEffects;

        [JsonProperty("actionEffects")]
        public Dictionary<string, List<AssetEffectBinding>> ActionEffects;
    }

    public sealed class AssetAssemblyProfileRoot
    {
        [JsonProperty("profiles")]
        public List<AssetAssemblyEntry> Profiles;
    }

    public static class AssetAssemblyProfiles
    {
        public const string CONFIG_NAME = "asset_assembly_profiles";

        private static Dictionary<string, AssetAssemblyEntry> _profiles;
        private static bool _loaded;

        public static string RoleProfileId(int clotheRes)
        {
            return clotheRes > 0 ? "role/model_clothe_" + clotheRes : string.Empty;
        }

        public static async Task<AssetAssemblyEntry> GetAsync(string profileId)
        {
            if (string.IsNullOrEmpty(profileId)) return null;
            await EnsureLoaded();
            return _profiles != null && _profiles.TryGetValue(profileId, out AssetAssemblyEntry entry)
                ? entry
                : null;
        }

        public static void Invalidate()
        {
            _loaded = false;
            _profiles = null;
        }

        private static async Task EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            _profiles = new Dictionary<string, AssetAssemblyEntry>();

            TextAsset asset = await ResManager.LoadAsync<TextAsset>(GameResPath.GetClientConfigPath(CONFIG_NAME));
            if (asset == null || string.IsNullOrWhiteSpace(asset.text)) return;

            try
            {
                _profiles = ParseProfiles(asset.text);
            }
            catch (JsonException e)
            {
                GameLog.Error("UI3D", "asset assembly profile parse failed:{0}", e.Message);
            }
            finally
            {
                ResManager.Release(asset);
            }
        }

        public static Dictionary<string, AssetAssemblyEntry> ParseProfiles(string text)
        {
            var profiles = new Dictionary<string, AssetAssemblyEntry>();
            if (string.IsNullOrWhiteSpace(text)) return profiles;

            JToken root = JToken.Parse(text);
            if (root.Type != JTokenType.Object) return profiles;

            JToken listToken = root["profiles"];
            if (listToken is JArray list)
            {
                foreach (JToken item in list)
                {
                    AssetAssemblyEntry entry = item.ToObject<AssetAssemblyEntry>();
                    if (entry == null || string.IsNullOrEmpty(entry.Id)) continue;
                    profiles[entry.Id] = entry;
                }
                return profiles;
            }

            Dictionary<string, AssetAssemblyEntry> map =
                root.ToObject<Dictionary<string, AssetAssemblyEntry>>();
            if (map == null) return profiles;
            foreach (KeyValuePair<string, AssetAssemblyEntry> kv in map)
            {
                if (kv.Value == null) continue;
                if (string.IsNullOrEmpty(kv.Value.Id)) kv.Value.Id = kv.Key;
                profiles[kv.Key] = kv.Value;
            }
            return profiles;
        }
    }
}
