using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Pray;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class PrayCase
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
            try
            {
                return Task.FromResult(RunSync());
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY pray EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            PrayController controller = PrayController.Instance;
            PrayModel model = PrayModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            bool oldHasError = model.HasError;
            uint oldErrorCode = model.LastErrorCode;
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var savedHandlers = new Dictionary<int, HandlerState>();
            for (int id = 41500; id <= 41502; id++)
            {
                SaveHandler(handlers, savedHandlers, id);
            }

            bool pass = false;
            bool restored = false;
            try
            {
                if (controller.IsInitialized)
                {
                    controller.Dispose();
                }

                if (handlers != null)
                {
                    for (int id = 41500; id <= 41502; id++)
                    {
                        handlers.Remove(id);
                    }
                }

                controller.Init();
                model.Reset();
                MethodInfo on41500 = typeof(PrayController).GetMethod("On41500", F);
                pass = handlers != null && on41500 != null
                    && handlers.Contains(41500) && NoPublicRequestOrSend();
                for (int id = 41501; id <= 41502; id++)
                {
                    pass &= !handlers.Contains(id);
                }

                object firstHandler = handlers != null && handlers.Contains(41500) ? handlers[41500] : null;
                controller.Init();
                pass &= controller.IsInitialized && firstHandler != null
                    && ReferenceEquals(handlers[41500], firstHandler);

                if (pass)
                {
                    pass &= Feed(on41500, controller, 0)
                        && model.HasError && model.LastErrorCode == 0;
                    pass &= Feed(on41500, controller, 1)
                        && model.HasError && model.LastErrorCode == 1;
                    pass &= Feed(on41500, controller, uint.MaxValue)
                        && model.HasError && model.LastErrorCode == uint.MaxValue;
                    pass &= Feed(on41500, controller, 7)
                        && model.HasError && model.LastErrorCode == 7;

                    controller.Dispose();
                    pass &= !controller.IsInitialized && !model.HasError && model.LastErrorCode == 0
                        && !handlers.Contains(41500);
                }

                Debug.Log("CLIVERIFY pray VERDICT pass=" + pass);
            }
            finally
            {
                if (controller.IsInitialized)
                {
                    controller.Dispose();
                }

                RestoreModelProperty(model, "HasError", oldHasError);
                RestoreModelProperty(model, "LastErrorCode", oldErrorCode);
                if (wasInitialized)
                {
                    controller.Init();
                }

                for (int id = 41500; id <= 41502; id++)
                {
                    RestoreHandler(handlers, savedHandlers[id], id);
                }

                restored = ReferenceEquals(PrayController.Instance, controller)
                    && ReferenceEquals(PrayModel.Instance, model)
                    && controller.IsInitialized == wasInitialized
                    && model.HasError == oldHasError && model.LastErrorCode == oldErrorCode;
                for (int id = 41500; id <= 41502; id++)
                {
                    restored &= HandlerMatches(handlers, savedHandlers[id], id);
                }

                Debug.Log("CLIVERIFY pray restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static bool Feed(MethodInfo handler, PrayController controller, uint errorCode)
        {
            byte[] bytes = new CliVerify.Pkt().I(errorCode).Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool NoPublicRequestOrSend()
        {
            foreach (MethodInfo method in typeof(PrayController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (method.Name.IndexOf("Request", StringComparison.OrdinalIgnoreCase) >= 0
                    || method.Name.IndexOf("Send", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static void SaveHandler(IDictionary handlers, IDictionary<int, HandlerState> savedHandlers, int id)
        {
            bool exists = handlers != null && handlers.Contains(id);
            savedHandlers[id] = new HandlerState { Exists = exists, Value = exists ? handlers[id] : null };
        }

        private static void RestoreHandler(IDictionary handlers, HandlerState savedHandler, int id)
        {
            if (handlers == null)
            {
                return;
            }

            if (savedHandler.Exists)
            {
                handlers[id] = savedHandler.Value;
            }
            else
            {
                handlers.Remove(id);
            }
        }

        private static bool HandlerMatches(IDictionary handlers, HandlerState savedHandler, int id)
        {
            return handlers != null && handlers.Contains(id) == savedHandler.Exists
                && (!savedHandler.Exists || ReferenceEquals(handlers[id], savedHandler.Value));
        }

        private static void RestoreModelProperty(PrayModel model, string propertyName, object value)
        {
            typeof(PrayModel).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.SetValue(model, value);
        }
    }
}
