using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.GuildFight;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class GuildFightEnterCase
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
                Debug.LogError("CLIVERIFY guildfightenter EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            GuildFightController controller = GuildFightController.Instance;
            GuildFightModel model = GuildFightModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            bool oldHasEnterResult = model.HasEnterResult;
            uint oldEnterResultCode = model.EnterResultCode;
            byte oldEnterResultType = model.EnterResultType;
            FieldInfo interceptor = typeof(GuildFightController).GetField("s_enterOutboundIntercept", SF);
            object oldInterceptor = interceptor?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var savedHandlers = new Dictionary<int, HandlerState>();
            for (int id = 50602; id <= 50604; id++)
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
                    for (int id = 50602; id <= 50604; id++)
                    {
                        handlers.Remove(id);
                    }
                }

                controller.Init();
                model.Reset();
                MethodInfo on50603 = typeof(GuildFightController).GetMethod("On50603", F);
                pass = interceptor != null && handlers != null && on50603 != null
                    && !handlers.Contains(50602) && handlers.Contains(50603) && !handlers.Contains(50604)
                    && OnlyRequestEnterIsPublic();

                object firstHandler = handlers != null && handlers.Contains(50603) ? handlers[50603] : null;
                controller.Init();
                pass &= controller.IsInitialized && firstHandler != null
                    && ReferenceEquals(handlers[50603], firstHandler);

                var frames = new List<byte[]>();
                if (pass)
                {
                    interceptor.SetValue(null, new Func<byte[], bool>(frame =>
                    {
                        frames.Add(frame);
                        return true;
                    }));
                    controller.RequestEnter();
                    pass &= OneEnterFrame(frames);
                    frames.Clear();

                    pass &= Feed(on50603, controller, 0, 0)
                        && model.HasEnterResult && model.EnterResultCode == 0 && model.EnterResultType == 0
                        && frames.Count == 0;
                    pass &= Feed(on50603, controller, 1, 1)
                        && model.HasEnterResult && model.EnterResultCode == 1 && model.EnterResultType == 1
                        && frames.Count == 0;
                    pass &= Feed(on50603, controller, uint.MaxValue, byte.MaxValue)
                        && model.HasEnterResult && model.EnterResultCode == uint.MaxValue && model.EnterResultType == byte.MaxValue
                        && frames.Count == 0;
                    pass &= Feed(on50603, controller, 7, 2)
                        && model.HasEnterResult && model.EnterResultCode == 7 && model.EnterResultType == 2
                        && frames.Count == 0;

                    controller.Dispose();
                    pass &= !controller.IsInitialized && !model.HasEnterResult
                        && model.EnterResultCode == 0 && model.EnterResultType == 0
                        && !handlers.Contains(50602) && !handlers.Contains(50603) && !handlers.Contains(50604)
                        && frames.Count == 0;
                }

                Debug.Log("CLIVERIFY guildfightenter VERDICT pass=" + pass);
            }
            finally
            {
                if (controller.IsInitialized)
                {
                    controller.Dispose();
                }

                RestoreModelProperty(model, "HasEnterResult", oldHasEnterResult);
                RestoreModelProperty(model, "EnterResultCode", oldEnterResultCode);
                RestoreModelProperty(model, "EnterResultType", oldEnterResultType);
                if (wasInitialized)
                {
                    controller.Init();
                }

                for (int id = 50602; id <= 50604; id++)
                {
                    RestoreHandler(handlers, savedHandlers[id], id);
                }

                if (interceptor != null)
                {
                    interceptor.SetValue(null, oldInterceptor);
                }

                restored = ReferenceEquals(GuildFightController.Instance, controller)
                    && ReferenceEquals(GuildFightModel.Instance, model)
                    && controller.IsInitialized == wasInitialized
                    && model.HasEnterResult == oldHasEnterResult
                    && model.EnterResultCode == oldEnterResultCode
                    && model.EnterResultType == oldEnterResultType
                    && (interceptor == null || ReferenceEquals(interceptor.GetValue(null), oldInterceptor));
                for (int id = 50602; id <= 50604; id++)
                {
                    restored &= HandlerMatches(handlers, savedHandlers[id], id);
                }

                Debug.Log("CLIVERIFY guildfightenter restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static bool Feed(MethodInfo handler, GuildFightController controller, uint errorCode, byte type)
        {
            byte[] bytes = new CliVerify.Pkt().I(errorCode).C(type).Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool OneEnterFrame(IReadOnlyList<byte[]> frames)
        {
            if (frames.Count != 1) return false;
            byte[] frame = frames[0];
            int protocolId = Proto.GUILD_FIGHT_ENTER;
            return frame != null && frame.Length == 7
                && frame[0] == 0 && frame[1] == 7
                && frame[2] == 3 && frame[3] == 232
                && frame[4] == (byte)(protocolId >> 8) && frame[5] == (byte)(protocolId & 0xFF)
                && frame[6] == 1;
        }

        private static bool OnlyRequestEnterIsPublic()
        {
            int requestOrSendCount = 0;
            foreach (MethodInfo method in typeof(GuildFightController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (method.Name.IndexOf("Request", StringComparison.OrdinalIgnoreCase) < 0
                    && method.Name.IndexOf("Send", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                requestOrSendCount++;
                if (method.Name != "RequestEnter" || method.ReturnType != typeof(void) || method.GetParameters().Length != 0)
                {
                    return false;
                }
            }

            return requestOrSendCount == 1;
        }

        private static void SaveHandler(IDictionary handlers, IDictionary<int, HandlerState> savedHandlers, int id)
        {
            bool exists = handlers != null && handlers.Contains(id);
            savedHandlers[id] = new HandlerState { Exists = exists, Value = exists ? handlers[id] : null };
        }

        private static void RestoreHandler(IDictionary handlers, HandlerState savedHandler, int id)
        {
            if (handlers == null) return;
            if (savedHandler.Exists) handlers[id] = savedHandler.Value;
            else handlers.Remove(id);
        }

        private static bool HandlerMatches(IDictionary handlers, HandlerState savedHandler, int id)
        {
            return handlers != null && handlers.Contains(id) == savedHandler.Exists
                && (!savedHandler.Exists || ReferenceEquals(handlers[id], savedHandler.Value));
        }

        private static void RestoreModelProperty(GuildFightModel model, string propertyName, object value)
        {
            typeof(GuildFightModel).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.SetValue(model, value);
        }
    }
}
