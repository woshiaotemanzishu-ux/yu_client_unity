using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.HolySeal;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class HolySealCase
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
                Debug.LogError("CLIVERIFY holyseal EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            HolySealController controller = HolySealController.Instance;
            HolySealModel model = HolySealModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            bool oldHasError = model.HasError;
            uint oldErrorCode = model.LastErrorCode;
            string oldErrorArgs = model.LastErrorArgs;
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var savedHandlers = new Dictionary<int, HandlerState>();
            for (int id = 65400; id <= 65409; id++)
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
                    for (int id = 65400; id <= 65409; id++)
                    {
                        handlers.Remove(id);
                    }
                }

                controller.Init();
                model.Reset();
                MethodInfo on65400 = typeof(HolySealController).GetMethod("On65400", F);
                pass = handlers != null && on65400 != null
                    && handlers.Contains(65400) && NoPublicRequestOrSend();
                for (int id = 65401; id <= 65409; id++)
                {
                    pass &= !handlers.Contains(id);
                }

                object firstHandler = handlers != null && handlers.Contains(65400) ? handlers[65400] : null;
                controller.Init();
                pass &= controller.IsInitialized && firstHandler != null
                    && ReferenceEquals(handlers[65400], firstHandler);

                if (pass)
                {
                    pass &= Feed(on65400, controller, 0, string.Empty)
                        && model.HasError && model.LastErrorCode == 0 && model.LastErrorArgs == string.Empty;
                    pass &= Feed(on65400, controller, 1012, "中文")
                        && model.HasError && model.LastErrorCode == 1012 && model.LastErrorArgs == "中文";
                    pass &= Feed(on65400, controller, uint.MaxValue, "最大值")
                        && model.HasError && model.LastErrorCode == uint.MaxValue && model.LastErrorArgs == "最大值";
                    pass &= Feed(on65400, controller, 7, "后包覆盖")
                        && model.HasError && model.LastErrorCode == 7 && model.LastErrorArgs == "后包覆盖";

                    controller.Dispose();
                    pass &= !controller.IsInitialized && !model.HasError && model.LastErrorCode == 0
                        && model.LastErrorArgs == null && !handlers.Contains(65400);
                }

                Debug.Log("CLIVERIFY holyseal VERDICT pass=" + pass);
            }
            finally
            {
                if (controller.IsInitialized)
                {
                    controller.Dispose();
                }

                RestoreModelProperty(model, "HasError", oldHasError);
                RestoreModelProperty(model, "LastErrorCode", oldErrorCode);
                RestoreModelProperty(model, "LastErrorArgs", oldErrorArgs);
                if (wasInitialized)
                {
                    controller.Init();
                }

                for (int id = 65400; id <= 65409; id++)
                {
                    RestoreHandler(handlers, savedHandlers[id], id);
                }

                restored = ReferenceEquals(HolySealController.Instance, controller)
                    && ReferenceEquals(HolySealModel.Instance, model)
                    && controller.IsInitialized == wasInitialized
                    && model.HasError == oldHasError && model.LastErrorCode == oldErrorCode
                    && model.LastErrorArgs == oldErrorArgs;
                for (int id = 65400; id <= 65409; id++)
                {
                    restored &= HandlerMatches(handlers, savedHandlers[id], id);
                }

                Debug.Log("CLIVERIFY holyseal restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static bool Feed(MethodInfo handler, HolySealController controller, uint errorCode, string errorArgs)
        {
            byte[] bytes = new CliVerify.Pkt().I(errorCode).S(errorArgs).Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool NoPublicRequestOrSend()
        {
            foreach (MethodInfo method in typeof(HolySealController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
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

        private static void RestoreModelProperty(HolySealModel model, string propertyName, object value)
        {
            typeof(HolySealModel).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.SetValue(model, value);
        }
    }
}
