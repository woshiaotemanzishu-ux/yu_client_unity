using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.OutWard
{
    /// <summary>
    /// 幻化外观临时壳(TEMP SHELL,同 PartnerShellView/ItemTipsView 约定:代码建 uGUI、数据全真、样式从简待用户重做 UI)。
    /// 固定两行(坐骑 type_id=1 / 剑魄同修 type_id=2,系统名硬字面——非配置数据):
    ///   名字 + 「N阶M星 祝福X/Y」(系统A,16002/16023 真实回包) + 「等级L(经验E)」(系统B,16028/16029 真实回包)
    ///   + 按钮[升星](StarUp)与[升级](LvUp)。无数据时如实显示等待 16002/16028,不造假。
    /// DoTask ctype23(100330)/ctype90(100521/100901)由此进入。
    /// </summary>
    public static class OutWardShellView
    {
        private static readonly (int typeId, string name)[] ROWS =
        {
            (1, "坐骑"),
            (2, "剑魄同修"),
        };

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
                EventDispatcher.On(GlobalEvent.EVT_OUTWARD_UPDATE, Rebuild);
                _listening = true;
            }
            Rebuild();
            GameLog.Info("OutWard", "OutWardShellView 打开");
        }

        public static void Close()
        {
            if (_listening)
            {
                EventDispatcher.Off(GlobalEvent.EVT_OUTWARD_UPDATE, Rebuild);
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

            for (int i = 0; i < ROWS.Length; i++)
            {
                (int typeId, string name) = ROWS[i];
                OutWardModel.OutWardVo vo = OutWardModel.Instance.Get(typeId);
                GameObject row = NewRow(i);

                TextMeshProUGUI label = NewText("Label", row.transform, 22, TextAlignmentOptions.MidlineLeft);
                var lrt = label.rectTransform;
                lrt.anchorMin = new Vector2(0f, 0f); lrt.anchorMax = new Vector2(1f, 1f);
                lrt.offsetMin = new Vector2(16f, 0f); lrt.offsetMax = new Vector2(-240f, 0f);
                label.text = BuildRowText(name, vo);

                bool canStarUp = vo != null;
                bool canLvUp = vo != null && vo.HasLv;
                NewButton(row.transform, "升星", -180f, canStarUp,
                    new Color(0.22f, 0.42f, 0.24f), () => OutWardController.Instance.StarUp(typeId));
                NewButton(row.transform, "升级", -70f, canLvUp,
                    new Color(0.20f, 0.30f, 0.48f), () => OutWardController.Instance.LvUp(typeId));
            }
        }

        private static string BuildRowText(string name, OutWardModel.OutWardVo vo)
        {
            if (vo == null)
            {
                return name + "　<color=#8893a6>等待 16002/16028(需活服)</color>";
            }
            long maxBlessing = OutWardConfigs.GetMaxBlessing(vo.TypeId, vo.Stage, vo.Star);
            string stageStar = "<color=#ffe222>" + vo.Stage + "阶" + vo.Star + "星</color>"
                + "　祝福 " + vo.Blessing + (maxBlessing > 0 ? "/" + maxBlessing : "");
            string lvText = vo.HasLv
                ? "　等级 " + vo.Level + "(经验 " + vo.CurExp + ")"
                : "　<color=#8893a6>等待 16028</color>";
            return name + "　" + stageStar + lvText;
        }

        // ---- 构建(代码建 uGUI;同 PartnerShellView TEMP 壳约定)----

        private static void EnsureBuilt()
        {
            if (_root != null) return;
            Transform parent = ViewManager.GetLayer(UILayer.Window);
            if (parent == null)
            {
                GameLog.Error("OutWard", "OutWardShellView 无法构建:UI Window 层未就绪");
                return;
            }

            _root = NewRect("OutWardShellView(TempShell)", parent, Vector2.zero, Vector2.one);

            GameObject bg = NewRect("Backdrop", _root.transform, Vector2.zero, Vector2.one);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.45f);
            bgImg.raycastTarget = true;
            UIUtil.AddClick(bgImg, Close);

            GameObject panel = NewRect("Panel", _root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var panelRt = (RectTransform)panel.transform;
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(620f, 420f);
            Image panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.07f, 0.09f, 0.14f, 0.97f);

            TextMeshProUGUI title = NewText("Title", panel.transform, 30, TextAlignmentOptions.Top);
            var trt = title.rectTransform;
            trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f); trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -20f); trt.sizeDelta = new Vector2(-40f, 44f);
            title.text = "坐骑/同修培养";
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
            rt.sizeDelta = new Vector2(0f, 84f);
            rt.anchoredPosition = new Vector2(0f, -index * 90f);
            Image bg = row.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.05f);
            bg.raycastTarget = false;
            _rows.Add(row);
            return row;
        }

        private static void NewButton(Transform parent, string text, float x, bool interactable, Color color, System.Action onClick)
        {
            var go = new GameObject("Btn" + text, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(96f, 48f);
            rt.anchoredPosition = new Vector2(x + 48f, 0f);
            Image img = go.AddComponent<Image>();
            img.color = interactable ? color : new Color(0.3f, 0.3f, 0.33f, 0.6f);
            img.raycastTarget = interactable;
            TextMeshProUGUI lbl = NewText("Label", go.transform, 22, TextAlignmentOptions.Center);
            Stretch(lbl.rectTransform);
            lbl.text = text;
            lbl.color = interactable ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.7f);
            if (interactable) UIUtil.AddClick(img, () => onClick?.Invoke());
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
