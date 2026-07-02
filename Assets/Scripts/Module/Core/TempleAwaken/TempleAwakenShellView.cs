using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.TempleAwaken
{
    /// <summary>
    /// 天命觉醒(觉醒之路)临时壳(TEMP SHELL,同 PartnerShellView/OutWardShellView 约定:代码建 uGUI、
    /// 数据全真、样式从简待用户重做 UI)。对标老端 TempleAwakenEnterView 最小可用面:标题+说明+[开启]按钮
    /// (发 42900)。按钮永远可点——前置/等级门槛服务端二次校验,失败原样显码,不在客户端臆造门禁判断。
    /// DoTask ctype81(主线 100590「觉醒之路」)由此进入。
    /// </summary>
    public static class TempleAwakenShellView
    {
        private static GameObject _root;
        private static TMP_FontAsset _font;
        private static Material _fontMat;
        private static bool _listening;

        public static void Show()
        {
            EnsureBuilt();
            if (_root == null) return;
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            if (!_listening)
            {
                EventDispatcher.On(GlobalEvent.EVT_TEMPLE_AWAKEN_UPDATE, Rebuild);
                _listening = true;
            }
            Rebuild();
            GameLog.Info("TempleAwaken", "TempleAwakenShellView 打开: preTaskFinished={0} opened={1}",
                TempleAwakenModel.Instance.PreTaskFinished, TempleAwakenModel.Instance.Opened);
        }

        public static void Close()
        {
            if (_listening)
            {
                EventDispatcher.Off(GlobalEvent.EVT_TEMPLE_AWAKEN_UPDATE, Rebuild);
                _listening = false;
            }
            if (_root != null) _root.SetActive(false);
        }

        private static TMP_Text _descText;

        private static void Rebuild()
        {
            if (_root == null || !_root.activeSelf || _descText == null) return;
            TempleAwakenModel model = TempleAwakenModel.Instance;
            _descText.text = "完成前置任务后可开启天命觉醒"
                + (model.PreTaskFinished ? "　<color=#7de17d>(前置已完成)</color>" : "　<color=#8893a6>(等待前置任务)</color>")
                + (model.Opened ? "\n<color=#ffe222>觉醒之路已开启</color>" : "");
        }

        // ---- 构建(代码建 uGUI;同 PartnerShellView/OutWardShellView TEMP 壳约定)----

        private static void EnsureBuilt()
        {
            if (_root != null) return;
            Transform parent = ViewManager.GetLayer(UILayer.Window);
            if (parent == null)
            {
                GameLog.Error("TempleAwaken", "TempleAwakenShellView 无法构建:UI Window 层未就绪");
                return;
            }

            _root = NewRect("TempleAwakenShellView(TempShell)", parent, Vector2.zero, Vector2.one);

            GameObject bg = NewRect("Backdrop", _root.transform, Vector2.zero, Vector2.one);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.45f);
            bgImg.raycastTarget = true;
            UIUtil.AddClick(bgImg, Close);

            GameObject panel = NewRect("Panel", _root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var panelRt = (RectTransform)panel.transform;
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(560f, 380f);
            Image panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.07f, 0.09f, 0.14f, 0.97f);

            TextMeshProUGUI title = NewText("Title", panel.transform, 30, TextAlignmentOptions.Top);
            var trt = title.rectTransform;
            trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f); trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -20f); trt.sizeDelta = new Vector2(-40f, 44f);
            title.text = "觉醒之路";
            title.color = new Color(1f, 0.86f, 0.45f);
            title.fontStyle = FontStyles.Bold;

            TextMeshProUGUI desc = NewText("Desc", panel.transform, 24, TextAlignmentOptions.Center);
            var drt = desc.rectTransform;
            drt.anchorMin = new Vector2(0f, 0f); drt.anchorMax = new Vector2(1f, 1f);
            drt.offsetMin = new Vector2(24f, 92f); drt.offsetMax = new Vector2(-24f, -76f);
            desc.color = new Color(0.85f, 0.88f, 0.95f);
            _descText = desc;

            GameObject openBtn = NewRect("BtnOpen", panel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            var openRt = (RectTransform)openBtn.transform;
            openRt.pivot = new Vector2(0.5f, 0f);
            openRt.sizeDelta = new Vector2(220f, 56f);
            openRt.anchoredPosition = new Vector2(0f, 14f);
            Image openImg = openBtn.AddComponent<Image>();
            openImg.color = new Color(0.22f, 0.42f, 0.24f);
            TextMeshProUGUI openLbl = NewText("Label", openBtn.transform, 26, TextAlignmentOptions.Center);
            Stretch(openLbl.rectTransform);
            openLbl.text = "开启";
            openLbl.color = Color.white;
            UIUtil.AddClick(openImg, () => TempleAwakenController.Instance.FinishInitial());
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
