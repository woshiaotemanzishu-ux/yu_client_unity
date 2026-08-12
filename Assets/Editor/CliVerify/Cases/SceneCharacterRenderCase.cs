using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Common.UI3D;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 场景角色共用合成台回归：使用当前线上可达的主角/NPC/怪物 prefab，强制专用相机 Render，
    /// 再从 SceneCharStageRT 读回像素。不能用 Renderer/Animation 存在代替真实出帧。
    /// </summary>
    public static class SceneCharacterRenderCase
    {
        private const string MainRolePath =
            "Assets/GameRes/object/role/role_1111/1111@idle.prefab";
        private const string NpcPath =
            "Assets/GameRes/object/npc/model_clothe_100102/model_clothe_100102.prefab";
        private const string MonsterPath =
            "Assets/GameRes/object/monster/model_clothe_10010103/model_clothe_10010103.prefab";
        private const string EvidenceRoot =
            "output/ui_route_audit/2026-08-06_scene-model-render/";
        private const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;

        public static Task<int> Run()
        {
            CliVerify.Stage stage = null;
            Transform npcTilt = null;
            Transform monsterTilt = null;
            bool pass = false;
            try
            {
                stage = CliVerify.Stage.Create();
                GameObject mainPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainRolePath);
                GameObject npcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NpcPath);
                GameObject monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPath);
                if (mainPrefab == null || npcPrefab == null || monsterPrefab == null)
                {
                    Debug.LogError("CLIVERIFY scene-character-render prefab missing: main=" + (mainPrefab != null)
                        + " npc=" + (npcPrefab != null) + " monster=" + (monsterPrefab != null));
                    return Task.FromResult(3);
                }

                GameObject mainInstance = UnityEngine.Object.Instantiate(mainPrefab);
                GameObject main = ArtModelStager.Stage(mainInstance, mainPrefab, DirectorWrapMode.Loop);
                GameObject npc = UnityEngine.Object.Instantiate(npcPrefab);
                GameObject monster = UnityEngine.Object.Instantiate(monsterPrefab);
                PlayFirst(main);
                PlayFirst(npc);
                PlayFirst(monster);

                SceneCharacterStage.SetMainRole(main);
                npcTilt = SceneCharacterStage.AddSceneCharacter(npc);
                monsterTilt = SceneCharacterStage.AddSceneCharacter(monster, 0.8f);
                SceneCharacterStage.SetSceneCharacterPixelOffset(npcTilt, new Vector2(-180f, 0f));
                SceneCharacterStage.SetSceneCharacterPixelOffset(monsterTilt, new Vector2(180f, 0f));

                Canvas.ForceUpdateCanvases();
                pass = CaptureCurrentStage(EvidenceRoot + "baseline_new_art_stage_rt.png", true,
                    out int alphaPixels, out int litPixels, out string rtPath, out string diagnostic);
                string compositePath = stage.Capture(EvidenceRoot + "baseline_new_art_scene_composite.png");
                Debug.Log("CLIVERIFY scene-character-render alpha=" + alphaPixels
                    + " lit=" + litPixels + " diagnostic=" + diagnostic
                    + " rt=" + rtPath + " composite=" + compositePath);
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY scene-character-render EXCEPTION " + e);
                pass = false;
            }
            finally
            {
                if (npcTilt != null) SceneCharacterStage.RemoveSceneCharacter(npcTilt);
                if (monsterTilt != null) SceneCharacterStage.RemoveSceneCharacter(monsterTilt);
                SceneCharacterStage.Clear();
                stage?.Dispose();
            }

            Debug.Log("CLIVERIFY scene-character-render VERDICT pass=" + pass);
            return Task.FromResult(pass ? 0 : 3);
        }

        /// <summary>
        /// 读取当前 SceneCharacterStage 的真实 RT。供静态 prefab 基线和活服 PlaySmoke 共用，
        /// 避免“模型已实例化 / Renderer 存在”被误判成已经出帧。
        /// </summary>
        internal static bool CaptureCurrentStage(string projectRelativePng, bool renderNow,
            out int alphaPixels, out int litPixels, out string path, out string diagnostic)
        {
            Type type = typeof(SceneCharacterStage);
            Camera camera = type.GetField("_cam", StaticPrivate)?.GetValue(null) as Camera;
            RenderTexture target = type.GetField("_rt", StaticPrivate)?.GetValue(null) as RenderTexture;
            RawImage image = type.GetField("_img", StaticPrivate)?.GetValue(null) as RawImage;
            Transform chars = type.GetField("_charsRoot", StaticPrivate)?.GetValue(null) as Transform;
            diagnostic = BuildDiagnostic(camera, target, image, chars);
            alphaPixels = 0;
            litPixels = 0;
            path = string.Empty;
            if (camera == null || target == null || image == null || chars == null) return false;

            // EditMode 的合成台没有播放器帧循环，需要显式 Render；PlayMode 已由 LateUpdate 驱动，
            // 禁止再从 EditorApplication.update 重入 Camera.Render（Unity 6 + D3D12 会崩溃）。
            if (renderNow) camera.Render();
            RenderTexture previous = RenderTexture.active;
            Texture2D copy = null;
            try
            {
                RenderTexture.active = target;
                copy = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false, true);
                copy.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
                copy.Apply(false);
                foreach (Color32 pixel in copy.GetPixels32())
                {
                    if (pixel.a > 2) alphaPixels++;
                    if (pixel.r > 2 || pixel.g > 2 || pixel.b > 2) litPixels++;
                }

                path = Path.GetFullPath(CliVerify.AppendResolutionSuffix(projectRelativePng));
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, copy.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                if (copy != null) UnityEngine.Object.DestroyImmediate(copy);
            }

            return alphaPixels > 256 && litPixels > 256;
        }

        private static string BuildDiagnostic(Camera camera, RenderTexture target, RawImage image, Transform chars)
        {
            int total = 0;
            int active = 0;
            int inFrustum = 0;
            if (camera != null && chars != null)
            {
                Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
                Renderer[] renderers = chars.GetComponentsInChildren<Renderer>(true);
                total = renderers.Length;
                foreach (Renderer renderer in renderers)
                {
                    if (renderer == null || !renderer.enabled || renderer.forceRenderingOff
                        || !renderer.gameObject.activeInHierarchy) continue;
                    active++;
                    if (GeometryUtility.TestPlanesAABB(planes, renderer.bounds)) inFrustum++;
                }
            }

            return "camera=" + (camera != null)
                + ",cameraEnabled=" + (camera != null && camera.enabled)
                + ",mask=" + (camera != null ? camera.cullingMask : 0)
                + ",rt=" + (target != null ? target.width + "x" + target.height : "null")
                + ",rtCreated=" + (target != null && target.IsCreated())
                + ",image=" + (image != null)
                + ",imageActive=" + (image != null && image.gameObject.activeInHierarchy)
                + ",renderers=" + total + ",active=" + active + ",inFrustum=" + inFrustum;
        }

        private static void PlayFirst(GameObject model)
        {
            if (model == null) return;
            Animation animation = model.GetComponent<Animation>();
            if (animation == null) return;
            if (animation.GetClip("idle") != null)
            {
                animation.Play("idle");
                return;
            }

            foreach (AnimationState state in animation)
            {
                animation.Play(state.name);
                break;
            }
        }
    }
}
