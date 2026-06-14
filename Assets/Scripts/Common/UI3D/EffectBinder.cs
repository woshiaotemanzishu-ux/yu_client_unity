using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Common.UI3D
{
    /// <summary>
    /// 特效挂接(老客户端 UIModelClass3D.LoadBodyParticle / LoadActionParticle 的对等物):
    /// 查 SceneObjectParticle.json(随客户端配置同步进 GameRes),把转换好的特效 prefab
    /// 挂到模型骨骼上(挂后清局部变换,对标 ResetTransform)。
    /// 特效产物 = 资产管理「特效」域转换的 effect/objs/{目录}/{名}/{名}.prefab;
    /// 未转换的特效只 warn 不报错(按 warn 提示去工具里转)。
    /// </summary>
    public static class EffectBinder
    {
        // 模型 module → (SceneObjectParticle 节名, 特效目录)。与编辑器 AssetHubEffects / electron 工具同表
        private static readonly Dictionary<string, (string section, string dir)> MAP =
            new Dictionary<string, (string, string)>
            {
                ["role"] = ("Body", "role_effect"),
                ["weapon"] = ("Weapon", "weapon_effect"),
                ["wing"] = ("Wing", "wing_effect"),
                ["mount"] = ("Horse", "mount_effect"),
                ["back"] = ("BackOrnament", "back_effect"),
                ["monster"] = ("Monster", "monster_effect"),
                ["pet"] = ("Pet", "pet_effect"),
                ["god"] = ("God", "god_effect"),
                ["littlepet"] = ("Demon", "littlepet_effect"),
                ["spirit"] = ("Sprite", "spirit_effect"),
                ["fabao"] = ("FaBao", "fabao_effect"),
                ["ghost"] = ("Ghost", "ghost_effect"),
            };

        private static JObject _cfg;

        private static async Task<JObject> EnsureConfig()
        {
            if (_cfg != null) return _cfg;
            TextAsset asset = await ResManager.LoadAsync<TextAsset>("resource/config/client/sceneobjectparticle");
            if (asset == null)
            {
                GameLog.Warn("Effect", "SceneObjectParticle 配置缺失(进 Play 前会自动同步)");
                return null;
            }
            _cfg = JObject.Parse(asset.text);
            ResManager.Release(asset);
            return _cfg;
        }

        /// <summary>挂常驻特效(SceneObjectParticle.{节}[modelKey].always)。host=该模型实例。</summary>
        public static async Task AttachAlways(GameObject host, string module, string modelKey)
        {
            await AttachGroup(host, module, modelKey, "always", null);
        }

        /// <summary>挂动作特效({节}[modelKey].action[actionName]),切动作前先 ClearTag(host,"action")。</summary>
        public static async Task AttachAction(GameObject host, string module, string modelKey, string actionName)
        {
            ClearTag(host, "action");
            await AttachGroup(host, module, modelKey, "action", actionName);
        }

        private static async Task AttachGroup(GameObject host, string module, string modelKey,
            string group, string actionName)
        {
            if (host == null || !MAP.TryGetValue(module, out (string section, string dir) map)) return;
            JObject cfg = await EnsureConfig();
            JToken entry = cfg?[map.section]?[modelKey];
            JObject boneMap = group == "always"
                ? entry?["always"] as JObject
                : entry?["action"]?[actionName] as JObject;
            if (boneMap == null) return;

            foreach (KeyValuePair<string, JToken> kv in boneMap)
            {
                IEnumerable<JToken> vos = kv.Value is JArray arr ? (IEnumerable<JToken>)arr : new[] { kv.Value };
                foreach (JToken vo in vos)
                {
                    string name = (vo as JObject)?.Value<string>("name");
                    if (!string.IsNullOrEmpty(name))
                        await AttachOne(host, kv.Key, map.dir, name, group == "always" ? "always" : "action");
                }
            }
        }

        /// <summary>
        /// 挂单个特效(业务直挂入口,如创角特效 cj_1100 → AttachOne(model, "root", "skills_effect", "cj_1100"))。
        /// tag 用于成组清理(动作特效切换时清 "action")。
        /// </summary>
        public static async Task<GameObject> AttachOne(GameObject host, string boneName, string effectDir,
            string effectName, string tag = "always", bool playOnAttach = true)
        {
            if (host == null) return null;
            string key = $"effect/objs/{effectDir}/{effectName}/{effectName}";
            GameObject prefab = await ResManager.LoadAsync<GameObject>(key);
            if (prefab == null)
            {
                GameLog.Warn("Effect", "特效未转换,跳过:{0}(资产管理「特效」域里转)", key);
                return null;
            }
            if (host == null) return null; // 加载期间宿主可能已销毁
            Transform bone = RoleModelAssembler.FindBone(host.transform, boneName) ?? host.transform;
            GameObject eff = Object.Instantiate(prefab, bone);
            eff.name = "__fx_" + tag + "_" + effectName;
            eff.transform.localPosition = Vector3.zero;
            eff.transform.localRotation = Quaternion.identity;
            eff.transform.localScale = Vector3.one;
            if (playOnAttach) PlayEffect(eff);
            else eff.SetActive(false);
            return eff;
        }

        public static void PlayEffect(GameObject effect)
        {
            if (effect == null) return;
            effect.SetActive(true);
            foreach (Animation anim in effect.GetComponentsInChildren<Animation>(true))
            {
                anim.Stop();
                if (anim.clip != null) anim.Play(anim.clip.name);
            }
            foreach (ParticleSystem ps in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Clear(true);
                ps.Play(true);
            }
        }

        public static void PlayOneShot(GameObject effect)
        {
            if (effect == null) return;
            PlayEffect(effect);
            Object.Destroy(effect, EstimateLifetime(effect) + 0.1f);
        }

        private static float EstimateLifetime(GameObject effect)
        {
            float max = 0.1f;
            foreach (Animation anim in effect.GetComponentsInChildren<Animation>(true))
            {
                foreach (AnimationState state in anim)
                {
                    if (state != null) max = Mathf.Max(max, state.length);
                }
            }
            foreach (ParticleSystem ps in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                float delay = Mathf.Max(main.startDelay.constant, main.startDelay.constantMax);
                float lifetime = Mathf.Max(main.startLifetime.constant, main.startLifetime.constantMax);
                max = Mathf.Max(max, delay + main.duration + lifetime);
            }
            return max;
        }

        /// <summary>清掉 host 下指定 tag 的特效("action"=动作特效;null=全部)。</summary>
        public static void ClearTag(GameObject host, string tag)
        {
            if (host == null) return;
            string prefix = tag == null ? "__fx_" : "__fx_" + tag + "_";
            var doomed = new List<GameObject>();
            foreach (Transform t in host.GetComponentsInChildren<Transform>(true))
            {
                if (t != null && t.name.StartsWith(prefix)) doomed.Add(t.gameObject);
            }
            foreach (GameObject go in doomed) Object.Destroy(go);
        }
    }
}
