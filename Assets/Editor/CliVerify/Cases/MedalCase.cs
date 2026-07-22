using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Medal;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>13401 勋章基础快照协议回归。</summary>
    public static class MedalCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;
        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e) { Debug.LogError("CLIVERIFY medal EXCEPTION " + e); return Task.FromResult(3); }
        }
        private static int RunSync()
        {
            MedalController controller = MedalController.Instance;
            MedalModel model = MedalModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            uint oldId = model.Id, oldLevel = model.StrengthenLevel, oldExp = model.StrengthenExp, oldPower = model.Power, oldPass = model.PassLayers;
            ulong oldHonour = model.Honour;
            bool oldHasData = model.HasData;
            FieldInfo intercept = typeof(MedalController).GetField("s_outboundIntercept", SF);
            object oldIntercept = intercept?.GetValue(null);
            try
            {
                controller.Init();
                model.Reset();
                MethodInfo on13401 = typeof(MedalController).GetMethod("On13401", F);
                var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
                bool pass = intercept != null && on13401 != null && handlers != null && handlers.Contains(Proto.MEDAL_INFO) && !handlers.Contains(13400);
                void Check(string tag, bool ok) { Debug.Log("CLIVERIFY medal " + tag + " ok=" + ok); if (!ok) pass = false; }
                Check("seams/register-only-13401", pass);
                if (!pass) return 3;
                var frames = new List<byte[]>();
                intercept.SetValue(null, new Func<byte[], bool>(f => { frames.Add(f); return true; }));
                controller.RequestStartup();
                Check("startup exact empty frame", Frame(frames, Proto.MEDAL_INFO));
                frames.Clear();
                byte[] first = new CliVerify.Pkt().I(1).I(2).I(3).L(5000000000L).I(4).I(5).Bytes();
                var firstReader = new NetReader(first, 0, first.Length);
                on13401.Invoke(controller, new object[] { firstReader });
                Check("fields/read-to-end/no-outbound", firstReader.Remaining == 0 && model.HasData && model.Id == 1 && model.StrengthenLevel == 2
                    && model.StrengthenExp == 3 && model.Honour == 5000000000UL && model.Power == 4 && model.PassLayers == 5 && frames.Count == 0);
                byte[] replacement = new CliVerify.Pkt().I(11).I(12).I(13).L(14).I(15).I(16).Bytes();
                var replacementReader = new NetReader(replacement, 0, replacement.Length);
                on13401.Invoke(controller, new object[] { replacementReader });
                Check("full replace", replacementReader.Remaining == 0 && model.Id == 11 && model.StrengthenLevel == 12 && model.StrengthenExp == 13
                    && model.Honour == 14 && model.Power == 15 && model.PassLayers == 16 && frames.Count == 0);
                controller.Dispose();
                Check("dispose reset", !controller.IsInitialized && !model.HasData && model.Id == 0 && model.Honour == 0 && model.Power == 0);
                Debug.Log("CLIVERIFY medal VERDICT pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                if (oldHasData) model.ReplaceData(oldId, oldLevel, oldExp, oldHonour, oldPower, oldPass);
                if (wasInitialized) controller.Init();
                if (intercept != null) intercept.SetValue(null, oldIntercept);
            }
        }
        private static bool Frame(IReadOnlyList<byte[]> frames, int id)
        {
            if (frames.Count != 1 || frames[0] == null) return false;
            byte[] f = frames[0];
            return f.Length == 6 && f[0] == 0 && f[1] == 6 && f[2] == 3 && f[3] == 232 && f[4] == (byte)(id >> 8) && f[5] == (byte)id;
        }
    }
}
