using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Shenxiao.Framework;
using Shenxiao.Framework.Config;

namespace Shenxiao.EditorTools.Bootstrap
{
    /// <summary>
    /// One-click creator for the Launch scene + AppConfig asset.
    /// Output:
    ///   Assets/_App/Configs/AppConfig.asset
    ///   Assets/_App/Scenes/Launch.unity  (Camera + EventSystem + Canvas + AppLauncher)
    /// </summary>
    public static class LaunchSceneCreator
    {
        private const string ConfigPath = "Assets/_App/Configs/AppConfig.asset";
        private const string ScenePath = "Assets/_App/Scenes/Launch.unity";

        [MenuItem("神霄/工具/创建启动场景", priority = 80)]
        public static void CreateLaunchScene()
        {
            // 1) AppConfig
            var config = AssetDatabase.LoadAssetAtPath<AppConfig>(ConfigPath);
            if (config == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
                config = ScriptableObject.CreateInstance<AppConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[Bootstrap] Created AppConfig at {ConfigPath}");
            }

            // 2) Scene
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.07f, 1f);
            cam.orthographic = true;
            camGo.AddComponent<AudioListener>();

            // EventSystem (use Input System UI module since project enabled the new Input System)
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            esGo.AddComponent<StandaloneInputModule>();
#endif

            // UIRoot Canvas
            var canvasGo = new GameObject("UIRoot");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = config.designResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.matchWidthOrHeight = config.canvasMatch;
            scaler.referencePixelsPerUnit = 100f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // Launcher
            var launcherGo = new GameObject("AppLauncher");
            var launcher = launcherGo.AddComponent<AppLauncher>();
            launcher.appConfig = config;
            launcher.rootCanvas = canvas;
            EditorUtility.SetDirty(launcher);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[Bootstrap] Saved Launch scene at {ScenePath}");

            // Add to Build Settings if missing
            AddSceneToBuild(ScenePath);

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(ScenePath);
        }

        private static void AddSceneToBuild(string scenePath)
        {
            var existing = EditorBuildSettings.scenes;
            foreach (var s in existing)
            {
                if (s.path == scenePath) return;
            }
            var arr = new EditorBuildSettingsScene[existing.Length + 1];
            existing.CopyTo(arr, 0);
            arr[arr.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = arr;
            Debug.Log($"[Bootstrap] Added {scenePath} to Build Settings.");
        }
    }
}
