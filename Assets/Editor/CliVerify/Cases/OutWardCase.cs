using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 幻化外观(OutWard)实证:config_mount_star 同步 + 16002/16023/16028/16029 合成包驱动 OutWardModel/Controller,
    /// 断言字段套值 + errcode!=1 不抛异常;再拉起 OutWardShellView 渲染断言「1阶2星」文案与升星按钮存在。
    /// 独立文件复用 CliVerify.Stage/Pkt/FindDeep(已 public),不改 CliVerify.cs 本体(主控统一接 RenderAll)。
    /// 轮24 PI 扩:幻化(Illusion)全链 16000/16001/16003/16004/16006-16012/16020/16022/16024/16027,
    /// 含裁决6红线(AllTypeIds 严格 {1,2,3,4,5,12})、4 张幻化专属配表加载、16006/16007/16011 嵌套数组
    /// 尾哨兵核对——扩既有 OutWardCase 而非新建 OutWardIllusionCase(仓库现状只有一个 OutWard Case,禁双份)。
    /// </summary>
    public static class OutWardCase
    {
        public static async Task<int> Run()
        {
            CliVerify.Stage stage = CliVerify.Stage.Create();
            try
            {
                Shenxiao.EditorTools.ConfigGen.ClientConfigSync.SyncIfStale(true);
                await Shenxiao.Module.Core.OutWard.OutWardConfigs.EnsureLoaded();
                await Shenxiao.Module.Core.Common.GoodsModel.EnsureLoaded();
                await Shenxiao.Module.Core.Skill.SkillConfigs.EnsureLoaded();
                await Shenxiao.Module.Core.Shop.ShopConfigs.EnsureLoaded();
                if (!Shenxiao.Module.Core.OutWard.OutWardConfigs.IsLoaded)
                {
                    Debug.LogError("CLIVERIFY FAIL config_mount_star not loaded");
                    return 3;
                }

                // 角色→垂神翼影(type_id=3)的静态配表闭包：4 个常驻技能、3 种培养材料、3 枚魔晶。
                // 这里只保护配置/排序契约，不替代真实 RoleFlow 组合页、点击、模型与 Web 像素门禁。
                IReadOnlyList<int> wingSkills = Shenxiao.Module.Core.OutWard.OutWardConfigs.GetDefaultSkillIds(3);
                IReadOnlyList<int> wingMaterials = Shenxiao.Module.Core.OutWard.OutWardConfigs.GetTrainGoodsIds(3);
                IReadOnlyList<int> wingCrystals = Shenxiao.Module.Core.OutWard.OutWardConfigs.GetCrystalGoodsIds(3);
                bool WingIdsMatch(IReadOnlyList<int> actual, params int[] expected)
                {
                    if (actual == null || actual.Count != expected.Length) return false;
                    for (int i = 0; i < expected.Length; i++)
                        if (actual[i] != expected[i]) return false;
                    return true;
                }
                bool wingConfigOk = WingIdsMatch(wingSkills, 59150004, 59150005, 59150006, 59150007)
                    && WingIdsMatch(wingMaterials, 18020001, 18020002, 18020003)
                    && WingIdsMatch(wingCrystals, 18010001, 18010002, 18010003);
                bool sharedConsumerShapeConfigOk = Shenxiao.Module.Core.OutWard.OutWardConfigs.GetDefaultSkillIds(1).Count == 4
                    && Shenxiao.Module.Core.OutWard.OutWardConfigs.GetDefaultSkillIds(4).Count == 4;
                IReadOnlyList<int> wingLevelSkills = Shenxiao.Module.Core.OutWard.OutWardConfigs.GetLevelSkillIds(3);
                bool wingLevelSkillConfigOk = WingIdsMatch(wingLevelSkills, 59150001, 59150002, 59150003);
                var trainRows = new List<Shenxiao.Module.Core.OutWard.OutWardConfigs.TrainGoodsConfig>
                {
                    new Shenxiao.Module.Core.OutWard.OutWardConfigs.TrainGoodsConfig { GoodsId = 1, Type = 1, Exp = 10 },
                    new Shenxiao.Module.Core.OutWard.OutWardConfigs.TrainGoodsConfig { GoodsId = 2, Type = 1, Exp = 30 },
                    new Shenxiao.Module.Core.OutWard.OutWardConfigs.TrainGoodsConfig { GoodsId = 3, Type = 4, Exp = 999 },
                };
                var trainVo = new Shenxiao.Module.Core.OutWard.OutWardModel.OutWardVo
                    { TypeId = 3, Stage = 1, Star = 1, Blessing = 20 };
                var oneKeyZero = Shenxiao.Module.Core.OutWard.OutWardModel.ProjectOneKeyState(
                    trainVo, 10, true, 100, trainRows, _ => 0);
                var oneKeyPartial = Shenxiao.Module.Core.OutWard.OutWardModel.ProjectOneKeyState(
                    trainVo, 10, true, 100, trainRows, id => id == 1 ? 2 : 0);
                var oneKeyExact = Shenxiao.Module.Core.OutWard.OutWardModel.ProjectOneKeyState(
                    trainVo, 10, true, 100, trainRows, id => id == 1 ? 2 : id == 2 ? 2 : 99);
                var oneKeyOver = Shenxiao.Module.Core.OutWard.OutWardModel.ProjectOneKeyState(
                    trainVo, 10, true, 100, trainRows, id => id == 2 ? 3 : 0);
                var maxVo = new Shenxiao.Module.Core.OutWard.OutWardModel.OutWardVo
                    { TypeId = 3, Stage = 10, Star = 10, Blessing = 0 };
                var oneKeyMax = Shenxiao.Module.Core.OutWard.OutWardModel.ProjectOneKeyState(
                    maxVo, 10, false, 100, trainRows, id => id == 2 ? 99 : 0);
                bool oneKeyProjectionOk = oneKeyZero.Availability == Shenxiao.Module.Core.OutWard.OutWardModel.OneKeyAvailability.Insufficient
                    && oneKeyZero.NeedBlessing == 80 && oneKeyZero.ProvidedExp == 0 && oneKeyZero.ShouldOpenQuickBuy && !oneKeyZero.ShowRedDot
                    && oneKeyPartial.Availability == Shenxiao.Module.Core.OutWard.OutWardModel.OneKeyAvailability.Insufficient
                    && oneKeyPartial.ProvidedExp == 20 && oneKeyPartial.ShouldOpenQuickBuy && !oneKeyPartial.CanSubmit
                    && oneKeyExact.Availability == Shenxiao.Module.Core.OutWard.OutWardModel.OneKeyAvailability.Ready
                    && oneKeyExact.ProvidedExp == 80 && oneKeyExact.CanSubmit && oneKeyExact.ShowRedDot
                    && oneKeyOver.Availability == Shenxiao.Module.Core.OutWard.OutWardModel.OneKeyAvailability.Ready
                    && oneKeyOver.ProvidedExp == 90 && oneKeyOver.CanSubmit
                    && oneKeyMax.Availability == Shenxiao.Module.Core.OutWard.OutWardModel.OneKeyAvailability.MaxStage
                    && !oneKeyMax.CanSubmit && !oneKeyMax.ShouldOpenQuickBuy && !oneKeyMax.ShowRedDot;
                Debug.Log("CLIVERIFY outward wing type3 config skills=[" + string.Join(",", wingSkills)
                    + "] materials=[" + string.Join(",", wingMaterials) + "] crystals=[" + string.Join(",", wingCrystals)
                    + "] targetOk=" + wingConfigOk + " representativeShapeOk=" + sharedConsumerShapeConfigOk);

                var effectState = new Shenxiao.Module.Core.OutWard.OutWardModel.EffectLifecycleState();
                effectState.Begin(7, "wing/1");
                bool effectStateOk = effectState.MarkAttached(7)
                    && effectState.ObserveFrame(7, 12, 101)
                    && effectState.ObserveFrame(7, 14, 202)
                    && effectState.Phase == Shenxiao.Module.Core.OutWard.OutWardModel.EffectLifecyclePhase.FirstFrameReady
                    && effectState.HasDynamicFrameChange;
                effectState.Release(8);
                effectStateOk &= effectState.Phase == Shenxiao.Module.Core.OutWard.OutWardModel.EffectLifecyclePhase.Released;

                Shenxiao.Module.Core.FairyWish.FairyWishModel fairyModel = Shenxiao.Module.Core.FairyWish.FairyWishModel.Instance;
                fairyModel.Reset();
                fairyModel.SetEntryRedStateForAuthority(1003,
                    Shenxiao.Module.Core.FairyWish.FairyWishModel.EntryRedState.Bubble);
                var firstTouch = fairyModel.ConfirmEntryTouch(1003);
                var secondTouch = fairyModel.ConfirmEntryTouch(1003);
                fairyModel.ApplyInfo(1003, 0, new List<(int, int, int)>());
                var purchaseState = fairyModel.GetOperateState(1003, 0, 999);
                bool fairySemanticsOk = firstTouch.Send51302
                    && firstTouch.State == Shenxiao.Module.Core.FairyWish.FairyWishModel.EntryRedState.RedDot
                    && !secondTouch.Send51302
                    && secondTouch.State == Shenxiao.Module.Core.FairyWish.FairyWishModel.EntryRedState.Hidden
                    && purchaseState.Kind == Shenxiao.Module.Core.FairyWish.FairyWishModel.OperateKind.PurchaseRequired;

                int[] expectedPolicies = { 15304, 16003, 16005, 16008, 16009, 16010, 16020, 16023, 16029, 16030, 51302 };
                bool refreshMatrixOk = Shenxiao.Module.Core.OutWard.OutWardTransactionRefreshPolicies.All.Length == expectedPolicies.Length;
                foreach (int command in expectedPolicies)
                {
                    Shenxiao.Module.Core.OutWard.OutWardTransactionRefreshPolicy policy =
                        Shenxiao.Module.Core.OutWard.OutWardTransactionRefreshPolicies.Get(command);
                    refreshMatrixOk &= policy != null && (command == 51302
                        ? !policy.HasAcknowledgement
                        : policy.HasAcknowledgement && !string.IsNullOrEmpty(policy.SuccessEvent)
                            && !string.IsNullOrEmpty(policy.FailureEvent));
                }
                System.Type illusionViewType = typeof(Shenxiao.Module.Core.Pet.IllusionBaseView);
                System.Type levelViewType = typeof(Shenxiao.Module.Core.Pet.OutwardLvSystemView);
                bool productionSourceContractOk = illusionViewType.BaseType == typeof(Shenxiao.Generated.UI.Pet.IllusionBaseViewBind)
                    && illusionViewType.GetMethod("Open", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance) != null
                    && illusionViewType.GetMethod("Close", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance) != null
                    && typeof(Shenxiao.Module.Core.OutWard.OutWardController).GetMethod("WearIllusion") != null
                    && typeof(Shenxiao.Module.Core.OutWard.OutWardController).GetMethod("ActivateFigure") != null
                    && typeof(Shenxiao.Module.Core.OutWard.OutWardController).GetMethod("UpgradeFigureStage") != null
                    && typeof(Shenxiao.Module.Core.OutWard.OutWardController).GetMethod("UpgradeFigureStar") != null
                    && levelViewType.BaseType == typeof(Shenxiao.Generated.UI.Pet.OutwardLvSystemBind)
                    && levelViewType.GetMethod("Open", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance) != null
                    && levelViewType.GetMethod("Close", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance) != null
                    && typeof(Shenxiao.Module.Core.OutWard.OutWardController).GetMethod("TryLvSkillUp") != null;
                bool productionReparentContractOk = VerifyProductionReparentContract();
                bool illusionSemanticContractOk = VerifyIllusionSemanticContract();
                Shenxiao.Module.Core.Shop.QuickBuyFlow.State quickBuyMin =
                    Shenxiao.Module.Core.Shop.QuickBuyFlow.Project(18020001, 0, 2, 10, 10, 30, 25);
                Shenxiao.Module.Core.Shop.QuickBuyFlow.State quickBuyPlus =
                    Shenxiao.Module.Core.Shop.QuickBuyFlow.Project(18020001, 2, 2, 10, 10, 30, 25);
                Shenxiao.Module.Core.Shop.QuickBuyFlow.State quickBuyMax =
                    Shenxiao.Module.Core.Shop.QuickBuyFlow.Project(18020001, 2, 1, 10, 10, 30, 25);
                Shenxiao.Module.Core.Shop.QuickBuyFlow.State quickBuyFail =
                    Shenxiao.Module.Core.Shop.QuickBuyFlow.Project(18020001, 3, 2, 10, 10, 30, 25);
                bool calculatorProjectionOk =
                    Shenxiao.Module.Core.Common.CalculatorFlow.ProjectDigit(12, 3, 200) == 123
                    && Shenxiao.Module.Core.Common.CalculatorFlow.ProjectDigit(12, 9, 100) == 100
                    && Shenxiao.Module.Core.Common.CalculatorFlow.ProjectBackspace(123) == 12;
                bool quickBuySourceRouteOk =
                    Shenxiao.Module.Core.Shop.QuickBuyFlow.ResolveGoodsSourceTab(140) == 1
                    && Shenxiao.Module.Core.Shop.QuickBuyFlow.ResolveGoodsSourceTab(141) == 2
                    && Shenxiao.Module.Core.Shop.QuickBuyFlow.ResolveGoodsSourceTab(53) == -1
                    && typeof(Shenxiao.Module.Core.Shop.QuickBuySourceItem).GetMethod("SetData") != null
                    && Shenxiao.Module.Core.Common.GoodsModel.GetGoodsSourceEntries(18020001).Count == 2
                    && Shenxiao.Module.Core.Common.GoodsModel.GetGoodsSourceEntries(18020001)[0].Id == 140
                    && Shenxiao.Module.Core.Common.GoodsModel.GetGoodsSourceEntries(18020001)[1].Id == 141;
                bool quickBuyContractOk = quickBuyMin.Count == 1 && quickBuyMin.TotalPrice == 10
                    && quickBuyPlus.CanBuy && quickBuyPlus.MaxAffordable == 2
                    && quickBuyMax.CanBuy && quickBuyMax.MaxAffordable == 3
                    && !quickBuyFail.CanBuy && quickBuyFail.BlockReason == "bound-gold-insufficient"
                    && Shenxiao.Module.Core.Shop.ShopConfigs.GetQuickBuyPrice(18020001) != null
                    && calculatorProjectionOk && quickBuySourceRouteOk;
                Debug.Log("CLIVERIFY outward pure states levelSkillCfg=" + wingLevelSkillConfigOk
                    + " oneKey=" + oneKeyProjectionOk + " effect=" + effectStateOk
                    + " fairy51302=" + fairySemanticsOk + " refreshMatrix=" + refreshMatrixOk
                    + " productionSource=" + productionSourceContractOk
                    + " productionReparent=" + productionReparentContractOk
                    + " illusionSemantic=" + illusionSemanticContractOk
                    + " quickBuy=" + quickBuyContractOk);

                Shenxiao.Module.Core.OutWard.OutWardController ctrl = Shenxiao.Module.Core.OutWard.OutWardController.Instance;
                ctrl.Init();
                const System.Reflection.BindingFlags F =
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                const System.Reflection.BindingFlags SF =
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;

                // 注册必须落进 NetManager 运行时真字典，不能只靠“存在同名 handler”反射冒充已接线。
                int[] expectedRegistered =
                {
                    Shenxiao.Framework.Net.Proto.OUTWARD_ERROR,
                    Shenxiao.Framework.Net.Proto.OUTWARD_SCENE_FIGURE_CHANGE,
                    Shenxiao.Framework.Net.Proto.OUTWARD_INFO,
                    Shenxiao.Framework.Net.Proto.OUTWARD_ILLUSION_WEAR,
                    Shenxiao.Framework.Net.Proto.OUTWARD_RIDE_TOGGLE,
                    Shenxiao.Framework.Net.Proto.OUTWARD_STAR_UP_GENERIC,
                    Shenxiao.Framework.Net.Proto.OUTWARD_ILLUSION_LIST,
                    Shenxiao.Framework.Net.Proto.OUTWARD_FIGURE_DETAIL,
                    Shenxiao.Framework.Net.Proto.OUTWARD_FIGURE_ACTIVATE,
                    Shenxiao.Framework.Net.Proto.OUTWARD_FIGURE_STAGE_UP,
                    Shenxiao.Framework.Net.Proto.OUTWARD_CRYSTAL_USE,
                    Shenxiao.Framework.Net.Proto.OUTWARD_CRYSTAL_COUNTER,
                    Shenxiao.Framework.Net.Proto.OUTWARD_FIGURE_EXPIRED,
                    Shenxiao.Framework.Net.Proto.OUTWARD_FIGURE_STAR_UP,
                    Shenxiao.Framework.Net.Proto.OUTWARD_FIGHT_PREVIEW,
                    Shenxiao.Framework.Net.Proto.OUTWARD_STAR_UP,
                    Shenxiao.Framework.Net.Proto.OUTWARD_AUTO_BUY,
                    Shenxiao.Framework.Net.Proto.OUTWARD_STAR_FIGHT_PREVIEW,
                    Shenxiao.Framework.Net.Proto.OUTWARD_LV_PANEL,
                    Shenxiao.Framework.Net.Proto.OUTWARD_LV_UP,
                    Shenxiao.Framework.Net.Proto.OUTWARD_LV_SKILL_UP,
                };
                System.Reflection.FieldInfo handlersField = typeof(Shenxiao.Framework.Net.NetManager).GetField("_handlers", SF);
                var runtimeHandlers = handlersField?.GetValue(null) as System.Collections.IDictionary;
                bool registrationOk = runtimeHandlers != null;
                foreach (int cmd in expectedRegistered)
                {
                    if (!registrationOk || !runtimeHandlers.Contains(cmd)
                        || !(runtimeHandlers[cmd] is System.Delegate handler)
                        || !object.ReferenceEquals(handler.Target, ctrl))
                    {
                        registrationOk = false;
                        break;
                    }
                }
                Debug.Log("CLIVERIFY outward runtime registration count=" + expectedRegistered.Length + " ok=" + registrationOk);

                // 现有 NetManager 无出站捕获器；用 Controller 内 UNITY_EDITOR-only 截获缝验证两个真实调用入口，
                // 截获时抑制网络发送，避免“断网不抛异常”弱断言，也避免 CliVerify 误向活连接发测试包。
                System.Reflection.MethodInfo mGameStart = ctrl.GetType().GetMethod("OnGameStart", F);
                System.Reflection.FieldInfo requestInterceptField = ctrl.GetType().GetField("s_initialRequestIntercept", SF);
                var requestTrace = new List<(int proto, int typeId)>();
                bool InitialPlanMatches(IReadOnlyList<(int proto, int typeId)> trace, IReadOnlyList<int> typeIds)
                {
                    int[] expected =
                    {
                        Shenxiao.Framework.Net.Proto.OUTWARD_INFO,
                        Shenxiao.Framework.Net.Proto.OUTWARD_ILLUSION_LIST,
                        Shenxiao.Framework.Net.Proto.OUTWARD_CRYSTAL_COUNTER,
                        Shenxiao.Framework.Net.Proto.OUTWARD_LV_PANEL,
                    };
                    if (trace.Count != typeIds.Count * expected.Length) return false;
                    int offset = 0;
                    foreach (int typeId in typeIds)
                    {
                        foreach (int proto in expected)
                        {
                            if (trace[offset].proto != proto || trace[offset].typeId != typeId) return false;
                            offset++;
                        }
                    }
                    return true;
                }

                bool gameStartRequestsOk = false;
                bool setTypeRequestsOk = false;
                GameObject requestProbe = null;
                if (mGameStart != null && requestInterceptField != null)
                {
                    System.Func<int, int, bool> intercept = (proto, typeId) =>
                    {
                        requestTrace.Add((proto, typeId));
                        return true;
                    };
                    try
                    {
                        requestInterceptField.SetValue(null, intercept);
                        mGameStart.Invoke(ctrl, null);
                        await Task.Delay(20); // OnGameStart 是 async void；配置已加载时通常同步完成，仍留一拍防未来改异步。
                        gameStartRequestsOk = InitialPlanMatches(requestTrace, Shenxiao.Module.Core.OutWard.OutWardController.AllTypeIds);

                        requestTrace.Clear();
                        requestProbe = new GameObject("CliVerify_OutWardBaseView_RequestProbe");
                        requestProbe.SetActive(false);
                        Shenxiao.Module.Core.Pet.OutWardBaseView probeView = requestProbe.AddComponent<Shenxiao.Module.Core.Pet.OutWardBaseView>();
                        probeView.SetType(12);
                        setTypeRequestsOk = InitialPlanMatches(requestTrace, new[] { 12 });
                    }
                    finally
                    {
                        requestInterceptField.SetValue(null, null);
                        if (requestProbe != null) Object.DestroyImmediate(requestProbe);
                    }
                }
                Debug.Log("CLIVERIFY outward init requests gameStart=" + gameStartRequestsOk + " setType=" + setTypeRequestsOk);

                System.Reflection.MethodInfo m16002 = ctrl.GetType().GetMethod("On16002", F);
                System.Reflection.MethodInfo m16023 = ctrl.GetType().GetMethod("On16023", F);
                System.Reflection.MethodInfo m16028 = ctrl.GetType().GetMethod("On16028", F);
                System.Reflection.MethodInfo m16029 = ctrl.GetType().GetMethod("On16029", F);
                if (m16002 == null || m16023 == null || m16028 == null || m16029 == null)
                {
                    Debug.LogError("CLIVERIFY outward handlers missing (reflection)");
                    return 3;
                }
                void Feed(System.Reflection.MethodInfo m, byte[] pkt) =>
                    m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });

                Shenxiao.Module.Core.OutWard.OutWardModel model = Shenxiao.Module.Core.OutWard.OutWardModel.Instance;

                // 16002 回包:type_id:c, stage:c, star:h, blessing:i, figure_stage:c, combat:i, etime:l, auto_buy:c,
                // attr_list[u16×{attr_id:c,attr_val:i}], skill_list[u16×{skill_id:i}]。type_id=1(坐骑) 1阶1星 blessing=5。
                byte[] p16002 = new CliVerify.Pkt()
                    .C(1)      // type_id
                    .C(1)      // stage
                    .H(1)      // star
                    .I(5)      // blessing
                    .C(0)      // figure_stage
                    .I(100)    // combat
                    .L(0)      // etime
                    .C(0)      // auto_buy
                    .H(0)      // attr_list 计数
                    .H(0)      // skill_list 计数
                    .Bytes();
                Feed(m16002, p16002);
                Shenxiao.Module.Core.OutWard.OutWardModel.OutWardVo vo1 = model.Get(1);
                bool infoOk = vo1 != null && vo1.Stage == 1 && vo1.Star == 1 && vo1.Blessing == 5 && vo1.Combat == 100;
                Debug.Log("CLIVERIFY outward 16002 type1 stage=" + (vo1?.Stage ?? -1) + " star=" + (vo1?.Star ?? -1)
                    + " blessing=" + (vo1?.Blessing ?? -1) + " ok=" + infoOk);

                // 16023 升星成功:errcode=1, type_id=1, stage=1, star=2, blessing=10, blessing_plus=0, etime=0, auto_buy=0, ratio_list 空
                // (成功后控制器内部会 SendFmt 16002 联动刷新,未连接只 warn,无害)。
                byte[] p16023Ok = new CliVerify.Pkt().I(1).C(1).C(1).H(2).I(10).I(0).L(0).C(0).H(0).Bytes();
                Feed(m16023, p16023Ok);
                vo1 = model.Get(1);
                bool starUpOk = vo1 != null && vo1.Stage == 1 && vo1.Star == 2 && vo1.Blessing == 10;
                Debug.Log("CLIVERIFY outward 16023 ok stage=" + (vo1?.Stage ?? -1) + " star=" + (vo1?.Star ?? -1)
                    + " blessing=" + (vo1?.Blessing ?? -1) + " ok=" + starUpOk);

                // 16023 升星失败:errcode=5,只要不抛异常(走 toast log 分支)即过。
                byte[] p16023Fail = new CliVerify.Pkt().I(5).C(1).C(1).H(2).I(0).I(0).L(0).C(0).H(0).Bytes();
                bool starUpFailNoThrow = true;
                try { Feed(m16023, p16023Fail); }
                catch (System.Exception e) { starUpFailNoThrow = false; Debug.LogError("CLIVERIFY outward 16023 fail threw: " + e); }
                Debug.Log("CLIVERIFY outward 16023 fail noThrow=" + starUpFailNoThrow);

                // 16028 面板回包:type_id:c, level:h, cur_exp:i, combat:i, attr_list[], skill_list[u16×{skill_id:i,skill_level:c}]。
                // type_id=2(同修) level=1。
                byte[] p16028 = new CliVerify.Pkt().C(2).H(1).I(0).I(50).H(0).H(0).Bytes();
                Feed(m16028, p16028);
                Shenxiao.Module.Core.OutWard.OutWardModel.OutWardVo vo2 = model.Get(2);
                bool lvPanelOk = vo2 != null && vo2.HasLv && vo2.Level == 1 && vo2.LvCombat == 50;
                Debug.Log("CLIVERIFY outward 16028 type2 level=" + (vo2?.Level ?? -1) + " combat=" + (vo2?.LvCombat ?? -1) + " ok=" + lvPanelOk);

                // 16029 升级成功:errcode=1, type_id=2, level=2, cur_exp=0, add_exp=100, combat=60, skill_list[]=0, ratio_list[]=0。
                byte[] p16029Ok = new CliVerify.Pkt().I(1).C(2).H(2).I(0).I(100).I(60).H(0).H(0).Bytes();
                Feed(m16029, p16029Ok);
                vo2 = model.Get(2);
                bool lvUpOk = vo2 != null && vo2.Level == 2 && vo2.LvCombat == 60;
                Debug.Log("CLIVERIFY outward 16029 ok level=" + (vo2?.Level ?? -1) + " combat=" + (vo2?.LvCombat ?? -1) + " ok=" + lvUpOk);

                // 16029 升级失败:errcode=5,只要不抛异常即过。
                byte[] p16029Fail = new CliVerify.Pkt().I(5).C(2).H(2).I(0).I(0).I(0).H(0).H(0).Bytes();
                bool lvUpFailNoThrow = true;
                try { Feed(m16029, p16029Fail); }
                catch (System.Exception e) { lvUpFailNoThrow = false; Debug.LogError("CLIVERIFY outward 16029 fail threw: " + e); }
                Debug.Log("CLIVERIFY outward 16029 fail noThrow=" + lvUpFailNoThrow);

                // =========================================================================================
                // 轮24 PI:幻化(Illusion)全链数据层实证。16000/16001/16003/16004/16006-16012/16020/16022/
                // 16024/16027 合成包驱动反射喂包 + 事件断言 + 嵌套数组尾哨兵核对。
                // =========================================================================================

                System.Reflection.MethodInfo m16000 = ctrl.GetType().GetMethod("On16000", F);
                System.Reflection.MethodInfo m16001 = ctrl.GetType().GetMethod("On16001", F);
                System.Reflection.MethodInfo m16003 = ctrl.GetType().GetMethod("On16003", F);
                System.Reflection.MethodInfo m16004 = ctrl.GetType().GetMethod("On16004", F);
                System.Reflection.MethodInfo m16006 = ctrl.GetType().GetMethod("On16006", F);
                System.Reflection.MethodInfo m16007 = ctrl.GetType().GetMethod("On16007", F);
                System.Reflection.MethodInfo m16008 = ctrl.GetType().GetMethod("On16008", F);
                System.Reflection.MethodInfo m16009 = ctrl.GetType().GetMethod("On16009", F);
                System.Reflection.MethodInfo m16010 = ctrl.GetType().GetMethod("On16010", F);
                System.Reflection.MethodInfo m16011 = ctrl.GetType().GetMethod("On16011", F);
                System.Reflection.MethodInfo m16012 = ctrl.GetType().GetMethod("On16012", F);
                System.Reflection.MethodInfo m16020 = ctrl.GetType().GetMethod("On16020", F);
                System.Reflection.MethodInfo m16022 = ctrl.GetType().GetMethod("On16022", F);
                System.Reflection.MethodInfo m16024 = ctrl.GetType().GetMethod("On16024", F);
                System.Reflection.MethodInfo m16027 = ctrl.GetType().GetMethod("On16027", F);
                if (m16000 == null || m16001 == null || m16003 == null || m16004 == null || m16006 == null || m16007 == null
                    || m16008 == null || m16009 == null || m16010 == null || m16011 == null || m16012 == null
                    || m16020 == null || m16022 == null || m16024 == null || m16027 == null)
                {
                    Debug.LogError("CLIVERIFY outward illusion handlers missing (reflection)");
                    return 3;
                }
                Shenxiao.Framework.Net.NetReader FeedR(System.Reflection.MethodInfo m, byte[] pkt)
                {
                    var rr = new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length);
                    m.Invoke(ctrl, new object[] { rr });
                    return rr;
                }

                // ---- 0. 裁决6红线:发送侧 TypeId 必须严格等于 {1,2,3,4,5,12},严禁 6/7/8(协议层不可达) ----
                int[] allTypeIds = Shenxiao.Module.Core.OutWard.OutWardController.AllTypeIds;
                bool allTypeIdsOk = allTypeIds.Length == 6;
                foreach (int expect in new[] { 1, 2, 3, 4, 5, 12 }) { if (System.Array.IndexOf(allTypeIds, expect) < 0) allTypeIdsOk = false; }
                foreach (int forbidden in new[] { 6, 7, 8 }) { if (System.Array.IndexOf(allTypeIds, forbidden) >= 0) allTypeIdsOk = false; }
                Debug.Log("CLIVERIFY outward 裁决6红线 AllTypeIds=[" + string.Join(",", allTypeIds) + "] ok=" + allTypeIdsOk);

                // ---- 1. 幻化专属4张配表(config_mount_figure/_stage/_star/config_mount_skill) ----
                bool illuCfgLoaded = Shenxiao.Module.Core.OutWard.OutWardConfigs.IsIllusionConfigLoaded;
                string figName = Shenxiao.Module.Core.OutWard.OutWardConfigs.GetFigureName(1, 1, 1);
                Shenxiao.Module.Core.OutWard.OutWardConfigs.GetFigureActivateCost(1, 1, 1, out long actGoodsId, out long actGoodsNum);
                long maxBless = Shenxiao.Module.Core.OutWard.OutWardConfigs.GetFigureStageMaxBlessing(1, 1, 1);
                Newtonsoft.Json.Linq.JObject starRow = Shenxiao.Module.Core.OutWard.OutWardConfigs.GetFigureStarRow(1, 1, 1);
                bool illuCfgOk = illuCfgLoaded && figName == "金匮福猊" && actGoodsId == 16030109 && actGoodsNum == 30
                    && maxBless == 500 && starRow != null;
                Debug.Log("CLIVERIFY outward illusion config name=" + figName + " goods=" + actGoodsId + "x" + actGoodsNum
                    + " maxBless=" + maxBless + " starRow=" + (starRow != null) + " ok=" + illuCfgOk);

                // ---- 2. 16000 族错误出口(errcode==1600023 特判上限 / 其余通用) ----
                int genericErrCount = 0; int lastGenericErr = 0;
                System.Action<int> onGenericErr = code => { genericErrCount++; lastGenericErr = code; };
                int limitErrCount = 0; int lastLimitErr = 0;
                System.Action<int> onLimitErr = code => { limitErrCount++; lastLimitErr = code; };
                Shenxiao.Framework.Event.EventDispatcher.On<int>(Shenxiao.Framework.Event.GlobalEvent.EVT_OUTWARD_ERROR, onGenericErr);
                Shenxiao.Framework.Event.EventDispatcher.On<int>(Shenxiao.Framework.Event.GlobalEvent.EVT_OUTWARD_ACTIVE_LIMIT, onLimitErr);
                Feed(m16000, new CliVerify.Pkt().I(99999).Bytes());
                Feed(m16000, new CliVerify.Pkt().I(1600023).Bytes());
                Shenxiao.Framework.Event.EventDispatcher.Off<int>(Shenxiao.Framework.Event.GlobalEvent.EVT_OUTWARD_ERROR, onGenericErr);
                Shenxiao.Framework.Event.EventDispatcher.Off<int>(Shenxiao.Framework.Event.GlobalEvent.EVT_OUTWARD_ACTIVE_LIMIT, onLimitErr);
                bool b16000 = genericErrCount == 1 && lastGenericErr == 99999 && limitErrCount == 1 && lastLimitErr == 1600023;
                Debug.Log("CLIVERIFY outward 16000 generic=" + lastGenericErr + " limit=" + lastLimitErr + " ok=" + b16000);

                // ---- 3. 16001 场景外观变化广播(S2C only) ----
                int sceneEventCount = 0; int lastSceneType = 0; long lastSceneRole = 0;
                System.Action<int, long> onScene = (t, rid) => { sceneEventCount++; lastSceneType = t; lastSceneRole = rid; };
                Shenxiao.Framework.Event.EventDispatcher.On<int, long>(Shenxiao.Framework.Event.GlobalEvent.EVT_OUTWARD_SCENE_FIGURE_CHANGE, onScene);
                Feed(m16001, new CliVerify.Pkt().C(1).L(88800001).C(1).I(1019).H(300).Bytes());
                Shenxiao.Framework.Event.EventDispatcher.Off<int, long>(Shenxiao.Framework.Event.GlobalEvent.EVT_OUTWARD_SCENE_FIGURE_CHANGE, onScene);
                bool b16001 = sceneEventCount == 1 && lastSceneType == 1 && lastSceneRole == 88800001;
                Debug.Log("CLIVERIFY outward 16001 scene type=" + lastSceneType + " role=" + lastSceneRole + " ok=" + b16001);

                // ---- 4. 16003 幻化穿戴/取消:type=2(幻化)成功 / type=1(基础)成功 / 失败no-throw ----
                // 穿戴回包依赖 16006 已建立的幻化容器；先落一个空容器，再断言 IllusionId 真正写入而非只看 FigureStage。
                model.Apply16006(1, 0, 0, new List<Shenxiao.Module.Core.OutWard.OutWardModel.FigureBriefVo>());
                Feed(m16003, new CliVerify.Pkt().I(1).C(1).C(2).I(77).I(0).Bytes());   // type_id=1,type=2,args=77(figure_id)
                Shenxiao.Module.Core.OutWard.OutWardModel.OutWardVo vo1AfterWear = model.Get(1);
                bool b16003Type2 = vo1AfterWear != null && vo1AfterWear.FigureStage == 0
                    && model.GetIllusionList(1)?.IllusionId == 77;
                Feed(m16003, new CliVerify.Pkt().I(1).C(1).C(1).I(3).I(0).Bytes());    // type=1,args=3(figure_stage)
                vo1AfterWear = model.Get(1);
                bool b16003Type1 = vo1AfterWear != null && vo1AfterWear.FigureStage == 3
                    && model.GetIllusionList(1)?.IllusionId == 0;
                bool b16003FailNoThrow = true;
                try { Feed(m16003, new CliVerify.Pkt().I(5).C(1).C(2).I(0).I(0).Bytes()); }
                catch (System.Exception e) { b16003FailNoThrow = false; Debug.LogError("CLIVERIFY outward 16003 fail threw: " + e); }
                Debug.Log("CLIVERIFY outward 16003 type2ok=" + b16003Type2 + " type1ok=" + b16003Type1 + " failNoThrow=" + b16003FailNoThrow);

                // ---- 5. 16004 上/下坐骑:成功事件 / 失败no-throw ----
                int rideEventCount = 0; int lastRideType = 0; int lastRideMode = -1;
                System.Action<int, int> onRide = (t, md) => { rideEventCount++; lastRideType = t; lastRideMode = md; };
                Shenxiao.Framework.Event.EventDispatcher.On<int, int>(Shenxiao.Framework.Event.GlobalEvent.EVT_OUTWARD_RIDE_TOGGLE, onRide);
                Feed(m16004, new CliVerify.Pkt().I(1).C(1).C(1).Bytes());
                Shenxiao.Framework.Event.EventDispatcher.Off<int, int>(Shenxiao.Framework.Event.GlobalEvent.EVT_OUTWARD_RIDE_TOGGLE, onRide);
                bool b16004Ok = rideEventCount == 1 && lastRideType == 1 && lastRideMode == 1;
                bool b16004FailNoThrow = true;
                try { Feed(m16004, new CliVerify.Pkt().I(5).C(1).C(0).Bytes()); }
                catch (System.Exception e) { b16004FailNoThrow = false; Debug.LogError("CLIVERIFY outward 16004 fail threw: " + e); }
                Debug.Log("CLIVERIFY outward 16004 ok=" + b16004Ok + " failNoThrow=" + b16004FailNoThrow);

                // ---- 6. 16006 幻化形象列表(嵌套数组×2条 + 尾哨兵;type_id=1 触发自动补拉16007) ----
                byte[] p16006 = new CliVerify.Pkt()
                    .I(1).C(1).I(0).H(0)          // errcode,type_id,illusion_id=0,color_id=0
                    .H(2)                          // figure_list 计数
                    .I(1).C(1).H(0).I(0)           // figure#1: id=1,stage=1,star=0,end_time=0
                    .I(2).C(1).H(3).I(1700000000)  // figure#2: id=2,stage=1,star=3,end_time=1700000000
                    .I(135797531)                  // 尾哨兵
                    .Bytes();
                Shenxiao.Framework.Net.NetReader reader16006 = FeedR(m16006, p16006);
                bool b16006Sentinel = reader16006.Remaining == 4 && reader16006.ReadU32() == 135797531;
                Shenxiao.Module.Core.OutWard.OutWardModel.IllusionListVo illu1 = model.GetIllusionList(1);
                bool b16006Data = illu1 != null && illu1.IllusionId == 0 && illu1.ColorId == 0 && illu1.FigureList.Count == 2
                    && illu1.FigureList[0].Id == 1 && illu1.FigureList[1].Id == 2 && illu1.FigureList[1].Star == 3;
                Debug.Log("CLIVERIFY outward 16006 data=" + b16006Data + " sentinel=" + b16006Sentinel);

                // ---- 7. 16007 幻化形象详情(attr/skill/color 三个嵌套数组 + 尾哨兵;type_id=1,id=1) ----
                byte[] p16007 = new CliVerify.Pkt()
                    .I(1).C(1).I(1).C(1).H(0).I(500).I(1000).I(200).I(0)   // errcode..end_time
                    .H(1).C(1).I(999)                    // attr_list: {attr_id=1,val=999}
                    .H(2).I(59140030).I(59140031)         // skill_list: 仅 id
                    .H(1).H(1).I(2)                       // color_list: {color_id=1,color_lv=2}
                    .L(12345)                              // next_star_power
                    .I(246813579)                          // 尾哨兵
                    .Bytes();
                Shenxiao.Framework.Net.NetReader reader16007 = FeedR(m16007, p16007);
                bool b16007Sentinel = reader16007.Remaining == 4 && reader16007.ReadU32() == 246813579;
                Shenxiao.Module.Core.OutWard.OutWardModel.FigureDetailVo detail11 = model.GetFigureDetail(1, 1);
                bool b16007Data = detail11 != null && detail11.Stage == 1 && detail11.Combat == 1000 && detail11.StarCombat == 200
                    && detail11.Attrs.Count == 1 && detail11.Skills.Count == 2 && detail11.Skills[0] == 59140030
                    && detail11.ColorList.Count == 1 && detail11.NextStarPower == 12345;
                Debug.Log("CLIVERIFY outward 16007 data=" + b16007Data + " sentinel=" + b16007Sentinel);

                // ---- 8. 16008 激活形象:成功事件(补拉16006)/ 失败no-throw ----
                int activateEventCount = 0; int lastActivateType = 0; int lastActivateId = 0;
                System.Action<int, int> onActivate = (t, id) => { activateEventCount++; lastActivateType = t; lastActivateId = id; };
                Shenxiao.Framework.Event.EventDispatcher.On<int, int>(Shenxiao.Framework.Event.GlobalEvent.EVT_OUTWARD_FIGURE_ACTIVATED, onActivate);
                Feed(m16008, new CliVerify.Pkt().I(1).C(1).I(3).I(1500).Bytes());
                Shenxiao.Framework.Event.EventDispatcher.Off<int, int>(Shenxiao.Framework.Event.GlobalEvent.EVT_OUTWARD_FIGURE_ACTIVATED, onActivate);
                bool b16008Ok = activateEventCount == 1 && lastActivateType == 1 && lastActivateId == 3;
                bool b16008FailNoThrow = true;
                try { Feed(m16008, new CliVerify.Pkt().I(5).C(1).I(3).I(0).Bytes()); }
                catch (System.Exception e) { b16008FailNoThrow = false; Debug.LogError("CLIVERIFY outward 16008 fail threw: " + e); }
                Debug.Log("CLIVERIFY outward 16008 ok=" + b16008Ok + " failNoThrow=" + b16008FailNoThrow);

                // ---- 9. 16009 幻化升阶:成功事件(嵌套ratio_list,补拉16006)/ 失败no-throw ----
                int stageEventCount = 0; int lastStageType = 0; int lastStageId = 0;
                System.Action<int, int> onStage = (t, id) => { stageEventCount++; lastStageType = t; lastStageId = id; };
                Shenxiao.Framework.Event.EventDispatcher.On<int, int>(Shenxiao.Framework.Event.GlobalEvent.EVT_OUTWARD_FIGURE_STAGE_UP, onStage);
                byte[] p16009Ok = new CliVerify.Pkt().I(1).C(1).I(3).C(2).I(600).I(0).H(1).C(1).H(50).I(16030109).Bytes();
                Feed(m16009, p16009Ok);
                Shenxiao.Framework.Event.EventDispatcher.Off<int, int>(Shenxiao.Framework.Event.GlobalEvent.EVT_OUTWARD_FIGURE_STAGE_UP, onStage);
                bool b16009Ok = stageEventCount == 1 && lastStageType == 1 && lastStageId == 3;
                bool b16009FailNoThrow = true;
                try { Feed(m16009, new CliVerify.Pkt().I(5).C(1).I(3).C(0).I(0).I(0).H(0).I(0).Bytes()); }
                catch (System.Exception e) { b16009FailNoThrow = false; Debug.LogError("CLIVERIFY outward 16009 fail threw: " + e); }
                Debug.Log("CLIVERIFY outward 16009 ok=" + b16009Ok + " failNoThrow=" + b16009FailNoThrow);

                // ---- 10. 16010 使用魔晶:成功(补拉16011+16002)/失败,均只需 no-throw ----
                bool b16010OkNoThrow = true;
                try { Feed(m16010, new CliVerify.Pkt().I(1).C(1).I(16030109).Bytes()); }
                catch (System.Exception e) { b16010OkNoThrow = false; Debug.LogError("CLIVERIFY outward 16010 ok threw: " + e); }
                bool b16010FailNoThrow = true;
                try { Feed(m16010, new CliVerify.Pkt().I(5).C(1).I(16030109).Bytes()); }
                catch (System.Exception e) { b16010FailNoThrow = false; Debug.LogError("CLIVERIFY outward 16010 fail threw: " + e); }
                Debug.Log("CLIVERIFY outward 16010 okNoThrow=" + b16010OkNoThrow + " failNoThrow=" + b16010FailNoThrow);

                // ---- 11. 16011 魔晶使用次数(嵌套数组 + 尾哨兵,无errcode字段) ----
                byte[] p16011 = new CliVerify.Pkt()
                    .C(1).H(2)
                    .I(16030109).I(3).I(10)
                    .I(16030110).I(0).I(5)
                    .I(864297531)   // 尾哨兵
                    .Bytes();
                Shenxiao.Framework.Net.NetReader reader16011 = FeedR(m16011, p16011);
                bool b16011Sentinel = reader16011.Remaining == 4 && reader16011.ReadU32() == 864297531;
                var counters1 = model.GetCrystalCounters(1);
                bool b16011Data = counters1 != null && counters1.Count == 2
                    && counters1[0].goodsId == 16030109 && counters1[0].times == 3 && counters1[0].timesLim == 10;
                Debug.Log("CLIVERIFY outward 16011 data=" + b16011Data + " sentinel=" + b16011Sentinel);

                // ---- 12. 16012 到期删除推送(纯下行,双删:形象列表 + 详情缓存) ----
                Feed(m16012, new CliVerify.Pkt().C(1).C(1).Bytes());
                Shenxiao.Module.Core.OutWard.OutWardModel.IllusionListVo illu1AfterExpire = model.GetIllusionList(1);
                bool b16012Removed = illu1AfterExpire != null && illu1AfterExpire.FigureList.TrueForAll(f => f.Id != 1)
                    && model.GetFigureDetail(1, 1) == null;
                Debug.Log("CLIVERIFY outward 16012 removed=" + b16012Removed);

                // ---- 13. 16020 幻化升星:成功原地patch(figure id=2,由16006建立) / 失败no-throw ----
                int starUpEventCount = 0; int lastStarUpType = 0; int lastStarUpId = 0;
                System.Action<int, int> onStarUp = (t, id) => { starUpEventCount++; lastStarUpType = t; lastStarUpId = id; };
                Shenxiao.Framework.Event.EventDispatcher.On<int, int>(Shenxiao.Framework.Event.GlobalEvent.EVT_OUTWARD_FIGURE_STAR_UP, onStarUp);
                Feed(m16020, new CliVerify.Pkt().I(1).C(1).I(2).H(5).Bytes());
                Shenxiao.Framework.Event.EventDispatcher.Off<int, int>(Shenxiao.Framework.Event.GlobalEvent.EVT_OUTWARD_FIGURE_STAR_UP, onStarUp);
                Shenxiao.Module.Core.OutWard.OutWardModel.IllusionListVo illu1AfterStar = model.GetIllusionList(1);
                Shenxiao.Module.Core.OutWard.OutWardModel.FigureBriefVo brief2 = illu1AfterStar?.FigureList.Find(f => f.Id == 2);
                bool b16020Ok = starUpEventCount == 1 && lastStarUpId == 2 && brief2 != null && brief2.Star == 5;
                bool b16020FailNoThrow = true;
                try { Feed(m16020, new CliVerify.Pkt().I(5).C(1).I(2).H(0).Bytes()); }
                catch (System.Exception e) { b16020FailNoThrow = false; Debug.LogError("CLIVERIFY outward 16020 fail threw: " + e); }
                Debug.Log("CLIVERIFY outward 16020 ok=" + b16020Ok + " failNoThrow=" + b16020FailNoThrow);

                // ---- 14. 16022 幻化战力预览(无errcode)+ "选中未缓存才请求"缓存门槛的缓存态验证 ----
                Feed(m16022, new CliVerify.Pkt().C(1).C(2).L(3000).L(1200).L(1600).Bytes());
                bool b16022Data = model.LastFightPreview.TypeId == 1 && model.LastFightPreview.FigureId == 2
                    && model.LastFightPreview.Power == 3000 && model.LastFightPreview.StarCombat == 1200
                    && model.LastFightPreview.NextStarPower == 1600;
                Debug.Log("CLIVERIFY outward 16022 data=" + b16022Data);
                // 缓存门槛:给 type=2,id=1 建一份 16007 详情缓存后,RequestFightPreview 应据此跳过实际发送
                // (SendFmt 本身在此黑盒用例里不可断言"是否被跳过",跳过分支已随源码人工核对,见 Controller 注释)。
                byte[] p16007ForType2 = new CliVerify.Pkt().I(1).C(2).I(1).C(1).H(0).I(0).I(0).I(0).I(0).H(0).H(0).H(0).L(0).Bytes();
                Feed(m16007, p16007ForType2);
                bool b16022GateCachePresent = model.GetFigureDetail(2, 1) != null;
                bool b16022GateNoThrow = true;
                try { Shenxiao.Module.Core.OutWard.OutWardController.Instance.RequestFightPreview(2, 1); }
                catch (System.Exception e) { b16022GateNoThrow = false; Debug.LogError("CLIVERIFY outward RequestFightPreview(cached) threw: " + e); }
                Debug.Log("CLIVERIFY outward 16022 gate cachePresent=" + b16022GateCachePresent + " noThrow=" + b16022GateNoThrow);

                // ---- 15. 16024 自动购买(type=1 的 vo 已由本用例最早的 16002 feed 建立) ----
                Feed(m16024, new CliVerify.Pkt().I(1).C(1).C(1).Bytes());
                Shenxiao.Module.Core.OutWard.OutWardModel.OutWardVo vo1AfterAutoBuy = model.Get(1);
                bool b16024Ok = vo1AfterAutoBuy != null && vo1AfterAutoBuy.AutoBuy == 1;
                Debug.Log("CLIVERIFY outward 16024 autoBuy=" + (vo1AfterAutoBuy?.AutoBuy ?? -1) + " ok=" + b16024Ok);

                // ---- 16. 16027 幻化升星战力预览(无errcode,无star_combat字段) ----
                Feed(m16027, new CliVerify.Pkt().C(1).C(2).L(4000).L(4800).Bytes());
                bool b16027Data = model.LastStarFightPreview.TypeId == 1 && model.LastStarFightPreview.FigureId == 2
                    && model.LastStarFightPreview.Power == 4000 && model.LastStarFightPreview.NextStarPower == 4800
                    && model.LastStarFightPreview.StarCombat == 0;
                Debug.Log("CLIVERIFY outward 16027 data=" + b16027Data);

                bool illusionPass = registrationOk && gameStartRequestsOk && setTypeRequestsOk
                    && allTypeIdsOk && illuCfgOk && b16000 && b16001
                    && b16003Type2 && b16003Type1 && b16003FailNoThrow && b16004Ok && b16004FailNoThrow
                    && b16006Data && b16006Sentinel && b16007Data && b16007Sentinel
                    && b16008Ok && b16008FailNoThrow && b16009Ok && b16009FailNoThrow
                    && b16010OkNoThrow && b16010FailNoThrow && b16011Data && b16011Sentinel && b16012Removed
                    && b16020Ok && b16020FailNoThrow && b16022Data && b16022GateCachePresent && b16022GateNoThrow
                    && b16024Ok && b16027Data;
                Debug.Log("CLIVERIFY outward illusion VERDICT registration=" + registrationOk
                    + " gameStartRequests=" + gameStartRequestsOk + " setTypeRequests=" + setTypeRequestsOk
                    + " allTypeIdsOk=" + allTypeIdsOk + " illuCfgOk=" + illuCfgOk
                    + " b16000=" + b16000 + " b16001=" + b16001 + " b16003=" + (b16003Type2 && b16003Type1 && b16003FailNoThrow)
                    + " b16004=" + (b16004Ok && b16004FailNoThrow) + " b16006=" + (b16006Data && b16006Sentinel)
                    + " b16007=" + (b16007Data && b16007Sentinel) + " b16008=" + (b16008Ok && b16008FailNoThrow)
                    + " b16009=" + (b16009Ok && b16009FailNoThrow) + " b16010=" + (b16010OkNoThrow && b16010FailNoThrow)
                    + " b16011=" + (b16011Data && b16011Sentinel) + " b16012=" + b16012Removed
                    + " b16020=" + (b16020Ok && b16020FailNoThrow) + " b16022=" + (b16022Data && b16022GateCachePresent && b16022GateNoThrow)
                    + " b16024=" + b16024Ok + " b16027=" + b16027Data + " illusionPass=" + illusionPass);

                Shenxiao.Module.Core.OutWard.OutWardShellView.Show();
                await Task.Delay(400);
                stage.ForceCjkFont();
                string png = stage.Capture("Temp/round16_outward_shell.png");

                Transform row0 = CliVerify.FindDeep(stage.CanvasRoot, "Row0");
                TMP_Text row0Label = row0 != null ? row0.GetComponentInChildren<TMP_Text>(true) : null;
                bool rowOk = row0Label != null && !string.IsNullOrEmpty(row0Label.text) && row0Label.text.Contains("1阶2星");
                Transform starBtn = CliVerify.FindDeep(stage.CanvasRoot, "Btn升星");
                bool starBtnOk = starBtn != null && starBtn.gameObject.activeInHierarchy;
                Debug.Log("CLIVERIFY outward shell rowOk=" + rowOk + " starBtn=" + starBtnOk + " shot=" + png);

                // 登出/Dispose 会调 Clear；两类瞬时战力预览也必须归零，不能串到下一角色/下一会话。
                model.Clear();
                bool clearPreviewOk = model.LastFightPreview.TypeId == 0 && model.LastFightPreview.FigureId == 0
                    && model.LastFightPreview.Power == 0 && model.LastFightPreview.StarCombat == 0 && model.LastFightPreview.NextStarPower == 0
                    && model.LastStarFightPreview.TypeId == 0 && model.LastStarFightPreview.FigureId == 0
                    && model.LastStarFightPreview.Power == 0 && model.LastStarFightPreview.StarCombat == 0
                    && model.LastStarFightPreview.NextStarPower == 0;
                Debug.Log("CLIVERIFY outward clear preview reset=" + clearPreviewOk);

                bool pass = wingConfigOk && sharedConsumerShapeConfigOk && wingLevelSkillConfigOk && oneKeyProjectionOk
                    && effectStateOk && fairySemanticsOk && refreshMatrixOk && productionSourceContractOk
                    && productionReparentContractOk && illusionSemanticContractOk && quickBuyContractOk
                    && infoOk && starUpOk && starUpFailNoThrow && lvPanelOk && lvUpOk && lvUpFailNoThrow && rowOk && starBtnOk
                    && illusionPass && clearPreviewOk;
                Debug.Log("CLIVERIFY outward VERDICT wingConfigOk=" + wingConfigOk
                    + " sharedConsumerShapeConfigOk=" + sharedConsumerShapeConfigOk
                    + " infoOk=" + infoOk + " starUpOk=" + starUpOk
                    + " starUpFailNoThrow=" + starUpFailNoThrow + " lvPanelOk=" + lvPanelOk + " lvUpOk=" + lvUpOk
                    + " lvUpFailNoThrow=" + lvUpFailNoThrow + " rowOk=" + rowOk + " starBtnOk=" + starBtnOk
                    + " illusionPass=" + illusionPass + " pass=" + pass);

                Shenxiao.Module.Core.OutWard.OutWardShellView.Close();
                Shenxiao.Module.Core.OutWard.OutWardModel.Instance.Clear();
                return pass ? 0 : 3;
            }
            finally
            {
                stage.Dispose();
            }
        }

        private static bool VerifyProductionReparentContract()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Pet/PetModule.prefab");
            if (prefab == null) return false;
            Shenxiao.Module.Core.Pet.IllusionBaseView prefabIllusion =
                prefab.GetComponentInChildren<Shenxiao.Module.Core.Pet.IllusionBaseView>(true);
            Shenxiao.Module.Core.Pet.OutwardLvSystemView prefabLevel =
                prefab.GetComponentInChildren<Shenxiao.Module.Core.Pet.OutwardLvSystemView>(true);
            if (prefabIllusion == null || prefabIllusion.gameObject.activeSelf
                || prefabLevel == null || prefabLevel.gameObject.activeSelf) return false;

            GameObject instance = Object.Instantiate(prefab);
            GameObject sharedHost = new GameObject("OutWardCase.SharedHost", typeof(RectTransform));
            try
            {
                Shenxiao.Module.Core.Pet.OutWardBaseView outward =
                    instance.GetComponentInChildren<Shenxiao.Module.Core.Pet.OutWardBaseView>(true);
                Shenxiao.Module.Core.Pet.IllusionBaseView illusion =
                    instance.GetComponentInChildren<Shenxiao.Module.Core.Pet.IllusionBaseView>(true);
                Shenxiao.Module.Core.Pet.OutwardLvSystemView level =
                    instance.GetComponentInChildren<Shenxiao.Module.Core.Pet.OutwardLvSystemView>(true);
                Shenxiao.Generated.UI.Common.FairyWishEnterBtnBind fairy =
                    instance.GetComponentInChildren<Shenxiao.Generated.UI.Common.FairyWishEnterBtnBind>(true);
                if (outward == null || illusion == null || level == null || fairy == null || fairy.img_btn == null
                    || fairy.img_red == null || fairy.box_pop == null || fairy.effect_con == null
                    || fairy.htmlContent == null || level.lv_skill_group == null || level.lv_exp_group == null
                    || level.btn_group3 == null || level.img_btn_group3 == null || level.goods_group == null
                    || level._tpl_PetRoundItem == null || level._tpl_BaseAwardItem == null) return false;
                Transform originalParent = illusion.transform.parent;
                Transform levelOriginalParent = level.transform.parent;
                const System.Reflection.BindingFlags privateInstance =
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                typeof(Shenxiao.Module.Core.Pet.OutWardBaseView).GetField("_illusionView", privateInstance)
                    ?.SetValue(outward, null);
                typeof(Shenxiao.Module.Core.Pet.OutWardBaseView).GetField("_illusionOriginalParent", privateInstance)
                    ?.SetValue(outward, null);
                typeof(Shenxiao.Module.Core.Pet.OutWardBaseView).GetField("_illusionOriginalSiblingIndex", privateInstance)
                    ?.SetValue(outward, -1);
                typeof(Shenxiao.Module.Core.Pet.OutWardBaseView).GetField("_levelSystemView", privateInstance)
                    ?.SetValue(outward, null);
                typeof(Shenxiao.Module.Core.Pet.OutWardBaseView).GetField("_levelSystemOriginalParent", privateInstance)
                    ?.SetValue(outward, null);
                typeof(Shenxiao.Module.Core.Pet.OutWardBaseView).GetField("_levelSystemOriginalSiblingIndex", privateInstance)
                    ?.SetValue(outward, -1);
                instance.SetActive(false);
                bool inactiveRootCapture = outward.CaptureSiblingIllusion() && !illusion.gameObject.activeSelf;
                bool inactiveLevelCapture = outward.CaptureSiblingLevelSystem() && !level.gameObject.activeSelf;
                outward.transform.SetParent(sharedHost.transform, false);
                bool moved = outward.PrepareIllusionHost(sharedHost.transform)
                    && illusion.transform.parent == sharedHost.transform;
                bool levelMoved = outward.PrepareLevelSystemHost()
                    && level.transform.parent == outward.donw_group_2;
                outward.RestoreCapturedIllusion();
                outward.RestoreCapturedLevelSystem();
                bool restored = illusion.transform.parent == originalParent && !illusion.gameObject.activeSelf;
                bool levelRestored = level.transform.parent == levelOriginalParent && !level.gameObject.activeSelf;
                return inactiveRootCapture && inactiveLevelCapture && moved && levelMoved && restored && levelRestored;
            }
            finally
            {
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(sharedHost);
            }
        }

        private static bool VerifyIllusionSemanticContract()
        {
            var inactive = new Shenxiao.Module.Core.OutWard.OutWardModel.IllusionFigureState
                { TypeId = 3, FigureId = 1, Activated = false, Stage = 0, Detail = null };
            var activeMissing = new Shenxiao.Module.Core.OutWard.OutWardModel.IllusionFigureState
                { TypeId = 3, FigureId = 1, Activated = true, Stage = 1, Detail = null };
            bool requestGate = !Shenxiao.Module.Core.Pet.IllusionBaseView.ShouldRequestActivatedDetail(inactive)
                && Shenxiao.Module.Core.Pet.IllusionBaseView.ShouldRequestActivatedDetail(activeMissing);
            bool commandArgs = Shenxiao.Module.Core.Pet.IllusionBaseView.ResolveUnwearStage(null) == 1
                && Shenxiao.Module.Core.Pet.IllusionBaseView.ResolveUnwearStage(
                    new Shenxiao.Module.Core.OutWard.OutWardModel.OutWardVo { Stage = 6 }) == 6
                && Shenxiao.Module.Core.Pet.IllusionBaseView.StageUpgradeGoodsId == 0;
            bool wingMode = Shenxiao.Module.Core.Pet.IllusionBaseView.UsesGoodsBasedStage(3)
                && !Shenxiao.Module.Core.Pet.IllusionBaseView.SupportsStarEntry(3);
            bool gridProjection = Mathf.Approximately(
                Shenxiao.Module.Core.Pet.IllusionBaseView.ComputeIllusionGridHeight(19), 700f);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Pet/PetModule.prefab");
            Shenxiao.Module.Core.Pet.IllusionBaseView view =
                prefab?.GetComponentInChildren<Shenxiao.Module.Core.Pet.IllusionBaseView>(true);
            GridLayoutGroup grid = view?.illusion_group?.GetComponent<GridLayoutGroup>();
            ContentSizeFitter illusionFitter = view?.illusion_group?.GetComponent<ContentSizeFitter>();
            ContentSizeFitter propFitter = view?.prop_group?.GetComponent<ContentSizeFitter>();
            bool prefabLayout = view != null && view.illusion_scroller != null
                && view.illusion_scroller.content == view.illusion_group
                && grid != null && grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount
                && grid.constraintCount == 3 && grid.cellSize == new Vector2(220f, 94f)
                && grid.spacing == new Vector2(4f, 7f)
                && illusionFitter != null && illusionFitter.verticalFit == ContentSizeFitter.FitMode.PreferredSize
                && view.prop_scroller != null && view.prop_scroller.content == view.prop_group
                && propFitter != null && propFitter.verticalFit == ContentSizeFitter.FitMode.PreferredSize;

            IReadOnlyList<int> figures = Shenxiao.Module.Core.OutWard.OutWardConfigs.GetFigureIds(3, 1);
            if (figures.Count == 0) return false;
            int figureId = figures[0];
            var attrsInactive = Shenxiao.Module.Core.OutWard.OutWardModel.Instance
                .GetIllusionAttributeRows(3, figureId, 0);
            var attrsActive = Shenxiao.Module.Core.OutWard.OutWardModel.Instance
                .GetIllusionAttributeRows(3, figureId, 1, new Shenxiao.Module.Core.OutWard.OutWardModel.FigureDetailVo
                { TypeId = 3, Id = figureId, Stage = 1, Attrs = new List<(int attrId, long val)>() });
            var skillsInactive = Shenxiao.Module.Core.OutWard.OutWardModel.Instance
                .GetIllusionSkillRows(3, figureId, 1, 0);
            var skillsActive = Shenxiao.Module.Core.OutWard.OutWardModel.Instance
                .GetIllusionSkillRows(3, figureId, 1, 300);
            bool attributeProjection = attrsInactive.Count > 0 && attrsActive.Count > 0
                && attrsInactive[0].CurrentValue == 0 && !string.IsNullOrEmpty(attrsInactive[0].Name)
                && !attrsInactive[0].Name.StartsWith("属性") && !string.IsNullOrEmpty(attrsInactive[0].NextText);
            bool skillProjection = skillsInactive.Count > 0 && skillsActive.Count == skillsInactive.Count;
            for (int i = 0; i < skillsInactive.Count && skillProjection; i++)
                skillProjection &= skillsInactive[i].SkillId > 0 && !string.IsNullOrEmpty(skillsInactive[i].Name)
                    && !string.IsNullOrEmpty(skillsInactive[i].Icon) && skillsInactive[i].Locked
                    && !skillsActive[i].Locked;
            return requestGate && commandArgs && wingMode && gridProjection && prefabLayout
                && attributeProjection && skillProjection;
        }
    }
}
