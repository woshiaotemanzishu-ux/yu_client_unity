using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// Runtime fallback for MainUI entries whose target module has not been ported yet.
    /// This keeps HUD buttons clickable without editing generated prefabs.
    /// </summary>
    public static class MainUIRoutePlaceholder
    {
        private static GameObject _root;
        private static TextMeshProUGUI _title;
        private static TextMeshProUGUI _body;

        public static void Show(string viewKey)
        {
            EnsureCreated();
            if (_root == null)
            {
                GameLog.Info("MainUI", "fallback route clicked: {0}", viewKey);
                return;
            }

            _title.text = "Coming Soon";
            _body.text = "Placeholder opened for MainUI route:\n" + viewKey;
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }

        public static void Hide()
        {
            if (_root != null) _root.SetActive(false);
        }

        private static void EnsureCreated()
        {
            if (_root != null) return;

            Transform parent = ViewManager.GetLayer(UILayer.Window);
            if (parent == null) return;

            _root = NewRect("MainUIRoutePlaceholder", parent);
            RectTransform rootRt = (RectTransform)_root.transform;
            Stretch(rootRt);
            Image shade = _root.AddComponent<Image>();
            shade.color = new Color(0f, 0f, 0f, 0.45f);
            UIUtil.AddClick(shade, Hide);

            GameObject panel = NewRect("Panel", _root.transform);
            RectTransform panelRt = (RectTransform)panel.transform;
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(420f, 230f);
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.06f, 0.08f, 0.10f, 0.92f);

            _title = NewText("Title", panel.transform, 34f, TextAlignmentOptions.Center);
            RectTransform titleRt = (RectTransform)_title.transform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -28f);
            titleRt.sizeDelta = new Vector2(-40f, 48f);
            _title.color = new Color(1f, 0.86f, 0.46f, 1f);

            _body = NewText("Body", panel.transform, 24f, TextAlignmentOptions.Center);
            RectTransform bodyRt = (RectTransform)_body.transform;
            bodyRt.anchorMin = new Vector2(0f, 0f);
            bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.pivot = new Vector2(0.5f, 0.5f);
            bodyRt.anchoredPosition = new Vector2(0f, -16f);
            bodyRt.sizeDelta = new Vector2(-48f, -110f);
            _body.color = Color.white;

            GameObject close = NewRect("Close", panel.transform);
            RectTransform closeRt = (RectTransform)close.transform;
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-12f, -12f);
            closeRt.sizeDelta = new Vector2(54f, 54f);
            Image closeImage = close.AddComponent<Image>();
            closeImage.color = new Color(0.85f, 0.28f, 0.20f, 0.95f);
            UIUtil.AddClick(closeImage, Hide);

            TextMeshProUGUI closeText = NewText("Label", close.transform, 28f, TextAlignmentOptions.Center);
            RectTransform closeTextRt = (RectTransform)closeText.transform;
            Stretch(closeTextRt);
            closeText.text = "X";
            closeText.color = Color.white;

            _root.SetActive(false);
        }

        private static GameObject NewRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI NewText(string name, Transform parent, float size, TextAlignmentOptions align)
        {
            GameObject go = NewRect(name, parent);
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.alignment = align;
            text.raycastTarget = false;
            ApplyFont(text);
            return text;
        }

        private static void ApplyFont(TextMeshProUGUI target)
        {
            TextMeshProUGUI source = Object.FindAnyObjectByType<TextMeshProUGUI>();
            if (source == null || source == target) return;
            target.font = source.font;
            target.fontSharedMaterial = source.fontSharedMaterial;
        }
    }
}
