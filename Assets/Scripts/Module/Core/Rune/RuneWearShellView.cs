using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Rune
{
    /// <summary>
    /// 灵魄镶嵌临时壳(TEMP SHELL,同 PartnerShellView/GuBaoShellView 约定:代码建 uGUI、数据全真、样式从简待用户重做 UI)。
    /// 与同目录既有「九霄劫魄」转换 UI(RuneMainUIView 等)完全独立 —— 本壳服务第19轮工单 B:灵魄镶嵌最小闭环。
    /// 标题「灵魄镶嵌」+ 槽位1状态(未镶嵌/已镶嵌 goods_type_id 名) + 符文背包列表前 6 件(名+[镶嵌到槽1])。
    /// 主线 100990(ctype33)=镶嵌一次(孔位1无条件开放)由此进入。Show 时发 16700(全量)+ 15010(符文背包)。
    /// </summary>
    public static class RuneWearShellView
    {
        private const int WEAR_POS = 1;   // 孔位1(无条件开放,工单指定)

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
                EventDispatcher.On(GlobalEvent.EVT_RUNE_UPDATE, Rebuild);
                _listening = true;
            }
            RuneController.Instance.RequestInfo();
            RuneController.Instance.RequestRuneBag();
            Rebuild();
            GameLog.Info("Rune", "RuneWearShellView 打开: slots={0} bagGoods={1} hasData={2}",
                RuneModel.Instance.Slots.Count, RuneModel.Instance.RuneBagGoods.Count, RuneModel.Instance.HasData);
        }

        public static void Close()
        {
            if (_listening)
            {
                EventDispatcher.Off(GlobalEvent.EVT_RUNE_UPDATE, Rebuild);
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

            RuneModel model = RuneModel.Instance;

            // 槽位1状态行
            GameObject slotRow = NewRow(0);
            TextMeshProUGUI slotLabel = NewText("Label", slotRow.transform, 24, TextAlignmentOptions.MidlineLeft);
            var slotLrt = slotLabel.rectTransform;
            slotLrt.anchorMin = new Vector2(0f, 0f); slotLrt.anchorMax = new Vector2(1f, 1f);

            RuneModel.SlotVo slot1 = model.HasData ? model.GetSlot(WEAR_POS) : null;
            if (!model.HasData)
            {
                slotLrt.offsetMin = new Vector2(16f, 0f); slotLrt.offsetMax = new Vector2(-16f, 0f);
                slotLabel.text = "等待 16700/15010(需活服)";
                slotLabel.color = new Color(0.65f, 0.72f, 0.85f);
            }
            else if (slot1 == null || !slot1.IsWorn)
            {
                slotLrt.offsetMin = new Vector2(16f, 0f); slotLrt.offsetMax = new Vector2(-16f, 0f);
                slotLabel.text = "槽位1:<color=#8893a6>未镶嵌</color>" + (slot1 != null && !slot1.IfOpen ? "(未开放)" : "");
                slotLabel.color = Color.white;
            }
            else
            {
                // 已镶嵌:让出右侧空间挂[强化]按钮(对标工单「已镶嵌槽位行加[强化]按钮」)。
                slotLrt.offsetMin = new Vector2(16f, 0f); slotLrt.offsetMax = new Vector2(-160f, 0f);
                slotLabel.text = "槽位1:<color=#ffe222>已镶嵌 " + GoodsModel.GetGoodsName(slot1.GoodsTypeId) + "</color>";
                slotLabel.color = Color.white;

                long wornGoodsId = slot1.GoodsId;
                NewButton(slotRow.transform, "强化", -80f, new Color(0.42f, 0.33f, 0.18f),
                    () => RuneController.Instance.Upgrade(wornGoodsId));
            }

            // 符文背包列表前 6 件
            if (!model.HasRuneBag || model.RuneBagGoods.Count == 0)
            {
                GameObject tip = NewRow(1);
                TextMeshProUGUI txt = NewText("Info", tip.transform, 22, TextAlignmentOptions.Center);
                Stretch(txt.rectTransform);
                txt.text = model.HasRuneBag ? "符文背包为空" : "等待 16700/15010(需活服)";
                txt.color = new Color(0.65f, 0.72f, 0.85f);
                return;
            }

            int shown = Mathf.Min(6, model.RuneBagGoods.Count);
            for (int i = 0; i < shown; i++)
            {
                RuneModel.BagGoodsVo vo = model.RuneBagGoods[i];
                GameObject row = NewRow(1 + i);

                TextMeshProUGUI label = NewText("Label", row.transform, 22, TextAlignmentOptions.MidlineLeft);
                var lrt = label.rectTransform;
                lrt.anchorMin = new Vector2(0f, 0f); lrt.anchorMax = new Vector2(1f, 1f);
                lrt.offsetMin = new Vector2(16f, 0f); lrt.offsetMax = new Vector2(-160f, 0f);
                label.text = GoodsModel.GetGoodsName(vo.TypeId) + (vo.Num > 1 ? " x" + vo.Num : "");

                long goodsId = vo.GoodsId;
                NewButton(row.transform, "镶嵌到槽1", -80f, new Color(0.22f, 0.42f, 0.24f),
                    () => RuneController.Instance.Wear(WEAR_POS, goodsId));
            }
        }

        // ---- 构建(代码建 uGUI;同 PartnerShellView TEMP 壳约定)----

        private static void EnsureBuilt()
        {
            if (_root != null) return;
            Transform parent = ViewManager.GetLayer(UILayer.Window);
            if (parent == null)
            {
                GameLog.Error("Rune", "RuneWearShellView 无法构建:UI Window 层未就绪");
                return;
            }

            _root = NewRect("RuneWearShellView(TempShell)", parent, Vector2.zero, Vector2.one);

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
            title.text = "灵魄镶嵌";
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
