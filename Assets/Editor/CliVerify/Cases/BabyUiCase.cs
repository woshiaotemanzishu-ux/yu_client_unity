using System;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Editor.UiCreator.Baby;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Res;
using Shenxiao.Generated.UI.Baby;
using Shenxiao.Module.Core.Baby;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>Baby UI 静态基线：回填后实例化两个源 prefab，验收窗口、Item Bind 与嵌套模板解析。</summary>
    public static class BabyUiCase
    {
        private const string ModulePath = "Assets/Prefabs/UI/Baby/BabyModule.prefab";
        private const string PropItemPath = "Assets/Prefabs/UI/Baby/BabyPropItem.prefab";
        private const string FramePath = "Assets/Prefabs/UI/Common/BaseWindowSkin.prefab";
        private const BindingFlags BindFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;

        public static Task<int> Run()
        {
            bool editorPreferFallbackBefore = ResManager.EditorPreferFallback;
            try
            {
                ResManager.EditorPreferFallback = true;
                bool likeStatic = BabyBindUpgrader.VerifyLikeStatic();
                _ = BabyRaiseConfigs.EnsureLoaded();
                _ = BabyFigureConfigs.EnsureLoaded();
                _ = BabyFigureStarConfigs.EnsureLoaded();
                _ = BabyValueConfigs.EnsureLoaded();
                _ = BabyStageConfigs.EnsureLoaded();
                _ = BabyPraiseConfigs.EnsureLoaded();
                _ = Shenxiao.Module.Core.Common.GoodsModel.EnsureLoaded();
                BabyFigureConfigs.BabyFigureCfg figureCfg = BabyFigureConfigs.Get(1);
                BabyFigureStarConfigs.BabyFigureStarCfg starCfg = BabyFigureStarConfigs.Get(1, 2);
                bool config = BabyRaiseConfigs.IsLoaded && BabyRaiseConfigs.Get(1) != null
                    && figureCfg != null && figureCfg.ResourceId == "1011"
                    && BabyFigureConfigs.All.Count == 9
                    && figureCfg.Costs.Count > 0 && figureCfg.Costs[0].TypeId == 68010001 && figureCfg.Costs[0].Num == 30
                    && starCfg != null && starCfg.Costs.Count > 0
                    && starCfg.Costs[0].TypeId == 68010001 && starCfg.Costs[0].Num == 25
                    && BabyValueConfigs.IsLoaded && BabyValueConfigs.StageRaiseLevel == 2
                    && BabyValueConfigs.StageMaterials.Count > 0 && BabyValueConfigs.StageMaterials[0].ItemId == 38040041
                    && BabyValueConfigs.StageMaterials[0].ExpPerItem == 10
                    && BabyStageConfigs.GetNext(1, 1) != null && BabyStageConfigs.GetNext(1, 1).ExpCon == 13
                    && BabyPraiseConfigs.IsLoaded && BabyPraiseConfigs.All.Count == 6
                    && BabyPraiseConfigs.GetByRank(1) != null && BabyPraiseConfigs.GetByRank(1).Rank1 == 1
                    && BabyPraiseConfigs.GetByRank(2) != null && BabyPraiseConfigs.GetByRank(2).Rank1 == 2
                    && BabyPraiseConfigs.GetByRank(100) != null && BabyPraiseConfigs.GetByRank(100).Rank2 == 100
                    && BabyPraiseConfigs.GetByRank(0) == null && BabyPraiseConfigs.GetByRank(101) == null
                    && BabyPraiseConfigs.All[0].Rewards.Count > 0
                    && BabyPraiseConfigs.All[0].Rewards[0].TypeId == 38040043 && BabyPraiseConfigs.All[0].Rewards[0].Num == 1
                    && BabyPraiseConfigs.All[5].Rewards.Count > 0
                    && BabyPraiseConfigs.All[5].Rewards[0].TypeId == 38040041 && BabyPraiseConfigs.All[5].Rewards[0].Num == 1;
                bool upgraded = BabyBindUpgrader.Generate();
                bool prefab = upgraded && VerifyInstances();
                bool pass = config && likeStatic && upgraded && prefab;
                Debug.Log("CLIVERIFY babyui VERDICT config=" + config + " likeStatic=" + likeStatic + " upgraded=" + upgraded + " prefab=" + prefab + " pass=" + pass);
                return Task.FromResult(pass ? 0 : 3);
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY babyui EXCEPTION " + e);
                return Task.FromResult(3);
            }
            finally
            {
                ResManager.EditorPreferFallback = editorPreferFallbackBefore;
                BabyModel.Instance.Reset();
                Shenxiao.Module.Core.Bag.BagModel.Instance.Clear();
            }
        }

        public static void RunBatch()
        {
            _ = RunBatchAsync();
        }

        private static async Task RunBatchAsync()
        {
            int code = await Run();
            Debug.Log("CLIVERIFY babyui EXIT " + code);
            EditorApplication.Exit(code);
        }

        private static bool VerifyInstances()
        {
            GameObject moduleAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModulePath);
            GameObject propItemAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PropItemPath);
            if (moduleAsset == null || propItemAsset == null) return false;

            GameObject module = UnityEngine.Object.Instantiate(moduleAsset);
            GameObject propItem = UnityEngine.Object.Instantiate(propItemAsset);
            FieldInfo interceptField = typeof(BabyController).GetField("s_outboundIntercept", BindingFlags.Static | BindingFlags.NonPublic);
            object oldIntercept = interceptField != null ? interceptField.GetValue(null) : null;
            var powerFrames = new System.Collections.Generic.List<byte[]>();
            try
            {
                if (interceptField == null) return false;
                interceptField.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    if (IsProtocol(frame, Proto.BABY_FIGURE_POWER)) powerFrames.Add(frame);
                    return true;
                }));
                Shenxiao.Module.Core.Bag.BagModel.Instance.UpdateNum(1, 68010001, 35);
                Shenxiao.Module.Core.Bag.BagModel.Instance.UpdateNum(2, 38040041, 1);
                bool pages = Check<GestateBabyViewBind>(module) && Check<BabyFamilyViewBind>(module)
                    && Check<BabyCultivateViewBind>(module) && Check<BabyChangedViewBind>(module)
                    && Check<BabyIllusionViewBind>(module);
                bool businessViews = module.GetComponentInChildren<GestateBabyView>(true) != null
                    && module.GetComponentInChildren<BabyFamilyView>(true) != null
                    && module.GetComponentInChildren<BabyCultivateView>(true) != null
                    && module.GetComponentInChildren<BabyIllusionView>(true) != null
                    && propItem.GetComponentInChildren<BabyPropItem>(true) != null;
                BabyPropItem propBusiness = propItem.GetComponentInChildren<BabyPropItem>(true);
                bool propDisplay = propBusiness != null;
                if (propDisplay)
                {
                    propBusiness.SetData(2, 100, true, 150);
                    propDisplay = !string.IsNullOrEmpty(propBusiness.nameLb.text)
                        && propBusiness.nextLb.text.Contains("+50") && propBusiness.arrow.gameObject.activeSelf;
                }
                BabyModel model = BabyModel.Instance;
                model.ApplyBasic(new BabyBasicInfo { BabyName = "baby" });
                BabyRaiseInfo raise = new BabyRaiseInfo { RaiseLevel = 7, RaiseExp = 11 };
                raise.TaskList.Add(new BabyTaskInfo { TaskId = 1, FinishNum = 0, FinishState = 0 });
                raise.TaskList.Add(new BabyTaskInfo { TaskId = 2, FinishNum = 3, FinishState = 1 });
                raise.TaskList.Add(new BabyTaskInfo { TaskId = 3, FinishNum = 2, FinishState = 2 });
                model.ApplyRaise(raise);
                model.ApplyStage(new BabyStageInfo { Stage = 1, StageLevel = 1, StageExp = 3 });
                BabyCultivateView cultivateView = module.GetComponentInChildren<BabyCultivateView>(true);
                bool display = cultivateView != null;
                if (display)
                {
                    cultivateView.gameObject.SetActive(true);
                    cultivateView.Show();
                    display = cultivateView.babyName.text == "baby" && cultivateView.lvLb.text == "7"
                        && cultivateView.lvExpLb.text == "11" && cultivateView.stageExpLb.text == "3"
                        && cultivateView.lvtaskRed.gameObject.activeSelf
                        && !model.BabyLikeRed && !cultivateView.likeRed.gameObject.activeSelf
                        && cultivateView.stageRed.gameObject.activeSelf && cultivateView.stageTabRed.gameObject.activeSelf;
                    model.ApplyPraisePush(new BabyPraisePush
                    {
                        PraiserId = Shenxiao.Module.Core.Role.RoleModel.Instance.RoleId ^ 1L
                    });
                    Shenxiao.Framework.Event.EventDispatcher.Emit(Shenxiao.Framework.Event.GlobalEvent.EVT_BABY_UPDATE, Proto.BABY_PRAISE_PUSH);
                    bool likeRedRefresh = model.BabyLikeRed && cultivateView.likeRed.gameObject.activeSelf;
                    model.ApplyPraiseRecords(new BabyPraiseRecordsInfo());
                    Shenxiao.Framework.Event.EventDispatcher.Emit(Shenxiao.Framework.Event.GlobalEvent.EVT_BABY_UPDATE, Proto.BABY_LIKE_RECORDS);
                    likeRedRefresh = likeRedRefresh && !model.BabyLikeRed && !cultivateView.likeRed.gameObject.activeSelf;
                    display = display && likeRedRefresh;
                    Shenxiao.Module.Core.Bag.BagModel.Instance.UpdateNum(2, 38040041, 0);
                    Shenxiao.Framework.Event.EventDispatcher.Emit(Shenxiao.Framework.Event.GlobalEvent.EVT_BAG_UPDATE);
                    bool stageRedRefresh = !cultivateView.stageRed.gameObject.activeSelf && !cultivateView.stageTabRed.gameObject.activeSelf;
                    Shenxiao.Module.Core.Bag.BagModel.Instance.UpdateNum(2, 38040041, 1);
                    Shenxiao.Framework.Event.EventDispatcher.Emit(Shenxiao.Framework.Event.GlobalEvent.EVT_BAG_UPDATE);
                    stageRedRefresh = stageRedRefresh && cultivateView.stageRed.gameObject.activeSelf && cultivateView.stageTabRed.gameObject.activeSelf;
                    raise.RaiseLevel = 1;
                    Shenxiao.Framework.Event.EventDispatcher.Emit(Shenxiao.Framework.Event.GlobalEvent.EVT_BABY_UPDATE, Proto.BABY_RAISE_INFO);
                    stageRedRefresh = stageRedRefresh && !cultivateView.stageRed.gameObject.activeSelf && !cultivateView.stageTabRed.gameObject.activeSelf;
                    raise.RaiseLevel = 7;
                    Shenxiao.Framework.Event.EventDispatcher.Emit(Shenxiao.Framework.Event.GlobalEvent.EVT_BABY_UPDATE, Proto.BABY_RAISE_INFO);
                    display = display && stageRedRefresh && cultivateView.stageRed.gameObject.activeSelf && cultivateView.stageTabRed.gameObject.activeSelf;
                    bool taskRedRefresh = model.TryApplyTaskProgress(2, 3, 2);
                    Shenxiao.Framework.Event.EventDispatcher.Emit(Shenxiao.Framework.Event.GlobalEvent.EVT_BABY_UPDATE, Proto.BABY_TASK_UPDATE);
                    taskRedRefresh = taskRedRefresh && !cultivateView.lvtaskRed.gameObject.activeSelf;
                    taskRedRefresh = taskRedRefresh && model.TryApplyTaskProgress(2, 3, 1);
                    Shenxiao.Framework.Event.EventDispatcher.Emit(Shenxiao.Framework.Event.GlobalEvent.EVT_BABY_UPDATE, Proto.BABY_TASK_UPDATE);
                    display = display && taskRedRefresh && cultivateView.lvtaskRed.gameObject.activeSelf;
                    display = display && VerifyTasks(cultivateView);
                    UnityEngine.UI.Button lvButton = cultivateView.lvBtnGp.GetComponent<UnityEngine.UI.Button>();
                    UnityEngine.UI.Button stageButton = cultivateView.stageBtnGp.GetComponent<UnityEngine.UI.Button>();
                    UnityEngine.UI.Button upButton = cultivateView.upBtn.GetComponent<UnityEngine.UI.Button>();
                    display = display && lvButton != null && stageButton != null && upButton != null;
                    if (display)
                    {
                        stageButton.onClick.Invoke();
                        display = cultivateView.stageGp.gameObject.activeSelf && !cultivateView.lvGp.gameObject.activeSelf;
                        lvButton.onClick.Invoke();
                        display = display && cultivateView.lvGp.gameObject.activeSelf && !cultivateView.stageGp.gameObject.activeSelf;
                    }
                    cultivateView.Hide();
                }
                BabyFamilyView familyView = module.GetComponentInChildren<BabyFamilyView>(true);
                BabyFamilyInfo family = new BabyFamilyInfo();
                family.InfoList.Add(new BabyFamilyEntry { ActiveTime = 1, BabyName = "family-a", RaiseLevel = 8, Stage = 5, StageLevel = 2, BabyPower = 600 });
                family.InfoList.Add(new BabyFamilyEntry { ActiveTime = 2, BabyName = "family-b", RaiseLevel = 9, Stage = 6, StageLevel = 3, BabyPower = 700 });
                model.ApplyFamily(family);
                bool familyDisplay = familyView != null;
                if (familyDisplay)
                {
                    familyView.gameObject.SetActive(true);
                    familyView.Show();
                    familyDisplay = familyView.scroller1.gameObject.activeSelf && familyView.scroller2.gameObject.activeSelf
                        && familyView.value1.text.Contains("family-a") && familyView.value1.text.Contains("8")
                        && familyView.value2.text.Contains("family-b") && familyView.value2.text.Contains("700");
                    familyView.Hide();
                }
                BabyIllusionView illusionView = module.GetComponentInChildren<BabyIllusionView>(true);
                model.ApplyBasic(new BabyBasicInfo { BabyId = 2 });
                model.ApplyFigures(new BabyFigureInfo());
                model.MergeFigure(1, 2, 1000, 1300);
                model.MergeFigure(2, 3, 2000, 2200);
                bool tabRedDisplay = VerifyIllusionTabRed();
                BabyFigureConfigs.BabyFigureCfg illusionCfg = BabyFigureConfigs.Get(1);
                bool illusionDisplay = illusionView != null && illusionCfg != null;
                if (illusionDisplay)
                {
                    illusionView.gameObject.SetActive(true);
                    illusionView.Show();
                    BabyIlluItem[] illusionItems = illusionView.illuGp.GetComponentsInChildren<BabyIlluItem>(true);
                    Shenxiao.Module.Core.Common.BaseAwardItem[] stageCostItems =
                        illusionView.stageitemGp.GetComponentsInChildren<Shenxiao.Module.Core.Common.BaseAwardItem>(true);
                    int inactiveCount = 0;
                    int loadedIconCount = 0;
                    for (int i = 0; i < illusionItems.Length; i++)
                    {
                        if (illusionItems[i].unActive.gameObject.activeSelf) inactiveCount++;
                        if (illusionItems[i].resImg.sprite != null) loadedIconCount++;
                    }
                    bool listDisplay = illusionView.illuGp.childCount == 9
                        && illusionView.babyName.text == illusionCfg.BabyName
                        && illusionItems.Length == 9 && inactiveCount >= 7 && loadedIconCount == 9
                        && !illusionView.selectedImg.gameObject.activeSelf
                        && !illusionView.unActive.gameObject.activeSelf
                        && illusionView.useGp.gameObject.activeSelf
                        && illusionView.activeBtn.GetComponent<UnityEngine.UI.Button>() != null
                        && illusionView.stageBtn.GetComponent<UnityEngine.UI.Button>() != null
                        && !illusionView.activeGp.gameObject.activeSelf
                        && illusionView.stageGp.gameObject.activeSelf
                        && !illusionView.maxImg.gameObject.activeSelf
                        && stageCostItems.Length == 1 && stageCostItems[0].gameObject.activeSelf
                        && stageCostItems[0].num_text.text == "30"
                        && illusionView.stageLb.text.Contains("35/30") && illusionView.stageLb.text.Contains("#0f9f00");
                    illusionDisplay = listDisplay;
                    BabyPropItem[] activeProps = illusionView.propGp.GetComponentsInChildren<BabyPropItem>(true);
                    bool activePropsDisplay = CountVisible(activeProps) == 4 && activeProps[0].nextLb.text.Contains("+30000");
                    illusionDisplay = illusionDisplay && activePropsDisplay;
                    Shenxiao.Module.Core.Common.FightingShowSmallItem[] fightingItems = illusionView.fight.GetComponentsInChildren<Shenxiao.Module.Core.Common.FightingShowSmallItem>(true);
                    bool activeFightingDisplay = fightingItems.Length == 1 && fightingItems[0]._lb_fighting.text == "1000"
                        && fightingItems[0]._box_up.gameObject.activeSelf && powerFrames.Count == 0;
                    illusionDisplay = illusionDisplay && activeFightingDisplay;
                    BabyIlluItem redIllu = illusionView.illuGp.Find("BabyIlluItem_1").GetComponent<BabyIlluItem>();
                    bool redDisplay = redIllu != null && redIllu.red_dot.gameObject.activeSelf
                        && illusionView.activeRed.gameObject.activeSelf && illusionView.stageRed.gameObject.activeSelf
                        && illusionView.babyName.text == illusionCfg.BabyName;
                    Shenxiao.Module.Core.Bag.BagModel.Instance.UpdateNum(1, 68010001, 0);
                    Shenxiao.Framework.Event.EventDispatcher.Emit(Shenxiao.Framework.Event.GlobalEvent.EVT_BAG_UPDATE);
                    redIllu = illusionView.illuGp.Find("BabyIlluItem_1").GetComponent<BabyIlluItem>();
                    redDisplay = redDisplay && redIllu != null && !redIllu.red_dot.gameObject.activeSelf
                        && !illusionView.activeRed.gameObject.activeSelf && !illusionView.stageRed.gameObject.activeSelf
                        && illusionView.babyName.text == illusionCfg.BabyName;
                    Shenxiao.Module.Core.Bag.BagModel.Instance.UpdateNum(1, 68010001, 35);
                    Shenxiao.Framework.Event.EventDispatcher.Emit(Shenxiao.Framework.Event.GlobalEvent.EVT_BAG_UPDATE);
                    redIllu = illusionView.illuGp.Find("BabyIlluItem_1").GetComponent<BabyIlluItem>();
                    redDisplay = redDisplay && redIllu != null && redIllu.red_dot.gameObject.activeSelf
                        && illusionView.activeRed.gameObject.activeSelf && illusionView.stageRed.gameObject.activeSelf
                        && illusionView.babyName.text == illusionCfg.BabyName;
                    illusionDisplay = illusionDisplay && redDisplay;
                    UnityEngine.UI.Button leftButton = illusionView.leftBtn.GetComponent<UnityEngine.UI.Button>();
                    UnityEngine.UI.Button rightButton = illusionView.rightBtn.GetComponent<UnityEngine.UI.Button>();
                    if (illusionDisplay && leftButton != null && rightButton != null)
                    {
                        leftButton.onClick.Invoke();
                        illusionDisplay = illusionView.babyName.text == illusionCfg.BabyName && powerFrames.Count == 0;
                        rightButton.onClick.Invoke();
                        BabyFigureConfigs.BabyFigureCfg secondCfg = BabyFigureConfigs.Get(2);
                        illusionDisplay = illusionDisplay && secondCfg != null && illusionView.babyName.text == secondCfg.BabyName && powerFrames.Count == 0;
                        leftButton.onClick.Invoke();
                        illusionDisplay = illusionDisplay && illusionView.babyName.text == illusionCfg.BabyName && powerFrames.Count == 0;
                    }
                    else illusionDisplay = false;
                    BabyIlluItem activeIllu = illusionView.illuGp.Find("BabyIlluItem_1").GetComponent<BabyIlluItem>();
                    bool activeStarsDisplay = activeIllu != null && activeIllu.star_group.childCount == 5
                        && activeIllu.star_shadow_group.childCount == 5 && CountActiveChildren(activeIllu.star_group) == 2
                        && AllSprites(activeIllu.star_group) && AllSprites(activeIllu.star_shadow_group);
                    string expectedFrame = "com_goods_plate_" + Shenxiao.Module.Core.Common.GoodsModel.GetDisplayColor(68010001);
                    bool activeFrameDisplay = activeIllu != null && activeIllu.box.sprite != null
                        && activeIllu.box.sprite.name == expectedFrame;
                    illusionDisplay = illusionDisplay && activeStarsDisplay && activeFrameDisplay;
                    Debug.Log("CLIVERIFY babyui illusion active list=" + listDisplay + " props=" + activePropsDisplay
                        + " fighting=" + activeFightingDisplay + " red=" + redDisplay + " stars=" + activeStarsDisplay + " frame=" + activeFrameDisplay);
                    if (illusionDisplay)
                    {
                        Transform third = illusionView.illuGp.Find("BabyIlluItem_3");
                        UnityEngine.UI.Button thirdButton = third != null ? third.GetComponent<BabyIlluItemBind>().clickGp.GetComponent<UnityEngine.UI.Button>() : null;
                        thirdButton?.onClick.Invoke();
                        BabyFigureConfigs.BabyFigureCfg thirdCfg = BabyFigureConfigs.Get(3);
                        BabyIlluItem inactiveIllu = illusionView.illuGp.Find("BabyIlluItem_3").GetComponent<BabyIlluItem>();
                        Shenxiao.Module.Core.Common.BaseAwardItem[] activeCostItems =
                            illusionView.activeitemGp.GetComponentsInChildren<Shenxiao.Module.Core.Common.BaseAwardItem>(true);
                        illusionDisplay = thirdCfg != null && illusionView.babyName.text == thirdCfg.BabyName
                            && illusionView.activeGp.gameObject.activeSelf && !illusionView.useGp.gameObject.activeSelf
                            && illusionView.unActive.gameObject.activeSelf && !illusionView.selectedImg.gameObject.activeSelf
                            && !illusionView.stageGp.gameObject.activeSelf && !illusionView.maxImg.gameObject.activeSelf
                            && activeCostItems.Length == 1 && activeCostItems[0].gameObject.activeSelf
                            && activeCostItems[0].num_text.text == "30"
                            && illusionView.activeLb.text.Contains("0/30") && illusionView.activeLb.text.Contains("#ff4f50")
                            && stageCostItems.Length == 1 && !stageCostItems[0].gameObject.activeSelf;
                        BabyPropItem[] inactiveProps = illusionView.propGp.GetComponentsInChildren<BabyPropItem>(true);
                        illusionDisplay = illusionDisplay && CountVisible(inactiveProps) == 4 && inactiveProps[0].nextLb.text.Contains("+70000");
                        illusionDisplay = illusionDisplay && inactiveIllu != null && !inactiveIllu.star_group.gameObject.activeSelf
                            && !inactiveIllu.star_shadow_group.gameObject.activeSelf;
                        bool requestPower = powerFrames.Count == 1 && IsPowerFrame(powerFrames[0], 3);
                        model.ApplyFigurePowerResult(new BabyFigurePowerResult { BabyId = 3, BabyStar = 0, Power = 700, NextPower = 900 });
                        Shenxiao.Framework.Event.EventDispatcher.Emit(Shenxiao.Framework.Event.GlobalEvent.EVT_BABY_UPDATE, Proto.BABY_FIGURE_POWER);
                        fightingItems = illusionView.fight.GetComponentsInChildren<Shenxiao.Module.Core.Common.FightingShowSmallItem>(true);
                        illusionDisplay = illusionDisplay && requestPower && powerFrames.Count == 1 && fightingItems.Length == 1
                            && fightingItems[0]._lb_fighting.text == "700+" && fightingItems[0]._box_up.gameObject.activeSelf;
                    }
                    illusionView.Hide();
                }
                model.Reset();
                bool items = Check<BabyCulTaskItemBind>(module) && Check<BabyCulTaskItem>(module) && Check<BabyIlluItemBind>(module) && Check<BabyIlluItem>(module)
                    && Check<BabyPropItemBind>(propItem) && Check<BabyPropItem>(propItem);
                BabyCultivateViewBind cultivate = module.GetComponentInChildren<BabyCultivateViewBind>(true);
                BabyIllusionViewBind illusion = module.GetComponentInChildren<BabyIllusionViewBind>(true);
                bool templates = Has<BabyCulTaskItemBind>(cultivate != null ? cultivate._tpl_BabyCulTaskItem : null)
                    && Has<BabyPropItemBind>(cultivate != null ? cultivate._tpl_BabyPropItem : null)
                    && Has<BabyPropItem>(cultivate != null ? cultivate._tpl_BabyPropItem : null)
                    && Has<BabyIlluItemBind>(illusion != null ? illusion._tpl_BabyIlluItem : null)
                    && Has<BabyIlluItem>(illusion != null ? illusion._tpl_BabyIlluItem : null)
                    && Has<BabyPropItemBind>(illusion != null ? illusion._tpl_BabyPropItem : null)
                    && Has<BabyPropItem>(illusion != null ? illusion._tpl_BabyPropItem : null);
                Debug.Log("CLIVERIFY babyui pages=" + pages + " businessViews=" + businessViews + " display=" + display + " familyDisplay=" + familyDisplay + " illusionDisplay=" + illusionDisplay + " items=" + items + " templates=" + templates);
                return pages && businessViews && propDisplay && display && familyDisplay && illusionDisplay && tabRedDisplay && items && templates;
            }
            finally
            {
                BabyModel.Instance.ApplyPraiseRecords(new BabyPraiseRecordsInfo());
                BabyModel.Instance.Reset();
                Shenxiao.Module.Core.Bag.BagModel.Instance.Clear();
                if (interceptField != null) interceptField.SetValue(null, oldIntercept);
                UnityEngine.Object.DestroyImmediate(module);
                UnityEngine.Object.DestroyImmediate(propItem);
            }
        }

        private static bool VerifyIllusionTabRed()
        {
            GameObject frameAsset = AssetDatabase.LoadAssetAtPath<GameObject>(FramePath);
            if (frameAsset == null) return false;
            GameObject frame = UnityEngine.Object.Instantiate(frameAsset);
            FieldInfo windowField = typeof(BabyFlow).GetField("_window", BindingFlags.Static | BindingFlags.NonPublic);
            FieldInfo configuredField = typeof(BabyFlow).GetField("_windowConfigured", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo bagUpdate = typeof(BabyFlow).GetMethod("OnBagUpdate", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo cultivateRefresh = typeof(BabyFlow).GetMethod("RefreshCultivateTabRed", BindingFlags.Static | BindingFlags.NonPublic);
            object oldWindow = windowField != null ? windowField.GetValue(null) : null;
            object oldConfigured = configuredField != null ? configuredField.GetValue(null) : null;
            try
            {
                Shenxiao.Module.Core.Common.BaseWindowSkinView window = frame.GetComponentInChildren<Shenxiao.Module.Core.Common.BaseWindowSkinView>(true);
                if (window == null || windowField == null || configuredField == null || bagUpdate == null || cultivateRefresh == null) return false;
                frame.SetActive(true);
                window.Show();
                window.ConfigureShared(3, null, null, 0, index => index >= 0 && index < 3);
                windowField.SetValue(null, window);
                configuredField.SetValue(null, true);

                Shenxiao.Module.Core.Common.TabButtonTwoSkin[] allTabs =
                    frame.GetComponentsInChildren<Shenxiao.Module.Core.Common.TabButtonTwoSkin>(true);
                Shenxiao.Module.Core.Common.TabButtonTwoSkin tab0 = null;
                Shenxiao.Module.Core.Common.TabButtonTwoSkin tab2 = null;
                int activeIndex = 0;
                for (int i = 0; i < allTabs.Length; i++)
                {
                    if (!allTabs[i].gameObject.activeSelf) continue;
                    if (activeIndex == 0) tab0 = allTabs[i];
                    if (activeIndex++ == 2) { tab2 = allTabs[i]; break; }
                }
                if (tab0 == null || tab2 == null || tab0.redDisplay == null || tab2.redDisplay == null) return false;

                cultivateRefresh.Invoke(null, null);
                bool cultivateVisible = tab0.redDisplay.gameObject.activeSelf;
                bool taskUpdated = BabyModel.Instance.TryApplyTaskProgress(2, 3, 2);
                cultivateRefresh.Invoke(null, null);
                bool cultivateStageKeepsVisible = taskUpdated && tab0.redDisplay.gameObject.activeSelf;
                Shenxiao.Module.Core.Bag.BagModel.Instance.UpdateNum(2, 38040041, 0);
                bagUpdate.Invoke(null, null);
                bool cultivateHidden = !tab0.redDisplay.gameObject.activeSelf;
                taskUpdated = taskUpdated && BabyModel.Instance.TryApplyTaskProgress(2, 3, 1);
                cultivateRefresh.Invoke(null, null);
                bool cultivateRestored = taskUpdated && tab0.redDisplay.gameObject.activeSelf;
                Shenxiao.Module.Core.Bag.BagModel.Instance.UpdateNum(2, 38040041, 1);
                bagUpdate.Invoke(null, null);
                bool cultivateStillVisible = tab0.redDisplay.gameObject.activeSelf;

                BabyModel.Instance.TryApplyTaskProgress(2, 3, 2);
                Shenxiao.Module.Core.Bag.BagModel.Instance.UpdateNum(2, 38040041, 0);
                cultivateRefresh.Invoke(null, null);
                bool cultivateLikeStartsHidden = !tab0.redDisplay.gameObject.activeSelf;
                BabyModel.Instance.ApplyPraisePush(new BabyPraisePush
                {
                    PraiserId = Shenxiao.Module.Core.Role.RoleModel.Instance.RoleId ^ 1L
                });
                cultivateRefresh.Invoke(null, null);
                bool cultivateLikeShows = tab0.redDisplay.gameObject.activeSelf;
                BabyModel.Instance.ApplyPraiseRecords(new BabyPraiseRecordsInfo());
                cultivateRefresh.Invoke(null, null);
                bool cultivateLikeClears = !tab0.redDisplay.gameObject.activeSelf;
                BabyModel.Instance.TryApplyTaskProgress(2, 3, 1);
                Shenxiao.Module.Core.Bag.BagModel.Instance.UpdateNum(2, 38040041, 1);
                cultivateRefresh.Invoke(null, null);
                bool cultivateTaskRestores = tab0.redDisplay.gameObject.activeSelf;

                bagUpdate.Invoke(null, null);
                bool visible = tab2.redDisplay.gameObject.activeSelf;
                Shenxiao.Module.Core.Bag.BagModel.Instance.UpdateNum(1, 68010001, 0);
                bagUpdate.Invoke(null, null);
                bool hidden = !tab2.redDisplay.gameObject.activeSelf;
                Shenxiao.Module.Core.Bag.BagModel.Instance.UpdateNum(1, 68010001, 35);
                bagUpdate.Invoke(null, null);
                return cultivateVisible && cultivateStageKeepsVisible && cultivateHidden && cultivateRestored && cultivateStillVisible
                    && cultivateLikeStartsHidden && cultivateLikeShows && cultivateLikeClears && cultivateTaskRestores
                    && visible && hidden && tab2.redDisplay.gameObject.activeSelf;
            }
            finally
            {
                if (windowField != null) windowField.SetValue(null, oldWindow);
                if (configuredField != null) configuredField.SetValue(null, oldConfigured);
                BabyModel.Instance.TryApplyTaskProgress(2, 3, 1);
                BabyModel.Instance.ApplyPraiseRecords(new BabyPraiseRecordsInfo());
                Shenxiao.Module.Core.Bag.BagModel.Instance.UpdateNum(2, 38040041, 1);
                Shenxiao.Module.Core.Bag.BagModel.Instance.UpdateNum(1, 68010001, 35);
                UnityEngine.Object.DestroyImmediate(frame);
            }
        }

        private static bool Check<T>(GameObject root) where T : Component
        {
            T bind = root.GetComponentInChildren<T>(true);
            if (bind == null) return false;
            foreach (FieldInfo field in typeof(T).GetFields(BindFields))
                if (field.GetValue(bind) == null) return false;
            return true;
        }

        private static bool VerifyTasks(BabyCultivateView view)
        {
            BabyCulTaskItem[] all = view.taskGp.GetComponentsInChildren<BabyCulTaskItem>(true);
            var tasks = new System.Collections.Generic.List<BabyCulTaskItem>();
            for (int i = 0; i < all.Length; i++) if (all[i].TaskId > 0) tasks.Add(all[i]);
            if (tasks.Count != 3 || tasks[0].TaskId != 2 || tasks[1].TaskId != 1 || tasks[2].TaskId != 3) return false;
            return tasks[0].getBtn.gameObject.activeSelf && tasks[0].reddot.gameObject.activeSelf
                && !tasks[0].goBtn.gameObject.activeSelf && !tasks[0].finishImg.gameObject.activeSelf
                && tasks[1].goBtn.gameObject.activeSelf && !tasks[1].getBtn.gameObject.activeSelf
                && !tasks[1].finishImg.gameObject.activeSelf && tasks[2].finishImg.gameObject.activeSelf
                && !tasks[2].goBtn.gameObject.activeSelf && !tasks[2].getBtn.gameObject.activeSelf;
        }

        private static bool Has<T>(GameObject template) where T : Component
        {
            return template != null && template.GetComponent<T>() != null;
        }

        private static int CountVisible<T>(T[] items) where T : Component
        {
            int count = 0;
            for (int i = 0; i < items.Length; i++) if (items[i] != null && items[i].gameObject.activeSelf) count++;
            return count;
        }

        private static bool IsPowerFrame(byte[] frame, int babyId)
        {
            if (frame == null || frame.Length != 10 || !IsProtocol(frame, Proto.BABY_FIGURE_POWER)) return false;
            return frame[6] == (byte)(babyId >> 24) && frame[7] == (byte)(babyId >> 16)
                && frame[8] == (byte)(babyId >> 8) && frame[9] == (byte)babyId;
        }

        private static bool IsProtocol(byte[] frame, int protocol)
        {
            return frame != null && frame.Length >= 6 && frame[4] == (byte)(protocol >> 8)
                && frame[5] == (byte)(protocol & 0xff);
        }

        private static int CountActiveChildren(Transform parent)
        {
            int count = 0;
            for (int i = 0; i < parent.childCount; i++) if (parent.GetChild(i).gameObject.activeSelf) count++;
            return count;
        }

        private static bool AllSprites(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
                if (parent.GetChild(i).GetComponent<UnityEngine.UI.Image>()?.sprite == null) return false;
            return true;
        }
    }
}
