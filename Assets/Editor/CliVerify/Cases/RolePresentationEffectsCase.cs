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
            "Assets/GameRes/effect/objs/other_effect/function_selection/function_selection.prefab",
            "Assets/GameRes/effect/objs/buff_effect/buffparticle_106_1/buffparticle_106_1.prefab",
            "Assets/GameRes/effect/objs/ui_effect/effect_ui_dayaolaixi/effect_ui_dayaolaixi.prefab",
            "Assets/GameRes/effect/objs/ui_effect/ui_renwuwancheng/ui_renwuwancheng.prefab",
        };

        public static async Task<int> Run()
        {
            bool fallbackBefore = ResManager.EditorPreferFallback;
            ResManager.EditorPreferFallback = true;
            GameObject role = null;
            GameObject attached = null;
            GameObject speedTrail = null;
            GameObject selectionTarget = null;
            GameObject selection = null;
            Transform selectionTilt = null;
            CliVerify.Stage ownedStage = null;
            UIEffectStage.Handle bossIntroBanner = null;
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
                SceneCharacterStage.SetMainRole(role, 0.85f);
                GameObject detached = SceneCharacterStage.MainRoleDetachedEffectHost;
                GameObject attachedHost = SceneCharacterStage.MainRoleAttachedEffectHost;
                Transform tilt = role.transform.parent;
                if (detached == null || tilt == null || detached.transform.parent != tilt.parent ||
                    (detached.transform.localPosition - tilt.localPosition).sqrMagnitude > 0.000001f ||
                    Quaternion.Angle(detached.transform.localRotation, Quaternion.identity) > 0.001f)
                {
                    Debug.LogError("CLIVERIFY role-effects attach_type=15 host is not upright/sibling/aligned");
                    return 3;
                }
                Debug.Log("CLIVERIFY role-effects 2 detached host upright and aligned");

                if (attachedHost == null || attachedHost.transform.parent != role.transform ||
                    attachedHost.transform.localPosition.sqrMagnitude > 0.000001f ||
                    Quaternion.Angle(attachedHost.transform.localRotation, Quaternion.identity) > 0.001f ||
                    (attachedHost.transform.lossyScale - Vector3.one).sqrMagnitude > 0.000001f ||
                    (attachedHost.transform.position - role.transform.position).sqrMagnitude > 0.000001f)
                {
                    Debug.LogError("CLIVERIFY role-effects attached host does not cancel model scale/alignment");
                    return 3;
                }
                Debug.Log("CLIVERIFY role-effects 2b attached host follows role at world scale 1");

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

                if (!VerifyStageCompositeCoverage(out string compositeDiagnostic))
                {
                    Debug.LogError("CLIVERIFY role-effects stage composite brightness coverage invalid: " +
                        compositeDiagnostic);
                    return 3;
                }
                Debug.Log("CLIVERIFY role-effects 3a stage composite preserves alpha and gives additive RGB " +
                    "soft coverage; " + compositeDiagnostic);

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

                speedTrail = await EffectBinder.AttachOne(attachedHost, "", "other_effect",
                    "char_acceleratebuff01", "verify_task_speed", false);
                if (speedTrail == null || speedTrail.transform.parent != attachedHost.transform ||
                    (speedTrail.transform.lossyScale - Vector3.one).sqrMagnitude > 0.000001f)
                {
                    Debug.LogError("CLIVERIFY role-effects task speed trail did not use stable unit-scale host");
                    return 3;
                }
                if (!VerifyTaskSpeedFlowAnimation(speedTrail, out string speedFlowDiagnostic))
                {
                    Debug.LogError("CLIVERIFY role-effects task speed flow animation invalid: " +
                        speedFlowDiagnostic);
                    return 3;
                }
                Debug.Log("CLIVERIFY role-effects 4b task speed trail attached at stable world scale 1; " +
                    speedFlowDiagnostic);

                selectionTarget = new GameObject("SelectionTargetProbe");
                selectionTilt = SceneCharacterStage.AddSceneCharacter(selectionTarget);
                selection = await EffectBinder.AttachOne(selectionTilt.gameObject, "", "other_effect",
                    "function_selection", "verify_selection", false);
                if (selection == null)
                {
                    Debug.LogError("CLIVERIFY role-effects function_selection failed through EffectBinder");
                    return 3;
                }
                selection.transform.localRotation = Quaternion.identity;
                selection.transform.localScale = Vector3.one * 0.7f;
                EffectBinder.PlayEffect(selection);
                Transform selectionRing = selection.transform.Find("eff/function_seclete_pring");
                float ringScreenHeightRatio = selectionRing != null
                    ? Mathf.Abs(Vector3.Dot(selectionRing.up.normalized, Vector3.up))
                    : 0f;
                float ringCameraFacing = selectionRing != null
                    ? Mathf.Abs(Vector3.Dot(selectionRing.forward.normalized, Vector3.forward))
                    : 0f;
                bool selectionVisualReady = selection.GetComponentsInChildren<Renderer>(true).Length > 0
                    && selection.GetComponentsInChildren<Animation>(true).Length > 0
                    && Quaternion.Angle(selection.transform.localRotation, Quaternion.identity) < 0.01f
                    && Quaternion.Angle(selectionTilt.localRotation, Quaternion.Euler(-38f, 0f, 0f)) < 0.01f
                    && Quaternion.Angle(selection.transform.rotation, selectionTilt.rotation) < 0.01f
                    && (selection.transform.localScale - Vector3.one * 0.7f).sqrMagnitude < 0.000001f;
                if (!selectionVisualReady || ringScreenHeightRatio < 0.45f || ringCameraFacing < 0.5f)
                {
                    Debug.LogError("CLIVERIFY role-effects function_selection visual/projection mismatch: " +
                        $"screenHeightRatio={ringScreenHeightRatio:F3},cameraFacing={ringCameraFacing:F3}");
                    return 3;
                }
                Debug.Log("CLIVERIFY role-effects 4c function_selection playable with old-client 0.7 scale/" +
                    $"-38deg screen projection; screenHeightRatio={ringScreenHeightRatio:F3}," +
                    $"cameraFacing={ringCameraFacing:F3}");

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

                bossIntroBanner = await UIEffectStage.AddAsync("effect_ui_dayaolaixi", top);
                MethodInfo resolveBossIntroSeconds = typeof(BossBornEffectPlayer).GetMethod(
                    "ResolveEffectSeconds", BindingFlags.NonPublic | BindingFlags.Static);
                float resolvedBossIntroSeconds = resolveBossIntroSeconds != null
                    ? (float)resolveBossIntroSeconds.Invoke(null,
                        new object[] { 1.5f, bossIntroBanner?.LongestLegacyAnimationSeconds ?? 0f })
                    : -1f;
                if (bossIntroBanner == null || resolveBossIntroSeconds == null ||
                    Mathf.Abs(bossIntroBanner.LongestLegacyAnimationSeconds - 1.083f) > 0.002f ||
                    Mathf.Abs(resolvedBossIntroSeconds - bossIntroBanner.LongestLegacyAnimationSeconds) > 0.001f)
                {
                    Debug.LogError("CLIVERIFY role-effects boss intro banner lifetime mismatch: " +
                        $"legacy={(bossIntroBanner?.LongestLegacyAnimationSeconds ?? -1f):F3}," +
                        $"resolved={resolvedBossIntroSeconds:F3}");
                    return 3;
                }
                Debug.Log("CLIVERIFY role-effects 7b boss intro body/background dispose together at " +
                    $"{resolvedBossIntroSeconds:F3}s; 3s remains load fallback only");
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
                if (speedTrail != null)
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(speedTrail);
                    else UnityEngine.Object.DestroyImmediate(speedTrail);
                }
                if (selectionTilt != null) SceneCharacterStage.RemoveSceneCharacter(selectionTilt);
                bossIntroBanner?.Dispose();
                taskSuccess?.Dispose();
                SceneCharacterStage.Clear();
                ownedStage?.Dispose();
                ResManager.EditorPreferFallback = fallbackBefore;
            }
        }

        /// <summary>
        /// Additive wing/effect materials can write visible RGB with zero alpha. StageComposite must derive
        /// soft coverage from that RGB so it remains readable over bright UI/world backgrounds, while never
        /// reducing alpha already written by opaque or conventionally transparent model materials.
        /// </summary>
        private static bool VerifyStageCompositeCoverage(out string diagnostic)
        {
            bool additiveOk = RenderStageCompositeSample(new Color(0.6f, 0.2f, 0.1f, 0f), out Color additive);
            bool alphaOk = RenderStageCompositeSample(new Color(0.2f, 0.1f, 0.05f, 0.8f), out Color alpha);
            diagnostic = $"additive={additive}, alpha={alpha}";
            return additiveOk && alphaOk &&
                   additive.r >= 0.5f && additive.a >= 0.5f && additive.a <= 0.7f &&
                   alpha.a >= 0.75f;
        }

        private static bool RenderStageCompositeSample(Color sourceColor, out Color sample)
        {
            sample = Color.clear;
            Shader shader = Shader.Find("Shenxiao/UI/StageComposite");
            if (shader == null || ShaderUtil.ShaderHasError(shader)) return false;

            Material material = null;
            Texture2D source = null;
            Texture2D readback = null;
            RenderTexture rt = null;
            GameObject cameraObject = null;
            GameObject quad = null;
            RenderTexture previous = RenderTexture.active;
            try
            {
                source = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
                source.SetPixel(0, 0, sourceColor);
                source.Apply();
                material = new Material(shader);
                material.SetTexture("_MainTex", source);

                rt = new RenderTexture(16, 16, 16, RenderTextureFormat.ARGB32);
                rt.Create();
                cameraObject = new GameObject("StageCompositeCoverageProbeCamera");
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
                return true;
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
                if (source != null) UnityEngine.Object.DestroyImmediate(source);
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
        /// 老端的御风“隐现”来自透明流光贴图沿 U 方向循环滚动，不是固定三角形常亮。
        /// Laya 动画写 _MainTex_ST，而实际效果 shader 也读取 _MainTex_ST；若资源只保留
        /// _BaseMap_ST，Unity 会把贴图定格在首帧，跑动时便一直显示成实心尾巴。
        /// </summary>
        private static bool VerifyTaskSpeedFlowAnimation(GameObject effect, out string diagnostic)
        {
            // 既验证 UV 扫光真正进入运行时材质，也验证老 .lm 的逐顶点 Alpha 没有在历史资产中丢失；
            // 后者缺失时，即使动画正常，亮区扫到矩形网格端点仍会形成用户看到的方形硬截断。
            Animation animation = effect != null
                ? effect.GetComponentInChildren<Animation>(true)
                : null;
            AnimationClip clip = animation != null ? animation.clip : null;
            if (clip == null)
            {
                diagnostic = "Animation/clip missing";
                return false;
            }

            EditorCurveBinding binding = Array.Find(AnimationUtility.GetCurveBindings(clip), item =>
                item.type == typeof(MeshRenderer) &&
                item.propertyName == "material._MainTex_ST.z");
            AnimationCurve scroll = string.IsNullOrEmpty(binding.propertyName)
                ? null
                : AnimationUtility.GetEditorCurve(clip, binding);
            if (scroll == null)
            {
                diagnostic = "material._MainTex_ST.z missing";
                return false;
            }

            float start = scroll.Evaluate(0f);
            float middle = scroll.Evaluate(0.5f);
            float end = scroll.Evaluate(1f);
            MeshRenderer renderer = animation.GetComponentInChildren<MeshRenderer>(true);
            AnimationState state = animation[clip.name];
            if (renderer == null || state == null)
            {
                diagnostic = "flow MeshRenderer/AnimationState missing";
                return false;
            }

            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            Color[] vertexColors = mesh != null ? mesh.colors : Array.Empty<Color>();
            if (mesh == null || vertexColors.Length != mesh.vertexCount)
            {
                diagnostic = $"flow mesh vertex fade missing: colors={vertexColors.Length}, " +
                             $"vertices={(mesh != null ? mesh.vertexCount : 0)}";
                return false;
            }
            float minVertexAlpha = 1f;
            float maxVertexAlpha = 0f;
            bool hasSoftVertexAlpha = false;
            foreach (Color color in vertexColors)
            {
                minVertexAlpha = Mathf.Min(minVertexAlpha, color.a);
                maxVertexAlpha = Mathf.Max(maxVertexAlpha, color.a);
                hasSoftVertexAlpha |= color.a > 0.01f && color.a < 0.99f;
            }
            if (minVertexAlpha > 0.01f || maxVertexAlpha < 0.99f || !hasSoftVertexAlpha)
            {
                diagnostic = $"flow mesh vertex fade invalid: alpha={minVertexAlpha:F3}..{maxVertexAlpha:F3}, " +
                             $"soft={hasSoftVertexAlpha}";
                return false;
            }

            EffectBinder.PlayEffect(effect);
            state.enabled = true;
            state.weight = 1f;
            var properties = new MaterialPropertyBlock();
            state.time = 0f;
            animation.Sample();
            renderer.GetPropertyBlock(properties);
            float runtimeStart = properties.GetVector("_MainTex_ST").z;
            state.time = 0.5f;
            animation.Sample();
            renderer.GetPropertyBlock(properties);
            float runtimeMiddle = properties.GetVector("_MainTex_ST").z;

            diagnostic = $"curve={start:F2}->{middle:F2}->{end:F2}, " +
                         $"runtime={runtimeStart:F2}->{runtimeMiddle:F2}, wrap={clip.wrapMode}, " +
                         $"vertexAlpha={minVertexAlpha:F3}..{maxVertexAlpha:F3}";
            return clip.wrapMode == WrapMode.Loop && scroll.length >= 2 &&
                   Mathf.Abs(start) <= 0.001f && Mathf.Abs(middle - 1.5f) <= 0.01f &&
                   Mathf.Abs(end - 3f) <= 0.01f && Mathf.Abs(runtimeStart) <= 0.01f &&
                   Mathf.Abs(runtimeMiddle - 1.5f) <= 0.01f;
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
