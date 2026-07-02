using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.GuBao
{
    /// <summary>
    /// 古宝(妖物)临时壳(TEMP SHELL,同 ItemTipsView/PartnerShellView 约定:代码建 uGUI、数据全真、样式从简待用户重做 UI)。
    /// 对标老端古宝面板的最小可用面:soap 10001「幽瞳」的 2 个碎片行(碎片真名+已激活标记 或 [激活]按钮 → 13321)。
    /// 主线 100811(ctype89)由此进入。无服务器数据(HasData=false)时如实显示等待 13320,不造假列表。
    /// </summary>
    public static class GuBaoShellView
    {
        private const int SOAP_ID = 10001;      // 幽瞳
        private const int DEBRIS_COUNT = 2;     // 工单范围:2 片碎片

        private static GameObject _root;
        private static Transform _rowsParent;
        private static TMP_FontAsset _font;
        private static Material _fontMat;
        private static readonly List<GameObject> _rows = new List<GameObject>();
        private static bool _listening;

        public static void Show()
        {
            EnsureBuilt();
            if (_root == null) return;
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            if (!_listening)
            {
                EventDispatcher.On(GlobalEvent.EVT_GUBAO_UPDATE, Rebuild);
                _listening = true;
            }
            Rebuild();
            GameLog.Info("GuBao", "GuBaoShellView 打开: hasData={0}", GuBaoModel.Instance.HasData);
        }

        public static void Close()
        {
            if (_listening)
            {
                EventDispatcher.Off(GlobalEvent.EVT_GUBAO_UPDATE, Rebuild);
                _listening = false;
            }
            if (_root != null) _root.SetActive(false);
        }

        private static void Rebuild()
        {
            if (_root == null || !_root.activeSelf) return;
            foreach (GameObject row in _rows)
                if (row != null) { if (Application.isPlaying) Object.Destroy(row); else Object.DestroyImmediate(row); }
            _rows.Clear();

            GuBaoModel model = GuBaoModel.Instance;
            if (!model.HasData)
            {
                GameObject tip = NewRow(0);
                TextMeshProUGUI txt = NewText("Info", tip.transform, 24, TextAlignmentOptions.Center);
                Stretch(txt.rectTransform);
                txt.text = "等待 13320(需活服)";
                txt.color = new Color(0.65f, 0.72f, 0.85f);
                return;
            }

            for (int i = 0; i < DEBRIS_COUNT; i++)
            {
                int debrisId = i + 1;
                GameObject row = NewRow(i);
                bool active = model.IsDebrisActive(SOAP_ID, debrisId);

                TextMeshProUGUI label = NewText("Label", row.transform, 24, TextAlignmentOptions.MidlineLeft);
                var lrt = label.rectTransform;
                lrt.anchorMin = new Vector2(0f, 0f); lrt.anchorMax = new Vector2(1f, 1f);
                lrt.offsetMin = new Vector2(16f, 0f); lrt.offsetMax = new Vector2(-160f, 0f);
                label.text = GuBaoConfigs.GetDebrisName(SOAP_ID, debrisId)
                    + (active ? "　<color=#5ce27a>(已激活)</color>" : "　<color=#8893a6>(未激活)</color>");

                if (active)
                {
                    TextMeshProUGUI mark = NewText("Mark", row.transform, 22, TextAlignmentOptions.Center);
                    var mrt = mark.rectTransform;
                    mrt.anchorMin = mrt.anchorMax = new Vector2(1f, 0.5f);
                    mrt.pivot = new Vector2(0.5f, 0.5f);
                    mrt.sizeDelta = new Vector2(110f, 48f);
                    mrt.anchoredPosition = new Vector2(-70f, 0f);
                    mark.text = "已激活";
                    mark.color = new Color(0.36f, 0.89f, 0.48f);
                }
                else
                {
                    int did = debrisId;
                    NewButton(row.transform, "激活", -70f, new Color(0.42f, 0.33f, 0.18f), () => GuBaoController.Instance.Activate(SOAP_ID, did));
                }
            }
        }

        // ---- 构建(代码建 uGUI;同 ItemTipsView TEMP 壳约定)----

        private static void EnsureBuilt()
        {
            if (_root != null) return;
            Transform parent = ViewManager.GetLayer(UILayer.Window);
            if (parent == null)
            {
                GameLog.Error("GuBao", "GuBaoShellView 无法构建:UI Window 层未就绪");
                return;
            }

            _root = NewRect("GuBaoShellView(TempShell)", parent, Vector2.zero, Vector2.one);

            GameObject bg = NewRect("Backdrop", _root.transform, Vector2.zero, Vector2.one);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.45f);
            bgImg.raycastTarget = true;
            UIUtil.AddClick(bgImg, Close);

            GameObject panel = NewRect("Panel", _root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var panelRt = (RectTransform)panel.transform;
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(560f, 480f);
            Image panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.07f, 0.09f, 0.14f, 0.97f);

            TextMeshProUGUI title = NewText("Title", panel.transform, 30, TextAlignmentOptions.Top);
            var trt = title.rectTransform;
            trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f); trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -20f); trt.sizeDelta = new Vector2(-40f, 44f);
            title.text = "古宝·森罗万象";
            title.color = new Color(1f, 0.86f, 0.45f);
            title.fontStyle = FontStyles.Bold;

            TextMeshProUGUI subtitle = NewText("Subtitle", panel.transform, 24, TextAlignmentOptions.Top);
            var srt = subtitle.rectTransform;
            srt.anchorMin = new Vector2(0f, 1f); srt.anchorMax = new Vector2(1f, 1f); srt.pivot = new Vector2(0.5f, 1f);
            srt.anchoredPosition = new Vector2(0f, -60f); srt.sizeDelta = new Vector2(-40f, 36f);
            subtitle.text = GuBaoConfigs.GetSoapName(SOAP_ID);
            subtitle.color = new Color(0.85f, 0.9f, 1f);

            GameObject rows = NewRect("Rows", panel.transform, new Vector2(0f, 0f), new Vector2(1f, 1f));
            var rrt = (RectTransform)rows.transform;
            rrt.offsetMin = new Vector2(12f, 92f); rrt.offsetMax = new Vector2(-12f, -104f);
            _rowsParent = rows.transform;

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

        private static GameObject NewRow(int index)
        {
            var row = new GameObject("Row" + index, typeof(RectTransform));
            row.transform.SetParent(_rowsParent, false);
            var rt = (RectTransform)row.transform;
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 64f);
            rt.anchoredPosition = new Vector2(0f, -index * 70f);
            Image bg = row.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.05f);
            bg.raycastTarget = false;
            _rows.Add(row);
            return row;
        }

        private static void NewButton(Transform parent, string text, float x, Color color, System.Action onClick)
        {
            var go = new GameObject("Btn" + text, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(110f, 48f);
            rt.anchoredPosition = new Vector2(x, 0f);
            Image img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;
            TextMeshProUGUI lbl = NewText("Label", go.transform, 24, TextAlignmentOptions.Center);
            Stretch(lbl.rectTransform);
            lbl.text = text;
            lbl.color = Color.white;
            UIUtil.AddClick(img, () => onClick?.Invoke());
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
