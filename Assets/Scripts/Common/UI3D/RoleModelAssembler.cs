using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Common.UI3D
{
    /// <summary>组装参数(老客户端 show_model_data 的最小子集;时装贴图 Clothe/坐骑待形象线)。</summary>
    public sealed class RoleModelSpec
    {
        public int Career;
        public int ClotheRes;       // model_clothe_{id}
        public int WeaponRes;       // model_weapon_r_{id},0=无
        public int HeadRes;         // model_head_{id},0=无
        public int WingId;          // model_wing_{id},0=无(挂 wing 骨)
        public int BackOrnamentId;  // model_back_{id},0=无(挂 wing 骨,AttachNode.BackOrnament)
        public string[] Actions;    // 按 ConfigModelAni(顺序播放,最后一个循环与否由 .lani 决定)
        public bool AutoPlayActions = true;
    }

    /// <summary>
    /// 角色 3D 组装(老客户端 UIModelClass3D 装配部分的对等物):
    /// 衣服为主体,头饰挂 head 骨、武器挂 rhand 骨(职业 1~4 均右手单持,
    /// 对标 GameResPath.GetWeaponRes 的 WeaponCountInfo.role),挂上后清局部变换(ResetTransform)。
    /// 动作 clip 从共享目录 object/role/action/{1000+career*100}/ 按名加载,对标 PlayActions 顺序播放。
    /// </summary>
    public static class RoleModelAssembler
    {
        public static async Task<GameObject> BuildAsync(RoleModelSpec spec)
        {
            if (spec == null || spec.ClotheRes <= 0) return null;
            AssetAssemblyEntry profile = await AssetAssemblyProfiles.GetAsync(AssetAssemblyProfiles.RoleProfileId(spec.ClotheRes));
            string defaultModelKey = Key("role", "model_clothe_" + spec.ClotheRes);
            string modelKey = !string.IsNullOrEmpty(profile?.Model) ? profile.Model : defaultModelKey;
            GameObject prefab = await ResManager.LoadAsync<GameObject>(modelKey);
            if (prefab == null)
            {
                GameLog.Warn("UI3D", "衣服模型未转换:{0}(资产管理工具里转)", modelKey);
                return null;
            }
            GameObject root = Object.Instantiate(prefab);
            if (profile != null && profile.AlwaysEffects != null)
            {
                await EffectBinder.AttachBindings(root, FilterEffects(profile.AlwaysEffects, "model"), "always");
            }
            else
            {
                // 常驻特效(SceneObjectParticle.Body;默认装多无记录,时装 N125 家族有)
                await EffectBinder.AttachAlways(root, "role", spec.ClotheRes.ToString());
            }

            if (spec.HeadRes > 0)
                await AttachPart(root, "head", Key("head", "model_head_" + spec.HeadRes), null, null);
            if (spec.WeaponRes > 0)
            {
                GameObject weapon = await AttachPart(root, "rhand", Key("weapon", "model_weapon_r_" + spec.WeaponRes),
                    profile == null ? "weapon" : null, spec.WeaponRes.ToString());
                if (profile != null)
                    await EffectBinder.AttachBindings(weapon, FilterEffects(profile.AlwaysEffects, "weapon"), "always");
            }
            if (spec.WingId > 0)
                await AttachPart(root, "wing", Key("wing", "model_wing_" + spec.WingId),
                    "wing", spec.WingId.ToString());
            if (spec.BackOrnamentId > 0)
                await AttachPart(root, "wing", Key("back", "model_back_" + spec.BackOrnamentId),
                    "back", spec.BackOrnamentId.ToString());

            await PrepareActions(root, spec.Career, spec.Actions, profile);
            if (spec.AutoPlayActions) PlayActions(root, spec.Actions);
            return root;
        }

        private static string Key(string module, string name)
        {
            return $"object/{module}/{name}/{name}";
        }

        private static async Task<GameObject> AttachPart(GameObject root, string boneName, string key,
            string effectModule, string effectKey)
        {
            GameObject prefab = await ResManager.LoadAsync<GameObject>(key);
            if (prefab == null)
            {
                GameLog.Warn("UI3D", "部件未转换,跳过:{0}(资产管理工具里转)", key);
                return null;
            }
            Transform bone = FindBone(root.transform, boneName);
            if (bone == null)
            {
                GameLog.Warn("UI3D", "挂点骨骼缺失:{0}(模型 {1})", boneName, root.name);
                return null;
            }
            GameObject part = Object.Instantiate(prefab, bone);
            // 对标老客户端 ResetTransform:挂上后清局部位移/旋转/缩放
            part.transform.localPosition = Vector3.zero;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = Vector3.one;
            // 部件常驻特效(如武器 Weapon[1100] 的剑光,挂在武器自身骨骼上)
            if (effectModule != null)
                await EffectBinder.AttachAlways(part, effectModule, effectKey);
            return part;
        }

        public static async Task PrepareActions(GameObject root, int career, string[] actions,
            AssetAssemblyEntry profile = null)
        {
            if (root == null || actions == null || actions.Length == 0) return;
            var anim = root.GetComponent<Animation>();
            if (anim == null) anim = root.AddComponent<Animation>();
            foreach (string name in actions)
            {
                if (string.IsNullOrEmpty(name)) continue;
                if (anim.GetClip(name) == null)
                {
                    string key = ActionKey(career, name, profile);
                    var clip = await ResManager.LoadAsync<AnimationClip>(key);
                    if (clip == null)
                    {
                        GameLog.Warn("UI3D", "动作未转换,跳过:{0}(资产管理工具勾选动作重转)", key);
                        continue;
                    }
                    anim.AddClip(clip, name);
                }
            }
        }

        public static async Task PrepareRoleActions(GameObject root, int career, int clotheRes, string[] actions)
        {
            AssetAssemblyEntry profile = await AssetAssemblyProfiles.GetAsync(AssetAssemblyProfiles.RoleProfileId(clotheRes));
            await PrepareActions(root, career, actions, profile);
        }

        public static async Task PlayActionAsync(GameObject root, string actionName, AssetAssemblyEntry profile)
        {
            if (root == null || string.IsNullOrEmpty(actionName)) return;
            var anim = root.GetComponent<Animation>();
            if (anim != null && anim.GetClip(actionName) != null)
            {
                anim.Stop();
                anim.Play(actionName);
            }
            if (profile?.ActionEffects != null
                && profile.ActionEffects.TryGetValue(actionName, out var bindings))
            {
                await EffectBinder.AttachBindings(root, bindings, "action");
            }
        }

        public static void PlayActions(GameObject root, string[] actions)
        {
            if (root == null || actions == null || actions.Length == 0) return;
            var anim = root.GetComponent<Animation>();
            if (anim == null) return;
            anim.Stop();
            bool first = true;
            foreach (string name in actions)
            {
                if (anim.GetClip(name) == null) continue;
                if (first)
                {
                    anim.Play(name);
                    first = false;
                }
                else
                {
                    anim.PlayQueued(name, QueueMode.CompleteOthers);
                }
            }
        }

        private static IEnumerable<AssetEffectBinding> FilterEffects(
            IEnumerable<AssetEffectBinding> bindings, string target)
        {
            if (bindings == null) yield break;
            foreach (AssetEffectBinding binding in bindings)
            {
                if (binding == null) continue;
                string bindingTarget = string.IsNullOrEmpty(binding.Target) ? "model" : binding.Target;
                if (bindingTarget == target) yield return binding;
            }
        }

        /// <summary>按名递归找骨骼(老客户端 Util.FindBone 对等;EffectBinder 也用)。</summary>
        public static Transform FindBone(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++)
            {
                Transform found = FindBone(t.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static string ActionKey(int career, string actionName, AssetAssemblyEntry profile)
        {
            if (profile?.Actions != null
                && profile.Actions.TryGetValue(actionName, out string key)
                && !string.IsNullOrEmpty(key))
            {
                return key;
            }
            // 动作目录 = 1000 + career*100(剑士1100/武姬1200/枪使1300/弓手1400)
            string dir = (1000 + career * 100).ToString();
            return $"object/role/action/{dir}/{actionName}";
        }
    }
}
