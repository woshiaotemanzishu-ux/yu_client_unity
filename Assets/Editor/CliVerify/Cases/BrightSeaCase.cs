using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.BrightSea;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class BrightSeaCase
    {
        private const BindingFlags InstanceNonPublic = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags StaticNonPublic = BindingFlags.Static | BindingFlags.NonPublic;

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunCore()); }
            catch (Exception e) { Debug.LogError("CLIVERIFY brightsea EXCEPTION " + e); return Task.FromResult(3); }
        }

        private static int RunCore()
        {
            BrightSeaController controller = BrightSeaController.Instance;
            BrightSeaModel model = BrightSeaModel.Instance;
            FieldInfo intercept = typeof(BrightSeaController).GetField("s_outboundIntercept", StaticNonPublic);
            var ambient = new AmbientState(controller, model, intercept);
            bool pass = false;
            bool restored = false;

            try
            {
                controller.Init();
                model.Clear();
                MethodInfo on18900 = typeof(BrightSeaController).GetMethod("On18900", InstanceNonPublic);
                MethodInfo on18901 = typeof(BrightSeaController).GetMethod("On18901", InstanceNonPublic);
                MethodInfo on18902 = typeof(BrightSeaController).GetMethod("On18902", InstanceNonPublic);
                MethodInfo on18915 = typeof(BrightSeaController).GetMethod("On18915", InstanceNonPublic);
                MethodInfo on18916 = typeof(BrightSeaController).GetMethod("On18916", InstanceNonPublic);
                IDictionary handlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                pass = intercept != null && on18900 != null && on18901 != null && on18902 != null && on18915 != null && on18916 != null && handlers != null
                    && handlers.Contains(18900) && handlers.Contains(18901) && handlers.Contains(18902) && handlers.Contains(18915) && handlers.Contains(18916);
                for (int proto = 18901; proto <= 18920; proto++)
                    pass &= handlers.Contains(proto) == (proto == 18901 || proto == 18902 || proto == 18915 || proto == 18916);

                var frames = new List<byte[]>();
                intercept.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                controller.RequestInfo();
                pass &= Frames(frames, 18900);
                frames.Clear();
                controller.RequestCruiseLogs();
                pass &= Frames(frames, 18901);
                frames.Clear();
                controller.RequestShipInfo();
                pass &= Frames(frames, 18902);
                frames.Clear();
                controller.RequestServerInfo();
                pass &= Frames(frames, 18915);
                frames.Clear();
                controller.RequestAssistBGoldInfo();
                pass &= Frames(frames, 18916);

                model.Replace("stale", 1, 1, 1, 1, 1, 1, 1, new List<BrightSeaModel.ShippingEntry> { new BrightSeaModel.ShippingEntry() });
                model.ReplaceCruiseLogs(new List<BrightSeaModel.CruiseLogEntry> { new BrightSeaModel.CruiseLogEntry() });
                model.ReplaceShipInfo(1, 2, 3, 4, 5, 6);
                model.ReplaceServerInfo(1, 2, new List<BrightSeaModel.ServerEntry> { new BrightSeaModel.ServerEntry() }, 3, 4, 5, new List<BrightSeaModel.ServerEntry>());
                model.ReplaceAssistBGoldInfo(1, 2);
                frames.Clear();
                EventDispatcher.Emit(GlobalEvent.EVT_GAME_START);
                pass &= Frames(frames, 18900) && !model.HasInfo && !model.HasCruiseLogs && !model.HasShipInfo && !model.HasServerInfo && !model.HasAssistBGoldInfo
                    && model.SendList.Count == 0 && model.CruiseLogs.Count == 0 && model.EnemyServers.Count == 0 && model.UnsatisfiedServers.Count == 0;

                string chinese = "海域中文";
                Invoke(on18900, controller, MainPacket(chinese), out int mainRemaining);
                pass &= mainRemaining == 0 && model.HasInfo && model.Picture == chinese
                    && model.PictureVersion == uint.MaxValue && model.RewardTimes == byte.MaxValue
                    && model.TotalRewardTimes == 0 && model.RobTimes == 254 && model.TotalRobTimes == 1
                    && model.AutoId == ulong.MaxValue && model.Status == 253 && model.SendList.Count == 2
                    && model.SendList[0].AutoId == 77 && model.SendList[1].AutoId == 77
                    && model.SendList[0].ShippingId == 1 && model.SendList[0].ServerId == uint.MaxValue
                    && model.SendList[0].ServerNumber == 4000000000U && model.SendList[0].GuildId == ulong.MaxValue
                    && model.SendList[0].GuildName == chinese && model.SendList[0].RoleId == ulong.MaxValue
                    && model.SendList[0].RoleName == "" && model.SendList[0].RoleLevel == ushort.MaxValue
                    && model.SendList[0].Power == ulong.MaxValue && model.SendList[0].Sex == byte.MaxValue
                    && model.SendList[0].Career == ushort.MaxValue && model.SendList[0].Turn == 254
                    && model.SendList[0].Picture == "" && model.SendList[0].PictureVersion == uint.MaxValue
                    && model.SendList[0].EndTime == uint.MaxValue && model.SendList[0].RobTimes == byte.MaxValue
                    && model.SendList[1].ShippingId == 2 && model.SendList[1].ServerId == 3
                    && model.SendList[1].ServerNumber == 4 && model.SendList[1].GuildId == 5
                    && model.SendList[1].GuildName == "g" && model.SendList[1].RoleId == 6
                    && model.SendList[1].RoleName == "r" && model.SendList[1].RoleLevel == 7
                    && model.SendList[1].Power == 8 && model.SendList[1].Sex == 9
                    && model.SendList[1].Career == 10 && model.SendList[1].Turn == 11
                    && model.SendList[1].Picture == "p" && model.SendList[1].PictureVersion == 12
                    && model.SendList[1].EndTime == 13 && model.SendList[1].RobTimes == 14;
                Invoke(on18915, controller, ServerMultiPacket(chinese), out int serverRemaining);
                pass &= serverRemaining == 0 && model.HasInfo && model.Picture == chinese && model.HasServerInfo
                    && model.TreasureModule == byte.MaxValue && model.WorldLevel == ushort.MaxValue
                    && model.EnemyServers.Count == 2 && model.EnemyServers[0].ServerId == uint.MaxValue
                    && model.EnemyServers[0].ServerNumber == ushort.MaxValue && model.EnemyServers[0].ServerName == chinese
                    && model.EnemyServers[0].WorldLevel == 0 && model.EnemyServers[1].ServerId == 1
                    && model.EnemyServers[1].ServerNumber == 2 && model.EnemyServers[1].ServerName == ""
                    && model.EnemyServers[1].WorldLevel == ushort.MaxValue
                    && model.UnsatisfiedModule == 254 && model.UnsatisfiedWorldLevel == ushort.MaxValue
                    && model.MinWorldLevel == 0 && model.UnsatisfiedServers.Count == 2
                    && model.UnsatisfiedServers[0].ServerId == 9 && model.UnsatisfiedServers[0].ServerNumber == 10
                    && model.UnsatisfiedServers[0].ServerName == "u" && model.UnsatisfiedServers[0].WorldLevel == 11
                    && model.UnsatisfiedServers[1].ServerId == 9 && model.UnsatisfiedServers[1].ServerNumber == 12
                    && model.UnsatisfiedServers[1].ServerName == "v" && model.UnsatisfiedServers[1].WorldLevel == 13;

                Invoke(on18901, controller, LogsMultiPacket(chinese), out int logsRemaining);
                BrightSeaModel.CruiseLogEntry firstLog = model.CruiseLogs.Count > 0 ? model.CruiseLogs[0] : null;
                BrightSeaModel.CruiseLogEntry secondLog = model.CruiseLogs.Count > 1 ? model.CruiseLogs[1] : null;
                pass &= logsRemaining == 0 && model.HasInfo && model.HasServerInfo && model.HasCruiseLogs && model.CruiseLogs.Count == 2
                    && firstLog != null && firstLog.AutoId == ulong.MaxValue && firstLog.Type == byte.MaxValue
                    && firstLog.RoberServerId == uint.MaxValue && firstLog.RoberServerNumber == 4000000000U
                    && firstLog.RoberGuildId == ulong.MaxValue && firstLog.RoberGuildName == chinese
                    && firstLog.RoberId == ulong.MaxValue && firstLog.RoberName == "" && firstLog.RoberPower == ulong.MaxValue
                    && firstLog.ShippingId == 254 && firstLog.Reward.Count == 2 && firstLog.Reward[0].Type == 1
                    && firstLog.Reward[0].TypeId == uint.MaxValue && firstLog.Reward[0].Num == uint.MaxValue
                    && firstLog.Reward[1].Type == 1 && firstLog.Reward[1].TypeId == uint.MaxValue && firstLog.Reward[1].Num == uint.MaxValue
                    && firstLog.BackList.Count == 0 && firstLog.ReceiveList.Count == 1 && firstLog.ReceiveList[0].Type == byte.MaxValue
                    && firstLog.ReceiveList[0].TypeId == 5 && firstLog.ReceiveList[0].Num == 6 && firstLog.Time == uint.MaxValue
                    && secondLog != null && secondLog.AutoId == ulong.MaxValue && secondLog.Type == 7
                    && secondLog.RoberServerId == 8 && secondLog.RoberServerNumber == 9 && secondLog.RoberGuildId == 10
                    && secondLog.RoberGuildName == "g" && secondLog.RoberId == 11 && secondLog.RoberName == "r"
                    && secondLog.RoberPower == 12 && secondLog.ShippingId == 13 && secondLog.Reward.Count == 0
                    && secondLog.BackList.Count == 1 && secondLog.BackList[0].Type == 14 && secondLog.BackList[0].TypeId == 15
                    && secondLog.BackList[0].Num == 16 && secondLog.ReceiveList.Count == 0 && secondLog.Time == 17;

                Invoke(on18902, controller, ShipPacket(255, 65535, 254, 253, 252, 251), out int shipRemaining);
                pass &= shipRemaining == 0 && model.HasShipInfo && model.ShippingId == 255 && model.LuckeyValue == ushort.MaxValue
                    && model.ShipRewardTimes == 254 && model.ShipTotalRewardTimes == 253 && model.UpTimes == 252 && model.TotalUpTimes == 251
                    && model.HasInfo && model.HasCruiseLogs && model.HasServerInfo;

                var fourBeforeAssist = new SavedState(model);
                Invoke(on18916, controller, AssistPacket(ushort.MaxValue, ushort.MaxValue), out int assistRemaining);
                pass &= assistRemaining == 0 && model.HasAssistBGoldInfo && model.AssistBGoldNum == ushort.MaxValue && model.AssistBGoldMax == ushort.MaxValue
                    && fourBeforeAssist.MatchesCoreFour(model);
                Invoke(on18916, controller, AssistPacket(7, ushort.MaxValue), out assistRemaining);
                pass &= assistRemaining == 0 && model.HasAssistBGoldInfo && model.AssistBGoldNum == 7 && model.AssistBGoldMax == ushort.MaxValue && fourBeforeAssist.MatchesCoreFour(model);

                Invoke(on18900, controller, MainSinglePacket(), out mainRemaining);
                pass &= mainRemaining == 0 && model.HasInfo && model.Picture == "next" && model.PictureVersion == 1
                    && model.RewardTimes == 2 && model.TotalRewardTimes == 3 && model.RobTimes == 4
                    && model.TotalRobTimes == 5 && model.AutoId == 2 && model.Status == 6
                    && model.SendList.Count == 1 && model.SendList[0].AutoId == 3 && model.SendList[0].RoleName == "solo"
                    && model.HasServerInfo && model.EnemyServers.Count == 2 && model.UnsatisfiedServers.Count == 2
                    && model.HasCruiseLogs && model.CruiseLogs.Count == 2 && model.CruiseLogs[0].AutoId == ulong.MaxValue && model.HasShipInfo
                    && AssistIs(model, 7, ushort.MaxValue);

                Invoke(on18900, controller, MainEmptyPacket(), out mainRemaining);
                pass &= mainRemaining == 0 && model.HasInfo && model.SendList.Count == 0 && model.Picture == ""
                    && model.HasServerInfo && model.EnemyServers.Count == 2 && model.UnsatisfiedServers.Count == 2;

                Invoke(on18915, controller, ServerSinglePacket(), out serverRemaining);
                pass &= serverRemaining == 0 && model.HasInfo && model.HasServerInfo && model.EnemyServers.Count == 1
                    && model.EnemyServers[0].ServerId == 3 && model.EnemyServers[0].ServerName == "solo"
                    && model.UnsatisfiedServers.Count == 1 && model.UnsatisfiedServers[0].ServerName == "tail"
                    && model.HasCruiseLogs && model.CruiseLogs.Count == 2 && model.CruiseLogs[1].AutoId == ulong.MaxValue && model.HasShipInfo
                    && AssistIs(model, 7, ushort.MaxValue);

                Invoke(on18915, controller, ServerEmptyPacket(), out serverRemaining);
                pass &= serverRemaining == 0 && model.HasServerInfo && model.EnemyServers.Count == 0
                    && model.UnsatisfiedServers.Count == 0 && model.TreasureModule == 0 && model.WorldLevel == 0;

                Invoke(on18901, controller, LogsSinglePacket(), out logsRemaining);
                pass &= logsRemaining == 0 && model.HasCruiseLogs && model.CruiseLogs.Count == 1
                    && model.CruiseLogs[0].AutoId == 18 && model.CruiseLogs[0].RoberName == "single"
                    && model.HasInfo && model.SendList.Count == 0 && model.HasServerInfo && model.EnemyServers.Count == 0 && model.HasShipInfo
                    && AssistIs(model, 7, ushort.MaxValue);

                Invoke(on18901, controller, LogsEmptyPacket(), out logsRemaining);
                pass &= logsRemaining == 0 && model.HasCruiseLogs && model.CruiseLogs.Count == 0
                    && model.HasInfo && model.HasServerInfo && model.HasShipInfo;

                Invoke(on18902, controller, ShipPacket(1, 2, 3, 4, 5, 6), out shipRemaining);
                pass &= shipRemaining == 0 && model.HasShipInfo && model.ShippingId == 1 && model.LuckeyValue == 2
                    && model.ShipRewardTimes == 3 && model.ShipTotalRewardTimes == 4 && model.UpTimes == 5 && model.TotalUpTimes == 6
                    && model.HasInfo && model.HasCruiseLogs && model.HasServerInfo && AssistIs(model, 7, ushort.MaxValue);
                frames.Clear();
                controller.RequestShipInfo();
                pass &= Frames(frames, 18902) && model.HasShipInfo && model.ShippingId == 1 && model.LuckeyValue == 2
                    && model.ShipRewardTimes == 3 && model.ShipTotalRewardTimes == 4 && model.UpTimes == 5 && model.TotalUpTimes == 6
                    && model.HasInfo && model.HasCruiseLogs && model.HasServerInfo;
                Invoke(on18902, controller, ShipPacket(0, 0, 0, 0, 0, 0), out shipRemaining);
                pass &= shipRemaining == 0 && model.HasShipInfo && model.ShippingId == 0 && model.LuckeyValue == 0
                    && model.ShipRewardTimes == 0 && model.ShipTotalRewardTimes == 0 && model.UpTimes == 0 && model.TotalUpTimes == 0
                    && model.HasInfo && model.HasCruiseLogs && model.HasServerInfo;
                frames.Clear();
                controller.RequestAssistBGoldInfo();
                pass &= Frames(frames, 18916) && model.HasAssistBGoldInfo && model.AssistBGoldNum == 7 && model.AssistBGoldMax == ushort.MaxValue;
                Invoke(on18916, controller, AssistPacket(0, 0), out assistRemaining);
                pass &= assistRemaining == 0 && model.HasAssistBGoldInfo && model.AssistBGoldNum == 0 && model.AssistBGoldMax == 0
                    && model.HasInfo && model.HasCruiseLogs && model.HasShipInfo && model.HasServerInfo;

                model.ReplaceServerInfo(7, 8, new List<BrightSeaModel.ServerEntry> { new BrightSeaModel.ServerEntry { ServerId = 9 } }, 10, 11, 12, new List<BrightSeaModel.ServerEntry>());
                pass &= model.HasServerInfo && model.EnemyServers.Count == 1 && model.EnemyServers[0].ServerId == 9;

                model.ReplaceCruiseLogs(new List<BrightSeaModel.CruiseLogEntry> { new BrightSeaModel.CruiseLogEntry { AutoId = 15 } });
                pass &= model.HasCruiseLogs && model.CruiseLogs.Count == 1 && model.CruiseLogs[0].AutoId == 15;

                model.Clear();
                model.Replace("main-only", 1, 2, 3, 4, 5, 6, 7, new List<BrightSeaModel.ShippingEntry> { new BrightSeaModel.ShippingEntry { AutoId = 8 } });
                var mainOnly = new SavedState(model);
                model.Clear();
                mainOnly.Restore(model);
                pass &= model.HasInfo && !model.HasCruiseLogs && !model.HasServerInfo && model.Picture == "main-only"
                    && model.SendList.Count == 1 && model.SendList[0].AutoId == 8;

                model.Clear();
                model.ReplaceServerInfo(9, 10, new List<BrightSeaModel.ServerEntry> { new BrightSeaModel.ServerEntry { ServerId = 11 } }, 12, 13, 14, new List<BrightSeaModel.ServerEntry>());
                var serverOnly = new SavedState(model);
                model.Clear();
                serverOnly.Restore(model);
                pass &= !model.HasInfo && !model.HasCruiseLogs && model.HasServerInfo && model.TreasureModule == 9
                    && model.EnemyServers.Count == 1 && model.EnemyServers[0].ServerId == 11;

                model.Clear();
                model.ReplaceCruiseLogs(new List<BrightSeaModel.CruiseLogEntry> { new BrightSeaModel.CruiseLogEntry { AutoId = 12 } });
                var logsOnly = new SavedState(model);
                model.Clear();
                logsOnly.Restore(model);
                pass &= !model.HasInfo && model.HasCruiseLogs && !model.HasServerInfo
                    && model.CruiseLogs.Count == 1 && model.CruiseLogs[0].AutoId == 12;

                model.Clear();
                model.ReplaceShipInfo(9, 10, 11, 12, 13, 14);
                var shipOnly = new SavedState(model);
                model.Clear();
                shipOnly.Restore(model);
                pass &= !model.HasInfo && !model.HasCruiseLogs && !model.HasServerInfo && model.HasShipInfo
                    && model.ShippingId == 9 && model.LuckeyValue == 10 && model.TotalUpTimes == 14;

                model.Clear();
                model.ReplaceAssistBGoldInfo(12, 13);
                var assistOnly = new SavedState(model);
                model.Clear();
                assistOnly.Restore(model);
                pass &= !model.HasInfo && !model.HasCruiseLogs && !model.HasServerInfo && !model.HasShipInfo && model.HasAssistBGoldInfo
                    && model.AssistBGoldNum == 12 && model.AssistBGoldMax == 13;

                model.Clear();
                model.Replace("four", 1, 2, 3, 4, 5, 6, 7, new List<BrightSeaModel.ShippingEntry> { new BrightSeaModel.ShippingEntry { AutoId = 8 } });
                model.ReplaceCruiseLogs(new List<BrightSeaModel.CruiseLogEntry> { new BrightSeaModel.CruiseLogEntry { AutoId = 9 } });
                model.ReplaceShipInfo(10, 11, 12, 13, 14, 15);
                model.ReplaceServerInfo(16, 17, new List<BrightSeaModel.ServerEntry> { new BrightSeaModel.ServerEntry { ServerId = 18 } }, 19, 20, 21, new List<BrightSeaModel.ServerEntry> { new BrightSeaModel.ServerEntry { ServerId = 22 } });
                model.ReplaceAssistBGoldInfo(23, 24);
                var allSlices = new SavedState(model);
                model.Clear();
                allSlices.Restore(model);
                pass &= model.HasInfo && model.Picture == "four" && model.SendList.Count == 1 && model.SendList[0].AutoId == 8
                    && model.HasCruiseLogs && model.CruiseLogs.Count == 1 && model.CruiseLogs[0].AutoId == 9
                    && model.HasShipInfo && model.ShippingId == 10 && model.LuckeyValue == 11 && model.ShipRewardTimes == 12
                    && model.ShipTotalRewardTimes == 13 && model.UpTimes == 14 && model.TotalUpTimes == 15
                    && model.HasServerInfo && model.TreasureModule == 16 && model.WorldLevel == 17 && model.EnemyServers.Count == 1
                    && model.EnemyServers[0].ServerId == 18 && model.UnsatisfiedModule == 19 && model.UnsatisfiedWorldLevel == 20
                    && model.MinWorldLevel == 21 && model.UnsatisfiedServers.Count == 1 && model.UnsatisfiedServers[0].ServerId == 22;
                pass &= model.HasAssistBGoldInfo && model.AssistBGoldNum == 23 && model.AssistBGoldMax == 24;

                controller.Dispose();
                frames.Clear();
                EventDispatcher.Emit(GlobalEvent.EVT_GAME_START);
                pass &= !controller.IsInitialized && !handlers.Contains(18900) && !handlers.Contains(18901) && !handlers.Contains(18902) && !handlers.Contains(18915) && !handlers.Contains(18916)
                    && !model.HasInfo && !model.HasCruiseLogs && !model.HasShipInfo && !model.HasServerInfo && !model.HasAssistBGoldInfo && model.SendList.Count == 0
                    && model.CruiseLogs.Count == 0 && model.EnemyServers.Count == 0 && model.UnsatisfiedServers.Count == 0 && frames.Count == 0;
            }
            finally
            {
                restored = ambient.Restore(controller, model, intercept);
                Debug.Log("CLIVERIFY brightsea restored=" + restored + " VERDICT pass=" + pass);
            }
            return pass && restored ? 0 : 3;
        }

        private static void Invoke(MethodInfo method, BrightSeaController controller, byte[] bytes, out int remaining)
        {
            var reader = new NetReader(bytes, 0, bytes.Length);
            method.Invoke(controller, new object[] { reader });
            remaining = reader.Remaining;
        }

        private static bool Frames(List<byte[]> frames, int proto) => frames.Count == 1 && frames[0].Length == 6
            && frames[0][0] == 0 && frames[0][1] == 6 && frames[0][2] == 3 && frames[0][3] == 232
            && frames[0][4] == (byte)(proto >> 8) && frames[0][5] == (byte)proto;

        private static byte[] MainPacket(string chinese) => new CliVerify.Pkt().S(chinese).I(4294967295L).C(255).C(0).C(254).C(1).L(-1).C(253).H(2)
            .L(77).C(1).I(4294967295L).I(4000000000L).L(-1).S(chinese).L(-1).S("").H(65535).L(-1)
            .C(255).H(65535).C(254).S("").I(4294967295L).I(4294967295L).C(255)
            .L(77).C(2).I(3).I(4).L(5).S("g").L(6).S("r").H(7).L(8).C(9).H(10).C(11).S("p").I(12).I(13).C(14).Bytes();

        private static byte[] MainSinglePacket() => new CliVerify.Pkt().S("next").I(1).C(2).C(3).C(4).C(5).L(2).C(6).H(1)
            .L(3).C(4).I(5).I(6).L(7).S("guild").L(8).S("solo").H(9).L(10).C(11).H(12).C(13).S("pic").I(14).I(15).C(16).Bytes();

        private static byte[] MainEmptyPacket() => new CliVerify.Pkt().S("").I(0).C(0).C(0).C(0).C(0).L(0).C(0).H(0).Bytes();

        private static byte[] LogsMultiPacket(string chinese) => new CliVerify.Pkt().H(2)
            .L(-1).C(255).I(4294967295L).I(4000000000L).L(-1).S(chinese).L(-1).S("").L(-1).C(254)
            .H(2).C(1).I(4294967295L).I(4294967295L).C(1).I(4294967295L).I(4294967295L).H(0).H(1).C(255).I(5).I(6).I(4294967295L)
            .L(-1).C(7).I(8).I(9).L(10).S("g").L(11).S("r").L(12).C(13)
            .H(0).H(1).C(14).I(15).I(16).H(0).I(17).Bytes();

        private static byte[] LogsSinglePacket() => new CliVerify.Pkt().H(1)
            .L(18).C(19).I(20).I(21).L(22).S("").L(23).S("single").L(24).C(25)
            .H(1).C(26).I(27).I(28).H(0).H(0).I(29).Bytes();

        private static byte[] LogsEmptyPacket() => new CliVerify.Pkt().H(0).Bytes();

        private static byte[] ShipPacket(byte shippingId, ushort luckeyValue, byte rewardTimes, byte totalRewardTimes, byte upTimes, byte totalUpTimes)
            => new CliVerify.Pkt().C(shippingId).H(luckeyValue).C(rewardTimes).C(totalRewardTimes).C(upTimes).C(totalUpTimes).Bytes();

        private static byte[] AssistPacket(ushort num, ushort max) => new CliVerify.Pkt().H(num).H(max).Bytes();

        private static byte[] ServerMultiPacket(string chinese) => new CliVerify.Pkt().C(255).H(65535).H(2)
            .I(4294967295L).H(65535).S(chinese).H(0).I(1).H(2).S("").H(65535)
            .C(254).H(65535).H(0).H(2).I(9).H(10).S("u").H(11).I(9).H(12).S("v").H(13).Bytes();

        private static byte[] ServerSinglePacket() => new CliVerify.Pkt().C(1).H(2).H(1).I(3).H(4).S("solo").H(5)
            .C(6).H(7).H(8).H(1).I(9).H(10).S("tail").H(11).Bytes();

        private static byte[] ServerEmptyPacket() => new CliVerify.Pkt().C(0).H(0).H(0).C(0).H(0).H(0).H(0).Bytes();

        private static bool AssistIs(BrightSeaModel m, ushort num, ushort max) => m.HasAssistBGoldInfo && m.AssistBGoldNum == num && m.AssistBGoldMax == max;

        private sealed class AmbientState
        {
            private static readonly int[] Protocols = { 18900, 18901, 18902, 18915, 18916 };
            private readonly bool _initialized;
            private readonly SavedState _model;
            private readonly object _intercept;
            private readonly Dictionary<int, object> _netHandlers = new Dictionary<int, object>();
            private readonly bool _hadGameStart;
            private readonly List<Delegate> _gameStartHandlers;

            public AmbientState(BrightSeaController controller, BrightSeaModel model, FieldInfo intercept)
            {
                _initialized = controller.IsInitialized;
                _model = new SavedState(model);
                _intercept = intercept == null ? null : intercept.GetValue(null);
                IDictionary net = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                if (net != null) foreach (int proto in Protocols) if (net.Contains(proto)) _netHandlers[proto] = net[proto];
                IDictionary events = typeof(EventDispatcher).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                _hadGameStart = events != null && events.Contains(GlobalEvent.EVT_GAME_START);
                if (_hadGameStart) _gameStartHandlers = new List<Delegate>((IList<Delegate>)events[GlobalEvent.EVT_GAME_START]);
            }

            public bool Restore(BrightSeaController controller, BrightSeaModel model, FieldInfo intercept)
            {
                try
                {
                    if (controller.IsInitialized) controller.Dispose();
                    model.Clear();
                    _model.Restore(model);
                    if (_initialized) controller.Init();

                    IDictionary net = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                    if (net == null) return false;
                    foreach (int proto in Protocols)
                    {
                        if (_netHandlers.TryGetValue(proto, out object handler)) net[proto] = handler;
                        else net.Remove(proto);
                    }

                    IDictionary events = typeof(EventDispatcher).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                    if (events == null) return false;
                    if (_hadGameStart) events[GlobalEvent.EVT_GAME_START] = new List<Delegate>(_gameStartHandlers);
                    else events.Remove(GlobalEvent.EVT_GAME_START);
                    if (intercept != null) intercept.SetValue(null, _intercept);
                    return controller.IsInitialized == _initialized && _model.Matches(model)
                        && HandlersMatch(net) && EventHandlersMatch(events)
                        && (intercept == null || ReferenceEquals(intercept.GetValue(null), _intercept));
                }
                catch (Exception e)
                {
                    Debug.LogError("CLIVERIFY brightsea restore EXCEPTION " + e);
                    return false;
                }
            }

            private bool HandlersMatch(IDictionary net)
            {
                foreach (int proto in Protocols)
                {
                    bool had = _netHandlers.TryGetValue(proto, out object expected);
                    if (net.Contains(proto) != had || (had && !ReferenceEquals(net[proto], expected))) return false;
                }
                return true;
            }

            private bool EventHandlersMatch(IDictionary events)
            {
                if (events.Contains(GlobalEvent.EVT_GAME_START) != _hadGameStart) return false;
                if (!_hadGameStart) return true;
                IList<Delegate> actual = events[GlobalEvent.EVT_GAME_START] as IList<Delegate>;
                if (actual == null || actual.Count != _gameStartHandlers.Count) return false;
                for (int i = 0; i < actual.Count; i++) if (!ReferenceEquals(actual[i], _gameStartHandlers[i])) return false;
                return true;
            }
        }

        private sealed class SavedState
        {
            private readonly bool _info, _cruiseLogs, _shipInfo, _serverInfo, _assistBGold;
            private readonly string _picture;
            private readonly uint _pictureVersion;
            private readonly byte _reward, _totalReward, _rob, _totalRob, _status, _module, _unsatisfiedModule;
            private readonly ulong _autoId;
            private readonly ushort _world, _unsatisfiedWorld, _minWorld;
            private readonly byte _shippingId, _shipReward, _shipTotalReward, _up, _totalUp;
            private readonly ushort _luckey;
            private readonly ushort _assistBGoldNum, _assistBGoldMax;
            private readonly List<BrightSeaModel.ShippingEntry> _ships;
            private readonly List<BrightSeaModel.CruiseLogEntry> _logs;
            private readonly List<BrightSeaModel.ServerEntry> _enemy, _unsatisfied;

            public SavedState(BrightSeaModel m)
            {
                _info = m.HasInfo; _cruiseLogs = m.HasCruiseLogs; _shipInfo = m.HasShipInfo; _serverInfo = m.HasServerInfo; _assistBGold = m.HasAssistBGoldInfo; _picture = m.Picture; _pictureVersion = m.PictureVersion;
                _reward = m.RewardTimes; _totalReward = m.TotalRewardTimes; _rob = m.RobTimes; _totalRob = m.TotalRobTimes;
                _status = m.Status; _autoId = m.AutoId; _module = m.TreasureModule; _world = m.WorldLevel;
                _unsatisfiedModule = m.UnsatisfiedModule; _unsatisfiedWorld = m.UnsatisfiedWorldLevel; _minWorld = m.MinWorldLevel;
                _shippingId = m.ShippingId; _luckey = m.LuckeyValue; _shipReward = m.ShipRewardTimes; _shipTotalReward = m.ShipTotalRewardTimes; _up = m.UpTimes; _totalUp = m.TotalUpTimes;
                _assistBGoldNum = m.AssistBGoldNum; _assistBGoldMax = m.AssistBGoldMax;
                _ships = m.SendList.ConvertAll(CloneShipping); _logs = m.CruiseLogs.ConvertAll(CloneLog);
                _enemy = m.EnemyServers.ConvertAll(CloneServer);
                _unsatisfied = m.UnsatisfiedServers.ConvertAll(CloneServer);
            }

            public void Restore(BrightSeaModel m)
            {
                if (_info) m.Replace(_picture, _pictureVersion, _reward, _totalReward, _rob, _totalRob, _autoId, _status, _ships);
                if (_cruiseLogs) m.ReplaceCruiseLogs(_logs);
                if (_shipInfo) m.ReplaceShipInfo(_shippingId, _luckey, _shipReward, _shipTotalReward, _up, _totalUp);
                if (_serverInfo) m.ReplaceServerInfo(_module, _world, _enemy, _unsatisfiedModule, _unsatisfiedWorld, _minWorld, _unsatisfied);
                if (_assistBGold) m.ReplaceAssistBGoldInfo(_assistBGoldNum, _assistBGoldMax);
            }

            public bool Matches(BrightSeaModel m)
            {
                return MatchesCoreFour(m) && _assistBGold == m.HasAssistBGoldInfo
                    && _assistBGoldNum == m.AssistBGoldNum && _assistBGoldMax == m.AssistBGoldMax;
            }

            public bool MatchesCoreFour(BrightSeaModel m)
            {
                return _info == m.HasInfo && _cruiseLogs == m.HasCruiseLogs && _shipInfo == m.HasShipInfo && _serverInfo == m.HasServerInfo
                    && _picture == m.Picture && _pictureVersion == m.PictureVersion && _reward == m.RewardTimes && _totalReward == m.TotalRewardTimes
                    && _rob == m.RobTimes && _totalRob == m.TotalRobTimes && _status == m.Status && _autoId == m.AutoId
                    && _module == m.TreasureModule && _world == m.WorldLevel && _unsatisfiedModule == m.UnsatisfiedModule
                    && _unsatisfiedWorld == m.UnsatisfiedWorldLevel && _minWorld == m.MinWorldLevel && _shippingId == m.ShippingId
                    && _luckey == m.LuckeyValue && _shipReward == m.ShipRewardTimes && _shipTotalReward == m.ShipTotalRewardTimes
                    && _up == m.UpTimes && _totalUp == m.TotalUpTimes
                    && SameList(_ships, m.SendList) && SameList(_logs, m.CruiseLogs) && SameList(_enemy, m.EnemyServers) && SameList(_unsatisfied, m.UnsatisfiedServers);
            }

            private static bool SameList<T>(List<T> expected, List<T> actual)
            {
                if (expected.Count != actual.Count) return false;
                for (int i = 0; i < expected.Count; i++) if (!SameObject(expected[i], actual[i])) return false;
                return true;
            }

            private static BrightSeaModel.ShippingEntry CloneShipping(BrightSeaModel.ShippingEntry x) => new BrightSeaModel.ShippingEntry
            {
                AutoId = x.AutoId, ShippingId = x.ShippingId, ServerId = x.ServerId, ServerNumber = x.ServerNumber, GuildId = x.GuildId, GuildName = x.GuildName,
                RoleId = x.RoleId, RoleName = x.RoleName, RoleLevel = x.RoleLevel, Power = x.Power, Sex = x.Sex, Career = x.Career, Turn = x.Turn,
                Picture = x.Picture, PictureVersion = x.PictureVersion, EndTime = x.EndTime, RobTimes = x.RobTimes
            };

            private static BrightSeaModel.ServerEntry CloneServer(BrightSeaModel.ServerEntry x) => new BrightSeaModel.ServerEntry
            { ServerId = x.ServerId, ServerNumber = x.ServerNumber, ServerName = x.ServerName, WorldLevel = x.WorldLevel };

            private static BrightSeaModel.ObjectEntry CloneObject(BrightSeaModel.ObjectEntry x) => new BrightSeaModel.ObjectEntry { Type = x.Type, TypeId = x.TypeId, Num = x.Num };

            private static BrightSeaModel.CruiseLogEntry CloneLog(BrightSeaModel.CruiseLogEntry x)
            {
                var clone = new BrightSeaModel.CruiseLogEntry
                {
                    AutoId = x.AutoId, Type = x.Type, RoberServerId = x.RoberServerId, RoberServerNumber = x.RoberServerNumber,
                    RoberGuildId = x.RoberGuildId, RoberGuildName = x.RoberGuildName, RoberId = x.RoberId, RoberName = x.RoberName,
                    RoberPower = x.RoberPower, ShippingId = x.ShippingId, Time = x.Time
                };
                clone.Reward.AddRange(x.Reward.ConvertAll(CloneObject));
                clone.BackList.AddRange(x.BackList.ConvertAll(CloneObject));
                clone.ReceiveList.AddRange(x.ReceiveList.ConvertAll(CloneObject));
                return clone;
            }

            private static bool SameObject(object expected, object actual)
            {
                if (ReferenceEquals(expected, actual)) return true;
                if (expected == null || actual == null || expected.GetType() != actual.GetType()) return false;
                Type type = expected.GetType();
                if (type.IsPrimitive || type == typeof(string)) return expected.Equals(actual);
                if (expected is IEnumerable expectedList && actual is IEnumerable actualList)
                {
                    IEnumerator a = expectedList.GetEnumerator(), b = actualList.GetEnumerator();
                    while (a.MoveNext())
                    {
                        if (!b.MoveNext() || !SameObject(a.Current, b.Current)) return false;
                    }
                    return !b.MoveNext();
                }
                foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
                    if (!SameObject(field.GetValue(expected), field.GetValue(actual))) return false;
                return true;
            }
        }
    }
}
