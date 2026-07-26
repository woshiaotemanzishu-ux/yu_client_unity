using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Guard;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class GuardCase
    {
        private const BindingFlags InstanceFlags = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags StaticFlags = BindingFlags.NonPublic | BindingFlags.Static;

        private sealed class HandlerState
        {
            public bool Exists;
            public object Value;
        }

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY guard EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            GuardController controller = GuardController.Instance;
            GuardModel model = GuardModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            bool oldHasData = model.HasData;
            var oldCircles = new List<GuardModel.Circle>(model.Circles);
            bool oldHasError = model.HasError;
            uint oldLastErrorCode = model.LastErrorCode;
            bool oldHasLoginCheckResult = model.HasLoginCheckResult;
            uint oldLoginCheckResultCode = model.LoginCheckResultCode;
            FieldInfo interceptField = typeof(GuardController).GetField("s_outboundIntercept", StaticFlags);
            object oldIntercept = interceptField?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", StaticFlags)?.GetValue(null) as IDictionary;
            var oldHandlers = new Dictionary<int, HandlerState>();
            SaveHandler(handlers, oldHandlers, 21600);
            SaveHandler(handlers, oldHandlers, 21601);
            SaveHandler(handlers, oldHandlers, 21606);
            bool pass = false;
            bool restored = false;

            try
            {
                controller.Init();
                model.Reset();

                MethodInfo errorHandler = typeof(GuardController).GetMethod("On21600", InstanceFlags);
                MethodInfo infoHandler = typeof(GuardController).GetMethod("On21601", InstanceFlags);
                MethodInfo loginCheckHandler = typeof(GuardController).GetMethod("On21606", InstanceFlags);
                pass = interceptField != null
                    && errorHandler != null && infoHandler != null && loginCheckHandler != null
                    && handlers != null
                    && handlers.Contains(21600) && handlers.Contains(21601) && handlers.Contains(21606)
                    && !handlers.Contains(21602) && !handlers.Contains(21603)
                    && !handlers.Contains(21604) && !handlers.Contains(21605);
                if (!pass) throw new InvalidOperationException("Guard handlers/interceptor precondition failed.");

                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                controller.RequestInfo();
                pass &= IsRequestFrame(frames);
                frames.Clear();

                byte[] infoBytes = new CliVerify.Pkt().H(1).C(2).C(3).I(4).C(5).C(6).Bytes();
                var infoReader = new NetReader(infoBytes, 0, infoBytes.Length);
                infoHandler.Invoke(controller, new object[] { infoReader });
                pass &= infoReader.Remaining == 0 && model.HasData && model.Circles.Count == 1
                    && model.Circles[0].Status == 2 && model.Circles[0].Level == 3
                    && model.Circles[0].EndTime == 4 && model.Circles[0].Show == 5 && model.Circles[0].FreeFlag == 6
                    && !model.HasError && !model.HasLoginCheckResult && frames.Count == 0;

                pass &= VerifyU32(errorHandler, controller, 0, () => model.HasError && model.LastErrorCode == 0
                    && !model.HasLoginCheckResult && model.Circles.Count == 1 && frames.Count == 0);
                pass &= VerifyU32(errorHandler, controller, 1, () => model.LastErrorCode == 1);
                pass &= VerifyU32(errorHandler, controller, uint.MaxValue, () => model.LastErrorCode == uint.MaxValue);

                pass &= VerifyU32(loginCheckHandler, controller, 0, () => model.HasLoginCheckResult && model.LoginCheckResultCode == 0
                    && model.HasError && model.LastErrorCode == uint.MaxValue && model.Circles.Count == 1 && frames.Count == 0);
                pass &= VerifyU32(loginCheckHandler, controller, 1, () => model.LoginCheckResultCode == 1);
                pass &= VerifyU32(loginCheckHandler, controller, uint.MaxValue, () => model.LoginCheckResultCode == uint.MaxValue);
                pass &= VerifyU32(errorHandler, controller, 7, () => model.HasError && model.LastErrorCode == 7
                    && model.HasLoginCheckResult && model.LoginCheckResultCode == uint.MaxValue && model.Circles.Count == 1 && frames.Count == 0);

                var emptyReader = new NetReader(new CliVerify.Pkt().H(0).Bytes(), 0, 2);
                infoHandler.Invoke(controller, new object[] { emptyReader });
                pass &= emptyReader.Remaining == 0 && model.HasData && model.Circles.Count == 0
                    && model.HasError && model.LastErrorCode == 7
                    && model.HasLoginCheckResult && model.LoginCheckResultCode == uint.MaxValue && frames.Count == 0;

                controller.Dispose();
                pass &= !model.HasData && model.Circles.Count == 0
                    && !model.HasError && model.LastErrorCode == 0
                    && !model.HasLoginCheckResult && model.LoginCheckResultCode == 0;
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                if (oldHasData) model.Replace(oldCircles);
                RestoreModelProperty(model, "HasError", oldHasError);
                RestoreModelProperty(model, "LastErrorCode", oldLastErrorCode);
                RestoreModelProperty(model, "HasLoginCheckResult", oldHasLoginCheckResult);
                RestoreModelProperty(model, "LoginCheckResultCode", oldLoginCheckResultCode);
                if (wasInitialized) controller.Init();
                RestoreHandler(handlers, oldHandlers[21600], 21600);
                RestoreHandler(handlers, oldHandlers[21601], 21601);
                RestoreHandler(handlers, oldHandlers[21606], 21606);
                if (interceptField != null) interceptField.SetValue(null, oldIntercept);
                restored = controller.IsInitialized == wasInitialized
                    && model.HasData == oldHasData && CirclesMatch(model.Circles, oldCircles)
                    && model.HasError == oldHasError && model.LastErrorCode == oldLastErrorCode
                    && model.HasLoginCheckResult == oldHasLoginCheckResult && model.LoginCheckResultCode == oldLoginCheckResultCode
                    && HandlerMatches(handlers, oldHandlers[21600], 21600)
                    && HandlerMatches(handlers, oldHandlers[21601], 21601)
                    && HandlerMatches(handlers, oldHandlers[21606], 21606)
                    && (interceptField == null || ReferenceEquals(interceptField.GetValue(null), oldIntercept));
                Debug.Log("CLIVERIFY guard restored=" + restored + " VERDICT pass=" + pass);
            }
            return pass && restored ? 0 : 3;
        }

        private static bool VerifyU32(MethodInfo handler, GuardController controller, uint value, Func<bool> check)
        {
            byte[] bytes = new CliVerify.Pkt().I(value).Bytes();
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

        private static void RestoreModelProperty(GuardModel model, string name, object value)
        {
            typeof(GuardModel).GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.SetValue(model, value);
        }

        private static bool CirclesMatch(IReadOnlyList<GuardModel.Circle> actual, IReadOnlyList<GuardModel.Circle> expected)
        {
            if (actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++)
                if (!ReferenceEquals(actual[i], expected[i])) return false;
            return true;
        }

        private static bool IsRequestFrame(IReadOnlyList<byte[]> frames)
        {
            return frames.Count == 1
                && frames[0] != null
                && frames[0].Length == 6
                && frames[0][0] == 0
                && frames[0][1] == 6
                && frames[0][2] == 3
                && frames[0][3] == 232
                && frames[0][4] == (byte)(21601 >> 8)
                && frames[0][5] == (byte)(21601 & 0xFF);
        }
    }
}
