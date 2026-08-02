using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.NineSky;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class NineSkyCase
    {
        private const BindingFlags InstanceNonPublic = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags StaticNonPublic = BindingFlags.NonPublic | BindingFlags.Static;

        public static Task<int> Run()
        {
            try
            {
                return Task.FromResult(RunCore());
            }
            catch (Exception exception)
            {
                Debug.LogError("CLIVERIFY ninesky EXCEPTION " + exception);
                return Task.FromResult(3);
            }
        }

        private static int RunCore()
        {
            NineSkyController controller = NineSkyController.Instance;
            NineSkyModel model = NineSkyModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            bool oldHasData = model.HasData;
            byte oldState = model.State;
            uint oldLeftTime = model.LeftTime;
            uint oldMod = model.Mod;
            uint oldGroupId = model.GroupId;
            ulong oldAverageLevel = model.AverageLevel;
            var oldServers = new List<NineSkyModel.ServerEntry>(model.Servers);
            FieldInfo interceptField = typeof(NineSkyController).GetField("s_outboundIntercept", StaticNonPublic);
            object oldIntercept = interceptField == null ? null : interceptField.GetValue(null);
            bool oldHasBattle = model.HasBattleInfo; byte oldCurFloor = model.CurFloor, oldMaxFloor = model.MaxFloor; uint oldBattleLeft = model.BattleLeftTime, oldScore = model.Score; ushort oldKill = model.KillNum, oldFirstServer = model.FirstServerNum; string oldFirstPlayer = model.FirstPlayer;
            bool oldHasFlag = model.HasFlagInfo; byte oldFlagIndex = model.FlagIndex; ushort oldFlagServer = model.FlagServerNum; ulong oldFlagRole = model.FlagRoleId; string oldFlagName = model.FlagRoleName; uint oldFlagLeft = model.FlagLeftTime;
            NineSkyModel.SettlementSnapshot oldSettlement = model.Settlement;
            IDictionary oldHandlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary; var handlerSnapshot = new Dictionary<int, object>(); if (oldHandlers != null) foreach (int id in new[] { 13500, 13503, 13504, 13507 }) if (oldHandlers.Contains(id)) handlerSnapshot[id] = oldHandlers[id];
            bool restored = false, pass = false;

            try
            {
                controller.Init();
                model.Reset();

                MethodInfo on13500 = typeof(NineSkyController).GetMethod("On13500", InstanceNonPublic);
                MethodInfo on13503 = typeof(NineSkyController).GetMethod("On13503", InstanceNonPublic);
                MethodInfo on13504 = typeof(NineSkyController).GetMethod("On13504", InstanceNonPublic);
                MethodInfo on13507 = typeof(NineSkyController).GetMethod("On13507", InstanceNonPublic);
                IDictionary handlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                pass = interceptField != null && on13500 != null && on13503 != null && on13504 != null && on13507 != null
                    && typeof(NineSkyController).GetMethod("RequestFlagInfo") == null
                    && typeof(NineSkyController).GetMethod("RequestSettlement") == null
                    && handlers != null && handlers.Contains(13500) && handlers.Contains(13503) && handlers.Contains(13504) && handlers.Contains(13507);
                for (int proto = 13501; proto <= 13510; proto++)
                {
                    pass &= (proto == 13503 || proto == 13504 || proto == 13507) ? handlers.Contains(proto) : !handlers.Contains(proto);
                }

                if (!pass)
                {
                    Debug.LogError("CLIVERIFY ninesky VERDICT pass=false (reflection/protocol registration missing)");
                    return 3;
                }

                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));
                controller.RequestInfo();
                pass &= frames.Count == 1
                    && frames[0].Length == 6
                    && frames[0][0] == 0 && frames[0][1] == 6
                    && frames[0][2] == 0x03 && frames[0][3] == 0xE8
                    && frames[0][4] == (byte)(Proto.NINE_SKY_INFO >> 8)
                    && frames[0][5] == (byte)(Proto.NINE_SKY_INFO & 0xFF);
                frames.Clear();
                controller.RequestBattleInfo();
                pass &= frames.Count == 1 && ExactEmpty(frames[0], Proto.NINE_SKY_BATTLE_INFO) && !model.HasBattleInfo;
                frames.Clear();

                const ulong max = ulong.MaxValue;
                const ulong highBit = 0x8000000000000000UL;
                const ulong averageAboveLongMax = 0x8000000000000001UL;
                const string chineseName = "九天中文服";
                byte[] firstBytes = new CliVerify.Pkt()
                    .C(255).I(4000000000L).I(2).I(3).H(2)
                    .L(unchecked((long)max)).L(unchecked((long)highBit)).S(chineseName).L(10)
                    .L(11).L(12).S("Second").L(unchecked((long)highBit))
                    .L(unchecked((long)averageAboveLongMax))
                    .Bytes();
                var firstReader = new NetReader(firstBytes, 0, firstBytes.Length);
                on13500.Invoke(controller, new object[] { firstReader });
                pass &= firstReader.Remaining == 0
                    && model.HasData && model.State == 255 && model.LeftTime == 4000000000U
                    && model.Mod == 2 && model.GroupId == 3 && model.Servers.Count == 2
                    && model.Servers[0].ServerId == max && model.Servers[0].ServerNumber == highBit
                    && model.Servers[0].ServerName == chineseName && model.Servers[0].WorldLevel == 10
                    && model.Servers[1].ServerId == 11 && model.Servers[1].ServerNumber == 12
                    && model.Servers[1].ServerName == "Second" && model.Servers[1].WorldLevel == highBit
                    && model.AverageLevel == averageAboveLongMax && frames.Count == 0;

                byte[] secondBytes = new CliVerify.Pkt()
                    .C(1).I(21).I(22).I(23).H(1)
                    .L(24).L(25).S("替换服").L(26).L(27)
                    .Bytes();
                var secondReader = new NetReader(secondBytes, 0, secondBytes.Length);
                on13500.Invoke(controller, new object[] { secondReader });
                pass &= secondReader.Remaining == 0
                    && model.HasData && model.State == 1 && model.LeftTime == 21
                    && model.Mod == 22 && model.GroupId == 23 && model.Servers.Count == 1
                    && model.Servers[0].ServerId == 24 && model.Servers[0].ServerNumber == 25
                    && model.Servers[0].ServerName == "替换服" && model.Servers[0].WorldLevel == 26
                    && model.AverageLevel == 27;

                byte[] thirdBytes = new CliVerify.Pkt().C(0).I(0).I(0).I(0).H(0).L(0).Bytes();
                var thirdReader = new NetReader(thirdBytes, 0, thirdBytes.Length);
                on13500.Invoke(controller, new object[] { thirdReader });
                pass &= thirdReader.Remaining == 0
                    && model.HasData && model.State == 0 && model.LeftTime == 0
                    && model.Mod == 0 && model.GroupId == 0 && model.Servers.Count == 0
                    && model.AverageLevel == 0;

                byte[] battleMax = new CliVerify.Pkt().C(byte.MaxValue).C(byte.MaxValue).I(uint.MaxValue).H(ushort.MaxValue).I(uint.MaxValue).H(ushort.MaxValue).S("九天").Bytes();
                var battleMaxReader = new NetReader(battleMax, 0, battleMax.Length); on13503.Invoke(controller, new object[] { battleMaxReader });
                pass &= battleMaxReader.Remaining == 0 && model.HasBattleInfo && model.CurFloor == byte.MaxValue && model.MaxFloor == byte.MaxValue && model.BattleLeftTime == uint.MaxValue && model.KillNum == ushort.MaxValue && model.Score == uint.MaxValue && model.FirstServerNum == ushort.MaxValue && model.FirstPlayer == "九天"
                    && model.HasData && model.State == 0 && model.LeftTime == 0 && model.Mod == 0 && model.GroupId == 0 && model.AverageLevel == 0 && model.Servers.Count == 0;
                byte[] battleSmall = new CliVerify.Pkt().C(1).C(2).I(3).H(4).I(5).H(6).S("小").Bytes();
                var battleSmallReader = new NetReader(battleSmall, 0, battleSmall.Length); on13503.Invoke(controller, new object[] { battleSmallReader });
                pass &= battleSmallReader.Remaining == 0 && model.CurFloor == 1 && model.MaxFloor == 2 && model.BattleLeftTime == 3 && model.KillNum == 4 && model.Score == 5 && model.FirstServerNum == 6 && model.FirstPlayer == "小";
                var reverseReader = new NetReader(firstBytes, 0, firstBytes.Length); on13500.Invoke(controller, new object[] { reverseReader });
                pass &= reverseReader.Remaining == 0 && model.HasBattleInfo && model.CurFloor == 1 && model.MaxFloor == 2 && model.BattleLeftTime == 3 && model.KillNum == 4 && model.Score == 5 && model.FirstServerNum == 6 && model.FirstPlayer == "小";
                controller.RequestBattleInfo(); pass &= frames.Count == 1 && ExactEmpty(frames[0], Proto.NINE_SKY_BATTLE_INFO) && model.CurFloor == 1 && model.FirstPlayer == "小"; frames.Clear();
                byte[] battleZero = new CliVerify.Pkt().C(0).C(0).I(0).H(0).I(0).H(0).S("").Bytes();
                var battleZeroReader = new NetReader(battleZero, 0, battleZero.Length); on13503.Invoke(controller, new object[] { battleZeroReader });
                pass &= battleZeroReader.Remaining == 0 && model.HasBattleInfo && model.CurFloor == 0 && model.MaxFloor == 0 && model.BattleLeftTime == 0 && model.KillNum == 0 && model.Score == 0 && model.FirstServerNum == 0 && model.FirstPlayer == "";

                byte[] flagFull = new CliVerify.Pkt().C(byte.MaxValue).H(ushort.MaxValue).L(unchecked((long)ulong.MaxValue)).S("旗帜").I(uint.MaxValue).Bytes(); var flagFullReader = new NetReader(flagFull, 0, flagFull.Length); on13504.Invoke(controller, new object[] { flagFullReader });
                pass &= flagFullReader.Remaining == 0 && model.HasFlagInfo && model.FlagIndex == byte.MaxValue && model.FlagServerNum == ushort.MaxValue && model.FlagRoleId == ulong.MaxValue && model.FlagRoleName == "旗帜" && model.FlagLeftTime == uint.MaxValue
                    && model.HasData && model.State == 255 && model.LeftTime == 4000000000U && model.Mod == 2 && model.GroupId == 3 && model.Servers.Count == 2 && model.AverageLevel == averageAboveLongMax
                    && model.HasBattleInfo && model.CurFloor == 0 && model.MaxFloor == 0 && model.BattleLeftTime == 0 && model.KillNum == 0 && model.Score == 0 && model.FirstServerNum == 0 && model.FirstPlayer == "";
                byte[] flagSmall = new CliVerify.Pkt().C(1).H(2).L(3).S("小旗").I(4).Bytes(); var flagSmallReader = new NetReader(flagSmall, 0, flagSmall.Length); on13504.Invoke(controller, new object[] { flagSmallReader });
                pass &= flagSmallReader.Remaining == 0 && model.FlagIndex == 1 && model.FlagServerNum == 2 && model.FlagRoleId == 3 && model.FlagRoleName == "小旗" && model.FlagLeftTime == 4;
                reverseReader = new NetReader(thirdBytes, 0, thirdBytes.Length); on13500.Invoke(controller, new object[] { reverseReader });
                battleSmallReader = new NetReader(battleSmall, 0, battleSmall.Length); on13503.Invoke(controller, new object[] { battleSmallReader });
                pass &= reverseReader.Remaining == 0 && battleSmallReader.Remaining == 0 && model.HasFlagInfo && model.FlagIndex == 1 && model.FlagServerNum == 2 && model.FlagRoleId == 3 && model.FlagRoleName == "小旗" && model.FlagLeftTime == 4;
                byte[] flagZero = new CliVerify.Pkt().C(0).H(0).L(0).S("").I(0).Bytes(); var flagZeroReader = new NetReader(flagZero, 0, flagZero.Length); on13504.Invoke(controller, new object[] { flagZeroReader });
                pass &= flagZeroReader.Remaining == 0 && model.HasFlagInfo && model.FlagIndex == 0 && model.FlagServerNum == 0 && model.FlagRoleId == 0 && model.FlagRoleName == "" && model.FlagLeftTime == 0;

                byte[] settlementFull = new CliVerify.Pkt()
                    .C(byte.MaxValue)
                    .H(2)
                    .C(byte.MaxValue).I(uint.MaxValue).I(4000000000L)
                    .C(7).I(8).I(9)
                    .H(ushort.MaxValue).S("首位中文")
                    .H(2)
                    .C(byte.MaxValue).H(ushort.MaxValue).S("甲")
                    .C(byte.MaxValue).H(2).S("")
                    .Bytes();
                var settlementFullReader = new NetReader(settlementFull, 0, settlementFull.Length);
                on13507.Invoke(controller, new object[] { settlementFullReader });
                NineSkyModel.SettlementSnapshot immutableFull = model.Settlement;
                pass &= settlementFullReader.Remaining == 0 && frames.Count == 0 && model.HasSettlement && immutableFull != null
                    && immutableFull.MaxFloor == byte.MaxValue && immutableFull.Rewards.Count == 2
                    && immutableFull.Rewards[0].Type == byte.MaxValue && immutableFull.Rewards[0].TypeId == uint.MaxValue && immutableFull.Rewards[0].Num == 4000000000U
                    && immutableFull.Rewards[1].Type == 7 && immutableFull.Rewards[1].TypeId == 8 && immutableFull.Rewards[1].Num == 9
                    && immutableFull.FirstServerNumber == ushort.MaxValue && immutableFull.FirstPlayer == "首位中文"
                    && immutableFull.FloorOwners.Count == 2
                    && immutableFull.FloorOwners[0].Index == byte.MaxValue && immutableFull.FloorOwners[0].ServerNumber == ushort.MaxValue && immutableFull.FloorOwners[0].RoleName == "甲"
                    && immutableFull.FloorOwners[1].Index == byte.MaxValue && immutableFull.FloorOwners[1].ServerNumber == 2 && immutableFull.FloorOwners[1].RoleName == ""
                    && model.HasData && model.HasBattleInfo && model.HasFlagInfo;

                byte[] settlementSmall = new CliVerify.Pkt().C(1).H(1).C(2).I(3).I(4).H(5).S("次").H(1).C(6).H(7).S("乙").Bytes();
                var settlementSmallReader = new NetReader(settlementSmall, 0, settlementSmall.Length);
                on13507.Invoke(controller, new object[] { settlementSmallReader });
                pass &= settlementSmallReader.Remaining == 0 && model.HasSettlement && model.Settlement != immutableFull
                    && model.Settlement.MaxFloor == 1 && model.Settlement.Rewards.Count == 1
                    && model.Settlement.Rewards[0].Type == 2 && model.Settlement.Rewards[0].TypeId == 3 && model.Settlement.Rewards[0].Num == 4
                    && model.Settlement.FirstServerNumber == 5 && model.Settlement.FirstPlayer == "次"
                    && model.Settlement.FloorOwners.Count == 1 && model.Settlement.FloorOwners[0].Index == 6 && model.Settlement.FloorOwners[0].ServerNumber == 7 && model.Settlement.FloorOwners[0].RoleName == "乙"
                    && immutableFull.MaxFloor == byte.MaxValue && immutableFull.Rewards.Count == 2 && immutableFull.Rewards[0].Num == 4000000000U
                    && immutableFull.FloorOwners.Count == 2 && immutableFull.FloorOwners[0].RoleName == "甲";

                reverseReader = new NetReader(firstBytes, 0, firstBytes.Length); on13500.Invoke(controller, new object[] { reverseReader });
                battleSmallReader = new NetReader(battleSmall, 0, battleSmall.Length); on13503.Invoke(controller, new object[] { battleSmallReader });
                flagSmallReader = new NetReader(flagSmall, 0, flagSmall.Length); on13504.Invoke(controller, new object[] { flagSmallReader });
                pass &= reverseReader.Remaining == 0 && battleSmallReader.Remaining == 0 && flagSmallReader.Remaining == 0
                    && model.Settlement.MaxFloor == 1 && model.Settlement.Rewards.Count == 1 && model.Settlement.FloorOwners.Count == 1;

                byte[] settlementEmpty = new CliVerify.Pkt().C(0).H(0).H(0).S("").H(0).Bytes();
                var settlementEmptyReader = new NetReader(settlementEmpty, 0, settlementEmpty.Length);
                on13507.Invoke(controller, new object[] { settlementEmptyReader });
                pass &= settlementEmptyReader.Remaining == 0 && model.HasSettlement && model.Settlement.MaxFloor == 0
                    && model.Settlement.Rewards.Count == 0 && model.Settlement.FirstServerNumber == 0 && model.Settlement.FirstPlayer == "" && model.Settlement.FloorOwners.Count == 0
                    && model.HasData && model.State == byte.MaxValue && model.HasBattleInfo && model.CurFloor == 1 && model.HasFlagInfo && model.FlagIndex == 1;

                controller.Dispose();
                pass &= !model.HasData && !model.HasBattleInfo && !model.HasFlagInfo && !model.HasSettlement && model.Settlement == null
                    && !handlers.Contains(13500) && !handlers.Contains(13503) && !handlers.Contains(13504) && !handlers.Contains(13507) && model.State == 0 && model.LeftTime == 0
                    && model.Mod == 0 && model.GroupId == 0 && model.Servers.Count == 0
                    && model.AverageLevel == 0 && model.CurFloor == 0 && model.MaxFloor == 0 && model.BattleLeftTime == 0 && model.KillNum == 0 && model.Score == 0 && model.FirstServerNum == 0 && model.FirstPlayer == null;

                Debug.Log("CLIVERIFY ninesky VERDICT pass=" + pass);
            }
            finally
            {
                try
                {
                    if (controller.IsInitialized) controller.Dispose();
                    model.Reset();
                    if (oldHasData) model.Replace(oldState, oldLeftTime, oldMod, oldGroupId, oldServers, oldAverageLevel);
                    if (oldHasBattle) model.ReplaceBattleInfo(oldCurFloor, oldMaxFloor, oldBattleLeft, oldKill, oldScore, oldFirstServer, oldFirstPlayer);
                    if (oldHasFlag) model.ReplaceFlagInfo(oldFlagIndex, oldFlagServer, oldFlagRole, oldFlagName, oldFlagLeft);
                    if (oldSettlement != null) model.ReplaceSettlement(oldSettlement);
                    if (wasInitialized) controller.Init();
                    IDictionary finalHandlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                    if (finalHandlers == null) throw new InvalidOperationException("handlers unavailable");
                    foreach (int id in new[] { 13500, 13503, 13504, 13507 }) if (handlerSnapshot.TryGetValue(id, out object value)) finalHandlers[id] = value; else finalHandlers.Remove(id);
                    if (interceptField != null) interceptField.SetValue(null, oldIntercept);
                    restored = controller.IsInitialized == wasInitialized && model.HasData == oldHasData && model.HasBattleInfo == oldHasBattle && model.HasFlagInfo == oldHasFlag && ReferenceEquals(model.Settlement, oldSettlement) && model.State == oldState && model.LeftTime == oldLeftTime && model.Mod == oldMod && model.GroupId == oldGroupId && model.AverageLevel == oldAverageLevel && model.CurFloor == oldCurFloor && model.MaxFloor == oldMaxFloor && model.BattleLeftTime == oldBattleLeft && model.KillNum == oldKill && model.Score == oldScore && model.FirstServerNum == oldFirstServer && model.FirstPlayer == oldFirstPlayer && model.FlagIndex == oldFlagIndex && model.FlagServerNum == oldFlagServer && model.FlagRoleId == oldFlagRole && model.FlagRoleName == oldFlagName && model.FlagLeftTime == oldFlagLeft && (interceptField == null || ReferenceEquals(interceptField.GetValue(null), oldIntercept));
                    if (model.Servers.Count != oldServers.Count) restored = false; else for (int i = 0; i < oldServers.Count; i++) { NineSkyModel.ServerEntry a = model.Servers[i], b = oldServers[i]; if (a.ServerId != b.ServerId || a.ServerNumber != b.ServerNumber || a.ServerName != b.ServerName || a.WorldLevel != b.WorldLevel) restored = false; }
                    foreach (int id in new[] { 13500, 13503, 13504, 13507 }) { bool existed = handlerSnapshot.TryGetValue(id, out object expected); if (finalHandlers.Contains(id) != existed || (existed && !ReferenceEquals(finalHandlers[id], expected))) restored = false; }
                }
                catch (Exception exception) { Debug.LogError("CLIVERIFY ninesky restore " + exception); restored = false; }
                Debug.Log("CLIVERIFY ninesky restored=" + restored + " pass=" + pass);
            }
            return pass && restored ? 0 : 3;
        }

        private static bool ExactEmpty(byte[] frame, int proto) => frame != null && frame.Length == 6 && frame[0] == 0 && frame[1] == 6 && frame[2] == 0x03 && frame[3] == 0xE8 && frame[4] == (byte)(proto >> 8) && frame[5] == (byte)(proto & 0xFF);
    }
}
