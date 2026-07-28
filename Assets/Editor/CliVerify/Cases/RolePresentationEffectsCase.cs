using System;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Scene;
using Shenxiao.Module.Core.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

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
            "Assets/GameRes/effect/objs/ui_effect/ui_renwuwancheng/ui_renwuwancheng.prefab",
        };

        public static async Task<int> Run()
        {
            bool fallbackBefore = ResManager.EditorPreferFallback;
            ResManager.EditorPreferFallback = true;
            GameObject role = null;
            GameObject attached = null;
            CliVerify.Stage ownedStage = null;
            UIEffectStage.Handle taskSuccess = null;
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

                if (ViewManager.GetLayer(UILayer.Scene) == null)
                    ownedStage = CliVerify.Stage.Create();

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

                RawImage[] images = UnityEngine.Object.FindObjectsByType<RawImage>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
                RawImage sceneImage = Array.Find(images, image => image != null && image.name == "__SceneChars");
                if (sceneImage == null || sceneImage.material == null || sceneImage.material.shader == null ||
                    sceneImage.material.shader.name != "Shenxiao/UI/StageComposite")
                {
                    Debug.LogError("CLIVERIFY role-effects scene RT is not using premultiplied composite");
                    return 3;
                }
                Debug.Log("CLIVERIFY role-effects 3 scene RT premultiplied composite ready");

                if (!VerifyLegacyLinearTint(out Color neutralTintSample))
                {
                    Debug.LogError("CLIVERIFY role-effects Laya neutral tint is dim/invalid in Linear space, sample=" +
                        neutralTintSample);
                    return 3;
                }
                Debug.Log("CLIVERIFY role-effects 3b Laya neutral tint sample=" + neutralTintSample);

                attached = await EffectBinder.AttachOne(detached, "", "other_effect", "effect_xemlvup",
                    "verify_levelup", false);
                if (attached == null || attached.transform.parent != detached.transform)
                {
                    Debug.LogError("CLIVERIFY role-effects effect_xemlvup failed through EffectBinder");
                    return 3;
                }
                EffectBinder.PlayEffect(attached);
                if (!VerifyLevelUpTextParticle(attached, out string levelTextDiagnostic))
                {
                    Debug.LogError("CLIVERIFY role-effects level-up text timing/size invalid: " +
                        levelTextDiagnostic);
                    return 3;
                }
                Debug.Log("CLIVERIFY role-effects 4 level-up effect attached; " + levelTextDiagnostic);

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
                Debug.Log("CLIVERIFY role-effects 5 task speed gate type=0/1/4 and distance>7");

                if (typeof(MainRoleAgent).GetMethod("NotifyLevelUp", BindingFlags.Public | BindingFlags.Static) == null ||
                    typeof(MainRoleAgent).GetMethod("MoveToTaskTarget", BindingFlags.Public | BindingFlags.Instance) == null ||
                    typeof(MainRoleAgent).GetMethod("PlayCollectCompleteEffect", BindingFlags.Public | BindingFlags.Instance) == null ||
                    !new RoleModelSpec().IncludeBodyAlwaysEffects)
                {
                    Debug.LogError("CLIVERIFY role-effects trigger entry/spec default missing");
                    return 3;
                }
                MethodInfo successEffect = typeof(TaskController).GetMethod("PlayTaskSuccessEffectAsync",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                FieldInfo successPosition = typeof(TaskController).GetField("TaskSuccessEffectPosition",
                    BindingFlags.NonPublic | BindingFlags.Static);
                if (successEffect == null || successPosition == null ||
                    (Vector2)successPosition.GetValue(null) != new Vector2(0f, -4f))
                {
                    Debug.LogError("CLIVERIFY role-effects task success effect entry/vertical RT mapping missing");
                    return 3;
                }
                Debug.Log("CLIVERIFY role-effects 6 trigger entries and UI body-always default ready");

                RectTransform top = ViewManager.GetLayer(UILayer.Top) as RectTransform;
                taskSuccess = await UIEffectStage.AddAsync("ui_renwuwancheng", top,
                    new Vector2(0f, -4f), Vector3.one);
                UIEffectStage.EffectDiagnostic taskSuccessDiagnostic = UIEffectStage.CollectDiagnostics()
                    .Find(item => item.Label == "ui_renwuwancheng");
                if (taskSuccess == null || !taskSuccessDiagnostic.EffectAlive ||
                    !taskSuccessDiagnostic.EffectActiveInHierarchy ||
                    taskSuccessDiagnostic.ParticleSystemCount <= 0 ||
                    taskSuccessDiagnostic.ParentName != top?.name)
                {
                    Debug.LogError("CLIVERIFY role-effects task success Top-layer instance failed");
                    return 3;
                }
                Debug.Log("CLIVERIFY role-effects 7 task success UI effect alive on Top layer");
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
                taskSuccess?.Dispose();
                SceneCharacterStage.Clear();
                ownedStage?.Dispose();
                ResManager.EditorPreferFallback = fallbackBefore;
            }
        }

        private static bool VerifyLevelUpTextParticle(GameObject effect, out string diagnostic)
        {
            ParticleSystem text = Array.Find(effect.GetComponentsInChildren<ParticleSystem>(true),
                item => item != null && item.name == "other_xemshenji01");
            if (text == null)
            {
                diagnostic = "other_xemshenji01 missing";
                return false;
            }

            ParticleSystem.MainModule main = text.main;
            text.Simulate(0.55f, false, true, true);
            var particles = new ParticleSystem.Particle[1];
            int count = text.GetParticles(particles);
            float currentSize = count > 0 ? particles[0].GetCurrentSize(text) : 0f;
            diagnostic = $"delay={main.startDelay.constant:F3}, lifetime={main.startLifetime.constant:F3}, " +
                $"count={count}, size@0.55={currentSize:F3}";
            return Mathf.Abs(main.startDelay.constant - 0.1f) <= 0.001f &&
                   Mathf.Abs(main.startLifetime.constant - 1.1f) <= 0.001f &&
                   count == 1 && currentSize >= 1.5f;
        }

        /// <summary>
        /// Laya 粒子用 0.5 tint × shader 2 作为“原色”。项目为 Linear 时 Unity 会先把 Color
        /// 转成约 0.214；这里走真实 shader/RT，防止升级文字和整套光效再次退化到约 43% 强度。
        /// </summary>
        private static bool VerifyLegacyLinearTint(out Color sample)
        {
            sample = Color.white;
            if (QualitySettings.activeColorSpace != ColorSpace.Linear) return true;

            Material source = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/GameRes/effect/objs/other_effect/effect_xemlvup/other_xemshenji01.mat");
            if (source == null || source.shader == null || ShaderUtil.ShaderHasError(source.shader)) return false;

            Material material = null;
            RenderTexture rt = null;
            GameObject cameraObject = null;
            GameObject quad = null;
            Texture2D readback = null;
            RenderTexture previous = RenderTexture.active;
            try
            {
                material = new Material(source);
                material.SetTexture("_MainTex", Texture2D.whiteTexture);
                material.SetColor("_BaseColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);

                rt = new RenderTexture(16, 16, 16, RenderTextureFormat.ARGB32);
                rt.Create();
                cameraObject = new GameObject("RoleEffectsTintProbeCamera");
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.orthographic = true;
                camera.orthographicSize = 1f;
                camera.transform.position = new Vector3(0f, 0f, -10f);
                camera.targetTexture = rt;
                camera.cullingMask = 1 << 31;

                quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.layer = 31;
                quad.transform.localScale = new Vector3(2f, 2f, 1f);
                quad.GetComponent<Renderer>().sharedMaterial = material;
                camera.Render();

                RenderTexture.active = rt;
                readback = new Texture2D(16, 16, TextureFormat.RGBA32, false, true);
                readback.ReadPixels(new Rect(0f, 0f, 16f, 16f), 0, 0);
                readback.Apply();
                sample = readback.GetPixel(8, 8);
                return sample.r >= 0.95f && sample.g >= 0.95f && sample.b >= 0.95f;
            }
            finally
            {
                RenderTexture.active = previous;
                if (readback != null) UnityEngine.Object.DestroyImmediate(readback);
                if (quad != null) UnityEngine.Object.DestroyImmediate(quad);
                if (cameraObject != null) UnityEngine.Object.DestroyImmediate(cameraObject);
                if (rt != null)
                {
                    rt.Release();
                    UnityEngine.Object.DestroyImmediate(rt);
                }
                if (material != null) UnityEngine.Object.DestroyImmediate(material);
            }
        }
    }
}
