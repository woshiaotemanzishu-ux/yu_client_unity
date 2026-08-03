using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Common.Proto;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Alert;
using Shenxiao.Generated.UI.Common;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Dress;
using Shenxiao.Module.Core.Fashion;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.OutWard;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Setting;
using Shenxiao.Editor.UiCreator.Dress;
using Shenxiao.Editor.UiCreator.Fashion;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 设置 → 更换头像 → 当前版时装/装扮/头像的真实 Prefab 点击验收。
    /// 同时覆盖外层四页签、装扮三子页、条目预览切换、穿戴/卸下即时刷新与首次/热开耗时。
    /// </summary>
    public static class SettingFashionCurrentCase
    {
        private const BindingFlags StaticNonPublic = BindingFlags.Static | BindingFlags.NonPublic;
        private static readonly string[] ResourceRoots =
        {
            "Assets/GameRes/resource/game",
            "Assets/GameRes/resource/object/fashion",
        };

        public static async Task<int> Run()
        {
            bool fallbackBefore = ResManager.EditorPreferFallback;
            FieldInfo interceptField = typeof(DressController).GetField("s_outboundIntercept", StaticNonPublic);
            object oldIntercept = interceptField?.GetValue(null);
            FieldInfo fashionInterceptField = typeof(FashionController).GetField("s_outboundIntercept", StaticNonPublic);
            object oldFashionIntercept = fashionInterceptField?.GetValue(null);
            bool controllerWasInitialized = DressController.Instance.IsInitialized;
            FigureProto oldFigure = RoleModel.Instance.Figure;
            var frames = new List<byte[]>();
            var fashionFrames = new List<byte[]>();
            CliVerify.Stage stage = null;
            GameObject eventSystemGo = null;
            HashSet<string> resourcesBefore = null;

            try
            {
                ResManager.EditorPreferFallback = true;
                FashionFlow.Reset();
                ResetSettingFlow();
                if (!controllerWasInitialized) DressController.Instance.Init();
                if (interceptField == null) throw new MissingFieldException("DressController.s_outboundIntercept");
                interceptField.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));
                if (fashionInterceptField == null) throw new MissingFieldException("FashionController.s_outboundIntercept");
                fashionInterceptField.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    fashionFrames.Add(frame);
                    return true;
                }));

                bool resourcePreflight = DressAssetPreflight.EnsureAddressables()
                    && FashionAssetPreflight.EnsureAddressables();
                if (!resourcePreflight) throw new InvalidOperationException("DressAssetPreflight failed");
                // 预检负责在构建/验收前一次性补齐资源闭包；从这里才开始观察，后续任何新增文件都
                // 是玩家点击过程中发生的运行时按需导入，必须挂红。
                resourcesBefore = SnapshotResources();
                await DressConfigs.EnsureLoaded();
                await GoodsModel.EnsureLoaded();
                await FashionConfigs.EnsureLoaded();
                await LoginConfigs.EnsureLoaded();
                Shenxiao.Module.Core.Bag.BagModel.Instance.Clear();
                SeedFashionState();
                SeedPreviewFigure();
                int career = RoleModel.Instance.Career > 0 ? RoleModel.Instance.Career : 1;
                IReadOnlyList<DressConfigs.Row> headRows = DressConfigs.GetDisplayRows(DressView.HeadType);
                DressConfigs.Row activeHead = headRows.FirstOrDefault(row =>
                    !string.IsNullOrEmpty(DressConfigs.GetHeadIcon(row, career)));
                if (activeHead == null) throw new InvalidOperationException("头像配置没有可展示的 career icon");
                DressModel.Instance.Replace(DressView.HeadType, activeHead.Id,
                    new List<DressModel.Entry> { new DressModel.Entry(activeHead.Id, 1, 12345, 23456) });

                stage = CliVerify.Stage.Create();
                eventSystemGo = new GameObject("SettingFashionCurrent_EventSystem", typeof(EventSystem));
                EventSystem eventSystem = eventSystemGo.GetComponent<EventSystem>();
                Canvas canvas = stage.CanvasRoot.GetComponent<Canvas>();
                GraphicRaycaster raycaster = stage.CanvasRoot.GetComponent<GraphicRaycaster>();
                Camera camera = canvas.worldCamera;

                SettingFlow.Open();
                SettingView setting = await WaitActive<SettingView>(8d);
                bool settingVisible = setting != null && setting.change_head_btn != null;
                stage.ForceCjkFont();
                string settingShot = stage.Capture("output/settings_fashion_current/setting.png");

                Stopwatch firstWatch = Stopwatch.StartNew();
                bool settingClick = settingVisible && Click(setting.change_head_btn, camera, raycaster, eventSystem);
                DressView dress = await WaitDressVisual(DressView.HeadType, 10d);
                firstWatch.Stop();

                BaseWindowSkinView window = FindActive<BaseWindowSkinView>();
                DressSubView sub = FindActive<DressSubView>();
                TabButtonTwoSkin[] outerTabs = window != null
                    ? window.GetComponentsInChildren<TabButtonTwoSkin>(false)
                    : Array.Empty<TabButtonTwoSkin>();
                string[] expectedOuter = { "时装", "发饰", "装扮", "套装" };
                string[] actualOuter = outerTabs.Select(TabText).ToArray();
                bool outerOk = window != null && window.CurrentIndex == 2 && outerTabs.Length == 4
                    && expectedOuter.SequenceEqual(actualOuter);
                bool headOk = dress != null && dress.SelectedType == DressView.HeadType && sub != null
                    && sub.Type == DressView.HeadType && sub.VisibleItemCount == headRows.Count
                    && sub.SelectedId == activeHead.Id && sub.PreviewReady
                    && sub.model_img != null && sub.model_img.gameObject.activeSelf && sub.model_img.sprite != null;
                bool firstFast = firstWatch.ElapsedMilliseconds < 5000;

                stage.ForceCjkFont();
                string avatarShot = stage.Capture("output/settings_fashion_current/avatar.png");

                DressSkillItem[] skillItems = sub != null
                    ? sub.GetComponentsInChildren<DressSkillItem>(false)
                    : Array.Empty<DressSkillItem>();
                bool skillItemsOk = skillItems.Length == 3 && skillItems.All(item => item.IsVisualReady);
                bool skillClick = skillItemsOk && Click(skillItems[0].skill_img, camera, raycaster, eventSystem);
                SkillTipsViewBind skillTip = skillClick ? await WaitActive<SkillTipsViewBind>(5d) : null;
                bool skillTipOk = skillTip != null && skillTip.icon != null && skillTip.icon.sprite != null
                    && !string.IsNullOrWhiteSpace(skillTip.name_text?.text)
                    && !string.IsNullOrWhiteSpace(skillTip.des_text?.text);
                stage.ForceCjkFont();
                string skillTipShot = stage.Capture("output/settings_fashion_current/skill_tip.png");
                bool skillTipClose = skillTipOk && Click(skillTip._Image1, camera, raycaster, eventSystem);
                await Task.Delay(100);
                skillTipClose = skillTipClose && FindActive<SkillTipsViewBind>() == null;

                DressTab[] innerTabs = dress != null
                    ? dress.GetComponentsInChildren<DressTab>(false)
                    : Array.Empty<DressTab>();
                DressTab bubble = innerTabs.FirstOrDefault(tab => tab._lb != null && tab._lb.text == "气泡");
                DressTab photo = innerTabs.FirstOrDefault(tab => tab._lb != null && tab._lb.text == "相框");
                DressTab head = innerTabs.FirstOrDefault(tab => tab._lb != null && tab._lb.text == "头像");

                bool bubbleClick = Click(bubble?._Image1, camera, raycaster, eventSystem);
                bool bubbleOk = bubbleClick && await WaitType(DressView.BubbleType, 5d);
                stage.ForceCjkFont();
                string bubbleShot = stage.Capture("output/settings_fashion_current/bubble.png");

                bool photoClick = Click(photo?._Image1, camera, raycaster, eventSystem);
                bool photoOk = photoClick && await WaitType(DressView.PhotoType, 5d);
                stage.ForceCjkFont();
                string photoShot = stage.Capture("output/settings_fashion_current/photo.png");

                bool headClick = Click(head?._Image1, camera, raycaster, eventSystem);
                bool headBackOk = headClick && await WaitType(DressView.HeadType, 5d);
                sub = FindActive<DressSubView>();
                DressItem[] items = sub != null ? sub.GetComponentsInChildren<DressItem>(false) : Array.Empty<DressItem>();
                DressItem candidate = items.FirstOrDefault(item => item.DressId != sub.SelectedId);
                uint beforeSelected = sub?.SelectedId ?? 0;
                bool uniqueItemSurface = candidate != null && HasUniqueItemSurface(candidate);
                bool itemClick = candidate != null && Click(candidate.ClickSurface, camera, raycaster, eventSystem);
                await WaitUntil(() => sub != null && sub.PreviewReady, 5d);
                bool itemChanged = itemClick && sub != null && sub.SelectedId != beforeSelected
                    && sub.PreviewReady && sub.model_img != null && sub.model_img.gameObject.activeSelf
                    && sub.model_img.sprite != null && !string.IsNullOrWhiteSpace(sub.dress_name?.text);

                // 返回已激活头像，真实点击“卸下→使用”，分别喂权威回包并断言父页即时刷新。
                DressItem activeItem = items.FirstOrDefault(item => item.DressId == activeHead.Id);
                bool activeItemClick = activeItem != null && Click(activeItem.ClickSurface, camera, raycaster, eventSystem);
                await Task.Delay(150);
                frames.Clear();
                bool takeOffClick = activeItemClick && sub != null && Click(sub.use_btn, camera, raycaster, eventSystem);
                bool takeOffFrame = frames.Count == 1 && Command(frames[0]) == Proto.DRESS_TAKE_OFF;
                MethodInfo on11203 = typeof(DressController).GetMethod("On11203", BindingFlags.Instance | BindingFlags.NonPublic);
                Feed(on11203, new CliVerify.Pkt().I(1).C(DressView.HeadType).I(activeHead.Id).Bytes());
                await Task.Delay(150);
                bool takeOffImmediate = sub != null && sub.use_btn_label != null && sub.use_btn_label.text == "使用"
                    && DressModel.Instance.TryGet(DressView.HeadType, out DressModel.Snapshot takenOff)
                    && takenOff.UsedDressId == 0;

                frames.Clear();
                bool useClick = takeOffImmediate && Click(sub.use_btn, camera, raycaster, eventSystem);
                bool useFrame = frames.Count == 1 && Command(frames[0]) == Proto.DRESS_USE;
                MethodInfo on11202 = typeof(DressController).GetMethod("On11202", BindingFlags.Instance | BindingFlags.NonPublic);
                Feed(on11202, new CliVerify.Pkt().I(1).C(DressView.HeadType).I(activeHead.Id).Bytes());
                await Task.Delay(150);
                bool useImmediate = sub.use_btn_label != null && sub.use_btn_label.text == "卸下"
                    && DressModel.Instance.TryGet(DressView.HeadType, out DressModel.Snapshot wornAgain)
                    && wornAgain.UsedDressId == activeHead.Id;
                bool dressWrites = takeOffClick && takeOffFrame && takeOffImmediate && useClick && useFrame && useImmediate;

                // 进入该窗口后，外层另外三个固定页签同样必须逐个走真实点击，不把“能看到页签”当验收。
                TabButtonTwoSkin fashionTab = outerTabs.FirstOrDefault(tab => TabText(tab) == "时装");
                TabButtonTwoSkin hairTab = outerTabs.FirstOrDefault(tab => TabText(tab) == "发饰");
                TabButtonTwoSkin dressTab = outerTabs.FirstOrDefault(tab => TabText(tab) == "装扮");
                TabButtonTwoSkin suitTab = outerTabs.FirstOrDefault(tab => TabText(tab) == "套装");
                bool fashionClick = Click(fashionTab?._Image1, camera, raycaster, eventSystem);
                FashionMainView fashionView = fashionClick ? await WaitActive<FashionMainView>(5d) : null;
                bool fashionModel = fashionView != null && await WaitUntil(
                    () => fashionView.IsModelPreviewReady && fashionView.RenderedColorId == 0, 12d);
                HorizontalLayoutGroup fashionLayout = fashionView?._list_fashion_item?.GetComponent<HorizontalLayoutGroup>();
                VerticalLayoutGroup colorLayout = fashionView?._box_color_item?.GetComponent<VerticalLayoutGroup>();
                Transform obsoleteFight = fashionView?.transform.Find("_box_fight");
                bool fashionVisual = fashionView != null
                    && Approximately(fashionView._list_fashion_item.anchoredPosition, new Vector2(95f, -618f))
                    && fashionLayout != null && Mathf.Approximately(fashionLayout.spacing, 14f)
                    && colorLayout != null && Mathf.Approximately(colorLayout.spacing, 0f)
                    && (obsoleteFight == null || !obsoleteFight.gameObject.activeSelf);
                bool fashionOk = fashionView != null && fashionView.PosId == 1 && window.CurrentIndex == 0
                    && fashionModel && fashionVisual && fashionView.PreviewHasWeapon
                    && fashionView.PreviewEffectCount > 0;
                stage.ForceCjkFont();
                string fashionShot = stage.Capture("output/settings_fashion_current/fashion.png");

                FashionItem[] fashionItems = fashionView != null
                    ? fashionView.GetComponentsInChildren<FashionItem>(false)
                        .OrderBy(item => item.transform.GetSiblingIndex()).ToArray()
                    : Array.Empty<FashionItem>();
                FashionItem[] inactiveFashionItems = fashionItems.Where(item =>
                    FashionModel.Instance.GetActive(fashionView.PosId, item.FashionId) == null).ToArray();
                bool fashionGray = UIGrayStyle.Material != null && inactiveFashionItems.Length > 0
                    && inactiveFashionItems.All(item => item.fashion_icon_image != null
                        && item.fashion_icon_image.material == UIGrayStyle.Material
                        && item.fashion_plate_image != null
                        && item.fashion_plate_image.material == UIGrayStyle.Material);
                bool inactiveRedHidden = inactiveFashionItems.Length > 0
                    && inactiveFashionItems.All(item => item.fashion_red_image == null
                        || !item.fashion_red_image.gameObject.activeSelf);
                bool gradeRedHidden = fashionView?._img_grade_red == null
                    || !fashionView._img_grade_red.gameObject.activeSelf;
                bool redStateDynamic = false;
                if (inactiveFashionItems.Length > 0)
                {
                    FashionItem redProbe = inactiveFashionItems[0];
                    List<(int type, int typeId, long num)> activationCosts = FashionConfigs.ParseCostList(
                        FashionConfigs.GetBaseRow(fashionView.PosId, redProbe.FashionId, 1).ActiveCostJson);
                    var goods = activationCosts.Select((cost, index) => new Shenxiao.Module.Core.Bag.BagGoods
                    {
                        GoodsId = 990000L + index,
                        TypeId = cost.typeId,
                        GoodsNum = cost.num,
                    }).ToList();
                    Shenxiao.Module.Core.Bag.BagModel.Instance.SetBagFull(goods.Count, goods.Count, goods);
                    Shenxiao.Framework.Event.EventDispatcher.Emit(Shenxiao.Framework.Event.GlobalEvent.EVT_BAG_UPDATE);
                    await Task.Delay(100);
                    bool redAppears = activationCosts.Count > 0 && redProbe.fashion_red_image != null
                        && redProbe.fashion_red_image.gameObject.activeSelf;
                    Shenxiao.Module.Core.Bag.BagModel.Instance.Clear();
                    Shenxiao.Framework.Event.EventDispatcher.Emit(Shenxiao.Framework.Event.GlobalEvent.EVT_BAG_UPDATE);
                    await Task.Delay(100);
                    bool redClears = redProbe.fashion_red_image == null
                        || !redProbe.fashion_red_image.gameObject.activeSelf;
                    redStateDynamic = redAppears && redClears;
                }
                fashionOk = fashionOk && fashionGray && inactiveRedHidden && gradeRedHidden && redStateDynamic;
                bool secondFashionSelected = fashionItems.Length > 1
                    && Click(fashionItems[1].ClickSurface, camera, raycaster, eventSystem)
                    && await WaitUntil(() => fashionView.SelectedFashionId == fashionItems[1].FashionId
                        && fashionView.IsModelPreviewReady, 12d);

                FashionColorItem[] colorItems = fashionView != null
                    ? fashionView.GetComponentsInChildren<FashionColorItem>(false)
                    : Array.Empty<FashionColorItem>();
                colorItems = colorItems.OrderBy(item => item.ColorId).ToArray();
                bool colorStates = secondFashionSelected && colorItems.Length == 4 && await WaitUntil(
                    () => colorItems.All(item => item != null && item.IsVisualReady), 5d);
                bool colorRedHidden = colorStates && colorItems.All(item => item.red == null
                    || !item.red.gameObject.activeSelf);
                colorStates = colorStates && colorRedHidden;
                var colorShots = new List<string>();
                for (int colorIndex = 1; colorStates && colorIndex <= 3; colorIndex++)
                {
                    int expectedColor = colorItems[colorIndex].ColorId;
                    colorStates = Click(colorItems[colorIndex].ClickSurface, camera, raycaster, eventSystem)
                        && await WaitUntil(() => fashionView.SelectedColorId == expectedColor
                            && fashionView.RenderedColorId == expectedColor
                            && fashionView.IsModelPreviewReady, 12d);
                    colorStates = colorStates && fashionView.RenderedTextureName.EndsWith(
                        "_" + expectedColor, StringComparison.Ordinal);
                    stage.ForceCjkFont();
                    colorShots.Add(stage.Capture("output/settings_fashion_current/fashion_color" + colorIndex + ".png"));
                }

                bool baseColorClick = colorStates && Click(colorItems[0].ClickSurface, camera, raycaster, eventSystem);
                bool baseColorRestored = baseColorClick && await WaitUntil(
                    () => fashionView.SelectedColorId == 0 && fashionView.RenderedColorId == 0, 12d);
                string fashionTextureDiag = fashionView.RenderedTextureName;
                int fashionEffectDiag = fashionView.PreviewEffectCount;

                bool hairClick = Click(hairTab?._Image1, camera, raycaster, eventSystem);
                await Task.Delay(250);
                fashionView = FindActive<FashionMainView>();
                bool hairOk = hairClick && fashionView != null && fashionView.PosId == 3 && window.CurrentIndex == 1
                    && await WaitUntil(() => fashionView.IsModelPreviewReady, 12d);
                stage.ForceCjkFont();
                string hairShot = stage.Capture("output/settings_fashion_current/hair.png");

                bool suitClick = Click(suitTab?._Image1, camera, raycaster, eventSystem);
                FashionSuitView suitView = suitClick ? await WaitActive<FashionSuitView>(5d) : null;
                bool suitOk = suitView != null && window.CurrentIndex == 3
                    && await WaitUntil(() => suitView.IsModelPreviewReady, 15d);
                var suitShots = new List<string>();
                FashionSuitTabItem[] suitItems = suitView != null
                    ? suitView.GetComponentsInChildren<FashionSuitTabItem>(false)
                        .OrderBy(item => item.SuitId).ToArray()
                    : Array.Empty<FashionSuitTabItem>();
                suitOk = suitOk && suitItems.Length == 4;
                FightingShowSmallItem suitFight = suitView != null
                    ? suitView.GetComponentInChildren<FightingShowSmallItem>(false)
                    : null;
                bool suitFightOk = suitFight != null && suitFight._lb_fighting != null
                    && suitFight._lb_fighting.text == "0";
                bool inactiveWearHidden = suitView != null
                    && (suitView._img_change == null || !suitView._img_change.gameObject.activeSelf)
                    && (suitView._img_changed == null || !suitView._img_changed.gameObject.activeSelf);
                FashionSuitGoodsItem[] suitGoods = suitView != null
                    ? suitView.GetComponentsInChildren<FashionSuitGoodsItem>(false)
                    : Array.Empty<FashionSuitGoodsItem>();
                bool suitGoodsGray = suitGoods.Length == 4 && suitGoods.All(item =>
                {
                    BaseAwardItem award = item.GetComponentInChildren<BaseAwardItem>(false);
                    return award != null && award.icon != null && award.icon.material == UIGrayStyle.Material
                        && award.item_bg != null && award.item_bg.material == UIGrayStyle.Material;
                });
                bool suitNameVertical = suitView != null && suitView._lb_name != null
                    && suitView._lb_name.text.Count(character => character == '\n')
                        == Mathf.Max(0, suitView._lb_name.text.Count(character => character != '\n') - 1);
                suitOk = suitOk && suitFightOk && inactiveWearHidden && suitGoodsGray && suitNameVertical;
                for (int suitIndex = 0; suitOk && suitIndex < suitItems.Length; suitIndex++)
                {
                    int expectedSuit = suitItems[suitIndex].SuitId;
                    suitOk = (suitIndex == 0 || Click(suitItems[suitIndex].ClickSurface, camera, raycaster, eventSystem))
                        && await WaitUntil(() => suitView.SelectedSuitId == expectedSuit
                            && suitView.RenderedSuitId == expectedSuit && suitView.IsModelPreviewReady, 15d);
                    FashionConfigs.SuitRow suitConfig = FashionConfigs.GetSuit(expectedSuit);
                    bool expectsMount = suitConfig != null && suitConfig.Conditions.Any(condition => condition.Type == 2 && condition.SubType == 1);
                    bool expectsWing = suitConfig != null && suitConfig.Conditions.Any(condition => condition.Type == 2 && condition.SubType == 3);
                    bool expectsWeapon = suitConfig != null && suitConfig.Conditions.Any(condition => condition.Type == 2 && condition.SubType == 5);
                    bool partsOk = expectsMount == suitView.PreviewHasMount
                        && expectsWing == suitView.PreviewHasWing
                        && expectsWeapon == suitView.PreviewHasWeapon;
                    Debug.Log("CLIVERIFY setting-fashion-current suit=" + expectedSuit
                        + " expected=" + expectsMount + "/" + expectsWing + "/" + expectsWeapon
                        + " rendered=" + suitView.PreviewHasMount + "/" + suitView.PreviewHasWing + "/"
                        + suitView.PreviewHasWeapon + " effects=" + suitView.PreviewEffectCount
                        + " partsOk=" + partsOk);
                    suitOk = suitOk && partsOk && suitView.PreviewEffectCount > 0;
                    stage.ForceCjkFont();
                    suitShots.Add(stage.Capture("output/settings_fashion_current/suit_" + expectedSuit + ".png"));
                }
                string suitShot = suitShots.Count > 0 ? suitShots[0] : stage.Capture("output/settings_fashion_current/suit.png");

                // 套装页不是只切四个预览：可见的“更换”必须真实点出确认框，发送两条 41302，
                // 并在权威回包后由父页立即从“更换”切到“已更换”。其余两件幻化预置为已穿，
                // 避免用未连接网络掩盖按钮链是否存在。
                FashionConfigs.SuitRow wearSuit = suitItems.Length > 0 ? FashionConfigs.GetSuit(suitItems[0].SuitId) : null;
                bool wearSelect = wearSuit != null && Click(suitItems[0].ClickSurface, camera, raycaster, eventSystem)
                    && await WaitUntil(() => suitView.SelectedSuitId == wearSuit.Id && suitView.IsModelPreviewReady, 15d);
                SeedWearableSuit(wearSuit);
                InvokeSuitRefresh(suitView);
                await Task.Delay(100);
                bool wearButtonReady = wearSelect && suitView._img_change != null
                    && suitView._img_change.gameObject.activeSelf
                    && suitView._img_changed != null && !suitView._img_changed.gameObject.activeSelf;
                fashionFrames.Clear();
                bool wearClick = wearButtonReady && Click(suitView._img_change, camera, raycaster, eventSystem);
                AlertTypeTwoBind confirm = wearClick ? await WaitActive<AlertTypeTwoBind>(5d) : null;
                await Task.Delay(100);
                Canvas.ForceUpdateCanvases();
                bool wearConfirm = confirm != null && Click(confirm._ok_btn, camera, raycaster, eventSystem);
                await Task.Delay(100);
                FashionConfigs.SuitCondition[] fashionConditions = wearSuit.Conditions
                    .Where(condition => condition.Type == 1).ToArray();
                bool wearFrames = wearConfirm && fashionFrames.Count == fashionConditions.Length
                    && fashionFrames.All(frame => Command(frame) == Proto.FASHION_WEAR);
                MethodInfo on41302 = typeof(FashionController).GetMethod("On41302", BindingFlags.Instance | BindingFlags.NonPublic);
                foreach (FashionConfigs.SuitCondition condition in fashionConditions)
                    FeedFashion(on41302, new CliVerify.Pkt().I(1).C(condition.SubType).I(condition.TypeId).C(0).Bytes());
                await Task.Delay(150);
                bool wearImmediate = suitView._img_change != null && !suitView._img_change.gameObject.activeSelf
                    && suitView._img_changed != null && suitView._img_changed.gameObject.activeSelf;
                FashionFlow.Close();
                await Task.Delay(100);
                FashionFlow.Open(3);
                FashionSuitView reopenedSuit = await WaitActive<FashionSuitView>(5d);
                bool wearReopen = reopenedSuit != null && await WaitUntil(
                    () => reopenedSuit.SelectedSuitId == wearSuit.Id && reopenedSuit.IsModelPreviewReady, 15d)
                    && reopenedSuit._img_change != null && !reopenedSuit._img_change.gameObject.activeSelf
                    && reopenedSuit._img_changed != null && reopenedSuit._img_changed.gameObject.activeSelf;
                suitView = reopenedSuit ?? suitView;
                bool suitWear = wearButtonReady && wearClick && wearConfirm && wearFrames && wearImmediate && wearReopen;
                Debug.Log("CLIVERIFY setting-fashion-current suit-wear select=" + wearSelect
                    + " button=" + wearButtonReady + " click=" + wearClick + " confirm=" + wearConfirm
                    + " frames=" + fashionFrames.Count + "/" + fashionConditions.Length + "/" + wearFrames
                    + " immediate=" + wearImmediate + " reopen=" + wearReopen);

                bool dressTabClick = Click(dressTab?._Image1, camera, raycaster, eventSystem);
                bool dressReturnOk = dressTabClick && await WaitType(DressView.HeadType, 5d) && window.CurrentIndex == 2;

                FashionFlow.Close();
                await Task.Delay(100);
                Stopwatch warmWatch = Stopwatch.StartNew();
                FashionFlow.OpenDress(DressView.HeadType);
                DressView warmDress = await WaitDressVisual(DressView.HeadType, 3d);
                warmWatch.Stop();
                bool warmFast = warmDress != null && warmWatch.ElapsedMilliseconds < 1000;

                HashSet<string> resourcesAfter = SnapshotResources();
                string[] addedResources = resourcesAfter.Except(resourcesBefore, StringComparer.OrdinalIgnoreCase).ToArray();
                bool noRuntimeImport = addedResources.Length == 0;

                bool pass = resourcePreflight && settingVisible && settingClick && outerOk && headOk && firstFast
                    && innerTabs.Length == 3 && bubbleOk && photoOk && headBackOk
                    && skillItemsOk && skillTipOk && skillTipClose
                    && uniqueItemSurface && itemChanged && dressWrites
                    && fashionOk && colorStates && baseColorRestored
                    && hairOk && suitOk && suitWear && dressReturnOk && warmFast && noRuntimeImport;
                Debug.Log("CLIVERIFY setting-fashion-current shots=" + settingShot + " | " + avatarShot
                    + " | " + skillTipShot
                    + " | " + bubbleShot + " | " + photoShot + " | " + fashionShot
                    + " | " + hairShot + " | " + suitShot);
                Debug.Log("CLIVERIFY setting-fashion-current timing firstMs=" + firstWatch.ElapsedMilliseconds
                    + " warmMs=" + warmWatch.ElapsedMilliseconds + " addedResources="
                    + (addedResources.Length == 0 ? "none" : string.Join(",", addedResources)));
                Debug.Log("CLIVERIFY setting-fashion-current VERDICT setting=" + settingVisible + "/" + settingClick
                    + " outer=" + outerOk + " head=" + headOk + " inner=" + innerTabs.Length
                    + " skills=" + skillItemsOk + "/" + skillTipOk + "/" + skillTipClose
                    + " bubble=" + bubbleOk + " photo=" + photoOk + " headBack=" + headBackOk
                    + " itemSurface=" + uniqueItemSurface + " itemChanged=" + itemChanged
                    + " dressWrites=" + takeOffClick + "/" + takeOffFrame + "/" + takeOffImmediate
                    + "/" + useClick + "/" + useFrame + "/" + useImmediate
                    + " outerChildren="
                    + fashionOk + "/item2=" + secondFashionSelected
                    + "/colors=" + colorStates + "/texture=" + fashionTextureDiag
                    + "/effects=" + fashionEffectDiag + "/base=" + baseColorRestored
                    + "/" + hairOk + "/" + suitOk + "/fight=" + suitFightOk
                    + "/fashionGray=" + fashionGray + "/redHidden=" + inactiveRedHidden + "/" + colorRedHidden
                    + "/redDynamic=" + redStateDynamic
                    + "/gradeRedHidden=" + gradeRedHidden + "/inactiveWearHidden=" + inactiveWearHidden
                    + "/suitGoodsGray=" + suitGoodsGray + "/suitNameVertical=" + suitNameVertical
                    + "/suitWear=" + suitWear + "/" + dressReturnOk
                    + " firstFast=" + firstFast
                    + " warmFast=" + warmFast + " noRuntimeImport=" + noRuntimeImport + " pass=" + pass);
                return pass ? 0 : 3;
            }
            catch (Exception exception)
            {
                Debug.LogError("CLIVERIFY setting-fashion-current EXCEPTION " + exception);
                return 3;
            }
            finally
            {
                FashionFlow.Reset();
                FashionModel.Instance.Clear();
                OutWardModel.Instance.Clear();
                Shenxiao.Module.Core.Bag.BagModel.Instance.Clear();
                RoleModel.Instance.Figure = oldFigure;
                DressSkillTipFlow.Reset();
                ResetSettingFlow();
                if (!controllerWasInitialized && DressController.Instance.IsInitialized) DressController.Instance.Dispose();
                interceptField?.SetValue(null, oldIntercept);
                fashionInterceptField?.SetValue(null, oldFashionIntercept);
                if (eventSystemGo != null) Object.DestroyImmediate(eventSystemGo);
                stage?.Dispose();
                ResManager.EditorPreferFallback = fallbackBefore;
            }
        }

        private static async Task<DressView> WaitDressVisual(byte type, double timeoutSeconds)
        {
            double deadline = EditorApplication.timeSinceStartup + timeoutSeconds;
            while (EditorApplication.timeSinceStartup < deadline)
            {
                Canvas.ForceUpdateCanvases();
                DressView dress = FindActive<DressView>();
                DressSubView sub = FindActive<DressSubView>();
                if (dress != null && dress.SelectedType == type && sub != null && sub.Type == type
                    && sub.VisibleItemCount > 0 && sub.PreviewReady && sub.model_img != null
                    && sub.model_img.gameObject.activeSelf && sub.model_img.sprite != null)
                {
                    DressItem[] items = sub.GetComponentsInChildren<DressItem>(false);
                    DressSkillItem[] skills = sub.GetComponentsInChildren<DressSkillItem>(false);
                    if (items.Length == sub.VisibleItemCount && items.All(item => item.DressType == type)
                        && skills.Length > 0 && skills.All(item => item.IsVisualReady))
                        return dress;
                }
                await Task.Delay(50);
            }
            return null;
        }

        private static int Command(byte[] frame)
            => frame != null && frame.Length >= 6 ? (frame[4] << 8) | frame[5] : -1;

        private static void Feed(MethodInfo method, byte[] bytes)
        {
            if (method == null) throw new MissingMethodException("Dress response handler");
            var reader = new NetReader(bytes, 0, bytes.Length);
            method.Invoke(DressController.Instance, new object[] { reader });
            if (reader.Remaining != 0) throw new InvalidDataException(method.Name + " remaining=" + reader.Remaining);
        }

        private static void FeedFashion(MethodInfo method, byte[] bytes)
        {
            if (method == null) throw new MissingMethodException("Fashion response handler");
            var reader = new NetReader(bytes, 0, bytes.Length);
            method.Invoke(FashionController.Instance, new object[] { reader });
            if (reader.Remaining != 0) throw new InvalidDataException(method.Name + " remaining=" + reader.Remaining);
        }

        private static async Task<bool> WaitType(byte type, double timeoutSeconds)
        {
            return await WaitDressVisual(type, timeoutSeconds) != null;
        }

        private static async Task<T> WaitActive<T>(double timeoutSeconds) where T : BaseView
        {
            double deadline = EditorApplication.timeSinceStartup + timeoutSeconds;
            while (EditorApplication.timeSinceStartup < deadline)
            {
                T view = FindActive<T>();
                if (view != null) return view;
                await Task.Delay(50);
            }
            return null;
        }

        private static async Task<bool> WaitUntil(Func<bool> predicate, double timeoutSeconds)
        {
            double deadline = EditorApplication.timeSinceStartup + timeoutSeconds;
            while (EditorApplication.timeSinceStartup < deadline)
            {
                Canvas.ForceUpdateCanvases();
                if (predicate()) return true;
                await Task.Delay(50);
            }
            return false;
        }

        private static bool Approximately(Vector2 actual, Vector2 expected)
            => Vector2.SqrMagnitude(actual - expected) < 0.01f;

        private static void SeedFashionState()
        {
            FashionModel.Instance.Clear();
            OutWardModel.Instance.Clear();
            IReadOnlyList<int> clothes = FashionConfigs.GetFashionIds(1);
            IReadOnlyList<int> heads = FashionConfigs.GetFashionIds(3);
            if (clothes.Count == 0 || heads.Count == 0) throw new InvalidOperationException("fashion model rows empty");
            int wornClothe = clothes[Mathf.Min(1, clothes.Count - 1)];
            int wornHead = heads[Mathf.Min(1, heads.Count - 1)];
            FashionModel.Instance.Apply41300(new List<FashionModel.PosWire>
            {
                new FashionModel.PosWire
                {
                    PosId = 1,
                    WearFashionId = wornClothe,
                    PosLv = 1,
                    Fashions = new List<FashionModel.FashionWire>
                    {
                        new FashionModel.FashionWire
                        {
                            FashionId = wornClothe,
                            StarLv = 1,
                            NowColorId = 0,
                            Colors = new List<FashionModel.ColorWire>(),
                        },
                    },
                },
                new FashionModel.PosWire
                {
                    PosId = 3,
                    WearFashionId = wornHead,
                    PosLv = 0,
                    Fashions = new List<FashionModel.FashionWire>
                    {
                        new FashionModel.FashionWire
                        {
                            FashionId = wornHead,
                            StarLv = 1,
                            NowColorId = 0,
                            Colors = new List<FashionModel.ColorWire>(),
                        },
                    },
                },
            });
            FashionModel.Instance.Apply41313(FashionConfigs.GetSuits().Select(suit => new FashionModel.SuitWire
            {
                SuitId = suit.Id,
                Lv = 0,
                ActiveNum = 0,
                ConformNum = 0,
                Power = 0,
                NextPower = 26760,
            }).ToList());
        }

        private static void SeedWearableSuit(FashionConfigs.SuitRow suit)
        {
            if (suit == null) throw new ArgumentNullException(nameof(suit));
            List<FashionModel.PosWire> positions = suit.Conditions
                .Where(condition => condition.Type == 1)
                .GroupBy(condition => condition.SubType)
                .Select(group => new FashionModel.PosWire
                {
                    PosId = group.Key,
                    WearFashionId = 0,
                    PosLv = 1,
                    Fashions = group.Select(condition => new FashionModel.FashionWire
                    {
                        FashionId = condition.TypeId,
                        StarLv = 1,
                        NowColorId = 0,
                        Colors = new List<FashionModel.ColorWire>(),
                    }).ToList(),
                }).ToList();
            FashionModel.Instance.Apply41300(positions);
            FashionModel.Instance.Apply41313(FashionConfigs.GetSuits().Select(row => new FashionModel.SuitWire
            {
                SuitId = row.Id,
                Lv = row.Id == suit.Id ? 1 : 0,
                ActiveNum = row.Id == suit.Id ? FashionModel.SUIT_PERFECT_ACTIVE_COUNT : 0,
                ConformNum = row.Id == suit.Id ? FashionModel.SUIT_PERFECT_ACTIVE_COUNT : 0,
                Power = row.Id == suit.Id ? 26760 : 0,
                NextPower = row.Id == suit.Id ? 31755 : 26760,
            }).ToList());

            OutWardModel.Instance.Clear();
            foreach (FashionConfigs.SuitCondition condition in suit.Conditions.Where(condition => condition.Type == 2))
            {
                OutWardModel.Instance.Apply16006(condition.SubType, condition.TypeId, 0,
                    new List<OutWardModel.FigureBriefVo>
                    {
                        new OutWardModel.FigureBriefVo { Id = condition.TypeId, Stage = 1, Star = 1 },
                    });
            }
        }

        private static void InvokeSuitRefresh(FashionSuitView suitView)
        {
            MethodInfo refresh = typeof(FashionSuitView).GetMethod("Refresh", BindingFlags.Instance | BindingFlags.NonPublic);
            if (refresh == null) throw new MissingMethodException("FashionSuitView.Refresh");
            refresh.Invoke(suitView, null);
        }

        private static void SeedPreviewFigure()
        {
            IReadOnlyList<int> clothes = FashionConfigs.GetFashionIds(1);
            IReadOnlyList<int> heads = FashionConfigs.GetFashionIds(3);
            int clotheFashionId = clothes[Mathf.Min(1, clothes.Count - 1)];
            int headFashionId = heads[Mathf.Min(1, heads.Count - 1)];
            int clotheModelId = FashionConfigs.GetModelRow(1, clotheFashionId, 1, 1, 0)?.ModelId ?? 0;
            int headModelId = FashionConfigs.GetModelRow(3, headFashionId, 1, 1, 0)?.ModelId ?? 0;
            var figure = new FigureProto { name = "验收角色", career = 1, sex = 1, level = 999, turn = 20 };
            figure.Raw["level_model_list"] = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { ["part_pos"] = 1, ["level_model_id"] = clotheModelId },
                new Dictionary<string, object> { ["part_pos"] = 2, ["level_model_id"] = 1101 },
                new Dictionary<string, object> { ["part_pos"] = 3, ["level_model_id"] = headModelId },
            };
            figure.Raw["fashion_model_list"] = new List<Dictionary<string, object>>();
            figure.Raw["figure_list"] = new List<Dictionary<string, object>>();
            RoleModel.Instance.Figure = figure;
        }

        private static T FindActive<T>() where T : Component
        {
            foreach (T value in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (value != null && value.gameObject.activeInHierarchy) return value;
            return null;
        }

        private static bool Click(Component target, Camera camera, GraphicRaycaster raycaster, EventSystem eventSystem)
        {
            if (target == null || camera == null || raycaster == null || eventSystem == null
                || !target.gameObject.activeInHierarchy) return false;
            Graphic surface = target as Graphic ?? target.GetComponent<Graphic>() ?? target.GetComponentInChildren<Graphic>(true);
            if (surface == null || !surface.enabled || !surface.raycastTarget) return false;
            Canvas.ForceUpdateCanvases();
            if (surface.depth < 0)
            {
                surface.enabled = false;
                surface.enabled = true;
                surface.SetAllDirty();
                Canvas.ForceUpdateCanvases();
                camera.Render();
            }
            RectTransform rect = surface.rectTransform;
            var pointer = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center)),
            };
            var hits = new List<RaycastResult>();
            Vector2 center = pointer.position;
            Vector2[] probes =
            {
                Vector2.zero,
                new Vector2(-24f, 0f), new Vector2(24f, 0f),
                new Vector2(0f, -24f), new Vector2(0f, 24f),
                new Vector2(-24f, -24f), new Vector2(24f, -24f),
                new Vector2(-24f, 24f), new Vector2(24f, 24f),
            };
            foreach (Vector2 probe in probes)
            {
                pointer.position = center + probe;
                hits.Clear();
                raycaster.Raycast(pointer, hits);
                foreach (RaycastResult hit in hits)
                {
                    if (hit.gameObject != surface.gameObject
                        && !hit.gameObject.transform.IsChildOf(target.transform)) continue;
                    ExecuteEvents.ExecuteHierarchy<IPointerClickHandler>(hit.gameObject, pointer,
                        ExecuteEvents.pointerClickHandler);
                    return true;
                }
            }
            pointer.position = center;
            Debug.LogError("CLIVERIFY setting-fashion-current raycast miss target=" + target.name
                + " point=" + pointer.position + " hits=" + string.Join(",", hits.Select(x => x.gameObject.name)));
            Debug.LogError("CLIVERIFY setting-fashion-current raycast detail surface=" + surface.name
                + " depth=" + surface.depth + " cull=" + surface.canvasRenderer.cull
                + " contains=" + RectTransformUtility.RectangleContainsScreenPoint(rect, pointer.position, camera)
                + " selfRaycast=" + surface.Raycast(pointer.position, camera)
                + " hierarchy=" + GetHierarchy(surface.transform));
            return false;
        }

        private static string GetHierarchy(Transform value)
        {
            var names = new List<string>();
            for (Transform current = value; current != null; current = current.parent) names.Add(current.name);
            return string.Join("/", names);
        }

        private static bool HasUniqueItemSurface(DressItem item)
        {
            Graphic surface = item?.ClickSurface;
            if (surface == null || !surface.enabled || !surface.raycastTarget) return false;
            return item.GetComponentsInChildren<Graphic>(true).Count(graphic => graphic.raycastTarget) == 1;
        }

        private static string TabText(TabButtonTwoSkin tab)
        {
            if (tab == null) return "";
            TMP_Text text = tab.transform.Find("labelDisplay")?.GetComponent<TMP_Text>();
            return text?.text ?? "";
        }

        private static HashSet<string> SnapshotResources()
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string root in ResourceRoots)
            {
                if (!Directory.Exists(root)) continue;
                foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                    result.Add(Path.GetFullPath(file));
            }
            return result;
        }

        private static void ResetSettingFlow()
        {
            typeof(SettingFlow).GetMethod("Reset", StaticNonPublic)?.Invoke(null, null);
        }
    }
}
