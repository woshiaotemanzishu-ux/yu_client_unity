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
            bool oldInfo = model.HasSnapshot;
            byte oldLeft = model.LeftCount;
            byte oldAll = model.AllCount;
            var oldMaps = new List<DragonWhisperModel.MapEntry>(model.Maps);
            bool oldDropLog = model.HasDropLog;
            var oldDrops = new List<DragonWhisperModel.DropLogEntry>(model.DropLogs);
            FieldInfo intercept = typeof(DragonWhisperController).GetField("s_outboundIntercept", StaticPrivate);
            object oldIntercept = intercept?.GetValue(null);
            try
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                controller.Init();
                MethodInfo onInfo = typeof(DragonWhisperController).GetMethod("On65101", InstancePrivate);
                MethodInfo onDropLog = typeof(DragonWhisperController).GetMethod("On65106", InstancePrivate);
                var handlers = typeof(NetManager).GetField("_handlers", StaticPrivate)?.GetValue(null) as IDictionary;
                bool pass = intercept != null && onInfo != null && onDropLog != null && handlers != null && handlers.Contains(Proto.DRAGON_WHISPER_INFO) && handlers.Contains(Proto.DRAGON_WHISPER_DROP_LOG);
                for (int proto = 65100; proto <= 65107; proto++) if (proto != Proto.DRAGON_WHISPER_INFO && proto != Proto.DRAGON_WHISPER_DROP_LOG) pass &= !handlers.Contains(proto);
                Check(ref pass, "seams/register-only-65101-65106", pass);
                if (!pass) return 3;

                var frames = new List<byte[]>();
                intercept.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                controller.RequestInfo();
                controller.RequestDropLog();
                Check(ref pass, "both requests exact six-byte empty frames", ExactFrames(frames, Proto.DRAGON_WHISPER_INFO, Proto.DRAGON_WHISPER_DROP_LOG) && !model.HasSnapshot && !model.HasDropLog);
                frames.Clear();

                Invoke(onInfo, controller, InfoPacket(0, 0, new MapSpec[0]), out NetReader infoEmptyReader);
                Check(ref pass, "65101 empty loaded/read-to-end/no-outbound", infoEmptyReader.Remaining == 0 && model.HasSnapshot && model.LeftCount == 0 && model.AllCount == 0 && model.Maps.Count == 0 && !model.HasDropLog && frames.Count == 0);

                var infoMany = new[]
                {
                    new MapSpec(byte.MaxValue, ushort.MaxValue, new[] { new MonsterSpec(uint.MaxValue, uint.MaxValue), new MonsterSpec(uint.MaxValue, 0) }),
                    new MapSpec(byte.MaxValue, 1, new[] { new MonsterSpec(2, 3) }),
                    new MapSpec(0, 0, new[] { new MonsterSpec(0, 0) })
                };
                Invoke(onInfo, controller, InfoPacket(byte.MaxValue, 0, infoMany), out NetReader infoManyReader);
                Check(ref pass, "65101 regression multi/boundary/order", infoManyReader.Remaining == 0 && model.HasSnapshot && model.LeftCount == byte.MaxValue && model.AllCount == 0 && model.Maps.Count == 3 && model.Maps[0].MapId == byte.MaxValue && model.Maps[0].RoleNum == ushort.MaxValue && model.Maps[0].Monsters.Count == 2 && model.Maps[0].Monsters[0].MonsterId == uint.MaxValue && model.Maps[0].Monsters[0].RebornTime == uint.MaxValue && model.Maps[0].Monsters[1].MonsterId == uint.MaxValue && model.Maps[0].Monsters[1].RebornTime == 0 && model.Maps[1].MapId == byte.MaxValue && model.Maps[1].RoleNum == 1 && model.Maps[1].Monsters.Count == 1 && model.Maps[1].Monsters[0].MonsterId == 2 && model.Maps[1].Monsters[0].RebornTime == 3 && model.Maps[2].MapId == 0 && model.Maps[2].RoleNum == 0 && model.Maps[2].Monsters.Count == 1 && model.Maps[2].Monsters[0].MonsterId == 0 && model.Maps[2].Monsters[0].RebornTime == 0 && frames.Count == 0);
                controller.RequestInfo();
                Check(ref pass, "65101 no-response preserves snapshot", ExactFrames(frames, Proto.DRAGON_WHISPER_INFO) && model.Maps.Count == 3 && model.Maps[0].Monsters.Count == 2 && !model.HasDropLog);
                frames.Clear();

                Invoke(onDropLog, controller, DropPacket(new DropSpec[0]), out NetReader dropEmptyReader);
                Check(ref pass, "65106 empty loaded/read-to-end/no-outbound", dropEmptyReader.Remaining == 0 && model.HasDropLog && model.DropLogs.Count == 0 && model.Maps.Count == 3 && frames.Count == 0);

                var manyDrops = new[]
                {
                    new DropSpec(uint.MaxValue, uint.MaxValue, uint.MaxValue, -1, "中文", uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue,
                        new[] { new ExtraSpec(byte.MaxValue, byte.MaxValue, ushort.MaxValue, uint.MaxValue, byte.MaxValue, uint.MaxValue), new ExtraSpec(0, 0, 0, 0, 0, 0), new ExtraSpec(byte.MaxValue, byte.MaxValue, ushort.MaxValue, uint.MaxValue, byte.MaxValue, uint.MaxValue) }, byte.MaxValue),
                    new DropSpec(0, 0, 0, 0, "", 0, 0, 0, 0, new[] { new ExtraSpec(0, 0, 0, 0, 0, 0) }, 0)
                };
                Invoke(onDropLog, controller, DropPacket(manyDrops), out NetReader dropManyReader);
                DragonWhisperModel.DropLogEntry first = model.DropLogs.Count > 0 ? model.DropLogs[0] : null;
                DragonWhisperModel.DropLogEntry second = model.DropLogs.Count > 1 ? model.DropLogs[1] : null;
                Check(ref pass, "65106 multiple/chinese-empty-name/boundaries/duplicate-extra-order", dropManyReader.Remaining == 0 && model.HasDropLog && model.DropLogs.Count == 2 && first != null && first.Time == uint.MaxValue && first.ServerId == uint.MaxValue && first.ServerNum == uint.MaxValue && first.RoleId == -1 && first.Name == "中文" && first.BossId == uint.MaxValue && first.GoodsId == uint.MaxValue && first.Num == uint.MaxValue && first.Rating == uint.MaxValue && first.EquipExtraAttrs.Count == 3 && Eq(first.EquipExtraAttrs[0], manyDrops[0].Extras[0]) && Eq(first.EquipExtraAttrs[1], manyDrops[0].Extras[1]) && Eq(first.EquipExtraAttrs[2], manyDrops[0].Extras[2]) && first.IsTop == byte.MaxValue && second != null && second.Name == "" && second.RoleId == 0 && second.EquipExtraAttrs.Count == 1 && Eq(second.EquipExtraAttrs[0], manyDrops[1].Extras[0]) && second.IsTop == 0 && model.Maps.Count == 3 && frames.Count == 0);

                controller.RequestDropLog();
                Check(ref pass, "65106 no-response preserves snapshot", ExactFrames(frames, Proto.DRAGON_WHISPER_DROP_LOG) && model.DropLogs.Count == 2 && model.DropLogs[0].Name == "中文" && model.Maps.Count == 3);
                frames.Clear();

                Invoke(onInfo, controller, InfoPacket(1, 2, new[] { new MapSpec(3, 4, new[] { new MonsterSpec(5, 6) }) }), out NetReader infoLessReader);
                Check(ref pass, "65101 full-to-less isolated-from-65106", infoLessReader.Remaining == 0 && model.Maps.Count == 1 && model.Maps[0].MapId == 3 && model.DropLogs.Count == 2 && model.DropLogs[0].Name == "中文" && frames.Count == 0);

                var oneDrop = new[] { new DropSpec(1, 2, 3, 4, "one", 5, 6, 7, 8, new ExtraSpec[0], 9) };
                Invoke(onDropLog, controller, DropPacket(oneDrop), out NetReader dropOneReader);
                Check(ref pass, "65106 full-to-one whole-replace/isolated-from-65101", dropOneReader.Remaining == 0 && model.DropLogs.Count == 1 && model.DropLogs[0].Time == 1 && model.DropLogs[0].Name == "one" && model.DropLogs[0].EquipExtraAttrs.Count == 0 && model.Maps.Count == 1 && model.Maps[0].MapId == 3 && frames.Count == 0);

                Invoke(onDropLog, controller, DropPacket(new DropSpec[0]), out NetReader dropClearReader);
                Invoke(onInfo, controller, InfoPacket(0, 0, new MapSpec[0]), out NetReader infoClearReader);
                Check(ref pass, "both full-to-less-to-empty clear", dropClearReader.Remaining == 0 && infoClearReader.Remaining == 0 && model.HasDropLog && model.DropLogs.Count == 0 && model.HasSnapshot && model.Maps.Count == 0 && frames.Count == 0);

                controller.Dispose();
                Check(ref pass, "dispose unregisters-both-and-clears-both", !controller.IsInitialized && !model.HasSnapshot && !model.HasDropLog && model.Maps.Count == 0 && model.DropLogs.Count == 0 && !handlers.Contains(Proto.DRAGON_WHISPER_INFO) && !handlers.Contains(Proto.DRAGON_WHISPER_DROP_LOG));
                Debug.Log("CLIVERIFY dragon-whisper VERDICT pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                if (oldInfo) model.Replace(oldLeft, oldAll, oldMaps);
                if (oldDropLog) model.ReplaceDropLog(oldDrops);
                if (wasInitialized) controller.Init();
                if (intercept != null) intercept.SetValue(null, oldIntercept);
            }
        }

        private static void Invoke(MethodInfo method, DragonWhisperController controller, byte[] bytes, out NetReader reader)
        {
            reader = new NetReader(bytes, 0, bytes.Length);
            method.Invoke(controller, new object[] { reader });
        }

        private static void Check(ref bool pass, string tag, bool ok) { Debug.Log("CLIVERIFY dragon-whisper " + tag + " ok=" + ok); if (!ok) pass = false; }

        private static bool ExactFrames(IReadOnlyList<byte[]> frames, params int[] protoIds)
        {
            if (frames.Count != protoIds.Length) return false;
            for (int i = 0; i < protoIds.Length; i++) { byte[] frame = frames[i]; if (frame == null || frame.Length != 6 || frame[0] != 0 || frame[1] != 6 || frame[2] != 3 || frame[3] != 232 || frame[4] != (byte)(protoIds[i] >> 8) || frame[5] != (byte)(protoIds[i] & 0xFF)) return false; }
            return true;
        }

        private static byte[] InfoPacket(byte left, byte all, MapSpec[] maps)
        {
            var packet = new CliVerify.Pkt().C(left).C(all).H(maps.Length);
            foreach (MapSpec map in maps) { packet.C(map.MapId).H(map.RoleNum).H(map.Monsters.Length); foreach (MonsterSpec monster in map.Monsters) packet.I(monster.MonsterId).I(monster.RebornTime); }
            return packet.Bytes();
        }

        private static byte[] DropPacket(DropSpec[] drops)
        {
            var packet = new CliVerify.Pkt().H(drops.Length);
            foreach (DropSpec drop in drops)
            {
                packet.I(drop.Time).I(drop.ServerId).I(drop.ServerNum).L(drop.RoleId).S(drop.Name).I(drop.BossId).I(drop.GoodsId).I(drop.Num).I(drop.Rating).H(drop.Extras.Length);
                foreach (ExtraSpec extra in drop.Extras) packet.C(extra.Color).C(extra.TypeId).H(extra.AttrId).I(extra.AttrValue).C(extra.PlusInterval).I(extra.PlusUnit);
                packet.C(drop.IsTop);
            }
            return packet.Bytes();
        }

        private static bool Eq(DragonWhisperModel.EquipExtraAttr actual, ExtraSpec expected) => actual.Color == expected.Color && actual.TypeId == expected.TypeId && actual.AttrId == expected.AttrId && actual.AttrValue == expected.AttrValue && actual.PlusInterval == expected.PlusInterval && actual.PlusUnit == expected.PlusUnit;
        private struct MonsterSpec { public readonly uint MonsterId; public readonly uint RebornTime; public MonsterSpec(uint id, uint time) { MonsterId = id; RebornTime = time; } }
        private struct MapSpec { public readonly byte MapId; public readonly ushort RoleNum; public readonly MonsterSpec[] Monsters; public MapSpec(byte id, ushort roles, MonsterSpec[] monsters) { MapId = id; RoleNum = roles; Monsters = monsters; } }
        private struct ExtraSpec { public readonly byte Color; public readonly byte TypeId; public readonly ushort AttrId; public readonly uint AttrValue; public readonly byte PlusInterval; public readonly uint PlusUnit; public ExtraSpec(byte color, byte typeId, ushort attrId, uint attrValue, byte interval, uint unit) { Color = color; TypeId = typeId; AttrId = attrId; AttrValue = attrValue; PlusInterval = interval; PlusUnit = unit; } }
        private struct DropSpec { public readonly uint Time; public readonly uint ServerId; public readonly uint ServerNum; public readonly long RoleId; public readonly string Name; public readonly uint BossId; public readonly uint GoodsId; public readonly uint Num; public readonly uint Rating; public readonly ExtraSpec[] Extras; public readonly byte IsTop; public DropSpec(uint time, uint serverId, uint serverNum, long roleId, string name, uint bossId, uint goodsId, uint num, uint rating, ExtraSpec[] extras, byte isTop) { Time = time; ServerId = serverId; ServerNum = serverNum; RoleId = roleId; Name = name; BossId = bossId; GoodsId = goodsId; Num = num; Rating = rating; Extras = extras; IsTop = isTop; } }
    }
}
