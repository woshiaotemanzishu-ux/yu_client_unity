using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Common.Proto;
using Shenxiao.Common.Tips;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Alert;
using Shenxiao.Generated.UI.Common;
using Shenxiao.Generated.UI.Suit;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Equip;
using Shenxiao.Module.Core.Resonance;
using Shenxiao.Module.Core.Role;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 人物→共鸣的无账号消耗真实路由。用 GraphicRaycaster 覆盖四页签、22 部位、46 阶属性、
    /// 每个部位的装备/材料详情、打造确认后取消、预览、回退查询后取消、说明、滚动、关闭与热重开。
    /// 15221/15222 最终确认永不点击，所有出站帧均在 Editor 内拦截。
    /// </summary>
    public static class ResonanceRouteCase
    {
        private const string DefaultEvidenceRoot =
            "output/ui_route_audit/2026-08-07_resonance/unity_editor/2026-08-07_1320_attempt5";
        private const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags AnyInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly int[] ExpectedPositionCounts = { 6, 6, 6, 4 };
        private static readonly int[] ExpectedStageCounts = { 16, 10, 8, 12 };
        private static readonly string[] ExpectedEffects =
            { "ui_shenzhuang01", "ui_shenzhuang02", "ui_shenzhuang03", "ui_shenzhuang03" };
        private static readonly int[] EquipmentTypeIds =
        {
            101016144, 101026144, 101036144, 101046144, 101056144,
            101066144, 101076144, 101086144, 101096144, 101106144,
        };

        public static string LastDetail { get; private set; } = "not-run";
        public static string EvidenceRoot { get; set; } = DefaultEvidenceRoot;
        public static int DiagnosticPositionLimit { get; set; }
        public static int DiagnosticStartTab { get; set; }

        private readonly struct EffectPixelMetrics
        {
            public EffectPixelMetrics(int pixels, int width, int height)
            {
                Pixels = pixels;
                Width = width;
                Height = height;
            }

            public int Pixels { get; }
            public int Width { get; }
            public int Height { get; }

            public override string ToString() => Pixels + "px/" + Width + "x" + Height;
        }

        public static async Task<int> Run()
        {
            LastDetail = "running";
            CliVerify.Stage stage = CliVerify.Stage.Create();
            EventSystem eventSystem = null;
            bool createdEventSystem = false;
            EquipReadController controller = EquipReadController.Instance;
            bool controllerWasInitialized = controller.IsInitialized;
            var bagState = new BagState();
            var equipState = new EquipState();
            var roleState = new RoleState();
            FieldInfo equipIntercept = typeof(EquipReadController).GetField("s_outboundIntercept", PrivateStatic);
            FieldInfo bagIntercept = typeof(BagController).GetField("s_outboundIntercept", PrivateStatic);
            object oldEquipIntercept = equipIntercept?.GetValue(null);
            object oldBagIntercept = bagIntercept?.GetValue(null);
            var equipFrames = new List<byte[]>();
            var bagFrames = new List<byte[]>();
            var failures = new List<string>();
            var shots = new List<string>();
            var effectPixels = new Dictionary<string, int>(StringComparer.Ordinal);
            int positionChecks = 0;
            int stageChecks = 0;
            int positionCells = 0;
            int materialDetails = 0;
            int materialExpected = 0;
            int buildCancels = 0;
            int previewChecks = 0;
            int returnChecks = 0;
            int visualTypographyChecks = 0;
            int componentLayoutChecks = 0;
            bool positionEffectFrame = false;
            bool pageEffectOwnership = false;
            EffectPixelMetrics positionEffectMetrics = default;
            bool instructionLayout = false;
            bool instructionLastReachable = false;
            bool instructionScrolled = false;
            long coldMs = 0;
            long warmMs = 0;
            string phase = "prepare";

            try
            {
                ResetFlows();
                await PrepareData();
                if (!controller.IsInitialized) controller.Init();
                equipIntercept?.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    equipFrames.Add((byte[])frame.Clone());
                    return true;
                }));
                bagIntercept?.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    bagFrames.Add((byte[])frame.Clone());
                    return true;
                }));

                Canvas canvas = stage.CanvasRoot.GetComponent<Canvas>();
                GraphicRaycaster raycaster = stage.CanvasRoot.GetComponent<GraphicRaycaster>();
                eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
                if (eventSystem == null)
                {
                    eventSystem = new GameObject("ResonanceRouteEventSystem", typeof(EventSystem))
                        .GetComponent<EventSystem>();
                    createdEventSystem = true;
                }
                Camera camera = canvas != null ? canvas.worldCamera : null;
                if (camera == null || raycaster == null || eventSystem == null)
                    throw new InvalidOperationException("共鸣路由缺相机/GraphicRaycaster/EventSystem");

                phase = "role-entry";
                RoleFlow.Open();
                EquipmentView equipmentView = await WaitForShown<EquipmentView>(12d);
                if (equipmentView == null) throw new InvalidOperationException("EquipmentView did not open");
                stage.ForceCjkFont();
                shots.Add(stage.Capture(EvidenceRoot + "/00_role_resonance_entry.png"));

                string entryHit;
                double coldStart = EditorApplication.timeSinceStartup;
                Require(Click(equipmentView._Group5, camera, raycaster, eventSystem, out entryHit),
                    "人物页共鸣入口射线失败 hit=" + entryHit, failures);
                await Task.Delay(350);
                stage.ForceCjkFont();
                shots.Add(stage.Capture(EvidenceRoot + "/01_resonance_0350ms.png"));
                await Task.Delay(650);
                stage.ForceCjkFont();
                shots.Add(stage.Capture(EvidenceRoot + "/02_resonance_1000ms.png"));

                phase = "wait-ready";
                ResonanceMainView main = await WaitForShown<ResonanceMainView>(15d);
                if (main == null) throw new InvalidOperationException("ResonanceMainView did not open");
                EquipSuitMianViewBind view = main.GetComponent<EquipSuitMianViewBind>();
                BaseWindowSkinView window = FindAncestor<BaseWindowSkinView>(main.transform);
                bool ready = await WaitUntil(() => view != null && window != null
                    && window.CurrentIndex == 0 && ActivePositions(view).Length == 6
                    && ActivePositions(view).All(item => item.nameLab != null
                        && !string.IsNullOrEmpty(item.nameLab.text)), 15d);
                coldMs = MillisecondsSince(coldStart);
                Require(ready, "共鸣首屏未 ready", failures);
                stage.ForceCjkFont();
                shots.Add(stage.Capture(EvidenceRoot + "/03_resonance_ready.png"));

                VerifyIdentityAndStructure(main, view, window, failures);
                VerifyPositionScroll(view, camera, raycaster, eventSystem, failures);

                for (int tabIndex = Mathf.Clamp(DiagnosticStartTab, 0, ResonanceConfigs.Tabs.Length - 1);
                     tabIndex < ResonanceConfigs.Tabs.Length; tabIndex++)
                {
                    phase = "tab-" + tabIndex;
                    TabButtonTwoSkin[] tabs = ActiveTabs(window);
                    string tabHit = "already-selected";
                    bool tabClick = window.CurrentIndex == tabIndex
                        || (tabs.Length == 4
                            && Click(tabs[tabIndex]._Image1, camera, raycaster, eventSystem, out tabHit));
                    bool tabReady = await WaitUntil(() => window.CurrentIndex == tabIndex
                        && ActivePositions(view).Length == ExpectedPositionCounts[tabIndex]
                        && view.nameLab != null && !string.IsNullOrEmpty(view.nameLab.text), 8d);
                    Require(tabClick && tabReady,
                        "页签" + tabIndex + "失败 hit=" + tabHit + " ready=" + tabReady, failures);

                    stage.ForceCjkFont();
                    shots.Add(stage.Capture(EvidenceRoot + "/tab_" + tabIndex + "_ready.png"));
                    int pixels = await CaptureEffectPixels(
                        ExpectedEffects[tabIndex],
                        EvidenceRoot + "/effects/tab_" + tabIndex + "_" + ExpectedEffects[tabIndex] + ".png");
                    effectPixels["tab" + tabIndex + ":" + ExpectedEffects[tabIndex]] = pixels;
                    Require(pixels >= 8, "页签" + tabIndex + "特效没有真实 RT 像素: " + pixels, failures);

                    IReadOnlyList<ResonanceConfigs.SuitItem> stages = ResonanceConfigs.GetSuitItems(
                        ResonanceConfigs.Tabs[tabIndex].SuitType,
                        ResonanceConfigs.Tabs[tabIndex].SubType);
                    Require(stages.Count == ExpectedStageCounts[tabIndex],
                        "页签" + tabIndex + "阶数=" + stages.Count, failures);
                    for (int stageIndex = 0; stageIndex < stages.Count; stageIndex++)
                    {
                        if (stageIndex > 0)
                        {
                            string arrowHit;
                            bool clicked = Click(view.rImg, camera, raycaster, eventSystem, out arrowHit);
                            bool changed = await WaitUntil(() => view.nameLab != null
                                && view.nameLab.text == stages[stageIndex].Name, 4d);
                            Require(clicked && changed,
                                "页签" + tabIndex + "属性阶" + stageIndex + "失败 hit=" + arrowHit, failures);
                        }
                        EquipNewSuitAttrItemBind[] attrItems = ActiveAttributes(view);
                        bool attrRows = attrItems.Length == stages[stageIndex].Tiers.Count;
                        Require(view.nameLab != null && view.nameLab.text == stages[stageIndex].Name && attrRows,
                            "页签" + tabIndex + "属性阶" + stageIndex + "内容不符", failures);
                        int activeCount = ResonanceConfigs.GetActiveCount(
                            ResonanceConfigs.Tabs[tabIndex].SuitType,
                            ResonanceConfigs.Tabs[tabIndex].SubType,
                            stages[stageIndex].Level);
                        int attrCount = Math.Min(attrItems.Length, stages[stageIndex].Tiers.Count);
                        for (int attrIndex = 0; attrIndex < attrCount; attrIndex++)
                        {
                            EquipNewSuitAttrItemBind attrItem = attrItems[attrIndex];
                            bool active = activeCount >= stages[stageIndex].Tiers[attrIndex].Count;
                            Color32 expectedColor = active
                                ? new Color32(10, 149, 62, 255) : new Color32(103, 103, 103, 255);
                            bool typography = attrItem.numLab != null && attrItem.attrHtml != null
                                && SameColor(attrItem.numLab.color, expectedColor)
                                && SameColor(attrItem.attrHtml.color, expectedColor)
                                && Mathf.Abs(attrItem.attrHtml.fontSize - 20f) < 0.01f
                                && Mathf.Abs(attrItem.attrHtml.rectTransform.rect.width - 200f) < 0.1f
                                && !attrItem.attrHtml.text.Contains(" +", StringComparison.Ordinal)
                                && (stages[stageIndex].Tiers[attrIndex].Attributes.Count == 0
                                    || attrItem.attrHtml.text.Contains("：", StringComparison.Ordinal));
                            Require(typography,
                                "页签" + tabIndex + "属性阶" + stageIndex + "第" + attrIndex
                                + "项字体/颜色/标点未对齐老端", failures);
                            visualTypographyChecks++;
                        }
                        stageChecks++;
                    }
                    string maxArrowHit;
                    string maxName = view.nameLab != null ? view.nameLab.text : string.Empty;
                    Require(Click(view.rImg, camera, raycaster, eventSystem, out maxArrowHit)
                            && view.nameLab != null && view.nameLab.text == maxName,
                        "页签" + tabIndex + "最高阶边界失败 hit=" + maxArrowHit, failures);
                    stage.ForceCjkFont();
                    shots.Add(stage.Capture(EvidenceRoot + "/tab_" + tabIndex + "_attribute_max.png"));

                    IReadOnlyList<byte> positions = ResonanceConfigs.GetPositions(
                        ResonanceConfigs.Tabs[tabIndex].SuitType);
                    for (int positionIndex = 0; positionIndex < positions.Count; positionIndex++)
                    {
                        await Task.Delay(100);
                        Canvas.ForceUpdateCanvases();
                        byte position = positions[positionIndex];
                        phase = "tab-" + tabIndex + "-position-" + position;
                        EquipSuitPosItemBind card = FindPosition(view, position);
                        Image positionSurface = card != null && card.itemBox != null
                            ? card.itemBox.GetComponent<Image>() : null;
                        string positionHit = "not-run";
                        bool positionClick = positionSurface != null
                            && Click(positionSurface, camera, raycaster, eventSystem, out positionHit);
                        bool positionReady = await WaitUntil(() =>
                        {
                            EquipSuitPosItemBind selected = FindPosition(view, position);
                            return selected != null && selected.selectImg != null
                                && selected.selectImg.gameObject.activeInHierarchy
                                && ActiveCosts(view).Length > 0;
                        }, 5d);
                        Require(positionClick && positionReady,
                            "tab=" + tabIndex + " pos=" + position + "选择失败 hit=" + positionHit, failures);
                        positionChecks++;
                        card = FindPosition(view, position);
                        EquipmentItem equipmentCell = card != null && card.iconBox != null
                            ? card.iconBox.GetComponentInChildren<EquipmentItem>(false) : null;
                        BagGoods equipped = BagModel.Instance.GetEquipmentAt(position);
                        Require(equipped != null && equipmentCell != null
                                && equipmentCell.gameObject.name == "EquipmentItem_" + equipped.TypeId
                                && FindActiveItemTipsLayout() == null,
                            "tab=" + tabIndex + " pos=" + position
                            + "部位格身份错误或违反老端 SetShowTips(false)", failures);
                        positionCells++;

                        EquipSuitCostItemBind[] costs = ActiveCosts(view);
                        ResonanceConfigs.BuildPreview preview = ResonanceConfigs.Preview(tabIndex, position);
                        materialExpected += preview.Costs.Count;
                        Require(costs.Length == preview.Costs.Count && preview.CanBuild,
                            "tab=" + tabIndex + " pos=" + position + "材料/打造状态异常 costs="
                            + costs.Length + "/" + preview.Costs.Count + " block=" + preview.Block, failures);
                        Canvas.ForceUpdateCanvases();
                        bool costsCentered = IsContentCentered(view.costList);
                        Require(costsCentered,
                            "tab=" + tabIndex + " pos=" + position + " 材料组未按整体宽度居中", failures);
                        componentLayoutChecks++;
                        RectTransform descRect = view.descHtml != null ? view.descHtml.rectTransform : null;
                        Require(view.descHtml != null && view.descHtml.text == "打造消耗"
                                && descRect != null
                                && Mathf.Abs(descRect.anchoredPosition.x - 165f) < 0.1f
                                && Mathf.Abs(descRect.rect.width - 120f) < 0.1f
                                && view.nameXLab != null
                                && view.nameXLab.text.Contains("<color=#00FFEA>", StringComparison.Ordinal),
                            "tab=" + tabIndex + " pos=" + position + "下阶标题/打造消耗布局未对齐老端", failures);
                        visualTypographyChecks++;
                        int costCount = Math.Min(costs.Length, preview.Costs.Count);
                        for (int costIndex = 0; costIndex < costCount; costIndex++)
                        {
                            EquipmentItem materialCell = costs[costIndex]
                                .GetComponentInChildren<EquipmentItem>(false);
                            Color32 expectedCostColor = preview.Costs[costIndex].Enough
                                ? new Color32(0, 250, 100, 255) : new Color32(250, 77, 77, 255);
                            bool costTypography = costs[costIndex].num_text != null
                                && costs[costIndex].num_text.text
                                    == GoodsModel.FormatCountNum(preview.Costs[costIndex].Have) + "/"
                                        + GoodsModel.FormatCountNum(preview.Costs[costIndex].Need)
                                && Mathf.Abs(costs[costIndex].num_text.fontSize - 18f) < 0.01f
                                && SameColor(costs[costIndex].num_text.color, expectedCostColor)
                                && materialCell != null && materialCell.num_text != null
                                && !materialCell.num_text.gameObject.activeSelf;
                            Require(costTypography,
                                "tab=" + tabIndex + " pos=" + position + "材料" + costIndex
                                + "数量层重复或字号/颜色错误", failures);
                            visualTypographyChecks++;
                            Image materialSurface = materialCell != null
                                ? FindRaycastImage(materialCell.click_group) : null;
                            string materialHit = "not-run";
                            bool materialClick = materialSurface != null
                                && Click(materialSurface, camera, raycaster, eventSystem, out materialHit);
                            GoodsTooltipsBind goodsTips = materialClick
                                ? await WaitForShown<GoodsTooltipsBind>(6d) : null;
                            string materialName = GoodsModel.GetGoodsName(preview.Costs[costIndex].TypeId);
                            RectTransform quantityRect = goodsTips != null && goodsTips.quantity_text != null
                                ? goodsTips.quantity_text.rectTransform : null;
                            bool materialIdentity = goodsTips != null && goodsTips.goods_name != null
                                && goodsTips.goods_name.text == materialName
                                && quantityRect != null
                                && Mathf.Abs(quantityRect.rect.width - 240f) < 0.1f
                                && Mathf.Abs(quantityRect.anchoredPosition.x - 120f) < 0.1f
                                && goodsTips.quantity_text.text.Contains(
                                    preview.Costs[costIndex].Have.ToString(), StringComparison.Ordinal);
                            Require(materialClick && materialIdentity,
                                "tab=" + tabIndex + " pos=" + position + "材料详情" + costIndex
                                + "身份/数量布局失败 hit=" + materialHit, failures);
                            visualTypographyChecks++;
                            if (goodsTips != null)
                            {
                                await Task.Delay(120);
                                Canvas.ForceUpdateCanvases();
                                if (goodsTips.transform is RectTransform goodsRoot)
                                    LayoutRebuilder.ForceRebuildLayoutImmediate(goodsRoot);
                                if (goodsTips.btn_group != null)
                                    LayoutRebuilder.ForceRebuildLayoutImmediate(goodsTips.btn_group);
                                Canvas.ForceUpdateCanvases();
                                goodsTips.type_text?.ForceMeshUpdate();
                                HorizontalLayoutGroup buttonLayout = goodsTips.btn_group != null
                                    ? goodsTips.btn_group.GetComponent<HorizontalLayoutGroup>() : null;
                                RectTransform[] buttons =
                                {
                                    goodsTips.useBtn, goodsTips.sellBtn, goodsTips.okBtn,
                                    goodsTips.upShelfBtn, goodsTips.outShelfBtn,
                                    goodsTips.treasureReceiveBtn, goodsTips.takeoutBtn,
                                    goodsTips.depositBtn, goodsTips.putBtn,
                                };
                                int activeButtons = buttons.Count(button => button != null
                                    && button.gameObject.activeInHierarchy);
                                float okCenterDelta = CenterDeltaX(goodsTips.okBtn, goodsTips.btn_group);
                                bool commonPopupLayout = goodsTips.type_text != null
                                    && goodsTips.type_text.rectTransform.rect.width >= 240f
                                    && goodsTips.type_text.textInfo.lineCount <= 1
                                    && buttonLayout != null
                                    && buttonLayout.childAlignment == TextAnchor.MiddleCenter
                                    && activeButtons == 1
                                    && goodsTips.okBtn != null && goodsTips.okBtn.gameObject.activeInHierarchy
                                    && okCenterDelta <= 0.5f;
                                Require(commonPopupLayout,
                                    "共享物品详情布局异常 typeWidth="
                                    + (goodsTips.type_text != null
                                        ? goodsTips.type_text.rectTransform.rect.width : -1f)
                                    + " lines=" + (goodsTips.type_text != null
                                        ? goodsTips.type_text.textInfo.lineCount : -1)
                                    + " activeButtons=" + activeButtons
                                    + " okCenterDelta=" + okCenterDelta, failures);
                                componentLayoutChecks++;
                                if (materialDetails == 0)
                                {
                                    stage.ForceCjkFont();
                                    shots.Add(stage.Capture(EvidenceRoot + "/detail_material.png"));
                                }
                                string goodsCloseHit;
                                bool goodsCloseClick = Click(goodsTips.okBtn,
                                    camera, raycaster, eventSystem, out goodsCloseHit);
                                Require(goodsCloseClick,
                                    "材料详情关闭失败 hit=" + goodsCloseHit, failures);
                                bool goodsClosed = await WaitUntil(
                                    () => !goodsTips.gameObject.activeInHierarchy, 3d);
                                Require(goodsClosed,
                                    "材料详情点击后未关闭 hit=" + goodsCloseHit, failures);
                                if (!goodsClosed)
                                {
                                    ItemTipsView.Close();
                                    ResetItemTipsCache();
                                }
                            }
                            materialDetails++;
                        }

                        int buildFramesBefore = CountProtocol(equipFrames, Proto.EQUIP_SUIT_BUILD);
                        string buildHit;
                        bool buildClick = Click(view.upBtn, camera, raycaster, eventSystem, out buildHit);
                        AlertTypeTwoBind confirm = buildClick
                            ? await WaitForShown<AlertTypeTwoBind>(6d) : null;
                        bool confirmText = confirm != null && confirm._content_html != null
                            && confirm._content_html.text.Contains("共鸣打造")
                            && preview.Costs.All(cost => confirm._content_html.text.Contains(
                                GoodsModel.GetGoodsName(cost.TypeId)));
                        Require(buildClick && confirmText,
                            "tab=" + tabIndex + " pos=" + position + "打造确认失败 hit=" + buildHit, failures);
                        if (confirm != null)
                        {
                            await Task.Delay(150);
                            Canvas.ForceUpdateCanvases();
                            if (confirm.transform is RectTransform confirmRoot)
                                LayoutRebuilder.ForceRebuildLayoutImmediate(confirmRoot);
                            Canvas.ForceUpdateCanvases();
                            if (tabIndex == 0 && positionIndex == 0)
                            {
                                stage.ForceCjkFont();
                                shots.Add(stage.Capture(EvidenceRoot + "/build_confirm.png"));
                            }
                            string cancelHit;
                            bool cancelClick = Click(confirm._cancel_btn,
                                camera, raycaster, eventSystem, out cancelHit);
                            bool cancelClosed = await WaitUntil(
                                () => !confirm.gameObject.activeInHierarchy, 3d);
                            Require(cancelClick && cancelClosed,
                                "打造取消失败 hit=" + cancelHit + " closed=" + cancelClosed, failures);
                            if (!cancelClosed) ConfirmDialog.ReloadView();
                        }
                        Require(CountProtocol(equipFrames, Proto.EQUIP_SUIT_BUILD) == buildFramesBefore,
                            "打造取消后错误发送15221", failures);
                        buildCancels++;

                        if (tabIndex < 3)
                        {
                            string previewHit = "not-run";
                            bool previewClick = view.previewBox != null
                                && view.previewBox.gameObject.activeInHierarchy
                                && Click(view.previewBox, camera, raycaster, eventSystem, out previewHit);
                            EquipSuitPreviewTipsBind previewView = previewClick
                                ? await WaitForShown<EquipSuitPreviewTipsBind>(6d) : null;
                            Require(previewClick && previewView != null && previewView.descLab != null
                                    && previewView.descLab.text.Contains("可激活炫酷特效"),
                                "tab=" + tabIndex + " pos=" + position + "特效预览失败 hit=" + previewHit, failures);
                            if (previewView != null)
                            {
                                if (positionIndex == 0)
                                {
                                    stage.ForceCjkFont();
                                    shots.Add(stage.Capture(EvidenceRoot + "/tab_" + tabIndex + "_preview.png"));
                                }
                                string previewCloseHit = "not-run";
                                Image previewMask = FindActiveImage("ResonancePreviewMask");
                                bool previewClosed = positionIndex % 2 == 0
                                    ? Click(previewView.closeBtn, camera, raycaster, eventSystem,
                                        out previewCloseHit)
                                    : ClickMaskCorner(previewMask, camera, raycaster, eventSystem,
                                        out previewCloseHit);
                                Require(previewClosed,
                                    "预览关闭失败 hit=" + previewCloseHit, failures);
                                await WaitUntil(() => !previewView.gameObject.activeInHierarchy, 3d);
                            }
                            previewChecks++;
                        }

                        EquipReadModel.Instance.UpsertSuit(position,
                            ResonanceConfigs.Tabs[tabIndex].SubType, 1);
                        EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_SUIT_UPDATE);
                        bool returnVisible = await WaitUntil(() => view._btn_back != null
                            && view._btn_back.gameObject.activeInHierarchy, 4d);
                        if (returnVisible)
                        {
                            await Task.Delay(120);
                            Canvas.ForceUpdateCanvases();
                        }
                        string returnHit = "not-run";
                        Image returnButtonImage = returnVisible ? FindRaycastImage(view._btn_back) : null;
                        bool returnClick = returnButtonImage != null
                            && Click(returnButtonImage, camera, raycaster, eventSystem, out returnHit);
                        EquipSuitReturnViewBind returnView = returnClick
                            ? await WaitForShown<EquipSuitReturnViewBind>(6d) : null;
                        bool returnIdentity = returnView != null && returnView._Label2 != null
                            && returnView._Label2.text == "返还材料"
                            && returnView.Content != null
                            && returnView.Content.Cast<Transform>().Any(child =>
                                child.name.StartsWith("ReturnReward_", StringComparison.Ordinal));
                        Require(returnClick && returnIdentity,
                            "tab=" + tabIndex + " pos=" + position + "回退预览失败 hit=" + returnHit, failures);
                        int returnFramesBefore = CountProtocol(equipFrames, Proto.EQUIP_SUIT_RETURN);
                        if (returnView != null)
                        {
                            if (positionIndex == 0)
                            {
                                stage.ForceCjkFont();
                                shots.Add(stage.Capture(EvidenceRoot + "/tab_" + tabIndex + "_return.png"));
                            }
                            string returnCloseHit;
                            Require(Click(returnView._gp_cancel, camera, raycaster, eventSystem, out returnCloseHit),
                                "回退取消失败 hit=" + returnCloseHit, failures);
                            await WaitUntil(() => !returnView.gameObject.activeInHierarchy, 3d);
                        }
                        Require(CountProtocol(equipFrames, Proto.EQUIP_SUIT_RETURN) == returnFramesBefore,
                            "回退取消后错误发送15222", failures);
                        EquipReadModel.Instance.UpsertSuit(position,
                            ResonanceConfigs.Tabs[tabIndex].SubType, 0);
                        EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_SUIT_UPDATE);
                        await WaitUntil(() => view._btn_back == null
                            || !view._btn_back.gameObject.activeInHierarchy, 3d);
                        await Task.Delay(100);
                        returnChecks++;
                        if (DiagnosticPositionLimit > 0 && positionChecks >= DiagnosticPositionLimit)
                            goto RouteAfterPositionAudit;
                    }
                }

            RouteAfterPositionAudit:
                phase = "position-effect-frame";
                TabButtonTwoSkin[] effectTabs = ActiveTabs(window);
                string effectTabHit = "already-selected";
                bool effectTabClick = window.CurrentIndex == 0
                    || (effectTabs.Length == 4
                        && Click(effectTabs[0]._Image1, camera, raycaster, eventSystem, out effectTabHit));
                bool effectTabReady = await WaitUntil(() => window.CurrentIndex == 0
                    && ActivePositions(view).Length == ExpectedPositionCounts[0], 8d);
                Require(effectTabClick && effectTabReady,
                    "部位特效回归无法切回妖魂共鸣 hit=" + effectTabHit, failures);
                byte effectPosition = ResonanceConfigs.GetPositions(ResonanceConfigs.Tabs[0].SuitType)[0];
                EquipSuitPosItemBind effectCard = FindPosition(view, effectPosition);
                string effectPositionHit = "not-run";
                bool effectPositionClick = effectCard != null
                    && Click(effectCard.itemBox, camera, raycaster, eventSystem, out effectPositionHit);
                bool effectPositionReady = await WaitUntil(() => FindPosition(view, effectPosition) != null
                    && view.nameLab != null && !string.IsNullOrEmpty(view.nameLab.text), 5d);
                BagGoods effectEquipment = BagModel.Instance.GetEquipmentAt(effectPosition);
                ushort effectLevel = ResonanceConfigs.GetMaxReachableLevel(
                    effectPosition, ResonanceConfigs.Tabs[0].SubType, effectEquipment);
                Require(effectPositionClick && effectPositionReady && effectEquipment != null && effectLevel > 0,
                    "部位特效回归准备失败 pos=" + effectPosition + " hit=" + effectPositionHit
                    + " level=" + effectLevel, failures);
                if (effectEquipment != null && effectLevel > 0)
                {
                    EquipReadModel.Instance.UpsertSuit(
                        effectPosition, ResonanceConfigs.Tabs[0].SubType, effectLevel);
                    EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_SUIT_UPDATE);
                    EquipmentItem effectCell = null;
                    bool effectAttached = await WaitUntil(() =>
                    {
                        effectCell = PositionEquipmentCell(view, effectPosition);
                        return effectCell != null && HasSuitEffect(effectCell)
                            && effectCell.effBox != null && effectCell.effBox.gameObject.activeInHierarchy
                            && UIEffectStage.CollectDiagnostics().Any(diagnostic =>
                                diagnostic.Label == "ui_shenzhuang01"
                                && diagnostic.ParentName == effectCell.effBox.name
                                && diagnostic.EffectAlive && diagnostic.EffectActiveInHierarchy
                                && diagnostic.ImageHasTexture);
                    }, 10d);
                    UIEffectStage.Handle effectHandle = GetSuitEffectHandle(effectCell);
                    positionEffectMetrics = await CaptureIsolatedEffectPixels(
                        effectHandle, EvidenceRoot + "/effects/position_ui_shenzhuang01.png");
                    effectPixels["position:ui_shenzhuang01"] = positionEffectMetrics.Pixels;
                    // 任意少量像素无法证明 77px 装备槽的贴边流光完整；必须形成可辨认二维框面。
                    positionEffectFrame = effectAttached
                        && positionEffectMetrics.Pixels >= 150
                        && positionEffectMetrics.Width >= 24
                        && positionEffectMetrics.Height >= 24;
                    Require(positionEffectFrame,
                        "共享 EquipmentItem 部位特效框未完成真实出帧 attached=" + effectAttached
                        + " footprint=" + positionEffectMetrics, failures);
                    pageEffectOwnership = await WaitUntil(() =>
                    {
                        EquipmentItem currentDisplay = view.iconSBox != null
                            ? view.iconSBox.GetComponentInChildren<EquipmentItem>(false) : null;
                        EquipmentItem nextDisplay = view.iconXBox != null
                            ? view.iconXBox.GetComponentInChildren<EquipmentItem>(false) : null;
                        UIEffectStage.EffectDiagnostic[] pageEffects = UIEffectStage.CollectDiagnostics()
                            .Where(diagnostic => diagnostic.Label == "ui_shenzhuang01"
                                && (diagnostic.ParentName == "effBox1" || diagnostic.ParentName == "effBox2"))
                            .ToArray();
                        return currentDisplay != null && !HasSuitEffect(currentDisplay)
                            && (nextDisplay == null || !HasSuitEffect(nextDisplay))
                            && pageEffects.Length > 0
                            && pageEffects.All(diagnostic => Mathf.Abs(diagnostic.LocalScale.x) <= 1.21f
                                && Mathf.Abs(diagnostic.LocalScale.y) <= 1.21f
                                && Mathf.Abs(diagnostic.LocalScale.z) <= 1.21f);
                    }, 10d);
                    Require(pageEffectOwnership,
                        "页面中央展示误用了 EquipmentItem 槽位流光或槽位倍率", failures);
                    stage.ForceCjkFont();
                    shots.Add(stage.Capture(EvidenceRoot + "/position_effect_frame.png"));

                    EquipReadModel.Instance.UpsertSuit(
                        effectPosition, ResonanceConfigs.Tabs[0].SubType, 0);
                    EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_SUIT_UPDATE);
                    bool effectCleared = await WaitUntil(() =>
                    {
                        EquipmentItem cell = PositionEquipmentCell(view, effectPosition);
                        return cell != null && !HasSuitEffect(cell)
                            && (cell.effBox == null || !cell.effBox.gameObject.activeInHierarchy)
                            && (cell.effBox1 == null || !cell.effBox1.gameObject.activeInHierarchy)
                            && (cell.effBox2 == null || !cell.effBox2.gameObject.activeInHierarchy)
                            && !UIEffectStage.CollectDiagnostics().Any(diagnostic =>
                                diagnostic.Label == "ui_shenzhuang01"
                                && diagnostic.ParentName == "effBox");
                    }, 8d);
                    Require(effectCleared, "共享 EquipmentItem 部位特效清理失败", failures);
                }

                phase = "success-effect";
                EquipReadController.SuitOperationResult synthetic = CreateSyntheticBuildSuccess();
                EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_SUIT_OPERATION_RESULT, synthetic);
                int successPixels = await CaptureEffectPixels(
                    "ui_gongmingchenggong", EvidenceRoot + "/effects/ui_gongmingchenggong.png");
                effectPixels["success:ui_gongmingchenggong"] = successPixels;
                Require(successPixels >= 8, "打造成功特效没有真实 RT 像素: " + successPixels, failures);

                phase = "instruction";
                string infoHit;
                bool infoClick = Click(view.infoBox, camera, raycaster, eventSystem, out infoHit);
                InstructionViewBind instruction = infoClick
                    ? await WaitForShown<InstructionViewBind>(8d) : null;
                if (instruction != null)
                {
                    // 验收舞台会在运行时把通用 TMP 字体替换为 CJK 字体；字体指标变化后
                    // 必须让说明流重新按最终字体测量，否则截图会换行但保留旧行高。
                    InstructionSmallItemBind previousFirst = ActiveInstructionLines(instruction).FirstOrDefault();
                    stage.ForceCjkFont();
                    InstructionFlow.Show(1524);
                    await WaitUntil(() =>
                    {
                        InstructionSmallItemBind currentFirst = ActiveInstructionLines(instruction).FirstOrDefault();
                        return currentFirst != null && currentFirst != previousFirst;
                    }, 3d);
                }
                Canvas.ForceUpdateCanvases();
                InstructionItemBind[] sections = ActiveInstructionSections(instruction);
                InstructionSmallItemBind[] lines = ActiveInstructionLines(instruction);
                bool instructionOk = instruction != null && instruction._lb_title != null
                    && instruction._lb_title.text == "装备共鸣"
                    && sections.Length == 1 && sections[0]._html_title.text == "共鸣说明"
                    && lines.Length == 7
                    && lines[0]._lb_desc.text.Contains("武防共鸣")
                    && lines[6]._lb_desc.text.Contains("装备阶数")
                    && instruction._panel_item != null && instruction._panel_item.content == instruction._vbox_con
                    && instruction._panel_item.viewport.GetComponent<RectMask2D>() != null;
                Require(infoClick && instructionOk,
                    "共鸣说明错误 hit=" + infoHit + " sections=" + sections.Length + " lines=" + lines.Length,
                    failures);
                if (instruction != null)
                {
                    string instructionLayoutDetail;
                    instructionLayout = InstructionLinesDoNotOverlap(
                        instruction, lines, out instructionLayoutDetail);
                    Require(instructionLayout,
                        "共鸣说明文字重叠: " + instructionLayoutDetail, failures);
                    shots.Add(stage.Capture(EvidenceRoot + "/instruction_top.png"));
                    bool scrollable = instruction._vbox_con.rect.height
                        > instruction._panel_item.viewport.rect.height + 1f;
                    if (scrollable && lines.Length > 0)
                    {
                        string dragHit;
                        bool dragged = Drag(instruction._panel_item, lines[0]._lb_desc,
                            new Vector2(0f, 420f), camera, raycaster, eventSystem, out dragHit);
                        bool moved = await WaitUntil(() => instruction._vbox_con.anchoredPosition.y > 40f, 3d);
                        Require(dragged && moved, "共鸣说明滚动失败 hit=" + dragHit, failures);
                        instructionScrolled = dragged && moved;
                        instruction._panel_item.StopMovement();
                        instruction._panel_item.verticalNormalizedPosition = 0f;
                        Canvas.ForceUpdateCanvases();
                        instructionLastReachable = IsInsideViewport(
                            (RectTransform)lines[lines.Length - 1].transform,
                            instruction._panel_item.viewport);
                        Require(instructionLastReachable, "共鸣说明滚动后末项不可达", failures);
                        stage.ForceCjkFont();
                        shots.Add(stage.Capture(EvidenceRoot + "/instruction_bottom.png"));
                    }
                    else if (lines.Length > 0)
                    {
                        instructionLastReachable = IsInsideViewport(
                            (RectTransform)lines[lines.Length - 1].transform,
                            instruction._panel_item.viewport);
                        Require(instructionLastReachable, "共鸣说明无需滚动但末项不在视口", failures);
                    }
                    string instructionCloseHit;
                    Require(Click(instruction._img_close, camera, raycaster, eventSystem,
                            out instructionCloseHit),
                        "共鸣说明关闭失败 hit=" + instructionCloseHit, failures);
                    await WaitUntil(() => !instruction.gameObject.activeInHierarchy, 3d);
                }

                phase = "close-reopen";
                string closeHit;
                Require(Click(window._img_return0, camera, raycaster, eventSystem, out closeHit),
                    "共鸣返回失败 hit=" + closeHit, failures);
                Require(await WaitUntil(() => !ResonanceFlow.IsOpen, 4d), "共鸣返回后仍打开", failures);
                Require(equipmentView.IsShown && equipmentView.gameObject.activeInHierarchy,
                    "共鸣关闭误关人物父页", failures);

                double warmStart = EditorApplication.timeSinceStartup;
                string reopenHit;
                Require(Click(equipmentView._Group5, camera, raycaster, eventSystem, out reopenHit),
                    "共鸣热重开入口失败 hit=" + reopenHit, failures);
                ResonanceMainView reopened = await WaitForShown<ResonanceMainView>(8d);
                warmMs = MillisecondsSince(warmStart);
                EquipSuitMianViewBind reopenedBind = reopened != null
                    ? reopened.GetComponent<EquipSuitMianViewBind>() : null;
                BaseWindowSkinView reopenedWindow = reopened != null
                    ? FindAncestor<BaseWindowSkinView>(reopened.transform) : null;
                int reopenedIndex = reopenedWindow != null ? reopenedWindow.CurrentIndex : -1;
                int reopenedPositions = reopenedBind != null ? ActivePositions(reopenedBind).Length : -1;
                int reopenedModules = CountResonanceModules();
                int reopenedPreviewMasks = CountActiveNamed("ResonancePreviewMask");
                int reopenedReturnMasks = CountActiveNamed("ResonanceReturnMask");
                bool reopenReady = reopenedBind != null && reopenedWindow != null
                    && reopenedIndex == 0
                    && reopenedPositions == 6
                    && reopenedModules == 1
                    && reopenedPreviewMasks == 0
                    && reopenedReturnMasks == 0;
                Require(reopenReady, "共鸣热重开状态/实例数异常 index=" + reopenedIndex
                    + " positions=" + reopenedPositions + " modules=" + reopenedModules
                    + " previewMasks=" + reopenedPreviewMasks + " returnMasks=" + reopenedReturnMasks, failures);
                stage.ForceCjkFont();
                shots.Add(stage.Capture(EvidenceRoot + "/reopen_ready.png"));
                if (reopenedWindow != null)
                {
                    string finalCloseHit;
                    Require(Click(reopenedWindow._img_return0, camera, raycaster, eventSystem,
                            out finalCloseHit),
                        "共鸣最终关闭失败 hit=" + finalCloseHit, failures);
                    await WaitUntil(() => !ResonanceFlow.IsOpen, 4d);
                }
                await WaitUntil(() => UIEffectStage.CollectDiagnostics().Count == 0, 4d);
                Require(UIEffectStage.CollectDiagnostics().Count == 0,
                    "共鸣关闭后仍残留 UI 特效 handle", failures);

                if (DiagnosticPositionLimit <= 0)
                {
                    Require(stageChecks == ExpectedStageCounts.Sum(), "属性阶覆盖=" + stageChecks, failures);
                    Require(positionChecks == ExpectedPositionCounts.Sum(), "部位覆盖=" + positionChecks, failures);
                    Require(positionCells == ExpectedPositionCounts.Sum(),
                        "部位装备格覆盖=" + positionCells, failures);
                    Require(materialDetails == materialExpected,
                        "材料详情覆盖=" + materialDetails + "/" + materialExpected, failures);
                    Require(buildCancels == ExpectedPositionCounts.Sum(), "打造取消覆盖=" + buildCancels, failures);
                    Require(returnChecks == ExpectedPositionCounts.Sum(), "回退预览覆盖=" + returnChecks, failures);
                    Require(previewChecks == 18, "特效预览覆盖=" + previewChecks, failures);
                }
                Require(CountProtocol(equipFrames, Proto.EQUIP_SUIT_BUILD) == 0
                        && CountProtocol(equipFrames, Proto.EQUIP_SUIT_RETURN) == 0,
                    "无消耗路由出现写协议", failures);
            }
            catch (Exception exception)
            {
                failures.Add("phase=" + phase + " exception=" + exception);
            }
            finally
            {
                ResetFlows();
                typeof(EquipReadController).GetMethod("ClearPending", PrivateInstance)?.Invoke(controller, null);
                if (!controllerWasInitialized && controller.IsInitialized) controller.Dispose();
                equipIntercept?.SetValue(null, oldEquipIntercept);
                bagIntercept?.SetValue(null, oldBagIntercept);
                roleState.Restore();
                bagState.Restore();
                equipState.Restore();
                if (createdEventSystem && eventSystem != null)
                    UnityEngine.Object.DestroyImmediate(eventSystem.gameObject);
                stage.Dispose();
            }

            bool pass = failures.Count == 0;
            string detail = "positions=" + positionChecks + "/22 stages=" + stageChecks + "/46"
                + " positionCells=" + positionCells + "/22 materialDetails="
                + materialDetails + "/" + materialExpected
                + " buildCancels=" + buildCancels + " previews=" + previewChecks
                + " returns=" + returnChecks
                + " visualTypography=" + visualTypographyChecks
                + " componentLayout=" + componentLayoutChecks
                + " positionEffectFrame=" + positionEffectFrame
                + " pageEffectOwnership=" + pageEffectOwnership
                + " positionEffectFootprint=" + positionEffectMetrics
                + " instructionLayout=" + instructionLayout
                + " instructionLastReachable=" + instructionLastReachable
                + " instructionScrolled=" + instructionScrolled
                + " equipFrames=" + ProtocolSummary(equipFrames)
                + " bagFrames=" + bagFrames.Count
                + " effects=" + string.Join(",", effectPixels.Select(pair => pair.Key + "=" + pair.Value))
                + " coldMs=" + coldMs + " warmMs=" + warmMs
                + " shots=" + shots.Count
                + (DiagnosticPositionLimit > 0 ? " diagnosticLimit=" + DiagnosticPositionLimit : string.Empty)
                + (DiagnosticStartTab > 0 ? " diagnosticStartTab=" + DiagnosticStartTab : string.Empty)
                + " failures=" + string.Join(" || ", failures);
            LastDetail = detail;
            Debug.Log("CLIVERIFY resonanceroute " + detail);
            Debug.Log("CLIVERIFY resonanceroute VERDICT pass=" + pass
                + " live15221=False live15222=False restored=True");
            return pass ? 0 : 3;
        }

        private static async Task PrepareData()
        {
            PrepareRole();
            BagModel.Instance.Clear();
            EquipReadModel.Instance.Reset();
            await Task.WhenAll(
                GoodsModel.EnsureLoaded(),
                ResonanceConfigs.EnsureLoaded(),
                InstructionConfigs.EnsureLoaded(),
                EquipmentTipsConfig.EnsureLoaded());

            var equipment = new List<BagGoods>();
            for (int i = 0; i < EquipmentTypeIds.Length; i++)
            {
                equipment.Add(new BagGoods
                {
                    GoodsId = 9100000000L + i,
                    TypeId = EquipmentTypeIds[i],
                    GoodsNum = 1,
                    // 业务品质必须保持配置色 7；共鸣页的老端 plate_4 由 Presenter 仅在显示层覆盖。
                    Color = 7,
                    Cell = i + 1,
                });
            }
            InvokeSetEquipmentFull(equipment);
            EquipReadModel.Instance.ReplaceSuitInfo(new List<EquipReadModel.SuitEntry>());

            var materialIds = new HashSet<int>();
            for (int tabIndex = 0; tabIndex < ResonanceConfigs.Tabs.Length; tabIndex++)
            {
                ResonanceConfigs.TabDefinition tab = ResonanceConfigs.Tabs[tabIndex];
                foreach (byte position in ResonanceConfigs.GetPositions(tab.SuitType))
                {
                    ResonanceConfigs.BuildPreview preview = ResonanceConfigs.Preview(tabIndex, position);
                    for (int i = 0; i < preview.Costs.Count; i++)
                        if (preview.Costs[i].TypeId > 0) materialIds.Add(preview.Costs[i].TypeId);
                }
            }
            var bag = new List<BagGoods>();
            int cell = 1;
            foreach (int typeId in materialIds.OrderBy(value => value))
            {
                bag.Add(new BagGoods
                {
                    GoodsId = 9200000000L + cell,
                    TypeId = typeId,
                    GoodsNum = 999999,
                    Cell = cell++,
                });
            }
            BagModel.Instance.SetBagFull(bag.Count, 200, bag);

            for (int tabIndex = 0; tabIndex < ResonanceConfigs.Tabs.Length; tabIndex++)
            {
                ResonanceConfigs.TabDefinition tab = ResonanceConfigs.Tabs[tabIndex];
                IReadOnlyList<ResonanceConfigs.SuitItem> stages =
                    ResonanceConfigs.GetSuitItems(tab.SuitType, tab.SubType);
                foreach (byte position in ResonanceConfigs.GetPositions(tab.SuitType))
                {
                    for (int i = 0; i < stages.Count; i++)
                    {
                        var powers = new List<EquipReadModel.SuitPowerEntry>();
                        for (int tierIndex = 0; tierIndex < stages[i].Tiers.Count; tierIndex++)
                        {
                            int count = stages[i].Tiers[tierIndex].Count;
                            powers.Add(new EquipReadModel.SuitPowerEntry(
                                unchecked((byte)count),
                                unchecked((ulong)(100000 + tabIndex * 10000 + i * 100 + count))));
                        }
                        EquipReadModel.Instance.ReplaceSuitPower(
                            new EquipReadModel.SuitPowerSnapshot(position, tab.SubType,
                                stages[i].Level, powers));
                    }

                    ResonanceConfigs.BuildPreview preview = ResonanceConfigs.Preview(tabIndex, position);
                    var rewards = new List<EquipReadModel.RewardEntry>();
                    for (int i = 0; i < preview.Costs.Count; i++)
                    {
                        ResonanceConfigs.CostItem cost = preview.Costs[i];
                        rewards.Add(new EquipReadModel.RewardEntry(
                            0, unchecked((uint)cost.TypeId),
                            unchecked((ushort)Mathf.Clamp(cost.Need, 1, ushort.MaxValue)), "[]"));
                    }
                    EquipReadModel.Instance.ReplaceReturnPreview(
                        new EquipReadModel.SuitReturnPreview(position, tab.SubType, rewards));
                }
            }
        }

        private static void PrepareRole()
        {
            RoleModel role = RoleModel.Instance;
            role.Reset();
            role.RoleId = 4294967524L;
            role.Level = 630;
            role.Exp = 2070000000000L;
            role.ExpLim = 11750000000000L;
            role.CombatPower = 22868566L;
            role.Figure = new FigureProto
            {
                name = "111111",
                career = 1,
                sex = 1,
                level = 630,
                turn = 5,
            };
            role.BattleAttr = new BattleAttrProto { Hp = 1868655, HpLim = 1868655, Speed = 250 };
            role.MarkBaseInfoReady();
        }

        private static void VerifyIdentityAndStructure(ResonanceMainView main, EquipSuitMianViewBind view,
            BaseWindowSkinView window, List<string> failures)
        {
            Require(main != null && view != null && window != null && main.IsShown && window.IsShown,
                "共鸣主窗身份缺失", failures);
            Require(main.transform.IsChildOf(window._gp_item_con), "共鸣主内容未进入共享窗内容层", failures);
            Require(ActiveTabs(window).Length == 4, "共鸣共享页签不是4个", failures);
            Require(ActiveTabs(window).Select(tab => tab.GetComponentInChildren<TMPro.TextMeshProUGUI>(false)?.text)
                    .SequenceEqual(ResonanceConfigs.Tabs.Select(tab => tab.Label)),
                "共鸣页签文案/顺序错误", failures);
            Require(ValidScroll(view.posList, true) && ValidScroll(view.atrsList, true)
                    && ValidScroll(view.costList, false),
                "共鸣主滚动结构不完整", failures);
            Image previewMask = FindActiveOrInactiveImage("ResonancePreviewMask");
            Image returnMask = FindActiveOrInactiveImage("ResonanceReturnMask");
            Require(FullMask(previewMask) && FullMask(returnMask), "共鸣弹层遮罩不是全屏命中层", failures);
            Require(view.giftIcon != null && !view.giftIcon.gameObject.activeInHierarchy,
                "无 eGongMing 类型切片时礼包入口不应伪显示", failures);
        }

        private static void VerifyPositionScroll(EquipSuitMianViewBind view, Camera camera,
            GraphicRaycaster raycaster, EventSystem eventSystem, List<string> failures)
        {
            EquipSuitPosItemBind[] positions = ActivePositions(view);
            if (positions.Length == 0 || view.posList == null) return;
            Canvas.ForceUpdateCanvases();
            bool scrollable = view.posList.content.rect.height > view.posList.viewport.rect.height + 1f;
            if (!scrollable) return;
            float before = view.posList.content.anchoredPosition.y;
            string dragHit;
            bool dragged = Drag(view.posList, positions[0].bgImg, new Vector2(0f, 180f),
                camera, raycaster, eventSystem, out dragHit);
            Canvas.ForceUpdateCanvases();
            bool moved = Mathf.Abs(view.posList.content.anchoredPosition.y - before) > 1f;
            Require(dragged && moved, "共鸣部位列表真实拖动失败 hit=" + dragHit, failures);
            view.posList.StopMovement();
            view.posList.verticalNormalizedPosition = 1f;
            Canvas.ForceUpdateCanvases();
        }

        private static EquipReadController.SuitOperationResult CreateSyntheticBuildSuccess()
        {
            var result = new EquipReadController.SuitOperationResult();
            SetProperty(result, "Protocol", Proto.EQUIP_SUIT_BUILD);
            SetProperty(result, "Success", true);
            SetProperty(result, "WasRequested", true);
            SetProperty(result, "EquipType", (byte)1);
            SetProperty(result, "MakeType", (byte)1);
            SetProperty(result, "Level", (ushort)1);
            return result;
        }

        private static void SetProperty(object target, string name, object value)
        {
            target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public)
                ?.SetValue(target, value);
        }

        private static async Task<int> CaptureEffectPixels(string label, string projectRelativePng)
        {
            bool ready = await WaitUntil(() => UIEffectStage.CollectDiagnostics().Any(diagnostic =>
                diagnostic.Label == label && diagnostic.EffectAlive && diagnostic.ImageHasTexture), 10d);
            if (!ready) return 0;
            SimulateEffect(label);
            typeof(UIEffectStage).GetMethod("Tick", PrivateStatic)?.Invoke(null, null);
            UIEffectStage.ChannelDiagnostic channel = UIEffectStage.CollectChannelDiagnostics()
                .FirstOrDefault(candidate => candidate.HandleCount > 0 && candidate.Texture != null
                    && candidate.Camera != null);
            if (channel.Texture == null || channel.Camera == null) return 0;
            channel.Camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = channel.Texture;
            var texture = new Texture2D(channel.Texture.width, channel.Texture.height,
                TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0f, 0f, channel.Texture.width, channel.Texture.height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            Color32[] pixels = texture.GetPixels32();
            int nonTransparent = pixels.Count(pixel => pixel.a >= 4);
            string full = Path.GetFullPath(projectRelativePng);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllBytes(full, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            return nonTransparent;
        }

        private static async Task<EffectPixelMetrics> CaptureIsolatedEffectPixels(
            UIEffectStage.Handle target, string projectRelativePng)
        {
            if (target == null) return default;
            bool ready = await WaitUntil(() => GetHandleEffect(target) != null, 10d);
            if (!ready) return default;

            object channel = target.GetType().GetField("SharedChannel", AnyInstance)?.GetValue(target);
            if (channel == null) return default;
            Camera camera = channel.GetType().GetField("Camera", AnyInstance)?.GetValue(channel) as Camera;
            RenderTexture renderTexture = channel.GetType().GetField("Texture", AnyInstance)?.GetValue(channel)
                as RenderTexture;
            if (camera == null || renderTexture == null) return default;

            var wrapperStates = new List<(GameObject wrapper, bool active)>();
            FieldInfo liveField = typeof(UIEffectStage).GetField("s_live", PrivateStatic);
            if (liveField?.GetValue(null) is IEnumerable live)
            {
                foreach (object handle in live)
                {
                    Transform wrapper = handle?.GetType().GetField("Wrapper", AnyInstance)
                        ?.GetValue(handle) as Transform;
                    if (wrapper == null) continue;
                    wrapperStates.Add((wrapper.gameObject, wrapper.gameObject.activeSelf));
                    wrapper.gameObject.SetActive(ReferenceEquals(handle, target));
                }
            }

            Texture2D texture = null;
            RenderTexture previous = RenderTexture.active;
            try
            {
                SimulateEffect(target);
                typeof(UIEffectStage).GetMethod("Tick", PrivateStatic)?.Invoke(null, null);
                camera.Render();
                RenderTexture.active = renderTexture;
                texture = new Texture2D(renderTexture.width, renderTexture.height,
                    TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0f, 0f, renderTexture.width, renderTexture.height), 0, 0);
                texture.Apply();

                int count = 0;
                int minX = renderTexture.width;
                int minY = renderTexture.height;
                int maxX = -1;
                int maxY = -1;
                Color32[] pixels = texture.GetPixels32();
                for (int y = 0; y < renderTexture.height; y++)
                {
                    int row = y * renderTexture.width;
                    for (int x = 0; x < renderTexture.width; x++)
                    {
                        if (pixels[row + x].a < 4) continue;
                        count++;
                        minX = Math.Min(minX, x);
                        minY = Math.Min(minY, y);
                        maxX = Math.Max(maxX, x);
                        maxY = Math.Max(maxY, y);
                    }
                }

                string full = Path.GetFullPath(projectRelativePng);
                Directory.CreateDirectory(Path.GetDirectoryName(full));
                File.WriteAllBytes(full, texture.EncodeToPNG());
                int width = maxX >= minX ? maxX - minX + 1 : 0;
                int height = maxY >= minY ? maxY - minY + 1 : 0;
                return new EffectPixelMetrics(count, width, height);
            }
            finally
            {
                RenderTexture.active = previous;
                if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
                for (int i = 0; i < wrapperStates.Count; i++)
                {
                    var state = wrapperStates[i];
                    if (state.wrapper != null) state.wrapper.SetActive(state.active);
                }
                typeof(UIEffectStage).GetMethod("Tick", PrivateStatic)?.Invoke(null, null);
            }
        }

        private static void SimulateEffect(string label)
        {
            FieldInfo liveField = typeof(UIEffectStage).GetField("s_live", PrivateStatic);
            if (!(liveField?.GetValue(null) is IEnumerable live)) return;
            foreach (object handle in live)
            {
                if (handle == null) continue;
                Type type = handle.GetType();
                string handleLabel = type.GetField("Label", PrivateInstance)?.GetValue(handle) as string;
                if (handleLabel != label) continue;
                SimulateEffect(handle as UIEffectStage.Handle);
            }
        }

        private static void SimulateEffect(UIEffectStage.Handle handle)
        {
            GameObject effect = GetHandleEffect(handle);
            if (effect == null) return;
            foreach (ParticleSystem particle in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                try
                {
                    particle.Simulate(0.35f, true, true, false);
                    particle.Play(true);
                }
                catch (Exception) { }
            }
            foreach (Animation animation in effect.GetComponentsInChildren<Animation>(true))
            {
                try
                {
                    foreach (AnimationState state in animation) state.normalizedTime = 0.35f;
                    animation.Sample();
                }
                catch (Exception) { }
            }
        }

        private static GameObject GetHandleEffect(UIEffectStage.Handle handle)
            => handle?.GetType().GetField("Effect", AnyInstance)?.GetValue(handle) as GameObject;

        private static void ResetFlows()
        {
            ItemTipsView.Close();
            ResetItemTipsCache();
            InstructionFlow.Close();
            typeof(InstructionFlow).GetMethod("Reset", PrivateStatic)?.Invoke(null, null);
            ConfirmDialog.ReloadView();
            TipsManager.ReloadView();
            ResonanceFlow.Reset();
            typeof(RoleFlow).GetMethod("Reset", PrivateStatic)?.Invoke(null, null);
        }

        private static void ResetItemTipsCache()
        {
            FieldInfo rootField = typeof(ItemTipsView).GetField("_moduleRoot", PrivateStatic);
            GameObject root = rootField?.GetValue(null) as GameObject;
            if (root != null) ResManager.ReleaseInstance(root);
            rootField?.SetValue(null, null);
            typeof(ItemTipsView).GetField("_layout", PrivateStatic)?.SetValue(null, null);
            typeof(ItemTipsView).GetField("_normalIconCell", PrivateStatic)?.SetValue(null, null);
        }

        private static void InvokeSetEquipmentFull(List<BagGoods> goods)
        {
            typeof(BagModel).GetMethod("SetEquipmentFull", PrivateInstance)
                ?.Invoke(BagModel.Instance, new object[] { 10, goods });
        }

        private static TabButtonTwoSkin[] ActiveTabs(BaseWindowSkinView window)
            => window == null ? Array.Empty<TabButtonTwoSkin>()
                : window.GetComponentsInChildren<TabButtonTwoSkin>(false)
                    .OrderBy(tab => tab.transform.GetSiblingIndex()).ToArray();

        private static EquipSuitPosItemBind[] ActivePositions(EquipSuitMianViewBind view)
            => view?.posList?.content == null ? Array.Empty<EquipSuitPosItemBind>()
                : view.posList.content.GetComponentsInChildren<EquipSuitPosItemBind>(false)
                    .Where(item => item.gameObject.name.StartsWith("ResonancePosition_", StringComparison.Ordinal))
                    .OrderBy(item => item.transform.GetSiblingIndex()).ToArray();

        private static EquipSuitPosItemBind FindPosition(EquipSuitMianViewBind view, byte position)
            => ActivePositions(view).FirstOrDefault(item =>
                item.gameObject.name == "ResonancePosition_" + position);

        private static EquipSuitCostItemBind[] ActiveCosts(EquipSuitMianViewBind view)
            => view?.costList?.content == null ? Array.Empty<EquipSuitCostItemBind>()
                : view.costList.content.GetComponentsInChildren<EquipSuitCostItemBind>(false)
                    .Where(item => item.gameObject.name.StartsWith("ResonanceCost_", StringComparison.Ordinal))
                    .OrderBy(item => item.transform.GetSiblingIndex()).ToArray();

        private static bool IsContentCentered(ScrollRect scroll)
        {
            if (scroll?.content == null || scroll.viewport == null) return false;
            Vector3 worldCenter = scroll.content.TransformPoint(scroll.content.rect.center);
            Vector3 viewportCenter = scroll.viewport.InverseTransformPoint(worldCenter);
            return Mathf.Abs(viewportCenter.x - scroll.viewport.rect.center.x) <= 0.5f;
        }

        private static float CenterDeltaX(RectTransform child, RectTransform parent)
        {
            if (child == null || parent == null) return float.PositiveInfinity;
            Vector3 worldCenter = child.TransformPoint(child.rect.center);
            Vector3 localCenter = parent.InverseTransformPoint(worldCenter);
            return Mathf.Abs(localCenter.x - parent.rect.center.x);
        }

        private static EquipmentItem PositionEquipmentCell(EquipSuitMianViewBind view, byte position)
        {
            EquipSuitPosItemBind card = FindPosition(view, position);
            return card?.iconBox != null
                ? card.iconBox.GetComponentInChildren<EquipmentItem>(false) : null;
        }

        private static UIEffectStage.Handle GetSuitEffectHandle(EquipmentItem item)
            => item == null ? null : typeof(EquipmentItem).GetField("_suitEffect", PrivateInstance)
                ?.GetValue(item) as UIEffectStage.Handle;

        private static bool HasSuitEffect(EquipmentItem item) => GetSuitEffectHandle(item) != null;

        private static EquipNewSuitAttrItemBind[] ActiveAttributes(EquipSuitMianViewBind view)
            => view?.atrsList?.content == null ? Array.Empty<EquipNewSuitAttrItemBind>()
                : view.atrsList.content.GetComponentsInChildren<EquipNewSuitAttrItemBind>(false)
                    .Where(item => item.gameObject.name.StartsWith("ResonanceAttr_", StringComparison.Ordinal))
                    .OrderBy(item => item.transform.GetSiblingIndex()).ToArray();

        private static InstructionItemBind[] ActiveInstructionSections(InstructionViewBind view)
            => view == null ? Array.Empty<InstructionItemBind>()
                : view._vbox_con.GetComponentsInChildren<InstructionItemBind>(false)
                    .OrderBy(item => item.transform.GetSiblingIndex()).ToArray();

        private static InstructionSmallItemBind[] ActiveInstructionLines(InstructionViewBind view)
            => view == null ? Array.Empty<InstructionSmallItemBind>()
                : ActiveInstructionSections(view).SelectMany(section => section._vbox_con
                    .GetComponentsInChildren<InstructionSmallItemBind>(false)
                    .OrderBy(line => line.transform.GetSiblingIndex())).ToArray();

        private static bool InstructionLinesDoNotOverlap(InstructionViewBind view,
            IReadOnlyList<InstructionSmallItemBind> lines, out string detail)
        {
            detail = "ok";
            if (view == null || view._vbox_con == null || lines == null || lines.Count == 0)
            {
                detail = "missing-view-or-lines";
                return false;
            }

            for (int index = 0; index + 1 < lines.Count; index++)
            {
                RectTransform current = lines[index]?._lb_desc?.rectTransform;
                RectTransform next = lines[index + 1]?._lb_desc?.rectTransform;
                if (current == null || next == null)
                {
                    detail = "missing-text-rect-" + index;
                    return false;
                }

                Bounds currentBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                    view._vbox_con, current);
                Bounds nextBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                    view._vbox_con, next);
                float gap = currentBounds.min.y - nextBounds.max.y;
                if (gap < -0.5f)
                {
                    detail = "pair=" + index + "/" + (index + 1) + " gap=" + gap.ToString("F1");
                    return false;
                }
            }

            return true;
        }

        private static bool IsInsideViewport(RectTransform item, RectTransform viewport)
        {
            if (item == null || viewport == null || !item.gameObject.activeInHierarchy) return false;
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, item);
            Rect rect = viewport.rect;
            return bounds.min.y >= rect.yMin - 1f && bounds.max.y <= rect.yMax + 1f;
        }

        private static T FindAncestor<T>(Transform transform) where T : Component
        {
            for (Transform current = transform; current != null; current = current.parent)
            {
                T component = current.GetComponent<T>();
                if (component != null) return component;
            }
            return null;
        }

        private static Image FindActiveImage(string name)
            => UnityEngine.Object.FindObjectsByType<Image>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(image => image != null && image.gameObject.name == name);

        private static Image FindActiveOrInactiveImage(string name)
            => UnityEngine.Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(image => image != null && image.gameObject.name == name);

        private static Image FindRaycastImage(RectTransform root)
            => root == null ? null : root.GetComponentsInChildren<Image>(true)
                .FirstOrDefault(image => image != null && image.raycastTarget
                    && image.gameObject.activeInHierarchy);

        private static ItemTipsModalLayout FindActiveItemTipsLayout()
            => UnityEngine.Object.FindObjectsByType<ItemTipsModalLayout>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(layout => layout != null && layout.gameObject.activeInHierarchy
                    && layout.dimBlocker != null && layout.dimBlocker.gameObject.activeInHierarchy);

        private static int CountActiveNamed(string name)
            => UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Count(transform => transform != null && transform.gameObject.name == name);

        private static int CountResonanceModules()
            => UnityEngine.Object.FindObjectsByType<ResonanceMainView>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Count(view => view != null && view.gameObject.scene.IsValid());

        private static bool ValidScroll(ScrollRect scroll, bool vertical)
            => scroll != null && scroll.enabled && scroll.content != null && scroll.viewport != null
                && scroll.viewport.GetComponent<RectMask2D>() != null
                && scroll.content.GetComponent<ContentSizeFitter>() != null
                && (scroll.content.GetComponent<GridLayoutGroup>() != null
                    || (vertical
                        ? scroll.content.GetComponent<VerticalLayoutGroup>() != null
                        : scroll.content.GetComponent<HorizontalLayoutGroup>() != null));

        private static bool FullMask(Image image)
            => image != null && image.raycastTarget
                && Near(image.rectTransform.anchorMin, Vector2.zero)
                && Near(image.rectTransform.anchorMax, Vector2.one)
                && Near(image.rectTransform.offsetMin, Vector2.zero)
                && Near(image.rectTransform.offsetMax, Vector2.zero);

        private static async Task<T> WaitForShown<T>(double timeoutSeconds) where T : BaseView
        {
            T result = null;
            await WaitUntil(() =>
            {
                result = UnityEngine.Object.FindObjectsByType<T>(
                        FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(view => view != null && view.IsShown
                        && view.gameObject.activeInHierarchy);
                return result != null;
            }, timeoutSeconds);
            return result;
        }

        private static async Task<bool> WaitUntil(Func<bool> predicate, double timeoutSeconds)
        {
            double deadline = EditorApplication.timeSinceStartup + timeoutSeconds;
            while (EditorApplication.timeSinceStartup < deadline)
            {
                if (predicate()) return true;
                await Task.Delay(50);
            }
            return predicate();
        }

        private static bool Click(Component target, Camera camera, GraphicRaycaster raycaster,
            EventSystem eventSystem, out string hitName)
        {
            hitName = "missing";
            RectTransform rect = target != null ? target.transform as RectTransform : null;
            if (rect == null || !rect.gameObject.activeInHierarchy) return false;
            Canvas.ForceUpdateCanvases();
            // EditMode 不会像真实 Player 那样每帧提交 CanvasRenderer.absoluteDepth；
            // 页面刷新/弹窗复用后先真实 Render 一帧，否则 GraphicRaycaster 会把已注册 Graphic 当 depth=-1 跳过。
            if (camera != null && camera.isActiveAndEnabled) camera.Render();
            Canvas.ForceUpdateCanvases();
            Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
            bool clicked = ClickAt(point, rect, raycaster, eventSystem, out hitName);
            if (!clicked)
            {
                var corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                Graphic graphic = target as Graphic;
                Canvas targetCanvas = graphic != null ? graphic.canvas : null;
                IList<Graphic> registered = targetCanvas != null
                    ? GraphicRegistry.GetGraphicsForCanvas(targetCanvas) : null;
                RectMask2D clip = rect.GetComponentInParent<RectMask2D>();
                string clipDetail = "none";
                if (clip != null)
                {
                    var clipCorners = new Vector3[4];
                    clip.rectTransform.GetWorldCorners(clipCorners);
                    clipDetail = clip.name + " screenBL="
                        + RectTransformUtility.WorldToScreenPoint(camera, clipCorners[0]).ToString("F1")
                        + " screenTR="
                        + RectTransformUtility.WorldToScreenPoint(camera, clipCorners[2]).ToString("F1")
                        + " contains=" + RectTransformUtility.RectangleContainsScreenPoint(
                            clip.rectTransform, point, camera);
                }
                hitName += "|target=" + rect.name
                    + " point=" + point.ToString("F1")
                    + " local=" + rect.rect.ToString()
                    + " worldBL=" + corners[0].ToString("F1")
                    + " worldTR=" + corners[2].ToString("F1")
                    + " anchored=" + rect.anchoredPosition.ToString("F1")
                    + " raycast=" + (graphic != null && graphic.raycastTarget)
                    + " enabled=" + (graphic != null && graphic.enabled)
                    + " depth=" + (graphic != null ? graphic.depth : -999)
                    + " culled=" + (graphic != null && graphic.canvasRenderer.cull)
                    + " alpha=" + (graphic != null ? graphic.color.a : -1f)
                    + " canvas=" + (targetCanvas != null ? targetCanvas.name : "none")
                    + " rayCanvas=" + (raycaster != null && raycaster.GetComponent<Canvas>() != null
                        ? raycaster.GetComponent<Canvas>().name : "none")
                    + " registered=" + (graphic != null && registered != null && registered.Contains(graphic))
                    + "/" + (registered != null ? registered.Count : -1)
                    + " parent=" + (rect.parent != null ? rect.parent.name : "none")
                    + " clip=" + clipDetail;
            }
            return clicked;
        }

        private static bool ClickVisualCenter(Component target, Camera camera, GraphicRaycaster raycaster,
            EventSystem eventSystem, out string hitName)
        {
            hitName = "missing";
            RectTransform rect = target != null ? target.transform as RectTransform : null;
            if (rect == null || !rect.gameObject.activeInHierarchy) return false;
            Canvas.ForceUpdateCanvases();
            if (camera != null && camera.isActiveAndEnabled) camera.Render();
            Canvas.ForceUpdateCanvases();
            var pointer = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(
                    camera, rect.TransformPoint(rect.rect.center)),
            };
            var hits = new List<RaycastResult>();
            raycaster.Raycast(pointer, hits);
            if (hits.Count == 0) return false;
            RaycastResult top = hits[0];
            GameObject handled = ExecuteEvents.ExecuteHierarchy<IPointerClickHandler>(
                top.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            hitName = top.gameObject.name + "->" + (handled != null ? handled.name : "unhandled");
            return handled != null;
        }

        private static bool ClickMaskCorner(Image mask, Camera camera, GraphicRaycaster raycaster,
            EventSystem eventSystem, out string hitName)
        {
            hitName = "missing";
            RectTransform rect = mask != null ? mask.rectTransform : null;
            if (rect == null || !rect.gameObject.activeInHierarchy) return false;
            Canvas.ForceUpdateCanvases();
            if (camera != null && camera.isActiveAndEnabled) camera.Render();
            Canvas.ForceUpdateCanvases();
            Vector3 localPoint = new Vector3(rect.rect.xMin + 20f, rect.rect.yMin + 20f, 0f);
            Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(localPoint));
            return ClickAt(point, rect, raycaster, eventSystem, out hitName);
        }

        private static bool ClickAt(Vector2 point, RectTransform scope, GraphicRaycaster raycaster,
            EventSystem eventSystem, out string hitName)
        {
            var pointer = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = point,
            };
            var hits = new List<RaycastResult>();
            raycaster.Raycast(pointer, hits);
            foreach (RaycastResult result in hits)
            {
                Transform hit = result.gameObject.transform;
                if (hit != scope && !hit.IsChildOf(scope)) continue;
                hitName = result.gameObject.name;
                ExecuteEvents.ExecuteHierarchy<IPointerClickHandler>(
                    result.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                return true;
            }
            hitName = string.Join("/", hits.Select(result => result.gameObject.name));
            return false;
        }

        private static bool Drag(ScrollRect scroll, Component surface, Vector2 delta, Camera camera,
            GraphicRaycaster raycaster, EventSystem eventSystem, out string hitName)
        {
            hitName = "missing";
            RectTransform scrollRect = scroll != null ? scroll.transform as RectTransform : null;
            RectTransform surfaceRect = surface != null ? surface.transform as RectTransform : null;
            if (scrollRect == null || surfaceRect == null) return false;
            Canvas.ForceUpdateCanvases();
            if (camera != null && camera.isActiveAndEnabled) camera.Render();
            Canvas.ForceUpdateCanvases();
            Vector2 start = RectTransformUtility.WorldToScreenPoint(
                camera, surfaceRect.TransformPoint(surfaceRect.rect.center));
            var pointer = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = start,
                pressPosition = start,
            };
            var hits = new List<RaycastResult>();
            raycaster.Raycast(pointer, hits);
            RaycastResult hit = hits.FirstOrDefault(result => result.gameObject.transform == scrollRect
                || result.gameObject.transform.IsChildOf(scrollRect));
            if (hit.gameObject == null)
            {
                hitName = string.Join("/", hits.Select(result => result.gameObject.name));
                return false;
            }
            hitName = hit.gameObject.name;
            pointer.pointerPressRaycast = hit;
            ExecuteEvents.Execute(scroll.gameObject, pointer, ExecuteEvents.beginDragHandler);
            pointer.delta = delta;
            pointer.position = start + delta;
            ExecuteEvents.Execute(scroll.gameObject, pointer, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(scroll.gameObject, pointer, ExecuteEvents.endDragHandler);
            return true;
        }

        private static bool Require(bool condition, string failure, List<string> failures)
        {
            if (!condition) failures.Add(failure);
            return condition;
        }

        private static bool SameColor(Color actual, Color32 expected)
        {
            Color32 converted = actual;
            return converted.r == expected.r && converted.g == expected.g
                && converted.b == expected.b && converted.a == expected.a;
        }

        private static int CountProtocol(IEnumerable<byte[]> frames, int protocol)
            => frames.Count(frame => frame != null && frame.Length >= 6
                && ((frame[4] << 8) | frame[5]) == protocol);

        private static string ProtocolSummary(IEnumerable<byte[]> frames)
            => string.Join(",", frames.GroupBy(frame => frame != null && frame.Length >= 6
                    ? (frame[4] << 8) | frame[5] : -1)
                .OrderBy(group => group.Key)
                .Select(group => group.Key + "x" + group.Count()));

        private static long MillisecondsSince(double start)
            => Math.Max(0L, (long)Math.Round((EditorApplication.timeSinceStartup - start) * 1000d));

        private static bool Near(Vector2 left, Vector2 right)
            => Vector2.Distance(left, right) <= 1f;

        private sealed class RoleState
        {
            private readonly long _roleId = RoleModel.Instance.RoleId;
            private readonly int _level = RoleModel.Instance.Level;
            private readonly long _exp = RoleModel.Instance.Exp;
            private readonly long _expLim = RoleModel.Instance.ExpLim;
            private readonly long _combatPower = RoleModel.Instance.CombatPower;
            private readonly FigureProto _figure = RoleModel.Instance.Figure;
            private readonly BattleAttrProto _battle = RoleModel.Instance.BattleAttr;
            private readonly bool _hasBaseInfo = RoleModel.Instance.HasBaseInfo;

            public void Restore()
            {
                RoleModel role = RoleModel.Instance;
                role.Reset();
                role.RoleId = _roleId;
                role.Level = _level;
                role.Exp = _exp;
                role.ExpLim = _expLim;
                role.CombatPower = _combatPower;
                role.Figure = _figure;
                role.BattleAttr = _battle;
                SetBackingField(role, "HasBaseInfo", _hasBaseInfo);
            }
        }

        private sealed class BagState
        {
            private readonly bool _hasBag = BagModel.Instance.HasData;
            private readonly bool _hasEquipment = BagModel.Instance.HasEquipmentData;
            private readonly int _bagMax = BagModel.Instance.GetMaxCell(BagModel.POS_BAG);
            private readonly int _equipmentMax = BagModel.Instance.GetMaxCell(BagModel.POS_EQUIP);
            private readonly int _cellNum = BagModel.Instance.CellNum;
            private readonly List<BagGoods> _bag = new List<BagGoods>(BagModel.Instance.BagGoodsList);
            private readonly List<BagGoods> _equipment =
                new List<BagGoods>(BagModel.Instance.GetContainer(BagModel.POS_EQUIP));

            public void Restore()
            {
                BagModel.Instance.Clear();
                if (_hasBag) BagModel.Instance.SetBagFull(_cellNum, _bagMax, _bag);
                if (_hasEquipment)
                {
                    typeof(BagModel).GetMethod("SetEquipmentFull", PrivateInstance)
                        ?.Invoke(BagModel.Instance, new object[] { _equipmentMax, _equipment });
                }
            }
        }

        private sealed class EquipState
        {
            private readonly bool _hasGodInfo = EquipReadModel.Instance.HasGodInfo;
            private readonly uint _godTotalPower = EquipReadModel.Instance.GodTotalPower;
            private readonly IReadOnlyList<EquipReadModel.GodEntry> _godEntries = EquipReadModel.Instance.GodEntries;
            private readonly bool _hasGodPowerPreview = EquipReadModel.Instance.HasGodPowerPreview;
            private readonly uint _godPowerPreview = EquipReadModel.Instance.GodPowerPreview;
            private readonly bool _hasSuitInfo = EquipReadModel.Instance.HasSuitInfo;
            private readonly IReadOnlyList<EquipReadModel.SuitEntry> _suitEntries = EquipReadModel.Instance.SuitEntries;
            private readonly int _version = EquipReadModel.Instance.Version;
            private readonly Dictionary<ushort, EquipReadModel.SuitReturnPreview> _returns;
            private readonly Dictionary<uint, EquipReadModel.SuitPowerSnapshot> _powers;

            public EquipState()
            {
                _returns = CloneDictionary<ushort, EquipReadModel.SuitReturnPreview>(
                    EquipReadModel.Instance, "_returnPreviews");
                _powers = CloneDictionary<uint, EquipReadModel.SuitPowerSnapshot>(
                    EquipReadModel.Instance, "_suitPowers");
            }

            public void Restore()
            {
                EquipReadModel model = EquipReadModel.Instance;
                model.Reset();
                SetBackingField(model, "HasGodInfo", _hasGodInfo);
                SetBackingField(model, "GodTotalPower", _godTotalPower);
                SetBackingField(model, "GodEntries", _godEntries);
                SetBackingField(model, "HasGodPowerPreview", _hasGodPowerPreview);
                SetBackingField(model, "GodPowerPreview", _godPowerPreview);
                SetBackingField(model, "HasSuitInfo", _hasSuitInfo);
                SetBackingField(model, "SuitEntries", _suitEntries);
                SetBackingField(model, "Version", _version);
                RestoreDictionary(model, "_returnPreviews", _returns);
                RestoreDictionary(model, "_suitPowers", _powers);
            }
        }

        private static Dictionary<TKey, TValue> CloneDictionary<TKey, TValue>(object owner, string name)
        {
            IDictionary<TKey, TValue> source = owner.GetType().GetField(name, PrivateInstance)
                ?.GetValue(owner) as IDictionary<TKey, TValue>;
            return source != null ? new Dictionary<TKey, TValue>(source) : new Dictionary<TKey, TValue>();
        }

        private static void RestoreDictionary<TKey, TValue>(object owner, string name,
            Dictionary<TKey, TValue> saved)
        {
            IDictionary<TKey, TValue> target = owner.GetType().GetField(name, PrivateInstance)
                ?.GetValue(owner) as IDictionary<TKey, TValue>;
            if (target == null) return;
            target.Clear();
            foreach (KeyValuePair<TKey, TValue> pair in saved) target[pair.Key] = pair.Value;
        }

        private static void SetBackingField(object owner, string property, object value)
        {
            owner.GetType().GetField("<" + property + ">k__BackingField", PrivateInstance)
                ?.SetValue(owner, value);
        }
    }
}
