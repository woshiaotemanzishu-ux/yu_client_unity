using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.TreasureMap;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class TreasureMapCase
    {
        private const BindingFlags InstancePrivate = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags StaticPrivate = BindingFlags.NonPublic | BindingFlags.Static;
        public static Task<int> Run() { try { return Task.FromResult(RunSync()); } catch (Exception e) { Debug.LogError("CLIVERIFY treasure-map EXCEPTION " + e); return Task.FromResult(3); } }

        private static int RunSync()
        {
            TreasureMapController controller = TreasureMapController.Instance;
            TreasureMapModel model = TreasureMapModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            bool oldHas = model.HasDrawLog;
            var oldLogs = new List<TreasureMapModel.DrawLogEntry>(model.DrawLogs);
            FieldInfo intercept = typeof(TreasureMapController).GetField("s_outboundIntercept", StaticPrivate);
            object oldIntercept = intercept?.GetValue(null);
            try
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                controller.Init();
                MethodInfo on = typeof(TreasureMapController).GetMethod("On20303", InstancePrivate);
                var handlers = typeof(NetManager).GetField("_handlers", StaticPrivate)?.GetValue(null) as IDictionary;
                bool pass = intercept != null && on != null && handlers != null && handlers.Contains(Proto.TREASURE_MAP_DRAW_LOG);
                for (int id = 20300; id <= 20304; id++) if (id != Proto.TREASURE_MAP_DRAW_LOG) pass &= !handlers.Contains(id);
                Check(ref pass, "seams/register-only-20303", pass);
                if (!pass) return 3;

                var frames = new List<byte[]>();
                intercept.SetValue(null, new Func<byte[], bool>(f => { frames.Add(f); return true; }));
                controller.RequestDrawLog();
                Check(ref pass, "request exact six-byte empty frame", ExactFrames(frames, Proto.TREASURE_MAP_DRAW_LOG) && !model.HasDrawLog);
                frames.Clear();

                Feed(on, controller, Packet(new LogSpec[0]), out NetReader emptyReader);
                Check(ref pass, "empty loaded/read-to-end/no-outbound", emptyReader.Remaining == 0 && model.HasDrawLog && model.DrawLogs.Count == 0 && frames.Count == 0);

                var many = new[]
                {
                    new LogSpec(uint.MaxValue, -1L, "中文", new[] { new RewardSpec(byte.MaxValue, uint.MaxValue, uint.MaxValue), new RewardSpec(0, 0, 0), new RewardSpec(byte.MaxValue, uint.MaxValue, uint.MaxValue) }),
                    new LogSpec(0, -1L, "", new RewardSpec[0])
                };
                Feed(on, controller, Packet(many), out NetReader manyReader);
                TreasureMapModel.DrawLogEntry first = model.DrawLogs.Count > 0 ? model.DrawLogs[0] : null;
                TreasureMapModel.DrawLogEntry second = model.DrawLogs.Count > 1 ? model.DrawLogs[1] : null;
                Check(ref pass, "multiple/boundaries/names/duplicate-role/reward-empty-duplicate-order", manyReader.Remaining == 0 && model.DrawLogs.Count == 2 && first != null && first.ServerNum == uint.MaxValue && first.RoleId == -1L && first.Name == "中文" && first.Rewards.Count == 3 && Eq(first.Rewards[0], many[0].Rewards[0]) && Eq(first.Rewards[1], many[0].Rewards[1]) && Eq(first.Rewards[2], many[0].Rewards[2]) && second != null && second.ServerNum == 0 && second.RoleId == -1L && second.Name == "" && second.Rewards.Count == 0 && frames.Count == 0);

                controller.RequestDrawLog();
                Check(ref pass, "no-response preserves snapshot", ExactFrames(frames, Proto.TREASURE_MAP_DRAW_LOG) && model.DrawLogs.Count == 2 && model.DrawLogs[0].Name == "中文" && model.DrawLogs[1].RoleId == -1L);
                frames.Clear();

                var one = new[] { new LogSpec(1, 2, "one", new[] { new RewardSpec(3, 4, 5) }) };
                Feed(on, controller, Packet(one), out NetReader oneReader);
                Check(ref pass, "multiple-to-one whole-replace", oneReader.Remaining == 0 && model.DrawLogs.Count == 1 && model.DrawLogs[0].ServerNum == 1 && model.DrawLogs[0].RoleId == 2 && model.DrawLogs[0].Name == "one" && model.DrawLogs[0].Rewards.Count == 1 && Eq(model.DrawLogs[0].Rewards[0], one[0].Rewards[0]) && frames.Count == 0);

                Feed(on, controller, Packet(new LogSpec[0]), out NetReader clearReader);
                Check(ref pass, "one-to-empty clears", clearReader.Remaining == 0 && model.HasDrawLog && model.DrawLogs.Count == 0 && frames.Count == 0);

                controller.Dispose();
                Check(ref pass, "dispose unregisters-and-resets", !controller.IsInitialized && !model.HasDrawLog && model.DrawLogs.Count == 0 && !handlers.Contains(Proto.TREASURE_MAP_DRAW_LOG));
                Debug.Log("CLIVERIFY treasure-map VERDICT pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                if (oldHas) model.ReplaceDrawLog(oldLogs);
                if (wasInitialized) controller.Init();
                if (intercept != null) intercept.SetValue(null, oldIntercept);
            }
        }

        private static void Feed(MethodInfo on, TreasureMapController controller, byte[] bytes, out NetReader reader) { reader = new NetReader(bytes, 0, bytes.Length); on.Invoke(controller, new object[] { reader }); }
        private static void Check(ref bool pass, string tag, bool ok) { Debug.Log("CLIVERIFY treasure-map " + tag + " ok=" + ok); if (!ok) pass = false; }
        private static bool ExactFrames(IReadOnlyList<byte[]> frames, params int[] ids) { if (frames.Count != ids.Length) return false; for (int i = 0; i < ids.Length; i++) { byte[] f = frames[i]; if (f == null || f.Length != 6 || f[0] != 0 || f[1] != 6 || f[2] != 3 || f[3] != 232 || f[4] != (byte)(ids[i] >> 8) || f[5] != (byte)(ids[i] & 0xFF)) return false; } return true; }
        private static byte[] Packet(LogSpec[] logs) { var packet = new CliVerify.Pkt().H(logs.Length); foreach (LogSpec log in logs) { packet.I(log.ServerNum).L(log.RoleId).S(log.Name).H(log.Rewards.Length); foreach (RewardSpec reward in log.Rewards) packet.C(reward.Style).I(reward.TypeId).I(reward.Count); } return packet.Bytes(); }
        private static bool Eq(TreasureMapModel.RewardEntry actual, RewardSpec expected) => actual.Style == expected.Style && actual.TypeId == expected.TypeId && actual.Count == expected.Count;
        private struct RewardSpec { public readonly byte Style; public readonly uint TypeId; public readonly uint Count; public RewardSpec(byte style, uint typeId, uint count) { Style = style; TypeId = typeId; Count = count; } }
        private struct LogSpec { public readonly uint ServerNum; public readonly long RoleId; public readonly string Name; public readonly RewardSpec[] Rewards; public LogSpec(uint serverNum, long roleId, string name, RewardSpec[] rewards) { ServerNum = serverNum; RoleId = roleId; Name = name; Rewards = rewards; } }
    }
}
