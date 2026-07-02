using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Dungeon
{
    /// <summary>
    /// 御魂秘境临时壳(TEMP SHELL,同 PartnerShellView/GuBaoShellView 约定:代码建 uGUI、数据全真、样式从简
    /// 待用户重做 UI)。对标老端 DungeonRuneEnterView 的最小可用面:固定展示 12001~12003 三层
    /// (config_dungeon 真名 + [进入] 发 61001)+ 底部[退出副本](发 61002)/[关闭] + 最近一次结算结果一行
    /// (61003 推送后由 EVT_DUNGEON_UPDATE 刷新)。DoTask ctype9(id=12,主线 100980「挑战御魂塔1层」)/
    /// ctype57(101522「通关3层」)由此进入。A2(专属关卡列表/每日/首通 UI)全部留后,不在本壳范围。
    /// </summary>
    public static class DungeonRuneShellView
    {
        private static readonly int[] DunIds = { 12001, 12002, 12003 };

        private static GameObject _root;
        private static Transform _rowsParent;
        private static TextMeshProUGUI _resultLabel;
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
                EventDispatcher.On(GlobalEvent.EVT_DUNGEON_UPDATE, Rebuild);
                _listening = true;
            }
            Rebuild();
            GameLog.Info("Dungeon", "DungeonRuneShellView 打开: inDungeon={0}", DungeonModel.Instance.InDungeonId);
        }

        public static void Close()
        {
            if (_listening)
            {
                EventDispatcher.Off(GlobalEvent.EVT_DUNGEON_UPDATE, Rebuild);
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

            for (int i = 0; i < DunIds.Length; i++)
            {
                int dunId = DunIds[i];
                GameObject row = NewRow(i);

                TextMeshProUGUI label = NewText("Label", row.transform, 24, TextAlignmentOptions.MidlineLeft);
                var lrt = label.rectTransform;
                lrt.anchorMin = new Vector2(0f, 0f); lrt.anchorMax = new Vector2(1f, 1f);
                lrt.offsetMin = new Vector2(16f, 0f); lrt.offsetMax = new Vector2(-140f, 0f);
                label.text = DungeonConfigs.GetName(dunId)
                    + (DungeonModel.Instance.InDungeonId == dunId ? "　<color=#7fe27f>(当前副本)</color>" : "");

                NewButton(row.transform, "进入", -70f, new Color(0.22f, 0.42f, 0.24f), () => DungeonController.Instance.Enter(dunId));
            }

            DungeonModel model = DungeonModel.Instance;
            if (_resultLabel != null)
            {
                _resultLabel.text = model.HasData && model.LastSettleResult != 0
                    ? "最近结算:" + (model.LastSettleResult == 1 ? "通关" : "失败") + "(result=" + model.LastSettleResult + ")"
                        + "　奖励 " + model.LastSettleRewards.Count + " 项"
                    : "尚无结算记录";
            }
        }

        // ---- 构建(代码建 uGUI;同 PartnerShellView TEMP 壳约定)----

        private static void EnsureBuilt()
        {
            if (_root != null) return;
            Transform parent = ViewManager.GetLayer(UILayer.Window);
            if (parent == null)
            {
                GameLog.Error("Dungeon", "DungeonRuneShellView 无法构建:UI Window 层未就绪");
                return;
            }

            _root = NewRect("DungeonRuneShellView(TempShell)", parent, Vector2.zero, Vector2.one);

            GameObject bg = NewRect("Backdrop", _root.transform, Vector2.zero, Vector2.one);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.45f);
            bgImg.raycastTarget = true;
            UIUtil.AddClick(bgImg, Close);

            GameObject panel = NewRect("Panel", _root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var panelRt = (RectTransform)panel.transform;
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(560f, 640f);
            Image panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.07f, 0.09f, 0.14f, 0.97f);

            TextMeshProUGUI title = NewText("Title", panel.transform, 30, TextAlignmentOptions.Top);
            var trt = title.rectTransform;
            trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f); trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -20f); trt.sizeDelta = new Vector2(-40f, 44f);
            title.text = "御魂秘境";
            title.color = new Color(1f, 0.86f, 0.45f);
            title.fontStyle = FontStyles.Bold;

            GameObject rows = NewRect("Rows", panel.transform, new Vector2(0f, 0f), new Vector2(1f, 1f));
            var rrt = (RectTransform)rows.transform;
            rrt.offsetMin = new Vector2(12f, 140f); rrt.offsetMax = new Vector2(-12f, -76f);
            _rowsParent = rows.transform;

            GameObject resultRow = NewRect("Result", panel.transform, new Vector2(0f, 0f), new Vector2(1f, 0f));
            var resultRt = (RectTransform)resultRow.transform;
            resultRt.pivot = new Vector2(0.5f, 0f);
            resultRt.anchoredPosition = new Vector2(0f, 82f);
            resultRt.sizeDelta = new Vector2(-24f, 40f);
            _resultLabel = NewText("Label", resultRow.transform, 22, TextAlignmentOptions.Center);
            Stretch(_resultLabel.rectTransform);
            _resultLabel.color = new Color(0.85f, 0.85f, 0.9f);
            _resultLabel.text = "尚无结算记录";

            GameObject exitBtn = NewRect("Exit", panel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            var exitRt = (RectTransform)exitBtn.transform;
            exitRt.pivot = new Vector2(0.5f, 0f);
            exitRt.sizeDelta = new Vector2(200f, 56f);
            exitRt.anchoredPosition = new Vector2(-110f, 14f);
            Image exitImg = exitBtn.AddComponent<Image>();
            exitImg.color = new Color(0.42f, 0.24f, 0.20f, 1f);
            TextMeshProUGUI exitLbl = NewText("Label", exitBtn.transform, 26, TextAlignmentOptions.Center);
            Stretch(exitLbl.rectTransform);
            exitLbl.text = "退出副本";
            exitLbl.color = Color.white;
            UIUtil.AddClick(exitImg, () => DungeonController.Instance.Exit());

            GameObject closeBtn = NewRect("Close", panel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            var closeRt = (RectTransform)closeBtn.transform;
            closeRt.pivot = new Vector2(0.5f, 0f);
            closeRt.sizeDelta = new Vector2(200f, 56f);
            closeRt.anchoredPosition = new Vector2(110f, 14f);
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
