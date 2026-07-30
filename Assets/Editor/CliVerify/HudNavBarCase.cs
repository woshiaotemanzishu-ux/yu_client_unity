using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Module.Core.MainUI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.EditorTools
{
    /// <summary>HudNavBar 经验条结构回归：进度变化不能改写图片和真实特效挂点的 Prefab 布局。</summary>
    public static class HudNavBarCase
    {
        private const string PrefabPath = "Assets/Prefabs/UI/MainUI/Regions/HudNavBar.prefab";
        private const string RemovedCreatorPath = "Assets/Editor/UiCreator/MainUI/HudNavBarCreator.cs";

        public static Task<int> Run()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject instance = prefab != null
                ? PrefabUtility.InstantiatePrefab(prefab) as GameObject
                : null;

            try
            {
                MainUIDownView view = instance != null
                    ? instance.GetComponentInChildren<MainUIDownView>(true)
                    : null;
                Image fill = view != null ? view._img_exp : null;
                Slider slider = fill != null ? fill.GetComponent<Slider>() : null;
                RectTransform handle = slider != null ? slider.handleRect : null;
                RectTransform track = handle != null ? handle.parent as RectTransform : null;
                RectTransform slot = view != null ? view._box_exp_effect : null;

                bool bindingOk = view != null
                    && fill != null
                    && slider != null
                    && handle != null
                    && handle.name == "ExpBarEffectHandle"
                    && track != null
                    && track.name == "ExpBarEffectTrack"
                    && slot != null
                    && slot.parent == handle
                    && fill.type == Image.Type.Filled
                    && fill.fillMethod == Image.FillMethod.Horizontal
                    && fill.fillOrigin == (int)Image.OriginHorizontal.Left
                    && slider.fillRect == null
                    && !slider.interactable
                    && slider.direction == Slider.Direction.LeftToRight
                    && Near(track.rect.height, 0f);
                if (!bindingOk)
                {
                    Debug.LogError("CLIVERIFY hudnavbar binding=False");
                    return Task.FromResult(3);
                }

                MethodInfo setProgress = typeof(MainUIDownView).GetMethod(
                    "SetExpProgress", BindingFlags.Instance | BindingFlags.NonPublic);
                if (setProgress == null)
                {
                    Debug.LogError("CLIVERIFY hudnavbar SetExpProgress missing");
                    return Task.FromResult(3);
                }

                RectTransform fillRect = fill.rectTransform;
                Vector2 fillPosition = fillRect.anchoredPosition;
                Vector2 fillSize = fillRect.rect.size;
                Vector2 fillAnchorMin = fillRect.anchorMin;
                Vector2 fillAnchorMax = fillRect.anchorMax;
                Vector2 slotPosition = slot.anchoredPosition;
                Vector2 slotSize = slot.rect.size;
                Vector2 slotAnchorMin = slot.anchorMin;
                Vector2 slotAnchorMax = slot.anchorMax;

                bool zeroOk = VerifyProgress(view, setProgress, slider, handle, 0f, 0f,
                    fillPosition, fillSize, fillAnchorMin, fillAnchorMax,
                    slotPosition, slotSize, slotAnchorMin, slotAnchorMax);
                bool quarterOk = VerifyProgress(view, setProgress, slider, handle, 0.25f, 0.25f,
                    fillPosition, fillSize, fillAnchorMin, fillAnchorMax,
                    slotPosition, slotSize, slotAnchorMin, slotAnchorMax);
                bool halfOk = VerifyProgress(view, setProgress, slider, handle, 0.5f, 0.5f,
                    fillPosition, fillSize, fillAnchorMin, fillAnchorMax,
                    slotPosition, slotSize, slotAnchorMin, slotAnchorMax);
                bool fullOk = VerifyProgress(view, setProgress, slider, handle, 1f, 1f,
                    fillPosition, fillSize, fillAnchorMin, fillAnchorMax,
                    slotPosition, slotSize, slotAnchorMin, slotAnchorMax);
                bool clampOk = VerifyProgress(view, setProgress, slider, handle, 2f, 1f,
                    fillPosition, fillSize, fillAnchorMin, fillAnchorMax,
                    slotPosition, slotSize, slotAnchorMin, slotAnchorMax);

                float originalWidth = fillRect.rect.width;
                fillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, originalWidth + 65f);
                setProgress.Invoke(view, new object[] { 0.5f });
                Canvas.ForceUpdateCanvases();
                float expectedCenter = track.rect.xMin + track.rect.width * 0.5f;
                bool resizedOk = Near(handle.localPosition.x, expectedCenter)
                    && Near(slot.anchoredPosition, slotPosition)
                    && Near(slot.rect.size, slotSize);

                bool creatorRemoved = AssetDatabase.LoadAssetAtPath<MonoScript>(RemovedCreatorPath) == null;
                bool pass = bindingOk && zeroOk && quarterOk && halfOk && fullOk && clampOk
                    && resizedOk && creatorRemoved;
                Debug.Log("CLIVERIFY hudnavbar binding=" + bindingOk
                    + " zero=" + zeroOk + " quarter=" + quarterOk + " half=" + halfOk
                    + " full=" + fullOk + " clamp=" + clampOk + " resized=" + resizedOk
                    + " creatorRemoved=" + creatorRemoved + " fillSize=" + fillSize
                    + " slotPosition=" + slotPosition + " slotSize=" + slotSize
                    + " pass=" + pass);
                return Task.FromResult(pass ? 0 : 3);
            }
            finally
            {
                if (instance != null) Object.DestroyImmediate(instance);
            }
        }

        private static bool VerifyProgress(
            MainUIDownView view,
            MethodInfo setProgress,
            Slider slider,
            RectTransform handle,
            float input,
            float expected,
            Vector2 fillPosition,
            Vector2 fillSize,
            Vector2 fillAnchorMin,
            Vector2 fillAnchorMax,
            Vector2 slotPosition,
            Vector2 slotSize,
            Vector2 slotAnchorMin,
            Vector2 slotAnchorMax)
        {
            setProgress.Invoke(view, new object[] { input });
            Canvas.ForceUpdateCanvases();

            RectTransform fill = view._img_exp.rectTransform;
            RectTransform slot = view._box_exp_effect;
            return Near(view._img_exp.fillAmount, expected)
                && Near(slider.value, expected)
                && Near(handle.anchorMin.x, expected)
                && Near(handle.anchorMax.x, expected)
                && Near(fill.anchoredPosition, fillPosition)
                && Near(fill.rect.size, fillSize)
                && Near(fill.anchorMin, fillAnchorMin)
                && Near(fill.anchorMax, fillAnchorMax)
                && Near(slot.anchoredPosition, slotPosition)
                && Near(slot.rect.size, slotSize)
                && Near(slot.anchorMin, slotAnchorMin)
                && Near(slot.anchorMax, slotAnchorMax);
        }

        private static bool Near(Vector2 a, Vector2 b) =>
            Near(a.x, b.x) && Near(a.y, b.y);

        private static bool Near(float a, float b) => Mathf.Abs(a - b) < 0.01f;
    }
}
