using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Common.Proto;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.AttributePotion;
using Shenxiao.Generated.UI.Common;
using Shenxiao.Module.Core.AttributePotion;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 人物→药水完整路由。真实射线覆盖四档页签、16 个物品详情、16 个使用按钮、
    /// 首用指引、权威 21701 即时刷新、背包即时刷新、滚动、关闭、遮罩和热重开。
    /// 所有使用均被 Editor 拦截，不会连接服务器或消耗真实道具。
    /// </summary>
    public static class RoleAttributePotionRouteCase
    {
        private const string EvidenceRoot =
            "output/ui_route_audit/2026-08-06_role_attribute_potion/cli/2026-08-06_2000_final";
        private const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        public static async Task<int> Run()
        {
            CliVerify.Stage stage = CliVerify.Stage.Create();
            EventSystem eventSystem = null;
            AttributePotionController controller = AttributePotionController.Instance;
            bool wasControllerInitialized = controller.IsInitialized;
            int oldNewestTask = TaskModel.Instance.NewestFinishTaskId;
            var savedBag = new List<BagGoods>(BagModel.Instance.BagGoodsList);
            List<AttributePotionModel.Count> savedCounts = SnapshotCounts();
            FieldInfo useIntercept = typeof(AttributePotionController).GetField(
                "s_outboundIntercept", PrivateStatic);
            FieldInfo guideIntercept = typeof(RoleController).GetField(
                "s_potionGuideOutboundIntercept", PrivateStatic);
            object oldUseIntercept = useIntercept?.GetValue(null);
            object oldGuideIntercept = guideIntercept?.GetValue(null);
            var useFrames = new List<byte[]>();
            var guideFrames = new List<byte[]>();
            bool pass = false;
            string detail = string.Empty;
            string phase = "prepare";

            try
            {
                ResetFlows();
                await PrepareData();
                controller.Init();
                useIntercept?.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    useFrames.Add(frame);
                    return true;
                }));
                guideIntercept?.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    guideFrames.Add(frame);
                    return true;
                }));

                Canvas canvas = stage.CanvasRoot.GetComponent<Canvas>();
                GraphicRaycaster raycaster = stage.CanvasRoot.GetComponent<GraphicRaycaster>();
                eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
                if (eventSystem == null)
                {
                    eventSystem = new GameObject(
                        "RoleAttributePotionRouteEventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
                }
                Camera camera = canvas != null ? canvas.worldCamera : null;
                if (camera == null || raycaster == null || eventSystem == null)
                    throw new InvalidOperationException("药水路由缺相机/GraphicRaycaster/EventSystem");

                RoleFlow.Open();
                phase = "wait-equipment";
                EquipmentView equipment = await WaitForShown<EquipmentView>(12d);
                if (equipment == null)
                    throw new InvalidOperationException("EquipmentView did not open");
                bool roleRedReady = await WaitUntil(() => equipment != null
                    && equipment.attribute_red != null
                    && equipment.attribute_red.gameObject.activeInHierarchy, 5d);
                // ScreenSpaceCamera + RenderTexture 在 batchmode 首次 Render 前不会建立可用的
                // GraphicRaycaster 几何；角色技能路线也在第一次真实点击前先 Capture 一帧。
                stage.ForceCjkFont();
                stage.Capture(EvidenceRoot + "/role_potion_entry.png");

                string entryHit = "not-run";
                double coldStart = EditorApplication.timeSinceStartup;
                phase = "click-entry";
                bool entryClick = equipment != null
                    && Click(equipment._btn_attribute, camera, raycaster, eventSystem, out entryHit);
                if (!entryClick)
                    throw new InvalidOperationException(
                        "attribute potion entry raycast failed; hit=" + entryHit
                        + " buttonActive=" + (equipment._btn_attribute != null
                            && equipment._btn_attribute.gameObject.activeInHierarchy)
                        + " featureOpen=" + FuncOpenConfig.CheckFuncOpenState("attributePotionView")
                        + " task=" + TaskModel.Instance.NewestFinishTaskId);
                phase = "wait-potion-view";
                AttributePotionViewBind view = await WaitForShown<AttributePotionViewBind>(15d);
                if (view == null)
                    throw new InvalidOperationException(
                        "attribute potion view did not open after entry click; hit=" + entryHit
                        + " featureOpen=" + FuncOpenConfig.CheckFuncOpenState("attributePotionView")
                        + " task=" + TaskModel.Instance.NewestFinishTaskId);
                phase = "wait-potion-content";
                bool contentReady = await WaitUntil(() => ActiveItems(view).Length == 4
                    && ActiveTabs(view).Length == 4
                    && ActiveItems(view).All(item =>
                    {
                        BaseAwardItem award = item.GetComponentInChildren<BaseAwardItem>(false);
                        return award != null && award.icon != null && award.icon.sprite != null;
                    }), 12d);
                long coldMs = MillisecondsSince(coldStart);
                stage.ForceCjkFont();
                Canvas.ForceUpdateCanvases();

                AttributePotionTabBind[] tabs = ActiveTabs(view);
                AttributePotionItemBind[] items = ActiveItems(view);
                ScrollRect itemScroll = GetItemScroll(view);
                Image mask = FindActiveMask();
                bool identity = view != null
                    && view.transform.IsChildOf(ViewManager.GetLayer(UILayer.Popup));
                bool maskFull = mask != null && mask.raycastTarget
                    && mask.GetComponent<RootCanvasRectFitter>() != null
                    && Near(mask.rectTransform.anchorMin, Vector2.zero)
                    && Near(mask.rectTransform.anchorMax, Vector2.one)
                    && Near(mask.rectTransform.offsetMin, Vector2.zero)
                    && Near(mask.rectTransform.offsetMax, Vector2.zero);
                bool centered = view != null
                    && Near(((RectTransform)view.transform).rect.size, new Vector2(647f, 655f))
                    && Near(((RectTransform)view.transform).anchoredPosition, Vector2.zero);
                bool listStructure = itemScroll != null && itemScroll.enabled
                    && itemScroll.viewport != null
                    && itemScroll.viewport.GetComponent<RectMask2D>() != null
                    && itemScroll.GetComponent<Image>() != null
                    && itemScroll.GetComponent<Image>().raycastTarget
                    && itemScroll.content != null
                    && itemScroll.content.GetComponent<VerticalLayoutGroup>() != null
                    && itemScroll.content.GetComponent<ContentSizeFitter>() != null
                    && view.Content1 != null && view.Content1.viewport != null
                    && view.Content1.viewport.GetComponent<RectMask2D>() != null
                    && view.Content1.content.GetComponent<HorizontalLayoutGroup>() != null
                    && view.Content1.content.GetComponent<ContentSizeFitter>() != null;
                bool listScrollable = itemScroll != null
                    && itemScroll.content.rect.height > itemScroll.viewport.rect.height + 1f;
                bool tabsFullyVisible = tabs.Length == 4
                    && ((RectTransform)view.Content1.transform).rect.height >= 100f - 1f
                    && tabs.All(tab => IsInsideViewport(
                        (RectTransform)tab.transform, view.Content1.viewport));
                bool tabLabels = tabs.Length == 4
                    && tabs[0]._lb_name.text == "初级"
                    && tabs[1]._lb_name.text == "中级"
                    && tabs[2]._lb_name.text == "高级"
                    && tabs[3]._lb_name.text == "顶级";
                bool initialTier = ItemsMatch(items, ExpectedIds(1))
                    && IsSelected(tabs[0])
                    && tabs.All(tab => tab._red_dot != null && tab._red_dot.gameObject.activeInHierarchy)
                    && items.All(ItemShowsReadyZeroState);
                bool guideSlots = items.Length == 4
                    && items[0]._btn_use.GetComponentsInChildren<UIEffectSlot>(true)
                        .Count(slot => slot.SlotId == "main_ui_guide_select"
                            || slot.SlotId == "main_ui_guide_finger") == 2;
                bool guideVisible = await WaitUntil(() =>
                    UnityEngine.Object.FindObjectsByType<ArrowComponent>(
                            FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                        .Any(arrow => arrow != null && arrow.gameObject.activeInHierarchy), 6d);
                string initialShot = stage.Capture(EvidenceRoot + "/potion_tier1_guide.png");

                string dragHit = "not-run";
                bool drag = items.Length > 0 && Drag(itemScroll, items[0]._Image11,
                    new Vector2(0f, 180f), camera, raycaster, eventSystem, out dragHit);
                bool moved = await WaitUntil(() => itemScroll != null
                    && itemScroll.content.anchoredPosition.y >= 50f, 3d);
                itemScroll?.StopMovement();
                Canvas.ForceUpdateCanvases();
                bool lastReachable = items.Length == 4
                    && IsInsideViewport((RectTransform)items[3].transform, itemScroll.viewport);
                string dragShot = stage.Capture(EvidenceRoot + "/potion_tier1_bottom.png");
                itemScroll.verticalNormalizedPosition = 1f;

                bool allTabs = true;
                bool allDetails = true;
                bool allUseControls = true;
                var detailFailures = new List<string>();
                int detailCount = 0;
                int useCount = 0;
                var tierShots = new List<string>();
                for (byte tier = 1; tier <= 4; tier++)
                {
                    tabs = ActiveTabs(view);
                    string tabHit = "not-run";
                    bool tabClick = tabs.Length == 4
                        && Click(tabs[tier - 1]._Image1, camera, raycaster, eventSystem, out tabHit);
                    int[] expectedIds = ExpectedIds(tier);
                    bool tierReady = await WaitUntil(() =>
                    {
                        AttributePotionItemBind[] current = ActiveItems(view);
                        AttributePotionTabBind[] currentTabs = ActiveTabs(view);
                        return tabClick && currentTabs.Length == 4 && IsSelected(currentTabs[tier - 1])
                            && ItemsMatch(current, expectedIds);
                    }, 4d);
                    allTabs &= tabClick && tierReady;
                    items = ActiveItems(view);
                    tierShots.Add(stage.Capture(EvidenceRoot + "/potion_tier" + tier + ".png"));

                    for (int itemIndex = 0; itemIndex < items.Length; itemIndex++)
                    {
                        // 第四行超出 450 高 Viewport，先让真实 ScrollRect 把目标行带入裁剪区；
                        // 前三行保持在顶部，避免射线绕过 RectMask2D 直接命中被裁掉的控件。
                        itemScroll.StopMovement();
                        itemScroll.verticalNormalizedPosition = itemIndex == 3 ? 0f : 1f;
                        Canvas.ForceUpdateCanvases();
                        int expectedId = expectedIds[itemIndex];
                        string expectedName = GoodsModel.GetGoodsName(expectedId);
                        BaseAwardItem award = items[itemIndex].GetComponentInChildren<BaseAwardItem>(false);
                        string detailHit = "not-run";
                        bool detailClick = award != null
                            && Click(award.click_group, camera, raycaster, eventSystem, out detailHit);
                        GoodsTooltipsBind tips = await WaitForShown<GoodsTooltipsBind>(7d);
                        bool detailReady = tips != null
                            && tips.goods_name != null && tips.goods_name.text == expectedName
                            && tips.quantity_text != null && tips.quantity_text.text.Contains(">2<");
                        // BaseView 从隐藏切回显示后，GraphicRegistry 要到下一次 Editor update
                        // 才能被 GraphicRaycaster 命中；真实玩家也不可能在同一帧完成打开与关闭。
                        await Task.Delay(50);
                        Canvas.ForceUpdateCanvases();
                        if (tier == 1 && itemIndex == 0)
                            stage.Capture(EvidenceRoot + "/potion_item_detail_first.png");
                        string closeTipHit = "not-run";
                        bool tipClose = detailReady
                            && Click(tips.okBtn, camera, raycaster, eventSystem, out closeTipHit);
                        bool tipClosed = await WaitUntil(() => tips != null
                            && !tips.gameObject.activeInHierarchy, 3d);
                        bool detailPassed = detailClick && detailReady && tipClose && tipClosed;
                        allDetails &= detailPassed;
                        if (!detailPassed)
                        {
                            detailFailures.Add("t" + tier + "i" + itemIndex
                                + ":click=" + detailClick + "/" + detailHit
                                + ",ready=" + detailReady
                                + ",name=" + (tips?.goods_name?.text ?? "<null>")
                                + ",qty=" + (tips?.quantity_text?.text ?? "<null>")
                                + ",close=" + tipClose + "/" + closeTipHit + "/" + tipClosed);
                        }
                        detailCount++;

                        int beforeFrames = useFrames.Count;
                        string useHit = "not-run";
                        bool useClick = Click(items[itemIndex]._btn_use,
                            camera, raycaster, eventSystem, out useHit);
                        byte[] payload = new CliVerify.Pkt().I(expectedId).I(2).C(tier).Bytes();
                        bool frameReady = await WaitUntil(() => useFrames.Count == beforeFrames + 1, 2d)
                            && MatchesFrame(useFrames[useFrames.Count - 1],
                                Proto.ATTRIBUTE_POTION_USE, payload);
                        allUseControls &= useClick && frameReady;
                        useCount++;
                    }
                }

                bool guideExactOnce = guideFrames.Count == 1
                    && MatchesFrame(guideFrames[0], Proto.ROLE_LIFELONG_INCREMENT,
                        new CliVerify.Pkt().H(300).H(1).H(1).Bytes());

                // 21701 是权威刷新：先仅耗尽初级 04，验证重排/红点/进度；再耗尽整档，
                // 验证老端 TIME_DATA_RETURN 语义会自动跳到首个仍可用档位。
                ApplyTierSnapshot(1, onlyRankFourExhausted: true);
                bool oneExhaustedRefresh = await WaitUntil(() =>
                {
                    AttributePotionItemBind[] current = ActiveItems(view);
                    if (current.Length != 4) return false;
                    int[] expected = { 56010003, 56010002, 56010001, 56010004 };
                    if (!ItemsMatch(current, expected)) return false;
                    AttributePotionProgressBarBind progress =
                        current[3].GetComponentInChildren<AttributePotionProgressBarBind>(false);
                    return current[3]._red_dot != null && !current[3]._red_dot.gameObject.activeSelf
                        && progress != null && progress.labelDisplay.text.Contains("/")
                        && progress.labelDisplay.text.Split('/')[0]
                            == progress.labelDisplay.text.Split('/')[1];
                }, 4d);
                string authoritativeShot = stage.Capture(EvidenceRoot + "/potion_authoritative_refresh.png");

                ApplyTierSnapshot(1, onlyRankFourExhausted: false);
                bool tierAutoSwitch = await WaitUntil(() =>
                {
                    AttributePotionTabBind[] currentTabs = ActiveTabs(view);
                    return currentTabs.Length == 4
                        && !currentTabs[0]._red_dot.gameObject.activeSelf
                        && IsSelected(currentTabs[1])
                        && ItemsMatch(ActiveItems(view), ExpectedIds(2));
                }, 4d);

                BagModel.Instance.BagGoodsList.RemoveAll(goods => goods.TypeId == 56020004);
                EventDispatcher.Emit(GlobalEvent.EVT_BAG_UPDATE);
                bool bagRefresh = await WaitUntil(() =>
                {
                    AttributePotionItemBind[] current = ActiveItems(view);
                    int[] expected = { 56020003, 56020002, 56020001, 56020004 };
                    return ItemsMatch(current, expected)
                        && !current[3]._red_dot.gameObject.activeSelf;
                }, 4d);
                string bagRefreshShot = stage.Capture(EvidenceRoot + "/potion_bag_refresh.png");

                string closeHit = "not-run";
                bool closeClick = view != null
                    && Click(view._btn_close, camera, raycaster, eventSystem, out closeHit);
                bool closed = await WaitUntil(() => view != null && !view.gameObject.activeInHierarchy, 3d);
                bool roleStayed = equipment != null && equipment.IsShown
                    && equipment.gameObject.activeInHierarchy;

                double warmStart = EditorApplication.timeSinceStartup;
                string reopenHit = "not-run";
                bool reopenClick = equipment != null
                    && Click(equipment._btn_attribute, camera, raycaster, eventSystem, out reopenHit);
                AttributePotionViewBind reopened = await WaitForShown<AttributePotionViewBind>(7d);
                long warmMs = MillisecondsSince(warmStart);
                bool reopenReady = reopened != null
                    && ActiveTabs(reopened).Length == 4
                    && ActiveItems(reopened).Length == 4
                    && CountPotionModules() == 1
                    && CountActiveMasks() == 1;
                string reopenShot = stage.Capture(EvidenceRoot + "/potion_reopen.png");

                Image reopenedMask = FindActiveMask();
                string maskHit = "not-run";
                bool maskClick = reopenedMask != null
                    && ClickMaskCorner(reopenedMask, camera, raycaster, eventSystem, out maskHit);
                bool maskClosed = await WaitUntil(() => reopened != null
                    && !reopened.gameObject.activeInHierarchy, 3d);
                bool maskNoThrough = equipment != null && equipment.IsShown
                    && equipment.gameObject.activeInHierarchy;
                string maskClosedShot = stage.Capture(EvidenceRoot + "/potion_mask_closed.png");

                pass = roleRedReady && entryClick && contentReady && identity && maskFull && centered
                    && listStructure && listScrollable && tabsFullyVisible
                    && tabLabels && initialTier && guideSlots && guideVisible
                    && drag && moved && lastReachable
                    && allTabs && allDetails && detailCount == 16
                    && allUseControls && useCount == 16 && useFrames.Count == 16
                    && guideExactOnce && oneExhaustedRefresh && tierAutoSwitch && bagRefresh
                    && closeClick && closed && roleStayed
                    && reopenClick && reopenReady && maskClick && maskClosed && maskNoThrough;
                detail = "roleRed=" + roleRedReady
                    + " entry=" + entryClick + "/" + entryHit
                    + " content=" + contentReady + " identity=" + identity
                    + " mask=" + maskFull + " centered=" + centered
                    + " structure=" + listStructure + "/scrollable=" + listScrollable
                    + "/tabsVisible=" + tabsFullyVisible + " tabs=" + tabLabels + "/" + allTabs
                    + " initial=" + initialTier + " guide=" + guideSlots + "/" + guideVisible
                    + " drag=" + drag + "/" + dragHit + "/" + moved + "/" + lastReachable
                    + " details=" + detailCount + "/16/" + allDetails
                    + "/failures=" + string.Join(";", detailFailures)
                    + " uses=" + useCount + "/16/" + allUseControls + "/frames=" + useFrames.Count
                    + " guideFrame=" + guideExactOnce
                    + " refresh=" + oneExhaustedRefresh + "/" + tierAutoSwitch + "/" + bagRefresh
                    + " close=" + closeClick + "/" + closeHit + "/" + closed
                    + " roleStayed=" + roleStayed
                    + " reopen=" + reopenClick + "/" + reopenHit + "/" + reopenReady
                    + " maskClose=" + maskClick + "/" + maskHit + "/" + maskClosed
                    + " noThrough=" + maskNoThrough
                    + " coldMs=" + coldMs + " warmMs=" + warmMs
                    + " shots=" + initialShot + "," + dragShot + ","
                    + string.Join(",", tierShots) + "," + authoritativeShot + ","
                    + bagRefreshShot + "," + reopenShot + "," + maskClosedShot;
            }
            catch (Exception e)
            {
                detail = "phase=" + phase + " exception=" + e;
                pass = false;
            }
            finally
            {
                ItemTipsView.Close();
                ResetFlows();
                useIntercept?.SetValue(null, oldUseIntercept);
                guideIntercept?.SetValue(null, oldGuideIntercept);
                if (!wasControllerInitialized && controller.IsInitialized) controller.Dispose();
                AttributePotionModel.Instance.Clear();
                AttributePotionModel.Instance.MergeAll(savedCounts);
                BagModel.Instance.BagGoodsList.Clear();
                BagModel.Instance.BagGoodsList.AddRange(savedBag);
                TaskModel.Instance.SetNewestFinishTaskId(oldNewestTask);
                RoleModel.Instance.Reset();
                if (eventSystem != null) UnityEngine.Object.DestroyImmediate(eventSystem.gameObject);
                stage.Dispose();
            }

            Debug.Log("CLIVERIFY roleattributepotionroute " + detail);
            Debug.Log("CLIVERIFY roleattributepotionroute VERDICT pass=" + pass + " restored=True");
            return pass ? 0 : 3;
        }

        private static async Task PrepareData()
        {
            await Task.WhenAll(
                AttributePotionConfigs.EnsureLoaded(),
                GoodsModel.EnsureLoaded(),
                FuncOpenConfig.EnsureLoaded());

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
            role.SetLifelongCount(300, 1, 1, 0);
            TaskModel.Instance.SetNewestFinishTaskId(101211);

            BagModel.Instance.BagGoodsList.Clear();
            var counts = new List<AttributePotionModel.Count>(16);
            long instanceId = 92000000L;
            for (byte tier = 1; tier <= 4; tier++)
            {
                IReadOnlyList<AttributePotionConfigs.Potion> rows = AttributePotionConfigs.GetPotions(tier);
                for (int i = 0; i < rows.Count; i++)
                {
                    BagModel.Instance.BagGoodsList.Add(new BagGoods
                    {
                        GoodsId = instanceId++,
                        TypeId = rows[i].GoodsId,
                        GoodsNum = 2,
                    });
                    counts.Add(new AttributePotionModel.Count
                    {
                        GoodsId = rows[i].GoodsId,
                        Level = tier,
                        CurrentDayCount = 0,
                        CurrentCount = 0,
                    });
                }
            }
            AttributePotionModel.Instance.Clear();
            AttributePotionModel.Instance.MergeAll(counts);
        }

        private static void ApplyTierSnapshot(byte tier, bool onlyRankFourExhausted)
        {
            MethodInfo on21701 = typeof(AttributePotionController).GetMethod("On21701", PrivateInstance);
            if (on21701 == null) throw new MissingMethodException("AttributePotionController.On21701");
            int[] ids = ExpectedIds(tier);
            var packet = new CliVerify.Pkt().H(ids.Length);
            for (int i = 0; i < ids.Length; i++)
            {
                bool exhaust = !onlyRankFourExhausted || ids[i] % 10 == 4;
                if (!AttributePotionConfigs.TryGetLimit(
                        ids[i], RoleModel.Instance.Level, out AttributePotionConfigs.Limit limit))
                    throw new InvalidOperationException("药水限制配置缺失 id=" + ids[i]);
                packet.I(ids[i]).C(tier)
                    .I(exhaust ? (int)limit.DayTimes : 0)
                    .L(exhaust ? (long)limit.AllTimes : 0L);
            }
            byte[] bytes = packet.Bytes();
            on21701.Invoke(AttributePotionController.Instance, new object[]
            {
                new NetReader(bytes, 0, bytes.Length),
            });
        }

        private static bool ItemShowsReadyZeroState(AttributePotionItemBind item)
        {
            if (item == null || string.IsNullOrEmpty(item._lb_name.text)
                || item._red_dot == null || !item._red_dot.gameObject.activeInHierarchy)
                return false;
            AttributePotionProgressBarBind progress =
                item.GetComponentInChildren<AttributePotionProgressBarBind>(false);
            return progress != null && progress.labelDisplay != null
                && progress.labelDisplay.text.StartsWith("0/")
                && item._lb_attr != null && item._lb_attr.text.Contains("+ 0");
        }

        private static int[] ExpectedIds(byte tier)
            => new[]
            {
                56000000 + tier * 10000 + 4,
                56000000 + tier * 10000 + 3,
                56000000 + tier * 10000 + 2,
                56000000 + tier * 10000 + 1,
            };

        private static bool ItemsMatch(AttributePotionItemBind[] items, IReadOnlyList<int> ids)
        {
            if (items == null || ids == null || items.Length != ids.Count) return false;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null || items[i]._lb_name == null
                    || items[i]._lb_name.text != GoodsModel.GetGoodsName(ids[i])) return false;
            }
            return true;
        }

        private static bool IsSelected(AttributePotionTabBind tab)
            => tab != null && tab._Image1 != null && tab._Image1.sprite != null
                && tab._Image1.sprite.name.IndexOf("uitc_006", StringComparison.OrdinalIgnoreCase) >= 0;

        private static AttributePotionTabBind[] ActiveTabs(AttributePotionViewBind view)
            => view == null || view.Content1 == null || view.Content1.content == null
                ? Array.Empty<AttributePotionTabBind>()
                : view.Content1.content.GetComponentsInChildren<AttributePotionTabBind>(false)
                    .Where(tab => tab != null && tab.gameObject.name.StartsWith(
                        "attributePotionTab_Runtime_", StringComparison.Ordinal))
                    .OrderBy(tab => tab.transform.GetSiblingIndex()).ToArray();

        private static AttributePotionItemBind[] ActiveItems(AttributePotionViewBind view)
            => GetItemScroll(view) == null || GetItemScroll(view).content == null
                ? Array.Empty<AttributePotionItemBind>()
                : GetItemScroll(view).content.GetComponentsInChildren<AttributePotionItemBind>(false)
                    .Where(item => item != null && item.gameObject.name.StartsWith(
                        "attributePotionItem_Runtime_", StringComparison.Ordinal))
                    .OrderBy(item => item.transform.GetSiblingIndex()).ToArray();

        private static ScrollRect GetItemScroll(AttributePotionViewBind view)
        {
            ScrollRect convertedList = view != null && view.Content != null
                ? view.Content.GetComponentInChildren<ScrollRect>(true)
                : null;
            return convertedList != null ? convertedList : view?._Scroller1;
        }

        private static Image FindActiveMask()
            => UnityEngine.Object.FindObjectsByType<Image>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(image => image != null && image.gameObject.name == "__ModalDim"
                    && HasAncestorNamed(image.transform, "AttributePotionModule(Runtime)"));

        private static int CountActiveMasks()
            => UnityEngine.Object.FindObjectsByType<Image>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Count(image => image != null && image.gameObject.name == "__ModalDim"
                    && HasAncestorNamed(image.transform, "AttributePotionModule(Runtime)"));

        private static int CountPotionModules()
            => UnityEngine.Object.FindObjectsByType<AttributePotionViewBind>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Count(view => view != null
                    && HasAncestorNamed(view.transform, "AttributePotionModule(Runtime)"));

        private static bool HasAncestorNamed(Transform transform, string name)
        {
            for (Transform current = transform; current != null; current = current.parent)
                if (current.name == name) return true;
            return false;
        }

        private static List<AttributePotionModel.Count> SnapshotCounts()
        {
            var result = new List<AttributePotionModel.Count>();
            FieldInfo field = typeof(AttributePotionModel).GetField("_byLevel", PrivateInstance);
            var levels = field?.GetValue(AttributePotionModel.Instance)
                as Dictionary<int, Dictionary<int, AttributePotionModel.Count>>;
            if (levels == null) return result;
            foreach (Dictionary<int, AttributePotionModel.Count> rows in levels.Values)
            {
                foreach (AttributePotionModel.Count row in rows.Values)
                {
                    result.Add(new AttributePotionModel.Count
                    {
                        GoodsId = row.GoodsId,
                        Level = row.Level,
                        CurrentDayCount = row.CurrentDayCount,
                        CurrentCount = row.CurrentCount,
                    });
                }
            }
            return result;
        }

        private static void ResetFlows()
        {
            typeof(AttributePotionFlow).GetMethod("Reset", PrivateStatic)?.Invoke(null, null);
            typeof(RoleFlow).GetMethod("Reset", PrivateStatic)?.Invoke(null, null);
        }

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

        private static bool Click(
            Component target, Camera camera, GraphicRaycaster raycaster,
            EventSystem eventSystem, out string hitName)
        {
            hitName = "missing";
            RectTransform rect = target != null ? target.transform as RectTransform : null;
            if (rect == null || !rect.gameObject.activeInHierarchy) return false;
            Canvas.ForceUpdateCanvases();
            Vector2 point = RectTransformUtility.WorldToScreenPoint(
                camera, rect.TransformPoint(rect.rect.center));
            return ClickAt(point, rect, raycaster, eventSystem, out hitName);
        }

        private static bool ClickMaskCorner(
            Image mask, Camera camera, GraphicRaycaster raycaster,
            EventSystem eventSystem, out string hitName)
        {
            RectTransform rect = mask != null ? mask.rectTransform : null;
            if (rect == null)
            {
                hitName = "missing";
                return false;
            }
            Vector3 world = rect.TransformPoint(new Vector3(
                rect.rect.xMin + 12f, rect.rect.yMin + 12f));
            return ClickAt(RectTransformUtility.WorldToScreenPoint(camera, world),
                rect, raycaster, eventSystem, out hitName);
        }

        private static bool ClickAt(
            Vector2 point, RectTransform scope, GraphicRaycaster raycaster,
            EventSystem eventSystem, out string hitName)
        {
            var pointer = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = point,
            };
            var hits = new List<RaycastResult>();
            raycaster.Raycast(pointer, hits);
            foreach (RaycastResult hitResult in hits)
            {
                Transform hit = hitResult.gameObject.transform;
                if (hit != scope && !hit.IsChildOf(scope)) continue;
                hitName = hitResult.gameObject.name;
                ExecuteEvents.ExecuteHierarchy<IPointerClickHandler>(
                    hitResult.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                return true;
            }
            hitName = string.Join("/", hits.Select(hit => hit.gameObject.name));
            return false;
        }

        private static bool Drag(
            ScrollRect scroll, Component surface, Vector2 delta, Camera camera,
            GraphicRaycaster raycaster, EventSystem eventSystem, out string hitName)
        {
            hitName = "missing";
            RectTransform scrollRect = scroll != null ? scroll.transform as RectTransform : null;
            RectTransform surfaceRect = surface != null ? surface.transform as RectTransform : null;
            if (scrollRect == null || surfaceRect == null) return false;
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
            RaycastResult hit = hits.FirstOrDefault(result =>
                result.gameObject.transform == scrollRect
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

        private static bool IsInsideViewport(RectTransform item, RectTransform viewport)
        {
            if (item == null || viewport == null || !item.gameObject.activeInHierarchy) return false;
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, item);
            Rect rect = viewport.rect;
            return bounds.min.y >= rect.yMin - 1f && bounds.max.y <= rect.yMax + 1f;
        }

        private static bool MatchesFrame(byte[] frame, int protocolId, byte[] payload)
        {
            int length = 6 + payload.Length;
            if (frame == null || frame.Length != length
                || frame[0] != (byte)(length >> 8) || frame[1] != (byte)(length & 0xFF)
                || frame[2] != 3 || frame[3] != 232
                || frame[4] != (byte)(protocolId >> 8) || frame[5] != (byte)(protocolId & 0xFF))
                return false;
            for (int i = 0; i < payload.Length; i++)
                if (frame[i + 6] != payload[i]) return false;
            return true;
        }

        private static long MillisecondsSince(double start)
            => Math.Max(0L, (long)Math.Round(
                (EditorApplication.timeSinceStartup - start) * 1000d));

        private static bool Near(Vector2 value, Vector2 expected)
            => Vector2.Distance(value, expected) <= 1f;
    }
}
