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
                bool bindingOk = view != null
                    && view.progressFront != null
                    && view.progressSlider != null
                    && view.progressEnd != null
                    && view.progressSlider.handleRect == view.progressEnd.rectTransform
                    && view.progressSlider.fillRect == null
                    && !view.progressSlider.interactable
                    && view.progressSlider.direction == Slider.Direction.LeftToRight;

                if (!bindingOk)
                {
                    Debug.Log("CLIVERIFY loadingview binding=False pass=False");
                    return Task.FromResult(3);
                }

                RectTransform front = view.progressFront.rectTransform;
                RectTransform end = view.progressEnd.rectTransform;
                float configuredOffset = end.anchoredPosition.x;

                bool zeroOk = VerifyProgress(view, 0f, 0f, false, configuredOffset);
                bool quarterOk = VerifyProgress(view, 0.25f, 0.25f, true, configuredOffset);
                bool halfOk = VerifyProgress(view, 0.5f, 0.5f, true, configuredOffset);
                bool fullOk = VerifyProgress(view, 1f, 1f, true, configuredOffset);
                bool clampOk = VerifyProgress(view, 2f, 1f, true, configuredOffset);

                float originalWidth = front.rect.width;
                front.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, originalWidth + 65f);
                bool resizedOk = VerifyProgress(view, 0.5f, 0.5f, true, configuredOffset);
                float expectedCenter = front.rect.xMin + front.rect.width * 0.5f + configuredOffset;
                resizedOk &= Near(end.localPosition.x, expectedCenter);

                bool creatorRemoved = AssetDatabase.LoadAssetAtPath<MonoScript>(RemovedCreatorPath) == null;
                bool pass = bindingOk && zeroOk && quarterOk && halfOk && fullOk && clampOk
                    && resizedOk && creatorRemoved;
                Debug.Log("CLIVERIFY loadingview binding=" + bindingOk
                    + " zero=" + zeroOk + " quarter=" + quarterOk + " half=" + halfOk
                    + " full=" + fullOk + " clamp=" + clampOk + " resized=" + resizedOk
                    + " creatorRemoved=" + creatorRemoved + " offset=" + configuredOffset
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
            float input,
            float expected,
            bool expectedVisible,
            float configuredOffset)
        {
            view.SetProgress(input);
            Canvas.ForceUpdateCanvases();

            RectTransform end = view.progressEnd.rectTransform;
            return Near(view.progressFront.fillAmount, expected)
                && Near(view.progressSlider.value, expected)
                && view.progressEnd.gameObject.activeSelf == expectedVisible
                && Near(end.anchorMin.x, expected)
                && Near(end.anchorMax.x, expected)
                && Near(end.anchoredPosition.x, configuredOffset);
        }

        private static bool Near(float a, float b) => Mathf.Abs(a - b) < 0.01f;
    }
}
