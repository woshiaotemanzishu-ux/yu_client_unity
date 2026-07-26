using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.TopPk;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class TopPkCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;

        private sealed class HandlerState
        {
            public bool Exists;
            public object Value;
        }

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e) { Debug.LogError("CLIVERIFY toppk EXCEPTION " + e); return Task.FromResult(3); }
        }

        private static int RunSync()
        {
            TopPkController controller = TopPkController.Instance;
            TopPkModel model = TopPkModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            bool oldHasError = model.HasError;
            uint oldErrorCode = model.LastErrorCode;
            string oldErrorArgs = model.LastErrorArgs;
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var savedHandlers = new Dictionary<int, HandlerState>();
            for (int id = 28100; id <= 28117; id++) SaveHandler(handlers, savedHandlers, id);
            bool pass = false;
            bool restored = false;

            try
            {
                controller.Init();
                model.Reset();
                MethodInfo on28100 = typeof(TopPkController).GetMethod("On28100", F);
                pass = handlers != null && on28100 != null && handlers.Contains(28100);
                for (int id = 28101; id <= 28117; id++) pass &= !handlers.Contains(id);
                pass &= NoPublicRequestOrSend();

                if (pass)
                {
                    pass &= VerifyError(on28100, controller, 0, string.Empty,
                        () => model.HasError && model.LastErrorCode == 0 && model.LastErrorArgs == string.Empty);
                    pass &= VerifyError(on28100, controller, 1, "\u6210\u529f",
                        () => model.HasError && model.LastErrorCode == 1 && model.LastErrorArgs == "\u6210\u529f");
                    pass &= VerifyError(on28100, controller, uint.MaxValue, "\u6700\u7ec8",
                        () => model.HasError && model.LastErrorCode == uint.MaxValue && model.LastErrorArgs == "\u6700\u7ec8");

                    model.Reset();
                    pass &= !model.HasError && model.LastErrorCode == 0 && model.LastErrorArgs == null;
                    controller.Dispose();
                    pass &= !controller.IsInitialized && !model.HasError && model.LastErrorCode == 0
                        && model.LastErrorArgs == null && !handlers.Contains(28100);
                }
                Debug.Log("CLIVERIFY toppk VERDICT pass=" + pass);
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                if (oldHasError) model.SetError(oldErrorCode, oldErrorArgs);
                if (wasInitialized) controller.Init();
                for (int id = 28100; id <= 28117; id++) RestoreHandler(handlers, savedHandlers[id], id);

                restored = controller.IsInitialized == wasInitialized
                    && model.HasError == oldHasError && model.LastErrorCode == oldErrorCode && model.LastErrorArgs == oldErrorArgs;
                for (int id = 28100; id <= 28117; id++) restored &= HandlerMatches(handlers, savedHandlers[id], id);
                Debug.Log("CLIVERIFY toppk restored=" + restored);
            }
            return pass && restored ? 0 : 3;
        }

        private static bool VerifyError(MethodInfo handler, TopPkController controller, uint code, string args, Func<bool> check)
        {
            byte[] bytes = new CliVerify.Pkt().I(code).S(args).Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0 && check();
        }

        private static bool NoPublicRequestOrSend()
        {
            foreach (MethodInfo method in typeof(TopPkController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (method.Name.IndexOf("Request", StringComparison.OrdinalIgnoreCase) >= 0
                    || method.Name.IndexOf("Send", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            }
            return true;
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
            return handlers != null && handlers.Contains(id) == saved.Exists
                && (!saved.Exists || ReferenceEquals(handlers[id], saved.Value));
        }
    }
}
