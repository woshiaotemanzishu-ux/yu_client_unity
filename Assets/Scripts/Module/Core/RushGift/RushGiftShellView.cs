using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.RushGift
{
    /// <summary>
    /// 冲级豪礼临时壳(TEMP SHELL,同 PartnerShellView/ItemTipsView 约定:代码建 uGUI、数据全真、样式从简待用户重做 UI)。
    /// 对标老端等级礼包面板的最小可用面:逐礼包一行(config_rush_giftbag 真名 + received 真实状态文案),
    /// received==1(可领)时挂[领取](发 41701)。最多显示前 8 行(超出仅日志说明,不截断真实数据)。
    /// 无服务器数据(HasData=false)时如实显示等待 41700,不造假列表。DoTask ctype 54(主线 100420)由此进入。
    /// </summary>
    public static class RushGiftShellView
    {
        private const int MAX_ROWS = 8;

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
                EventDispatcher.On(GlobalEvent.EVT_RUSH_GIFT_UPDATE, Rebuild);
                _listening = true;
            }
            Rebuild();
            GameLog.Info("RushGift", "RushGiftShellView 打开: list={0} hasData={1}",
                RushGiftModel.Instance.List.Count, RushGiftModel.Instance.HasData);
        }

        public static void Close()
        {
            if (_listening)
            {
                EventDispatcher.Off(GlobalEvent.EVT_RUSH_GIFT_UPDATE, Rebuild);
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

            RushGiftModel model = RushGiftModel.Instance;
            if (!model.HasData || model.List.Count == 0)
            {
                GameObject tip = NewRow(0);
                TextMeshProUGUI txt = NewText("Info", tip.transform, 24, TextAlignmentOptions.Center);
                Stretch(txt.rectTransform);
                txt.text = model.HasData ? "暂无礼包" : "等待41700(需活服)";
                txt.color = new Color(0.65f, 0.72f, 0.85f);
                return;
            }

            int shown = Mathf.Min(model.List.Count, MAX_ROWS);
            for (int i = 0; i < shown; i++)
            {
                RushGiftModel.GiftVo vo = model.List[i];
                GameObject row = NewRow(i);

                TextMeshProUGUI label = NewText("Label", row.transform, 24, TextAlignmentOptions.MidlineLeft);
                var lrt = label.rectTransform;
                lrt.anchorMin = new Vector2(0f, 0f); lrt.anchorMax = new Vector2(1f, 1f);
                lrt.offsetMin = new Vector2(16f, 0f); lrt.offsetMax = new Vector2(-160f, 0f);
                label.text = RushGiftConfigs.GetName(vo.Lv) + "　" + StatusText(vo.Received);

                if (vo.Received == 1)
                {
                    int lv = vo.Lv;
                    NewButton(row.transform, "领取", -80f, new Color(0.22f, 0.42f, 0.24f), () => RushGiftController.Instance.Receive(lv));
                }
            }

            if (model.List.Count > MAX_ROWS)
                GameLog.Info("RushGift", "RushGiftShellView 仅显示前{0}行,总数={1}(超出未截断数据,仅壳展示限制)", MAX_ROWS, model.List.Count);
        }

        /// <summary>received 状态文案:0条件不足/1可领/2已领/4已被领完。</summary>
        private static string StatusText(int received)
        {
            switch (received)
            {
                case 0: return "<color=#8893a6>条件不足</color>";
                case 1: return "<color=#ffe222>可领取</color>";
                case 2: return "<color=#6fd06f>已领取</color>";
                case 4: return "<color=#d06f6f>已被领完</color>";
                default: return "<color=#8893a6>状态(" + received + ")</color>";
            }
        }

        // ---- 构建(代码建 uGUI;同 PartnerShellView TEMP 壳约定)----

        private static void EnsureBuilt()
        {
            if (_root != null) return;
            Transform parent = ViewManager.GetLayer(UILayer.Window);
            if (parent == null)
            {
                GameLog.Error("RushGift", "RushGiftShellView 无法构建:UI Window 层未就绪");
                return;
            }

            _root = NewRect("RushGiftShellView(TempShell)", parent, Vector2.zero, Vector2.one);

            GameObject bg = NewRect("Backdrop", _root.transform, Vector2.zero, Vector2.one);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.45f);
            bgImg.raycastTarget = true;
            UIUtil.AddClick(bgImg, Close);

            GameObject panel = NewRect("Panel", _root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var panelRt = (RectTransform)panel.transform;
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(560f, 720f);
            Image panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.07f, 0.09f, 0.14f, 0.97f);

            TextMeshProUGUI title = NewText("Title", panel.transform, 30, TextAlignmentOptions.Top);
            var trt = title.rectTransform;
            trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f); trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -20f); trt.sizeDelta = new Vector2(-40f, 44f);
            title.text = "冲级豪礼";
            title.color = new Color(1f, 0.86f, 0.45f);
            title.fontStyle = FontStyles.Bold;

            GameObject rows = NewRect("Rows", panel.transform, new Vector2(0f, 0f), new Vector2(1f, 1f));
            var rrt = (RectTransform)rows.transform;
            rrt.offsetMin = new Vector2(12f, 92f); rrt.offsetMax = new Vector2(-12f, -76f);
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
            rt.anchoredPosition = new Vector2(x + 50f, 0f);
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
