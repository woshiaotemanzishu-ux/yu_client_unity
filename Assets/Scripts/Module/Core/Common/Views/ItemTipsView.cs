using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Common
{
    /// <summary>
    /// 物品详情 tips —— 临时原生 uGUI 壳(TEMP SHELL),对标老端 common/UIToolTipMgr.AppendGoodsTips → GoodsTooltips。
    ///
    /// 链路:点任意 <see cref="BaseAwardItem"/> 物品格(完成弹层/背包),未设点击回调 → 默认弹本 tips(对标 UIToolTipMgr 默认分支)。
    /// ★数据全为真★:名/图标/品质底板/描述均来自 config_goods 经 <see cref="GoodsModel"/> 解出
    /// (名=key"1" goods_name,图标=key"14" goods_icon,品质=key"18" color→com_goods_plate_{color},描述=key"2" intro)。
    /// 图标 + 品质底板【复用通用 BaseAwardItem.prefab】(同 TaskFinishView:InstantiateAsync + SetData,真实图标+底板);
    /// 描述 intro 原文含 Laya HTML(&lt;br/&gt;/&lt;font color&gt;)→ <see cref="ToTmpRich"/> 转 TMP 富文本。
    /// 老端 GoodsTooltips.lh 无 Unity 转换产物(无 Bind/prefab),故按任务包许可做最小原生壳(同 TaskFinishView TEMP 壳约定);
    /// 字体复用场景中已打开文本的 TMP 字体(含中文字形)。装备详情(属性/按钮)/来源/使用按钮等待装备系统移植,本轮只做通用物品详情。
    /// </summary>
    public static class ItemTipsView
    {
        private static GameObject _root;
        private static TextMeshProUGUI _nameText;
        private static TextMeshProUGUI _introText;
        private static RectTransform _iconSlot;
        private static GameObject _iconCell;
        private static int _epoch;

        private static TMP_FontAsset _font;
        private static Material _fontMat;

        /// <summary>弹物品详情(对标 UIToolTipMgr.AppendGoodsTips):typeId 不在 config_goods 则不弹(对标 if(!basic) return)。</summary>
        public static void Show(int typeId)
        {
            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(typeId);
            if (basic == null)
            {
                GameLog.Warn("Common", "ItemTips: typeId={0} 不在 config_goods(或未加载)→ 不弹详情", typeId);
                return;
            }

            EnsureBuilt();
            if (_root == null) return;
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();

            _nameText.text = string.IsNullOrEmpty(basic.Name) ? ("#" + typeId) : basic.Name;
            string intro = ToTmpRich(basic.Intro);
            _introText.text = string.IsNullOrEmpty(intro) ? "<color=#8893a6>(暂无描述)</color>" : intro;

            _ = BuildIcon(typeId);
            GameLog.Info("Common", "ItemTips 打开: typeId={0} '{1}' color={2} intro={3}字",
                typeId, basic.Name, basic.Color, basic.Intro?.Length ?? 0);
        }

        public static void Close()
        {
            _epoch++;
            if (_iconCell != null) { ResManager.ReleaseInstance(_iconCell); _iconCell = null; }
            if (_root != null) _root.SetActive(false);
        }

        // 图标 + 品质底板:复用 BaseAwardItem.prefab(真实图标 + com_goods_plate_{color}),epoch 防重开/关闭竞态。
        private static async Task BuildIcon(int typeId)
        {
            int epoch = ++_epoch;
            if (_iconCell != null) { ResManager.ReleaseInstance(_iconCell); _iconCell = null; }
            if (_iconSlot == null) return;

            GameObject go = await ResManager.InstantiateAsync(GameResPath.GetUIPrefab("common", "BaseAwardItem"), _iconSlot);
            if (epoch != _epoch) { if (go != null) ResManager.ReleaseInstance(go); return; }
            if (go == null)
            {
                GameLog.Warn("Common", "ItemTips: 复用 BaseAwardItem 失败(prefab 未导入/未分组?) typeId={0}", typeId);
                return;
            }
            go.SetActive(true);
            var cell = go.GetComponent<BaseAwardItem>();
            if (cell == null)
            {
                GameLog.Warn("Common", "ItemTips: BaseAwardItem.prefab 根缺 BaseAwardItem 组件(跑 神霄/UI/回填 Bind 组件)typeId={0}", typeId);
                ResManager.ReleaseInstance(go);
                return;
            }
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            cell.SetClickCallBack(() => { }); // tips 内的图标不再二次弹 tips(避免递归)
            cell.SetData(typeId, 1);
            _iconCell = go;
        }

        /// <summary>Laya HTML → TMP 富文本:&lt;br/&gt;→换行,&lt;font color='#x'&gt;→&lt;color=#x&gt;,&lt;/font&gt;→&lt;/color&gt;。</summary>
        private static string ToTmpRich(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = Regex.Replace(s, "<br\\s*/?>", "\n", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, "<font\\s+color=['\"]?(#?[0-9a-fA-F]+)['\"]?\\s*>", "<color=$1>", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, "</font>", "</color>", RegexOptions.IgnoreCase);
            return s;
        }

        // ===================== 构建(代码建 uGUI,居中弹层;同 TaskFinishView TEMP 壳)=====================

        private static void EnsureBuilt()
        {
            if (_root != null) return;
            Transform parent = ViewManager.GetLayer(UILayer.Popup);
            if (parent == null)
            {
                GameLog.Error("Common", "ItemTipsView 无法构建:UI Popup 层未就绪");
                return;
            }

            _root = NewRect("ItemTipsView(TempShell)", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject bg = NewRect("Backdrop", _root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.45f);
            bgImg.raycastTarget = true;
            UIUtil.AddClick(bgImg, Close);

            GameObject panel = NewRect("Panel", _root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var panelRt = (RectTransform)panel.transform;
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(480f, 420f);
            panelRt.anchoredPosition = Vector2.zero;
            Image panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.07f, 0.09f, 0.14f, 0.97f);

            // 物品名(顶部居中)
            _nameText = NewText("Name", panel.transform, 30, TextAlignmentOptions.Top);
            var nameRt = _nameText.rectTransform;
            nameRt.anchorMin = new Vector2(0f, 1f); nameRt.anchorMax = new Vector2(1f, 1f); nameRt.pivot = new Vector2(0.5f, 1f);
            nameRt.anchoredPosition = new Vector2(0f, -20f); nameRt.sizeDelta = new Vector2(-40f, 44f);
            _nameText.color = new Color(1f, 0.86f, 0.45f);
            _nameText.fontStyle = FontStyles.Bold;

            // 图标位(品质底板 + 真实图标,复用 BaseAwardItem,127px)
            GameObject slot = NewRect("IconSlot", panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            _iconSlot = (RectTransform)slot.transform;
            _iconSlot.pivot = new Vector2(0.5f, 1f);
            _iconSlot.sizeDelta = new Vector2(127f, 127f);
            _iconSlot.anchoredPosition = new Vector2(0f, -76f);

            // 描述(intro)
            _introText = NewText("Intro", panel.transform, 23, TextAlignmentOptions.TopLeft);
            var introRt = _introText.rectTransform;
            introRt.anchorMin = new Vector2(0f, 0f); introRt.anchorMax = new Vector2(1f, 1f);
            introRt.offsetMin = new Vector2(28f, 70f); introRt.offsetMax = new Vector2(-28f, -224f);
            _introText.textWrappingMode = TextWrappingModes.Normal;
            _introText.color = new Color(0.86f, 0.91f, 1f);

            // 关闭按钮(底部居中)
            GameObject closeBtn = NewRect("Close", panel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            var closeRt = (RectTransform)closeBtn.transform;
            closeRt.pivot = new Vector2(0.5f, 0f);
            closeRt.sizeDelta = new Vector2(200f, 60f);
            closeRt.anchoredPosition = new Vector2(0f, 24f);
            Image closeImg = closeBtn.AddComponent<Image>();
            closeImg.color = new Color(0.20f, 0.30f, 0.48f, 1f);
            TextMeshProUGUI closeLbl = NewText("Label", closeBtn.transform, 26, TextAlignmentOptions.Center);
            var clRt = closeLbl.rectTransform;
            clRt.anchorMin = Vector2.zero; clRt.anchorMax = Vector2.one; clRt.offsetMin = Vector2.zero; clRt.offsetMax = Vector2.zero;
            closeLbl.text = "关闭";
            closeLbl.color = Color.white;
            UIUtil.AddClick(closeImg, Close);
        }

        // ---- uGUI 构建小工具(同 TaskFinishView 的 TEMP 壳约定)----

        private static GameObject NewRect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = offMin; rt.offsetMax = offMax;
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

        private static void ApplyFont(TextMeshProUGUI t)
        {
            if (_font == null)
            {
                TextMeshProUGUI src = Object.FindAnyObjectByType<TextMeshProUGUI>();
                if (src != null) { _font = src.font; _fontMat = src.fontSharedMaterial; }
            }
            if (_font != null) t.font = _font;
            if (_fontMat != null) t.fontSharedMaterial = _fontMat;
        }
    }
}
