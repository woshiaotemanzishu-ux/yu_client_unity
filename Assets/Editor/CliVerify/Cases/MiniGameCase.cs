using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.MiniGame;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class MiniGameCase
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
                Debug.LogError("CLIVERIFY minigame EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            MiniGameController controller = MiniGameController.Instance;
            MiniGameModel model = MiniGameModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            bool oldHasError = model.HasError;
            uint oldErrorCode = model.LastErrorCode;
            string oldErrorMessage = model.LastErrorMessage;
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var savedHandlers = new Dictionary<int, HandlerState>();
            for (int id = 39900; id <= 39931; id++)
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
                    for (int id = 39900; id <= 39931; id++)
                    {
                        handlers.Remove(id);
                    }
                }

                controller.Init();
                model.Reset();
                MethodInfo on39900 = typeof(MiniGameController).GetMethod("On39900", F);
                pass = handlers != null && on39900 != null
                    && handlers.Contains(39900) && NoPublicRequestOrSend();
                for (int id = 39901; id <= 39931; id++)
                {
                    pass &= !handlers.Contains(id);
                }

                object firstHandler = handlers != null && handlers.Contains(39900) ? handlers[39900] : null;
                controller.Init();
                pass &= controller.IsInitialized && firstHandler != null
                    && ReferenceEquals(handlers[39900], firstHandler);

                if (pass)
                {
                    pass &= Feed(on39900, controller, 0, string.Empty)
                        && model.HasError && model.LastErrorCode == 0 && model.LastErrorMessage == string.Empty;
                    pass &= Feed(on39900, controller, 1012, "中文")
                        && model.HasError && model.LastErrorCode == 1012 && model.LastErrorMessage == "中文";
                    pass &= Feed(on39900, controller, uint.MaxValue, "最大值")
                        && model.HasError && model.LastErrorCode == uint.MaxValue && model.LastErrorMessage == "最大值";
                    pass &= Feed(on39900, controller, 7, "后包覆盖")
                        && model.HasError && model.LastErrorCode == 7 && model.LastErrorMessage == "后包覆盖";

                    controller.Dispose();
                    pass &= !controller.IsInitialized && !model.HasError && model.LastErrorCode == 0
                        && model.LastErrorMessage == null && !handlers.Contains(39900);
                }

                Debug.Log("CLIVERIFY minigame VERDICT pass=" + pass);
            }
            finally
            {
                if (controller.IsInitialized)
                {
                    controller.Dispose();
                }

                RestoreModelProperty(model, "HasError", oldHasError);
                RestoreModelProperty(model, "LastErrorCode", oldErrorCode);
                RestoreModelProperty(model, "LastErrorMessage", oldErrorMessage);
                if (wasInitialized)
                {
                    controller.Init();
                }

                for (int id = 39900; id <= 39931; id++)
                {
                    RestoreHandler(handlers, savedHandlers[id], id);
                }

                restored = ReferenceEquals(MiniGameController.Instance, controller)
                    && ReferenceEquals(MiniGameModel.Instance, model)
                    && controller.IsInitialized == wasInitialized
                    && model.HasError == oldHasError && model.LastErrorCode == oldErrorCode
                    && model.LastErrorMessage == oldErrorMessage;
                for (int id = 39900; id <= 39931; id++)
                {
                    restored &= HandlerMatches(handlers, savedHandlers[id], id);
                }

                Debug.Log("CLIVERIFY minigame restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static bool Feed(MethodInfo handler, MiniGameController controller, uint errorCode, string errorMessage)
        {
            byte[] bytes = new CliVerify.Pkt().I(errorCode).S(errorMessage).Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool NoPublicRequestOrSend()
        {
            foreach (MethodInfo method in typeof(MiniGameController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
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

        private static void RestoreModelProperty(MiniGameModel model, string propertyName, object value)
        {
            typeof(MiniGameModel).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.SetValue(model, value);
        }
    }
}
