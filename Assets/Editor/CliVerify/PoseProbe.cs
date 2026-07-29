using System.IO;
using System.Text.RegularExpressions;
using Shenxiao.Common.UI3D;
using UnityEditor;
using UnityEngine;

namespace ArtDelivery
{
    /// <summary>
    /// 姿势探针:对指定 prefab,从其 Timeline 解出真实动画 clip,SampleAnimation 到固定进度
    /// (0%/50%/100%),用固定正面相机截图——两个工程各跑一次,像素级对比"是否镜像/旋转/形变"。
    /// 相机恒定:模型正面朝 +Z(交付约定),相机在 +Z 侧平视模型中心,无任何旋转/翻转。
    /// 批处理:Unity.exe -batchmode -projectPath . -executeMethod ArtDelivery.PoseProbe.Run
    ///   -logFile ... (输出 PNG 到工程根 PoseProbe_*.png)
    /// </summary>
    public static class PoseProbe
    {
        private static readonly string[] PrefabPaths =
        {
            "Assets/Role/role_1213/1213@create3.prefab",
            "Assets/GameRes/object/role/role_1213/1213@create3.prefab",
            "Assets/Role/role_1213/1213@idle.prefab",
            "Assets/GameRes/object/role/role_1213/1213@idle.prefab",
        };

        public static void Run()
        {
            int code = 0;
            try
            {
                foreach (string path in PrefabPaths)
                {
                    // File.Exists 的相对路径在 batch 工作目录下不可靠,以 AssetDatabase 为准
                    if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) Probe(path);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[PoseProbe] EXCEPTION " + e);
                code = 1;
            }
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        [MenuItem("交付/姿势探针(渲三帧)")]
        private static void RunFromMenu() { Run(); }

        private static void Probe(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) { Debug.LogError("[PoseProbe] 载入失败 " + prefabPath); return; }
            string stem = Path.GetFileNameWithoutExtension(prefabPath);
            int at = stem.IndexOf('@');
            string action = at >= 0 ? stem.Substring(at + 1) : "create3";
            string folder = Path.GetDirectoryName(prefabPath)?.Replace('\\', '/');

            AnimationClip clip = FindClipFromTimeline(folder, action) ?? FindClipByFileName(folder, action);
            GameObject inst = Object.Instantiate(prefab);
            inst.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                Animator animator = inst.GetComponentInChildren<Animator>(true);
                GameObject sampleRoot = animator != null ? animator.gameObject : inst;
                foreach (float f in new[] { 0f, 0.5f, 1f })
                {
                    if (clip != null)
                        clip.SampleAnimation(sampleRoot, Mathf.Clamp(clip.length * f, 0f, clip.length - 0.001f));
                    string png = Path.GetFullPath($"PoseProbe_{stem.Replace('@', '_')}_f{Mathf.RoundToInt(f * 100)}.png");
                    CaptureFront(inst, png);
                    Debug.Log($"[PoseProbe] {stem} clip={(clip != null ? clip.name : "无(绑定姿势)")} f={f:P0} → {png}");
                }
            }
            finally
            {
                Object.DestroyImmediate(inst);
            }
        }

        /// <summary>固定正面相机:按蒙皮包围盒取景,相机位于 +Z 侧平视包围盒中心(模型正面朝 +Z)。</summary>
        private static void CaptureFront(GameObject root, string pngPath)
        {
            Bounds b = default;
            bool has = false;
            foreach (SkinnedMeshRenderer smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var baked = new Mesh();
                smr.BakeMesh(baked, true);
                baked.RecalculateBounds();
                Bounds bb = baked.bounds;
                Matrix4x4 m = smr.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var c = bb.center + new Vector3(
                        ((i & 1) == 0 ? -1 : 1) * bb.extents.x,
                        ((i & 2) == 0 ? -1 : 1) * bb.extents.y,
                        ((i & 4) == 0 ? -1 : 1) * bb.extents.z);
                    Vector3 w = m.MultiplyPoint3x4(c);
                    if (!has) { b = new Bounds(w, Vector3.zero); has = true; } else b.Encapsulate(w);
                }
                Object.DestroyImmediate(baked);
            }
            if (!has) b = new Bounds(root.transform.position, Vector3.one);
            ArtModelStager.DisableSurfaceLighting(root);

            var rt = new RenderTexture(560, 900, 24);
            var camGo = new GameObject("__probeCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.15f, 0.15f, 0.18f, 1f);
            cam.targetTexture = rt;
            float dist = b.extents.magnitude * 2.0f + 0.01f;
            cam.transform.position = b.center + new Vector3(0f, 0f, dist); // +Z 正前方
            cam.transform.rotation = Quaternion.Euler(0f, 180f, 0f);       // 看向 -Z(平视,无翻转)
            cam.nearClipPlane = Mathf.Max(0.01f, dist * 0.01f);
            cam.farClipPlane = dist * 10f;
            cam.Render();

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(pngPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            cam.targetTexture = null;
            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(rt);
        }

        private static AnimationClip FindClipFromTimeline(string folder, string action)
        {
            string abs = Path.GetFullPath($"{folder}/Timeline/{action}.playable");
            if (!File.Exists(abs)) return null;
            foreach (Match m in Regex.Matches(File.ReadAllText(abs),
                         @"m_Clip: \{fileID: (-?\d+), guid: ([0-9a-f]{32})"))
            {
                long fileId = long.Parse(m.Groups[1].Value);
                string assetPath = AssetDatabase.GUIDToAssetPath(m.Groups[2].Value);
                if (string.IsNullOrEmpty(assetPath)) continue;
                foreach (Object sub in AssetDatabase.LoadAllAssetsAtPath(assetPath))
                {
                    if (!(sub is AnimationClip c) || c.name.StartsWith("__preview")) continue;
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(c, out _, out long lid) && lid == fileId)
                        return c;
                }
            }
            return null;
        }

        private static AnimationClip FindClipByFileName(string folder, string action)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { folder }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (!Path.GetFileNameWithoutExtension(p).ToLowerInvariant().Contains(action.ToLowerInvariant())) continue;
                foreach (Object sub in AssetDatabase.LoadAllAssetsAtPath(p))
                {
                    if (sub is AnimationClip c && !c.name.StartsWith("__preview")) return c;
                }
            }
            return null;
        }
    }
}
