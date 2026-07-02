using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Guild
{
    /// <summary>
    /// 结社加入临时壳(TEMP SHELL,同 GuBaoShellView/PartnerShellView 约定:代码建 uGUI、数据全真、样式从简待用户重做 UI)。
    /// 对标老端 GuildJoinView 最小可用面:结社列表(前 6 行,名+等级+人数)+ [一键申请]/[创建结社]/[关闭]。
    /// 主线 101080(ctype14)由此进入。打开即发 40001 求列表 + 30008 补触发任务判定(对标老端 LoadSuccess)。
    /// 空列表如实提示可建社;已有公会(HasGuild)顶部提示已加入,不阻断操作(老端允许继续浏览/换社)。
    /// </summary>
    public static class GuildJoinShellView
    {
        private const int ROW_COUNT = 6;               // 工单范围:列表前 6 行
        private const string DEFAULT_GUILD_NAME = "神霄阁";

        private static GameObject _root;
        private static Transform _rowsParent;
        private static TextMeshProUGUI _statusText;
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
                EventDispatcher.On(GlobalEvent.EVT_GUILD_UPDATE, Rebuild);
                _listening = true;
            }
            GuildJoinController.Instance.RequestList();
            GuildJoinController.Instance.NotifyTaskCheck();
            Rebuild();
            GameLog.Info("Guild", "GuildJoinShellView 打开: hasData={0}", GuildJoinModel.Instance.HasData);
        }

        public static void Close()
        {
            if (_listening)
            {
                EventDispatcher.Off(GlobalEvent.EVT_GUILD_UPDATE, Rebuild);
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

            GuildJoinModel model = GuildJoinModel.Instance;
            _statusText.text = model.HasGuild ? "已加入结社" : "结社";

            if (!model.HasData)
            {
                GameObject tip = NewRow(0);
                TextMeshProUGUI txt = NewText("Info", tip.transform, 24, TextAlignmentOptions.Center);
                Stretch(txt.rectTransform);
                txt.text = "等待 40001(需活服)";
                txt.color = new Color(0.65f, 0.72f, 0.85f);
                return;
            }

            if (model.List.Count == 0)
            {
                GameObject tip = NewRow(0);
                TextMeshProUGUI txt = NewText("Info", tip.transform, 24, TextAlignmentOptions.Center);
                Stretch(txt.rectTransform);
                txt.text = "暂无结社,可创建本服第一个结社";
                txt.color = new Color(0.85f, 0.75f, 0.4f);
                return;
            }

            int count = Mathf.Min(ROW_COUNT, model.List.Count);
            for (int i = 0; i < count; i++)
            {
                GuildJoinModel.GuildBrief g = model.List[i];
                GameObject row = NewRow(i);
                TextMeshProUGUI label = NewText("Label", row.transform, 22, TextAlignmentOptions.MidlineLeft);
                Stretch(label.rectTransform);
                label.text = string.Format("{0}　Lv{1}　{2}/{3}", g.Name, g.Lv, g.MemberNum, g.MemberCapacity);
            }
        }

        // ---- 构建(代码建 uGUI;同 GuBaoShellView TEMP 壳约定)----

        private static void EnsureBuilt()
        {
            if (_root != null) return;
            Transform parent = ViewManager.GetLayer(UILayer.Window);
            if (parent == null)
            {
                GameLog.Error("Guild", "GuildJoinShellView 无法构建:UI Window 层未就绪");
                return;
            }

            _root = NewRect("GuildJoinShellView(TempShell)", parent, Vector2.zero, Vector2.one);

            GameObject bg = NewRect("Backdrop", _root.transform, Vector2.zero, Vector2.one);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.45f);
            bgImg.raycastTarget = true;
            UIUtil.AddClick(bgImg, Close);

            GameObject panel = NewRect("Panel", _root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var panelRt = (RectTransform)panel.transform;
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(560f, 560f);
            Image panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.07f, 0.09f, 0.14f, 0.97f);

            TextMeshProUGUI title = NewText("Title", panel.transform, 30, TextAlignmentOptions.Top);
            var trt = title.rectTransform;
            trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f); trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -20f); trt.sizeDelta = new Vector2(-40f, 44f);
            title.text = "结社";
            title.color = new Color(1f, 0.86f, 0.45f);
            title.fontStyle = FontStyles.Bold;

            _statusText = NewText("Status", panel.transform, 22, TextAlignmentOptions.Top);
            var strt = _statusText.rectTransform;
            strt.anchorMin = new Vector2(0f, 1f); strt.anchorMax = new Vector2(1f, 1f); strt.pivot = new Vector2(0.5f, 1f);
            strt.anchoredPosition = new Vector2(0f, -60f); strt.sizeDelta = new Vector2(-40f, 34f);
            _statusText.text = "结社";
            _statusText.color = new Color(0.85f, 0.9f, 1f);

            GameObject rows = NewRect("Rows", panel.transform, new Vector2(0f, 0f), new Vector2(1f, 1f));
            var rrt = (RectTransform)rows.transform;
            rrt.offsetMin = new Vector2(12f, 108f); rrt.offsetMax = new Vector2(-12f, -100f);
            _rowsParent = rows.transform;

            NewButton(panel.transform, "一键申请", new Vector2(0.18f, 0f), new Color(0.20f, 0.45f, 0.30f, 1f),
                () => GuildJoinController.Instance.ApplyAll());
            NewButton(panel.transform, "创建结社", new Vector2(0.5f, 0f), new Color(0.42f, 0.33f, 0.18f, 1f),
                () => GuildJoinController.Instance.Create(DEFAULT_GUILD_NAME));
            NewButton(panel.transform, "关闭", new Vector2(0.82f, 0f), new Color(0.20f, 0.30f, 0.48f, 1f), Close);
        }

        private static GameObject NewRow(int index)
        {
            var row = new GameObject("Row" + index, typeof(RectTransform));
            row.transform.SetParent(_rowsParent, false);
            var rt = (RectTransform)row.transform;
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 60f);
            rt.anchoredPosition = new Vector2(0f, -index * 64f);
            Image bg = row.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.05f);
            bg.raycastTarget = false;
            _rows.Add(row);
            return row;
        }

        private static void NewButton(Transform parent, string text, Vector2 anchorX, Color color, System.Action onClick)
        {
            var go = new GameObject("Btn" + text, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(anchorX.x, 0f); rt.anchorMax = new Vector2(anchorX.x, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(160f, 56f);
            rt.anchoredPosition = new Vector2(0f, 14f);
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
