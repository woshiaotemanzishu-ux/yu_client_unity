using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
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
            bool wasInitialized = controller.IsInitialized;
            bool oldHasData = model.HasData;
            uint oldOpenTime = model.OpenTime;
            uint oldEnterTime = model.EnterTime;
            uint oldEndTime = model.EndTime;
            bool oldHasJoinInfo = model.HasJoinInfo;
            byte oldCanEnterScene = model.CanEnterScene;
            var oldJoinList = new List<EternityModel.JoinEntry>(model.JoinList);
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
                foreach (int id in new[] { 27900, 27901 })
                {
                    if (handlers.Contains(id)) oldHandlers[id] = handlers[id];
                }
            }
            bool pass = false;
            bool restored = false;

            try
            {
                controller.Init();
                model.Reset();

                MethodInfo on27900 = typeof(EternityController).GetMethod("On27900", InstanceNonPublic);
                MethodInfo on27901 = typeof(EternityController).GetMethod("On27901", InstanceNonPublic);
                MethodInfo onRoleInfoUpdate = typeof(EternityController).GetMethod("OnRoleInfoUpdate", InstanceNonPublic);
                handlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                pass = hasBaseInfoField != null && interceptField != null && lastLevelField != null
                    && on27900 != null && on27901 != null && onRoleInfoUpdate != null && handlers != null && handlers.Contains(27900) && handlers.Contains(27901);
                for (int proto = 27902; proto <= 27909; proto++)
                {
                    pass &= !handlers.Contains(proto);
                }

                if (!pass)
                {
                    throw new InvalidOperationException("Eternity reflection/protocol registration precondition failed.");
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
                role.Level = 479;
                controller.RequestStartup();
                pass &= frames.Count == 0 && !model.HasData && !model.HasJoinInfo && model.OpenTime == 0 && model.EnterTime == 0 && model.EndTime == 0
                    && model.CanEnterScene == 0 && model.JoinList.Count == 0;

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

                controller.Dispose();
                pass &= !controller.IsInitialized && !handlers.Contains(27900) && !handlers.Contains(27901)
                    && !model.HasData && !model.HasJoinInfo && model.OpenTime == 0 && model.EnterTime == 0 && model.EndTime == 0 && model.CanEnterScene == 0 && model.JoinList.Count == 0;
            }
            finally
            {
                try
                {
                    if (controller.IsInitialized) controller.Dispose();

                    model.Reset();
                    if (oldHasData) model.Replace(oldOpenTime, oldEnterTime, oldEndTime);
                    if (oldHasJoinInfo) model.ReplaceJoinInfo(oldCanEnterScene, oldJoinList);

                    role.Level = oldLevel;
                    if (hasBaseInfoField != null) hasBaseInfoField.SetValue(role, oldHasBaseInfo);

                    if (wasInitialized) controller.Init();

                    if (lastLevelField != null) lastLevelField.SetValue(controller, oldLastLevel);
                    handlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                    if (handlers == null) throw new InvalidOperationException("Eternity handlers unavailable during restore.");
                    foreach (int id in new[] { 27900, 27901 })
                    {
                        if (oldHandlers.TryGetValue(id, out object handler)) handlers[id] = handler;
                        else handlers.Remove(id);
                    }

                    if (interceptField != null) interceptField.SetValue(null, oldIntercept);

                    restored = controller.IsInitialized == wasInitialized
                        && model.HasData == oldHasData && (!oldHasData || (model.OpenTime == oldOpenTime && model.EnterTime == oldEnterTime && model.EndTime == oldEndTime))
                        && model.HasJoinInfo == oldHasJoinInfo && (!oldHasJoinInfo || (model.CanEnterScene == oldCanEnterScene && JoinListsMatch(model.JoinList, oldJoinList)))
                        && role.Level == oldLevel && hasBaseInfoField != null && (bool)hasBaseInfoField.GetValue(role) == oldHasBaseInfo
                        && lastLevelField != null && (int)lastLevelField.GetValue(controller) == oldLastLevel
                        && (interceptField == null || ReferenceEquals(interceptField.GetValue(null), oldIntercept));
                    foreach (int id in new[] { 27900, 27901 })
                    {
                        bool existed = oldHandlers.TryGetValue(id, out object expected);
                        if (handlers.Contains(id) != existed || (existed && !ReferenceEquals(handlers[id], expected))) restored = false;
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogError("CLIVERIFY eternity restore " + exception);
                    restored = false;
                }
            }

            Debug.Log("CLIVERIFY eternity restored=" + restored + " VERDICT pass=" + pass);
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

        private static bool JoinListsMatch(IReadOnlyList<EternityModel.JoinEntry> actual, IReadOnlyList<EternityModel.JoinEntry> expected)
        {
            if (actual.Count != expected.Count) return false;
            for (int index = 0; index < expected.Count; index++)
            {
                EternityModel.JoinEntry a = actual[index];
                EternityModel.JoinEntry b = expected[index];
                if (a.Scene != b.Scene || a.SelfServerNum != b.SelfServerNum || a.SceneNum != b.SceneNum) return false;
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
