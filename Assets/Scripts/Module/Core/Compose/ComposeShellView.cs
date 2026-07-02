using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Compose
{
    /// <summary>
    /// 神装合成临时壳(TEMP SHELL,同 GuBaoShellView/RuneWearShellView 约定:代码建 uGUI、数据全真、样式从简待用户重做 UI)。
    /// 列 config_goods_compose 中 type==2(装备类)规则前 6 条:名称(产物 goods 名经 GoodsModel)+[合成]按钮。
    ///
    /// 材料匹配逻辑(工单最小化范围,如实标注不臆造):只按规则 <see cref="ComposeConfigs.Rule.RegularMat"/>(列8固定材料)
    /// 的 type_id 在 <see cref="BagModel.BagGoodsList"/> 里找同 TypeId 的 goods_id,数量需 ≥ MatEntry.Num。
    /// ⚠实证:config_goods_compose 内 954 条 type==2 规则里,仅 288 条 RegularMat 非空,其余 666 条(含本壳按
    /// JSON 原始键序取到的「前 6 条」)RegularMat 为空表——这些规则实际材料来自 irregular_mat(候选池选任一),
    /// 工单范围未要求实现该匹配,故 RegularMat 为空的规则如实显示「本规则材料为 irregular_mat(未接匹配)」,
    /// 不伪造成"材料充足"或瞎选。凑不齐(或不支持匹配)时点击[合成]按钮 Toast「材料不足(需 X)」。
    /// 主线 101725(ctype73)由此进入。
    /// </summary>
    public static class ComposeShellView
    {
        private const int SHOW_COUNT = 6;   // 工单指定:前 6 条 type=2 规则

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
                EventDispatcher.On(GlobalEvent.EVT_COMPOSE_UPDATE, Rebuild);
                _listening = true;
            }
            Rebuild();
            GameLog.Info("Compose", "ComposeShellView 打开: configLoaded={0}", ComposeConfigs.IsLoaded);
        }

        public static void Close()
        {
            if (_listening)
            {
                EventDispatcher.Off(GlobalEvent.EVT_COMPOSE_UPDATE, Rebuild);
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

            if (!ComposeConfigs.IsLoaded)
            {
                GameObject tip = NewRow(0);
                TextMeshProUGUI txt = NewText("Info", tip.transform, 24, TextAlignmentOptions.Center);
                Stretch(txt.rectTransform);
                txt.text = "等待 config_goods_compose 加载";
                txt.color = new Color(0.65f, 0.72f, 0.85f);
                return;
            }

            List<ComposeConfigs.Rule> rules = ComposeConfigs.GetEquipRules(SHOW_COUNT);
            if (rules.Count == 0)
            {
                GameObject tip = NewRow(0);
                TextMeshProUGUI txt = NewText("Info", tip.transform, 24, TextAlignmentOptions.Center);
                Stretch(txt.rectTransform);
                txt.text = "无 type=2 装备类合成规则";
                txt.color = new Color(0.65f, 0.72f, 0.85f);
                return;
            }

            for (int i = 0; i < rules.Count; i++)
            {
                ComposeConfigs.Rule rule = rules[i];
                GameObject row = NewRow(i);

                string productName = rule.Goods.Count > 0 ? GoodsModel.GetGoodsName(rule.Goods[0].TypeId) : "";
                if (string.IsNullOrEmpty(productName)) productName = rule.Name;

                TextMeshProUGUI label = NewText("Label", row.transform, 22, TextAlignmentOptions.MidlineLeft);
                var lrt = label.rectTransform;
                lrt.anchorMin = new Vector2(0f, 0f); lrt.anchorMax = new Vector2(1f, 1f);
                lrt.offsetMin = new Vector2(16f, 0f); lrt.offsetMax = new Vector2(-160f, 0f);

                bool hasRegularMat = rule.RegularMat.Count > 0;
                (bool enough, string need) = hasRegularMat ? CheckMaterials(rule) : (false, "");
                string matDesc = hasRegularMat
                    ? (enough ? "<color=#5ce27a>材料充足</color>" : "<color=#e2645c>材料不足(需" + need + ")</color>")
                    : "<color=#8893a6>材料=irregular_mat(未接匹配)</color>";
                label.text = "[" + rule.Id + "] " + productName + "　" + matDesc;

                ComposeConfigs.Rule capturedRule = rule;
                NewButton(row.transform, "合成", -80f, new Color(0.22f, 0.42f, 0.24f), () => OnComposeClick(capturedRule));
            }
        }

        /// <summary>点击[合成]:材料齐→真发 15020;不齐或规则未接匹配(irregular_mat)→ Toast 提示,不发包。</summary>
        private static void OnComposeClick(ComposeConfigs.Rule rule)
        {
            if (rule.RegularMat.Count == 0)
            {
                TipsManager.Toast("该规则材料为 irregular_mat,未接匹配逻辑");
                return;
            }
            (bool enough, string need) = CheckMaterials(rule);
            if (!enough)
            {
                TipsManager.Toast("材料不足(需" + need + ")");
                return;
            }
            List<long> regulars = ResolveMaterialGoodsIds(rule);
            ComposeController.Instance.Compose(rule.Id, regulars, new List<long>());
        }

        /// <summary>
        /// 材料匹配(工单最小化范围):regular_mat 每项按 type_id 在 BagModel.BagGoodsList 找同 TypeId 且数量够的记录。
        /// 只做「是否凑齐」判定;真正扣减/堆叠拆分由服务端 15020 处理,本壳不模拟背包扣减。
        /// </summary>
        private static (bool enough, string needDesc) CheckMaterials(ComposeConfigs.Rule rule)
        {
            var needDescParts = new List<string>();
            bool allEnough = true;
            foreach (ComposeConfigs.MatEntry mat in rule.RegularMat)
            {
                long have = 0;
                foreach (BagGoods g in BagModel.Instance.BagGoodsList)
                    if (g.TypeId == mat.TypeId) have += g.GoodsNum;
                if (have < mat.Num)
                {
                    allEnough = false;
                    string name = GoodsModel.GetGoodsName(mat.TypeId);
                    needDescParts.Add((string.IsNullOrEmpty(name) ? "物品" + mat.TypeId : name) + "×" + mat.Num);
                }
            }
            return (allEnough, string.Join("、", needDescParts));
        }

        /// <summary>凑齐后按 regular_mat 的 type_id 逐项在背包取第一件匹配的 goods_id(实例主键,15020 WriteFMT("l",goods_id) 用)。</summary>
        private static List<long> ResolveMaterialGoodsIds(ComposeConfigs.Rule rule)
        {
            var result = new List<long>();
            foreach (ComposeConfigs.MatEntry mat in rule.RegularMat)
            {
                foreach (BagGoods g in BagModel.Instance.BagGoodsList)
                {
                    if (g.TypeId == mat.TypeId)
                    {
                        result.Add(g.GoodsId);
                        break;
                    }
                }
            }
            return result;
        }

        // ---- 构建(代码建 uGUI;同 GuBaoShellView TEMP 壳约定)----

        private static void EnsureBuilt()
        {
            if (_root != null) return;
            Transform parent = ViewManager.GetLayer(UILayer.Window);
            if (parent == null)
            {
                GameLog.Error("Compose", "ComposeShellView 无法构建:UI Window 层未就绪");
                return;
            }

            _root = NewRect("ComposeShellView(TempShell)", parent, Vector2.zero, Vector2.one);

            GameObject bg = NewRect("Backdrop", _root.transform, Vector2.zero, Vector2.one);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.45f);
            bgImg.raycastTarget = true;
            UIUtil.AddClick(bgImg, Close);

            GameObject panel = NewRect("Panel", _root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var panelRt = (RectTransform)panel.transform;
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(620f, 640f);
            Image panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.07f, 0.09f, 0.14f, 0.97f);

            TextMeshProUGUI title = NewText("Title", panel.transform, 30, TextAlignmentOptions.Top);
            var trt = title.rectTransform;
            trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f); trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -20f); trt.sizeDelta = new Vector2(-40f, 44f);
            title.text = "神装合成";
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
