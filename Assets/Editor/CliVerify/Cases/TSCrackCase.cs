using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.TSCrack;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class TSCrackCase
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
                Debug.LogError("CLIVERIFY tscrack EXCEPTION " + exception);
                return Task.FromResult(3);
            }
        }

        private static int RunCore()
        {
            TSCrackController controller = TSCrackController.Instance;
            TSCrackModel model = TSCrackModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            bool oldHasData = model.HasData;
            byte oldStatus = model.Status;
            var oldServers = new List<TSCrackModel.ServerEntry>(model.Servers);
            bool oldHasMainData = model.HasMainData;
            uint oldMyValue = model.MyValue;
            uint oldMyServerValue = model.MyServerValue;
            var oldMainCastles = new List<TSCrackModel.CastleEntry>(model.MainCastles);
            var oldCastleDetails = new List<TSCrackModel.CastleEntry>(model.CastleDetails);
            bool oldHasDailyActivities = model.HasDailyActivities;
            var oldDailyActivities = new List<TSCrackModel.DailyActivityEntry>(model.DailyActivities);
            bool oldHasDailyRewards = model.HasDailyRewards;
            uint oldDailyValue = model.DailyValue;
            uint oldTotalValue = model.TotalValue;
            var oldDailyRewards = new List<TSCrackModel.DailyRewardEntry>(model.DailyRewards);
            bool oldHasSeasonGoals = model.HasSeasonGoals;
            var oldSeasonGoals = new List<TSCrackModel.SeasonGoalEntry>(model.SeasonGoals);
            bool oldHasPersonalRanks = model.HasPersonalRanks;
            var oldPersonalRanks = new List<TSCrackModel.RankEntry>(model.PersonalRanks);
            bool oldHasCurrentCastle = model.HasCurrentCastle;
            uint oldCurrentCastleId = model.CurrentCastleId;
            FieldInfo interceptField = typeof(TSCrackController).GetField("s_outboundIntercept", StaticNonPublic);
            object oldIntercept = interceptField == null ? null : interceptField.GetValue(null);

            try
            {
                controller.Init();
                model.Reset();

                IDictionary handlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                int[] registered = { 20401, 20402, 20404, 20405, 20407, 20409, 20410, 20411 };
                int[] excluded = { 20400, 20403, 20406, 20408 };
                bool passA = interceptField != null && handlers != null;
                foreach (int proto in registered) passA &= handlers != null && handlers.Contains(proto);
                foreach (int proto in excluded) passA &= handlers != null && !handlers.Contains(proto);
                Debug.Log("CLIVERIFY tscrack A registration=" + passA);

                var frames = new List<byte[]>();
                interceptField?.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));

                controller.RequestInfo();
                controller.RequestMainInfo();
                controller.RequestCastleInfo(0xABCD);
                controller.RequestDailyActivities();
                controller.RequestDailyRewards();
                controller.RequestSeasonGoals();
                controller.RequestPersonalRanks();
                controller.RequestCurrentCastle();

                int[] expectedIds = { 20411, 20401, 20402, 20404, 20405, 20407, 20409, 20410 };
                bool passB = frames.Count == expectedIds.Length;
                for (int i = 0; i < expectedIds.Length && i < frames.Count; i++)
                {
                    byte[] payload = expectedIds[i] == 20402 ? new byte[] { 0xAB, 0xCD } : Array.Empty<byte>();
                    passB &= IsFrame(frames[i], expectedIds[i], payload);
                }
                frames.Clear();
                Debug.Log("CLIVERIFY tscrack B explicitRequests=" + passB);

                byte[] mainBytes = new CliVerify.Pkt()
                    .I(4294967295L).I(0).H(1)
                    .H(65535).I(4294967295L).I(0).I(2147483648L).S("主据点")
                    .H(2)
                    .I(9).S("服甲").I(7)
                    .I(9).S("服甲重复").I(4294967295L)
                    .H(2)
                    .I(10).S("角色甲").I(8).C(255)
                    .I(10).S("").I(0).C(0)
                    .H(65535).H(0)
                    .Bytes();
                bool passC = Invoke(controller, "On20401", mainBytes);
                TSCrackModel.CastleEntry mainCastle = model.MainCastles.Count == 1 ? model.MainCastles[0] : null;
                passC &= model.HasMainData && model.MyValue == uint.MaxValue && model.MyServerValue == 0
                    && mainCastle != null && mainCastle.CastleId == ushort.MaxValue
                    && mainCastle.BaseServerNumber == uint.MaxValue && mainCastle.NeedValue == 0
                    && mainCastle.ServerNumber == 0x80000000u && mainCastle.ServerName == "主据点"
                    && mainCastle.Servers.Count == 2
                    && mainCastle.Servers[0].ServerNumber == 9 && mainCastle.Servers[0].ServerName == "服甲" && mainCastle.Servers[0].Value == 7
                    && mainCastle.Servers[1].ServerNumber == 9 && mainCastle.Servers[1].ServerName == "服甲重复" && mainCastle.Servers[1].Value == uint.MaxValue
                    && mainCastle.Roles.Count == 2
                    && mainCastle.Roles[0].ServerNumber == 10 && mainCastle.Roles[0].RoleName == "角色甲"
                    && mainCastle.Roles[0].Value == 8 && mainCastle.Roles[0].IsOccupying == 255
                    && mainCastle.Roles[1].ServerNumber == 10 && mainCastle.Roles[1].RoleName == string.Empty
                    && mainCastle.Roles[1].Value == 0 && mainCastle.Roles[1].IsOccupying == 0
                    && mainCastle.RoleCount == ushort.MaxValue && mainCastle.ProviderCount == 0
                    && frames.Count == 0;
                Debug.Log("CLIVERIFY tscrack C mainSnapshot=" + passC);

                bool passD = Invoke(controller, "On20402", CastlePacket(7, 101, "明细七", 1, 2));
                passD &= Invoke(controller, "On20402", CastlePacket(8, 202, "明细八", 3, 4));
                passD &= model.CastleDetailCount == 2
                    && model.TryGetCastleDetail(7, out TSCrackModel.CastleEntry detail7)
                    && detail7.NeedValue == 101 && detail7.ServerName == "明细七"
                    && model.TryGetCastleDetail(8, out TSCrackModel.CastleEntry detail8)
                    && detail8.NeedValue == 202 && detail8.ServerName == "明细八";
                passD &= Invoke(controller, "On20402", CastlePacket(7, 303, "替换七", 0, 0));
                passD &= model.CastleDetailCount == 2
                    && model.TryGetCastleDetail(7, out detail7) && detail7.NeedValue == 303
                    && detail7.ServerName == "替换七" && detail7.Servers.Count == 0 && detail7.Roles.Count == 0
                    && model.TryGetCastleDetail(8, out detail8) && detail8.NeedValue == 202;
                Debug.Log("CLIVERIFY tscrack D keyedCastleDetail=" + passD);

                bool passE = Invoke(controller, "On20404", new CliVerify.Pkt()
                    .H(2).H(65535).H(0).I(4294967295L).H(65535).H(0).I(1).Bytes());
                passE &= model.HasDailyActivities && model.DailyActivities.Count == 2
                    && model.DailyActivities[0].ModuleId == ushort.MaxValue
                    && model.DailyActivities[0].SubModuleId == 0 && model.DailyActivities[0].Value == uint.MaxValue
                    && model.DailyActivities[1].ModuleId == ushort.MaxValue && model.DailyActivities[1].Value == 1;
                passE &= Invoke(controller, "On20405", new CliVerify.Pkt()
                    .I(4294967295L).I(0).H(2).C(255).C(2).C(255).C(0).Bytes());
                passE &= model.HasDailyRewards && model.DailyValue == uint.MaxValue && model.TotalValue == 0
                    && model.DailyRewards.Count == 2 && model.DailyRewards[0].Stage == 255
                    && model.DailyRewards[0].Status == 2 && model.DailyRewards[1].Stage == 255
                    && model.DailyRewards[1].Status == 0;
                passE &= Invoke(controller, "On20407", new CliVerify.Pkt()
                    .H(2).H(65535).I(4294967295L).C(255).H(65535).I(0).C(0).Bytes());
                passE &= model.HasSeasonGoals && model.SeasonGoals.Count == 2
                    && model.SeasonGoals[0].GoalId == ushort.MaxValue && model.SeasonGoals[0].Value == uint.MaxValue
                    && model.SeasonGoals[0].Status == 255 && model.SeasonGoals[1].GoalId == ushort.MaxValue
                    && model.SeasonGoals[1].Value == 0 && model.SeasonGoals[1].Status == 0;
                Debug.Log("CLIVERIFY tscrack E dailyAndSeason=" + passE);

                bool passF = Invoke(controller, "On20409", new CliVerify.Pkt()
                    .H(2)
                    .I(4294967295L).L(-1L).S("排行甲").I(4294967295L)
                    .I(0).L(-1L).S("").I(0)
                    .Bytes());
                passF &= model.HasPersonalRanks && model.PersonalRanks.Count == 2
                    && model.PersonalRanks[0].ServerNumber == uint.MaxValue && model.PersonalRanks[0].RoleId == -1L
                    && model.PersonalRanks[0].RoleName == "排行甲" && model.PersonalRanks[0].Value == uint.MaxValue
                    && model.PersonalRanks[1].ServerNumber == 0 && model.PersonalRanks[1].RoleId == -1L
                    && model.PersonalRanks[1].RoleName == string.Empty && model.PersonalRanks[1].Value == 0;
                passF &= Invoke(controller, "On20410", new CliVerify.Pkt().I(4294967295L).Bytes());
                passF &= model.HasCurrentCastle && model.CurrentCastleId == uint.MaxValue;
                passF &= Invoke(controller, "On20411", new CliVerify.Pkt()
                    .C(255).H(2)
                    .I(4294967295L).S("时空中文服").H(65535)
                    .I(0).S("Second").H(0)
                    .Bytes());
                passF &= model.HasData && model.Status == 255 && model.Servers.Count == 2
                    && model.Servers[0].ServerNumber == uint.MaxValue
                    && model.Servers[0].ServerName == "时空中文服" && model.Servers[0].Level == ushort.MaxValue
                    && model.Servers[1].ServerNumber == 0 && model.Servers[1].ServerName == "Second"
                    && model.Servers[1].Level == 0 && frames.Count == 0;
                Debug.Log("CLIVERIFY tscrack F rankCurrentWorld=" + passF);

                bool passG = Invoke(controller, "On20401", new CliVerify.Pkt().I(0).I(0).H(0).Bytes())
                    && Invoke(controller, "On20404", new CliVerify.Pkt().H(0).Bytes())
                    && Invoke(controller, "On20405", new CliVerify.Pkt().I(0).I(0).H(0).Bytes())
                    && Invoke(controller, "On20407", new CliVerify.Pkt().H(0).Bytes())
                    && Invoke(controller, "On20409", new CliVerify.Pkt().H(0).Bytes())
                    && Invoke(controller, "On20410", new CliVerify.Pkt().I(0).Bytes())
                    && Invoke(controller, "On20411", new CliVerify.Pkt().C(0).H(0).Bytes());
                passG &= model.HasMainData && model.MyValue == 0 && model.MyServerValue == 0 && model.MainCastles.Count == 0
                    && model.HasDailyActivities && model.DailyActivities.Count == 0
                    && model.HasDailyRewards && model.DailyValue == 0 && model.TotalValue == 0 && model.DailyRewards.Count == 0
                    && model.HasSeasonGoals && model.SeasonGoals.Count == 0
                    && model.HasPersonalRanks && model.PersonalRanks.Count == 0
                    && model.HasCurrentCastle && model.CurrentCastleId == 0
                    && model.HasData && model.Status == 0 && model.Servers.Count == 0
                    && model.CastleDetailCount == 2 && model.TryGetCastleDetail(8, out detail8) && detail8.NeedValue == 202
                    && frames.Count == 0;
                Debug.Log("CLIVERIFY tscrack G emptyReplaceAndIsolation=" + passG);

                controller.Dispose();
                bool passH = !model.HasData && model.Status == 0 && model.Servers.Count == 0
                    && !model.HasMainData && model.MainCastles.Count == 0 && !model.HasAnyCastleDetail
                    && !model.HasDailyActivities && model.DailyActivities.Count == 0
                    && !model.HasDailyRewards && model.DailyRewards.Count == 0
                    && !model.HasSeasonGoals && model.SeasonGoals.Count == 0
                    && !model.HasPersonalRanks && model.PersonalRanks.Count == 0
                    && !model.HasCurrentCastle && model.CurrentCastleId == 0;
                Debug.Log("CLIVERIFY tscrack H disposeReset=" + passH);

                bool pass = passA && passB && passC && passD && passE && passF && passG && passH;
                Debug.Log("CLIVERIFY tscrack VERDICT pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                if (oldHasData) model.Replace(oldStatus, oldServers);
                if (oldHasMainData) model.ReplaceMain(oldMyValue, oldMyServerValue, oldMainCastles);
                foreach (TSCrackModel.CastleEntry castle in oldCastleDetails) model.ReplaceCastleDetail(castle);
                if (oldHasDailyActivities) model.ReplaceDailyActivities(oldDailyActivities);
                if (oldHasDailyRewards) model.ReplaceDailyRewards(oldDailyValue, oldTotalValue, oldDailyRewards);
                if (oldHasSeasonGoals) model.ReplaceSeasonGoals(oldSeasonGoals);
                if (oldHasPersonalRanks) model.ReplacePersonalRanks(oldPersonalRanks);
                if (oldHasCurrentCastle) model.ReplaceCurrentCastle(oldCurrentCastleId);
                if (wasInitialized) controller.Init();
                if (interceptField != null) interceptField.SetValue(null, oldIntercept);
            }
        }

        private static byte[] CastlePacket(ushort castleId, uint needValue, string serverName, ushort serverCount, ushort roleCount)
        {
            var packet = new CliVerify.Pkt()
                .H(castleId).I(1).I(needValue).I(2).S(serverName)
                .H(serverCount);
            for (int i = 0; i < serverCount; i++) packet.I((uint)(10 + i)).S("服" + i).I((uint)(20 + i));
            packet.H(roleCount);
            for (int i = 0; i < roleCount; i++) packet.I((uint)(30 + i)).S("角" + i).I((uint)(40 + i)).C(i);
            return packet.H(50).H(60).Bytes();
        }

        private static bool Invoke(TSCrackController controller, string methodName, byte[] bytes)
        {
            MethodInfo method = typeof(TSCrackController).GetMethod(methodName, InstanceNonPublic);
            if (method == null) return false;
            var reader = new NetReader(bytes, 0, bytes.Length);
            method.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool IsFrame(byte[] frame, int protoId, byte[] payload)
        {
            if (frame == null || payload == null || frame.Length != 6 + payload.Length) return false;
            if (frame[0] != 0 || frame[1] != frame.Length || frame[2] != 0x03 || frame[3] != 0xE8) return false;
            if (frame[4] != (byte)(protoId >> 8) || frame[5] != (byte)protoId) return false;
            for (int i = 0; i < payload.Length; i++) if (frame[6 + i] != payload[i]) return false;
            return true;
        }
    }
}
