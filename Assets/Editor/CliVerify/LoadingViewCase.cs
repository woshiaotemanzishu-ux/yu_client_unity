using System.Threading.Tasks;
using Shenxiao.Module.Core.Login;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// LoadingView 的 Prefab 驱动回归：端标由 Slider handle 跟随实际轨道宽度，
    /// SetProgress 不得覆盖 Prefab 中配置的 ProgressEnd 偏移。
    /// </summary>
    public static class LoadingViewCase
    {
        private const string PrefabPath = "Assets/Prefabs/UI/Login/LoadingView.prefab";
        private const string RemovedCreatorPath = "Assets/Editor/UiCreator/Login/LoadingCreator.cs";

        public static Task<int> Run()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject instance = prefab != null ? Object.Instantiate(prefab) : null;
            try
            {
                LoadingView view = instance != null ? instance.GetComponent<LoadingView>() : null;
                RectTransform handle = view != null && view.progressSlider != null
                    ? view.progressSlider.handleRect
                    : null;
                RectTransform track = handle != null ? handle.parent as RectTransform : null;
                bool bindingOk = view != null
                    && view.progressFront != null
                    && view.progressSlider != null
                    && view.progressEnd != null
                    && handle != null
                    && handle.name == "ProgressEndHandle"
                    && track != null
                    && track.name == "ProgressEndTrack"
                    && view.progressEnd.transform.parent == handle
                    && view.progressSlider.fillRect == null
                    && !view.progressSlider.interactable
                    && view.progressSlider.direction == Slider.Direction.LeftToRight
                    && Near(track.rect.height, 0f);

                if (!bindingOk)
                {
                    Debug.Log("CLIVERIFY loadingview binding=False pass=False");
                    return Task.FromResult(3);
                }

                RectTransform front = view.progressFront.rectTransform;
                RectTransform end = view.progressEnd.rectTransform;
                Vector2 configuredPosition = end.anchoredPosition;
                Vector2 configuredSize = end.rect.size;
                Vector2 configuredAnchorMin = end.anchorMin;
                Vector2 configuredAnchorMax = end.anchorMax;

                bool zeroOk = VerifyProgress(view, handle, 0f, 0f, false,
                    configuredPosition, configuredSize, configuredAnchorMin, configuredAnchorMax);
                bool quarterOk = VerifyProgress(view, handle, 0.25f, 0.25f, true,
                    configuredPosition, configuredSize, configuredAnchorMin, configuredAnchorMax);
                bool halfOk = VerifyProgress(view, handle, 0.5f, 0.5f, true,
                    configuredPosition, configuredSize, configuredAnchorMin, configuredAnchorMax);
                bool fullOk = VerifyProgress(view, handle, 1f, 1f, true,
                    configuredPosition, configuredSize, configuredAnchorMin, configuredAnchorMax);
                bool clampOk = VerifyProgress(view, handle, 2f, 1f, true,
                    configuredPosition, configuredSize, configuredAnchorMin, configuredAnchorMax);

                float originalWidth = front.rect.width;
                front.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, originalWidth + 65f);
                bool resizedOk = VerifyProgress(view, handle, 0.5f, 0.5f, true,
                    configuredPosition, configuredSize, configuredAnchorMin, configuredAnchorMax);
                float expectedCenter = track.rect.xMin + track.rect.width * 0.5f;
                resizedOk &= Near(handle.localPosition.x, expectedCenter);

                bool creatorRemoved = AssetDatabase.LoadAssetAtPath<MonoScript>(RemovedCreatorPath) == null;
                bool pass = bindingOk && zeroOk && quarterOk && halfOk && fullOk && clampOk
                    && resizedOk && creatorRemoved;
                Debug.Log("CLIVERIFY loadingview binding=" + bindingOk
                    + " zero=" + zeroOk + " quarter=" + quarterOk + " half=" + halfOk
                    + " full=" + fullOk + " clamp=" + clampOk + " resized=" + resizedOk
                    + " creatorRemoved=" + creatorRemoved + " position=" + configuredPosition
                    + " size=" + configuredSize
                    + " pass=" + pass);
                return Task.FromResult(pass ? 0 : 3);
            }
            finally
            {
                if (instance != null) Object.DestroyImmediate(instance);
            }
        }

        private static bool VerifyProgress(
            LoadingView view,
            RectTransform handle,
            float input,
            float expected,
            bool expectedVisible,
            Vector2 configuredPosition,
            Vector2 configuredSize,
            Vector2 configuredAnchorMin,
            Vector2 configuredAnchorMax)
        {
            view.SetProgress(input);
            Canvas.ForceUpdateCanvases();

            RectTransform end = view.progressEnd.rectTransform;
            return Near(view.progressFront.fillAmount, expected)
                && Near(view.progressSlider.value, expected)
                && view.progressEnd.gameObject.activeSelf == expectedVisible
                && Near(handle.anchorMin.x, expected)
                && Near(handle.anchorMax.x, expected)
                && Near(end.anchoredPosition, configuredPosition)
                && Near(end.rect.size, configuredSize)
                && Near(end.anchorMin, configuredAnchorMin)
                && Near(end.anchorMax, configuredAnchorMax);
        }

        private static bool Near(Vector2 a, Vector2 b) =>
            Near(a.x, b.x) && Near(a.y, b.y);

        private static bool Near(float a, float b) => Mathf.Abs(a - b) < 0.01f;
    }
}
