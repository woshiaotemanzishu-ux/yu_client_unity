using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Generated.UI.Dialogue;
using Shenxiao.Module.Core.Dialogue;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// NPC 对话全面屏锚定与真实点击链专项：720×1600 下底栏仍贴底，背景/模型铺满，
    /// 任意位置均经 GraphicRaycaster → PointerClick 进入生产代码的统一语义点击面。
    /// </summary>
    public static class DialogueInteractionCase
    {
        private const string PrefabPath = "Assets/Prefabs/UI/Dialogue/DialogueModule.prefab";

        public static async Task<int> Run()
        {
            GameObject canvasGo = null;
            GameObject cameraGo = null;
            GameObject eventSystemGo = null;
            GameObject instance = null;
            RenderTexture renderTexture = null;
            try
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                if (prefab == null)
                {
                    Debug.LogError("CLIVERIFY dialogue-interaction prefab missing: " + PrefabPath);
                    return 3;
                }

                canvasGo = new GameObject("DialogueInteractionCase_Canvas", typeof(RectTransform),
                    typeof(Canvas), typeof(GraphicRaycaster));
                RectTransform canvasRt = (RectTransform)canvasGo.transform;
                canvasRt.sizeDelta = new Vector2(720f, 1600f);
                Canvas canvas = canvasGo.GetComponent<Canvas>();
                cameraGo = new GameObject("DialogueInteractionCase_Camera", typeof(Camera));
                Camera camera = cameraGo.GetComponent<Camera>();
                camera.transform.position = new Vector3(0f, 0f, -10f);
                camera.orthographic = true;
                camera.orthographicSize = 800f;
                renderTexture = new RenderTexture(720, 1600, 0);
                camera.targetTexture = renderTexture;
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = camera;
                GraphicRaycaster raycaster = canvasGo.GetComponent<GraphicRaycaster>();
                raycaster.ignoreReversedGraphics = false;

                eventSystemGo = new GameObject("DialogueInteractionCase_EventSystem", typeof(EventSystem));
                EventSystem eventSystem = eventSystemGo.GetComponent<EventSystem>();

                instance = PrefabUtility.InstantiatePrefab(prefab, canvasGo.transform) as GameObject;
                DialogueViewBind bind = instance != null
                    ? instance.GetComponentInChildren<DialogueViewBind>(true)
                    : null;
                if (bind == null)
                {
                    Debug.LogError("CLIVERIFY dialogue-interaction DialogueViewBind missing");
                    return 3;
                }
                instance.SetActive(true);
                bind.gameObject.SetActive(true);
                Canvas.ForceUpdateCanvases();

                RectTransform viewRt = (RectTransform)bind.transform;
                bool stretchOk = viewRt.anchorMin == Vector2.zero && viewRt.anchorMax == Vector2.one
                    && viewRt.sizeDelta.sqrMagnitude < 0.001f
                    && Mathf.Abs(viewRt.rect.width - 720f) < 0.1f
                    && Mathf.Abs(viewRt.rect.height - 1600f) < 0.1f;
                bool backgroundOk = SameRect(bind._img_bg.rectTransform, viewRt);
                bool modelOk = SameRect(bind._box_model, viewRt);
                bool bottomOk = bind._box_bottom.anchorMin.y == 0f && bind._box_bottom.anchorMax.y == 0f
                    && Mathf.Abs(WorldBottom(bind._box_bottom) - WorldBottom(viewRt)) < 0.01f;
                if (!stretchOk || !backgroundOk || !modelOk || !bottomOk)
                {
                    Debug.LogError("CLIVERIFY dialogue-interaction anchors mismatch: stretch=" + stretchOk
                        + " background=" + backgroundOk + " model=" + modelOk + " bottom=" + bottomOk);
                    return 3;
                }

                var runtime = new DialogueView();
                Type type = typeof(DialogueView);
                type.GetField("_moduleRoot", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(runtime, instance);
                type.GetField("_bind", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(runtime, bind);
                MethodInfo configure = type.GetMethod("ConfigureUniversalClickSurface",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                FieldInfo actionField = type.GetField("_currentClickAction",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                FieldInfo consumedField = type.GetField("_actionConsumed",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (configure == null || actionField == null || consumedField == null)
                {
                    Debug.LogError("CLIVERIFY dialogue-interaction production click members missing");
                    return 3;
                }
                configure.Invoke(runtime, null);

                Graphic rootGraphic = instance.GetComponent<Graphic>();
                Button rootButton = instance.GetComponent<Button>();
                bool decorationsIgnoreRaycast = rootGraphic != null && rootGraphic.raycastTarget && rootButton != null;
                foreach (Graphic graphic in instance.GetComponentsInChildren<Graphic>(true))
                {
                    if (graphic != rootGraphic && graphic.raycastTarget) decorationsIgnoreRaycast = false;
                }
                if (!decorationsIgnoreRaycast)
                {
                    Debug.LogError("CLIVERIFY dialogue-interaction child Graphic still intercepts raycast");
                    return 3;
                }

                int clickCount = 0;
                Vector3[] points =
                {
                    viewRt.TransformPoint(viewRt.rect.center),
                    bind._box_bottom.TransformPoint(bind._box_bottom.rect.center),
                    viewRt.TransformPoint(new Vector3(viewRt.rect.xMin + 30f, viewRt.rect.yMax - 30f, 0f)),
                };
                for (int i = 0; i < points.Length; i++)
                {
                    actionField.SetValue(runtime, (Action)(() => clickCount++));
                    consumedField.SetValue(runtime, false);
                    var pointer = new PointerEventData(eventSystem)
                    {
                        button = PointerEventData.InputButton.Left,
                        position = RectTransformUtility.WorldToScreenPoint(camera, points[i]),
                    };
                    var hits = new List<RaycastResult>();
                    raycaster.Raycast(pointer, hits);
                    RaycastResult? hit = null;
                    for (int hitIndex = 0; hitIndex < hits.Count; hitIndex++)
                    {
                        if (hits[hitIndex].gameObject == instance)
                        {
                            hit = hits[hitIndex];
                            break;
                        }
                    }
                    if (!hit.HasValue)
                    {
                        Debug.LogError("CLIVERIFY dialogue-interaction point " + i + " did not hit module root");
                        return 3;
                    }
                    ExecuteEvents.ExecuteHierarchy<IPointerClickHandler>(hit.Value.gameObject,
                        pointer, ExecuteEvents.pointerClickHandler);
                }

                bool pass = clickCount == points.Length;
                Debug.Log("CLIVERIFY dialogue-interaction VERDICT stretch=" + stretchOk
                    + " background=" + backgroundOk + " model=" + modelOk + " bottom=" + bottomOk
                    + " pointerClicks=" + clickCount + "/" + points.Length + " pass=" + pass);
                await Task.CompletedTask;
                return pass ? 0 : 3;
            }
            catch (Exception ex)
            {
                Debug.LogError("CLIVERIFY dialogue-interaction exception: " + ex);
                return 1;
            }
            finally
            {
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
                if (eventSystemGo != null) UnityEngine.Object.DestroyImmediate(eventSystemGo);
                if (cameraGo != null) UnityEngine.Object.DestroyImmediate(cameraGo);
                if (renderTexture != null) UnityEngine.Object.DestroyImmediate(renderTexture);
                if (canvasGo != null) UnityEngine.Object.DestroyImmediate(canvasGo);
            }
        }

        private static bool SameRect(RectTransform a, RectTransform b)
        {
            return a != null && b != null
                && Mathf.Abs(a.rect.width - b.rect.width) < 0.1f
                && Mathf.Abs(a.rect.height - b.rect.height) < 0.1f
                && (a.position - b.position).sqrMagnitude < 0.001f;
        }

        private static float WorldBottom(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            return Mathf.Min(corners[0].y, corners[3].y);
        }
    }
}
