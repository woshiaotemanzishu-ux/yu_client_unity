using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Shenxiao.Editor.RuntimeCapture
{
    /// <summary>
    /// Editor-only runtime UI evidence capture. It records the current Game View screenshot
    /// and a Canvas node dump for old-client vs Unity UI patrols.
    /// </summary>
    public static class RuntimeUiCaptureTool
    {
        private const string OutputSubPath = "output/runtime_unity";
        private const string ProbeName = "RuntimeUiCaptureProbe";
        private const float DefaultPlayCaptureDelaySeconds = 45f;

        private static bool _quitAfterDelayedCapture;
        private static float _pendingDelaySeconds = DefaultPlayCaptureDelaySeconds;
        private static double _delayedCaptureAt;
        private static double _delayedExitAt;

        [MenuItem("神霄/调试/UI运行态/截图+节点Dump", priority = 110)]
        public static void CaptureNow()
        {
            try
            {
                CaptureInternal();
            }
            catch (Exception e)
            {
                Debug.LogError("[RuntimeUiCapture] Capture failed: " + e);
            }
        }

        [MenuItem("神霄/调试/UI运行态/进入Play后延迟截图+节点Dump", priority = 111)]
        public static void PlayThenCaptureFromMenu()
        {
            PlayThenCapture(DefaultPlayCaptureDelaySeconds, false);
        }

        /// <summary>
        /// Command-line entry. Use without -quit; this method exits Unity after capture.
        /// Optional arg: -runtimeCaptureDelaySeconds 60
        /// </summary>
        public static void PlayThenCaptureFromCommandLine()
        {
            PlayThenCapture(GetCommandLineDelaySeconds(), true);
        }

        private static void PlayThenCapture(float delaySeconds, bool quitAfterCapture)
        {
            _quitAfterDelayedCapture = quitAfterCapture;
            _pendingDelaySeconds = delaySeconds;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.update -= OnDelayedCaptureUpdate;
            EditorApplication.update -= OnDelayedExitUpdate;

            if (Application.isPlaying)
            {
                ScheduleDelayedCapture(delaySeconds);
                return;
            }

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            Debug.Log("[RuntimeUiCapture] Entering Play Mode, capture delay=" + delaySeconds.ToString("0.###", CultureInfo.InvariantCulture) + "s.");
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode)
            {
                return;
            }

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            ScheduleDelayedCapture(_pendingDelaySeconds);
        }

        private static void ScheduleDelayedCapture(float delaySeconds)
        {
            float safeDelay = Mathf.Clamp(delaySeconds, 1f, 300f);
            _delayedCaptureAt = EditorApplication.timeSinceStartup + safeDelay;
            EditorApplication.update -= OnDelayedCaptureUpdate;
            EditorApplication.update += OnDelayedCaptureUpdate;
            Debug.Log("[RuntimeUiCapture] Scheduled runtime capture after " + safeDelay.ToString("0.###", CultureInfo.InvariantCulture) + "s.");
        }

        private static void OnDelayedCaptureUpdate()
        {
            if (EditorApplication.timeSinceStartup < _delayedCaptureAt)
            {
                return;
            }

            EditorApplication.update -= OnDelayedCaptureUpdate;
            try
            {
                CaptureInternal();
                if (_quitAfterDelayedCapture)
                {
                    ScheduleCommandLineExit();
                }
                else
                {
                    _quitAfterDelayedCapture = false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[RuntimeUiCapture] Delayed capture failed: " + e);
                if (_quitAfterDelayedCapture)
                {
                    EditorApplication.Exit(1);
                }
            }
        }

        private static void ScheduleCommandLineExit()
        {
            // CaptureScreenshot writes at frame end; keep the editor alive briefly so the png can flush.
            _delayedExitAt = EditorApplication.timeSinceStartup + 2d;
            EditorApplication.update -= OnDelayedExitUpdate;
            EditorApplication.update += OnDelayedExitUpdate;
        }

        private static void OnDelayedExitUpdate()
        {
            if (EditorApplication.timeSinceStartup < _delayedExitAt)
            {
                return;
            }

            EditorApplication.update -= OnDelayedExitUpdate;
            _quitAfterDelayedCapture = false;
            EditorApplication.Exit(0);
        }

        private static float GetCommandLineDelaySeconds()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], "-runtimeCaptureDelaySeconds", StringComparison.OrdinalIgnoreCase) &&
                    float.TryParse(args[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out float seconds))
                {
                    return seconds;
                }
            }

            return DefaultPlayCaptureDelaySeconds;
        }

        private static void CaptureInternal()
        {
            bool isPlaying = Application.isPlaying;
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            string sessionDir = Path.Combine(projectRoot, OutputSubPath, stamp);
            Directory.CreateDirectory(sessionDir);

            string screenshotPath = Path.Combine(sessionDir, "screenshot.png");
            string dumpPath = Path.Combine(sessionDir, "ui_dump.txt");
            string jsonPath = Path.Combine(sessionDir, "ui_dump.json");
            string statusPath = Path.Combine(sessionDir, "status.txt");

            if (isPlaying)
            {
                ScreenCapture.CaptureScreenshot(screenshotPath);
            }

            List<Canvas> canvases = CollectRootCanvases();
            canvases.Sort(CompareCanvas);

            int nodeCount = 0;
            string dump = BuildDump(stamp, isPlaying, canvases, ref nodeCount);
            string json = BuildJson(stamp, isPlaying, canvases);
            string status = BuildStatus(stamp, isPlaying, screenshotPath, dumpPath, nodeCount, canvases.Count);

            File.WriteAllText(dumpPath, dump, Encoding.UTF8);
            File.WriteAllText(jsonPath, json, Encoding.UTF8);
            File.WriteAllText(statusPath, status, Encoding.UTF8);

            Debug.Log(
                "[RuntimeUiCapture] Completed. canvas=" + canvases.Count +
                " nodes=" + nodeCount +
                " dir=" + sessionDir +
                (isPlaying ? " screenshot will be written at frame end." : " screenshot skipped because Unity is not in Play Mode."));
        }

        private static int CompareCanvas(Canvas left, Canvas right)
        {
            int sorting = left.sortingOrder.CompareTo(right.sortingOrder);
            if (sorting != 0)
            {
                return sorting;
            }

            return string.CompareOrdinal(left.name, right.name);
        }

        private static List<Canvas> CollectRootCanvases()
        {
            var roots = new List<GameObject>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                {
                    roots.AddRange(scene.GetRootGameObjects());
                }
            }

            if (Application.isPlaying)
            {
                roots.AddRange(GetDontDestroyOnLoadRoots());
            }

            var result = new List<Canvas>();
            var seen = new HashSet<Canvas>();
            foreach (GameObject root in roots)
            {
                if (root == null)
                {
                    continue;
                }

                Canvas[] children = root.GetComponentsInChildren<Canvas>(true);
                foreach (Canvas canvas in children)
                {
                    Canvas rootCanvas = canvas.rootCanvas;
                    if (rootCanvas != null && seen.Add(rootCanvas))
                    {
                        result.Add(rootCanvas);
                    }
                }
            }

            return result;
        }

        private static IEnumerable<GameObject> GetDontDestroyOnLoadRoots()
        {
            var probe = new GameObject(ProbeName) { hideFlags = HideFlags.HideAndDontSave };
            UnityEngine.Object.DontDestroyOnLoad(probe);
            Scene scene = probe.scene;
            GameObject[] roots = scene.IsValid() ? scene.GetRootGameObjects() : Array.Empty<GameObject>();
            UnityEngine.Object.DestroyImmediate(probe);

            var result = new List<GameObject>();
            foreach (GameObject root in roots)
            {
                if (root != null && root.name != ProbeName)
                {
                    result.Add(root);
                }
            }

            return result;
        }

        private static string BuildDump(string stamp, bool isPlaying, List<Canvas> canvases, ref int nodeCount)
        {
            var sb = new StringBuilder(64 * 1024);
            sb.AppendLine("# Runtime UI Capture");
            sb.AppendLine("timestamp        : " + stamp);
            sb.AppendLine("isPlaying        : " + isPlaying);
            sb.AppendLine("screen           : " + Screen.width + " x " + Screen.height);
            sb.AppendLine("loadedScenes     : " + string.Join(", ", GetLoadedSceneNames()));
            sb.AppendLine("eventSystems     : " + string.Join(", ", GetEventSystemNames()));
            sb.AppendLine("selected         : " + GetSelectedGameObjectName());
            sb.AppendLine("rootCanvasCount  : " + canvases.Count);
            sb.AppendLine();

            foreach (Canvas canvas in canvases)
            {
                AppendNode(sb, canvas.transform, null, 0, ref nodeCount);
            }

            sb.AppendLine();
            sb.AppendLine("nodeCount        : " + nodeCount);
            return sb.ToString();
        }

        private static string BuildStatus(
            string stamp,
            bool isPlaying,
            string screenshotPath,
            string dumpPath,
            int nodeCount,
            int canvasCount)
        {
            var sb = new StringBuilder();
            sb.AppendLine("timestamp=" + stamp);
            sb.AppendLine("isPlaying=" + isPlaying);
            sb.AppendLine("canvasCount=" + canvasCount);
            sb.AppendLine("nodeCount=" + nodeCount);
            sb.AppendLine("dumpPath=" + dumpPath);
            if (isPlaying)
            {
                sb.AppendLine("screenshotRequested=true");
                sb.AppendLine("screenshotPath=" + screenshotPath);
                sb.AppendLine("note=ScreenCapture writes the PNG at frame end.");
            }
            else
            {
                sb.AppendLine("screenshotRequested=false");
                sb.AppendLine("screenshotPath=<skipped: not in Play Mode>");
                sb.AppendLine("note=The node dump reflects the current editor scene objects, not a runtime login state.");
            }

            return sb.ToString();
        }

        private static void AppendNode(StringBuilder sb, Transform transform, string parentPath, int depth, ref int nodeCount)
        {
            nodeCount++;

            GameObject go = transform.gameObject;
            string path = string.IsNullOrEmpty(parentPath) ? transform.name : parentPath + "/" + transform.name;

            sb.Append(' ', depth * 2);
            sb.Append(go.activeInHierarchy ? "[+] " : "[-] ");
            sb.Append(path);
            sb.Append(" activeSelf=").Append(go.activeSelf);
            sb.Append(" activeInHierarchy=").Append(go.activeInHierarchy);
            sb.Append(" sibling=").Append(transform.GetSiblingIndex());

            AppendRect(sb, transform as RectTransform);
            AppendCanvas(sb, transform.GetComponent<Canvas>());
            AppendCanvasGroup(sb, transform.GetComponent<CanvasGroup>());
            AppendGraphic(sb, transform.GetComponent<Graphic>());
            AppendButton(sb, transform.GetComponent<Button>());
            sb.AppendLine();

            int childCount = transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                AppendNode(sb, transform.GetChild(i), path, depth + 1, ref nodeCount);
            }
        }

        private static void AppendRect(StringBuilder sb, RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            sb.Append(" rect[pos=").Append(F2(rect.anchoredPosition));
            sb.Append(" size=").Append(F2(rect.sizeDelta));
            sb.Append(" anchorMin=").Append(F2(rect.anchorMin));
            sb.Append(" anchorMax=").Append(F2(rect.anchorMax));
            sb.Append(" pivot=").Append(F2(rect.pivot));
            sb.Append(" scale=").Append(F3(rect.localScale));
            sb.Append(']');
        }

        private static void AppendCanvas(StringBuilder sb, Canvas canvas)
        {
            if (canvas == null)
            {
                return;
            }

            sb.Append(" canvas[enabled=").Append(canvas.enabled);
            sb.Append(" mode=").Append(canvas.renderMode);
            sb.Append(" sortingOrder=").Append(canvas.sortingOrder);
            sb.Append(" overrideSorting=").Append(canvas.overrideSorting);
            sb.Append(']');
        }

        private static void AppendCanvasGroup(StringBuilder sb, CanvasGroup group)
        {
            if (group == null)
            {
                return;
            }

            sb.Append(" canvasGroup[alpha=").Append(F(group.alpha));
            sb.Append(" interactable=").Append(group.interactable);
            sb.Append(" blocksRaycasts=").Append(group.blocksRaycasts);
            sb.Append(" ignoreParentGroups=").Append(group.ignoreParentGroups);
            sb.Append(']');
        }

        private static void AppendGraphic(StringBuilder sb, Graphic graphic)
        {
            if (graphic == null)
            {
                return;
            }

            sb.Append(" graphic[type=").Append(graphic.GetType().Name);
            sb.Append(" enabled=").Append(graphic.enabled);
            sb.Append(" raycastTarget=").Append(graphic.raycastTarget);
            sb.Append(" color=").Append(F4(graphic.color));

            if (graphic is Image image && image.sprite != null)
            {
                sb.Append(" sprite=").Append(Sanitize(image.sprite.name));
            }
            else if (graphic is RawImage rawImage && rawImage.texture != null)
            {
                sb.Append(" texture=").Append(Sanitize(rawImage.texture.name));
            }

            string text = GetText(graphic);
            if (!string.IsNullOrEmpty(text))
            {
                sb.Append(" text=\"").Append(Truncate(Sanitize(text), 80)).Append('"');
            }

            sb.Append(']');
        }

        private static void AppendButton(StringBuilder sb, Button button)
        {
            if (button == null)
            {
                return;
            }

            sb.Append(" button[enabled=").Append(button.enabled);
            sb.Append(" interactable=").Append(button.interactable);
            sb.Append(']');
        }

        private static string GetText(Graphic graphic)
        {
            if (graphic is Text text)
            {
                return text.text;
            }

            if (graphic is TMP_Text tmpText)
            {
                return tmpText.text;
            }

            return null;
        }

        private static string[] GetLoadedSceneNames()
        {
            var names = new List<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                {
                    names.Add(scene.name);
                }
            }

            return names.ToArray();
        }

        private static string[] GetEventSystemNames()
        {
            EventSystem[] systems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var names = new string[systems.Length];
            for (int i = 0; i < systems.Length; i++)
            {
                names[i] = systems[i].name + (systems[i].isActiveAndEnabled ? "" : " (disabled)");
            }

            return names;
        }

        private static string GetSelectedGameObjectName()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null || eventSystem.currentSelectedGameObject == null)
            {
                return "<none>";
            }

            return eventSystem.currentSelectedGameObject.name;
        }

        private static string Sanitize(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            return text.Replace("\r", string.Empty).Replace("\n", "\\n").Replace("\t", " ");
        }

        private static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            {
                return text;
            }

            return text.Substring(0, maxLength) + "...";
        }

        private static string F(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string F2(Vector2 value)
        {
            return "(" + F(value.x) + "," + F(value.y) + ")";
        }

        private static string F3(Vector3 value)
        {
            return "(" + F(value.x) + "," + F(value.y) + "," + F(value.z) + ")";
        }

        private static string F4(Color value)
        {
            return "(" + F(value.r) + "," + F(value.g) + "," + F(value.b) + "," + F(value.a) + ")";
        }

        private static string BuildJson(string stamp, bool isPlaying, List<Canvas> canvases)
        {
            var trees = new List<object>(canvases.Count);
            foreach (Canvas canvas in canvases)
            {
                trees.Add(BuildNodeModel(canvas.transform, null));
            }

            var root = new Dictionary<string, object>
            {
                ["timestamp"] = stamp,
                ["isPlaying"] = isPlaying,
                ["screen"] = new[] { Screen.width, Screen.height },
                ["loadedScenes"] = GetLoadedSceneNames(),
                ["eventSystems"] = GetEventSystemNames(),
                ["selected"] = GetSelectedGameObjectName(),
                ["rootCanvasCount"] = canvases.Count,
                ["rootCanvases"] = trees,
            };

            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                Converters = { new FloatArrayConverter() },
            };
            return JsonConvert.SerializeObject(root, settings);
        }

        private static Dictionary<string, object> BuildNodeModel(Transform transform, string parentPath)
        {
            GameObject go = transform.gameObject;
            string path = string.IsNullOrEmpty(parentPath) ? transform.name : parentPath + "/" + transform.name;

            var node = new Dictionary<string, object>
            {
                ["name"] = transform.name,
                ["path"] = path,
                ["activeSelf"] = go.activeSelf,
                ["activeInHierarchy"] = go.activeInHierarchy,
                ["siblingIndex"] = transform.GetSiblingIndex(),
            };

            var rect = transform as RectTransform;
            if (rect != null)
            {
                node["rect"] = new Dictionary<string, object>
                {
                    ["anchoredPosition"] = V2(rect.anchoredPosition),
                    ["sizeDelta"] = V2(rect.sizeDelta),
                    ["anchorMin"] = V2(rect.anchorMin),
                    ["anchorMax"] = V2(rect.anchorMax),
                    ["pivot"] = V2(rect.pivot),
                    ["localScale"] = V3(rect.localScale),
                };
            }

            Canvas canvas = transform.GetComponent<Canvas>();
            if (canvas != null)
            {
                node["canvas"] = new Dictionary<string, object>
                {
                    ["enabled"] = canvas.enabled,
                    ["renderMode"] = canvas.renderMode.ToString(),
                    ["sortingOrder"] = canvas.sortingOrder,
                    ["overrideSorting"] = canvas.overrideSorting,
                };
            }

            CanvasGroup group = transform.GetComponent<CanvasGroup>();
            if (group != null)
            {
                node["canvasGroup"] = new Dictionary<string, object>
                {
                    ["alpha"] = group.alpha,
                    ["interactable"] = group.interactable,
                    ["blocksRaycasts"] = group.blocksRaycasts,
                    ["ignoreParentGroups"] = group.ignoreParentGroups,
                };
            }

            Graphic graphic = transform.GetComponent<Graphic>();
            if (graphic != null)
            {
                var g = new Dictionary<string, object>
                {
                    ["type"] = graphic.GetType().Name,
                    ["enabled"] = graphic.enabled,
                    ["raycastTarget"] = graphic.raycastTarget,
                    ["color"] = Col(graphic.color),
                };
                if (graphic is Image image && image.sprite != null)
                {
                    g["sprite"] = image.sprite.name;
                }
                else if (graphic is RawImage rawImage && rawImage.texture != null)
                {
                    g["texture"] = rawImage.texture.name;
                }

                string text = GetText(graphic);
                if (!string.IsNullOrEmpty(text))
                {
                    g["text"] = text;
                }

                node["graphic"] = g;
            }

            Button button = transform.GetComponent<Button>();
            if (button != null)
            {
                node["button"] = new Dictionary<string, object>
                {
                    ["enabled"] = button.enabled,
                    ["interactable"] = button.interactable,
                };
            }

            int childCount = transform.childCount;
            if (childCount > 0)
            {
                var children = new List<object>(childCount);
                for (int i = 0; i < childCount; i++)
                {
                    children.Add(BuildNodeModel(transform.GetChild(i), path));
                }

                node["children"] = children;
            }

            return node;
        }

        private static float[] V2(Vector2 value)
        {
            return new[] { value.x, value.y };
        }

        private static float[] V3(Vector3 value)
        {
            return new[] { value.x, value.y, value.z };
        }

        private static float[] Col(Color value)
        {
            return new[] { value.r, value.g, value.b, value.a };
        }

        /// <summary>把 float[] 序列化成单行 [a, b]，避免 Indented 模式下每个分量一行、文件过长。</summary>
        private sealed class FloatArrayConverter : JsonConverter
        {
            public override bool CanRead => false;

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(float[]);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                throw new NotSupportedException();
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var array = (float[])value;
                var sb = new StringBuilder("[");
                for (int i = 0; i < array.Length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(array[i].ToString("0.######", CultureInfo.InvariantCulture));
                }

                sb.Append(']');
                writer.WriteRawValue(sb.ToString());
            }
        }
    }
}
