using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Common.UI3D
{
    /// <summary>组装参数(老客户端 show_model_data 的最小子集;翅膀/时装贴图/背饰待时装线)。</summary>
    public sealed class RoleModelSpec
    {
        public int Career;
        public int ClotheRes;       // model_clothe_{id}
        public int WeaponRes;       // model_weapon_r_{id},0=无
        public int HeadRes;         // model_head_{id},0=无
        public string[] Actions;    // 按 ConfigModelAni(顺序播放,最后一个循环与否由 .lani 决定)
    }

    /// <summary>
    /// 角色 3D 组装(老客户端 UIModelClass3D 装配部分的对等物):
    /// 衣服为主体,头饰挂 head 骨、武器挂 rhand 骨(职业 1~4 均右手单持,
    /// 对标 GameResPath.GetWeaponRes 的 WeaponCountInfo.role),挂上后清局部变换(ResetTransform)。
    /// 动作 clip 从共享目录 object/role/action/{career*1000+100}/ 按名加载,对标 PlayActions 顺序播放。
    /// </summary>
    public static class RoleModelAssembler
    {
        public static async Task<GameObject> BuildAsync(RoleModelSpec spec)
        {
            if (spec == null || spec.ClotheRes <= 0) return null;
            GameObject prefab = await ResManager.LoadAsync<GameObject>(Key("role", "model_clothe_" + spec.ClotheRes));
            if (prefab == null)
            {
                GameLog.Warn("UI3D", "衣服模型未转换:model_clothe_{0}(资产管理工具里转)", spec.ClotheRes);
                return null;
            }
            GameObject root = Object.Instantiate(prefab);

            if (spec.HeadRes > 0)
                await AttachPart(root, "head", Key("head", "model_head_" + spec.HeadRes));
            if (spec.WeaponRes > 0)
                await AttachPart(root, "rhand", Key("weapon", "model_weapon_r_" + spec.WeaponRes));

            await ApplyActions(root, spec);
            return root;
        }

        private static string Key(string module, string name)
        {
            return $"object/{module}/{name}/{name}";
        }

        private static async Task AttachPart(GameObject root, string boneName, string key)
        {
            GameObject prefab = await ResManager.LoadAsync<GameObject>(key);
            if (prefab == null)
            {
                GameLog.Warn("UI3D", "部件未转换,跳过:{0}(资产管理工具里转)", key);
                return;
            }
            Transform bone = FindDeep(root.transform, boneName);
            if (bone == null)
            {
                GameLog.Warn("UI3D", "挂点骨骼缺失:{0}(模型 {1})", boneName, root.name);
                return;
            }
            GameObject part = Object.Instantiate(prefab, bone);
            // 对标老客户端 ResetTransform:挂上后清局部位移/旋转/缩放
            part.transform.localPosition = Vector3.zero;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = Vector3.one;
        }

        private static async Task ApplyActions(GameObject root, RoleModelSpec spec)
        {
            if (spec.Actions == null || spec.Actions.Length == 0) return;
            var anim = root.GetComponent<Animation>();
            if (anim == null) anim = root.AddComponent<Animation>();
            string dir = (spec.Career * 1000 + 100).ToString();
            bool first = true;
            foreach (string name in spec.Actions)
            {
                if (anim.GetClip(name) == null)
                {
                    var clip = await ResManager.LoadAsync<AnimationClip>($"object/role/action/{dir}/{name}");
                    if (clip == null)
                    {
                        GameLog.Warn("UI3D", "动作未转换,跳过:{0}/{1}(资产管理工具勾选动作重转)", dir, name);
                        continue;
                    }
                    anim.AddClip(clip, name);
                }
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

        private static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++)
            {
                Transform found = FindDeep(t.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
