using System;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Common.Tips
{
    /// <summary>
    /// 双按钮确认框(对标老客户端 Alert.Show(text, Alert_Type.Two, onYes, onNo, "确认", "取消")):
    /// 全屏遮罩 + 居中面板 + 文案 + 确认/取消。代码建树(无 prefab 依赖),挂 Tip 层置顶;
    /// 单实例复用,再次 Show 覆盖文案与回调(老端 Alert 同为单例)。点遮罩等价取消。
    /// 样式为可用起步值;后续要皮肤化可移到 UiCreator 出 prefab,这里的调用方不变。
    /// </summary>
    public static class ConfirmDialog
    {
        private static GameObject _root;
        private static TextMeshProUGUI _body;
        private static Action _onYes;
        private static Action _onNo;

        public static void Show(string text, Action onYes, Action onNo)
        {
            EnsureCreated();
            if (_root == null)
            {
                onYes?.Invoke(); // 层缺失兜底(理论上 Confirm 已挡,双保险)
                return;
            }

            _onYes = onYes;
            _onNo = onNo;
            _body.text = text ?? "";
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }

        private static void Close(bool yes)
        {
            if (_root != null) _root.SetActive(false);
            Action cb = yes ? _onYes : _onNo;
            _onYes = null;
            _onNo = null;
            cb?.Invoke();
        }

        private static void EnsureCreated()
        {
            if (_root != null) return;
            Transform layer = ViewManager.GetLayer(UILayer.Tip);
            if (layer == null) return;

            _root = NewRect("ConfirmDialog", layer);
            var rootRt = (RectTransform)_root.transform;
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            Image shade = _root.AddComponent<Image>();
            shade.color = new Color(0f, 0f, 0f, 0.5f);
            UIUtil.AddClick(shade, () => Close(false)); // 点遮罩=取消(老端 click_bg 关闭)

            GameObject panel = NewRect("Panel", _root.transform);
            var panelRt = (RectTransform)panel.transform;
            panelRt.anchorMin = panelRt.anchorMax = panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(520f, 260f);
            Image panelBg = panel.AddComponent<Image>();
            panelBg.color = new Color(0.12f, 0.09f, 0.05f, 0.96f);
            panelBg.raycastTarget = true; // 面板体吞点击,别漏给遮罩当取消

            _body = NewText("Body", panel.transform, 26f);
            var bodyRt = (RectTransform)_body.transform;
            bodyRt.anchorMin = new Vector2(0f, 0.35f);
            bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.offsetMin = new Vector2(30f, 0f);
            bodyRt.offsetMax = new Vector2(-30f, -24f);
            _body.color = new Color(0.95f, 0.90f, 0.80f, 1f);

            MakeButton(panel.transform, "Yes", "确 认", new Vector2(-110f, 44f), new Color(0.72f, 0.45f, 0.12f, 1f), () => Close(true));
            MakeButton(panel.transform, "No", "取 消", new Vector2(110f, 44f), new Color(0.35f, 0.37f, 0.42f, 1f), () => Close(false));

            _root.SetActive(false);
        }

        private static void MakeButton(Transform parent, string name, string label, Vector2 pos, Color color, Action onClick)
        {
            GameObject go = NewRect(name, parent);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(170f, 58f);
            Image bg = go.AddComponent<Image>();
            bg.color = color;
            UIUtil.AddClick(bg, onClick);

            TextMeshProUGUI text = NewText("Label", go.transform, 26f);
            var textRt = (RectTransform)text.transform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            text.text = label;
            text.color = Color.white;
        }

        private static GameObject NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static TextMeshProUGUI NewText(string name, Transform parent, float size)
        {
            GameObject go = NewRect(name, parent);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            ApplyFont(text);
            return text;
        }

        /// <summary>复用场景中已打开文本的 CJK 字体(同 TipsManager.ApplyFont 约定)。</summary>
        private static void ApplyFont(TextMeshProUGUI target)
        {
            foreach (TextMeshProUGUI candidate in UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
            {
                if (candidate == target) continue;
                target.font = candidate.font;
                target.fontSharedMaterial = candidate.fontSharedMaterial;
                break;
            }
        }
    }
}
