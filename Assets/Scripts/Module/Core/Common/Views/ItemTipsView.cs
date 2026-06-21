using System.Collections.Generic;
using System.Text;
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
    /// 物品详情 tips —— 临时原生 uGUI 壳(TEMP SHELL),对标老端 common/UIToolTipMgr.DefaultAppendTips →
    /// GoodsTooltips(普通物品)/ EquipToolTips(装备 type==10)。
    ///
    /// 链路:点任意 <see cref="BaseAwardItem"/> 物品格(完成弹层/背包),未设点击回调 → 默认弹本 tips(对标 UIToolTipMgr 默认分支);
    /// 数量由格子透传(<see cref="BaseAwardItem.OnClick"/> → Show(typeId,num))。
    /// ★数据全为真★,均来自 config 经 <see cref="GoodsModel"/> 解出:
    ///   · 名=key"1" / 图标=key"14" / 品质=key"18" / 描述=key"2"
    ///   · 类型文本=GoodsType[type"9"].type_name(对标 GoodsTooltips.type_text=WordManager.GetGoodsStyle)
    ///   · 数量=透传的堆叠数(对标 GoodsTooltips.quantity_text)
    ///   · 获取途径=key"3" getway(对标 GoodsTooltips.ways=basic.getway)
    ///   · 装备(type==10):基础属性=base_attrlist key"26" 经 ErlangParser + ConfigItemAttr 取真名(对标 EquipToolTips basePro);
    ///     部位=equip_type key"13"、阶/评分=config_equip_attr、等级需求=key"16"、职业=career_id key"15"(对标 EquipToolTips)。
    /// 图标 + 品质底板【复用通用 BaseAwardItem.prefab】(同 TaskFinishView:InstantiateAsync + SetData)。
    /// 装备「实例」属性(极品 equip_extra_attr / 强化 stren)需活服实装备 + 实例透传到 tips,本轮只显 config 基础属性(精确 blocker,不画假属性)。
    /// 老端 GoodsTooltips.lh/EquipToolTips.lh 无 Unity 转换产物,故按任务包许可做最小原生壳(同 TaskFinishView TEMP 壳约定);
    /// 字体复用场景中已打开文本的 TMP 字体(含中文字形)。
    /// </summary>
    public static class ItemTipsView
    {
        private static GameObject _root;
        private static TextMeshProUGUI _nameText;
        private static TextMeshProUGUI _bodyText;
        private static RectTransform _iconSlot;
        private static GameObject _iconCell;
        private static int _epoch;

        private static TMP_FontAsset _font;
        private static Material _fontMat;

        /// <summary>弹物品详情(对标 UIToolTipMgr.DefaultAppendTips):typeId 不在 config_goods 则不弹(对标 if(!basic) return)。
        /// num=堆叠数量(对标 GoodsTooltips quantity_text,由格子透传;默认 1)。</summary>
        public static void Show(int typeId, long num = 1)
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
            _bodyText.text = BuildBody(typeId, num, basic);

            _ = BuildIcon(typeId);
            GameLog.Info("Common", "ItemTips 打开: typeId={0} '{1}' type={2}({3}) color={4} num={5} equip={6}",
                typeId, basic.Name, basic.Type, GoodsModel.GetGoodsTypeName(basic.Type), basic.Color, num, GoodsModel.IsEquip(typeId));
        }

        public static void Close()
        {
            _epoch++;
            if (_iconCell != null) { ResManager.ReleaseInstance(_iconCell); _iconCell = null; }
            if (_root != null) _root.SetActive(false);
        }

        /// <summary>
        /// 组装详情正文(对标 GoodsTooltips/EquipToolTips 的字段拼装):类型 + 数量 → 装备(基础属性 + 部位/阶/等级/职业)或
        /// 普通物品(描述 intro)→ 获取途径。全字段真实 config 驱动,缺则跳过(不占位、不臆造)。
        /// </summary>
        private static string BuildBody(int typeId, long num, GoodsModel.GoodsBasic basic)
        {
            var sb = new StringBuilder();

            // —— 类型 + 数量(对标 GoodsTooltips type_text / quantity_text)——
            string typeName = GoodsModel.GetGoodsTypeName(basic.Type);
            var head = new List<string>();
            if (!string.IsNullOrEmpty(typeName)) head.Add("类型：<color=#ffe222>" + typeName + "</color>");
            head.Add("数量：<color=#ffe222>" + num + "</color>");
            sb.Append(string.Join("    ", head));

            if (GoodsModel.IsEquip(typeId))
                AppendEquip(sb, typeId, basic);
            else
                AppendNormal(sb, basic);

            // —— 获取途径(对标 GoodsTooltips ways=basic.getway,key "3";空 / "[]" 空列表占位则不显)——
            string getway = basic.Getway?.Trim();
            if (!string.IsNullOrEmpty(getway) && getway != "[]")
                sb.Append("\n\n<color=#7fd0ff>获取途径：</color>").Append(ToTmpRich(getway));

            return sb.ToString();
        }

        /// <summary>普通物品:描述 intro(对标 GoodsTooltips else 分支 basic.intro)。</summary>
        private static void AppendNormal(StringBuilder sb, GoodsModel.GoodsBasic basic)
        {
            string intro = ToTmpRich(basic.Intro);
            sb.Append("\n\n").Append(string.IsNullOrEmpty(intro) ? "<color=#8893a6>(暂无描述)</color>" : intro);
        }

        /// <summary>装备(type==10):部位/阶/等级/职业/评分 + 基础属性行(对标 EquipToolTips pos/grade/level/career/basePro)。</summary>
        private static void AppendEquip(StringBuilder sb, int typeId, GoodsModel.GoodsBasic basic)
        {
            GoodsModel.EquipAttr ea = GoodsModel.GetEquipAttr(typeId);

            // 部位 + 阶(对标 EquipToolTips pos=GetEquipPos / grade=`${stage}阶`)
            var meta = new List<string>();
            string pos = GoodsModel.GetEquipPosName(basic.EquipType);
            if (!string.IsNullOrEmpty(pos)) meta.Add("部位：<color=#ffe222>" + pos + "</color>");
            if (ea != null && ea.Stage > 0)
                meta.Add("<color=#ffe222>" + ea.Stage + "阶" + (ea.Star > 0 ? ea.Star + "星" : "") + "</color>");
            if (basic.Level > 0) meta.Add("等级需求：<color=#ffe222>" + basic.Level + "</color>");
            meta.Add("职业：<color=#ffe222>" + GoodsModel.GetCareerName(basic.CareerId) + "</color>");
            if (meta.Count > 0) sb.Append("\n").Append(string.Join("    ", meta));
            if (ea != null && ea.BaseRating > 0)
                sb.Append("\n评分：<color=#ffef67>").Append(ea.BaseRating).Append("</color>");

            // 基础属性行(对标 EquipToolTips basePro:base_attrlist 逐项 GetProperties+值)
            List<(string name, long val)> attrs = GoodsModel.GetBaseAttrs(typeId);
            sb.Append("\n\n<color=#7fd0ff>【基础属性】</color>");
            if (attrs.Count == 0)
            {
                sb.Append("\n<color=#8893a6>(该装备 config 无基础属性)</color>");
            }
            else
            {
                foreach ((string name, long val) in attrs)
                    sb.Append("\n").Append(name).Append("　<color=#d15e00>+").Append(val).Append("</color>");
            }

            // 实例属性(极品/强化)需活服实装备 + 实例透传 → 本轮精确 blocker(不画假属性)
            sb.Append("\n<color=#8893a6>(极品/强化等实例属性需登录活服取实装备)</color>");

            // 描述附在属性之后(若有)
            string intro = ToTmpRich(basic.Intro);
            if (!string.IsNullOrEmpty(intro)) sb.Append("\n\n").Append(intro);
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
            panelRt.sizeDelta = new Vector2(480f, 560f);
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

            // 正文(类型/数量 + 装备属性 或 描述 + 获取途径)
            _bodyText = NewText("Body", panel.transform, 22, TextAlignmentOptions.TopLeft);
            var bodyRt = _bodyText.rectTransform;
            bodyRt.anchorMin = new Vector2(0f, 0f); bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.offsetMin = new Vector2(28f, 70f); bodyRt.offsetMax = new Vector2(-28f, -218f);
            _bodyText.textWrappingMode = TextWrappingModes.Normal;
            _bodyText.color = new Color(0.86f, 0.91f, 1f);

            // 关闭按钮(底部居中)
            GameObject closeBtn = NewRect("Close", panel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            var closeRt = (RectTransform)closeBtn.transform;
            closeRt.pivot = new Vector2(0.5f, 0f);
            closeRt.sizeDelta = new Vector2(200f, 56f);
            closeRt.anchoredPosition = new Vector2(0f, 18f);
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
