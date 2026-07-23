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
                MethodInfo on18915 = typeof(BrightSeaController).GetMethod("On18915", InstanceNonPublic);
                IDictionary handlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                bool pass = intercept != null && on18900 != null && on18915 != null && handlers != null
                    && handlers.Contains(18900) && handlers.Contains(18915);
                for (int proto = 18901; proto <= 18920; proto++)
                    pass &= handlers.Contains(proto) == (proto == 18915);

                var frames = new List<byte[]>();
                intercept.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                controller.RequestInfo();
                pass &= Frames(frames, 18900);
                frames.Clear();
                controller.RequestServerInfo();
                pass &= Frames(frames, 18915);

                model.Replace("stale", 1, 1, 1, 1, 1, 1, 1, new List<BrightSeaModel.ShippingEntry> { new BrightSeaModel.ShippingEntry() });
                model.ReplaceServerInfo(1, 2, new List<BrightSeaModel.ServerEntry> { new BrightSeaModel.ServerEntry() }, 3, 4, 5, new List<BrightSeaModel.ServerEntry>());
                frames.Clear();
                EventDispatcher.Emit(GlobalEvent.EVT_GAME_START);
                pass &= Frames(frames, 18900) && !model.HasInfo && !model.HasServerInfo
                    && model.SendList.Count == 0 && model.EnemyServers.Count == 0 && model.UnsatisfiedServers.Count == 0;

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

                Invoke(on18900, controller, MainSinglePacket(), out mainRemaining);
                pass &= mainRemaining == 0 && model.HasInfo && model.Picture == "next" && model.PictureVersion == 1
                    && model.RewardTimes == 2 && model.TotalRewardTimes == 3 && model.RobTimes == 4
                    && model.TotalRobTimes == 5 && model.AutoId == 2 && model.Status == 6
                    && model.SendList.Count == 1 && model.SendList[0].AutoId == 3 && model.SendList[0].RoleName == "solo"
                    && model.HasServerInfo && model.EnemyServers.Count == 2 && model.UnsatisfiedServers.Count == 2;

                Invoke(on18900, controller, MainEmptyPacket(), out mainRemaining);
                pass &= mainRemaining == 0 && model.HasInfo && model.SendList.Count == 0 && model.Picture == ""
                    && model.HasServerInfo && model.EnemyServers.Count == 2 && model.UnsatisfiedServers.Count == 2;

                Invoke(on18915, controller, ServerSinglePacket(), out serverRemaining);
                pass &= serverRemaining == 0 && model.HasInfo && model.HasServerInfo && model.EnemyServers.Count == 1
                    && model.EnemyServers[0].ServerId == 3 && model.EnemyServers[0].ServerName == "solo"
                    && model.UnsatisfiedServers.Count == 1 && model.UnsatisfiedServers[0].ServerName == "tail";

                Invoke(on18915, controller, ServerEmptyPacket(), out serverRemaining);
                pass &= serverRemaining == 0 && model.HasServerInfo && model.EnemyServers.Count == 0
                    && model.UnsatisfiedServers.Count == 0 && model.TreasureModule == 0 && model.WorldLevel == 0;

                model.ReplaceServerInfo(7, 8, new List<BrightSeaModel.ServerEntry> { new BrightSeaModel.ServerEntry { ServerId = 9 } }, 10, 11, 12, new List<BrightSeaModel.ServerEntry>());
                pass &= model.HasServerInfo && model.EnemyServers.Count == 1 && model.EnemyServers[0].ServerId == 9;

                model.Clear();
                model.Replace("main-only", 1, 2, 3, 4, 5, 6, 7, new List<BrightSeaModel.ShippingEntry> { new BrightSeaModel.ShippingEntry { AutoId = 8 } });
                var mainOnly = new SavedState(model);
                model.Clear();
                mainOnly.Restore(model);
                pass &= model.HasInfo && !model.HasServerInfo && model.Picture == "main-only"
                    && model.SendList.Count == 1 && model.SendList[0].AutoId == 8;

                model.Clear();
                model.ReplaceServerInfo(9, 10, new List<BrightSeaModel.ServerEntry> { new BrightSeaModel.ServerEntry { ServerId = 11 } }, 12, 13, 14, new List<BrightSeaModel.ServerEntry>());
                var serverOnly = new SavedState(model);
                model.Clear();
                serverOnly.Restore(model);
                pass &= !model.HasInfo && model.HasServerInfo && model.TreasureModule == 9
                    && model.EnemyServers.Count == 1 && model.EnemyServers[0].ServerId == 11;

                controller.Dispose();
                frames.Clear();
                EventDispatcher.Emit(GlobalEvent.EVT_GAME_START);
                pass &= !controller.IsInitialized && !handlers.Contains(18900) && !handlers.Contains(18915)
                    && !model.HasInfo && !model.HasServerInfo && model.SendList.Count == 0
                    && model.EnemyServers.Count == 0 && model.UnsatisfiedServers.Count == 0 && frames.Count == 0;
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

        private static byte[] ServerMultiPacket(string chinese) => new CliVerify.Pkt().C(255).H(65535).H(2)
            .I(4294967295L).H(65535).S(chinese).H(0).I(1).H(2).S("").H(65535)
            .C(254).H(65535).H(0).H(2).I(9).H(10).S("u").H(11).I(9).H(12).S("v").H(13).Bytes();

        private static byte[] ServerSinglePacket() => new CliVerify.Pkt().C(1).H(2).H(1).I(3).H(4).S("solo").H(5)
            .C(6).H(7).H(8).H(1).I(9).H(10).S("tail").H(11).Bytes();

        private static byte[] ServerEmptyPacket() => new CliVerify.Pkt().C(0).H(0).H(0).C(0).H(0).H(0).H(0).Bytes();

        private sealed class SavedState
        {
            private readonly bool _info, _serverInfo;
            private readonly string _picture;
            private readonly uint _pictureVersion;
            private readonly byte _reward, _totalReward, _rob, _totalRob, _status, _module, _unsatisfiedModule;
            private readonly ulong _autoId;
            private readonly ushort _world, _unsatisfiedWorld, _minWorld;
            private readonly List<BrightSeaModel.ShippingEntry> _ships;
            private readonly List<BrightSeaModel.ServerEntry> _enemy, _unsatisfied;

            public SavedState(BrightSeaModel m)
            {
                _info = m.HasInfo; _serverInfo = m.HasServerInfo; _picture = m.Picture; _pictureVersion = m.PictureVersion;
                _reward = m.RewardTimes; _totalReward = m.TotalRewardTimes; _rob = m.RobTimes; _totalRob = m.TotalRobTimes;
                _status = m.Status; _autoId = m.AutoId; _module = m.TreasureModule; _world = m.WorldLevel;
                _unsatisfiedModule = m.UnsatisfiedModule; _unsatisfiedWorld = m.UnsatisfiedWorldLevel; _minWorld = m.MinWorldLevel;
                _ships = new List<BrightSeaModel.ShippingEntry>(m.SendList); _enemy = new List<BrightSeaModel.ServerEntry>(m.EnemyServers);
                _unsatisfied = new List<BrightSeaModel.ServerEntry>(m.UnsatisfiedServers);
            }

            public void Restore(BrightSeaModel m)
            {
                if (_info) m.Replace(_picture, _pictureVersion, _reward, _totalReward, _rob, _totalRob, _autoId, _status, _ships);
                if (_serverInfo) m.ReplaceServerInfo(_module, _world, _enemy, _unsatisfiedModule, _unsatisfiedWorld, _minWorld, _unsatisfied);
            }
        }
    }
}
