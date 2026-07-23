using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.DragonWhisper;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class DragonWhisperCase
    {
        private const BindingFlags InstancePrivate = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags StaticPrivate = BindingFlags.NonPublic | BindingFlags.Static;

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e) { Debug.LogError("CLIVERIFY dragon-whisper EXCEPTION " + e); return Task.FromResult(3); }
        }

        private static int RunSync()
        {
            DragonWhisperController controller = DragonWhisperController.Instance;
            DragonWhisperModel model = DragonWhisperModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            bool oldHasSnapshot = model.HasSnapshot;
            byte oldLeft = model.LeftCount;
            byte oldAll = model.AllCount;
            var oldMaps = new List<DragonWhisperModel.MapEntry>(model.Maps);
            FieldInfo intercept = typeof(DragonWhisperController).GetField("s_outboundIntercept", StaticPrivate);
            object oldIntercept = intercept?.GetValue(null);

            try
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                controller.Init();

                MethodInfo onInfo = typeof(DragonWhisperController).GetMethod("On65101", InstancePrivate);
                var handlers = typeof(NetManager).GetField("_handlers", StaticPrivate)?.GetValue(null) as IDictionary;
                bool pass = intercept != null && onInfo != null && handlers != null && handlers.Contains(Proto.DRAGON_WHISPER_INFO);
                for (int proto = 65100; proto <= 65107; proto++) if (proto != Proto.DRAGON_WHISPER_INFO) pass &= !handlers.Contains(proto);
                Check(ref pass, "seams/register-only-65101", pass);
                if (!pass) return 3;

                var frames = new List<byte[]>();
                intercept.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                controller.RequestInfo();
                Check(ref pass, "request exact six-byte empty frame", ExactFrames(frames, Proto.DRAGON_WHISPER_INFO) && !model.HasSnapshot);
                frames.Clear();

                var emptyBytes = Packet(0, 0, new MapSpec[0]);
                var emptyReader = new NetReader(emptyBytes, 0, emptyBytes.Length);
                onInfo.Invoke(controller, new object[] { emptyReader });
                Check(ref pass, "empty snapshot/read-to-end/no-outbound", emptyReader.Remaining == 0 && model.HasSnapshot && model.LeftCount == 0 && model.AllCount == 0 && model.Maps.Count == 0 && frames.Count == 0);

                var manyMaps = new[]
                {
                    new MapSpec(byte.MaxValue, ushort.MaxValue, new[]
                    {
                        new MonsterSpec(uint.MaxValue, uint.MaxValue),
                        new MonsterSpec(uint.MaxValue, 0u)
                    }),
                    new MapSpec(byte.MaxValue, 1, new[] { new MonsterSpec(2, 3) }),
                    new MapSpec(0, 0, new[] { new MonsterSpec(0, 0) })
                };
                var manyBytes = Packet(byte.MaxValue, 0, manyMaps);
                var manyReader = new NetReader(manyBytes, 0, manyBytes.Length);
                onInfo.Invoke(controller, new object[] { manyReader });
                DragonWhisperModel.MapEntry first = model.Maps.Count > 0 ? model.Maps[0] : null;
                DragonWhisperModel.MapEntry second = model.Maps.Count > 1 ? model.Maps[1] : null;
                Check(ref pass, "multiple maps/monsters/boundaries/duplicate-wire-order", manyReader.Remaining == 0 && model.LeftCount == byte.MaxValue && model.AllCount == 0 && model.Maps.Count == 3 && first != null && first.MapId == byte.MaxValue && first.RoleNum == ushort.MaxValue && first.Monsters.Count == 2 && first.Monsters[0].MonsterId == uint.MaxValue && first.Monsters[0].RebornTime == uint.MaxValue && first.Monsters[1].MonsterId == uint.MaxValue && first.Monsters[1].RebornTime == 0 && second != null && second.MapId == byte.MaxValue && second.RoleNum == 1 && second.Monsters.Count == 1 && second.Monsters[0].MonsterId == 2 && second.Monsters[0].RebornTime == 3 && model.Maps[2].MapId == 0 && model.Maps[2].RoleNum == 0 && model.Maps[2].Monsters.Count == 1 && model.Maps[2].Monsters[0].MonsterId == 0 && model.Maps[2].Monsters[0].RebornTime == 0 && frames.Count == 0);

                controller.RequestInfo();
                Check(ref pass, "no-response preserves snapshot", ExactFrames(frames, Proto.DRAGON_WHISPER_INFO) && model.Maps.Count == 3 && model.Maps[0].Monsters.Count == 2 && model.Maps[1].MapId == byte.MaxValue && model.Maps[2].MapId == 0);
                frames.Clear();

                var lessBytes = Packet(1, 2, new[] { new MapSpec(3, 4, new[] { new MonsterSpec(5, 6) }) });
                var lessReader = new NetReader(lessBytes, 0, lessBytes.Length);
                onInfo.Invoke(controller, new object[] { lessReader });
                Check(ref pass, "full-to-less whole replacement", lessReader.Remaining == 0 && model.LeftCount == 1 && model.AllCount == 2 && model.Maps.Count == 1 && model.Maps[0].MapId == 3 && model.Maps[0].RoleNum == 4 && model.Maps[0].Monsters.Count == 1 && model.Maps[0].Monsters[0].MonsterId == 5 && model.Maps[0].Monsters[0].RebornTime == 6 && frames.Count == 0);

                var clearBytes = Packet(0, 0, new MapSpec[0]);
                var clearReader = new NetReader(clearBytes, 0, clearBytes.Length);
                onInfo.Invoke(controller, new object[] { clearReader });
                Check(ref pass, "full-to-less-to-empty clears", clearReader.Remaining == 0 && model.HasSnapshot && model.Maps.Count == 0 && frames.Count == 0);

                controller.Dispose();
                Check(ref pass, "dispose unregisters-and-resets", !controller.IsInitialized && !model.HasSnapshot && model.LeftCount == 0 && model.AllCount == 0 && model.Maps.Count == 0 && !handlers.Contains(Proto.DRAGON_WHISPER_INFO));
                Debug.Log("CLIVERIFY dragon-whisper VERDICT pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                if (oldHasSnapshot) model.Replace(oldLeft, oldAll, oldMaps);
                if (wasInitialized) controller.Init();
                if (intercept != null) intercept.SetValue(null, oldIntercept);
            }
        }

        private static void Check(ref bool pass, string tag, bool ok)
        {
            Debug.Log("CLIVERIFY dragon-whisper " + tag + " ok=" + ok);
            if (!ok) pass = false;
        }

        private static bool ExactFrames(IReadOnlyList<byte[]> frames, params int[] protoIds)
        {
            if (frames.Count != protoIds.Length) return false;
            for (int i = 0; i < protoIds.Length; i++)
            {
                byte[] frame = frames[i];
                if (frame == null || frame.Length != 6 || frame[0] != 0 || frame[1] != 6 || frame[2] != 3 || frame[3] != 232 || frame[4] != (byte)(protoIds[i] >> 8) || frame[5] != (byte)(protoIds[i] & 0xFF)) return false;
            }
            return true;
        }

        private static byte[] Packet(byte leftCount, byte allCount, MapSpec[] maps)
        {
            var packet = new CliVerify.Pkt().C(leftCount).C(allCount).H(maps.Length);
            for (int i = 0; i < maps.Length; i++)
            {
                MapSpec map = maps[i];
                packet.C(map.MapId).H(map.RoleNum).H(map.Monsters.Length);
                for (int j = 0; j < map.Monsters.Length; j++) packet.I(map.Monsters[j].MonsterId).I(map.Monsters[j].RebornTime);
            }
            return packet.Bytes();
        }

        private struct MonsterSpec
        {
            public readonly uint MonsterId;
            public readonly uint RebornTime;
            public MonsterSpec(uint monsterId, uint rebornTime) { MonsterId = monsterId; RebornTime = rebornTime; }
        }

        private struct MapSpec
        {
            public readonly byte MapId;
            public readonly ushort RoleNum;
            public readonly MonsterSpec[] Monsters;
            public MapSpec(byte mapId, ushort roleNum, MonsterSpec[] monsters) { MapId = mapId; RoleNum = roleNum; Monsters = monsters; }
        }
    }
}
