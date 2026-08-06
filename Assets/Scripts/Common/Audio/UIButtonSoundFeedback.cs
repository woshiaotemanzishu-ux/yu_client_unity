using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Shenxiao.Common.Audio
{
    /// <summary>对标老端 Util.AddClickEvent：可交互按钮按下时播放 ui/2_dianji。</summary>
    [DisallowMultipleComponent]
    public sealed class UIButtonSoundFeedback : MonoBehaviour, IPointerDownHandler
    {
        private Button _button;

        private void Awake() => _button = GetComponent<Button>();

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_button == null || !_button.IsActive() || !_button.IsInteractable()) return;
            _ = AudioManager.PlayUi("2_dianji");
        }
    }
}
