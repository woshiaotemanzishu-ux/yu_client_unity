using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.OnHook
{
    /// <summary>
    /// 挂机收益临时壳(TEMP SHELL,同 GuBaoShellView/OutWardShellView 约定:代码建 uGUI、样式从简待用户重做 UI)。
    /// 最小可用面:标题「挂机收益」+ [领取] 按钮 → 发 13216。信息协议(累计挂机时长/经验等)13211/13212/13214 未移植,
    /// 不画假进度条。主线 101211(ctype91)由此进入。
    /// </summary>
    public static class OnHookShellView
    {
        private static GameObject _root;
        private static TMP_FontAsset _font;
        private static Material _fontMat;

        public static void Show()
        {
            EnsureBuilt();
            if (_root == null) return;
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            GameLog.Info("OnHook", "OnHookShellView 打开");
        }

        public static void Close()
        {
            if (_root != null) _root.SetActive(false);
        }

        private static void EnsureBuilt()
        {
            if (_root != null) return;
            Transform parent = ViewManager.GetLayer(UILayer.Window);
            if (parent == null)
            {
                GameLog.Error("OnHook", "OnHookShellView 无法构建:UI Window 层未就绪");
                return;
            }

            _root = NewRect("OnHookShellView(TempShell)", parent, Vector2.zero, Vector2.one);

            GameObject bg = NewRect("Backdrop", _root.transform, Vector2.zero, Vector2.one);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.45f);
            bgImg.raycastTarget = true;
            UIUtil.AddClick(bgImg, Close);

            GameObject panel = NewRect("Panel", _root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var panelRt = (RectTransform)panel.transform;
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(480f, 300f);
            Image panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.07f, 0.09f, 0.14f, 0.97f);

            TextMeshProUGUI title = NewText("Title", panel.transform, 30, TextAlignmentOptions.Top);
            var trt = title.rectTransform;
            trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f); trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -20f); trt.sizeDelta = new Vector2(-40f, 44f);
            title.text = "挂机收益";
            title.color = new Color(1f, 0.86f, 0.45f);
            title.fontStyle = FontStyles.Bold;

            GameObject receiveBtn = NewRect("Receive", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var receiveRt = (RectTransform)receiveBtn.transform;
            receiveRt.pivot = new Vector2(0.5f, 0.5f);
            receiveRt.sizeDelta = new Vector2(200f, 64f);
            receiveRt.anchoredPosition = new Vector2(0f, 10f);
            Image receiveImg = receiveBtn.AddComponent<Image>();
            receiveImg.color = new Color(0.22f, 0.42f, 0.24f, 1f);
            TextMeshProUGUI receiveLbl = NewText("Label", receiveBtn.transform, 28, TextAlignmentOptions.Center);
            Stretch(receiveLbl.rectTransform);
            receiveLbl.text = "领取";
            receiveLbl.color = Color.white;
            UIUtil.AddClick(receiveImg, () => OnHookController.Instance.Receive());

            GameObject closeBtn = NewRect("Close", panel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            var closeRt = (RectTransform)closeBtn.transform;
            closeRt.pivot = new Vector2(0.5f, 0f);
            closeRt.sizeDelta = new Vector2(200f, 56f);
            closeRt.anchoredPosition = new Vector2(0f, 14f);
            Image closeImg = closeBtn.AddComponent<Image>();
            closeImg.color = new Color(0.20f, 0.30f, 0.48f, 1f);
            TextMeshProUGUI closeLbl = NewText("Label", closeBtn.transform, 26, TextAlignmentOptions.Center);
            Stretch(closeLbl.rectTransform);
            closeLbl.text = "关闭";
            closeLbl.color = Color.white;
            UIUtil.AddClick(closeImg, Close);
        }

        private static GameObject NewRect(string name, Transform parent, Vector2 aMin, Vector2 aMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return go;
        }

        private static TextMeshProUGUI NewText(string name, Transform parent, float size, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.fontSize = size;
            t.alignment = align;
            t.richText = true;
            ApplyFont(t);
            return t;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static void ApplyFont(TextMeshProUGUI t)
        {
            if (_font == null)
            {
                foreach (TextMeshProUGUI candidate in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
                {
                    if (candidate != t) { _font = candidate.font; _fontMat = candidate.fontSharedMaterial; break; }
                }
            }
            if (_font != null) t.font = _font;
            if (_fontMat != null) t.fontSharedMaterial = _fontMat;
        }
    }
}
