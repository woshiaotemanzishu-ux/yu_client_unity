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
            bool wasInitialized = controller.IsInitialized;
            SavedState old = new SavedState(model);
            FieldInfo intercept = typeof(BrightSeaController).GetField("s_outboundIntercept", StaticNonPublic);
            object oldIntercept = intercept == null ? null : intercept.GetValue(null);

            try
            {
                controller.Init();
                model.Clear();
                MethodInfo on18900 = typeof(BrightSeaController).GetMethod("On18900", InstanceNonPublic);
                MethodInfo on18901 = typeof(BrightSeaController).GetMethod("On18901", InstanceNonPublic);
                MethodInfo on18902 = typeof(BrightSeaController).GetMethod("On18902", InstanceNonPublic);
                MethodInfo on18915 = typeof(BrightSeaController).GetMethod("On18915", InstanceNonPublic);
                IDictionary handlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                bool pass = intercept != null && on18900 != null && on18901 != null && on18902 != null && on18915 != null && handlers != null
                    && handlers.Contains(18900) && handlers.Contains(18901) && handlers.Contains(18902) && handlers.Contains(18915);
                for (int proto = 18901; proto <= 18920; proto++)
                    pass &= handlers.Contains(proto) == (proto == 18901 || proto == 18902 || proto == 18915);

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

                model.Replace("stale", 1, 1, 1, 1, 1, 1, 1, new List<BrightSeaModel.ShippingEntry> { new BrightSeaModel.ShippingEntry() });
                model.ReplaceCruiseLogs(new List<BrightSeaModel.CruiseLogEntry> { new BrightSeaModel.CruiseLogEntry() });
                model.ReplaceShipInfo(1, 2, 3, 4, 5, 6);
                model.ReplaceServerInfo(1, 2, new List<BrightSeaModel.ServerEntry> { new BrightSeaModel.ServerEntry() }, 3, 4, 5, new List<BrightSeaModel.ServerEntry>());
                frames.Clear();
                EventDispatcher.Emit(GlobalEvent.EVT_GAME_START);
                pass &= Frames(frames, 18900) && !model.HasInfo && !model.HasCruiseLogs && !model.HasShipInfo && !model.HasServerInfo
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

                Invoke(on18900, controller, MainSinglePacket(), out mainRemaining);
                pass &= mainRemaining == 0 && model.HasInfo && model.Picture == "next" && model.PictureVersion == 1
                    && model.RewardTimes == 2 && model.TotalRewardTimes == 3 && model.RobTimes == 4
                    && model.TotalRobTimes == 5 && model.AutoId == 2 && model.Status == 6
                    && model.SendList.Count == 1 && model.SendList[0].AutoId == 3 && model.SendList[0].RoleName == "solo"
                    && model.HasServerInfo && model.EnemyServers.Count == 2 && model.UnsatisfiedServers.Count == 2
                    && model.HasCruiseLogs && model.CruiseLogs.Count == 2 && model.CruiseLogs[0].AutoId == ulong.MaxValue && model.HasShipInfo;

                Invoke(on18900, controller, MainEmptyPacket(), out mainRemaining);
                pass &= mainRemaining == 0 && model.HasInfo && model.SendList.Count == 0 && model.Picture == ""
                    && model.HasServerInfo && model.EnemyServers.Count == 2 && model.UnsatisfiedServers.Count == 2;

                Invoke(on18915, controller, ServerSinglePacket(), out serverRemaining);
                pass &= serverRemaining == 0 && model.HasInfo && model.HasServerInfo && model.EnemyServers.Count == 1
                    && model.EnemyServers[0].ServerId == 3 && model.EnemyServers[0].ServerName == "solo"
                    && model.UnsatisfiedServers.Count == 1 && model.UnsatisfiedServers[0].ServerName == "tail"
                    && model.HasCruiseLogs && model.CruiseLogs.Count == 2 && model.CruiseLogs[1].AutoId == ulong.MaxValue && model.HasShipInfo;

                Invoke(on18915, controller, ServerEmptyPacket(), out serverRemaining);
                pass &= serverRemaining == 0 && model.HasServerInfo && model.EnemyServers.Count == 0
                    && model.UnsatisfiedServers.Count == 0 && model.TreasureModule == 0 && model.WorldLevel == 0;

                Invoke(on18901, controller, LogsSinglePacket(), out logsRemaining);
                pass &= logsRemaining == 0 && model.HasCruiseLogs && model.CruiseLogs.Count == 1
                    && model.CruiseLogs[0].AutoId == 18 && model.CruiseLogs[0].RoberName == "single"
                    && model.HasInfo && model.SendList.Count == 0 && model.HasServerInfo && model.EnemyServers.Count == 0 && model.HasShipInfo;

                Invoke(on18901, controller, LogsEmptyPacket(), out logsRemaining);
                pass &= logsRemaining == 0 && model.HasCruiseLogs && model.CruiseLogs.Count == 0
                    && model.HasInfo && model.HasServerInfo && model.HasShipInfo;

                Invoke(on18902, controller, ShipPacket(1, 2, 3, 4, 5, 6), out shipRemaining);
                pass &= shipRemaining == 0 && model.HasShipInfo && model.ShippingId == 1 && model.LuckeyValue == 2
                    && model.ShipRewardTimes == 3 && model.ShipTotalRewardTimes == 4 && model.UpTimes == 5 && model.TotalUpTimes == 6
                    && model.HasInfo && model.HasCruiseLogs && model.HasServerInfo;
                frames.Clear();
                controller.RequestShipInfo();
                pass &= Frames(frames, 18902) && model.HasShipInfo && model.ShippingId == 1 && model.LuckeyValue == 2
                    && model.ShipRewardTimes == 3 && model.ShipTotalRewardTimes == 4 && model.UpTimes == 5 && model.TotalUpTimes == 6
                    && model.HasInfo && model.HasCruiseLogs && model.HasServerInfo;
                Invoke(on18902, controller, ShipPacket(0, 0, 0, 0, 0, 0), out shipRemaining);
                pass &= shipRemaining == 0 && model.HasShipInfo && model.ShippingId == 0 && model.LuckeyValue == 0
                    && model.ShipRewardTimes == 0 && model.ShipTotalRewardTimes == 0 && model.UpTimes == 0 && model.TotalUpTimes == 0
                    && model.HasInfo && model.HasCruiseLogs && model.HasServerInfo;

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
                model.Replace("four", 1, 2, 3, 4, 5, 6, 7, new List<BrightSeaModel.ShippingEntry> { new BrightSeaModel.ShippingEntry { AutoId = 8 } });
                model.ReplaceCruiseLogs(new List<BrightSeaModel.CruiseLogEntry> { new BrightSeaModel.CruiseLogEntry { AutoId = 9 } });
                model.ReplaceShipInfo(10, 11, 12, 13, 14, 15);
                model.ReplaceServerInfo(16, 17, new List<BrightSeaModel.ServerEntry> { new BrightSeaModel.ServerEntry { ServerId = 18 } }, 19, 20, 21, new List<BrightSeaModel.ServerEntry> { new BrightSeaModel.ServerEntry { ServerId = 22 } });
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

                controller.Dispose();
                frames.Clear();
                EventDispatcher.Emit(GlobalEvent.EVT_GAME_START);
                pass &= !controller.IsInitialized && !handlers.Contains(18900) && !handlers.Contains(18901) && !handlers.Contains(18902) && !handlers.Contains(18915)
                    && !model.HasInfo && !model.HasCruiseLogs && !model.HasShipInfo && !model.HasServerInfo && model.SendList.Count == 0
                    && model.CruiseLogs.Count == 0 && model.EnemyServers.Count == 0 && model.UnsatisfiedServers.Count == 0 && frames.Count == 0;
                Debug.Log("CLIVERIFY brightsea VERDICT pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Clear();
                old.Restore(model);
                if (wasInitialized) controller.Init();
                if (intercept != null) intercept.SetValue(null, oldIntercept);
            }
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

        private static byte[] ServerMultiPacket(string chinese) => new CliVerify.Pkt().C(255).H(65535).H(2)
            .I(4294967295L).H(65535).S(chinese).H(0).I(1).H(2).S("").H(65535)
            .C(254).H(65535).H(0).H(2).I(9).H(10).S("u").H(11).I(9).H(12).S("v").H(13).Bytes();

        private static byte[] ServerSinglePacket() => new CliVerify.Pkt().C(1).H(2).H(1).I(3).H(4).S("solo").H(5)
            .C(6).H(7).H(8).H(1).I(9).H(10).S("tail").H(11).Bytes();

        private static byte[] ServerEmptyPacket() => new CliVerify.Pkt().C(0).H(0).H(0).C(0).H(0).H(0).H(0).Bytes();

        private sealed class SavedState
        {
            private readonly bool _info, _cruiseLogs, _shipInfo, _serverInfo;
            private readonly string _picture;
            private readonly uint _pictureVersion;
            private readonly byte _reward, _totalReward, _rob, _totalRob, _status, _module, _unsatisfiedModule;
            private readonly ulong _autoId;
            private readonly ushort _world, _unsatisfiedWorld, _minWorld;
            private readonly byte _shippingId, _shipReward, _shipTotalReward, _up, _totalUp;
            private readonly ushort _luckey;
            private readonly List<BrightSeaModel.ShippingEntry> _ships;
            private readonly List<BrightSeaModel.CruiseLogEntry> _logs;
            private readonly List<BrightSeaModel.ServerEntry> _enemy, _unsatisfied;

            public SavedState(BrightSeaModel m)
            {
                _info = m.HasInfo; _cruiseLogs = m.HasCruiseLogs; _shipInfo = m.HasShipInfo; _serverInfo = m.HasServerInfo; _picture = m.Picture; _pictureVersion = m.PictureVersion;
                _reward = m.RewardTimes; _totalReward = m.TotalRewardTimes; _rob = m.RobTimes; _totalRob = m.TotalRobTimes;
                _status = m.Status; _autoId = m.AutoId; _module = m.TreasureModule; _world = m.WorldLevel;
                _unsatisfiedModule = m.UnsatisfiedModule; _unsatisfiedWorld = m.UnsatisfiedWorldLevel; _minWorld = m.MinWorldLevel;
                _shippingId = m.ShippingId; _luckey = m.LuckeyValue; _shipReward = m.ShipRewardTimes; _shipTotalReward = m.ShipTotalRewardTimes; _up = m.UpTimes; _totalUp = m.TotalUpTimes;
                _ships = new List<BrightSeaModel.ShippingEntry>(m.SendList); _logs = new List<BrightSeaModel.CruiseLogEntry>(m.CruiseLogs);
                _enemy = new List<BrightSeaModel.ServerEntry>(m.EnemyServers);
                _unsatisfied = new List<BrightSeaModel.ServerEntry>(m.UnsatisfiedServers);
            }

            public void Restore(BrightSeaModel m)
            {
                if (_info) m.Replace(_picture, _pictureVersion, _reward, _totalReward, _rob, _totalRob, _autoId, _status, _ships);
                if (_cruiseLogs) m.ReplaceCruiseLogs(_logs);
                if (_shipInfo) m.ReplaceShipInfo(_shippingId, _luckey, _shipReward, _shipTotalReward, _up, _totalUp);
                if (_serverInfo) m.ReplaceServerInfo(_module, _world, _enemy, _unsatisfiedModule, _unsatisfiedWorld, _minWorld, _unsatisfied);
            }
        }
    }
}
