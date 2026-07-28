using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Dungeon;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>61051 阶段奖励专项：精确请求、同键替换、异键隔离、极值/顺序/空表及 ambient 深恢复。</summary>
    public static class DungeonDragonStageRewardCase
    {
        private const BindingFlags IF = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags SF = BindingFlags.Static | BindingFlags.NonPublic;

        private sealed class AmbientEntry
        {
            public DungeonModel.DragonStageRewardSnapshot Snapshot;
            public byte HistoryWave;
            public List<byte> ClaimedList;
            public byte[] ClaimedValues;
        }

        public static Task<int> Run()
        {
            try
            {
                return Task.FromResult(RunSync());
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY dungeon-dragon-stage-reward EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            DungeonController controller = DungeonController.Instance;
            DungeonModel model = DungeonModel.Instance;
            bool oldInitialized = controller.IsInitialized;
            Dictionary<uint, AmbientEntry> oldRewards = Capture(model.DragonStageRewardsByDunId);
            FieldInfo interceptField = typeof(DungeonController).GetField("s_dragonStageRewardOutboundIntercept", SF);
            object oldIntercept = interceptField?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            bool oldHandlerExists = handlers != null && handlers.Contains(Proto.DUNGEON_DRAGON_STAGE_REWARD);
            object oldHandler = oldHandlerExists ? handlers[Proto.DUNGEON_DRAGON_STAGE_REWARD] : null;
            bool pass = false;
            bool restored = false;

            try
            {
                model.DragonStageRewardsByDunId.Clear();
                MethodInfo on61051 = typeof(DungeonController).GetMethod("On61051", IF);
                pass = Proto.DUNGEON_DRAGON_STAGE_REWARD == 61051 && on61051 != null
                    && interceptField != null && (!oldInitialized || oldHandlerExists);
                Check(ref pass, "seams/registration", pass);

                model.ApplyDragonStageReward(77, 8, new List<byte> { 9, 10 });
                DungeonModel.DragonStageRewardSnapshot seed = null;
                model.TryGetDragonStageReward(77, out seed);

                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));
                controller.RequestDragonStageRewardInfo(uint.MaxValue);
                controller.RequestDragonStageRewardInfo(0);
                Check(ref pass, "exact 10B max/zero requests", frames.Count == 2
                    && Frame(frames[0], uint.MaxValue) && Frame(frames[1], 0));
                Check(ref pass, "request no response keeps snapshot",
                    model.TryGetDragonStageReward(77, out DungeonModel.DragonStageRewardSnapshot seededAfter)
                    && ReferenceEquals(seededAfter, seed) && Values(seededAfter, 8, 9, 10));

                DungeonModel.DragonStageRewardSnapshot multi = null;
                Check(ref pass, "u32/u8 max duplicate order/read-to-end",
                    Feed(on61051, controller, uint.MaxValue, byte.MaxValue, byte.MaxValue, 0, byte.MaxValue)
                    && model.TryGetDragonStageReward(uint.MaxValue, out multi)
                    && Values(multi, byte.MaxValue, byte.MaxValue, 0, byte.MaxValue));

                DungeonModel.DragonStageRewardSnapshot other = null;
                Check(ref pass, "different key isolated/read-to-end",
                    Feed(on61051, controller, 0, 3, 4, 4)
                    && model.TryGetDragonStageReward(0, out other)
                    && Values(other, 3, 4, 4)
                    && model.TryGetDragonStageReward(uint.MaxValue, out DungeonModel.DragonStageRewardSnapshot multiAfter)
                    && ReferenceEquals(multiAfter, multi));

                DungeonModel.DragonStageRewardSnapshot single = null;
                DungeonModel.DragonStageRewardSnapshot otherAfter = null;
                Check(ref pass, "multi-to-single same key replace/read-to-end",
                    Feed(on61051, controller, uint.MaxValue, 7, 8)
                    && model.TryGetDragonStageReward(uint.MaxValue, out single)
                    && !ReferenceEquals(single, multi) && Values(single, 7, 8)
                    && model.TryGetDragonStageReward(0, out otherAfter)
                    && ReferenceEquals(otherAfter, other));

                Check(ref pass, "single-to-empty same key replace/read-to-end",
                    Feed(on61051, controller, uint.MaxValue, 0)
                    && model.TryGetDragonStageReward(uint.MaxValue, out DungeonModel.DragonStageRewardSnapshot empty)
                    && !ReferenceEquals(empty, single) && Values(empty, 0)
                    && model.TryGetDragonStageReward(0, out otherAfter) && ReferenceEquals(otherAfter, other));

                Check(ref pass, "ambient untouched during run",
                    controller.IsInitialized == oldInitialized
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler));
                Debug.Log("CLIVERIFY dungeon-dragon-stage-reward VERDICT pass=" + pass);
            }
            finally
            {
                Restore(model.DragonStageRewardsByDunId, oldRewards);
                if (interceptField != null) interceptField.SetValue(null, oldIntercept);

                restored = controller.IsInitialized == oldInitialized
                    && AmbientEquals(model.DragonStageRewardsByDunId, oldRewards)
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler)
                    && (interceptField == null || ReferenceEquals(interceptField.GetValue(null), oldIntercept));
                Debug.Log("CLIVERIFY dungeon-dragon-stage-reward restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static bool Feed(MethodInfo handler, DungeonController controller, uint dunId, byte historyWave,
            params byte[] claimedWaves)
        {
            var p = new CliVerify.Pkt().I(dunId).C(historyWave).H(claimedWaves.Length);
            foreach (byte wave in claimedWaves) p.C(wave);
            byte[] bytes = p.Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool Frame(byte[] frame, uint dunId)
        {
            return frame != null && frame.Length == 10
                && frame[0] == 0 && frame[1] == 10 && frame[2] == 3 && frame[3] == 232
                && frame[4] == 238 && frame[5] == 123
                && frame[6] == (byte)(dunId >> 24) && frame[7] == (byte)(dunId >> 16)
                && frame[8] == (byte)(dunId >> 8) && frame[9] == (byte)dunId;
        }

        private static bool Values(DungeonModel.DragonStageRewardSnapshot snapshot, byte historyWave,
            params byte[] claimedWaves)
        {
            if (snapshot == null || snapshot.HistoryWave != historyWave || snapshot.ClaimedWaves == null
                || snapshot.ClaimedWaves.Count != claimedWaves.Length) return false;
            for (int i = 0; i < claimedWaves.Length; i++)
                if (snapshot.ClaimedWaves[i] != claimedWaves[i]) return false;
            return true;
        }

        private static Dictionary<uint, AmbientEntry> Capture(
            Dictionary<uint, DungeonModel.DragonStageRewardSnapshot> source)
        {
            var captured = new Dictionary<uint, AmbientEntry>(source.Count);
            foreach (KeyValuePair<uint, DungeonModel.DragonStageRewardSnapshot> pair in source)
            {
                DungeonModel.DragonStageRewardSnapshot snapshot = pair.Value;
                List<byte> list = snapshot?.ClaimedWaves;
                captured[pair.Key] = new AmbientEntry
                {
                    Snapshot = snapshot,
                    HistoryWave = snapshot != null ? snapshot.HistoryWave : (byte)0,
                    ClaimedList = list,
                    ClaimedValues = list?.ToArray(),
                };
            }
            return captured;
        }

        private static void Restore(Dictionary<uint, DungeonModel.DragonStageRewardSnapshot> target,
            Dictionary<uint, AmbientEntry> captured)
        {
            target.Clear();
            foreach (KeyValuePair<uint, AmbientEntry> pair in captured)
            {
                AmbientEntry old = pair.Value;
                if (old.Snapshot != null)
                {
                    old.Snapshot.HistoryWave = old.HistoryWave;
                    old.Snapshot.ClaimedWaves = old.ClaimedList;
                    if (old.ClaimedList != null)
                    {
                        old.ClaimedList.Clear();
                        if (old.ClaimedValues != null) old.ClaimedList.AddRange(old.ClaimedValues);
                    }
                }
                target[pair.Key] = old.Snapshot;
            }
        }

        private static bool AmbientEquals(Dictionary<uint, DungeonModel.DragonStageRewardSnapshot> actual,
            Dictionary<uint, AmbientEntry> captured)
        {
            if (actual.Count != captured.Count) return false;
            foreach (KeyValuePair<uint, AmbientEntry> pair in captured)
            {
                if (!actual.TryGetValue(pair.Key, out DungeonModel.DragonStageRewardSnapshot snapshot)
                    || !ReferenceEquals(snapshot, pair.Value.Snapshot)) return false;
                if (snapshot == null) continue;
                if (snapshot.HistoryWave != pair.Value.HistoryWave
                    || !ReferenceEquals(snapshot.ClaimedWaves, pair.Value.ClaimedList)) return false;
                byte[] values = pair.Value.ClaimedValues;
                if ((snapshot.ClaimedWaves?.Count ?? -1) != (values?.Length ?? -1)) return false;
                if (values != null)
                    for (int i = 0; i < values.Length; i++)
                        if (snapshot.ClaimedWaves[i] != values[i]) return false;
            }
            return true;
        }

        private static bool HandlerUnchanged(IDictionary handlers, bool existed, object value)
        {
            return handlers != null && handlers.Contains(Proto.DUNGEON_DRAGON_STAGE_REWARD) == existed
                && (!existed || ReferenceEquals(handlers[Proto.DUNGEON_DRAGON_STAGE_REWARD], value));
        }

        private static void Check(ref bool pass, string name, bool ok)
        {
            Debug.Log("CLIVERIFY dungeon-dragon-stage-reward " + name + " ok=" + ok);
            pass &= ok;
        }
    }
}
