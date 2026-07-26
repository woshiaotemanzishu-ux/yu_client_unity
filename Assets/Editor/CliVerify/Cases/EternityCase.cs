using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Eternity;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class EternityCase
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
                Debug.LogError("CLIVERIFY eternity EXCEPTION " + exception);
                return Task.FromResult(3);
            }
        }

        private static int RunCore()
        {
            EternityController controller = EternityController.Instance;
            EternityModel model = EternityModel.Instance;
            RoleModel role = RoleModel.Instance;
            bool pass = false;
            bool restored = false;
            bool wasInitialized = controller.IsInitialized;
            bool oldHasData = model.HasData;
            uint oldOpenTime = model.OpenTime;
            uint oldEnterTime = model.EnterTime;
            uint oldEndTime = model.EndTime;
            bool oldHasJoinInfo = model.HasJoinInfo;
            byte oldCanEnterScene = model.CanEnterScene;
            var oldJoinList = new List<EternityModel.JoinEntry>(model.JoinList);
            bool oldHasReliveInfo = model.HasReliveInfo;
            ushort oldDieTimes = model.DieTimes;
            uint oldTime = model.Time;
            uint oldDieTime = model.DieTime;
            uint oldSafeTime = model.SafeTime;
            int oldLevel = role.Level;
            FieldInfo hasBaseInfoField = typeof(RoleModel).GetField("<HasBaseInfo>k__BackingField", InstanceNonPublic);
            bool oldHasBaseInfo = hasBaseInfoField != null && (bool)hasBaseInfoField.GetValue(role);
            FieldInfo interceptField = typeof(EternityController).GetField("s_outboundIntercept", StaticNonPublic);
            FieldInfo lastLevelField = typeof(EternityController).GetField("_lastLevel", InstanceNonPublic);
            object oldIntercept = interceptField == null ? null : interceptField.GetValue(null);
            int oldLastLevel = lastLevelField == null ? -1 : (int)lastLevelField.GetValue(controller);
            IDictionary handlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
            var oldHandlers = new Dictionary<int, object>();
            if (handlers != null)
            {
                foreach (int proto in new[] { 27900, 27901, 27906 })
                {
                    if (handlers.Contains(proto)) oldHandlers[proto] = handlers[proto];
                }
            }
            IDictionary eventHandlers = typeof(EventDispatcher).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
            bool oldHadRoleEvent = eventHandlers != null && eventHandlers.Contains(GlobalEvent.EVT_ROLE_INFO_UPDATE);
            var oldRoleSubscribers = oldHadRoleEvent
                ? new List<Delegate>((List<Delegate>)eventHandlers[GlobalEvent.EVT_ROLE_INFO_UPDATE])
                : new List<Delegate>();

            try
            {
                controller.Init();
                model.Reset();

                MethodInfo on27900 = typeof(EternityController).GetMethod("On27900", InstanceNonPublic);
                MethodInfo on27901 = typeof(EternityController).GetMethod("On27901", InstanceNonPublic);
                MethodInfo on27906 = typeof(EternityController).GetMethod("On27906", InstanceNonPublic);
                MethodInfo onRoleInfoUpdate = typeof(EternityController).GetMethod("OnRoleInfoUpdate", InstanceNonPublic);
                pass = hasBaseInfoField != null && interceptField != null && lastLevelField != null
                    && on27900 != null && on27901 != null && on27906 != null && onRoleInfoUpdate != null && handlers != null
                    && eventHandlers != null && handlers.Contains(27900) && handlers.Contains(27901) && handlers.Contains(27906);
                for (int proto = 27902; proto <= 27909; proto++)
                {
                    pass &= proto == 27906 ? handlers.Contains(proto) : !handlers.Contains(proto);
                }

                if (!pass)
                {
                    Debug.LogError("CLIVERIFY eternity VERDICT pass=false (reflection/protocol registration missing)");
                    return 3;
                }

                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));
                hasBaseInfoField.SetValue(role, true);

                model.Replace(1, 2, 3);
                model.ReplaceJoinInfo(1, new List<EternityModel.JoinEntry> { new EternityModel.JoinEntry(4, 5, 6) });
                model.ReplaceReliveInfo(7, 8, 9, 10);
                role.Level = 479;
                controller.RequestStartup();
                pass &= frames.Count == 0 && !model.HasData && !model.HasJoinInfo && !model.HasReliveInfo && model.OpenTime == 0 && model.EnterTime == 0 && model.EndTime == 0
                    && model.CanEnterScene == 0 && model.JoinList.Count == 0 && model.DieTimes == 0 && model.Time == 0 && model.DieTime == 0 && model.SafeTime == 0;

                model.Replace(4, 5, 6);
                role.Level = 480;
                controller.RequestStartup();
                pass &= frames.Count == 1 && !model.HasData;
                pass &= IsExactRequest(frames[0]);
                frames.Clear();

                role.Level = 479;
                controller.RequestStartup();
                role.Level = 480;
                onRoleInfoUpdate.Invoke(controller, null);
                pass &= frames.Count == 1 && IsExactRequest(frames[0]);
                onRoleInfoUpdate.Invoke(controller, null);
                pass &= frames.Count == 1;
                role.Level = 481;
                onRoleInfoUpdate.Invoke(controller, null);
                pass &= frames.Count == 1;

                frames.Clear();
                role.Level = 479;
                controller.RequestStartup();
                role.Level = 481;
                onRoleInfoUpdate.Invoke(controller, null);
                pass &= frames.Count == 0;

                byte[] firstBytes = new CliVerify.Pkt().I(0).I(4000000000L).I(4294967295L).Bytes();
                var firstReader = new NetReader(firstBytes, 0, firstBytes.Length);
                on27900.Invoke(controller, new object[] { firstReader });
                pass &= firstReader.Remaining == 0 && model.HasData
                    && model.OpenTime == 0 && model.EnterTime == 4000000000U && model.EndTime == uint.MaxValue
                    && frames.Count == 0;

                byte[] secondBytes = new CliVerify.Pkt().I(7).I(8).I(9).Bytes();
                var secondReader = new NetReader(secondBytes, 0, secondBytes.Length);
                on27900.Invoke(controller, new object[] { secondReader });
                pass &= secondReader.Remaining == 0 && model.HasData
                    && model.OpenTime == 7 && model.EnterTime == 8 && model.EndTime == 9;

                controller.RequestJoinInfo();
                pass &= frames.Count == 1 && IsExactJoinRequest(frames[0]) && !model.HasJoinInfo;
                frames.Clear();

                byte[] emptyJoinBytes = JoinPacket(0, new JoinSpec[0]);
                var emptyJoinReader = new NetReader(emptyJoinBytes, 0, emptyJoinBytes.Length);
                on27901.Invoke(controller, new object[] { emptyJoinReader });
                pass &= emptyJoinReader.Remaining == 0 && model.HasJoinInfo && model.CanEnterScene == 0 && model.JoinList.Count == 0 && frames.Count == 0;

                JoinSpec firstJoin = new JoinSpec(uint.MaxValue, ushort.MaxValue, 0);
                JoinSpec secondJoin = new JoinSpec(uint.MaxValue, 1, ushort.MaxValue);
                byte[] fullJoinBytes = JoinPacket(byte.MaxValue, new[] { firstJoin, secondJoin });
                var fullJoinReader = new NetReader(fullJoinBytes, 0, fullJoinBytes.Length);
                on27901.Invoke(controller, new object[] { fullJoinReader });
                pass &= fullJoinReader.Remaining == 0 && model.HasJoinInfo && model.CanEnterScene == byte.MaxValue && model.JoinList.Count == 2
                    && IsJoin(model.JoinList[0], firstJoin) && IsJoin(model.JoinList[1], secondJoin) && frames.Count == 0;

                controller.RequestJoinInfo();
                pass &= frames.Count == 1 && IsExactJoinRequest(frames[0]) && model.JoinList.Count == 2 && IsJoin(model.JoinList[0], firstJoin);
                frames.Clear();

                byte[] isolatedTimeBytes = new CliVerify.Pkt().I(10).I(11).I(12).Bytes();
                var isolatedTimeReader = new NetReader(isolatedTimeBytes, 0, isolatedTimeBytes.Length);
                on27900.Invoke(controller, new object[] { isolatedTimeReader });
                pass &= isolatedTimeReader.Remaining == 0 && model.HasData && model.OpenTime == 10 && model.EnterTime == 11 && model.EndTime == 12 && model.JoinList.Count == 2;
                byte[] lessJoinBytes = JoinPacket(0, new[] { new JoinSpec(13, 14, 15) });
                var lessJoinReader = new NetReader(lessJoinBytes, 0, lessJoinBytes.Length);
                on27901.Invoke(controller, new object[] { lessJoinReader });
                pass &= lessJoinReader.Remaining == 0 && model.HasData && model.OpenTime == 10 && model.EnterTime == 11 && model.EndTime == 12
                    && model.CanEnterScene == 0 && model.JoinList.Count == 1 && model.JoinList[0].Scene == 13 && frames.Count == 0;

                byte[] clearJoinBytes = JoinPacket(byte.MaxValue, new JoinSpec[0]);
                var clearJoinReader = new NetReader(clearJoinBytes, 0, clearJoinBytes.Length);
                on27901.Invoke(controller, new object[] { clearJoinReader });
                pass &= clearJoinReader.Remaining == 0 && model.HasJoinInfo && model.CanEnterScene == byte.MaxValue && model.JoinList.Count == 0 && model.HasData && model.OpenTime == 10 && frames.Count == 0;

                controller.RequestReliveInfo();
                pass &= frames.Count == 1 && IsExactReliveRequest(frames[0]) && !model.HasReliveInfo;
                frames.Clear();
                byte[] fullReliveBytes = new CliVerify.Pkt().H(ushort.MaxValue).I(uint.MaxValue).I(4000000000L).I(1).Bytes();
                var fullReliveReader = new NetReader(fullReliveBytes, 0, fullReliveBytes.Length);
                on27906.Invoke(controller, new object[] { fullReliveReader });
                pass &= fullReliveReader.Remaining == 0 && model.HasReliveInfo && model.DieTimes == ushort.MaxValue
                    && model.Time == uint.MaxValue && model.DieTime == 4000000000U && model.SafeTime == 1 && model.HasData && model.HasJoinInfo && frames.Count == 0;
                byte[] smallReliveBytes = new CliVerify.Pkt().H(2).I(3).I(4).I(5).Bytes();
                var smallReliveReader = new NetReader(smallReliveBytes, 0, smallReliveBytes.Length);
                on27906.Invoke(controller, new object[] { smallReliveReader });
                pass &= smallReliveReader.Remaining == 0 && model.DieTimes == 2 && model.Time == 3 && model.DieTime == 4 && model.SafeTime == 5;
                controller.RequestReliveInfo();
                pass &= frames.Count == 1 && IsExactReliveRequest(frames[0]) && model.DieTimes == 2 && model.Time == 3 && model.DieTime == 4 && model.SafeTime == 5;
                frames.Clear();
                byte[] reliveIsolatedTimeBytes = new CliVerify.Pkt().I(20).I(21).I(22).Bytes();
                var reliveIsolatedTimeReader = new NetReader(reliveIsolatedTimeBytes, 0, reliveIsolatedTimeBytes.Length);
                on27900.Invoke(controller, new object[] { reliveIsolatedTimeReader });
                pass &= reliveIsolatedTimeReader.Remaining == 0 && model.OpenTime == 20 && model.EnterTime == 21 && model.EndTime == 22
                    && model.DieTimes == 2 && model.Time == 3 && model.DieTime == 4 && model.SafeTime == 5;
                byte[] reliveIsolatedJoinBytes = JoinPacket(1, new[] { new JoinSpec(23, 24, 25) });
                var reliveIsolatedJoinReader = new NetReader(reliveIsolatedJoinBytes, 0, reliveIsolatedJoinBytes.Length);
                on27901.Invoke(controller, new object[] { reliveIsolatedJoinReader });
                pass &= reliveIsolatedJoinReader.Remaining == 0 && model.OpenTime == 20 && model.EnterTime == 21 && model.EndTime == 22
                    && model.CanEnterScene == 1 && model.JoinList.Count == 1 && model.JoinList[0].Scene == 23
                    && model.DieTimes == 2 && model.Time == 3 && model.DieTime == 4 && model.SafeTime == 5 && frames.Count == 0;
                byte[] zeroReliveBytes = new CliVerify.Pkt().H(0).I(0).I(0).I(0).Bytes();
                var zeroReliveReader = new NetReader(zeroReliveBytes, 0, zeroReliveBytes.Length);
                on27906.Invoke(controller, new object[] { zeroReliveReader });
                pass &= zeroReliveReader.Remaining == 0 && model.HasReliveInfo && model.DieTimes == 0 && model.Time == 0 && model.DieTime == 0 && model.SafeTime == 0;

                controller.Dispose();
                pass &= !controller.IsInitialized && !handlers.Contains(27900) && !handlers.Contains(27901) && !handlers.Contains(27906)
                    && !model.HasData && !model.HasJoinInfo && !model.HasReliveInfo && model.OpenTime == 0 && model.EnterTime == 0 && model.EndTime == 0 && model.CanEnterScene == 0 && model.JoinList.Count == 0
                    && model.DieTimes == 0 && model.Time == 0 && model.DieTime == 0 && model.SafeTime == 0;

                Debug.Log("CLIVERIFY eternity VERDICT pass=" + pass);
            }
            finally
            {
                try
                {
                    if (controller.IsInitialized)
                    {
                        controller.Dispose();
                    }

                    model.Reset();
                    if (oldHasData)
                    {
                        model.Replace(oldOpenTime, oldEnterTime, oldEndTime);
                    }
                    if (oldHasJoinInfo)
                    {
                        model.ReplaceJoinInfo(oldCanEnterScene, oldJoinList);
                    }
                    if (oldHasReliveInfo)
                    {
                        model.ReplaceReliveInfo(oldDieTimes, oldTime, oldDieTime, oldSafeTime);
                    }

                    role.Level = oldLevel;
                    if (hasBaseInfoField != null)
                    {
                        hasBaseInfoField.SetValue(role, oldHasBaseInfo);
                    }

                    if (wasInitialized)
                    {
                        controller.Init();
                    }

                    if (lastLevelField != null)
                    {
                        lastLevelField.SetValue(controller, oldLastLevel);
                    }

                    if (interceptField != null)
                    {
                        interceptField.SetValue(null, oldIntercept);
                    }

                    if (handlers != null)
                    {
                        foreach (int proto in new[] { 27900, 27901, 27906 })
                        {
                            if (oldHandlers.TryGetValue(proto, out object handler)) handlers[proto] = handler;
                            else handlers.Remove(proto);
                        }
                    }

                    if (eventHandlers != null)
                    {
                        eventHandlers.Remove(GlobalEvent.EVT_ROLE_INFO_UPDATE);
                        if (oldHadRoleEvent)
                        {
                            eventHandlers[GlobalEvent.EVT_ROLE_INFO_UPDATE] = new List<Delegate>(oldRoleSubscribers);
                        }
                    }

                    restored = controller.IsInitialized == wasInitialized
                        && model.HasData == oldHasData && (!oldHasData || model.OpenTime == oldOpenTime && model.EnterTime == oldEnterTime && model.EndTime == oldEndTime)
                        && model.HasJoinInfo == oldHasJoinInfo && (!oldHasJoinInfo || model.CanEnterScene == oldCanEnterScene && SameJoins(model.JoinList, oldJoinList))
                        && model.HasReliveInfo == oldHasReliveInfo && (!oldHasReliveInfo || model.DieTimes == oldDieTimes && model.Time == oldTime && model.DieTime == oldDieTime && model.SafeTime == oldSafeTime)
                        && role.Level == oldLevel && hasBaseInfoField != null && (bool)hasBaseInfoField.GetValue(role) == oldHasBaseInfo
                        && lastLevelField != null && (int)lastLevelField.GetValue(controller) == oldLastLevel
                        && interceptField != null && ReferenceEquals(interceptField.GetValue(null), oldIntercept)
                        && HandlersMatch(handlers, oldHandlers)
                        && RoleSubscribersMatch(eventHandlers, oldHadRoleEvent, oldRoleSubscribers);
                }
                catch (Exception exception)
                {
                    Debug.LogError("CLIVERIFY eternity restore EXCEPTION " + exception);
                    restored = false;
                }
                Debug.Log("CLIVERIFY eternity restored=" + restored + " pass=" + pass);
            }
            return pass && restored ? 0 : 3;
        }

        private static bool IsExactRequest(byte[] frame)
        {
            return frame != null
                && frame.Length == 6
                && frame[0] == 0 && frame[1] == 6
                && frame[2] == 0x03 && frame[3] == 0xE8
                && frame[4] == (byte)(Proto.ETERNITY_TIME_INFO >> 8)
                && frame[5] == (byte)(Proto.ETERNITY_TIME_INFO & 0xFF);
        }

        private static bool IsExactJoinRequest(byte[] frame)
        {
            return frame != null && frame.Length == 6 && frame[0] == 0 && frame[1] == 6 && frame[2] == 0x03 && frame[3] == 0xE8
                && frame[4] == (byte)(Proto.ETERNITY_JOIN_INFO >> 8) && frame[5] == (byte)(Proto.ETERNITY_JOIN_INFO & 0xFF);
        }

        private static bool IsExactReliveRequest(byte[] frame)
        {
            return frame != null && frame.Length == 6 && frame[0] == 0 && frame[1] == 6 && frame[2] == 0x03 && frame[3] == 0xE8
                && frame[4] == (byte)(Proto.ETERNITY_RELIVE_INFO >> 8) && frame[5] == (byte)(Proto.ETERNITY_RELIVE_INFO & 0xFF);
        }

        private static byte[] JoinPacket(byte canEnterScene, JoinSpec[] joins)
        {
            var packet = new CliVerify.Pkt().C(canEnterScene).H(joins.Length);
            foreach (JoinSpec join in joins) packet.I(join.Scene).H(join.SelfServerNum).H(join.SceneNum);
            return packet.Bytes();
        }

        private static bool IsJoin(EternityModel.JoinEntry actual, JoinSpec expected)
        {
            return actual.Scene == expected.Scene && actual.SelfServerNum == expected.SelfServerNum && actual.SceneNum == expected.SceneNum;
        }

        private static bool SameJoins(IReadOnlyList<EternityModel.JoinEntry> actual, IReadOnlyList<EternityModel.JoinEntry> expected)
        {
            if (actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++)
            {
                if (actual[i].Scene != expected[i].Scene || actual[i].SelfServerNum != expected[i].SelfServerNum || actual[i].SceneNum != expected[i].SceneNum) return false;
            }
            return true;
        }

        private static bool HandlersMatch(IDictionary handlers, Dictionary<int, object> expected)
        {
            if (handlers == null) return false;
            foreach (int proto in new[] { 27900, 27901, 27906 })
            {
                bool had = expected.TryGetValue(proto, out object handler);
                if (handlers.Contains(proto) != had || had && !ReferenceEquals(handlers[proto], handler)) return false;
            }
            return true;
        }

        private static bool RoleSubscribersMatch(IDictionary handlers, bool expectedHadEvent, List<Delegate> expected)
        {
            if (handlers == null || handlers.Contains(GlobalEvent.EVT_ROLE_INFO_UPDATE) != expectedHadEvent) return false;
            if (!expectedHadEvent) return true;
            var actual = handlers[GlobalEvent.EVT_ROLE_INFO_UPDATE] as List<Delegate>;
            if (actual == null || actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++)
            {
                if (!ReferenceEquals(actual[i], expected[i])) return false;
            }
            return true;
        }

        private struct JoinSpec
        {
            public readonly uint Scene; public readonly ushort SelfServerNum; public readonly ushort SceneNum;
            public JoinSpec(uint scene, ushort selfServerNum, ushort sceneNum) { Scene = scene; SelfServerNum = selfServerNum; SceneNum = sceneNum; }
        }
    }
}
