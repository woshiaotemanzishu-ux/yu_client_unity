using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Reincarnation;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>16400 天命觉醒激活列表快照回归。</summary>
    public static class ReincarnationCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;
        public static Task<int> Run() { try { return Task.FromResult(RunSync()); } catch (Exception e) { Debug.LogError("CLIVERIFY reincarnation EXCEPTION " + e); return Task.FromResult(3); } }
        private static int RunSync()
        {
            ReincarnationController controller = ReincarnationController.Instance; ReincarnationModel model = ReincarnationModel.Instance;
            bool wasInitialized = controller.IsInitialized; var oldIds = new List<uint>(model.ActiveIds); bool oldHasData = model.HasData;
            FieldInfo intercept = typeof(ReincarnationController).GetField("s_outboundIntercept", SF); object oldIntercept = intercept?.GetValue(null);
            try
            {
                controller.Init(); model.Reset(); MethodInfo on16400 = typeof(ReincarnationController).GetMethod("On16400", F);
                var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
                bool pass = intercept != null && on16400 != null && handlers != null && handlers.Contains(Proto.REINCARNATION_AWAKEN_INFO) && !handlers.Contains(16401);
                void Check(string tag, bool ok) { Debug.Log("CLIVERIFY reincarnation " + tag + " ok=" + ok); if (!ok) pass = false; }
                Check("seams/register-only-16400", pass); if (!pass) return 3;
                var frames = new List<byte[]>(); intercept.SetValue(null, new Func<byte[], bool>(f => { frames.Add(f); return true; })); controller.RequestStartup();
                Check("startup exact empty frame", Frame(frames, Proto.REINCARNATION_AWAKEN_INFO)); frames.Clear();
                byte[] first = new CliVerify.Pkt().H(4).I(1).I(3000000000L).I(1).I(3).Bytes(); var firstReader = new NetReader(first, 0, first.Length); on16400.Invoke(controller, new object[] { firstReader });
                Check("order-duplicate/read-to-end/no-outbound", firstReader.Remaining == 0 && model.HasData && model.ActiveIds.Count == 4 && model.ActiveIds[0] == 1 && model.ActiveIds[1] == 3000000000U && model.ActiveIds[2] == 1 && model.ActiveIds[3] == 3 && frames.Count == 0);
                byte[] replacement = new CliVerify.Pkt().H(0).Bytes(); var replacementReader = new NetReader(replacement, 0, replacement.Length); on16400.Invoke(controller, new object[] { replacementReader });
                Check("full replace empty", replacementReader.Remaining == 0 && model.HasData && model.ActiveIds.Count == 0 && frames.Count == 0);
                controller.Dispose(); Check("dispose reset", !controller.IsInitialized && !model.HasData && model.ActiveIds.Count == 0);
                Debug.Log("CLIVERIFY reincarnation VERDICT pass=" + pass); return pass ? 0 : 3;
            }
            finally { if (controller.IsInitialized) controller.Dispose(); model.Reset(); if (oldHasData) model.ReplaceData(oldIds); if (wasInitialized) controller.Init(); if (intercept != null) intercept.SetValue(null, oldIntercept); }
        }
        private static bool Frame(IReadOnlyList<byte[]> frames, int id) { if (frames.Count != 1 || frames[0] == null) return false; byte[] f = frames[0]; return f.Length == 6 && f[0] == 0 && f[1] == 6 && f[2] == 3 && f[3] == 232 && f[4] == (byte)(id >> 8) && f[5] == (byte)id; }
    }
}
