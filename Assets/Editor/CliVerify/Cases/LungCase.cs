using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Lung;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>18100 神纹基础快照，以及 18105/18112 熔炉启动与服务器时间刷新闭环回归。</summary>
    public static class LungCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e) { Debug.LogError("CLIVERIFY lung EXCEPTION " + e); return Task.FromResult(3); }
        }

        private static int RunSync()
        {
            LungController ctrl = LungController.Instance;
            bool wasInitialized = ctrl.IsInitialized;
            try { ctrl.Init(); return RunInitialized(ctrl); }
            finally { if (!wasInitialized && ctrl.IsInitialized) ctrl.Dispose(); }
        }

        private static int RunInitialized(LungController ctrl)
        {
            LungModel model = LungModel.Instance;
            var savedAttributes = new List<LungModel.AttributeEntry>(model.Attributes);
            var savedPositions = new List<LungModel.PositionEntry>(model.Positions);
            bool savedHasLungData = model.HasLungData;
            uint savedCombatPower = model.CombatPower;
            FieldInfo[] state = typeof(LungModel).GetFields(BindingFlags.Public | BindingFlags.Instance);
            object[] saved = new object[state.Length];
            for (int i = 0; i < state.Length; i++) saved[i] = state[i].GetValue(model);
            try { model.Reset(); return RunIsolated(ctrl, model); }
            finally
            {
                model.Reset();
                for (int i = 0; i < state.Length; i++) state[i].SetValue(model, saved[i]);
                if (savedHasLungData) model.ReplaceLungData(savedAttributes, savedPositions, savedCombatPower);
            }
        }

        private static int RunIsolated(LungController ctrl, LungModel model)
        {
            MethodInfo on18100 = ctrl.GetType().GetMethod("On18100", F);
            MethodInfo on18105 = ctrl.GetType().GetMethod("On18105", F);
            MethodInfo on18112 = ctrl.GetType().GetMethod("On18112", F);
            FieldInfo intercept = ctrl.GetType().GetField("s_outboundIntercept", SF);
            bool pass = on18100 != null && on18105 != null && on18112 != null && intercept != null;
            void Check(string tag, bool ok) { Debug.Log("CLIVERIFY lung " + tag + " ok=" + ok); if (!ok) pass = false; }
            Check("handlers", pass);
            if (!pass) return 3;

            FieldInfo handlersField = typeof(NetManager).GetField("_handlers", SF);
            var handlers = handlersField?.GetValue(null) as IDictionary;
            Check("registered 18100/18105/18112", handlers != null && handlers.Contains(Proto.LUNG_INFO) && handlers.Contains(Proto.LUNG_STOVE_INFO) && handlers.Contains(Proto.LUNG_STOVE_OPEN_STATE));

            object oldIntercept = intercept.GetValue(null);
            var trace = new List<byte[]>();
            try
            {
                intercept.SetValue(null, new Func<byte[], bool>(frame => { trace.Add(frame); return true; }));
                ctrl.RequestStartup();
                Check("startup exact three empty frames", Frames(trace, Proto.LUNG_INFO, Proto.LUNG_STOVE_INFO, Proto.LUNG_STOVE_OPEN_STATE));

                byte[] first = new CliVerify.Pkt().H(2).C(1).I(101).C(2).I(202)
                    .H(2).C(3).H(4).L(5000000000L).C(5).H(6).L(7000000000L).I(303).Bytes();
                var firstReader = new NetReader(first, 0, first.Length);
                on18100.Invoke(ctrl, new object[] { firstReader });
                Check("18100 fields/read-to-end", firstReader.Remaining == 0 && model.HasLungData && model.CombatPower == 303
                    && model.Attributes.Count == 2 && model.Attributes[0].AttributeId == 1 && model.Attributes[1].AttributeValue == 202
                    && model.Positions.Count == 2 && model.Positions[0].Position == 3 && model.Positions[0].Level == 4
                    && model.Positions[0].NextPower == 5000000000UL && model.Positions[1].NextPower == 7000000000UL);

                byte[] replacement = new CliVerify.Pkt().H(0).H(0).I(404).Bytes();
                var replacementReader = new NetReader(replacement, 0, replacement.Length);
                on18100.Invoke(ctrl, new object[] { replacementReader });
                Check("18100 full replace accepts empty", replacementReader.Remaining == 0 && model.HasLungData && model.CombatPower == 404
                    && model.Attributes.Count == 0 && model.Positions.Count == 0);

                trace.Clear();
                var reader = new NetReader(new CliVerify.Pkt().H(7).I(1700000000).Bytes(), 0, 6);
                on18112.Invoke(ctrl, new object[] { reader });
                Check("18112 apply/read-to-end/one-18105", reader.Remaining == 0 && model.HasOpenSchedule && model.NextCrucibleId == 7 && model.NextStartTime == 1700000000L && Frames(trace, Proto.LUNG_STOVE_INFO));

                trace.Clear();
                EventDispatcher.Emit(GlobalEvent.EVT_SERVER_TIME_REFRESH);
                Check("time refresh one 18112", Frames(trace, Proto.LUNG_STOVE_OPEN_STATE));

                ctrl.Dispose();
                trace.Clear();
                EventDispatcher.Emit(GlobalEvent.EVT_SERVER_TIME_REFRESH);
                Check("dispose off/reset", trace.Count == 0 && !model.HasOpenSchedule && model.NextCrucibleId == 0 && model.NextStartTime == 0
                    && !model.HasLungData && model.CombatPower == 0 && model.Attributes.Count == 0 && model.Positions.Count == 0);
                ctrl.Init(); // restore this ControllerHub singleton for later RenderAll cases.
            }
            finally { intercept.SetValue(null, oldIntercept); }
            Debug.Log("CLIVERIFY lung VERDICT pass=" + pass);
            return pass ? 0 : 3;
        }

        private static bool Frames(IReadOnlyList<byte[]> frames, params int[] ids)
        {
            if (frames.Count != ids.Length) return false;
            for (int i = 0; i < ids.Length; i++)
            {
                byte[] f = frames[i];
                if (f == null || f.Length != 6 || f[0] != 0 || f[1] != 6 || f[2] != 3 || f[3] != 232 || f[4] != (byte)(ids[i] >> 8) || f[5] != (byte)ids[i]) return false;
            }
            return true;
        }
    }
}
