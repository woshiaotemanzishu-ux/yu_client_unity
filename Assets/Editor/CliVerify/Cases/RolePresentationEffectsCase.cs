using System;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Res;
using Shenxiao.Module.Core.Scene;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 主角场景自身特效回归：资源齐备、attach_type=15 独立直立宿主、任务加速范围门禁，
    /// 以及升级/采集公开触发入口。日志前缀 CLIVERIFY role-effects。
    /// </summary>
    public static class RolePresentationEffectsCase
    {
        private static readonly string[] EffectPrefabs =
        {
            "Assets/GameRes/effect/objs/other_effect/effect_xemlvup/effect_xemlvup.prefab",
            "Assets/GameRes/effect/objs/other_effect/char_acceleratebuff01/char_acceleratebuff01.prefab",
            "Assets/GameRes/effect/objs/other_effect/char_jumpfx_01/char_jumpfx_01.prefab",
            "Assets/GameRes/effect/objs/other_effect/char_jumpfx_02/char_jumpfx_02.prefab",
            "Assets/GameRes/effect/objs/other_effect/effect_jump_qitiaoyan/effect_jump_qitiaoyan.prefab",
            "Assets/GameRes/effect/objs/other_effect/other_effect_caiji_02/other_effect_caiji_02.prefab",
            "Assets/GameRes/effect/objs/buff_effect/buffparticle_106_1/buffparticle_106_1.prefab",
        };

        public static async Task<int> Run()
        {
            bool fallbackBefore = ResManager.EditorPreferFallback;
            ResManager.EditorPreferFallback = true;
            GameObject role = null;
            GameObject attached = null;
            try
            {
                foreach (string path in EffectPrefabs)
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null)
                    {
                        Debug.LogError("CLIVERIFY role-effects missing prefab: " + path);
                        return 3;
                    }
                    if (prefab.GetComponentsInChildren<Renderer>(true).Length == 0 &&
                        prefab.GetComponentsInChildren<Animation>(true).Length == 0)
                    {
                        Debug.LogError("CLIVERIFY role-effects prefab has no playable visual: " + path);
                        return 3;
                    }
                }
                Debug.Log("CLIVERIFY role-effects 1 required prefabs ready=" + EffectPrefabs.Length);

                role = new GameObject("RoleEffectsProbe");
                SceneCharacterStage.SetMainRole(role);
                GameObject detached = SceneCharacterStage.MainRoleDetachedEffectHost;
                Transform tilt = role.transform.parent;
                if (detached == null || tilt == null || detached.transform.parent != tilt.parent ||
                    (detached.transform.localPosition - tilt.localPosition).sqrMagnitude > 0.000001f ||
                    Quaternion.Angle(detached.transform.localRotation, Quaternion.identity) > 0.001f)
                {
                    Debug.LogError("CLIVERIFY role-effects attach_type=15 host is not upright/sibling/aligned");
                    return 3;
                }
                Debug.Log("CLIVERIFY role-effects 2 detached host upright and aligned");

                attached = await EffectBinder.AttachOne(detached, "", "other_effect", "effect_xemlvup",
                    "verify_levelup", false);
                if (attached == null || attached.transform.parent != detached.transform)
                {
                    Debug.LogError("CLIVERIFY role-effects effect_xemlvup failed through EffectBinder");
                    return 3;
                }
                EffectBinder.PlayEffect(attached);
                Debug.Log("CLIVERIFY role-effects 3 level-up effect attached through EffectBinder");

                MethodInfo eligible = typeof(MainRoleAgent).GetMethod("IsTaskSpeedEligible",
                    BindingFlags.NonPublic | BindingFlags.Static);
                if (eligible == null)
                {
                    Debug.LogError("CLIVERIFY role-effects IsTaskSpeedEligible missing");
                    return 3;
                }
                bool Far(int type, float x, float y) =>
                    (bool)eligible.Invoke(null, new object[] { type, 0f, 0f, x, y });
                if (!Far(0, 8f * 60f, 0f) || !Far(1, 0f, 8f * 30f) || !Far(4, 8f * 60f, 0f) ||
                    Far(2, 8f * 60f, 0f) || Far(1, 7f * 60f, 0f))
                {
                    Debug.LogError("CLIVERIFY role-effects task speed scene/distance gate mismatch");
                    return 3;
                }
                Debug.Log("CLIVERIFY role-effects 4 task speed gate type=0/1/4 and distance>7");

                if (typeof(MainRoleAgent).GetMethod("NotifyLevelUp", BindingFlags.Public | BindingFlags.Static) == null ||
                    typeof(MainRoleAgent).GetMethod("MoveToTaskTarget", BindingFlags.Public | BindingFlags.Instance) == null ||
                    typeof(MainRoleAgent).GetMethod("PlayCollectCompleteEffect", BindingFlags.Public | BindingFlags.Instance) == null ||
                    !new RoleModelSpec().IncludeBodyAlwaysEffects)
                {
                    Debug.LogError("CLIVERIFY role-effects trigger entry/spec default missing");
                    return 3;
                }
                Debug.Log("CLIVERIFY role-effects 5 trigger entries and UI body-always default ready");
                Debug.Log("CLIVERIFY role-effects ALL PASS");
                return 0;
            }
            catch (Exception ex)
            {
                Debug.LogError("CLIVERIFY role-effects exception: " + ex);
                return 1;
            }
            finally
            {
                if (attached != null)
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(attached);
                    else UnityEngine.Object.DestroyImmediate(attached);
                }
                SceneCharacterStage.Clear();
                ResManager.EditorPreferFallback = fallbackBefore;
            }
        }
    }
}
