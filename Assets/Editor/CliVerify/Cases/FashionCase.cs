using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 时装(Fashion,pt_413)实证,第21轮 PA:反射喂合成包驱动 FashionController 的 8 个私有 handler +
    /// 仅活下行的 41311,断言 FashionModel 逐号落地正确、失败分支不抛异常;再拉起 FashionFlow 渲染断言
    /// FashionMainView 真实挂上业务子类(而非裸 Bind)且 UI 文本落了真数据(非空)。
    /// 独立文件复用 CliVerify.Stage/Pkt/FindDeep(已 public),不改 CliVerify.cs 本体(RenderAll 接线留给
    /// 后续碰 CliVerify.cs 的人,本轮 PG 也在动这个文件,不抢改)。
    /// </summary>
    public static class FashionCase
    {
        private const int POS = 1;
        private const int FASHION_ID = 12010001; // 真实配置样本(config_fashion "1@12010001@1" 已实读存在)

        public static async Task<int> Run()
        {
            CliVerify.Stage stage = CliVerify.Stage.Create();
            try
            {
                Shenxiao.EditorTools.ConfigGen.ClientConfigSync.SyncIfStale(true);
                await Shenxiao.Module.Core.Fashion.FashionConfigs.EnsureLoaded();
                await Shenxiao.Module.Core.Common.GoodsModel.EnsureLoaded();
                if (!Shenxiao.Module.Core.Fashion.FashionConfigs.IsLoaded)
                {
                    Debug.LogError("CLIVERIFY FAIL config_fashion not loaded");
                    return 3;
                }

                IReadOnlyList<Shenxiao.Module.Core.Fashion.FashionConfigs.PositionRow> position1 =
                    Shenxiao.Module.Core.Fashion.FashionConfigs.GetPositionRows(1);
                IReadOnlyList<Shenxiao.Module.Core.Fashion.FashionConfigs.PositionRow> position2 =
                    Shenxiao.Module.Core.Fashion.FashionConfigs.GetPositionRows(2);
                IReadOnlyList<Shenxiao.Module.Core.Fashion.FashionConfigs.PositionRow> position3 =
                    Shenxiao.Module.Core.Fashion.FashionConfigs.GetPositionRows(3);
                IReadOnlyList<Shenxiao.Module.Core.Fashion.FashionConfigs.SuitRow> suits =
                    Shenxiao.Module.Core.Fashion.FashionConfigs.GetSuits();
                int suitStarCount = 0;
                for (int suitId = 1; suitId <= 4; suitId++)
                    suitStarCount += Shenxiao.Module.Core.Fashion.FashionConfigs.GetSuitStars(suitId).Count;
                Shenxiao.Module.Core.Fashion.FashionConfigs.PositionRow positionSample =
                    Shenxiao.Module.Core.Fashion.FashionConfigs.GetPositionRow(1, 3);
                Shenxiao.Module.Core.Fashion.FashionConfigs.SuitStarRow suitStarSample =
                    Shenxiao.Module.Core.Fashion.FashionConfigs.GetSuitStar(1, 2);
                bool secondKnifeConfigOk = position1.Count + position2.Count + position3.Count == 3003
                    && position1.Count == 1001 && position2.Count == 1001 && position3.Count == 1001
                    && suits.Count == 4 && suitStarCount == 40
                    && positionSample != null && positionSample.Cost == 250 && positionSample.AttrAdds.Count == 3
                    && suits[0].Id == 1 && suits[0].Conditions.Count == 4
                    && suitStarSample != null && suitStarSample.StarId == 2 && suitStarSample.Conditions.Count == 4
                    && suitStarSample.Conditions[0].Slot == 1 && suitStarSample.Conditions[0].RequiredLevel == 2;
                Debug.Log("CLIVERIFY fashion secondKnife config pos="
                    + (position1.Count + position2.Count + position3.Count) + " suit=" + suits.Count
                    + " suitStar=" + suitStarCount + " sampleCost=" + (positionSample?.Cost ?? -1)
                    + " ok=" + secondKnifeConfigOk);

                Shenxiao.Module.Core.Fashion.FashionController ctrl =
                    Shenxiao.Module.Core.Fashion.FashionController.Instance;
                const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
                MethodInfo m41300 = ctrl.GetType().GetMethod("On41300", F);
                MethodInfo m41301 = ctrl.GetType().GetMethod("On41301", F);
                MethodInfo m41302 = ctrl.GetType().GetMethod("On41302", F);
                MethodInfo m41303 = ctrl.GetType().GetMethod("On41303", F);
                MethodInfo m41304 = ctrl.GetType().GetMethod("On41304", F);
                MethodInfo m41306 = ctrl.GetType().GetMethod("On41306", F);
                MethodInfo m41312 = ctrl.GetType().GetMethod("On41312", F);
                MethodInfo m41316 = ctrl.GetType().GetMethod("On41316", F);
                MethodInfo m41311 = ctrl.GetType().GetMethod("On41311", F);
                MethodInfo m41305 = ctrl.GetType().GetMethod("On41305", F);
                MethodInfo m41313 = ctrl.GetType().GetMethod("On41313", F);
                MethodInfo m41314 = ctrl.GetType().GetMethod("On41314", F);
                MethodInfo m41315 = ctrl.GetType().GetMethod("On41315", F);
                if (m41300 == null || m41301 == null || m41302 == null || m41303 == null || m41304 == null
                    || m41305 == null || m41306 == null || m41311 == null || m41312 == null
                    || m41313 == null || m41314 == null || m41315 == null || m41316 == null)
                {
                    Debug.LogError("CLIVERIFY fashion handlers missing (reflection)");
                    return 3;
                }

                bool secondKnifeC2sOk = false;
                FieldInfo outboundField = ctrl.GetType().GetField("s_outboundIntercept",
                    BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo candidateCountMethod = typeof(Shenxiao.Module.Core.Fashion.FashionLevelView)
                    .GetMethod("GetCandidateCount", BindingFlags.NonPublic | BindingFlags.Static);
                if (outboundField != null && candidateCountMethod != null)
                {
                    object oldOutbound = outboundField.GetValue(null);
                    var frames = new List<byte[]>();
                    try
                    {
                        outboundField.SetValue(null, new Func<byte[], bool>(frame =>
                        {
                            frames.Add(frame);
                            return true;
                        }));

                        // 用真实背包实例字段验证：194 点缺口、单件 100 经验时只取 2 件，而不是整堆 30000 件。
                        var bagGoods = new Shenxiao.Module.Core.Bag.BagGoods
                        {
                            GoodsId = 0x0102030405060708L,
                            TypeId = 12010001,
                            GoodsNum = 30000,
                        };
                        int safeCount = (int)candidateCountMethod.Invoke(null, new object[] { 194L, 100, bagGoods.GoodsNum });
                        ctrl.UpgradePosition(POS, new List<(long goodsId, int num)> { (bagGoods.GoodsId, safeCount) });
                        bool c2s41305 = frames.Count == 1 && FrameEquals(frames[0],
                            Shenxiao.Framework.Net.Proto.FASHION_POSITION_UPGRADE,
                            new CliVerify.Pkt().C(POS).H(1).L(bagGoods.GoodsId).H(2).Bytes());

                        frames.Clear();
                        ctrl.RequestSuitInfo();
                        bool c2s41313 = frames.Count == 1 && FrameEquals(frames[0],
                            Shenxiao.Framework.Net.Proto.FASHION_SUIT_INFO, Array.Empty<byte>());

                        frames.Clear();
                        ctrl.ActivateSuit(1, 4);
                        bool c2s41314 = frames.Count == 1 && FrameEquals(frames[0],
                            Shenxiao.Framework.Net.Proto.FASHION_SUIT_ACTIVATE,
                            new CliVerify.Pkt().C(1).C(4).Bytes());

                        frames.Clear();
                        ctrl.UpgradeSuit(1);
                        bool c2s41315 = frames.Count == 1 && FrameEquals(frames[0],
                            Shenxiao.Framework.Net.Proto.FASHION_SUIT_UPGRADE,
                            new CliVerify.Pkt().C(1).Bytes());

                        frames.Clear();
                        ctrl.UpgradePosition(2, new List<(long goodsId, int num)> { (bagGoods.GoodsId, 1) });
                        ctrl.UpgradePosition(POS, new List<(long goodsId, int num)> { (0L, 1) });
                        ctrl.ActivateSuit(1, 3);
                        ctrl.UpgradeSuit(0);
                        bool guardsOk = frames.Count == 0;
                        secondKnifeC2sOk = safeCount == 2 && c2s41305 && c2s41313 && c2s41314 && c2s41315 && guardsOk;
                        Debug.Log("CLIVERIFY fashion secondKnife C2S safeCount=" + safeCount
                            + " 41305=" + c2s41305 + " 41313=" + c2s41313 + " 41314=" + c2s41314
                            + " 41315=" + c2s41315 + " guards=" + guardsOk + " ok=" + secondKnifeC2sOk);
                    }
                    finally
                    {
                        outboundField.SetValue(null, oldOutbound);
                    }
                }
                else
                {
                    Debug.LogError("CLIVERIFY fashion secondKnife request/safe-count probe missing");
                }
                void Feed(MethodInfo m, byte[] pkt) =>
                    m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });

                var model = Shenxiao.Module.Core.Fashion.FashionModel.Instance;
                model.Clear();

                // 41300 全量:Code=1,pos1 一条,fashion_list 空(对标"该位无任何时装已激活"的冷启动态)
                byte[] p41300 = new CliVerify.Pkt()
                    .I(1)          // Code
                    .H(1)          // PosList 计数
                    .C(POS).I(0).H(0).I(0)  // PosId, WearFashionId, PosLv, PosUpgradeNum
                    .H(0)          // FashionList 计数(空)
                    .Bytes();
                Feed(m41300, p41300);
                Shenxiao.Module.Core.Fashion.FashionModel.PosInfo pos = model.GetPos(POS);
                bool infoOk = pos != null && pos.WearFashionId == 0 && !model.IsActivated(POS, FASHION_ID);
                Debug.Log("CLIVERIFY fashion 41300 pos=" + POS + " wear=" + (pos?.WearFashionId ?? -1) + " ok=" + infoOk);

                // 41305 success updates the existing position; a non-success Code must leave it unchanged.
                Feed(m41305, new CliVerify.Pkt().I(1).C(POS).H(3).I(456).Bytes());
                pos = model.GetPos(POS);
                bool posUpgradeOk = pos != null && pos.PosLv == 3 && pos.PosUpgradeNum == 456;
                Feed(m41305, new CliVerify.Pkt().I(5).C(POS).H(9).I(999).Bytes());
                pos = model.GetPos(POS);
                bool posUpgradeFailStable = pos != null && pos.PosLv == 3 && pos.PosUpgradeNum == 456;
                Debug.Log("CLIVERIFY fashion 41305 success=" + posUpgradeOk + " failStable=" + posUpgradeFailStable
                    + " lv=" + (pos?.PosLv ?? -1) + " exp=" + (pos?.PosUpgradeNum ?? -1));

                // 41304 激活成功:Code=1,PosId,FashionId → 内部会自动 SendFmt(41302)(未连接只是 fire-and-forget,无害)
                byte[] p41304 = new CliVerify.Pkt().I(1).C(POS).I(FASHION_ID).Bytes();
                Feed(m41304, p41304);
                var entry = model.GetActive(POS, FASHION_ID);
                bool activateOk = entry != null && entry.StarLv == 1 && entry.NowColorId == 0;
                Debug.Log("CLIVERIFY fashion 41304 activate star=" + (entry?.StarLv ?? -1) + " ok=" + activateOk);

                // 41302 穿戴成功:Code=1,PosId,FashionId,ColorId=0
                byte[] p41302 = new CliVerify.Pkt().I(1).C(POS).I(FASHION_ID).C(0).Bytes();
                Feed(m41302, p41302);
                pos = model.GetPos(POS);
                bool wearOk = pos != null && pos.WearFashionId == FASHION_ID;
                Debug.Log("CLIVERIFY fashion 41302 wear=" + (pos?.WearFashionId ?? -1) + " ok=" + wearOk);

                // 41303 卸下成功:Code=1,PosId,FashionId
                byte[] p41303 = new CliVerify.Pkt().I(1).C(POS).I(FASHION_ID).Bytes();
                Feed(m41303, p41303);
                pos = model.GetPos(POS);
                bool takeOffOk = pos != null && pos.WearFashionId == 0;
                Debug.Log("CLIVERIFY fashion 41303 takeoff wear=" + (pos?.WearFashionId ?? -1) + " ok=" + takeOffOk);

                // 41301 解锁颜色1成功:Code=1,PosId,FashionId,ColorId=1,Type=2
                byte[] p41301 = new CliVerify.Pkt().I(1).C(POS).I(FASHION_ID).C(1).C(2).Bytes();
                Feed(m41301, p41301);
                entry = model.GetActive(POS, FASHION_ID);
                bool unlockOk = entry != null && entry.IsColorUnlocked(1) && entry.GetStarLv(1) == 1;
                Debug.Log("CLIVERIFY fashion 41301 unlock color1 star=" + (entry?.GetStarLv(1) ?? -1) + " ok=" + unlockOk);

                // 41306 基础色进阶成功:Code=1,PosId,FashionId,ColorId=0,FashionStarLv=2(⚠16位)
                byte[] p41306 = new CliVerify.Pkt().I(1).C(POS).I(FASHION_ID).C(0).H(2).Bytes();
                Feed(m41306, p41306);
                entry = model.GetActive(POS, FASHION_ID);
                bool baseUpOk = entry != null && entry.GetStarLv(0) == 2;
                Debug.Log("CLIVERIFY fashion 41306 base star=" + (entry?.GetStarLv(0) ?? -1) + " ok=" + baseUpOk);

                // 41316 彩色进阶成功:PosId,FashionId,ColorId=1,Lv=2(⚠8位),Code=1(⚠最后)
                byte[] p41316 = new CliVerify.Pkt().C(POS).I(FASHION_ID).C(1).C(2).I(1).Bytes();
                Feed(m41316, p41316);
                entry = model.GetActive(POS, FASHION_ID);
                bool colorUpOk = entry != null && entry.GetStarLv(1) == 2;
                Debug.Log("CLIVERIFY fashion 41316 color1 star=" + (entry?.GetStarLv(1) ?? -1) + " ok=" + colorUpOk);

                // 41312 战力(⚠无 Code):PosId,FashionId,ColorPowerList 1 条{0, 100, 200}
                byte[] p41312 = new CliVerify.Pkt().C(POS).I(FASHION_ID).H(1).C(0).L(100).L(200).Bytes();
                Feed(m41312, p41312);
                var powers = model.GetPower(POS, FASHION_ID);
                bool powerOk = powers != null && powers.Count == 1 && powers[0].Power == 100 && powers[0].NextPower == 200;
                Debug.Log("CLIVERIFY fashion 41312 power=" + (powers != null && powers.Count > 0 ? powers[0].Power : -1) + " ok=" + powerOk);

                const int SUIT_ID = 7;
                // 41313 has no Code field. Its six fields must land in the model in wire order.
                Feed(m41313, new CliVerify.Pkt().H(1).C(SUIT_ID).C(0).C(0).C(4).I(1000).I(2000).Bytes());
                Shenxiao.Module.Core.Fashion.FashionModel.SuitEntry suit = model.GetSuit(SUIT_ID);
                bool suitSnapshotOk = suit != null && suit.Lv == 0 && suit.ActiveNum == 0 && suit.ConformNum == 4
                    && suit.Power == 1000 && suit.NextPower == 2000;
                Debug.Log("CLIVERIFY fashion 41313 snapshot=" + suitSnapshotOk + " suit=" + (suit?.SuitId ?? -1));

                // 41314 puts Code third. Perfect activation (4 pieces) also initializes Lv to 1.
                Feed(m41314, new CliVerify.Pkt().C(SUIT_ID).C(4).I(1).I(3000).I(4000).Bytes());
                suit = model.GetSuit(SUIT_ID);
                bool suitActivateOk = suit != null && suit.ActiveNum == 4 && suit.Lv == 1
                    && suit.Power == 3000 && suit.NextPower == 4000;
                Feed(m41314, new CliVerify.Pkt().C(SUIT_ID).C(2).I(5).I(9000).I(10000).Bytes());
                suit = model.GetSuit(SUIT_ID);
                bool suitActivateFailStable = suit != null && suit.ActiveNum == 4 && suit.Lv == 1
                    && suit.Power == 3000 && suit.NextPower == 4000;
                Debug.Log("CLIVERIFY fashion 41314 success=" + suitActivateOk + " failStable=" + suitActivateFailStable);

                // 41315 also puts Code third. Failure must not overwrite the successful increment.
                Feed(m41315, new CliVerify.Pkt().C(SUIT_ID).C(2).I(1).I(5000).I(6000).Bytes());
                suit = model.GetSuit(SUIT_ID);
                bool suitUpgradeOk = suit != null && suit.Lv == 2 && suit.Power == 5000 && suit.NextPower == 6000;
                Feed(m41315, new CliVerify.Pkt().C(SUIT_ID).C(9).I(5).I(11000).I(12000).Bytes());
                suit = model.GetSuit(SUIT_ID);
                bool suitUpgradeFailStable = suit != null && suit.Lv == 2 && suit.Power == 5000 && suit.NextPower == 6000;
                Debug.Log("CLIVERIFY fashion 41315 success=" + suitUpgradeOk + " failStable=" + suitUpgradeFailStable);

                // 41301/41302/41306/41316 失败分支(code!=1)只要不抛异常即过(对标老端 Util.ErrorCodeShow 显码)
                bool failNoThrow = true;
                try
                {
                    Feed(m41301, new CliVerify.Pkt().I(5).C(POS).I(FASHION_ID).C(2).C(2).Bytes());
                    Feed(m41302, new CliVerify.Pkt().I(5).C(POS).I(FASHION_ID).C(0).Bytes());
                    Feed(m41306, new CliVerify.Pkt().I(5).C(POS).I(FASHION_ID).C(0).H(3).Bytes());
                    Feed(m41316, new CliVerify.Pkt().C(POS).I(FASHION_ID).C(1).C(3).I(5).Bytes());
                }
                catch (System.Exception e) { failNoThrow = false; Debug.LogError("CLIVERIFY fashion fail-branch threw: " + e); }
                Debug.Log("CLIVERIFY fashion fail branches noThrow=" + failNoThrow);

                // 41311(仅活下行,本端不发,只测收到不抛异常;RoleModel.Instance.RoleId 未登录时默认 0)
                bool figurePushNoThrow = true;
                try
                {
                    byte[] p41311 = new CliVerify.Pkt().L(0).H(1).C(1).I(5001).C(0).Bytes();
                    Feed(m41311, p41311);
                }
                catch (System.Exception e) { figurePushNoThrow = false; Debug.LogError("CLIVERIFY fashion 41311 threw: " + e); }
                Debug.Log("CLIVERIFY fashion 41311 noThrow=" + figurePushNoThrow);

                // 重新穿上(为渲染断言准备一个非空态)
                Feed(m41302, new CliVerify.Pkt().I(1).C(POS).I(FASHION_ID).C(0).Bytes());

                // 渲染断言:打开面板,FashionMainView 是否真挂了业务子类(而不是裸 FashionMainViewBind)
                Shenxiao.Module.Core.Fashion.FashionFlow.Open(0);
                await Task.Delay(400);
                stage.ForceCjkFont();
                string png = stage.Capture("Temp/round21_fashion_main.png");

                Transform mainViewT = CliVerify.FindDeep(stage.CanvasRoot, "FashionMainView");
                Shenxiao.Module.Core.Fashion.FashionMainView mainView =
                    mainViewT != null ? mainViewT.GetComponent<Shenxiao.Module.Core.Fashion.FashionMainView>() : null;
                bool viewUpgradedOk = mainView != null;

                Transform lbNameT = mainViewT != null ? CliVerify.FindDeep(mainViewT, "_lb_name") : null;
                TMP_Text lbName = lbNameT != null ? lbNameT.GetComponent<TMP_Text>() : null;
                bool nameOk = lbName != null && !string.IsNullOrEmpty(lbName.text);
                Debug.Log("CLIVERIFY fashion shell viewUpgraded=" + viewUpgradedOk + " lbName='" + (lbName?.text ?? "<null>")
                    + "' nameOk=" + nameOk + " shot=" + png);

                // 第二刀 UI：套装页必须是真业务类并吃到真实 config_fashion_suit；部位升级弹窗同理。
                Feed(m41313, new CliVerify.Pkt().H(1).C(1).C(1).C(4).C(4).I(1200).I(2400).Bytes());
                Shenxiao.Module.Core.Fashion.FashionFlow.Open(3);
                await Task.Delay(300);
                Transform suitT = CliVerify.FindDeep(stage.CanvasRoot, "FashionSuitView");
                var suitView = suitT != null ? suitT.GetComponent<Shenxiao.Module.Core.Fashion.FashionSuitView>() : null;
                Transform suitNameT = suitT != null ? CliVerify.FindDeep(suitT, "_lb_name") : null;
                TMP_Text suitName = suitNameT != null ? suitNameT.GetComponent<TMP_Text>() : null;
                bool suitViewOk = suitView != null && suitName != null && !string.IsNullOrEmpty(suitName.text);
                bool suitConditionGateOk = false;
                MethodInfo suitUpgradeMethod = typeof(Shenxiao.Module.Core.Fashion.FashionSuitView)
                    .GetMethod("Upgrade", BindingFlags.NonPublic | BindingFlags.Instance);
                if (suitView != null && suitUpgradeMethod != null && outboundField != null)
                {
                    object oldOutbound = outboundField.GetValue(null);
                    var blockedFrames = new List<byte[]>();
                    try
                    {
                        outboundField.SetValue(null, new Func<byte[], bool>(frame =>
                        {
                            blockedFrames.Add(frame);
                            return true;
                        }));
                        // 真实套装1下一阶要求四个培养位；当前模型只激活了不相干的时装，点击必须被 UI 门控。
                        suitUpgradeMethod.Invoke(suitView, null);
                        suitConditionGateOk = blockedFrames.Count == 0;
                    }
                    finally
                    {
                        outboundField.SetValue(null, oldOutbound);
                    }
                }
                string suitShot = stage.Capture("Temp/round21_fashion_suit.png");
                Debug.Log("CLIVERIFY fashion suit view=" + (suitView != null) + " name='" + (suitName?.text ?? "<null>")
                    + "' conditionGate=" + suitConditionGateOk + " ok=" + suitViewOk + " shot=" + suitShot);

                Shenxiao.Module.Core.Fashion.FashionFlow.Open(0);
                Shenxiao.Module.Core.Fashion.FashionFlow.OpenLevel(POS);
                await Task.Delay(200);
                Transform levelT = CliVerify.FindDeep(stage.CanvasRoot, "FashionLevelView");
                var levelView = levelT != null ? levelT.GetComponent<Shenxiao.Module.Core.Fashion.FashionLevelView>() : null;
                Transform levelLabelT = levelT != null ? CliVerify.FindDeep(levelT, "flv_level_label") : null;
                TMP_Text levelLabel = levelLabelT != null ? levelLabelT.GetComponent<TMP_Text>() : null;
                bool levelViewOk = levelView != null && levelLabel != null && levelLabel.text.Contains("3");
                string secondShot = stage.Capture("Temp/round21_fashion_level.png");
                Debug.Log("CLIVERIFY fashion level view=" + (levelView != null) + " label='" + (levelLabel?.text ?? "<null>")
                    + "' ok=" + levelViewOk + " shot=" + secondShot);

                Shenxiao.Module.Core.Fashion.FashionFlow.Close();
                Shenxiao.Module.Core.Fashion.FashionFlow.Reset();
                model.Clear();

                bool pass = secondKnifeConfigOk && secondKnifeC2sOk
                    && infoOk && posUpgradeOk && posUpgradeFailStable && activateOk && wearOk && takeOffOk && unlockOk
                    && baseUpOk && colorUpOk && powerOk && suitSnapshotOk && suitActivateOk && suitActivateFailStable
                    && suitUpgradeOk && suitUpgradeFailStable && failNoThrow && figurePushNoThrow && viewUpgradedOk && nameOk
                    && suitViewOk && suitConditionGateOk && levelViewOk;
                Debug.Log("CLIVERIFY fashion VERDICT secondKnifeConfigOk=" + secondKnifeConfigOk
                    + " secondKnifeC2sOk=" + secondKnifeC2sOk + " infoOk=" + infoOk + " activateOk=" + activateOk + " wearOk=" + wearOk
                    + " takeOffOk=" + takeOffOk + " unlockOk=" + unlockOk + " baseUpOk=" + baseUpOk + " colorUpOk=" + colorUpOk
                    + " powerOk=" + powerOk + " posUpgradeOk=" + posUpgradeOk + " posUpgradeFailStable=" + posUpgradeFailStable
                    + " suitSnapshotOk=" + suitSnapshotOk + " suitActivateOk=" + suitActivateOk
                    + " suitActivateFailStable=" + suitActivateFailStable + " suitUpgradeOk=" + suitUpgradeOk
                    + " suitUpgradeFailStable=" + suitUpgradeFailStable + " failNoThrow=" + failNoThrow + " figurePushNoThrow=" + figurePushNoThrow
                    + " viewUpgradedOk=" + viewUpgradedOk + " nameOk=" + nameOk + " suitViewOk=" + suitViewOk
                    + " suitConditionGateOk=" + suitConditionGateOk
                    + " levelViewOk=" + levelViewOk + " pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                stage.Dispose();
            }
        }

        /// <summary>
        /// 批处理入口:Unity.exe -batchmode -projectPath . -executeMethod
        ///   Shenxiao.EditorTools.FashionCase.RunBatch -logFile Temp/cliverify_fashion.log
        /// </summary>
        public static void RunBatch()
        {
            _ = RunBatchAsync();
        }

        private static async Task RunBatchAsync()
        {
            int code;
            try
            {
                code = await Run();
            }
            catch (System.Exception e)
            {
                Debug.LogError("CLIVERIFY fashion 异常: " + e);
                code = 1;
            }
            UnityEditor.EditorApplication.Exit(code);
        }

        private static bool FrameEquals(byte[] actual, int protoId, byte[] payload)
        {
            int total = 6 + payload.Length;
            if (actual == null || actual.Length != total) return false;
            byte[] expected = new byte[total];
            expected[0] = (byte)(total >> 8);
            expected[1] = (byte)total;
            expected[2] = 0x03;
            expected[3] = 0xE8;
            expected[4] = (byte)(protoId >> 8);
            expected[5] = (byte)protoId;
            Buffer.BlockCopy(payload, 0, expected, 6, payload.Length);
            for (int i = 0; i < total; i++)
                if (actual[i] != expected[i]) return false;
            return true;
        }
    }
}
