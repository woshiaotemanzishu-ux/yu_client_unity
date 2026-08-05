using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Common.Proto;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Alert;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Dungeon;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Skill;
using Shenxiao.Module.Core.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 人物→技能→主动/被动/天赋→返回→关闭重开的真实 Prefab 路由。
    /// 禁止用 SelectTab/私有方法代替玩家点击；所有切换都必须来自 GraphicRaycaster 命中。
    /// </summary>
    public static class RoleSkillRouteCase
    {
        private const string EvidenceRoot =
            "output/ui_route_audit/2026-08-04_role_web_round2/cli_role_skill_route_20260805_2614";

        public static async Task<int> Run()
        {
            CliVerify.Stage stage = CliVerify.Stage.Create();
            EventSystem eventSystem = null;
            bool pass = false;
            string detail = string.Empty;
            try
            {
                ResetRoleFlow();
                await PrepareData();

                Canvas canvas = stage.CanvasRoot.GetComponent<Canvas>();
                GraphicRaycaster raycaster = stage.CanvasRoot.GetComponent<GraphicRaycaster>();
                eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
                if (eventSystem == null)
                    eventSystem = new GameObject("RoleSkillRouteEventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
                Camera camera = canvas != null ? canvas.worldCamera : null;
                if (camera == null || raycaster == null || eventSystem == null)
                    throw new InvalidOperationException("角色技能路由缺相机/GraphicRaycaster/EventSystem");

                double coldStart = EditorApplication.timeSinceStartup;
                RoleFlow.Open();
                EquipmentView equipment = await WaitForShown<EquipmentView>(12d);
                int coldModelPixels = await WaitForModelPixels(
                    equipment,
                    EvidenceRoot + "/role_model_cold_rt.png",
                    12d);
                long coldMs = MillisecondsSince(coldStart);
                BaseWindowSkinView roleWindow = FindShownWindow("BaseWindowSkin");
                bool coldReady = equipment != null && roleWindow != null && coldModelPixels >= 64;
                stage.ForceCjkFont();
                string roleShot = stage.Capture(EvidenceRoot + "/role_person.png");

                string entryHit = "not-clicked";
                bool entryClick = equipment != null
                    && Click(equipment._Group1, camera, raycaster, eventSystem, out entryHit);
                BaseWindowSkinView skillWindow = await WaitForWindow("RoleSkillWindow", 10d);
                SkillInitiativeSubItem active = await WaitForShown<SkillInitiativeSubItem>(6d);
                bool activeReady = skillWindow != null && skillWindow.CurrentIndex == 0 && active != null;
                bool forbiddenEntryHidden = active != null && active.skill_wear_btn != null
                    && !active.skill_wear_btn.gameObject.activeSelf
                    && !active.skill_wear_btn.raycastTarget;
                stage.ForceCjkFont();
                string activeShot = stage.Capture(EvidenceRoot + "/skill_active.png");

                TabButtonTwoSkin[] tabs = skillWindow != null
                    ? skillWindow.GetComponentsInChildren<TabButtonTwoSkin>(false)
                        .OrderBy(t => ((RectTransform)t.transform).anchoredPosition.x)
                        .ToArray()
                    : Array.Empty<TabButtonTwoSkin>();
                string activeTabHit = "not-clicked";
                bool activeTabClick = tabs.Length == 3
                    && Click(tabs[0]._Image1, camera, raycaster, eventSystem, out activeTabHit);
                bool activeTabReady = await WaitUntil(() =>
                    skillWindow != null && skillWindow.CurrentIndex == 0
                    && active != null && active.IsShown, 3d);

                // 重复点击已选页签会在本帧重建六个格子；等射线注册和布局表完成后再做首个条目点击。
                await Task.Delay(100);
                Canvas.ForceUpdateCanvases();

                SkillInitiativeItem[] activeItems = active != null
                    ? active.GetComponentsInChildren<SkillInitiativeItem>(false)
                        .Where(item => item != null && item.gameObject.activeInHierarchy)
                        .OrderBy(item => item.transform.GetSiblingIndex())
                        .ToArray()
                    : Array.Empty<SkillInitiativeItem>();
                string activeNameBefore = active != null && active._lb_name != null
                    ? active._lb_name.text
                    : string.Empty;
                string activeDetailHit = "not-clicked";
                bool activeDetailClick = activeItems.Length >= 2
                    && Click(activeItems[1]._img_icon, camera, raycaster, eventSystem, out activeDetailHit);
                bool activeDetailReady = await WaitUntil(() =>
                    activeDetailClick && active != null && active._lb_name != null
                    && !string.IsNullOrEmpty(active._lb_name.text)
                    && active._lb_name.text != activeNameBefore, 3d);
                stage.ForceCjkFont();
                string activeDetailShot = stage.Capture(EvidenceRoot + "/skill_active_detail.png");

                List<SkillUIConfigs.CareerSkill> activeConfigured =
                    SkillUIConfigs.GetCareerSkills(RoleModel.Instance.Career);
                var activeSlotShots = new List<string>();
                bool activeSlotsReady = activeItems.Length == 6
                    && activeConfigured.Count >= activeItems.Length;
                for (int i = 0; activeSlotsReady && i < activeItems.Length; i++)
                {
                    string expectedName = new SkillVo(activeConfigured[i].SkillId).GetName();
                    string hit;
                    bool clicked = Click(activeItems[i]._img_icon, camera, raycaster, eventSystem, out hit);
                    bool selected = await WaitUntil(() => clicked && active != null
                        && active._lb_name != null && active._lb_name.text == expectedName, 3d);
                    activeSlotsReady &= clicked && selected;
                    stage.ForceCjkFont();
                    activeSlotShots.Add(stage.Capture(
                        EvidenceRoot + "/skill_active_slot_" + (i + 1) + ".png"));
                }

                string passiveHit = "not-clicked";
                bool passiveClick = tabs.Length == 3
                    && Click(tabs[1]._Image1, camera, raycaster, eventSystem, out passiveHit);
                SkillPassiveSubItem passive = await WaitForShown<SkillPassiveSubItem>(6d);
                bool passiveReady = skillWindow != null && skillWindow.CurrentIndex == 1 && passive != null;
                stage.ForceCjkFont();
                if (passive != null && passive._lb_desc3 != null)
                    passive._lb_desc3.ForceMeshUpdate();
                bool passiveDescSingleLine = passive != null && passive._lb_desc3 != null
                    && passive._lb_desc3.textInfo.lineCount == 1;
                string passiveShot = stage.Capture(EvidenceRoot + "/skill_passive.png");

                SkillPassiveItem[] passiveItems = passive != null
                    ? passive.GetComponentsInChildren<SkillPassiveItem>(false)
                        .Where(item => item != null && item.gameObject.activeInHierarchy)
                        .OrderBy(item => item.transform.GetSiblingIndex())
                        .ToArray()
                    : Array.Empty<SkillPassiveItem>();
                ScrollRect passiveScroll = passive != null ? passive._Scroller1 : null;
                RectTransform passiveViewport = passiveScroll != null ? passiveScroll.viewport : null;
                RectTransform passiveContent = passiveScroll != null ? passiveScroll.content : null;
                bool passiveListStructure = passiveScroll != null && passiveViewport != null
                    && passiveContent != null && passiveViewport.GetComponent<RectMask2D>() != null
                    && passiveContent.GetComponent<LayoutGroup>() != null
                    && passiveContent.GetComponent<ContentSizeFitter>() != null;
                bool passiveLastReachable = passiveItems.Length > 0 && passiveViewport != null
                    && IsInsideViewport(passiveItems[passiveItems.Length - 1].transform as RectTransform,
                        passiveViewport);
                string passiveNameBefore = passive != null && passive._lb_name != null
                    ? passive._lb_name.text
                    : string.Empty;
                string passiveDetailHit = "not-clicked";
                bool passiveDetailClick = passiveItems.Length >= 2
                    && Click(passiveItems[1]._img_2, camera, raycaster, eventSystem, out passiveDetailHit);
                bool passiveDetailReady = await WaitUntil(() =>
                    passiveDetailClick && passive != null && passive._lb_name != null
                    && !string.IsNullOrEmpty(passive._lb_name.text)
                    && passive._lb_name.text != passiveNameBefore, 3d);
                stage.ForceCjkFont();
                string passiveDetailShot = stage.Capture(EvidenceRoot + "/skill_passive_detail.png");

                var passiveSlotShots = new List<string>();
                bool passiveSlotsReady = passiveItems.Length == 6;
                for (int i = 0; passiveSlotsReady && i < passiveItems.Length; i++)
                {
                    string expectedName = passiveItems[i]._lb_name != null
                        ? passiveItems[i]._lb_name.text
                        : string.Empty;
                    string hit;
                    bool clicked = Click(passiveItems[i]._img_2, camera, raycaster, eventSystem, out hit);
                    bool selected = await WaitUntil(() => clicked && passive != null
                        && passive._lb_name != null && !string.IsNullOrEmpty(expectedName)
                        && passive._lb_name.text == expectedName, 3d);
                    passiveSlotsReady &= clicked && selected;
                    stage.ForceCjkFont();
                    passiveSlotShots.Add(stage.Capture(
                        EvidenceRoot + "/skill_passive_slot_" + (i + 1) + ".png"));
                }

                // 四转角色必须能经真实页签点击进入天赋，不能直接调用 SelectTab(2)。
                tabs = skillWindow != null
                    ? skillWindow.GetComponentsInChildren<TabButtonTwoSkin>(false)
                        .OrderBy(t => ((RectTransform)t.transform).anchoredPosition.x)
                        .ToArray()
                    : Array.Empty<TabButtonTwoSkin>();
                string talentHit = "not-clicked";
                bool talentClick = tabs.Length == 3
                    && Click(tabs[2]._Image1, camera, raycaster, eventSystem, out talentHit);
                InnateSkillView talent = await WaitForShown<InnateSkillView>(6d);
                bool talentReady = skillWindow != null && skillWindow.CurrentIndex == 2 && talent != null;
                stage.ForceCjkFont();
                string talentShot = stage.Capture(EvidenceRoot + "/skill_talent.png");

                InnateInfoItem talentInfo = talent != null
                    ? talent.GetComponentInChildren<InnateInfoItem>(false)
                    : null;
                RectTransform talentDetailContent = talentInfo != null ? talentInfo.DecContainer : null;
                RectTransform talentDetailViewport = talentDetailContent != null
                    ? talentDetailContent.parent as RectTransform
                    : null;
                ScrollRect talentDetailScroll = talentDetailViewport != null
                    ? talentDetailViewport.GetComponent<ScrollRect>()
                    : null;
                bool talentDetailScrollStructure = talentDetailScroll != null
                    && talentDetailScroll.content == talentDetailContent
                    && talentDetailScroll.viewport == talentDetailViewport
                    && talentDetailScroll.vertical && !talentDetailScroll.horizontal
                    && talentDetailViewport.GetComponent<RectMask2D>() != null
                    && talentDetailContent.GetComponent<VerticalLayoutGroup>() != null
                    && talentDetailContent.GetComponent<ContentSizeFitter>() != null;
                InnateSkillItem[] talentItems = talent != null
                    ? talent.GetComponentsInChildren<InnateSkillItem>(false)
                        .Where(item => item != null && item.SkillId > 0 && item.gameObject.activeInHierarchy)
                        .OrderBy(item => item.transform.GetSiblingIndex())
                        .ToArray()
                    : Array.Empty<InnateSkillItem>();
                bool talentGrayReady = talentItems.Length == 10 && talentItems.All(item =>
                {
                    bool shouldGray = SkillTalentModel.Instance.GetTalentLevel(item.SkillId) <= 0;
                    return (item._img_icon != null && (item._img_icon.material == UIGrayStyle.Material) == shouldGray)
                        && (item._Image1 != null && (item._Image1.material == UIGrayStyle.Material) == shouldGray)
                        && (item._Image2 != null && (item._Image2.material == UIGrayStyle.Material) == shouldGray);
                });
                InnateUpInfoItem talentUp = talent != null
                    ? talent.GetComponentInChildren<InnateUpInfoItem>(false)
                    : null;
                GoodsModel.GoodsBasic talentPointGood = GoodsModel.GetGoodsBasicByTypeId(6200001);
                bool talentCostIconReady = await WaitUntil(() => talentUp != null
                    && talentUp._img_icon != null && talentUp._img_icon.sprite != null
                    && talentPointGood != null
                    && string.Equals(talentUp._img_icon.sprite.name, talentPointGood.Icon,
                        StringComparison.OrdinalIgnoreCase), 3d);
                string talentNameBefore = talentInfo != null && talentInfo.NameLabel != null
                    ? talentInfo.NameLabel.text
                    : string.Empty;
                string talentDetailHit = "not-clicked";
                bool talentDetailClick = talentItems.Length >= 2
                    && Click(talentItems[1]._group, camera, raycaster, eventSystem, out talentDetailHit);
                bool talentDetailReady = await WaitUntil(() =>
                    talentDetailClick && talentInfo != null && talentInfo.NameLabel != null
                    && !string.IsNullOrEmpty(talentInfo.NameLabel.text)
                    && talentInfo.NameLabel.text != talentNameBefore, 3d);
                bool talentZeroPreviewReady = talentDetailReady && talentItems.Length >= 2
                    && SkillTalentModel.Instance.GetTalentLevel(talentItems[1].SkillId) <= 0
                    && talentInfo.DecContainer != null
                    && talentInfo.DecContainer.GetComponentsInChildren<TextMeshProUGUI>(false)
                        .Any(label => label != null && label.text == "[满级效果]");
                stage.ForceCjkFont();
                string talentDetailShot = stage.Capture(EvidenceRoot + "/skill_talent_detail.png");

                var talentSlotShots = new List<string>();
                bool talentSlotsReady = talentItems.Length == 10;
                for (int i = 0; talentSlotsReady && i < talentItems.Length; i++)
                {
                    string expectedName = SkillConfigs.GetName(talentItems[i].SkillId);
                    string hit;
                    bool clicked = Click(talentItems[i]._group, camera, raycaster, eventSystem, out hit);
                    bool selected = await WaitUntil(() => clicked && talentInfo != null
                        && talentInfo.NameLabel != null && !string.IsNullOrEmpty(expectedName)
                        && talentInfo.NameLabel.text == expectedName, 3d);
                    talentSlotsReady &= clicked && selected;
                    stage.ForceCjkFont();
                    talentSlotShots.Add(stage.Capture(
                        EvidenceRoot + "/skill_talent_slot_" + (i + 1) + ".png"));
                }

                InnateTypeItemRenderer[] talentTypeTabs = talent != null
                    ? talent.GetComponentsInChildren<InnateTypeItemRenderer>(false)
                        .Where(tab => tab != null && tab.SkillType > 0 && tab.gameObject.activeInHierarchy)
                        .OrderBy(tab => ((RectTransform)tab.transform).anchoredPosition.x)
                        .ToArray()
                    : Array.Empty<InnateTypeItemRenderer>();
                bool talentTypeTabsReady = talentTypeTabs.Length == 4;
                var talentTypeShots = new List<string>();
                var talentTypeSlotShots = new List<string>();
                var talentTreeTrace = new List<string>();
                bool talentTreeScrollReady = true;
                for (int i = 0; talentTypeTabsReady && i < talentTypeTabs.Length; i++)
                {
                    int clickedType = talentTypeTabs[i].SkillType;
                    List<SkillUIConfigs.InnateSlot> expectedSlots =
                        SkillUIConfigs.GetInnateSlots(clickedType, RoleModel.Instance.Career);
                    int expectedFirstSkill = expectedSlots.Count > 0 ? expectedSlots[0].SkillId : 0;
                    string expectedName = SkillConfigs.GetName(expectedFirstSkill);
                    string hit;
                    bool clicked = Click(talentTypeTabs[i]._img_skill_icon,
                        camera, raycaster, eventSystem, out hit);
                    bool selected = await WaitUntil(() => clicked && talentInfo != null
                        && talentInfo.NameLabel != null && !string.IsNullOrEmpty(expectedName)
                        && talentInfo.NameLabel.text == expectedName
                        && talent.GetComponentsInChildren<InnateSkillItem>(false)
                            .Any(item => item != null && item.gameObject.activeInHierarchy
                                && item.SkillId == expectedFirstSkill), 3d);
                    talentTypeTabsReady &= clicked && selected;
                    stage.ForceCjkFont();
                    talentTypeShots.Add(stage.Capture(
                        EvidenceRoot + "/skill_talent_type_" + clickedType + ".png"));

                    InnateSkillItem[] typeItems = talent.GetComponentsInChildren<InnateSkillItem>(false)
                        .Where(item => item != null && item.SkillId > 0 && item.gameObject.activeInHierarchy)
                        .OrderByDescending(item => ((RectTransform)item.transform).anchoredPosition.y)
                        .ThenBy(item => ((RectTransform)item.transform).anchoredPosition.x)
                        .ToArray();
                    talentTypeTabsReady &= typeItems.Length == expectedSlots.Count;
                    bool draggedThisType = false;
                    for (int slot = 0; talentTypeTabsReady && slot < typeItems.Length; slot++)
                    {
                        if (clickedType == 7
                            && ((RectTransform)typeItems[slot].transform).anchoredPosition.y <= -600f
                            && !IsInsideViewport(typeItems[slot].transform as RectTransform,
                                talent._Scroller1 != null ? talent._Scroller1.viewport : null))
                        {
                            Vector2 before = talent._Scroller1 != null && talent._Scroller1.content != null
                                ? talent._Scroller1.content.anchoredPosition
                                : Vector2.zero;
                            Vector2 dragDelta = new Vector2(0f, 260f);
                            string dragHit;
                            bool dragged = Drag(talent._Scroller1, typeItems[0]._group, dragDelta,
                                camera, raycaster, eventSystem, out dragHit);
                            Canvas.ForceUpdateCanvases();
                            Vector2 after = talent._Scroller1 != null && talent._Scroller1.content != null
                                ? talent._Scroller1.content.anchoredPosition
                                : before;
                            if (Vector2.Distance(before, after) <= 1f)
                            {
                                dragDelta = -dragDelta;
                                dragged |= Drag(talent._Scroller1, typeItems[0]._group, dragDelta,
                                    camera, raycaster, eventSystem, out string reverseHit);
                                dragHit += "/reverse:" + reverseHit;
                                Canvas.ForceUpdateCanvases();
                                after = talent._Scroller1 != null && talent._Scroller1.content != null
                                    ? talent._Scroller1.content.anchoredPosition
                                    : before;
                            }
                            if (!IsInsideViewport(typeItems[slot].transform as RectTransform,
                                    talent._Scroller1 != null ? talent._Scroller1.viewport : null))
                            {
                                dragged |= Drag(talent._Scroller1, typeItems[0]._group, dragDelta,
                                    camera, raycaster, eventSystem, out string repeatHit);
                                dragHit += "/repeat:" + repeatHit;
                                Canvas.ForceUpdateCanvases();
                                after = talent._Scroller1 != null && talent._Scroller1.content != null
                                    ? talent._Scroller1.content.anchoredPosition
                                    : before;
                            }
                            talentTreeScrollReady &= dragged && Vector2.Distance(before, after) > 1f
                                && IsInsideViewport(typeItems[slot].transform as RectTransform,
                                    talent._Scroller1.viewport);
                            talentTreeTrace.Add(clickedType + ":" + slot + " hit=" + dragHit
                                + " dragged=" + dragged + " pos=" + before + "->" + after
                                + " contentH=" + talent._Scroller1.content.rect.height
                                + " viewH=" + talent._Scroller1.viewport.rect.height
                                + " inside=" + IsInsideViewport(typeItems[slot].transform as RectTransform,
                                    talent._Scroller1.viewport));
                            draggedThisType |= dragged;
                            stage.ForceCjkFont();
                            talentTypeShots.Add(stage.Capture(
                                EvidenceRoot + "/skill_talent_type_" + clickedType + "_scrolled.png"));
                        }
                        string slotExpectedName = SkillConfigs.GetName(typeItems[slot].SkillId);
                        string slotHit;
                        bool slotClicked = ClickWithin(typeItems[slot]._group, typeItems[slot]._img_icon,
                            camera, raycaster, eventSystem, out slotHit);
                        bool slotSelected = await WaitUntil(() => slotClicked && talentInfo != null
                            && talentInfo.NameLabel != null && !string.IsNullOrEmpty(slotExpectedName)
                            && talentInfo.NameLabel.text == slotExpectedName, 3d);
                        talentTypeTabsReady &= slotClicked && slotSelected;
                        stage.ForceCjkFont();
                        talentTypeSlotShots.Add(stage.Capture(EvidenceRoot + "/skill_talent_type_"
                            + clickedType + "_slot_" + (slot + 1) + ".png"));
                    }
                    if (clickedType == 7) talentTreeScrollReady &= draggedThisType;
                }

                ApplyTalentInfo(0);
                int blockedSkillId = SkillUIConfigs.GetInnateSlots(8, RoleModel.Instance.Career)[0].SkillId;
                int blockedSkillLevel = SkillTalentModel.Instance.GetTalentLevel(blockedSkillId);
                bool blockedByModel = !SkillTalentModel.Instance.CanLearn(blockedSkillId, out string blockedReason)
                    && !string.IsNullOrEmpty(blockedReason);
                string upgradeHit = "not-clicked";
                bool upgradeClick = talentUp != null && Click(talentUp._btn_up,
                    camera, raycaster, eventSystem, out upgradeHit);
                await Task.Delay(150);
                bool talentUpgradeBlocked = blockedByModel && upgradeClick
                    && SkillTalentModel.Instance.LessPoint == 0
                    && SkillTalentModel.Instance.GetTalentLevel(blockedSkillId) == blockedSkillLevel;
                stage.ForceCjkFont();
                string upgradeBlockedShot = stage.Capture(
                    EvidenceRoot + "/skill_talent_upgrade_blocked.png");

                string resetHit = "not-clicked";
                bool resetClick = talent != null && Click(talent._Image3,
                    camera, raycaster, eventSystem, out resetHit);
                AlertTypeTwoBind resetDialog = await WaitForShown<AlertTypeTwoBind>(6d);
                bool resetDialogReady = resetClick && resetDialog != null
                    && resetDialog._content_html != null
                    && resetDialog._content_html.text.Contains("重置");
                stage.ForceCjkFont();
                string resetDialogShot = stage.Capture(
                    EvidenceRoot + "/skill_talent_reset_dialog.png");
                string resetCancelHit = "not-clicked";
                bool resetCancelClick = resetDialogReady && Click(resetDialog._cancel_btn,
                    camera, raycaster, eventSystem, out resetCancelHit);
                bool resetCancelled = await WaitUntil(() => resetDialog != null && !resetDialog.IsShown
                    && talent != null && talent.IsShown, 3d);
                stage.ForceCjkFont();
                string resetCancelledShot = stage.Capture(
                    EvidenceRoot + "/skill_talent_reset_cancelled.png");

                string returnHit = "not-clicked";
                bool returnClick = skillWindow != null
                    && Click(skillWindow._img_return0, camera, raycaster, eventSystem, out returnHit);
                bool returned = await WaitUntil(() =>
                    skillWindow != null && !skillWindow.IsShown
                    && equipment != null && equipment.IsShown
                    && roleWindow != null && roleWindow.IsShown, 6d);

                // 返回动作会在同帧重新 Configure 人物窗；等一帧布局与射线表稳定后再点关闭。
                await Task.Delay(250);
                Canvas.ForceUpdateCanvases();
                stage.ForceCjkFont();
                string returnedShot = stage.Capture(EvidenceRoot + "/role_person_returned.png");

                string closeHit = "not-clicked";
                bool closeClick = roleWindow != null
                    && Click(roleWindow._img_return0, camera, raycaster, eventSystem, out closeHit);
                bool closed = await WaitUntil(() => roleWindow != null && !roleWindow.IsShown, 4d);

                double warmStart = EditorApplication.timeSinceStartup;
                RoleFlow.Open();
                bool warmShown = await WaitUntil(() => equipment != null && equipment.IsShown
                    && roleWindow != null && roleWindow.IsShown, 8d);
                int warmModelPixels = await WaitForModelPixels(
                    equipment,
                    EvidenceRoot + "/role_model_warm_rt.png",
                    10d);
                long warmMs = MillisecondsSince(warmStart);
                bool warmReady = warmShown && warmModelPixels >= 64;
                stage.ForceCjkFont();
                string warmShot = stage.Capture(EvidenceRoot + "/role_person_warm.png");

                pass = coldReady && entryClick && activeReady && forbiddenEntryHidden
                    && activeTabClick && activeTabReady
                    && activeDetailClick && activeDetailReady && activeSlotsReady
                    && passiveClick && passiveReady && passiveDescSingleLine
                    && passiveDetailClick && passiveDetailReady && passiveSlotsReady
                    && passiveListStructure && passiveLastReachable
                    && talentClick && talentReady && talentDetailClick && talentDetailReady && talentZeroPreviewReady
                    && talentSlotsReady && talentGrayReady && talentCostIconReady && talentTypeTabsReady
                    && talentDetailScrollStructure && talentTreeScrollReady && talentUpgradeBlocked
                    && resetDialogReady && resetCancelClick && resetCancelled
                    && returnClick && returned && closeClick && closed && warmReady;
                detail = "coldReady=" + coldReady + " coldMs=" + coldMs + " coldPixels=" + coldModelPixels
                    + " entry=" + entryClick + "(" + entryHit + ") active=" + activeReady
                    + " forbiddenEntryHidden=" + forbiddenEntryHidden
                    + " activeTab=" + activeTabClick + "/" + activeTabReady + "(" + activeTabHit + ")"
                    + " activeDetail=" + activeDetailClick + "/" + activeDetailReady + "(" + activeDetailHit + ")"
                    + " activeSlots=" + activeSlotsReady + "/" + activeSlotShots.Count
                    + " activeItems=" + activeItems.Length + "/config=" + activeConfigured.Count
                    + " passive=" + passiveClick + "/" + passiveReady + "(" + passiveHit + ")"
                    + " passiveDescSingleLine=" + passiveDescSingleLine
                    + " passiveDetail=" + passiveDetailClick + "/" + passiveDetailReady + "(" + passiveDetailHit + ")"
                    + " passiveSlots=" + passiveSlotsReady + "/" + passiveSlotShots.Count
                    + " passiveList=" + passiveListStructure + "/last=" + passiveLastReachable
                    + " talent=" + talentClick + "/" + talentReady + "(" + talentHit + ")"
                    + " talentDetail=" + talentDetailClick + "/" + talentDetailReady + "(" + talentDetailHit + ")"
                    + " talentZeroPreview=" + talentZeroPreviewReady
                    + " talentSlots=" + talentSlotsReady + "/" + talentSlotShots.Count
                    + " talentGray=" + talentGrayReady + " talentCostIcon=" + talentCostIconReady
                    + " talentTypeTabs=" + talentTypeTabsReady + "/" + talentTypeShots.Count
                    + " talentTypeSlots=" + talentTypeSlotShots.Count
                    + " talentDetailScroll=" + talentDetailScrollStructure
                    + " talentTreeScroll=" + talentTreeScrollReady
                    + " talentTreeTrace=" + string.Join(";", talentTreeTrace)
                    + " talentUpgradeBlocked=" + talentUpgradeBlocked + "(" + upgradeHit + ")"
                    + " reset=" + resetDialogReady + "/cancel=" + resetCancelClick + "/"
                    + resetCancelled + "(" + resetHit + "/" + resetCancelHit + ")"
                    + " return=" + returnClick + "/" + returned + "(" + returnHit + ")"
                    + " close=" + closeClick + "/" + closed + "(" + closeHit + ")"
                    + " warmReady=" + warmReady + " warmMs=" + warmMs + " warmPixels=" + warmModelPixels
                    + " shots=" + roleShot + "|" + activeShot + "|" + activeDetailShot + "|"
                    + passiveShot + "|" + passiveDetailShot + "|" + talentShot + "|"
                    + talentDetailShot + "|" + upgradeBlockedShot + "|" + resetDialogShot + "|"
                    + resetCancelledShot + "|" + returnedShot + "|" + warmShot;
            }
            catch (Exception e)
            {
                detail = "exception=" + e;
                pass = false;
            }
            finally
            {
                ResetRoleFlow();
                SkillManager.Instance.Clear();
                DungeonModel.Instance.Clear();
                SkillTalentModel.Instance.Clear();
                RoleModel.Instance.Reset();
                ConfirmDialog.ReloadView();
                if (eventSystem != null) UnityEngine.Object.DestroyImmediate(eventSystem.gameObject);
                stage.Dispose();
            }

            Debug.Log("CLIVERIFY roleskillroute " + detail);
            Debug.Log("CLIVERIFY roleskillroute VERDICT pass=" + pass + " restored=True");
            return pass ? 0 : 3;
        }

        private static async Task PrepareData()
        {
            await SkillConfigs.EnsureLoaded();
            await SkillUIConfigs.EnsureLoaded();
            await SkillPassiveConfigs.EnsureLoaded();
            await TaskConfigs.EnsureLoaded();
            await GoodsModel.EnsureLoaded();

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
            role.BattleAttr.Attrs["att"] = 57263;
            role.BattleAttr.Attrs["wreck"] = 24067;
            role.BattleAttr.Attrs["def"] = 36438;
            role.BattleAttr.Attrs["hit"] = 1006;
            role.BattleAttr.Attrs["dodge"] = 1649;
            role.BattleAttr.Attrs["crit"] = 1016;
            role.BattleAttr.Attrs["ten"] = 1686;
            role.MarkBaseInfoReady();

            List<SkillUIConfigs.CareerSkill> configured = SkillUIConfigs.GetCareerSkills(1);
            var packet = new CliVerify.Pkt().H(configured.Count);
            for (int i = 0; i < configured.Count; i++)
                packet.I(configured[i].SkillId).H(i == configured.Count - 1 ? 1 : 2);
            byte[] bytes = packet.Bytes();
            SkillManager.Instance.Clear();
            SkillManager.Instance.CreateSkillList(new NetReader(bytes, 0, bytes.Length));

            List<SkillPassiveConfigs.PassiveSkillCfg> passive = SkillPassiveConfigs.GetForCareer(1);
            var heartSkills = new List<DungeonModel.HeartSkillInfoEntry>();
            for (int i = 0; i < passive.Count; i++)
            {
                heartSkills.Add(new DungeonModel.HeartSkillInfoEntry
                {
                    SkillId = (uint)passive[i].SkillId,
                    SkillLv = 0,
                });
            }
            DungeonModel.Instance.ApplyHeartSkillInfo(heartSkills);

            // 天赋页必须有真实21010数据，类型5与老端当前默认攻击页一致。
            SkillTalentModel.Instance.Clear();
            ApplyTalentInfo(10);
        }

        private static void ApplyTalentInfo(int lessPoint)
        {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            MethodInfo on21010 = SkillController.Instance.GetType().GetMethod("On21010", flags);
            if (on21010 == null) throw new MissingMethodException("SkillController.On21010");
            byte[] talentBytes = new CliVerify.Pkt()
                .H(lessPoint).H(1)
                .C(5).H(3).H(1).I(59340001).H(2)
                .Bytes();
            on21010.Invoke(SkillController.Instance, new object[]
            {
                new NetReader(talentBytes, 0, talentBytes.Length),
            });
        }

        private static void ResetRoleFlow()
        {
            typeof(RoleFlow).GetMethod("Reset", BindingFlags.NonPublic | BindingFlags.Static)
                ?.Invoke(null, null);
        }

        private static BaseWindowSkinView FindShownWindow(string name)
        {
            return UnityEngine.Object.FindObjectsByType<BaseWindowSkinView>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(v => v != null && v.IsShown
                    && (v.gameObject.name == name || v.transform.root.name == name));
        }

        private static async Task<BaseWindowSkinView> WaitForWindow(string name, double timeoutSeconds)
        {
            BaseWindowSkinView result = null;
            await WaitUntil(() => (result = FindShownWindow(name)) != null, timeoutSeconds);
            return result;
        }

        private static async Task<T> WaitForShown<T>(double timeoutSeconds) where T : BaseView
        {
            T result = null;
            await WaitUntil(() =>
            {
                result = UnityEngine.Object.FindObjectsByType<T>(
                        FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(v => v != null && v.IsShown && v.gameObject.activeInHierarchy);
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

        private static async Task<int> WaitForModelPixels(
            EquipmentView equipment, string evidencePath, double timeoutSeconds)
        {
            int pixels = 0;
            double deadline = EditorApplication.timeSinceStartup + timeoutSeconds;
            while (EditorApplication.timeSinceStartup < deadline)
            {
                await Task.Delay(100);
                RawImage image = equipment != null && equipment.model_gp != null
                    ? equipment.model_gp.GetComponentInChildren<RawImage>(true)
                    : null;
                pixels = CaptureRenderedPixels(image, evidencePath);
                if (pixels >= 64) break;
            }
            return pixels;
        }

        private static int CaptureRenderedPixels(RawImage image, string evidencePath)
        {
            if (image == null || !image.gameObject.activeInHierarchy
                || !(image.texture is RenderTexture rt) || !rt.IsCreated()) return 0;

            UIModelStage.RenderNow();
            RenderTexture previous = RenderTexture.active;
            Texture2D copy = null;
            try
            {
                RenderTexture.active = rt;
                copy = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false, true);
                copy.ReadPixels(new Rect(0f, 0f, rt.width, rt.height), 0, 0, false);
                copy.Apply(false, false);
                Color32[] values = copy.GetPixels32();
                int count = values.Count(p => p.a >= 8);
                if (count > 0)
                {
                    string full = Path.GetFullPath(CliVerify.AppendResolutionSuffix(evidencePath));
                    Directory.CreateDirectory(Path.GetDirectoryName(full) ?? "Temp");
                    File.WriteAllBytes(full, copy.EncodeToPNG());
                }
                return count;
            }
            finally
            {
                RenderTexture.active = previous;
                if (copy != null) UnityEngine.Object.DestroyImmediate(copy);
            }
        }

        private static bool Click(
            Component target,
            Camera camera,
            GraphicRaycaster raycaster,
            EventSystem eventSystem,
            out string hitName)
        {
            hitName = "missing";
            RectTransform rect = target != null ? target.transform as RectTransform : null;
            if (rect == null || !rect.gameObject.activeInHierarchy) return false;

            Canvas.ForceUpdateCanvases();
            var pointer = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(
                    camera, rect.TransformPoint(rect.rect.center)),
            };
            var hits = new List<RaycastResult>();
            raycaster.Raycast(pointer, hits);
            for (int i = 0; i < hits.Count; i++)
            {
                Transform hit = hits[i].gameObject.transform;
                if (hit != rect && !hit.IsChildOf(rect)) continue;
                hitName = hits[i].gameObject.name;
                ExecuteEvents.ExecuteHierarchy<IPointerClickHandler>(
                    hits[i].gameObject, pointer, ExecuteEvents.pointerClickHandler);
                return true;
            }
            hitName = string.Join("/", hits.Select(h => h.gameObject.name));
            return false;
        }

        private static bool Drag(
            Component target,
            Component dragSurface,
            Vector2 screenDelta,
            Camera camera,
            GraphicRaycaster raycaster,
            EventSystem eventSystem,
            out string hitName)
        {
            hitName = "missing";
            RectTransform rect = target != null ? target.transform as RectTransform : null;
            RectTransform surface = dragSurface != null ? dragSurface.transform as RectTransform : null;
            if (rect == null || surface == null || !rect.gameObject.activeInHierarchy
                || !surface.gameObject.activeInHierarchy || !surface.IsChildOf(rect)) return false;

            Canvas.ForceUpdateCanvases();
            Vector2 start = RectTransformUtility.WorldToScreenPoint(
                camera, surface.TransformPoint(surface.rect.center));
            var pointer = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = start,
                pressPosition = start,
            };
            var hits = new List<RaycastResult>();
            raycaster.Raycast(pointer, hits);
            for (int i = 0; i < hits.Count; i++)
            {
                Transform hit = hits[i].gameObject.transform;
                if (hit != rect && !hit.IsChildOf(rect)) continue;
                GameObject handler = ExecuteEvents.CanHandleEvent<IBeginDragHandler>(rect.gameObject)
                    ? rect.gameObject
                    : ExecuteEvents.GetEventHandler<IBeginDragHandler>(hits[i].gameObject);
                if (handler == null) continue;
                hitName = hits[i].gameObject.name;
                pointer.pointerPressRaycast = hits[i];
                ExecuteEvents.Execute(handler, pointer, ExecuteEvents.beginDragHandler);
                pointer.delta = screenDelta;
                pointer.position = start + screenDelta;
                ExecuteEvents.Execute(handler, pointer, ExecuteEvents.dragHandler);
                ExecuteEvents.Execute(handler, pointer, ExecuteEvents.endDragHandler);
                return true;
            }
            hitName = string.Join("/", hits.Select(h => h.gameObject.name));
            return false;
        }

        private static bool ClickWithin(
            Component scope,
            Component pointTarget,
            Camera camera,
            GraphicRaycaster raycaster,
            EventSystem eventSystem,
            out string hitName)
        {
            hitName = "missing";
            RectTransform scopeRect = scope != null ? scope.transform as RectTransform : null;
            RectTransform pointRect = pointTarget != null ? pointTarget.transform as RectTransform : null;
            if (scopeRect == null || pointRect == null || !scopeRect.gameObject.activeInHierarchy
                || !pointRect.gameObject.activeInHierarchy || !pointRect.IsChildOf(scopeRect)) return false;

            Canvas.ForceUpdateCanvases();
            var pointer = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(
                    camera, pointRect.TransformPoint(pointRect.rect.center)),
            };
            var hits = new List<RaycastResult>();
            raycaster.Raycast(pointer, hits);
            for (int i = 0; i < hits.Count; i++)
            {
                Transform hit = hits[i].gameObject.transform;
                if (hit != scopeRect && !hit.IsChildOf(scopeRect)) continue;
                hitName = hits[i].gameObject.name;
                ExecuteEvents.ExecuteHierarchy<IPointerClickHandler>(
                    hits[i].gameObject, pointer, ExecuteEvents.pointerClickHandler);
                return true;
            }
            hitName = string.Join("/", hits.Select(h => h.gameObject.name));
            return false;
        }

        private static bool IsInsideViewport(RectTransform item, RectTransform viewport)
        {
            if (item == null || viewport == null || !item.gameObject.activeInHierarchy) return false;
            Canvas.ForceUpdateCanvases();
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, item);
            Rect rect = viewport.rect;
            const float tolerance = 1f;
            return bounds.min.x >= rect.xMin - tolerance && bounds.max.x <= rect.xMax + tolerance
                && bounds.min.y >= rect.yMin - tolerance && bounds.max.y <= rect.yMax + tolerance;
        }

        private static long MillisecondsSince(double start)
        {
            return Math.Max(0L, (long)Math.Round((EditorApplication.timeSinceStartup - start) * 1000d));
        }
    }
}
