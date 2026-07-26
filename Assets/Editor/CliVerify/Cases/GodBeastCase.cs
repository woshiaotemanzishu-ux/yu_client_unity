using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.GodBeast;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class GodBeastCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;
        private sealed class HandlerState { public bool Exists; public object Value; }

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e) { Debug.LogError("CLIVERIFY godbeast EXCEPTION " + e); return Task.FromResult(3); }
        }

        private static int RunSync()
        {
            GodBeastController controller = GodBeastController.Instance;
            GodBeastModel model = GodBeastModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            byte oldFight = model.FightCount;
            var oldBeasts = new List<GodBeastModel.Beast>(model.Beasts);
            bool oldHasData = model.HasData;
            bool oldHasError = model.HasError;
            uint oldErrorCode = model.LastErrorCode;
            string oldErrorArgs = model.LastErrorArgs;
            FieldInfo interceptor = typeof(GodBeastController).GetField("s_outboundIntercept", SF);
            object oldInterceptor = interceptor?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var savedHandlers = new Dictionary<int, HandlerState>();
            SaveHandler(handlers, savedHandlers, 17300);
            SaveHandler(handlers, savedHandlers, 17301);
            bool pass = false;
            bool restored = false;

            try
            {
                controller.Init();
                model.Reset();
                MethodInfo on17300 = typeof(GodBeastController).GetMethod("On17300", F);
                MethodInfo on17301 = typeof(GodBeastController).GetMethod("On17301", F);
                pass = interceptor != null && handlers != null && on17300 != null && on17301 != null
                    && handlers.Contains(17300) && handlers.Contains(17301)
                    && !handlers.Contains(17302) && !handlers.Contains(17303) && !handlers.Contains(17304)
                    && !handlers.Contains(17305) && !handlers.Contains(17306) && !handlers.Contains(17307)
                    && !handlers.Contains(17308) && !handlers.Contains(17309) && !handlers.Contains(17310)
                    && !handlers.Contains(17311) && !handlers.Contains(17312);

                var frames = new List<byte[]>();
                if (pass)
                {
                    interceptor.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                    controller.RequestStartup();
                    pass &= Frame(frames, Proto.GODBEAST_OVERVIEW);
                    frames.Clear();

                    byte[] snapshotBytes = new CliVerify.Pkt().C(1).H(1).I(2).C(3).I(4).H(0).H(0).Bytes();
                    var snapshotReader = new NetReader(snapshotBytes, 0, snapshotBytes.Length);
                    on17301.Invoke(controller, new object[] { snapshotReader });
                    pass &= snapshotReader.Remaining == 0 && model.HasData && model.FightCount == 1 && model.Beasts.Count == 1
                        && model.Beasts[0].Id == 2 && model.Beasts[0].State == 3 && model.Beasts[0].Score == 4
                        && !model.HasError && frames.Count == 0;

                    pass &= VerifyError(on17300, controller, 0, string.Empty, () => model.HasError && model.LastErrorCode == 0 && model.LastErrorArgs == string.Empty && model.Beasts.Count == 1 && frames.Count == 0);
                    pass &= VerifyError(on17300, controller, 1, "\u6210\u529f", () => model.LastErrorCode == 1 && model.LastErrorArgs == "\u6210\u529f");
                    pass &= VerifyError(on17300, controller, uint.MaxValue, "\u6700\u7ec8", () => model.LastErrorCode == uint.MaxValue && model.LastErrorArgs == "\u6700\u7ec8");

                    byte[] emptySnapshot = new CliVerify.Pkt().C(9).H(0).Bytes();
                    var emptyReader = new NetReader(emptySnapshot, 0, emptySnapshot.Length);
                    on17301.Invoke(controller, new object[] { emptyReader });
                    pass &= emptyReader.Remaining == 0 && model.HasData && model.FightCount == 9 && model.Beasts.Count == 0
                        && model.HasError && model.LastErrorCode == uint.MaxValue && model.LastErrorArgs == "\u6700\u7ec8" && frames.Count == 0;

                    controller.Dispose();
                    pass &= !controller.IsInitialized && !model.HasData && model.FightCount == 0 && model.Beasts.Count == 0
                        && !model.HasError && model.LastErrorCode == 0 && model.LastErrorArgs == null
                        && !handlers.Contains(17300) && !handlers.Contains(17301);
                }
                Debug.Log("CLIVERIFY godbeast VERDICT pass=" + pass);
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                if (oldHasData) model.ReplaceData(oldFight, oldBeasts);
                RestoreModelProperty(model, "HasError", oldHasError);
                RestoreModelProperty(model, "LastErrorCode", oldErrorCode);
                RestoreModelProperty(model, "LastErrorArgs", oldErrorArgs);
                if (wasInitialized) controller.Init();
                RestoreHandler(handlers, savedHandlers[17300], 17300);
                RestoreHandler(handlers, savedHandlers[17301], 17301);
                if (interceptor != null) interceptor.SetValue(null, oldInterceptor);
                restored = controller.IsInitialized == wasInitialized
                    && model.FightCount == oldFight && model.HasData == oldHasData && BeastsMatch(model.Beasts, oldBeasts)
                    && model.HasError == oldHasError && model.LastErrorCode == oldErrorCode && model.LastErrorArgs == oldErrorArgs
                    && HandlerMatches(handlers, savedHandlers[17300], 17300) && HandlerMatches(handlers, savedHandlers[17301], 17301)
                    && (interceptor == null || ReferenceEquals(interceptor.GetValue(null), oldInterceptor));
                Debug.Log("CLIVERIFY godbeast restored=" + restored);
            }
            return pass && restored ? 0 : 3;
        }

        private static bool VerifyError(MethodInfo handler, GodBeastController controller, uint code, string args, Func<bool> check)
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

        private static void RestoreModelProperty(GodBeastModel model, string name, object value)
        {
            typeof(GodBeastModel).GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.SetValue(model, value);
        }

        private static bool BeastsMatch(IReadOnlyList<GodBeastModel.Beast> actual, IReadOnlyList<GodBeastModel.Beast> expected)
        {
            if (actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++)
                if (!ReferenceEquals(actual[i], expected[i])) return false;
            return true;
        }

        private static bool Frame(IReadOnlyList<byte[]> frames, int id)
        {
            if (frames.Count != 1 || frames[0] == null) return false;
            byte[] frame = frames[0];
            return frame.Length == 6 && frame[0] == 0 && frame[1] == 6 && frame[2] == 3 && frame[3] == 232
                && frame[4] == (byte)(id >> 8) && frame[5] == (byte)id;
        }
    }
}
