using System;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Common
{
    public sealed class TabButtonTwoSkin : TabButtonTwoSkinBind
    {
        private static readonly Color SelectedColor = new Color(1f, 0.78f, 0.28f, 1f);
        private static readonly Color UnselectedColor = new Color(0.12f, 0.42f, 0.34f, 0.92f);
        private static readonly Color DisabledColor = new Color(0.42f, 0.42f, 0.42f, 1f);
        private static readonly Color SelectedTextColor = Color.white;
        private static readonly Color UnselectedTextColor = Color.white;
        private const string DefaultTextTabUp = "resource/game/common/texture/uian_001b.png";
        private const string DefaultTextTabDown = "resource/game/common/texture/uian_001a2.png";

        private int _index;
        private Action<int> _onClick;
        private string _label;
        private string _upImagePath;
        private string _downImagePath;
        private Sprite _up;
        private Sprite _down;
        private bool _enabled = true;
        private int _imageRequestId;
        private TextMeshProUGUI _labelText;

        public void SetData(
            int index,
            Action<int> onClick,
            string label = null,
            string upImagePath = null,
            string downImagePath = null,
            Sprite up = null,
            Sprite down = null)
        {
            _index = index;
            _onClick = onClick;
            _label = label;
            _upImagePath = upImagePath;
            _downImagePath = downImagePath;
            _up = up;
            _down = down;

            EnsureGeometry();
            EnsureLabel();

            if (redDisplay != null) redDisplay.gameObject.SetActive(false);
            if (_Image1 != null)
            {
                _Image1.enabled = true;
                _Image1.raycastTarget = true;
                UIUtil.AddClick(_Image1, OnClick);
            }
            SetSelected(false);
        }

        private void OnClick()
        {
            if (!_enabled) return;
            _onClick?.Invoke(_index);
        }

        public void SetSelected(bool selected)
        {
            if (_Image1 == null) return;

            if (_up != null && _down != null)
            {
                _Image1.enabled = true;
                _Image1.sprite = selected ? _down : _up;
                _Image1.color = _enabled ? Color.white : DisabledColor;
            }
            else if (!string.IsNullOrEmpty(_upImagePath) || !string.IsNullOrEmpty(_downImagePath))
            {
                string imagePath = selected && !string.IsNullOrEmpty(_downImagePath)
                    ? _downImagePath
                    : _upImagePath;
                _Image1.enabled = true;
                _Image1.color = _enabled ? Color.white : DisabledColor;
                if (!string.IsNullOrEmpty(imagePath)) _ = ApplyImageAsync(imagePath, ++_imageRequestId, selected);
            }
            else if (UsesDefaultTextSkin())
            {
                string imagePath = selected ? DefaultTextTabDown : DefaultTextTabUp;
                _Image1.enabled = true;
                _Image1.color = _enabled ? Color.white : DisabledColor;
                _ = ApplyImageAsync(imagePath, ++_imageRequestId, selected);
            }
            else
            {
                ApplyColorFallback(selected);
            }

            ApplyLabelVisual(selected);
        }

        public void SetRed(bool on)
        {
            if (redDisplay != null) redDisplay.gameObject.SetActive(on);
        }

        public void SetEnabledVisual(bool enabled)
        {
            _enabled = enabled;
            SetSelected(false);
        }

        private async Task ApplyImageAsync(string imagePath, int requestId, bool selected)
        {
            Sprite sprite = await ResManager.LoadAsync<Sprite>(imagePath);
            if (requestId != _imageRequestId || _Image1 == null) return;
            if (sprite == null)
            {
                ApplyColorFallback(selected);
                return;
            }

            _Image1.sprite = sprite;
            _Image1.enabled = true;
            _Image1.color = _enabled ? Color.white : DisabledColor;
        }

        private bool UsesDefaultTextSkin()
        {
            return !string.IsNullOrEmpty(_label)
                   && string.IsNullOrEmpty(_upImagePath)
                   && string.IsNullOrEmpty(_downImagePath)
                   && _up == null
                   && _down == null;
        }

        private void ApplyColorFallback(bool selected)
        {
            _Image1.enabled = true;
            _Image1.sprite = null;
            _Image1.color = !_enabled ? DisabledColor : (selected ? SelectedColor : UnselectedColor);
        }

        private void EnsureGeometry()
        {
            RectTransform rt = transform as RectTransform;
            if (rt != null)
            {
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 150f);
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 90f);
            }

            if (_Image1 == null) return;

            RectTransform img = _Image1.rectTransform;
            img.anchorMin = new Vector2(0f, 1f);
            img.anchorMax = new Vector2(0f, 1f);
            img.pivot = new Vector2(0f, 1f);
            img.anchoredPosition = new Vector2(0f, -8f);
            img.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 140f);
            img.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 62f);
        }

        private void EnsureLabel()
        {
            if (_labelText != null) return;

            GameObject go = new GameObject(
                "labelDisplay",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);

            RectTransform rt = go.transform as RectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(0f, -10f);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 140f);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 58f);

            _labelText = go.GetComponent<TextMeshProUGUI>();
            _labelText.alignment = TextAlignmentOptions.Center;
            _labelText.fontSize = 28f;
            _labelText.enableAutoSizing = true;
            _labelText.fontSizeMin = 18f;
            _labelText.fontSizeMax = 30f;
            _labelText.raycastTarget = false;
            CopyFont(_labelText);
        }

        private void ApplyLabelVisual(bool selected)
        {
            if (_labelText == null) return;

            bool useLabel = UsesDefaultTextSkin();
            _labelText.gameObject.SetActive(useLabel);
            if (!useLabel) return;

            _labelText.text = _label;
            _labelText.color = !_enabled ? DisabledColor : (selected ? SelectedTextColor : UnselectedTextColor);
        }

        private static void CopyFont(TextMeshProUGUI target)
        {
            TextMeshProUGUI[] texts = UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < texts.Length; i++)
            {
                TextMeshProUGUI source = texts[i];
                if (source == null || source == target || source.font == null) continue;
                target.font = source.font;
                target.fontSharedMaterial = source.fontSharedMaterial;
                return;
            }
        }
    }
}
