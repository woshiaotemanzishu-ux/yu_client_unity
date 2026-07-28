using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Task;
using Shenxiao.Module.Core.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 任务完成弹层真实点击链专项：面板内外任意位置都必须进入领取/提交语义，不能只执行纯关闭。
    /// 测试不注入 TaskVo，生产 OnSubmit 会走空任务保护并关闭弹层，以此证明 PointerClick 已进入同一入口。
    /// </summary>
    public static class TaskFinishInteractionCase
    {
        private const string PrefabPath = "Assets/Prefabs/UI/Task/TaskModule.prefab";

        public static async Task<int> Run()
        {
            GameObject canvasGo = null;
            GameObject cameraGo = null;
            GameObject eventSystemGo = null;
            GameObject instance = null;
            RenderTexture warmupTexture = null;
            try
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                if (prefab == null)
                {
                    Debug.LogError("CLIVERIFY task-finish-interaction prefab missing: " + PrefabPath);
                    return 3;
                }

                canvasGo = new GameObject("TaskFinishInteractionCase_Canvas", typeof(RectTransform),
                    typeof(Canvas), typeof(GraphicRaycaster));
                RectTransform canvasRt = (RectTransform)canvasGo.transform;
                canvasRt.sizeDelta = new Vector2(720f, 1600f);
                Canvas canvas = canvasGo.GetComponent<Canvas>();
                cameraGo = new GameObject("TaskFinishInteractionCase_Camera", typeof(Camera));
                Camera camera = cameraGo.GetComponent<Camera>();
                camera.transform.position = new Vector3(0f, 0f, -10f);
                camera.orthographic = true;
                camera.orthographicSize = 800f;
                camera.pixelRect = new Rect(0f, 0f, Screen.width, Screen.height);
                camera.aspect = 720f / 1600f;
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = camera;
                GraphicRaycaster raycaster = canvasGo.GetComponent<GraphicRaycaster>();
                raycaster.ignoreReversedGraphics = false;

                eventSystemGo = new GameObject("TaskFinishInteractionCase_EventSystem", typeof(EventSystem));
                EventSystem eventSystem = eventSystemGo.GetComponent<EventSystem>();

                instance = PrefabUtility.InstantiatePrefab(prefab, canvasGo.transform) as GameObject;
                TaskFinishViewBind bind = instance != null
                    ? instance.GetComponentInChildren<TaskFinishViewBind>(true)
                    : null;
                if (bind == null)
                {
                    Debug.LogError("CLIVERIFY task-finish-interaction TaskFinishViewBind missing");
                    return 3;
                }

                foreach (BaseView view in instance.GetComponentsInChildren<BaseView>(true))
                    view.gameObject.SetActive(false);
                instance.SetActive(true);
                bind.gameObject.SetActive(true);

                var runtime = new TaskFinishView();
                Type type = typeof(TaskFinishView);
                type.GetField("_moduleRoot", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(runtime, instance);
                type.GetField("_bind", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(runtime, bind);
                MethodInfo configure = type.GetMethod("ConfigureUniversalSubmitSurface",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                FieldInfo submitSent = type.GetField("_submitSent", BindingFlags.NonPublic | BindingFlags.Instance);
                if (configure == null || submitSent == null)
                {
                    Debug.LogError("CLIVERIFY task-finish-interaction production click members missing");
                    return 3;
                }
                configure.Invoke(runtime, null);
                canvas.enabled = false;
                canvas.enabled = true;
                Canvas.ForceUpdateCanvases();
                warmupTexture = new RenderTexture(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height), 0);
                camera.targetTexture = warmupTexture;
                camera.Render();
                camera.targetTexture = null;
                camera.pixelRect = new Rect(0f, 0f, Screen.width, Screen.height);
                camera.aspect = 720f / 1600f;

                Graphic moduleGraphic = instance.GetComponent<Graphic>();
                Graphic viewGraphic = bind.GetComponent<Graphic>();
                bool surfacesOk = moduleGraphic != null && moduleGraphic.raycastTarget
                    && viewGraphic != null && viewGraphic.raycastTarget
                    && instance.GetComponent<Button>() != null && bind.GetComponent<Button>() != null;
                foreach (Graphic graphic in instance.GetComponentsInChildren<Graphic>(true))
                {
                    if (graphic != moduleGraphic && graphic != viewGraphic && graphic.raycastTarget)
                        surfacesOk = false;
                }
                if (!surfacesOk)
                {
                    Debug.LogError("CLIVERIFY task-finish-interaction click surfaces/raycast ownership mismatch");
                    return 3;
                }

                Canvas.ForceUpdateCanvases();
                RectTransform moduleRt = (RectTransform)instance.transform;
                Vector3[] points =
                {
                    moduleRt.TransformPoint(moduleRt.rect.center),
                    moduleRt.TransformPoint(new Vector3(moduleRt.rect.xMin + 30f, moduleRt.rect.yMax - 30f, 0f)),
                };
                int enteredSubmit = 0;
                for (int i = 0; i < points.Length; i++)
                {
                    instance.SetActive(true);
                    bind.gameObject.SetActive(true);
                    submitSent.SetValue(runtime, false);
                    Canvas.ForceUpdateCanvases();

                    var pointer = new PointerEventData(eventSystem)
                    {
                        button = PointerEventData.InputButton.Left,
                        position = RectTransformUtility.WorldToScreenPoint(camera, points[i]),
                    };
                    var hits = new List<RaycastResult>();
                    raycaster.Raycast(pointer, hits);
                    RaycastResult? semanticHit = null;
                    for (int hitIndex = 0; hitIndex < hits.Count; hitIndex++)
                    {
                        GameObject hitObject = hits[hitIndex].gameObject;
                        if (hitObject == instance || hitObject == bind.gameObject)
                        {
                            semanticHit = hits[hitIndex];
                            break;
                        }
                    }
                    if (!semanticHit.HasValue)
                    {
                        var hitNames = new List<string>();
                        for (int hitIndex = 0; hitIndex < hits.Count; hitIndex++)
                            hitNames.Add(hits[hitIndex].gameObject != null ? hits[hitIndex].gameObject.name : "<null>");
                        Debug.LogError("CLIVERIFY task-finish-interaction point " + i
                            + " missed semantic surface; screen=" + pointer.position
                            + " screenSize=" + Screen.width + "x" + Screen.height
                            + " cameraRect=" + camera.pixelRect
                            + " moduleRect=" + moduleRt.rect
                            + " hits=[" + string.Join(",", hitNames) + "]");
                        return 3;
                    }

                    ExecuteEvents.ExecuteHierarchy<IPointerClickHandler>(semanticHit.Value.gameObject,
                        pointer, ExecuteEvents.pointerClickHandler);
                    if (!instance.activeSelf) enteredSubmit++;
                }

                bool pass = enteredSubmit == points.Length;
                Debug.Log("CLIVERIFY task-finish-interaction VERDICT surfaces=" + surfacesOk
                    + " pointerSubmits=" + enteredSubmit + "/" + points.Length + " pass=" + pass);
                await Task.CompletedTask;
                return pass ? 0 : 3;
            }
            catch (Exception ex)
            {
                Debug.LogError("CLIVERIFY task-finish-interaction exception: " + ex);
                return 1;
            }
            finally
            {
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
                if (eventSystemGo != null) UnityEngine.Object.DestroyImmediate(eventSystemGo);
                if (cameraGo != null) UnityEngine.Object.DestroyImmediate(cameraGo);
                if (warmupTexture != null) UnityEngine.Object.DestroyImmediate(warmupTexture);
                if (canvasGo != null) UnityEngine.Object.DestroyImmediate(canvasGo);
            }
        }
    }
}
