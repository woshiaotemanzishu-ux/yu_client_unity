using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.KfStage;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>10200 跨服分组基础快照回归。</summary>
    public static class KfStageCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;
        public static Task<int> Run() { try { return Task.FromResult(RunSync()); } catch (Exception e) { Debug.LogError("CLIVERIFY kfstage EXCEPTION " + e); return Task.FromResult(3); } }
        private static int RunSync()
        {
            KfStageController controller = KfStageController.Instance; KfStageModel model = KfStageModel.Instance;
            bool wasInitialized = controller.IsInitialized; uint oldOpenDay = model.OpenDay; var oldServers = new List<KfStageModel.ServerEntry>(model.Servers); var oldModules = new List<KfStageModel.ModuleEntry>(model.Modules); bool oldHasData = model.HasData;
            FieldInfo intercept = typeof(KfStageController).GetField("s_outboundIntercept", SF); object oldIntercept = intercept?.GetValue(null);
            try
            {
                controller.Init(); model.Reset(); MethodInfo on10200 = typeof(KfStageController).GetMethod("On10200", F);
                var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
                bool pass = intercept != null && on10200 != null && handlers != null
                    && handlers.Contains(Proto.KF_STAGE_INFO) && !handlers.Contains(10204);
                void Check(string tag, bool ok) { Debug.Log("CLIVERIFY kfstage " + tag + " ok=" + ok); if (!ok) pass = false; }
                Check("seams/register", pass); if (!pass) return 3;
                var frames = new List<byte[]>(); intercept.SetValue(null, new Func<byte[], bool>(f => { frames.Add(f); return true; })); controller.RequestStartup();
                Check("startup exact empty frame", Frame(frames, Proto.KF_STAGE_INFO)); frames.Clear();
                byte[] first = new CliVerify.Pkt().I(33).H(2).H(1).H(10).S("仙域一服").H(88).H(2).H(20).S("Beta").H(99)
                    .H(2).H(100).C(1).H(70).H(2).H(1).H(2).H(2).H(3).H(4).H(200).C(2).H(71).H(1).H(2).H(1).H(5).Bytes();
                var firstReader = new NetReader(first, 0, first.Length); on10200.Invoke(controller, new object[] { firstReader });
                Check("nested utf8/read-to-end", firstReader.Remaining == 0 && model.HasData && model.OpenDay == 33 && model.Servers.Count == 2
                    && model.Servers[0].ServerId == 1 && model.Servers[0].ServerNum == 10 && model.Servers[0].ServerName == "仙域一服" && model.Servers[0].WorldLevel == 88
                    && model.FindServer(2).ServerNum == 20 && model.FindServer(2).ServerName == "Beta" && model.FindServer(2).WorldLevel == 99
                    && model.Modules.Count == 2 && model.FindModule(100).Mod == 1 && model.FindModule(100).AverageLevel == 70
                    && model.FindModule(100).ServerIds.Count == 2 && model.FindModule(100).ServerIds[0] == 1 && model.FindModule(100).ServerIds[1] == 2
                    && model.FindModule(100).NextServerIds.Count == 2 && model.FindModule(100).NextServerIds[0] == 3 && model.FindModule(100).NextServerIds[1] == 4
                    && model.FindModule(200).Mod == 2 && model.FindModule(200).AverageLevel == 71 && model.FindModule(200).ServerIds.Count == 1
                    && model.FindModule(200).ServerIds[0] == 2 && model.FindModule(200).NextServerIds.Count == 1 && model.FindModule(200).NextServerIds[0] == 5
                    && frames.Count == 0);
                byte[] replacement = new CliVerify.Pkt().I(44).H(0).H(0).Bytes(); var replacementReader = new NetReader(replacement, 0, replacement.Length); on10200.Invoke(controller, new object[] { replacementReader });
                Check("full replace empty", replacementReader.Remaining == 0 && model.HasData && model.OpenDay == 44 && model.Servers.Count == 0 && model.Modules.Count == 0 && frames.Count == 0);
                controller.Dispose(); Check("dispose reset", !controller.IsInitialized && !model.HasData && model.OpenDay == 0 && model.Servers.Count == 0 && model.Modules.Count == 0);
                Debug.Log("CLIVERIFY kfstage VERDICT pass=" + pass); return pass ? 0 : 3;
            }
            finally { if (controller.IsInitialized) controller.Dispose(); model.Reset(); if (oldHasData) model.ReplaceData(oldOpenDay, oldServers, oldModules); if (wasInitialized) controller.Init(); if (intercept != null) intercept.SetValue(null, oldIntercept); }
        }
        private static bool Frame(IReadOnlyList<byte[]> frames, int id) { if (frames.Count != 1 || frames[0] == null) return false; byte[] f = frames[0]; return f.Length == 6 && f[0] == 0 && f[1] == 6 && f[2] == 3 && f[3] == 232 && f[4] == (byte)(id >> 8) && f[5] == (byte)id; }
    }
}
