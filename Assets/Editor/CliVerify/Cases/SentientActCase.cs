using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.SentientAct;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class SentientActCase
    {
        private const BindingFlags InstanceNonPublic = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags StaticNonPublic = BindingFlags.Static | BindingFlags.NonPublic;

        public static Task<int> Run()
        {
            try
            {
                return Task.FromResult(RunSync());
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY sentient-act EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            var controller = SentientActController.Instance;
            var model = SentientActModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            FieldInfo interceptField = typeof(SentientActController).GetField("s_outboundIntercept", StaticNonPublic);
            object oldIntercept = interceptField?.GetValue(null);
            SavedState saved = new SavedState(model);

            try
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                controller.Init();

                MethodInfo onInfo = Handler("On24101");
                MethodInfo onPortals = Handler("On24102");
                MethodInfo onPortalRemoved = Handler("On24106");
                MethodInfo onCounts = Handler("On24107");
                var handlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                bool pass = interceptField != null && onInfo != null && onPortals != null && onPortalRemoved != null && onCounts != null && handlers != null;
                pass &= handlers != null && handlers.Contains(Proto.DUNGEON_SENTIENT_MONSTER_PROGRESS);
                for (int id = 24100; id <= 24109; id++)
                    pass &= (id == 24101 || id == 24102 || id == 24106 || id == 24107) ? handlers.Contains(id) : !handlers.Contains(id);
                Chk(ref pass, "24100..24109 only 01/02/06/07 plus 61066 registered", pass);

                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));

                controller.RequestInfo();
                controller.RequestPortals();
                controller.RequestCounts();
                Chk(ref pass, "explicit requests exact 6-byte 01/02/07", Frames(frames, 24101, 24102, 24107));
                frames.Clear();

                Feed(onInfo, controller, Info(9, 1, 2, 3, 4, new[] { new Server(-9, -8, "old", -7) }, -6), out var infoSeed);
                frames.Clear();
                Feed(onPortals, controller, Portals(new[]
                {
                    new Portal(11, 12, 13)
                }), out var portalSeed);
                Feed(onCounts, controller, Counts(14, 15), out var countSeed);
                Chk(ref pass, "seed all slices read-to-end", infoSeed.Remaining == 0 && portalSeed.Remaining == 0 && countSeed.Remaining == 0 && model.HasInfo && model.HasPortals && model.HasCounts && frames.Count == 0);

                Feed(onPortals, controller, Portals(new[]
                {
                    new Portal(-1, 0, uint.MaxValue), new Portal(-1, uint.MaxValue, 0), new Portal(2, 3, 4)
                }), out var portalsMany);
                Chk(ref pass, "24102 many duplicate/max/order isolated", portalsMany.Remaining == 0 && model.Portals.Count == 3 && model.Portals[0].PortalId == -1 && model.Portals[0].X == 0 && model.Portals[0].Y == uint.MaxValue && model.Portals[1].PortalId == -1 && model.Portals[1].X == uint.MaxValue && model.Portals[1].Y == 0 && model.Portals[2].PortalId == 2 && model.State == 9 && model.AssistNum == 14 && model.EnterNum == 15 && frames.Count == 0);
                Feed(onPortals, controller, Portals(new[] { new Portal(5, 6, 7) }), out var portalsOne);
                Chk(ref pass, "24102 many-to-one replace/all fields isolated", portalsOne.Remaining == 0 && model.HasPortals && model.Portals.Count == 1 && model.Portals[0].PortalId == 5 && model.Portals[0].X == 6 && model.Portals[0].Y == 7 && model.State == 9 && model.AssistNum == 14 && model.EnterNum == 15 && frames.Count == 0);
                Feed(onPortals, controller, Portals(new Portal[0]), out var portalsEmpty);
                Chk(ref pass, "24102 one-to-empty replace/read-end isolated", portalsEmpty.Remaining == 0 && model.HasPortals && model.Portals.Count == 0 && model.HasInfo && model.HasCounts && model.State == 9 && model.AssistNum == 14 && model.EnterNum == 15 && frames.Count == 0);

                Feed(onPortals, controller, Portals(new[] { new Portal(21, 22, 23) }), out _);
                Feed(onInfo, controller, Info(0, 0, 0, 0, 0, new Server[0], 0), out var inactiveZero);
                Chk(ref pass, "24101 state0 clears servers not portals/no followup", inactiveZero.Remaining == 0 && model.HasInfo && model.State == 0 && model.EndTime == 0 && model.Mod == 0 && model.GroupId == 0 && model.NextStartTime == 0 && model.AvgLevel == 0 && model.Servers.Count == 0 && model.Portals.Count == 1 && model.Portals[0].PortalId == 21 && frames.Count == 0);

                Feed(onInfo, controller, Info(255, uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue, new[]
                {
                    new Server(-1, -1, "中文", -1), new Server(-1, -1, "", 0)
                }, -1), out var activeMany);
                Chk(ref pass, "24101 active full fields/duplicate servers/followup", activeMany.Remaining == 0 && model.State == 255 && model.EndTime == uint.MaxValue && model.Mod == uint.MaxValue && model.GroupId == uint.MaxValue && model.NextStartTime == uint.MaxValue && model.AvgLevel == -1 && model.Servers.Count == 2 && model.Servers[0].ServerId == -1 && model.Servers[0].ServerNum == -1 && model.Servers[0].Name == "中文" && model.Servers[0].WorldLevel == -1 && model.Servers[1].ServerId == -1 && model.Servers[1].ServerNum == -1 && model.Servers[1].Name == "" && model.Servers[1].WorldLevel == 0 && model.Portals.Count == 1 && model.AssistNum == 14 && model.EnterNum == 15 && Frames(frames, 24102));
                frames.Clear();
                Feed(onInfo, controller, Info(1, 1, 2, 3, 4, new[] { new Server(5, 6, "one", 7) }, 8), out var activeOne);
                Chk(ref pass, "24101 servers many-to-one/all fields/followup", activeOne.Remaining == 0 && model.State == 1 && model.EndTime == 1 && model.Mod == 2 && model.GroupId == 3 && model.NextStartTime == 4 && model.AvgLevel == 8 && model.Servers.Count == 1 && model.Servers[0].ServerId == 5 && model.Servers[0].ServerNum == 6 && model.Servers[0].Name == "one" && model.Servers[0].WorldLevel == 7 && model.Portals.Count == 1 && model.AssistNum == 14 && model.EnterNum == 15 && Frames(frames, 24102));
                frames.Clear();
                Feed(onInfo, controller, Info(0, 10, 11, 12, 13, new Server[0], 14), out var inactiveEmpty);
                Chk(ref pass, "24101 servers one-to-empty/portals retained", inactiveEmpty.Remaining == 0 && model.State == 0 && model.EndTime == 10 && model.Mod == 11 && model.GroupId == 12 && model.NextStartTime == 13 && model.AvgLevel == 14 && model.Servers.Count == 0 && model.Portals.Count == 1 && frames.Count == 0);

                Feed(onCounts, controller, Counts(0, 0), out var countsZero);
                Chk(ref pass, "24107 zero absolute snapshot isolated", countsZero.Remaining == 0 && model.HasCounts && model.AssistNum == 0 && model.EnterNum == 0 && model.State == 0 && model.Portals.Count == 1 && frames.Count == 0);
                Feed(onCounts, controller, Counts(uint.MaxValue, 0), out var countsMax);
                Chk(ref pass, "24107 assist-enter max wire order isolated", countsMax.Remaining == 0 && model.AssistNum == uint.MaxValue && model.EnterNum == 0 && model.State == 0 && model.Portals.Count == 1 && frames.Count == 0);
                Feed(onCounts, controller, Counts(1, 2), out var countsOne);
                Chk(ref pass, "24107 small absolute overwrite/isolation", countsOne.Remaining == 0 && model.AssistNum == 1 && model.EnterNum == 2 && model.HasCounts && model.State == 0 && model.Portals.Count == 1 && frames.Count == 0);

                Feed(onInfo, controller, Info(1, 31, 32, 33, 34, new[] { new Server(35, 36, "keep", 37) }, 38), out _);
                frames.Clear();
                Feed(onPortals, controller, Portals(new[] { new Portal(39, 40, 41) }), out _);
                Feed(onCounts, controller, Counts(42, 43), out _);
                controller.RequestInfo();
                controller.RequestPortals();
                controller.RequestCounts();
                Chk(ref pass, "no response exact requests preserve all slices", Frames(frames, 24101, 24102, 24107) && model.State == 1 && model.EndTime == 31 && model.Servers.Count == 1 && model.Servers[0].Name == "keep" && model.Portals.Count == 1 && model.Portals[0].PortalId == 39 && model.AssistNum == 42 && model.EnterNum == 43);
                frames.Clear();

                EventDispatcher.Emit(GlobalEvent.EVT_GAME_START);
                Chk(ref pass, "GAME_START reset then exact 01-07-02", Frames(frames, 24101, 24107, 24102) && Empty(model));
                frames.Clear();
                controller.Dispose();
                EventDispatcher.Emit(GlobalEvent.EVT_GAME_START);
                Chk(ref pass, "dispose removes handlers/resets/no GAME_START frames", !controller.IsInitialized && !handlers.Contains(24101) && !handlers.Contains(24102) && !handlers.Contains(24106) && !handlers.Contains(24107) && !handlers.Contains(Proto.DUNGEON_SENTIENT_MONSTER_PROGRESS) && Empty(model) && frames.Count == 0);

                Debug.Log("CLIVERIFY sentient-act VERDICT pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                saved.Restore(model);
                if (wasInitialized) controller.Init();
                if (interceptField != null) interceptField.SetValue(null, oldIntercept);
            }
        }

        private static MethodInfo Handler(string name)
        {
            return typeof(SentientActController).GetMethod(name, InstanceNonPublic);
        }

        private static void Feed(MethodInfo method, SentientActController controller, byte[] data, out NetReader reader)
        {
            reader = new NetReader(data, 0, data.Length);
            method.Invoke(controller, new object[] { reader });
        }

        private static void Chk(ref bool pass, string label, bool ok)
        {
            Debug.Log("CLIVERIFY sentient-act " + label + " ok=" + ok);
            pass &= ok;
        }

        private static bool Empty(SentientActModel model)
        {
            return !model.HasInfo && !model.HasPortals && !model.HasPortalRemoved && model.LastPortalRemoved == null && !model.HasCounts && !model.HasMonsterProgress && model.LastMonsterProgress == null && model.State == 0 && model.EndTime == 0 && model.Mod == 0 && model.GroupId == 0 && model.NextStartTime == 0 && model.AvgLevel == 0 && model.Servers.Count == 0 && model.Portals.Count == 0 && model.AssistNum == 0 && model.EnterNum == 0;
        }

        private static bool Frames(List<byte[]> frames, params int[] ids)
        {
            if (frames.Count != ids.Length) return false;
            for (int i = 0; i < ids.Length; i++)
            {
                byte[] frame = frames[i];
                if (frame.Length != 6 || frame[0] != 0 || frame[1] != 6 || frame[2] != 3 || frame[3] != 232 || frame[4] != (byte)(ids[i] >> 8) || frame[5] != (byte)ids[i]) return false;
            }
            return true;
        }

        private static byte[] Info(byte state, uint end, uint mod, uint group, uint next, Server[] servers, long avg)
        {
            var packet = new CliVerify.Pkt().C(state).I(end).I(mod).I(group).I(next).H(servers.Length);
            foreach (Server server in servers) packet.L(server.Id).L(server.Num).S(server.Name).L(server.World);
            return packet.L(avg).Bytes();
        }

        private static byte[] Portals(Portal[] portals)
        {
            var packet = new CliVerify.Pkt().H(portals.Length);
            foreach (Portal portal in portals) packet.L(portal.Id).I(portal.X).I(portal.Y);
            return packet.Bytes();
        }

        private static byte[] Counts(uint assist, uint enter)
        {
            return new CliVerify.Pkt().I(assist).I(enter).Bytes();
        }

        private readonly struct Server
        {
            public readonly long Id;
            public readonly long Num;
            public readonly string Name;
            public readonly long World;

            public Server(long id, long num, string name, long world)
            {
                Id = id;
                Num = num;
                Name = name;
                World = world;
            }
        }

        private readonly struct Portal
        {
            public readonly long Id;
            public readonly uint X;
            public readonly uint Y;

            public Portal(long id, uint x, uint y)
            {
                Id = id;
                X = x;
                Y = y;
            }
        }

        private sealed class SavedState
        {
            private readonly bool _hasInfo;
            private readonly byte _state;
            private readonly uint _endTime;
            private readonly uint _mod;
            private readonly uint _groupId;
            private readonly uint _nextStartTime;
            private readonly long _avgLevel;
            private readonly List<SentientActModel.ServerEntry> _servers;
            private readonly bool _hasPortals;
            private readonly List<SentientActModel.PortalEntry> _portals;
            private readonly bool _hasPortalRemoved;
            private readonly SentientActModel.PortalRemovedSnapshot _portalRemoved;
            private readonly bool _hasCounts;
            private readonly uint _assist;
            private readonly uint _enter;
            private readonly bool _hasMonsterProgress;
            private readonly SentientActModel.MonsterProgressSnapshot _monsterProgress;

            public SavedState(SentientActModel model)
            {
                _hasInfo = model.HasInfo;
                _state = model.State;
                _endTime = model.EndTime;
                _mod = model.Mod;
                _groupId = model.GroupId;
                _nextStartTime = model.NextStartTime;
                _avgLevel = model.AvgLevel;
                _servers = new List<SentientActModel.ServerEntry>(model.Servers);
                _hasPortals = model.HasPortals;
                _portals = new List<SentientActModel.PortalEntry>(model.Portals);
                _hasPortalRemoved = model.HasPortalRemoved;
                _portalRemoved = model.LastPortalRemoved;
                _hasCounts = model.HasCounts;
                _assist = model.AssistNum;
                _enter = model.EnterNum;
                _hasMonsterProgress = model.HasMonsterProgress;
                _monsterProgress = model.LastMonsterProgress;
            }

            public void Restore(SentientActModel model)
            {
                model.Reset();
                if (_hasInfo) model.ReplaceInfo(_state, _endTime, _mod, _groupId, _nextStartTime, _servers, _avgLevel);
                if (_hasPortals) model.ReplacePortals(_portals);
                if (_hasPortalRemoved && _portalRemoved != null) model.ReplacePortalRemoved(_portalRemoved.PortalId);
                if (_hasCounts) model.ReplaceCounts(_assist, _enter);
                typeof(SentientActModel).GetProperty("HasMonsterProgress", BindingFlags.Public | BindingFlags.Instance)
                    ?.SetValue(model, _hasMonsterProgress);
                typeof(SentientActModel).GetProperty("LastMonsterProgress", BindingFlags.Public | BindingFlags.Instance)
                    ?.SetValue(model, _monsterProgress);
            }
        }
    }
}
