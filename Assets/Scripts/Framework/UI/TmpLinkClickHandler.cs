using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Shenxiao.Framework.UI
{
    /// <summary>
    /// 把 TMP 的 &lt;link&gt; 富文本区域转换为真实指针点击。
    /// 只负责解析 link id；具体业务动作由 View 在显示时注入。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class TmpLinkClickHandler : MonoBehaviour, IPointerClickHandler
    {
        private TMP_Text _text;
        private Action<string> _onLinkClick;

        public void SetHandler(Action<string> onLinkClick)
        {
            _onLinkClick = onLinkClick;
            EnsureText();
            if (_text != null) _text.raycastTarget = true;
        }

        public void ClearHandler()
        {
            _onLinkClick = null;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            EnsureText();
            if (_text == null || eventData == null) return;

            Camera eventCamera = eventData.pressEventCamera ?? eventData.enterEventCamera;
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(_text, eventData.position, eventCamera);
            if (linkIndex < 0 || linkIndex >= _text.textInfo.linkCount) return;

            string linkId = _text.textInfo.linkInfo[linkIndex].GetLinkID();
            if (!string.IsNullOrEmpty(linkId)) _onLinkClick?.Invoke(linkId);
        }

        private void Awake()
        {
            EnsureText();
        }

        private void EnsureText()
        {
            if (_text == null) _text = GetComponent<TMP_Text>();
        }
    }
}
