using System;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Editor.UiCreator.Baby;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Event;
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
        private const string LikeViewPath = "Assets/Prefabs/UI/Baby/BabyLikeView.prefab";
        private const string BelikeViewPath = "Assets/Prefabs/UI/Baby/BabyBelikeView.prefab";
        private const string EquipFuncViewPath = "Assets/Prefabs/UI/Baby/BabyEquipFuncView.prefab";
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
                bool teaseStatic = BabyBindUpgrader.VerifyTeaseStatic();
                bool equipStatic = BabyBindUpgrader.VerifyEquipStatic();
                bool imprintStatic = BabyBindUpgrader.VerifyImprintStatic();
                Shenxiao.Framework.UI.UIViewAttribute likeAddress = typeof(BabyLikeView).GetCustomAttribute<Shenxiao.Framework.UI.UIViewAttribute>();
                Shenxiao.Framework.UI.UIViewAttribute belikeAddress = typeof(BabyBelikeView).GetCustomAttribute<Shenxiao.Framework.UI.UIViewAttribute>();
                Shenxiao.Framework.UI.UIViewAttribute equipAddress = typeof(BabyEquipFuncView).GetCustomAttribute<Shenxiao.Framework.UI.UIViewAttribute>();
                bool viewAddresses = likeAddress != null && likeAddress.AddrKey == "prefabs/ui/baby/babylikeview"
                    && belikeAddress != null && belikeAddress.AddrKey == "prefabs/ui/baby/babybelikeview"
                    && equipAddress != null && equipAddress.AddrKey == "prefabs/ui/baby/babyequipfuncview";
                _ = BabyRaiseConfigs.EnsureLoaded();
                _ = BabyFigureConfigs.EnsureLoaded();
                _ = BabyFigureStarConfigs.EnsureLoaded();
                _ = BabyValueConfigs.EnsureLoaded();
                _ = BabyStageConfigs.EnsureLoaded();
                _ = BabyPraiseConfigs.EnsureLoaded();
                _ = BabyEquipConfigs.EnsureLoaded();
                _ = BabyEquipUpgradeConfigs.EnsureLoaded();
                _ = BabyEquipEngraveConfigs.EnsureLoaded();
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
                    && BabyPraiseConfigs.All[5].Rewards[0].TypeId == 38040041 && BabyPraiseConfigs.All[5].Rewards[0].Num == 1
                    && BabyEquipConfigs.IsLoaded && BabyEquipConfigs.All.Count == 43
                    && BabyEquipConfigs.Get(65010200) != null && BabyEquipConfigs.Get(65010200).PosId == 1 && BabyEquipConfigs.Get(65010200).Skills.Count == 1 && BabyEquipConfigs.Get(65010200).Skills[0] == 2001001
                    && BabyEquipConfigs.Get(65060601) != null && BabyEquipConfigs.Get(65060601).PosId == 6 && BabyEquipConfigs.Get(65060601).Color == 6
                    && BabyEquipConfigs.CanWear(65010200, 1, 1) && !BabyEquipConfigs.CanWear(65010200, 2, 1) && !BabyEquipConfigs.CanWear(65010200, 1, 0) && !BabyEquipConfigs.CanWear(1, 1, 1)
                    && VerifyEquipUpgradeConfigs() && VerifyEquipEngraveConfigs();
                bool upgraded = BabyBindUpgrader.Generate();
                bool prefab = upgraded && VerifyInstances();
                bool likeRank = likeStatic && VerifyLikeRank();
                bool belike = likeStatic && VerifyBelike();
                bool equip = equipStatic && VerifyEquip();
                bool pass = config && likeStatic && teaseStatic && equipStatic && imprintStatic && viewAddresses && upgraded && prefab && likeRank && belike && equip;
                Debug.Log("CLIVERIFY babyui VERDICT config=" + config + " likeStatic=" + likeStatic + " teaseStatic=" + teaseStatic + " equipStatic=" + equipStatic + " imprintStatic=" + imprintStatic + " addresses=" + viewAddresses + " upgraded=" + upgraded + " prefab=" + prefab + " likeRank=" + likeRank + " belike=" + belike + " equip=" + equip + " pass=" + pass);
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
                    UnityEngine.UI.Button rankButton = cultivateView.rankBtn.GetComponent<UnityEngine.UI.Button>();
                    display = display && lvButton != null && stageButton != null && upButton != null && rankButton != null;
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

        private static bool VerifyLikeRank()
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(LikeViewPath);
            if (asset == null) return false;
            GameObject viewObject = UnityEngine.Object.Instantiate(asset);
            FieldInfo interceptField = typeof(BabyController).GetField("s_outboundIntercept", BindingFlags.Static | BindingFlags.NonPublic);
            object oldIntercept = interceptField != null ? interceptField.GetValue(null) : null;
            try
            {
                BabyLikeView view = viewObject.GetComponent<BabyLikeView>();
                if (view == null || interceptField == null) return false;
                var frames = new System.Collections.Generic.List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                BabyPraiseRankInfo rank = new BabyPraiseRankInfo { RoleId = 2 };
                rank.Entries.Add(new BabyPraiseRankEntry { RoleId = 1, Name = "first", BabyPower = 11, PraiseNum = 21 });
                rank.Entries.Add(new BabyPraiseRankEntry { RoleId = 2, Name = "self", BabyPower = 12, PraiseNum = 22 });
                rank.Entries.Add(new BabyPraiseRankEntry { RoleId = 3, Name = "third", BabyPower = 13, PraiseNum = 23 });
                BabyModel.Instance.ApplyPraiseRank(rank);
                viewObject.SetActive(true);
                view.Show();
                TMPro.TextMeshProUGUI myRank = FindNode(viewObject.transform, "myRank")?.GetComponent<TMPro.TextMeshProUGUI>();
                TMPro.TextMeshProUGUI myLike = FindNode(viewObject.transform, "mylike")?.GetComponent<TMPro.TextMeshProUGUI>();
                bool shown = frames.Count == 1 && IsProtocol(frames[0], Proto.BABY_LIKE_RANK)
                    && myRank != null && myRank.text == "我的排名:2" && myLike != null && myLike.text == "我的赞:22";
                UnityEngine.UI.Button closeButton = FindNode(viewObject.transform, "closeBtn")?.GetComponent<UnityEngine.UI.Button>();
                UnityEngine.UI.Button belikeButton = FindNode(viewObject.transform, "belikeBtn")?.GetComponent<UnityEngine.UI.Button>();
                shown = shown && closeButton != null && belikeButton != null;
                Transform content = view._Scroller1 != null ? view._Scroller1.content : FindNode(viewObject.transform, "Content1");
                BabyLikeItem[] items = content != null ? content.GetComponentsInChildren<BabyLikeItem>(true) : new BabyLikeItem[0];
                TMPro.TextMeshProUGUI firstName = items.Length > 0 ? FindNode(items[0].transform, "nameLb")?.GetComponent<TMPro.TextMeshProUGUI>() : null;
                TMPro.TextMeshProUGUI secondRank = items.Length > 1 ? FindNode(items[1].transform, "rankLb")?.GetComponent<TMPro.TextMeshProUGUI>() : null;
                shown = shown && content != null && items.Length == 3 && firstName != null && firstName.text == "first"
                    && secondRank != null && secondRank.text == "2";
                Transform rewardContent = FindNode(viewObject.transform, "rewardScroller")?.Find("Content");
                BabyLikeReward[] rewards = rewardContent != null ? rewardContent.GetComponentsInChildren<BabyLikeReward>(true) : new BabyLikeReward[0];
                Shenxiao.Module.Core.Common.BaseAwardItem[] firstRewards = rewards.Length > 0
                    ? rewards[0].GetComponentsInChildren<Shenxiao.Module.Core.Common.BaseAwardItem>(true)
                    : new Shenxiao.Module.Core.Common.BaseAwardItem[0];
                int activeRewardCount = 0;
                bool firstRewardCountHidden = false;
                string secondRewardNum = string.Empty;
                for (int i = 0; i < firstRewards.Length; i++)
                {
                    if (!firstRewards[i].gameObject.activeSelf) continue;
                    if (activeRewardCount == 0) firstRewardCountHidden = !firstRewards[i].num_text.gameObject.activeSelf;
                    else if (activeRewardCount == 1) secondRewardNum = firstRewards[i].num_text.text;
                    activeRewardCount++;
                }
                TMPro.TextMeshProUGUI rewardLabel = rewards.Length > 0 ? FindNode(rewards[0].transform, "lb")?.GetComponent<TMPro.TextMeshProUGUI>() : null;
                shown = shown && rewards.Length == 6 && rewardLabel != null && rewardLabel.text == "第1名" && activeRewardCount == 2
                    && firstRewardCountHidden && secondRewardNum == "3";
                BabyModel.Instance.ApplyPraiseRank(new BabyPraiseRankInfo { RoleId = 2 });
                Shenxiao.Framework.Event.EventDispatcher.Emit(Shenxiao.Framework.Event.GlobalEvent.EVT_BABY_UPDATE, Proto.BABY_LIKE_RANK);
                TMPro.TextMeshProUGUI empty = FindNode(viewObject.transform, "noOneLb")?.GetComponent<TMPro.TextMeshProUGUI>();
                int emptyItems = content != null ? content.GetComponentsInChildren<BabyLikeItem>(true).Length : -1;
                bool emptyShown = content != null && empty != null && empty.gameObject.activeSelf && emptyItems == 0;
                Debug.Log("CLIVERIFY babyui likerank frames=" + frames.Count + " shown=" + shown
                    + " items=" + items.Length + " myRank=" + (myRank != null ? myRank.text : "null")
                    + " myLike=" + (myLike != null ? myLike.text : "null") + " emptyShown=" + emptyShown
                    + " emptyItems=" + emptyItems + " emptyChildren=" + (content != null ? content.childCount : -1));
                view.Hide();
                return shown && emptyShown;
            }
            finally
            {
                BabyModel.Instance.Reset();
                if (interceptField != null) interceptField.SetValue(null, oldIntercept);
                UnityEngine.Object.DestroyImmediate(viewObject);
            }
        }

        private static Transform FindNode(Transform root, string name)
        {
            Transform[] nodes = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < nodes.Length; i++) if (nodes[i].name == name) return nodes[i];
            return null;
        }

        private static bool VerifyBelike()
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(BelikeViewPath);
            if (asset == null) return false;
            GameObject viewObject = UnityEngine.Object.Instantiate(asset);
            FieldInfo interceptField = typeof(BabyController).GetField("s_outboundIntercept", BindingFlags.Static | BindingFlags.NonPublic);
            object oldIntercept = interceptField != null ? interceptField.GetValue(null) : null;
            try
            {
                BabyBelikeView view = viewObject.GetComponent<BabyBelikeView>();
                if (view == null || interceptField == null) return false;
                var frames = new System.Collections.Generic.List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                BabyPraiseRecordsInfo records = new BabyPraiseRecordsInfo();
                records.Entries.Add(new BabyPraiseRecordEntry { PraiserId = 11, Name = "pending", IsPraiseBack = false });
                records.Entries.Add(new BabyPraiseRecordEntry { PraiserId = 12, Name = "done", IsPraiseBack = true });
                BabyModel.Instance.ApplyPraiseRecords(records);
                viewObject.SetActive(true);
                view.Show();
                Transform content = view._Scroller1 != null ? view._Scroller1.content : FindNode(viewObject.transform, "Content");
                BabyBelikeItem[] items = content != null ? content.GetComponentsInChildren<BabyBelikeItem>(true) : new BabyBelikeItem[0];
                TMPro.TextMeshProUGUI firstName = items.Length > 0 ? FindNode(items[0].transform, "lb")?.GetComponent<TMPro.TextMeshProUGUI>() : null;
                UnityEngine.UI.Image firstButton = items.Length > 0 ? FindNode(items[0].transform, "likeBtn")?.GetComponent<UnityEngine.UI.Image>() : null;
                UnityEngine.UI.Image secondButton = items.Length > 1 ? FindNode(items[1].transform, "likeBtn")?.GetComponent<UnityEngine.UI.Image>() : null;
                bool shown = frames.Count == 1 && IsProtocol(frames[0], Proto.BABY_LIKE_RECORDS) && items.Length == 2
                    && firstName != null && firstName.text == "pending" && firstButton != null && firstButton.gameObject.activeSelf
                    && secondButton != null && !secondButton.gameObject.activeSelf;
                shown = shown && FindNode(viewObject.transform, "closeBtn")?.GetComponent<UnityEngine.UI.Button>() != null;
                UnityEngine.UI.Button button = firstButton != null ? firstButton.GetComponent<UnityEngine.UI.Button>() : null;
                button?.onClick.Invoke();
                shown = shown && frames.Count == 2 && IsProtocol(frames[1], Proto.BABY_PRAISE);
                BabyModel.Instance.ApplyPraiseRecords(new BabyPraiseRecordsInfo());
                Shenxiao.Framework.Event.EventDispatcher.Emit(Shenxiao.Framework.Event.GlobalEvent.EVT_BABY_UPDATE, Proto.BABY_LIKE_RECORDS);
                TMPro.TextMeshProUGUI empty = FindNode(viewObject.transform, "noOneLb")?.GetComponent<TMPro.TextMeshProUGUI>();
                bool emptyShown = empty != null && empty.gameObject.activeSelf
                    && (content == null || content.GetComponentsInChildren<BabyBelikeItem>(true).Length == 0);
                view.Hide();
                return shown && emptyShown;
            }
            finally
            {
                BabyModel.Instance.Reset();
                if (interceptField != null) interceptField.SetValue(null, oldIntercept);
                UnityEngine.Object.DestroyImmediate(viewObject);
            }
        }

        private static bool VerifyEquipUpgradeConfigs()
        {
            if (!BabyEquipUpgradeConfigs.IsLoaded || BabyEquipUpgradeConfigs.Materials.Count != 3) return false;
            var materials = BabyEquipUpgradeConfigs.Materials;
            BabyEquipUpgradeConfigs.StrenCfg first = BabyEquipUpgradeConfigs.GetStren(1, 0, 1);
            BabyEquipUpgradeConfigs.StrenCfg final = BabyEquipUpgradeConfigs.GetStren(6, 10, 0);
            BabyEquipUpgradeConfigs.StageCfg stage10 = BabyEquipUpgradeConfigs.GetStage(10);
            bool realConfig = materials[0].TypeId == 38040031 && materials[0].ExpPerItem == 10
                && materials[1].TypeId == 38040032 && materials[1].ExpPerItem == 50
                && materials[2].TypeId == 38040033 && materials[2].ExpPerItem == 100
                && first != null && first.PointCon == 10 && final != null && final.PointCon == 0
                && stage10 != null && stage10.Costs.Count == 3
                && stage10.Costs[0].TypeId == 38040034 && stage10.Costs[0].Num == 25
                && stage10.Costs[1].TypeId == 38040035 && stage10.Costs[1].Num == 10
                && stage10.Costs[2].TypeId == 38040036 && stage10.Costs[2].Num == 5;
            if (!realConfig) return false;

            MethodInfo setBagFull = typeof(Shenxiao.Module.Core.Bag.BagModel).GetMethod("SetBagFull", BindingFlags.Instance | BindingFlags.Public);
            if (setBagFull == null) return false;
            var bag = Shenxiao.Module.Core.Bag.BagModel.Instance;
            var entry = new BabyEquipEntry { PositionId = 1, Stage = 0, StageLevel = 0, StageExp = 0 };
            try
            {
                setBagFull.Invoke(bag, new object[] { 0, 20, new System.Collections.Generic.List<Shenxiao.Module.Core.Bag.BagGoods>
                {
                    new Shenxiao.Module.Core.Bag.BagGoods { GoodsId = 1, TypeId = 38040031, GoodsNum = 1 }
                }});
                BabyEquipUpgradeConfigs.PreviewResult normalExact = BabyEquipUpgradeConfigs.Preview(entry);
                bool normal = !normalExact.IsStageUpgrade && normalExact.RequiredExp == 10 && normalExact.Enough
                    && normalExact.Costs.Count == 1 && normalExact.Costs[0].TypeId == 38040031 && normalExact.Costs[0].Num == 1;

                setBagFull.Invoke(bag, new object[] { 0, 20, new System.Collections.Generic.List<Shenxiao.Module.Core.Bag.BagGoods>() });
                BabyEquipUpgradeConfigs.PreviewResult normalShort = BabyEquipUpgradeConfigs.Preview(entry);
                normal = normal && !normalShort.Enough && normalShort.Costs.Count == 0;

                entry.StageExp = 0;
                entry.StageLevel = 1;
                setBagFull.Invoke(bag, new object[] { 0, 20, new System.Collections.Generic.List<Shenxiao.Module.Core.Bag.BagGoods>
                {
                    new Shenxiao.Module.Core.Bag.BagGoods { GoodsId = 2, TypeId = 38040031, GoodsNum = 1 },
                    new Shenxiao.Module.Core.Bag.BagGoods { GoodsId = 3, TypeId = 38040032, GoodsNum = 1 }
                }});
                BabyEquipUpgradeConfigs.PreviewResult crossMaterial = BabyEquipUpgradeConfigs.Preview(entry);
                normal = normal && crossMaterial.Enough && crossMaterial.Costs.Count == 2
                    && crossMaterial.Costs[0].TypeId == 38040031 && crossMaterial.Costs[0].Num == 1
                    && crossMaterial.Costs[1].TypeId == 38040032 && crossMaterial.Costs[1].Num == 1;

                entry.Stage = 9;
                entry.StageLevel = 10;
                entry.StageExp = 0;
                setBagFull.Invoke(bag, new object[] { 0, 20, new System.Collections.Generic.List<Shenxiao.Module.Core.Bag.BagGoods>
                {
                    new Shenxiao.Module.Core.Bag.BagGoods { GoodsId = 4, TypeId = 38040034, GoodsNum = 25 },
                    new Shenxiao.Module.Core.Bag.BagGoods { GoodsId = 5, TypeId = 38040035, GoodsNum = 10 },
                    new Shenxiao.Module.Core.Bag.BagGoods { GoodsId = 6, TypeId = 38040036, GoodsNum = 5 }
                }});
                BabyEquipUpgradeConfigs.PreviewResult stageExact = BabyEquipUpgradeConfigs.Preview(entry);
                bool stage = stageExact.IsStageUpgrade && stageExact.Enough && stageExact.Costs.Count == 3
                    && stageExact.Costs[0].TypeId == 38040034 && stageExact.Costs[0].Num == 25
                    && stageExact.Costs[1].TypeId == 38040035 && stageExact.Costs[1].Num == 10
                    && stageExact.Costs[2].TypeId == 38040036 && stageExact.Costs[2].Num == 5;
                setBagFull.Invoke(bag, new object[] { 0, 20, new System.Collections.Generic.List<Shenxiao.Module.Core.Bag.BagGoods>
                {
                    new Shenxiao.Module.Core.Bag.BagGoods { GoodsId = 4, TypeId = 38040034, GoodsNum = 25 },
                    new Shenxiao.Module.Core.Bag.BagGoods { GoodsId = 5, TypeId = 38040035, GoodsNum = 10 },
                    new Shenxiao.Module.Core.Bag.BagGoods { GoodsId = 6, TypeId = 38040036, GoodsNum = 4 }
                }});
                BabyEquipUpgradeConfigs.PreviewResult stageShort = BabyEquipUpgradeConfigs.Preview(entry);
                stage = stage && stageShort.IsStageUpgrade && !stageShort.Enough && stageShort.Costs.Count == 3;

                entry.PositionId = 6;
                entry.Stage = 10;
                entry.StageLevel = 0;
                BabyEquipUpgradeConfigs.PreviewResult finalStage = BabyEquipUpgradeConfigs.Preview(entry);
                BabyEquipUpgradeConfigs.PreviewResult missing = BabyEquipUpgradeConfigs.Preview(new BabyEquipEntry { PositionId = 7, Stage = 1, StageLevel = 1 });
                return normal && stage && !finalStage.Enough && !finalStage.IsStageUpgrade
                    && !missing.Enough && !missing.IsStageUpgrade;
            }
            finally
            {
                bag.Clear();
            }
        }

        private static bool VerifyEquipEngraveConfigs()
        {
            BabyEquipEngraveConfigs.EngraveCfg weak = BabyEquipEngraveConfigs.Get(2, 38040037);
            BabyEquipEngraveConfigs.EngraveCfg strong = BabyEquipEngraveConfigs.Get(2, 38040040);
            if (!BabyEquipEngraveConfigs.IsLoaded || weak == null || strong == null || weak.Num != 2 || weak.Ratio != 500
                || strong.Num != 1 || strong.Ratio != 10000 || BabyEquipEngraveConfigs.GetColorCandidates(2).Count != 4) return false;
            MethodInfo setBagFull = typeof(Shenxiao.Module.Core.Bag.BagModel).GetMethod("SetBagFull", BindingFlags.Instance | BindingFlags.Public);
            if (setBagFull == null) return false;
            var bag = Shenxiao.Module.Core.Bag.BagModel.Instance;
            try
            {
                setBagFull.Invoke(bag, new object[] { 0, 20, new System.Collections.Generic.List<Shenxiao.Module.Core.Bag.BagGoods>
                {
                    new Shenxiao.Module.Core.Bag.BagGoods { GoodsId = 1, TypeId = 38040037, GoodsNum = 4 },
                    new Shenxiao.Module.Core.Bag.BagGoods { GoodsId = 2, TypeId = 38040040, GoodsNum = 2 }
                }});
                var entry = new BabyEquipEntry { PositionId = 1, GoodsTypeId = 65010200, SkillId = 0 };
                BabyEquipEngraveConfigs.PreviewResult repeated = BabyEquipEngraveConfigs.Preview(entry, new[] { 38040037, 38040037, 38040040 });
                BabyEquipEngraveConfigs.PreviewResult capped = BabyEquipEngraveConfigs.Preview(entry, new[] { 38040040, 38040040 });
                bool preview = repeated.Valid && repeated.Enough && repeated.Ratio == 10000 && repeated.Costs.Count == 2
                    && repeated.Costs[0].TypeId == 38040037 && repeated.Costs[0].Num == 4
                    && repeated.Costs[1].TypeId == 38040040 && repeated.Costs[1].Num == 1
                    && capped.Valid && capped.Enough && capped.Ratio == 10000
                    && bag.GetTypeGoodsNum(38040037) == 4 && bag.GetTypeGoodsNum(38040040) == 2;
                setBagFull.Invoke(bag, new object[] { 0, 20, new System.Collections.Generic.List<Shenxiao.Module.Core.Bag.BagGoods>
                {
                    new Shenxiao.Module.Core.Bag.BagGoods { GoodsId = 3, TypeId = 38040037, GoodsNum = 3 }
                }});
                BabyEquipEngraveConfigs.PreviewResult shortBag = BabyEquipEngraveConfigs.Preview(entry, new[] { 38040037, 38040037 });
                entry.SkillId = 1;
                BabyEquipEngraveConfigs.PreviewResult hadSkill = BabyEquipEngraveConfigs.Preview(entry, new[] { 38040037 });
                BabyEquipEngraveConfigs.PreviewResult invalid = BabyEquipEngraveConfigs.Preview(new BabyEquipEntry { GoodsTypeId = 65010200 }, new[] { 1 });
                return preview && shortBag.Valid && !shortBag.Enough && !hadSkill.Valid && !invalid.Valid;
            }
            finally { bag.Clear(); }
        }

        private static bool VerifyEquip()
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(EquipFuncViewPath);
            if (asset == null) return false;
            GameObject root = UnityEngine.Object.Instantiate(asset);
            FieldInfo interceptField = typeof(BabyController).GetField("s_outboundIntercept", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo setBagFull = typeof(Shenxiao.Module.Core.Bag.BagModel).GetMethod("SetBagFull", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo setBabyEquipBagFull = typeof(Shenxiao.Module.Core.Bag.BagModel).GetMethod("SetBabyEquipBagFull", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo setBabyEquipFull = typeof(Shenxiao.Module.Core.Bag.BagModel).GetMethod("SetBabyEquipFull", BindingFlags.Instance | BindingFlags.NonPublic);
            object oldIntercept = interceptField != null ? interceptField.GetValue(null) : null;
            try
            {
                BabyEquipFuncView view = root.GetComponent<BabyEquipFuncView>();
                if (view == null || interceptField == null || setBagFull == null || setBabyEquipBagFull == null || setBabyEquipFull == null) return false;
                var frames = new System.Collections.Generic.List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                setBagFull.Invoke(Shenxiao.Module.Core.Bag.BagModel.Instance, new object[] { 1, 20, new System.Collections.Generic.List<Shenxiao.Module.Core.Bag.BagGoods>
                {
                    new Shenxiao.Module.Core.Bag.BagGoods { GoodsId = 0x5152535455565758L, TypeId = 38040031, GoodsNum = 1 }
                }});
                setBabyEquipBagFull.Invoke(Shenxiao.Module.Core.Bag.BagModel.Instance, new object[] { 4, new System.Collections.Generic.List<Shenxiao.Module.Core.Bag.BagGoods>
                {
                    new Shenxiao.Module.Core.Bag.BagGoods { GoodsId = 0x0102030405060708L, TypeId = 65010200, GoodsNum = 1, Rating = 10 },
                    new Shenxiao.Module.Core.Bag.BagGoods { GoodsId = 0x1112131415161718L, TypeId = 65010300, GoodsNum = 1, Rating = 30 },
                    new Shenxiao.Module.Core.Bag.BagGoods { GoodsId = 0x2122232425262728L, TypeId = 65020200, GoodsNum = 1 },
                    new Shenxiao.Module.Core.Bag.BagGoods { GoodsId = 0x3132333435363738L, TypeId = 38040041, GoodsNum = 1 }
                }});
                const long wornId = 0x4142434445464748L;
                setBabyEquipFull.Invoke(Shenxiao.Module.Core.Bag.BagModel.Instance, new object[] { 1, new System.Collections.Generic.List<Shenxiao.Module.Core.Bag.BagGoods>
                {
                    new Shenxiao.Module.Core.Bag.BagGoods { GoodsId = wornId, TypeId = 65010200, GoodsNum = 1, Rating = 20 }
                }});
                BabyModel.Instance.ApplyStage(new BabyStageInfo { Stage = 1 });
                BabyModel.Instance.ApplyBasic(new BabyBasicInfo { BabyName = "equip" });
                BabyEquipInfo equip = new BabyEquipInfo { Power = 77 };
                equip.EquipList.Add(new BabyEquipEntry { PositionId = 1, Id = wornId, GoodsTypeId = 65010300 });
                BabyModel.Instance.ApplyEquip(equip);
                root.SetActive(true); view.Show();
                BabyEquipView content = null;
                BabyEquipView[] allContents = root.GetComponentsInChildren<BabyEquipView>(true);
                for (int i = 0; i < allContents.Length; i++)
                {
                    if (!allContents[i].gameObject.activeInHierarchy) continue;
                    content = allContents[i];
                    break;
                }
                BabyEquipIcon[] allIcons = content != null ? content.GetComponentsInChildren<BabyEquipIcon>(true) : new BabyEquipIcon[0];
                var icons = new System.Collections.Generic.List<BabyEquipIcon>();
                for (int i = 0; i < allIcons.Length; i++) if (allIcons[i].gameObject.activeInHierarchy) icons.Add(allIcons[i]);
                TMPro.TextMeshProUGUI name = content != null ? FindNode(content.transform, "nameLb")?.GetComponent<TMPro.TextMeshProUGUI>() : null;
                Shenxiao.Module.Core.Common.FightingShowSmallItem fighting = null;
                Shenxiao.Module.Core.Common.FightingShowSmallItem[] allFighting = content != null
                    ? content.GetComponentsInChildren<Shenxiao.Module.Core.Common.FightingShowSmallItem>(true)
                    : new Shenxiao.Module.Core.Common.FightingShowSmallItem[0];
                for (int i = 0; i < allFighting.Length; i++)
                {
                    if (!allFighting[i].gameObject.activeInHierarchy) continue;
                    fighting = allFighting[i];
                    break;
                }
                BabyEquipIcon first = null, last = null;
                for (int i = 0; i < icons.Count; i++) { if (icons[i].PositionId == 1) first = icons[i]; else if (icons[i].PositionId == 6) last = icons[i]; }
                UnityEngine.UI.Image firstDefault = first != null ? FindNode(first.transform, "defaultImg")?.GetComponent<UnityEngine.UI.Image>() : null;
                UnityEngine.UI.Image lastDefault = last != null ? FindNode(last.transform, "defaultImg")?.GetComponent<UnityEngine.UI.Image>() : null;
                bool pass = frames.Count == 1 && IsProtocol(frames[0], Proto.BABY_EQUIP_INFO) && content != null && icons.Count == 6
                    && name != null && name.text == "equip" && fighting != null && fighting._lb_fighting != null && fighting._lb_fighting.text == "77"
                    && first != null && first.IsOccupied && last != null && !last.IsOccupied
                    && firstDefault != null && !firstDefault.gameObject.activeSelf && lastDefault != null && lastDefault.gameObject.activeSelf;
                UnityEngine.UI.Button forgeButton = content != null ? FindNode(content.transform, "forgeBtn")?.GetComponent<UnityEngine.UI.Button>() : null;
                UnityEngine.UI.Button imprintButton = content != null ? FindNode(content.transform, "imprintBtn")?.GetComponent<UnityEngine.UI.Button>() : null;
                bool forgeReady = forgeButton != null && forgeButton.interactable && content.ForgeInteractable && imprintButton == null;
                var candidates = new System.Collections.Generic.List<BabyEquipSubItem>();
                BabyEquipSubItem[] allCandidates = content != null ? content.GetComponentsInChildren<BabyEquipSubItem>(true) : new BabyEquipSubItem[0];
                for (int i = 0; i < allCandidates.Length; i++) if (allCandidates[i].gameObject.activeInHierarchy) candidates.Add(allCandidates[i]);
                bool initialCandidates = first != null && FindNode(first.transform, "effectGp")?.gameObject.activeSelf == true
                    && candidates.Count == 2 && candidates[0].GoodsId == 0x0102030405060708L && candidates[1].GoodsId == 0x1112131415161718L;
                bool ratingRed = candidates.Count == 2 && !candidates[0].BetterVisible && candidates[1].BetterVisible;
                bool wornOverride = first != null && first.GoodsId == wornId && first.TypeId == 65010200;
                if (forgeButton != null) { forgeButton.onClick.Invoke(); forgeButton.onClick.Invoke(); }
                int upgradeFrames = 0;
                bool upgradeWire = false;
                for (int i = 0; i < frames.Count; i++)
                {
                    if (!IsProtocol(frames[i], Proto.BABY_EQUIP_UPGRADE)) continue;
                    upgradeFrames++;
                    NetReader reader = new NetReader(frames[i], 6, frames[i].Length - 6);
                    upgradeWire = reader.ReadU8() == 1 && reader.Remaining == 0;
                }
                string forgeMaterialName = Shenxiao.Module.Core.Common.GoodsModel.GetGoodsName(38040031);
                bool forgeConfirm = content.UpgradePending && !content.ForgeInteractable && upgradeFrames == 1 && upgradeWire
                    && !string.IsNullOrEmpty(forgeMaterialName) && content.LastForgeConfirmText.Contains(forgeMaterialName)
                    && content.LastForgeConfirmText.Contains("×1");
                EventDispatcher.Emit(GlobalEvent.EVT_BABY_UPDATE, Proto.BABY_EQUIP_UPGRADE);
                bool forgeRecovered = !content.UpgradePending && content.ForgeInteractable;
                forgeButton = FindNode(content.transform, "forgeBtn")?.GetComponent<UnityEngine.UI.Button>();
                setBagFull.Invoke(Shenxiao.Module.Core.Bag.BagModel.Instance, new object[] { 0, 20, new System.Collections.Generic.List<Shenxiao.Module.Core.Bag.BagGoods>() });
                EventDispatcher.Emit(GlobalEvent.EVT_BAG_UPDATE);
                bool forgeBagDisabled = !content.ForgeInteractable && forgeButton != null && !forgeButton.interactable;
                setBabyEquipFull.Invoke(Shenxiao.Module.Core.Bag.BagModel.Instance, new object[] { 1, new System.Collections.Generic.List<Shenxiao.Module.Core.Bag.BagGoods>
                {
                    new Shenxiao.Module.Core.Bag.BagGoods { GoodsId = wornId + 1, TypeId = 65010200, GoodsNum = 1 }
                }});
                EventDispatcher.Emit(GlobalEvent.EVT_BABY_EQUIP_UPDATE);
                allIcons = content.GetComponentsInChildren<BabyEquipIcon>(true);
                icons.Clear();
                for (int i = 0; i < allIcons.Length; i++) if (allIcons[i].gameObject.activeInHierarchy) icons.Add(allIcons[i]);
                first = null;
                for (int i = 0; i < icons.Count; i++) if (icons[i].PositionId == 1) { first = icons[i]; break; }
                bool wornFallback = first != null && first.GoodsId == wornId && first.TypeId == 65010300;
                candidates.Clear();
                allCandidates = content.GetComponentsInChildren<BabyEquipSubItem>(true);
                for (int i = 0; i < allCandidates.Length; i++) if (allCandidates[i].gameObject.activeInHierarchy) candidates.Add(allCandidates[i]);
                bool fallbackRed = candidates.Count == 2 && candidates[0].BetterVisible && candidates[1].BetterVisible;
                BabyEquipIcon second = null;
                for (int i = 0; i < icons.Count; i++) if (icons[i].PositionId == 2) { second = icons[i]; break; }
                UnityEngine.UI.Button secondButton = second != null ? second.GetComponent<UnityEngine.UI.Button>() : null;
                if (secondButton != null) secondButton.onClick.Invoke();
                BabyEquipIcon selectedFirst = null, selectedSecond = null;
                allIcons = content != null ? content.GetComponentsInChildren<BabyEquipIcon>(true) : new BabyEquipIcon[0];
                for (int i = 0; i < allIcons.Length; i++)
                {
                    if (!allIcons[i].gameObject.activeInHierarchy) continue;
                    if (allIcons[i].PositionId == 1) selectedFirst = allIcons[i];
                    else if (allIcons[i].PositionId == 2) selectedSecond = allIcons[i];
                }
                candidates.Clear();
                allCandidates = content != null ? content.GetComponentsInChildren<BabyEquipSubItem>(true) : new BabyEquipSubItem[0];
                for (int i = 0; i < allCandidates.Length; i++) if (allCandidates[i].gameObject.activeInHierarchy) candidates.Add(allCandidates[i]);
                bool selectedSecondEffect = selectedSecond != null && FindNode(selectedSecond.transform, "effectGp")?.gameObject.activeSelf == true;
                bool selectedFirstEffect = selectedFirst != null && FindNode(selectedFirst.transform, "effectGp")?.gameObject.activeSelf == true;
                bool secondCandidates = selectedSecondEffect && selectedFirst != null && !selectedFirstEffect
                    && candidates.Count == 1 && candidates[0].TypeId == 65020200 && candidates[0].BetterVisible;
                UnityEngine.UI.Button candidateButton = candidates.Count == 1 ? candidates[0].GetComponent<UnityEngine.UI.Button>() : null;
                if (candidateButton != null) candidateButton.onClick.Invoke();
                bool wearFrame = false;
                for (int i = 0; i < frames.Count; i++)
                {
                    if (!IsProtocol(frames[i], Proto.BABY_EQUIP_WEAR)) continue;
                    NetReader reader = new NetReader(frames[i], 6, frames[i].Length - 6);
                    wearFrame = reader.ReadU8() == 2 && reader.ReadU64() == 0x2122232425262728L && reader.Remaining == 0;
                    break;
                }
                Debug.Log("CLIVERIFY baby equip frames=" + frames.Count + " proto=" + (frames.Count > 0 && IsProtocol(frames[0], Proto.BABY_EQUIP_INFO))
                    + " content=" + (content != null) + " icons=" + icons.Count + " name=" + (name != null ? name.text : "<null>")
                    + " fighting=" + (fighting != null && fighting._lb_fighting != null ? fighting._lb_fighting.text : "<null>") + " first=" + (first != null ? first.IsOccupied.ToString() : "<null>")
                    + " last=" + (last != null ? last.IsOccupied.ToString() : "<null>") + " firstDefault=" + (firstDefault != null ? firstDefault.gameObject.activeSelf.ToString() : "<null>")
                    + " lastDefault=" + (lastDefault != null ? lastDefault.gameObject.activeSelf.ToString() : "<null>") + " forge=" + forgeReady + "/" + forgeConfirm + "/" + forgeRecovered + "/" + forgeBagDisabled
                    + " initialCandidates=" + initialCandidates + " secondCandidates=" + secondCandidates + " wearFrame=" + wearFrame
                    + " worn=" + wornOverride + "/" + wornFallback
                    + " selectedFirst=" + (selectedFirst != null) + "/" + selectedFirstEffect + " selectedSecond=" + (selectedSecond != null) + "/" + selectedSecondEffect
                    + " candidateCount=" + candidates.Count + " candidateType=" + (candidates.Count > 0 ? candidates[0].TypeId : 0));
                pass = pass && forgeReady && forgeConfirm && forgeRecovered && forgeBagDisabled && initialCandidates && ratingRed && wornOverride && wornFallback && fallbackRed && secondCandidates && wearFrame;
                view.Hide(); return pass;
            }
            finally { Shenxiao.Module.Core.Bag.BagModel.Instance.Clear(); BabyModel.Instance.Reset(); if (interceptField != null) interceptField.SetValue(null, oldIntercept); UnityEngine.Object.DestroyImmediate(root); }
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
