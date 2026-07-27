using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.HolyTerritory;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class HolyTerritoryCase
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
            catch (Exception e) { Debug.LogError("CLIVERIFY holyterritory EXCEPTION " + e); return Task.FromResult(3); }
        }

        private static int RunSync()
        {
            HolyTerritoryController controller = HolyTerritoryController.Instance;
            HolyTerritoryModel model = HolyTerritoryModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            bool oldHasError = model.HasError;
            uint oldErrorCode = model.LastErrorCode;
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var savedHandlers = new Dictionary<int, HandlerState>();
            for (int id = 28300; id <= 28319; id++) SaveHandler(handlers, savedHandlers, id);
            bool pass = false;
            bool restored = false;

            try
            {
                controller.Init();
                model.Reset();
                MethodInfo on28300 = typeof(HolyTerritoryController).GetMethod("On28300", F);
                pass = handlers != null && on28300 != null && handlers.Contains(28300);
                for (int id = 28301; id <= 28319; id++) pass &= !handlers.Contains(id);
                pass &= NoPublicRequestOrSend();

                if (pass)
                {
                    pass &= VerifyError(on28300, controller, 0,
                        () => model.HasError && model.LastErrorCode == 0);
                    pass &= VerifyError(on28300, controller, 1012,
                        () => model.HasError && model.LastErrorCode == 1012);
                    pass &= VerifyError(on28300, controller, uint.MaxValue,
                        () => model.HasError && model.LastErrorCode == uint.MaxValue);

                    model.Reset();
                    pass &= !model.HasError && model.LastErrorCode == 0;
                    pass &= VerifyError(on28300, controller, 1012,
                        () => model.HasError && model.LastErrorCode == 1012);

                    controller.Dispose();
                    pass &= !controller.IsInitialized && !model.HasError && model.LastErrorCode == 0
                        && !handlers.Contains(28300);
                }
                Debug.Log("CLIVERIFY holyterritory VERDICT pass=" + pass);
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                if (oldHasError) model.SetError(oldErrorCode);
                if (wasInitialized) controller.Init();
                for (int id = 28300; id <= 28319; id++) RestoreHandler(handlers, savedHandlers[id], id);

                restored = controller.IsInitialized == wasInitialized
                    && model.HasError == oldHasError && model.LastErrorCode == oldErrorCode;
                for (int id = 28300; id <= 28319; id++) restored &= HandlerMatches(handlers, savedHandlers[id], id);
                Debug.Log("CLIVERIFY holyterritory restored=" + restored);
            }
            return pass && restored ? 0 : 3;
        }

        private static bool VerifyError(MethodInfo handler, HolyTerritoryController controller, uint code, Func<bool> check)
        {
            byte[] bytes = new CliVerify.Pkt().I(code).Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0 && check();
        }

        private static bool NoPublicRequestOrSend()
        {
            foreach (MethodInfo method in typeof(HolyTerritoryController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
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
