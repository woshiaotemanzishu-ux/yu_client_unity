using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Common;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Role;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Common
{
    /// <summary>
    /// 物品/装备详情。层级、遮罩、单面板和对比双面板位置全部来自 CommonModule.prefab；
    /// 本类只负责数据、显隐、点击和协议。装备字段顺序/颜色/分组对标老端 EquipToolTips。
    /// </summary>
    public static class ItemTipsView
    {
        public enum ItemContext
        {
            Bag,
            Equipped,
            WarehouseBag,
            WarehouseStorage,
        }

        private enum EquipButtons
        {
            None,
            Wear,
            Close,
            Deposit,
            Takeout,
        }

        private static GameObject _moduleRoot;
        private static ItemTipsModalLayout _layout;
        private static GameObject _normalIconCell;
        private static BagGoods _goods;
        private static ItemContext _context;
        private static int _epoch;

        public static void Show(int typeId, long num = 1) => _ = ShowInternal(typeId, num, null, ItemContext.Bag);

        public static void Show(BagGoods goods)
        {
            if (goods != null) _ = ShowInternal(goods.TypeId, goods.GoodsNum, goods, ItemContext.Bag);
        }

        public static void ShowEquipped(BagGoods goods)
        {
            if (goods != null) _ = ShowInternal(goods.TypeId, goods.GoodsNum, goods, ItemContext.Equipped);
        }

        public static void ShowWarehouse(BagGoods goods, bool inStorage)
        {
            if (goods != null)
                _ = ShowInternal(goods.TypeId, goods.GoodsNum, goods,
                    inStorage ? ItemContext.WarehouseStorage : ItemContext.WarehouseBag);
        }

        private static async Task ShowInternal(int typeId, long num, BagGoods goods, ItemContext context)
        {
            int epoch = ++_epoch;
            await Task.WhenAll(GoodsModel.EnsureLoaded(), EquipmentTipsConfig.EnsureLoaded(), FuncOpenConfig.EnsureLoaded());
            if (epoch != _epoch) return;

            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(typeId);
            if (basic == null)
            {
                GameLog.Warn("Common", "ItemTips: typeId={0} 不在 config_goods，不打开详情", typeId);
                return;
            }

            if (!await EnsureBuilt() || epoch != _epoch) return;

            _goods = goods;
            _context = context;
            PrepareModule();

            if (GoodsModel.IsEquip(typeId))
                ShowEquipment(basic, goods, context, epoch);
            else
                ShowGoods(basic, num, goods, context, epoch);
        }

        private static void PrepareModule()
        {
            _moduleRoot.SetActive(true);
            _moduleRoot.transform.SetAsLastSibling();
            HideView(_layout.goods);
            HideView(_layout.equipSingle);
            HideView(_layout.compareCurrent);
            HideView(_layout.compareCandidate);
            _layout.dimBlocker.gameObject.SetActive(false);
            _layout.compareBlocker.gameObject.SetActive(false);
        }

        private static void ShowEquipment(GoodsModel.GoodsBasic candidateBasic, BagGoods candidate,
            ItemContext context, int epoch)
        {
            BagGoods current = context == ItemContext.Bag && candidate != null && candidateBasic.EquipType > 0
                ? BagModel.Instance.GetEquipmentAt(candidateBasic.EquipType)
                : null;
            bool compare = current != null && current.GoodsId != candidate.GoodsId;

            if (compare)
            {
                GoodsModel.GoodsBasic currentBasic = GoodsModel.GetGoodsBasicByTypeId(current.TypeId);
                if (currentBasic != null)
                {
                    ActivateBlocker(_layout.compareBlocker);
                    _layout.compareCurrent.Show();
                    _layout.compareCandidate.Show();
                    ConfigureEquipButtons(_layout.compareCurrent, EquipButtons.Close);
                    ConfigureEquipButtons(_layout.compareCandidate, EquipButtons.Wear);
                    RenderEquipment(_layout.compareCurrent, _layout.compareCurrentIcon, currentBasic, current, null, epoch);
                    RenderEquipment(_layout.compareCandidate, _layout.compareCandidateIcon, candidateBasic, candidate, null, epoch);
                    RequestEquipmentDetail(_layout.compareCurrent, _layout.compareCurrentIcon, currentBasic, current, epoch);
                    RequestEquipmentDetail(_layout.compareCandidate, _layout.compareCandidateIcon, candidateBasic, candidate, epoch);
                    return;
                }
            }

            ActivateBlocker(_layout.dimBlocker);
            _layout.equipSingle.Show();
            ConfigureEquipButtons(_layout.equipSingle, ResolveSingleButtons(candidate, context));
            RenderEquipment(_layout.equipSingle, _layout.equipSingleIcon, candidateBasic, candidate, null, epoch);
            RequestEquipmentDetail(_layout.equipSingle, _layout.equipSingleIcon, candidateBasic, candidate, epoch);
        }

        private static EquipButtons ResolveSingleButtons(BagGoods goods, ItemContext context)
        {
            if (goods == null) return EquipButtons.None;
            switch (context)
            {
                case ItemContext.Bag: return EquipButtons.Wear;
                case ItemContext.Equipped: return EquipButtons.Close;
                case ItemContext.WarehouseBag: return EquipButtons.Deposit;
                case ItemContext.WarehouseStorage: return EquipButtons.Takeout;
                default: return EquipButtons.None;
            }
        }

        private static void RequestEquipmentDetail(EquipToolTipsBind view, EquipmentItem icon,
            GoodsModel.GoodsBasic basic, BagGoods goods, int epoch)
        {
            if (goods == null || goods.GoodsId <= 0) return;
            GoodsDynamicModel.Instance.RequestDetail(goods.GoodsId, detail =>
            {
                if (epoch != _epoch || detail == null || view == null || !view.gameObject.activeInHierarchy) return;
                RenderEquipment(view, icon, basic, goods, detail, epoch);
            });
        }

        private static void RenderEquipment(EquipToolTipsBind view, EquipmentItem icon,
            GoodsModel.GoodsBasic basic, BagGoods goods, GoodsDetailVo detail, int epoch)
        {
            GoodsModel.EquipAttr equip = GoodsModel.GetEquipAttr(basic.TypeId);
            int stage = equip?.Stage ?? 0;
            int qualityColor = goods != null ? Mathf.Clamp(goods.Color, 0, 8) : basic.Color;
            int strengthen = detail != null ? detail.Stren : goods?.Stren ?? 0;
            long rating = detail != null && detail.Rating > 0 ? detail.Rating
                : goods != null && goods.Rating > 0 ? goods.Rating
                : equip?.BaseRating ?? 0;

            view.equip_name.text = string.IsNullOrEmpty(basic.Name) ? "#" + basic.TypeId : basic.Name;
            view.grade.text = stage > 0 ? stage + "阶" : "";
            view.score.text = "评分：<color=#ffef67>" + rating + "</color>";
            view.lb_level.text = "等级：";
            int displayLevel = basic.Level > 370 ? basic.Level - 370 : basic.Level;
            string levelText = displayLevel == 1 ? "无限制" : displayLevel.ToString();
            string levelColor = RoleModel.Instance.Level >= basic.Level ? "#ffef67" : "#fa4d4d";
            view.level.text = "<color=" + levelColor + ">" + levelText + "</color>";
            SetActive(view.sc_img, basic.Level > 370);

            int roleTurn = RoleModel.Instance.Figure?.turn ?? 0;
            bool careerMismatch = basic.CareerId != 0 && RoleModel.Instance.Career != basic.CareerId;
            bool sexMismatch = basic.Sex != 0 && RoleModel.Instance.Sex != basic.Sex;
            bool turnMismatch = roleTurn < basic.Turn;
            string careerColor = careerMismatch || sexMismatch || turnMismatch ? "#ef4848" : "#d15e00";
            view.career.text = "职业：<color=" + careerColor + ">" +
                               EquipmentTipsConfig.GetCareerRequirementName(basic) + "</color>";
            view.pos_part.text = "部位：";
            view.pos.text = "<color=#d15e00>" + GoodsModel.GetEquipPosName(basic.EquipType) + "</color>";
            _ = SetQualityHeader(view.nameBG, qualityColor, epoch);

            if (icon != null)
            {
                icon.SetClickCallBack(() => { });
                icon.SetData(basic.TypeId, 1, goods != null && goods.Bind != 0);
                icon.SetDisplayColor(qualityColor);
                icon.SetGrade(stage);
                icon.SetStar(equip?.Star ?? 0);
                icon.SetBadIcon(equip?.ClassType == 1);
                icon.SetStrengthen(strengthen);
                icon.SetTimeLimit(GoodsModel.HasConfigExpiry(basic.TypeId) || (goods != null && goods.ExpireTime > 0));
            }

            ClearEquipmentSections(view);
            RenderBestAttributes(view, basic, goods, detail);
            RenderBaseAttributes(view, basic, strengthen);
            RenderSpecialAttributes(view, basic, equip);
            RenderStones(view, basic, stage, detail, epoch);
            RenderWash(view, detail);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(view.content_group);
            Canvas.ForceUpdateCanvases();
            view.content_scroll.StopMovement();
            Vector2 contentPosition = view.content_group.anchoredPosition;
            contentPosition.y = 0f;
            view.content_group.anchoredPosition = contentPosition;
            view.content_scroll.verticalNormalizedPosition = 1f;
        }

        private static async Task SetQualityHeader(Image image, int color, int epoch)
        {
            if (image == null) return;
            await ResManager.SetImageAsync(image,
                GameResPath.GetIcon("common4", "ui_tips_pz_" + color), false, false);
            if (epoch != _epoch) return;
        }

        private static void ClearEquipmentSections(EquipToolTipsBind view)
        {
            ClearChildren(view.best_pro_conta);
            ClearChildren(view.stone_pro_conta);
            SetActive(view.best_conta, false);
            SetActive(view.base_pro_conta, true);
            SetActive(view.spec_conta, false);
            SetActive(view.god_conta, false);
            SetActive(view.stone_conta, false);
            SetActive(view.wash_conta, false);
            SetActive(view.smelt_conta, false);
            SetActive(view.suit_conta, false);
            SetActive(view.suit_conta2, false);
            SetActive(view.refine, false);
            SetActive(view.refine_pro_conta, false);
            view.@base.text = "基础属性";
            view.basePro.text = "";
            view._html_stren.text = "";
            view.specPro.text = "";
            view.washText.text = "";
        }

        private static void RenderBestAttributes(EquipToolTipsBind view, GoodsModel.GoodsBasic basic,
            BagGoods goods, GoodsDetailVo detail)
        {
            List<EquipExtraAttr> extras = detail?.ExtraAttrs ?? goods?.ExtraAttrs;
            if (extras != null && extras.Count > 0)
            {
                view.best.text = "极品属性";
                SetActive(view.best_conta, true);
                foreach (EquipExtraAttr attr in extras)
                {
                    string name = AttrName(attr.AttrId);
                    string value;
                    if (attr.AttrTypeId == 1)
                    {
                        name = name.Replace("{0}", attr.PlusInterval.ToString());
                        value = attr.PlusUnit.ToString();
                    }
                    else value = GoodsModel.FormatAttrValue(attr.AttrId, attr.AttrVal);
                    AddBestRow(view, "<color=" + EquipmentTipsConfig.GetLightColor(attr.Color) + ">" +
                                     name + " +" + value + "</color>");
                }
                return;
            }

            List<GoodsModel.EquipRecommendAttrRow> preview = GoodsModel.GetEquipRecommendAttrRows(basic.TypeId);
            if (preview.Count == 0) return;
            int count = GoodsModel.GetBestProNum(basic.TypeId);
            view.best.text = count > 0 ? "极品属性(随机生成" + count + "条极品属性)" : "极品属性";
            SetActive(view.best_conta, true);
            foreach (GoodsModel.EquipRecommendAttrRow row in preview)
                AddBestRow(view, "<color=" + EquipmentTipsConfig.GetDarkColor(row.Color) + ">[推荐] " +
                                 row.Name + " +" + row.Value + "</color>");
        }

        private static void AddBestRow(EquipToolTipsBind view, string text)
        {
            if (view._tpl_EquipBestProItem == null) return;
            GameObject go = UnityEngine.Object.Instantiate(view._tpl_EquipBestProItem, view.best_pro_conta);
            go.name = "BestAttributeRow";
            go.SetActive(true);
            EquipBestProItemBind row = go.GetComponent<EquipBestProItemBind>();
            if (row == null) return;
            if (row.star != null) row.star.gameObject.SetActive(false);
            row.pro.text = text;
        }

        private static void RenderBaseAttributes(EquipToolTipsBind view, GoodsModel.GoodsBasic basic, int strengthen)
        {
            List<GoodsModel.EquipBaseAttrRow> rows = GoodsModel.GetBaseAttrRows(basic.TypeId);
            var strengthenByAttr = new Dictionary<int, long>();
            foreach (EquipmentTipsConfig.AttrValue value in EquipmentTipsConfig.GetStrengthenAttrs(basic.EquipType, strengthen))
                strengthenByAttr[value.AttrId] = value.Value;

            var baseLines = new List<string>();
            var strengthenLines = new List<string>();
            foreach (GoodsModel.EquipBaseAttrRow row in rows)
            {
                baseLines.Add(row.Name + "：<color=#d15e00>" + row.Value + "</color>");
                strengthenLines.Add(strengthenByAttr.TryGetValue(row.AttrId, out long value) && value > 0
                    ? "<color=#0a953e>强化 +" + value + "</color>"
                    : "");
            }
            view.basePro.text = string.Join("\n", baseLines);
            view._html_stren.text = string.Join("\n", strengthenLines);
        }

        private static void RenderSpecialAttributes(EquipToolTipsBind view, GoodsModel.GoodsBasic basic,
            GoodsModel.EquipAttr equip)
        {
            List<(string name, string val)> rows = GoodsModel.GetEquipOtherAttrs(basic.TypeId);
            if (rows.Count == 0) return;
            view.spec.text = EquipmentTipsConfig.GetColorName(basic.Color) + (equip?.Star ?? 0) + "专有属性";
            var lines = new List<string>();
            foreach ((string name, string value) in rows)
                lines.Add("<color=#d15e00>" + name + "：" + value + "</color>");
            view.specPro.text = string.Join("\n", lines);
            SetActive(view.spec_conta, true);
        }

        private static void RenderStones(EquipToolTipsBind view, GoodsModel.GoodsBasic basic, int stage,
            GoodsDetailVo detail, int epoch)
        {
            if (!FuncOpenConfig.CheckFuncOpenState("EquipJewelView") || view._tpl_EquipStoneItem == null) return;
            var installed = new Dictionary<int, int>();
            if (detail?.StoneList != null)
                foreach (GoodsStoneSlot slot in detail.StoneList) installed[slot.Pos] = slot.TypeId;

            view.stone.text = "宝石属性";
            SetActive(view.stone_conta, true);
            for (int position = 1; position <= 6; position++)
            {
                GameObject go = UnityEngine.Object.Instantiate(view._tpl_EquipStoneItem, view.stone_pro_conta);
                go.name = "StoneSlot" + position;
                go.SetActive(true);
                EquipStoneItemBind row = go.GetComponent<EquipStoneItemBind>();
                if (row == null) continue;

                if (installed.TryGetValue(position, out int stoneTypeId) && stoneTypeId > 0)
                {
                    GoodsModel.GoodsBasic stone = GoodsModel.GetGoodsBasicByTypeId(stoneTypeId);
                    string name = stone != null && !string.IsNullOrEmpty(stone.Name) ? stone.Name : "#" + stoneTypeId;
                    row.stone_name.text = "<color=" + EquipmentTipsConfig.GetDarkColor(stone?.Color ?? 0) + ">" + name + "</color>";
                    row.stone_lock.gameObject.SetActive(false);
                    row.stone_label.gameObject.SetActive(true);
                    row.stone_label.text = BuildStoneAttributes(stoneTypeId);
                    if (stone != null) _ = SetStoneIcon(row.stone_icon, stone.Icon, epoch);
                }
                else
                {
                    EquipmentTipsConfig.StoneUnlock unlock = EquipmentTipsConfig.GetStoneUnlock(basic.EquipType, position);
                    string lockText = unlock.Stage > stage ? unlock.Stage + "阶解锁"
                        : unlock.Vip > 0 ? "VIP" + unlock.Vip + "解锁"
                        : "未镶嵌";
                    row.stone_name.text = lockText;
                    row.stone_lock.gameObject.SetActive(unlock.Stage > stage || unlock.Vip > 0);
                    row.stone_label.text = "";
                    row.stone_label.gameObject.SetActive(false);
                    _ = ResManager.SetImageAsync(row.stone_icon,
                        GameResPath.GetIcon("common4", "ui_tips_03"), false, false);
                }
            }
        }

        private static string BuildStoneAttributes(int stoneTypeId)
        {
            IReadOnlyList<EquipmentTipsConfig.AttrValue> attrs = EquipmentTipsConfig.GetStoneAttrs(stoneTypeId);
            var lines = new List<string>();
            var line = new StringBuilder();
            for (int i = 0; i < attrs.Count; i++)
            {
                if (i % 2 == 0 && line.Length > 0)
                {
                    lines.Add(line.ToString());
                    line.Clear();
                }
                if (line.Length > 0) line.Append("  ");
                line.Append(AttrName(attrs[i].AttrId)).Append("：<color=#d15e00>").Append(attrs[i].Value).Append("</color>");
            }
            if (line.Length > 0) lines.Add(line.ToString());
            return string.Join("\n", lines);
        }

        private static async Task SetStoneIcon(Image image, string icon, int epoch)
        {
            if (image == null || string.IsNullOrEmpty(icon)) return;
            await ResManager.SetImageAsync(image, GameResPath.GetGoodsIconPath(icon), false, false);
            if (epoch != _epoch) return;
        }

        private static void RenderWash(EquipToolTipsBind view, GoodsDetailVo detail)
        {
            if (detail?.WashAttrs == null || detail.WashAttrs.Count == 0) return;
            view.wash.text = "洗炼属性";
            var lines = new List<string>();
            foreach (GoodsWashAttr attr in detail.WashAttrs)
            {
                string color = attr.Color == 1 ? "#0a953e" : EquipmentTipsConfig.GetDarkColor(attr.Color);
                lines.Add("<color=" + color + ">" + AttrName(attr.AttrId) + "+" +
                          GoodsModel.FormatAttrValue(attr.AttrId, attr.AttrVal) + "</color>");
            }
            view.washText.text = string.Join("\n", lines);
            SetActive(view.wash_conta, true);
        }

        private static string AttrName(int attrId)
        {
            string name = GoodsModel.GetAttrName(attrId);
            return string.IsNullOrEmpty(name) ? "属性" + attrId : name;
        }

        private static void ShowGoods(GoodsModel.GoodsBasic basic, long num, BagGoods goods,
            ItemContext context, int epoch)
        {
            ActivateBlocker(_layout.dimBlocker);
            GoodsTooltipsBind view = _layout.goods;
            view.Show();
            view.goods_name.text = string.IsNullOrEmpty(basic.Name) ? "#" + basic.TypeId : basic.Name;
            string typeName = GoodsModel.GetGoodsTypeName(basic.Type);
            view.type_text.text = string.IsNullOrEmpty(typeName) ? "" : "类型：<color=#ffe222>" + typeName + "</color>";
            view.quantity_text.text = "数量：<color=#ffe222>" + num + "</color>";
            view.level_text.text = basic.Level > 1 ? "等级：" : "";
            view.level_text_value.text = basic.Level > 1 ? basic.Level.ToString() : "";
            string intro = NormalizeConfigText(basic.Intro);
            string ways = NormalizeConfigText(basic.Getway);
            view.intro.text = intro;
            view.ways.text = ways;
            view.tips.text = "";
            SetActive(view._Group1, !string.IsNullOrEmpty(intro));
            SetActive(view.intro, !string.IsNullOrEmpty(intro));
            SetActive(view.tips, false);
            SetActive(view.line2, !string.IsNullOrEmpty(ways));
            SetActive(view.label2, !string.IsNullOrEmpty(ways));
            SetActive(view.ways, !string.IsNullOrEmpty(ways));
            SetActive(view.rewardsList, false);
            SetActive(view._gp_cooling, false);
            RenderGoodsSources(view, GoodsModel.GetGoodsSourceEntries(basic.TypeId));
            ConfigureGoodsButtons(view, goods, context);
            _ = SetQualityHeader(view.nameBG, basic.Color, epoch);
            LayoutGoodsTooltip(view);
            _ = BuildNormalIcon(view, basic.TypeId, epoch);
        }

        private static void RenderGoodsSources(GoodsTooltipsBind view,
            List<GoodsModel.GoodsSourceEntry> entries)
        {
            bool show = entries != null && entries.Count > 0;
            SetActive(view.sourceGp, show);
            if (!show)
            {
                if (view.source_txt != null) view.source_txt.text = "";
                return;
            }

            var text = new StringBuilder();
            for (int i = 0; i < entries.Count; i++)
            {
                if (i > 0) text.Append("  ");
                GoodsModel.GoodsSourceEntry entry = entries[i];
                string color = entry.Argument != 0 ? "#d15e00" : "#8b8b8a";
                text.Append("<color=").Append(color).Append('>').Append(entry.Name).Append("</color>");
            }
            view.source_txt.text = text.ToString();
        }

        /// <summary>
        /// 老端 GoodsTooltips.SetTipsHeight 的 Unity 等价实现：详情、来源和按钮按 preferred height 排列，
        /// 内容过长时只滚动详情区，背景始终包住全部可见分组。
        /// </summary>
        private static void LayoutGoodsTooltip(GoodsTooltipsBind view)
        {
            if (view == null || view.root_wnd == null) return;

            FitTextHeight(view.intro, 24f);
            FitTextHeight(view.tips, 24f);
            FitTextHeight(view.ways, 24f);
            if (view.source_txt != null)
            {
                RectTransform sourceTextRect = (RectTransform)view.source_txt.transform;
                sourceTextRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 253f);
                FitTextHeight(view.source_txt, 24f);
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(view.detail_group);
            float contentHeight = GetActiveChildrenHeight(view.detail_group);
            view.detail_group.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);

            float sourceHeight = view.sourceGp != null && view.sourceGp.gameObject.activeSelf
                ? 45f + Mathf.Max(24f, ((RectTransform)view.source_txt.transform).rect.height)
                : 0f;
            if (view.sourceGp != null)
                view.sourceGp.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sourceHeight);

            const float headerHeight = 166f;
            const float buttonHeight = 86f;
            float naturalHeight = headerHeight + contentHeight + sourceHeight + buttonHeight + 8f;
            float panelHeight = Mathf.Clamp(naturalHeight, 450f, 680f);
            float buttonTop = panelHeight - buttonHeight;
            float sourceTop = buttonTop - sourceHeight - (sourceHeight > 0f ? 8f : 0f);
            float scrollBottom = sourceHeight > 0f ? sourceTop : buttonTop;
            float scrollHeight = Mathf.Clamp(scrollBottom - headerHeight, 1f, 400f);

            RectTransform viewRect = view.transform as RectTransform;
            viewRect?.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, panelHeight);
            view.root_wnd.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, panelHeight);
            if (view.bg != null)
                ((RectTransform)view.bg.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, panelHeight);
            if (view.detail_scroller != null)
                ((RectTransform)view.detail_scroller.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, scrollHeight);
            if (view.btn_group != null)
            {
                view.btn_group.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, buttonHeight);
                Vector2 pos = view.btn_group.anchoredPosition;
                pos.y = -buttonTop;
                view.btn_group.anchoredPosition = pos;
                LayoutRebuilder.ForceRebuildLayoutImmediate(view.btn_group);
            }
            if (view.sourceGp != null && sourceHeight > 0f)
            {
                Vector2 pos = view.sourceGp.anchoredPosition;
                pos.y = -sourceTop;
                view.sourceGp.anchoredPosition = pos;
            }

            view.detail_scroller.StopMovement();
            Vector2 contentPosition = view.detail_group.anchoredPosition;
            contentPosition.y = 0f;
            view.detail_group.anchoredPosition = contentPosition;
            view.detail_scroller.verticalNormalizedPosition = 1f;
            Canvas.ForceUpdateCanvases();
        }

        private static float FitTextHeight(TextMeshProUGUI text, float minimum)
        {
            if (text == null || !text.gameObject.activeSelf) return 0f;
            RectTransform rect = (RectTransform)text.transform;
            float width = rect.rect.width > 1f ? rect.rect.width : rect.sizeDelta.x;
            float height = Mathf.Max(minimum, Mathf.Ceil(text.GetPreferredValues(text.text, width, 0f).y) + 2f);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            return height;
        }

        private static float GetActiveChildrenHeight(RectTransform parent)
        {
            if (parent == null) return 0f;
            VerticalLayoutGroup layout = parent.GetComponent<VerticalLayoutGroup>();
            float spacing = layout != null ? layout.spacing : 0f;
            float height = layout != null ? layout.padding.top + layout.padding.bottom : 0f;
            int count = 0;
            for (int i = 0; i < parent.childCount; i++)
            {
                RectTransform child = parent.GetChild(i) as RectTransform;
                if (child == null || !child.gameObject.activeSelf) continue;
                LayoutElement element = child.GetComponent<LayoutElement>();
                if (element != null && element.ignoreLayout) continue;
                height += child.rect.height;
                count++;
            }
            if (count > 1) height += spacing * (count - 1);
            return Mathf.Max(1f, height);
        }

        private static void ConfigureGoodsButtons(GoodsTooltipsBind view, BagGoods goods, ItemContext context)
        {
            bool use = goods != null && context == ItemContext.Bag && GoodsModel.GetGoodsBasicByTypeId(goods.TypeId)?.Use != 0;
            bool deposit = goods != null && context == ItemContext.WarehouseBag;
            bool takeout = goods != null && context == ItemContext.WarehouseStorage;
            SetActive(view.useBtn, use);
            SetActive(view.okBtn, true);
            SetActive(view.depositBtn, deposit);
            SetActive(view.takeoutBtn, takeout);
            SetActive(view.sellBtn, false);
            SetActive(view.upShelfBtn, false);
            SetActive(view.outShelfBtn, false);
            SetActive(view.treasureReceiveBtn, false);
            SetActive(view.putBtn, false);
            BindUnique(view.useBtn, use ? OnUseClick : null);
            BindUnique(view.okBtn, Close);
            BindUnique(view.depositBtn, deposit ? OnMoveClick : null);
            BindUnique(view.takeoutBtn, takeout ? OnMoveClick : null);
        }

        private static void ConfigureEquipButtons(EquipToolTipsBind view, EquipButtons buttons)
        {
            SetActive(view.replaceBtn, buttons == EquipButtons.Wear);
            SetActive(view.closeBtn, buttons == EquipButtons.Close);
            SetActive(view.depositBtn, buttons == EquipButtons.Deposit);
            SetActive(view.takeoutBtn, buttons == EquipButtons.Takeout);
            SetActive(view.donateBtn, false);
            SetActive(view.destroyBtn, false);
            SetActive(view.exchangeBtn, false);
            SetActive(view.UninstallBtn, false);
            SetActive(view.upShelfBtn, false);
            SetActive(view.outShelfBtn, false);
            SetActive(view.UninstallBtn_spirit, false);
            SetActive(view.sellBtn, false);
            SetActive(view.treasureReceiveBtn, false);
            SetActive(view.guild_conta, false);
            BindUnique(view.replaceBtn, buttons == EquipButtons.Wear ? OnWearClick : null);
            BindUnique(view.closeBtn, buttons == EquipButtons.Close ? Close : null);
            BindUnique(view.depositBtn, buttons == EquipButtons.Deposit ? OnMoveClick : null);
            BindUnique(view.takeoutBtn, buttons == EquipButtons.Takeout ? OnMoveClick : null);
        }

        private static async Task BuildNormalIcon(GoodsTooltipsBind view, int typeId, int epoch)
        {
            if (_normalIconCell != null)
            {
                ResManager.ReleaseInstance(_normalIconCell);
                _normalIconCell = null;
            }
            GameObject go = await ResManager.InstantiateAsync(GameResPath.GetUIPrefab("common", "BaseAwardItem"), view.item_group);
            if (epoch != _epoch)
            {
                if (go != null) ResManager.ReleaseInstance(go);
                return;
            }
            if (go == null) return;
            go.SetActive(true);
            BaseAwardItem cell = go.GetComponent<BaseAwardItem>();
            if (cell == null)
            {
                ResManager.ReleaseInstance(go);
                return;
            }
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            cell.SetClickCallBack(() => { });
            cell.SetData(typeId, 1);
            _normalIconCell = go;
        }

        private static void ActivateBlocker(Image blocker)
        {
            blocker.gameObject.SetActive(true);
            BindUnique(blocker, Close);
        }

        public static void Close()
        {
            _epoch++;
            _goods = null;
            _context = ItemContext.Bag;
            if (_normalIconCell != null)
            {
                ResManager.ReleaseInstance(_normalIconCell);
                _normalIconCell = null;
            }
            if (_layout != null)
            {
                HideView(_layout.goods);
                HideView(_layout.equipSingle);
                HideView(_layout.compareCurrent);
                HideView(_layout.compareCandidate);
                _layout.dimBlocker.gameObject.SetActive(false);
                _layout.compareBlocker.gameObject.SetActive(false);
            }
            if (_moduleRoot != null) _moduleRoot.SetActive(false);
        }

        private static void OnWearClick()
        {
            BagGoods goods = _goods;
            if (goods == null) return;
            Equip.EquipWearController.Instance.Wear(goods.GoodsId);
            Close();
        }

        private static void OnMoveClick()
        {
            BagGoods goods = _goods;
            if (goods == null) return;
            int from = _context == ItemContext.WarehouseStorage ? BagModel.POS_WAREHOUSE : BagModel.POS_BAG;
            int to = from == BagModel.POS_WAREHOUSE ? BagModel.POS_BAG : BagModel.POS_WAREHOUSE;
            BagController.Instance.MoveGoods(goods.GoodsId, from, to);
            Close();
        }

        private static void OnUseClick()
        {
            BagGoods goods = _goods;
            if (goods == null) return;
            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(goods.TypeId);
            if (basic == null) return;

            if (basic.Type == 38 && basic.Subtype == 39)
            {
                if (basic.Level > 0 && basic.Level > RoleModel.Instance.Level)
                {
                    TipsManager.Toast("等级不足");
                    Close();
                    return;
                }
                TransferJob.TransferJobFlow.Show();
                Close();
                return;
            }

            string blocked = UseBranchBlocker(basic);
            if (blocked != null)
            {
                TipsManager.Toast("该物品需专属界面(未移植:" + blocked + ")");
                return;
            }

            if (goods.GoodsNum <= 1)
            {
                BagController.Instance.UseGoods(goods.GoodsId, 1);
                Close();
            }
            else
            {
                BatchUseFlow.Show(goods);
                Close();
            }
        }

        private static string UseBranchBlocker(GoodsModel.GoodsBasic basic)
        {
            if (basic.Type == 34) return "SelectGiftView(礼包选择)";
            if (basic.Type == 37 && basic.Subtype == 2) return "经验符 buff 比较流程";
            if (basic.Type == 38 && basic.Subtype == 6) return "OpenFun 35(定时宝箱)";
            if (basic.Type == 38 && basic.Subtype == 10) return "MarriageFlowerView(婚礼鲜花)";
            if (basic.Type == 38 && basic.Subtype == 36) return "MaskUseView(蒙面人道具)";
            if (basic.Type == 38 && basic.Subtype == 42) return "OpenFun 18(转生界面)";
            if (basic.Type == 75) return "藏宝图(野外场景使用)";
            if (basic.Type == 59) return "OpenFun 203(装扮)";
            if (basic.Type == 83 && basic.Subtype == 1) return "OpenFun 240(古宝)";
            if (basic.Type == 14 && basic.Subtype == 12) return "OpenFun 11(宝石直升)";
            if (basic.Type == 22 && basic.Subtype == 1) return "OpenFun 16(伙伴魂珠)";
            if (basic.TypeId == 37090001) return "人物直升丹确认流程";
            return null;
        }

        private static async Task<bool> EnsureBuilt()
        {
            if (_moduleRoot != null && _layout != null) return true;
            Transform parent = ViewManager.GetLayer(UILayer.Popup);
            if (parent == null)
            {
                GameLog.Error("Common", "ItemTipsView 无法打开：Popup 层未就绪");
                return false;
            }
            _moduleRoot = await ResManager.InstantiateAsync(GameResPath.GetUIPrefab("common", "CommonModule"), parent);
            if (_moduleRoot == null) return false;
            _moduleRoot.name = "CommonModule(ItemTips)";
            _layout = _moduleRoot.GetComponent<ItemTipsModalLayout>();
            if (_layout == null || _layout.dimBlocker == null || _layout.compareBlocker == null ||
                _layout.goods == null || _layout.equipSingle == null || _layout.compareCurrent == null ||
                _layout.compareCandidate == null)
            {
                GameLog.Error("Common", "CommonModule 缺 ItemTipsModalLayout 完整序列化引用");
                ResManager.ReleaseInstance(_moduleRoot);
                _moduleRoot = null;
                _layout = null;
                return false;
            }
            // CommonModule 内含多个同级弹层，也含详情板自身使用的 EquipmentItem 等 BaseView 子组件。
            // 初始化只关闭最外层窗口；嵌套展示组件随所属窗口显隐，不能被单独关掉。
            foreach (BaseView view in _moduleRoot.GetComponentsInChildren<BaseView>(true))
            {
                bool nestedInView = false;
                for (Transform parentView = view.transform.parent;
                     parentView != null && parentView != _moduleRoot.transform;
                     parentView = parentView.parent)
                {
                    if (parentView.GetComponent<BaseView>() == null) continue;
                    nestedInView = true;
                    break;
                }
                if (!nestedInView) view.gameObject.SetActive(false);
            }
            _layout.dimBlocker.gameObject.SetActive(false);
            _layout.compareBlocker.gameObject.SetActive(false);
            _moduleRoot.SetActive(false);
            return true;
        }

        private static void BindUnique(Component target, Action action)
        {
            if (target == null) return;
            GameObject go = target.gameObject;
            Image image = target as Image ?? go.GetComponent<Image>();
            if (image == null)
            {
                image = go.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0f);
            }
            foreach (Graphic graphic in go.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = graphic == image && action != null;
            UIUtil.ClearClicks(image);
            if (action != null) UIUtil.AddClick(image, action);
        }

        private static void ClearChildren(RectTransform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject go = parent.GetChild(i).gameObject;
                go.SetActive(false);
                if (Application.isPlaying) UnityEngine.Object.Destroy(go);
                else UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static void HideView(BaseView view)
        {
            if (view == null) return;
            if (view.IsShown) view.Hide();
            else view.gameObject.SetActive(false);
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null) component.gameObject.SetActive(active);
        }

        private static string ToTmpRich(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            value = Regex.Replace(value, "<br\\s*/?>", "\n", RegexOptions.IgnoreCase);
            value = Regex.Replace(value, "<font\\s+color=['\"]?(#?[0-9a-fA-F]+)['\"]?\\s*>", "<color=$1>", RegexOptions.IgnoreCase);
            return Regex.Replace(value, "</font>", "</color>", RegexOptions.IgnoreCase);
        }

        private static string NormalizeConfigText(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            value = value.Trim();
            return value == "[]" ? "" : ToTmpRich(value);
        }
    }
}
