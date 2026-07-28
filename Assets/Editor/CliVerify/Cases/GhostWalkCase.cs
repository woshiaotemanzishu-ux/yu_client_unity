using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.GhostWalk;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class GhostWalkCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;
        private sealed class HandlerState { public bool Exists; public object Value; }

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunCore()); }
            catch (Exception e) { Debug.LogError("CLIVERIFY ghostwalk EXCEPTION " + e); return Task.FromResult(3); }
        }

        private static int RunCore()
        {
            GhostWalkController controller = GhostWalkController.Instance;
            GhostWalkModel model = GhostWalkModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            bool oldHasData = model.HasData;
            byte oldState = model.State;
            uint oldEndTime = model.EndTime;
            byte oldServerModule = model.ServerModule;
            uint oldGroupId = model.GroupId;
            ushort oldAverageWorldLevel = model.AverageWorldLevel;
            var oldServers = new List<GhostWalkModel.Server>(model.Servers);
            bool oldHasError = model.HasError;
            uint oldErrorCode = model.LastErrorCode;
            string oldErrorArgs = model.LastErrorArgs;
            bool oldHasBossInfo = model.HasBossInfo;
            var oldBossScenes = new List<GhostWalkModel.SceneBossInfo>(model.BossScenes);
            FieldInfo interceptor = typeof(GhostWalkController).GetField("s_outboundIntercept", SF);
            object oldInterceptor = interceptor?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var savedHandlers = new Dictionary<int, HandlerState>();
            for (int id = 20600; id <= 20605; id++) SaveHandler(handlers, savedHandlers, id);
            bool pass = false;
            bool restored = false;

            try
            {
                controller.Init();
                model.Reset();
                MethodInfo on20600 = typeof(GhostWalkController).GetMethod("On20600", F);
                MethodInfo on20601 = typeof(GhostWalkController).GetMethod("On20601", F);
                MethodInfo on20602 = typeof(GhostWalkController).GetMethod("On20602", F);
                pass = interceptor != null && handlers != null && on20600 != null && on20601 != null && on20602 != null
                    && handlers.Contains(20600) && handlers.Contains(20601) && handlers.Contains(20602)
                    && !handlers.Contains(20603)
                    && !handlers.Contains(20604) && !handlers.Contains(20605);

                object firstErrorHandler = handlers != null && handlers.Contains(20600) ? handlers[20600] : null;
                object firstInfoHandler = handlers != null && handlers.Contains(20601) ? handlers[20601] : null;
                object firstBossHandler = handlers != null && handlers.Contains(20602) ? handlers[20602] : null;
                controller.Init();
                pass &= firstErrorHandler != null && firstInfoHandler != null && firstBossHandler != null
                    && ReferenceEquals(handlers[20600], firstErrorHandler)
                    && ReferenceEquals(handlers[20601], firstInfoHandler)
                    && ReferenceEquals(handlers[20602], firstBossHandler);

                var frames = new List<byte[]>();
                if (pass)
                {
                    interceptor.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                    controller.RequestInfo();
                    pass &= Frame(frames, Proto.GHOST_WALK_INFO);
                    frames.Clear();
                    controller.RequestBossInfo(0);
                    pass &= FrameU32(frames, Proto.GHOST_WALK_BOSS_INFO, 0);
                    frames.Clear();
                    controller.RequestBossInfo(uint.MaxValue);
                    pass &= FrameU32(frames, Proto.GHOST_WALK_BOSS_INFO, uint.MaxValue);
                    frames.Clear();

                    byte[] bossManyBytes = new CliVerify.Pkt().H(2)
                        .I(uint.MaxValue).C(255).H(3).I(0).I(uint.MaxValue).I(0)
                        .I(7).C(0).H(2).I(1).I(1).Bytes();
                    var bossManyReader = new NetReader(bossManyBytes, 0, bossManyBytes.Length);
                    on20602.Invoke(controller, new object[] { bossManyReader });
                    pass &= bossManyReader.Remaining == 0 && model.HasBossInfo && model.BossScenes.Count == 2
                        && model.BossScenes[0].SceneId == uint.MaxValue && model.BossScenes[0].Num == byte.MaxValue
                        && model.BossScenes[0].BossIds.Count == 3 && model.BossScenes[0].BossIds[0] == 0
                        && model.BossScenes[0].BossIds[1] == uint.MaxValue && model.BossScenes[0].BossIds[2] == 0
                        && model.BossScenes[1].SceneId == 7 && model.BossScenes[1].Num == 0
                        && model.BossScenes[1].BossIds.Count == 2 && model.BossScenes[1].BossIds[0] == 1
                        && model.BossScenes[1].BossIds[1] == 1 && !model.HasData && !model.HasError && frames.Count == 0;

                    const string chineseName = "百鬼中文服";
                    byte[] firstBytes = new CliVerify.Pkt()
                        .C(255).I(4294967295L).C(254).I(4000000000L).H(2)
                        .H(0).H(65535).S(chineseName).H(65535).H(0)
                        .H(65535).H(0).S("Second").H(1).H(65535)
                        .H(65535)
                        .Bytes();
                    var firstReader = new NetReader(firstBytes, 0, firstBytes.Length);
                    on20601.Invoke(controller, new object[] { firstReader });
                    pass &= firstReader.Remaining == 0
                        && model.HasData && model.State == 255 && model.EndTime == uint.MaxValue
                        && model.ServerModule == 254 && model.GroupId == 4000000000U && model.Servers.Count == 2
                        && model.Servers[0].Id == 0 && model.Servers[0].Number == ushort.MaxValue
                        && model.Servers[0].Name == chineseName && model.Servers[0].OpenDay == ushort.MaxValue
                        && model.Servers[0].WorldLevel == 0
                        && model.Servers[1].Id == ushort.MaxValue && model.Servers[1].Number == 0
                        && model.Servers[1].Name == "Second" && model.Servers[1].OpenDay == 1
                        && model.Servers[1].WorldLevel == ushort.MaxValue
                        && model.AverageWorldLevel == ushort.MaxValue && !model.HasError && model.HasBossInfo && model.BossScenes.Count == 2 && frames.Count == 0;

                    pass &= VerifyError(on20600, controller, 0, string.Empty, () => model.HasError
                        && model.LastErrorCode == 0 && model.LastErrorArgs == string.Empty
                        && model.State == 255 && model.EndTime == uint.MaxValue && model.Servers.Count == 2
                        && model.Servers[0].Name == chineseName && frames.Count == 0);

                    byte[] secondBytes = new CliVerify.Pkt()
                        .C(1).I(2).C(3).I(4).H(1)
                        .H(5).H(6).S("替换服").H(7).H(8)
                        .H(9)
                        .Bytes();
                    var secondReader = new NetReader(secondBytes, 0, secondBytes.Length);
                    on20601.Invoke(controller, new object[] { secondReader });
                    pass &= secondReader.Remaining == 0
                        && model.HasData && model.State == 1 && model.EndTime == 2
                        && model.ServerModule == 3 && model.GroupId == 4 && model.Servers.Count == 1
                        && model.Servers[0].Id == 5 && model.Servers[0].Number == 6
                        && model.Servers[0].Name == "替换服" && model.Servers[0].OpenDay == 7
                        && model.Servers[0].WorldLevel == 8 && model.AverageWorldLevel == 9
                        && model.HasError && model.LastErrorCode == 0 && model.LastErrorArgs == string.Empty
                        && frames.Count == 0;

                    pass &= VerifyError(on20600, controller, 1, "\u6210\u529f", () => model.HasError
                        && model.LastErrorCode == 1 && model.LastErrorArgs == "\u6210\u529f"
                        && model.State == 1 && model.EndTime == 2 && model.Servers.Count == 1
                        && model.Servers[0].Name == "替换服" && frames.Count == 0);

                    byte[] thirdBytes = new CliVerify.Pkt().C(0).I(0).C(0).I(0).H(0).H(0).Bytes();
                    var thirdReader = new NetReader(thirdBytes, 0, thirdBytes.Length);
                    on20601.Invoke(controller, new object[] { thirdReader });
                    pass &= thirdReader.Remaining == 0
                        && model.HasData && model.State == 0 && model.EndTime == 0
                        && model.ServerModule == 0 && model.GroupId == 0 && model.Servers.Count == 0
                        && model.AverageWorldLevel == 0 && model.HasError && model.LastErrorCode == 1
                        && model.LastErrorArgs == "\u6210\u529f" && frames.Count == 0;

                    byte[] bossSingleBytes = new CliVerify.Pkt().H(1).I(3).C(2).H(1).I(4).Bytes();
                    var bossSingleReader = new NetReader(bossSingleBytes, 0, bossSingleBytes.Length);
                    on20602.Invoke(controller, new object[] { bossSingleReader });
                    pass &= bossSingleReader.Remaining == 0 && model.HasBossInfo && model.BossScenes.Count == 1
                        && model.BossScenes[0].SceneId == 3 && model.BossScenes[0].Num == 2 && model.BossScenes[0].BossIds.Count == 1
                        && model.BossScenes[0].BossIds[0] == 4 && model.HasData && model.Servers.Count == 0 && model.HasError && frames.Count == 0;
                    var bossEmptyReader = new NetReader(new CliVerify.Pkt().H(0).Bytes(), 0, 2);
                    on20602.Invoke(controller, new object[] { bossEmptyReader });
                    pass &= bossEmptyReader.Remaining == 0 && model.HasBossInfo && model.BossScenes.Count == 0
                        && model.HasData && model.HasError && frames.Count == 0;

                    pass &= VerifyError(on20600, controller, uint.MaxValue, "\u6700\u7ec8", () => model.HasError
                        && model.LastErrorCode == uint.MaxValue && model.LastErrorArgs == "\u6700\u7ec8"
                        && model.State == 0 && model.EndTime == 0 && model.Servers.Count == 0 && frames.Count == 0);

                    controller.Dispose();
                    pass &= !controller.IsInitialized && !model.HasData && model.State == 0 && model.EndTime == 0
                        && model.ServerModule == 0 && model.GroupId == 0 && model.Servers.Count == 0 && model.AverageWorldLevel == 0
                        && !model.HasError && model.LastErrorCode == 0 && model.LastErrorArgs == null
                        && !model.HasBossInfo && model.BossScenes.Count == 0
                        && !handlers.Contains(20600) && !handlers.Contains(20601) && !handlers.Contains(20602);
                }
                Debug.Log("CLIVERIFY ghostwalk VERDICT pass=" + pass);
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                if (oldHasData) model.Replace(oldState, oldEndTime, oldServerModule, oldGroupId, oldServers, oldAverageWorldLevel);
                if (oldHasBossInfo) model.ReplaceBossInfo(oldBossScenes);
                RestoreModelProperty(model, "HasError", oldHasError);
                RestoreModelProperty(model, "LastErrorCode", oldErrorCode);
                RestoreModelProperty(model, "LastErrorArgs", oldErrorArgs);
                if (wasInitialized) controller.Init();
                for (int id = 20600; id <= 20605; id++) RestoreHandler(handlers, savedHandlers[id], id);
                if (interceptor != null) interceptor.SetValue(null, oldInterceptor);
                restored = controller.IsInitialized == wasInitialized && model.HasData == oldHasData
                    && model.State == oldState && model.EndTime == oldEndTime && model.ServerModule == oldServerModule
                    && model.GroupId == oldGroupId && model.AverageWorldLevel == oldAverageWorldLevel && ServersMatch(model.Servers, oldServers)
                    && model.HasError == oldHasError && model.LastErrorCode == oldErrorCode && model.LastErrorArgs == oldErrorArgs
                    && model.HasBossInfo == oldHasBossInfo && BossScenesMatch(model.BossScenes, oldBossScenes)
                    && HandlersMatch(handlers, savedHandlers, 20600, 20605)
                    && (interceptor == null || ReferenceEquals(interceptor.GetValue(null), oldInterceptor));
                Debug.Log("CLIVERIFY ghostwalk restored=" + restored);
            }
            return pass && restored ? 0 : 3;
        }

        private static bool VerifyError(MethodInfo handler, GhostWalkController controller, uint code, string args, Func<bool> check)
        {
            byte[] bytes = new CliVerify.Pkt().I(code).S(args).Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0 && check();
        }

        private static void SaveHandler(IDictionary handlers, IDictionary<int, HandlerState> saved, int id)
        {
            bool exists = handlers != null && handlers.Contains(id);
            saved[id] = new HandlerState { Exists = exists, Value = exists ? handlers[id] : null };
        }

        private static void RestoreHandler(IDictionary handlers, HandlerState saved, int id)
        {
            if (handlers == null) return;
            if (saved.Exists) handlers[id] = saved.Value;
            else handlers.Remove(id);
        }

        private static bool HandlerMatches(IDictionary handlers, HandlerState saved, int id)
        {
            return handlers != null && handlers.Contains(id) == saved.Exists && (!saved.Exists || ReferenceEquals(handlers[id], saved.Value));
        }

        private static void RestoreModelProperty(GhostWalkModel model, string name, object value)
        {
            typeof(GhostWalkModel).GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.SetValue(model, value);
        }

        private static bool ServersMatch(IReadOnlyList<GhostWalkModel.Server> actual, IReadOnlyList<GhostWalkModel.Server> expected)
        {
            if (actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++)
                if (!ReferenceEquals(actual[i], expected[i])) return false;
            return true;
        }

        private static bool BossScenesMatch(IReadOnlyList<GhostWalkModel.SceneBossInfo> actual, IReadOnlyList<GhostWalkModel.SceneBossInfo> expected)
        {
            if (actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++) if (!ReferenceEquals(actual[i], expected[i])) return false;
            return true;
        }

        private static bool HandlersMatch(IDictionary handlers, IDictionary<int, HandlerState> saved, int first, int last)
        {
            for (int id = first; id <= last; id++) if (!HandlerMatches(handlers, saved[id], id)) return false;
            return true;
        }

        private static bool Frame(IReadOnlyList<byte[]> frames, int id)
        {
            if (frames.Count != 1 || frames[0] == null) return false;
            byte[] frame = frames[0];
            return frame.Length == 6 && frame[0] == 0 && frame[1] == 6 && frame[2] == 3 && frame[3] == 232
                && frame[4] == (byte)(id >> 8) && frame[5] == (byte)id;
        }

        private static bool FrameU32(IReadOnlyList<byte[]> frames, int id, uint value)
        {
            if (frames.Count != 1 || frames[0] == null || frames[0].Length != 10) return false;
            byte[] frame = frames[0];
            return frame[0] == 0 && frame[1] == 10 && frame[2] == 3 && frame[3] == 232
                && frame[4] == (byte)(id >> 8) && frame[5] == (byte)id
                && frame[6] == (byte)(value >> 24) && frame[7] == (byte)(value >> 16)
                && frame[8] == (byte)(value >> 8) && frame[9] == (byte)value;
        }
    }
}
