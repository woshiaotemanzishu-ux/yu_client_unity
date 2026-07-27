using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Kf1vn;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class Kf1vnExitCase
    {
        private const BindingFlags IF = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags PF = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
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
                Debug.LogError("CLIVERIFY kf1vnexit EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            Kf1vnController controller = Kf1vnController.Instance;
            Kf1vnModel model = Kf1vnModel.Instance;
            bool oldInitialized = controller.IsInitialized;
            int oldStage = model.Stage;
            int oldTurn = model.Turn;
            long oldEdtime = model.Edtime;
            int oldSubStage = model.SubStage;
            long oldSubEdtime = model.SubEdtime;
            bool oldHasStageInfo = model.HasStageInfo;

            FieldInfo lastLevelField = typeof(Kf1vnController).GetField("_lastLevel", IF);
            object oldLastLevel = lastLevelField?.GetValue(controller);
            FieldInfo interceptor = typeof(Kf1vnController).GetField("s_exitOutboundIntercept", SF);
            object oldInterceptor = interceptor?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            int[] handlerIds = { 62101, 62103, 62107, 62132 };
            var savedHandlers = new Dictionary<int, HandlerState>();
            foreach (int id in handlerIds)
            {
                SaveHandler(handlers, savedHandlers, id);
            }

            bool pass = false;
            bool restored = false;
            try
            {
                MethodInfo requestExit = typeof(Kf1vnController).GetMethod("RequestExit", PF);
                MethodInfo requestLeave = typeof(Kf1vnController).GetMethod("RequestLeave", PF);
                MethodInfo on62107 = typeof(Kf1vnController).GetMethod("On62107", IF);
                pass = interceptor != null && lastLevelField != null && handlers != null
                    && requestExit != null && requestExit.ReturnType == typeof(void) && requestExit.GetParameters().Length == 0
                    && requestLeave == null && on62107 == null && !handlers.Contains(62107);

                var frames = new List<byte[]>();
                if (pass)
                {
                    interceptor.SetValue(null, new Func<byte[], bool>(frame =>
                    {
                        frames.Add(frame);
                        return true;
                    }));
                    controller.RequestExit();
                    pass &= OneEmptyFrame(frames, Proto.KF1VN_EXIT);
                    pass &= AmbientMatches(
                        controller, model, oldInitialized,
                        oldStage, oldTurn, oldEdtime, oldSubStage, oldSubEdtime, oldHasStageInfo,
                        lastLevelField, oldLastLevel, handlers, savedHandlers, handlerIds);
                }

                Debug.Log("CLIVERIFY kf1vnexit VERDICT pass=" + pass);
            }
            finally
            {
                model.Stage = oldStage;
                model.Turn = oldTurn;
                model.Edtime = oldEdtime;
                model.SubStage = oldSubStage;
                model.SubEdtime = oldSubEdtime;
                model.HasStageInfo = oldHasStageInfo;
                if (lastLevelField != null)
                {
                    lastLevelField.SetValue(controller, oldLastLevel);
                }
                foreach (int id in handlerIds)
                {
                    RestoreHandler(handlers, savedHandlers[id], id);
                }
                if (interceptor != null)
                {
                    interceptor.SetValue(null, oldInterceptor);
                }

                restored = ReferenceEquals(Kf1vnController.Instance, controller)
                    && ReferenceEquals(Kf1vnModel.Instance, model)
                    && AmbientMatches(
                        controller, model, oldInitialized,
                        oldStage, oldTurn, oldEdtime, oldSubStage, oldSubEdtime, oldHasStageInfo,
                        lastLevelField, oldLastLevel, handlers, savedHandlers, handlerIds)
                    && (interceptor == null || ReferenceEquals(interceptor.GetValue(null), oldInterceptor));
                Debug.Log("CLIVERIFY kf1vnexit restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static bool AmbientMatches(
            Kf1vnController controller,
            Kf1vnModel model,
            bool initialized,
            int stage,
            int turn,
            long edtime,
            int subStage,
            long subEdtime,
            bool hasStageInfo,
            FieldInfo lastLevelField,
            object lastLevel,
            IDictionary handlers,
            IReadOnlyDictionary<int, HandlerState> savedHandlers,
            IEnumerable<int> handlerIds)
        {
            bool matches = controller.IsInitialized == initialized
                && model.Stage == stage && model.Turn == turn && model.Edtime == edtime
                && model.SubStage == subStage && model.SubEdtime == subEdtime
                && model.HasStageInfo == hasStageInfo
                && lastLevelField != null && Equals(lastLevelField.GetValue(controller), lastLevel);
            foreach (int id in handlerIds)
            {
                matches &= HandlerMatches(handlers, savedHandlers[id], id);
            }
            return matches;
        }

        private static bool OneEmptyFrame(IReadOnlyList<byte[]> frames, int protocolId)
        {
            if (frames.Count != 1) return false;
            byte[] frame = frames[0];
            return frame != null && frame.Length == 6
                && frame[0] == 0 && frame[1] == 6
                && frame[2] == 3 && frame[3] == 232
                && frame[4] == (byte)(protocolId >> 8) && frame[5] == (byte)(protocolId & 0xFF);
        }

        private static void SaveHandler(IDictionary handlers, IDictionary<int, HandlerState> savedHandlers, int id)
        {
            bool exists = handlers != null && handlers.Contains(id);
            savedHandlers[id] = new HandlerState { Exists = exists, Value = exists ? handlers[id] : null };
        }

        private static bool HandlerMatches(IDictionary handlers, HandlerState savedHandler, int id)
        {
            return handlers != null && handlers.Contains(id) == savedHandler.Exists
                && (!savedHandler.Exists || ReferenceEquals(handlers[id], savedHandler.Value));
        }

        private static void RestoreHandler(IDictionary handlers, HandlerState savedHandler, int id)
        {
            if (handlers == null) return;
            if (savedHandler.Exists) handlers[id] = savedHandler.Value;
            else handlers.Remove(id);
        }
    }
}
