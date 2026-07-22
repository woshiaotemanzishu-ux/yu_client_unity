using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Armor;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>14401 不朽圣骸基础快照协议回归。</summary>
    public static class ArmorCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e) { Debug.LogError("CLIVERIFY armor EXCEPTION " + e); return Task.FromResult(3); }
        }

        private static int RunSync()
        {
            ArmorController controller = ArmorController.Instance;
            ArmorModel model = ArmorModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            var savedStages = new List<ArmorModel.StageEntry>(model.Stages);
            bool savedHasData = model.HasData;
            FieldInfo intercept = typeof(ArmorController).GetField("s_outboundIntercept", SF);
            object oldIntercept = intercept?.GetValue(null);
            try
            {
                controller.Init();
                model.Reset();
                MethodInfo on14401 = typeof(ArmorController).GetMethod("On14401", F);
                FieldInfo handlersField = typeof(NetManager).GetField("_handlers", SF);
                var handlers = handlersField?.GetValue(null) as IDictionary;
                bool pass = intercept != null && on14401 != null && handlers != null && handlers.Contains(Proto.ARMOR_INFO);
                void Check(string tag, bool ok) { Debug.Log("CLIVERIFY armor " + tag + " ok=" + ok); if (!ok) pass = false; }
                Check("seams/register", pass);
                if (!pass) return 3;

                var frames = new List<byte[]>();
                intercept.SetValue(null, new Func<byte[], bool>(f => { frames.Add(f); return true; }));
                controller.RequestStartup();
                Check("startup exact frame", Frame(frames, Proto.ARMOR_INFO, 0, 0));

                frames.Clear();
                byte[] first = new CliVerify.Pkt().H(2)
                    .C(9).H(1).C(10).C(1).H(1).I(11).C(12).C(0)
                    .C(1).H(2).C(3).C(1).H(2).I(5).C(6).C(1).I(3000000000L).C(4).C(0).C(2).C(0).H(1).I(7).C(8).C(1).Bytes();
                var firstReader = new NetReader(first, 0, first.Length);
                on14401.Invoke(controller, new object[] { firstReader });
                Check("nested fields/read-to-end", firstReader.Remaining == 0 && model.HasData && model.Stages.Count == 2
                    && model.Stages[0].Stage == 1 && model.Stages[0].Types.Count == 2
                    && model.Stages[0].Types[0].Type == 2 && model.Stages[0].Types[0].Positions[0].Position == 8
                    && model.Stages[0].Types[1].Type == 3 && model.Stages[0].Types[1].Positions.Count == 2
                    && model.Stages[0].Types[1].Positions[0].Position == 4
                    && model.Stages[0].Types[1].Positions[0].GTypeId == 3000000000U
                    && model.Stages[0].Types[1].Positions[0].Status == 0
                    && model.Stages[1].Stage == 9 && model.Stages[1].Types[0].Positions[0].Position == 12 && frames.Count == 0);

                byte[] replacement = new CliVerify.Pkt().H(1).C(2).H(0).Bytes();
                var replacementReader = new NetReader(replacement, 0, replacement.Length);
                on14401.Invoke(controller, new object[] { replacementReader });
                Check("full replace", replacementReader.Remaining == 0 && model.HasData && model.Stages.Count == 1 && model.Stages[0].Stage == 2 && frames.Count == 0);

                controller.Dispose();
                Check("dispose reset", !controller.IsInitialized && !model.HasData && model.Stages.Count == 0);
                Debug.Log("CLIVERIFY armor VERDICT pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                if (savedHasData) model.ReplaceData(savedStages);
                if (wasInitialized) controller.Init();
                if (intercept != null) intercept.SetValue(null, oldIntercept);
            }
        }

        private static bool Frame(IReadOnlyList<byte[]> frames, int id, byte a, byte b)
        {
            if (frames.Count != 1 || frames[0] == null) return false;
            byte[] f = frames[0];
            return f.Length == 8 && f[0] == 0 && f[1] == 8 && f[2] == 3 && f[3] == 232
                && f[4] == (byte)(id >> 8) && f[5] == (byte)id && f[6] == a && f[7] == b;
        }
    }
}
