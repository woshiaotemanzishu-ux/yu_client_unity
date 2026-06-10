using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Shenxiao.Editor.LayaUI
{
    /// <summary>
    /// 720×1280 预览场景:UI prefab 不挂在带 CanvasScaler 的 Canvas 下看,位置和"屏幕"
    /// 都没有参照(直接开 prefab 会漂在天空盒里),所谓"超出屏幕"多半是这个观察方式造成的。
    /// 此工具建一个设计分辨率画布场景并把模块 prefab 摆进去,Game 视图切 720x1280 即所见即所得。
    /// </summary>
    public static class LayaUIPreview
    {
        private const string SCENE_PATH = "Assets/_App/Scenes/LayaUIPreview.unity";

        public static void OpenWithPrefab(string module)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            LayaUIManifest manifest = LayaUIManifest.Load();
            string moduleDir = manifest != null ? manifest.ModuleDir(module) : module;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject camGo = new GameObject("Camera", typeof(Camera));
            Camera cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
            cam.orthographic = true;

            GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720f, 1280f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f; // 近似 Laya fixedauto

            // 设计分辨率参考框(只在编辑器里画线,不参与渲染)
            GameObject frame = new GameObject("__DesignFrame", typeof(RectTransform));
            RectTransform frt = (RectTransform)frame.transform;
            frt.SetParent(canvasGo.transform, false);
            frt.sizeDelta = new Vector2(720f, 1280f);

            string folder = LayaUISettings.PREFAB_ROOT + "/" + moduleDir;
            int placed = 0;
            if (System.IO.Directory.Exists(folder))
            {
                foreach (string file in System.IO.Directory.GetFiles(folder, "*.prefab", System.IO.SearchOption.TopDirectoryOnly))
                {
                    GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(file.Replace('\\', '/'));
                    if (asset == null) continue;
                    GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                    inst.transform.SetParent(canvasGo.transform, false);
                    placed++;
                }
            }
            if (placed == 0)
            {
                Debug.LogWarning("[LayaUI] " + folder + " 下没有 prefab,先转换模块 " + module);
            }

            System.IO.Directory.CreateDirectory("Assets/_App/Scenes");
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            Debug.Log("[LayaUI] 预览场景已就绪: " + SCENE_PATH +
                      "(Game 视图分辨率切成 720x1280;在 Hierarchy 里勾选要看的窗口," +
                      "想看真实叠层效果就同时激活 LoginBgView + 当前窗口)");
        }
    }
}
