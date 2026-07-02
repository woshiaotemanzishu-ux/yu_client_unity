using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Jjc
{
    /// <summary>
    /// 排位赛(竞技场 JJC)临时壳(TEMP SHELL,同 GuBaoShellView/ComposeShellView 约定:代码建 uGUI、数据全真、
    /// 样式从简待用户重做 UI)。显示我的排名/剩余次数 + [随机对手] + 对手列表首个[挑战]。
    /// 顶部固定一行服务端断链警示(诚实告知,不隐藏已知缺口)。主线 101465(ctype35)由此进入。
    /// </summary>
    public static class JjcShellView
    {
        private const string SERVER_GAP_WARNING = "⚠服务端计数断链(mod_jjc_cast.erl:87),挑战不推进任务,待服务端修复";

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
                EventDispatcher.On(GlobalEvent.EVT_JJC_UPDATE, Rebuild);
                _listening = true;
            }
            JjcController.Instance.RequestInfo();
            JjcController.Instance.RequestRivals();
            Rebuild();
            GameLog.Info("Jjc", "JjcShellView 打开: hasInfo={0} hasRivals={1}", JjcModel.Instance.HasInfo, JjcModel.Instance.HasRivals);
        }

        public static void Close()
        {
            if (_listening)
            {
                EventDispatcher.Off(GlobalEvent.EVT_JJC_UPDATE, Rebuild);
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

            JjcModel model = JjcModel.Instance;

            // 行0:我的排名/剩余次数
            GameObject infoRow = NewRow(0);
            TextMeshProUGUI infoLabel = NewText("Label", infoRow.transform, 22, TextAlignmentOptions.MidlineLeft);
            var infoLrt = infoLabel.rectTransform;
            infoLrt.anchorMin = new Vector2(0f, 0f); infoLrt.anchorMax = new Vector2(1f, 1f);
            infoLrt.offsetMin = new Vector2(16f, 0f); infoLrt.offsetMax = new Vector2(-160f, 0f);
            if (!model.HasInfo)
            {
                infoLabel.text = "等待 28001(需活服)";
                infoLabel.color = new Color(0.65f, 0.72f, 0.85f);
            }
            else
            {
                infoLabel.text = "我的排名:<color=#ffe222>" + model.Rank + "</color>　剩余次数:<color=#ffe222>" + model.Num + "</color>　荣誉:" + model.Honour;
                infoLabel.color = Color.white;
            }
            NewButton(infoRow.transform, "刷新对手", -80f, new Color(0.20f, 0.30f, 0.48f), () => JjcController.Instance.RequestRivals());

            // 对手列表(首个带[挑战]按钮)
            if (!model.HasRivals || model.Rivals.Count == 0)
            {
                GameObject tip = NewRow(1);
                TextMeshProUGUI txt = NewText("Info", tip.transform, 22, TextAlignmentOptions.Center);
                Stretch(txt.rectTransform);
                txt.text = model.HasRivals ? "无对手数据" : "等待 28002(需活服)";
                txt.color = new Color(0.65f, 0.72f, 0.85f);
                return;
            }

            for (int i = 0; i < model.Rivals.Count; i++)
            {
                JjcModel.RivalVo rival = model.Rivals[i];
                GameObject row = NewRow(1 + i);

                TextMeshProUGUI label = NewText("Label", row.transform, 22, TextAlignmentOptions.MidlineLeft);
                var lrt = label.rectTransform;
                lrt.anchorMin = new Vector2(0f, 0f); lrt.anchorMax = new Vector2(1f, 1f);
                lrt.offsetMin = new Vector2(16f, 0f); lrt.offsetMax = new Vector2(-160f, 0f);
                string name = rival.Figure != null && !string.IsNullOrEmpty(rival.Figure.name) ? rival.Figure.name : ("角色" + rival.RoleId);
                label.text = "第" + rival.Rank + "名　" + name + "　战力" + rival.Combat;

                if (i == 0)
                {
                    int selfRank = model.Rank;
                    long rivalId = rival.RoleId;
                    int rivalRank = rival.Rank;
                    NewButton(row.transform, "挑战", -80f, new Color(0.42f, 0.20f, 0.20f),
                        () => JjcController.Instance.Challenge(selfRank, rivalId, rivalRank));
                }
            }
        }

        // ---- 构建(代码建 uGUI;同 GuBaoShellView TEMP 壳约定)----

        private static void EnsureBuilt()
        {
            if (_root != null) return;
            Transform parent = ViewManager.GetLayer(UILayer.Window);
            if (parent == null)
            {
                GameLog.Error("Jjc", "JjcShellView 无法构建:UI Window 层未就绪");
                return;
            }

            _root = NewRect("JjcShellView(TempShell)", parent, Vector2.zero, Vector2.one);

            GameObject bg = NewRect("Backdrop", _root.transform, Vector2.zero, Vector2.one);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.45f);
            bgImg.raycastTarget = true;
            UIUtil.AddClick(bgImg, Close);

            GameObject panel = NewRect("Panel", _root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var panelRt = (RectTransform)panel.transform;
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(640f, 680f);
            Image panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.07f, 0.09f, 0.14f, 0.97f);

            TextMeshProUGUI title = NewText("Title", panel.transform, 30, TextAlignmentOptions.Top);
            var trt = title.rectTransform;
            trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f); trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -20f); trt.sizeDelta = new Vector2(-40f, 44f);
            title.text = "排位赛";
            title.color = new Color(1f, 0.86f, 0.45f);
            title.fontStyle = FontStyles.Bold;

            // 服务端断链警示(固定顶部一行,工单要求诚实告知)。
            TextMeshProUGUI warning = NewText("ServerGapWarning", panel.transform, 18, TextAlignmentOptions.Top);
            var wrt = warning.rectTransform;
            wrt.anchorMin = new Vector2(0f, 1f); wrt.anchorMax = new Vector2(1f, 1f); wrt.pivot = new Vector2(0.5f, 1f);
            wrt.anchoredPosition = new Vector2(0f, -60f); wrt.sizeDelta = new Vector2(-24f, 32f);
            warning.text = SERVER_GAP_WARNING;
            warning.color = new Color(1f, 0.55f, 0.35f);

            GameObject rows = NewRect("Rows", panel.transform, new Vector2(0f, 0f), new Vector2(1f, 1f));
            var rrt = (RectTransform)rows.transform;
            rrt.offsetMin = new Vector2(12f, 92f); rrt.offsetMax = new Vector2(-12f, -100f);
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
            rt.sizeDelta = new Vector2(140f, 48f);
            rt.anchoredPosition = new Vector2(x + 70f, 0f);
            Image img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;
            TextMeshProUGUI lbl = NewText("Label", go.transform, 20, TextAlignmentOptions.Center);
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
