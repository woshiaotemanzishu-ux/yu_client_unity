using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.Playables;

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
    /// 衣服为主体。带独立 Timeline 的新头饰挂 head_mount 完整继承身体头骨变换,自身 Timeline 播子骨动画;
    /// 无独立动画的静态头饰才挂 head_mount(旧资源回退 head)。武器以 weapon_attach 对齐角色 rhand。
    /// 动作 clip 从共享目录 object/role/action/{1000+career*100}/ 按名加载,对标 PlayActions 顺序播放。
    /// </summary>
    public static class RoleModelAssembler
    {
        public static async Task<GameObject> BuildAsync(RoleModelSpec spec)
        {
            if (spec == null || spec.ClotheRes <= 0) return null;

            // 新模型替换(资产管理按动作逐条配置,model_replacement.json):该模型有任何动作配了新
            // prefab → 返回混合驱动容器(ReplaceableRoleModel):配了的动作亮新模型,没配的动作自动
            // 切回老拼装模型,逐动作互切。清单里完全没配 → 纯老管线,零改动。
            await ModelReplacement.EnsureLoaded();
            if (ModelReplacement.HasEntry("role", spec.ClotheRes))
            {
                var container = new GameObject($"role_{spec.ClotheRes}_mix");
                var driver = container.AddComponent<ReplaceableRoleModel>();
                driver.Init(spec);
                string first = driver.PreferredAction(spec.Actions);
                await driver.PlayAsync(first, restart: true);
                return container;
            }
            return await BuildOldModelAsync(spec);
        }

        /// <summary>原始管线(老拼装):衣服+部件+老 clip。混合驱动器的老模型分支也走这里。</summary>
        internal static async Task<GameObject> BuildOldModelAsync(RoleModelSpec spec)
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
            LoadedAssetReleaser.Track(root, prefab);
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

        /// <summary>
        /// 新模型整装:清单指到的身体 prefab + 部件(头饰/武器/翅膀/背饰,清单有新用新、没新挂老件——
        /// 带独立 Timeline 的头饰已经包含自身飘动动作,挂 head_mount 继承身体头骨完整变换;
        /// 静态头饰才走 head_mount(旧资源仍回退 head)。ArtModelStager 统一上台包装
        /// (落点归一/根位移/透明分流)。循环/停末帧由 ReplaceableRoleModel 按动作再设。
        /// 加载失败返回 null,调用方回落原始管线。
        /// </summary>
        internal static async Task<GameObject> BuildNewModelAsync(RoleModelSpec spec, string bodyKey, string action)
        {
            GameObject bodyPrefab = await ResManager.LoadOptionalAsync<GameObject>(bodyKey);
            if (bodyPrefab == null)
            {
                GameLog.Warn("UI3D", "替换清单指向的新模型缺失:{0}(资产管理[更新导入])", bodyKey);
                return null;
            }
            GameObject inst = Object.Instantiate(bodyPrefab);
            LoadedAssetReleaser.Track(inst, bodyPrefab);

            if (spec.HeadRes > 0)
            {
                string headKey = ModelReplacement.GetPrefabKey("head", spec.HeadRes, action)
                    ?? ModelReplacement.GetPrefabKey("head", spec.HeadRes, "idle")
                    ?? Key("head", "model_head_" + spec.HeadRes);
                await AttachPartOptional(inst, "head_mount", headKey, "head",
                    attachAnimatedAtHeadSocket: true,
                    animatedPositionOffset: ModelReplacement.GetAttachmentPositionOffset("head", spec.HeadRes),
                    animatedRotationOffset: ModelReplacement.GetAttachmentRotationOffset("head", spec.HeadRes),
                    animatedScale: ModelReplacement.GetAttachmentScale("head", spec.HeadRes));
            }
            if (spec.WeaponRes > 0)
            {
                string weaponKey = ModelReplacement.GetPrefabKey("weapon", spec.WeaponRes, action)
                    ?? ModelReplacement.GetPrefabKey("weapon", spec.WeaponRes, "idle")
                    ?? Key("weapon", "model_weapon_r_" + spec.WeaponRes);
                await AttachPartOptional(inst, "rhand", weaponKey,
                    attachmentLocatorName: "weapon_attach",
                    attachmentPositionOffset: ModelReplacement.GetAttachmentPositionOffset("weapon", spec.WeaponRes),
                    attachmentRotationOffset: ModelReplacement.GetAttachmentRotationOffset("weapon", spec.WeaponRes),
                    attachmentScale: ModelReplacement.GetAttachmentScale("weapon", spec.WeaponRes));
            }
            if (spec.WingId > 0)
            {
                string replacementWingKey = ModelReplacement.GetPrefabKey("wing", spec.WingId, action)
                    ?? ModelReplacement.GetPrefabKey("wing", spec.WingId, "idle");
                string wingKey = replacementWingKey ?? Key("wing", "model_wing_" + spec.WingId);
                await AttachPartOptional(inst, "wing", wingKey,
                    attachmentLocatorName: replacementWingKey != null ? "wing_attach" : null,
                    attachmentPositionOffset: ModelReplacement.GetAttachmentPositionOffset("wing", spec.WingId),
                    attachmentRotationOffset: ModelReplacement.GetAttachmentRotationOffset("wing", spec.WingId),
                    attachmentScale: ModelReplacement.GetAttachmentScale("wing", spec.WingId));
            }
            if (spec.BackOrnamentId > 0)
            {
                string replacementBackKey = ModelReplacement.GetPrefabKey("back", spec.BackOrnamentId, action)
                    ?? ModelReplacement.GetPrefabKey("back", spec.BackOrnamentId, "idle");
                string backKey = replacementBackKey ?? Key("back", "model_back_" + spec.BackOrnamentId);
                await AttachPartOptional(inst, "wing", backKey,
                    attachmentLocatorName: replacementBackKey != null ? "back_attach" : null,
                    attachmentPositionOffset: ModelReplacement.GetAttachmentPositionOffset("back", spec.BackOrnamentId),
                    attachmentRotationOffset: ModelReplacement.GetAttachmentRotationOffset("back", spec.BackOrnamentId),
                    attachmentScale: ModelReplacement.GetAttachmentScale("back", spec.BackOrnamentId));
            }

            GameObject staged = ArtModelStager.Stage(inst, bodyPrefab, UnityEngine.Playables.DirectorWrapMode.Loop);
            GameLog.Info("UI3D", "新模型上台:{0}(action={1},head={2},weapon={3},wing={4},back={5})",
                bodyKey, action, spec.HeadRes, spec.WeaponRes, spec.WingId, spec.BackOrnamentId);
            return staged;
        }

        /// <summary>新模型部件挂接:资源缺失只警告不阻塞(对标 AttachPart,不带特效绑定)。</summary>
        private static async Task AttachPartOptional(GameObject root, string boneName, string key,
            string legacyBoneName = null, bool attachAnimatedAtHeadSocket = false,
            Vector3 animatedPositionOffset = default, Vector3 animatedRotationOffset = default,
            float animatedScale = 1f, string attachmentLocatorName = null,
            Vector3 attachmentPositionOffset = default, Vector3 attachmentRotationOffset = default,
            float attachmentScale = 1f)
        {
            GameObject prefab = await ResManager.LoadOptionalAsync<GameObject>(key);
            if (prefab == null)
            {
                GameLog.Warn("UI3D", "新模型部件缺失,跳过:{0}", key);
                return;
            }
            // 动态头饰的 Bone_head 根不参与自身 Timeline，实际动画只在发丝/装饰子骨。
            // 因此动态件和静态件都挂 head_mount：父级负责身体头骨完整变换，头饰 Timeline 负责内部子骨动画。
            bool attachAnimatedAtSocket = attachAnimatedAtHeadSocket &&
                                          prefab.GetComponentInChildren<PlayableDirector>(true) != null;
            Transform anchor = FindBone(root.transform, boneName);
            if (anchor == null && !string.IsNullOrEmpty(legacyBoneName))
            {
                anchor = FindBone(root.transform, legacyBoneName);
                if (anchor != null)
                    GameLog.Warn("UI3D", "模型缺新挂点 {0},临时回退旧挂点 {1}:{2}(请从 Art 项目重导)",
                        boneName, legacyBoneName, root.name);
            }
            if (anchor == null)
            {
                GameLog.Warn("UI3D", "挂点骨骼缺失:{0}(模型 {1},美术工程跑[交付/补挂点]后重导)", boneName, root.name);
                ResManager.Release(prefab); // 借了没用上,当场归还
                return;
            }
            GameObject part = Object.Instantiate(prefab, anchor);
            LoadedAssetReleaser.Track(part, prefab);
            // 新模型部件(带渲染档案)自带烤好的根偏移:根节点偏移 + 子 FBX 等值负偏移,相消把网格
            // 摆在 prefab 原点。保留这份烤入变换,网格才精确落到挂点节点(根+子相消=0,数学实锤);
            // 清零会打破相消→网格飘出挂点(头饰浮空实锤)。老式静态件网格原点即挂接点,才清零贴骨。
            if (attachAnimatedAtSocket || part.GetComponentInChildren<ArtModelRenderProfile>(true) != null)
            {
                part.transform.localPosition = prefab.transform.localPosition;
                part.transform.localRotation = prefab.transform.localRotation;
                part.transform.localScale = prefab.transform.localScale;
            }
            else
            {
                part.transform.localPosition = Vector3.zero;
                part.transform.localRotation = Quaternion.identity;
                part.transform.localScale = Vector3.one;
            }

            if (attachAnimatedAtSocket)
            {
                Transform bodyHead = FindSkinnedBone(root, "head", "Bip001 Head", "Bip001_Head");
                // 新规范优先使用独立、非蒙皮的 head_attach 定位节点；旧资源才回退 Bone_head。
                // 定位节点与发丝骨架职责分离后，美术可以在模板内校准包络而不改动画骨。
                Transform attachmentHead = FindBone(part.transform, "head_attach")
                    ?? FindSkinnedBone(part, "Bone_head");
                if (bodyHead != null && attachmentHead != null)
                {
                    var follower = part.AddComponent<AnimatedAttachmentPositionFollower>();
                    follower.Initialize(bodyHead, attachmentHead, root.transform, animatedPositionOffset,
                        animatedRotationOffset, animatedScale);
                }
                else
                {
                    GameLog.Warn("UI3D",
                        "动态头饰定位骨缺失:身体 head={0},头饰 head_attach/Bone_head={1}({2});已挂 head_mount 但无法做局部校准",
                        bodyHead != null, attachmentHead != null, key);
                }
            }

            if (!string.IsNullOrEmpty(attachmentLocatorName))
            {
                Transform locator = FindBone(part.transform, attachmentLocatorName);
                if (locator != null)
                {
                    var aligner = part.AddComponent<AttachmentSocketAligner>();
                    aligner.Initialize(locator, attachmentPositionOffset, attachmentRotationOffset, attachmentScale);
                }
                else
                {
                    // 旧武器没有独立 locator：保持原 prefab 根姿态，只叠加临时校准；新交付必须补 weapon_attach。
                    part.transform.localPosition += attachmentPositionOffset;
                    part.transform.localRotation *= Quaternion.Euler(attachmentRotationOffset);
                    part.transform.localScale = Vector3.Scale(part.transform.localScale,
                        Vector3.one * Mathf.Max(0.01f, attachmentScale));
                    GameLog.Warn("UI3D", "部件定位点缺失:{0}({1});已按旧 prefab 根兼容,请从 Art 模板重导",
                        attachmentLocatorName, key);
                }
            }
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
                ResManager.Release(prefab); // 借了没用上,当场归还
                return null;
            }
            GameObject part = Object.Instantiate(prefab, bone);
            LoadedAssetReleaser.Track(part, prefab);
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
            // 新模型(带渲染档案,Timeline 自播):老 clip 按 Transform 路径绑老骨架,喂进来也绑不上,直接跳过
            if (root.GetComponentInChildren<ArtModelRenderProfile>(true) != null) return;
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
                    LoadedAssetReleaser.Track(root, clip);
                }
            }
        }

        public static async Task PrepareRoleActions(GameObject root, int career, int clotheRes, string[] actions)
        {
            if (root == null || actions == null || actions.Length == 0) return;
            var driver = root.GetComponent<ReplaceableRoleModel>();
            if (driver != null)
            {
                await driver.PrepareActionsAsync(actions);
                return;
            }
            AssetAssemblyEntry profile = await AssetAssemblyProfiles.GetAsync(AssetAssemblyProfiles.RoleProfileId(clotheRes));
            await PrepareActions(root, career, actions, profile);
        }

        public static async Task PlayActionAsync(GameObject root, string actionName, AssetAssemblyEntry profile)
        {
            if (root == null || string.IsNullOrEmpty(actionName)) return;
            var driver = root.GetComponent<ReplaceableRoleModel>();
            if (driver != null)
            {
                await driver.PlayAsync(actionName, restart: true); // 先完成新老互切，特效才能挂到本次动作实例
                if (driver == null || driver.ActiveModel == null) return;
                root = driver.ActiveModel;
            }
            else
            {
                var anim = root.GetComponent<Animation>();
                if (anim != null && anim.GetClip(actionName) != null)
                {
                    anim.Stop();
                    anim.Play(actionName);
                }
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
            var driver = root.GetComponent<ReplaceableRoleModel>();
            if (driver != null)
            {
                // 混合模型没有旧 Animation 的排队能力:优先播序列中已交付的新动作。
                // create2 未交而 create3 已交时，直接上 create3，不能永久停在旧 create2。
                driver.Play(driver.PreferredAction(actions), restart: true);
                return;
            }
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

        private static Transform FindSkinnedBone(GameObject root, params string[] names)
        {
            if (root == null || names == null) return null;
            foreach (string name in names)
            {
                foreach (SkinnedMeshRenderer renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    foreach (Transform bone in renderer.bones)
                    {
                        if (bone != null && bone.name == name) return bone;
                    }
                }
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
