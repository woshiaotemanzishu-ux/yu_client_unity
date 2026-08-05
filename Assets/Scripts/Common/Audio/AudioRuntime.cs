using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Common.Audio
{
    /// <summary>AudioManager 的生命周期与全局按钮声音绑定器；不改写任何人工 Prefab。</summary>
    public sealed class AudioRuntime : MonoBehaviour
    {
        private const float ButtonScanInterval = 0.5f;
        private float _nextButtonScan;

        private void Update()
        {
            AudioManager.Tick();
            if (Time.unscaledTime < _nextButtonScan) return;
            _nextButtonScan = Time.unscaledTime + ButtonScanInterval;
            BindActiveButtons();
        }

        private static void BindActiveButtons()
        {
            Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null || button.GetComponent<UIButtonSoundFeedback>() != null) continue;
                button.gameObject.AddComponent<UIButtonSoundFeedback>();
            }
        }

        private void OnDestroy() => AudioManager.OnRuntimeDestroy(this);
        private void OnApplicationQuit() => AudioManager.OnApplicationQuit();
    }
}
