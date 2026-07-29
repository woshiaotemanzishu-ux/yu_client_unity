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

        private sealed class HandlerState { public bool Exists; public object Value; }

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e) { Debug.LogError("CLIVERIFY pray EXCEPTION " + e); return Task.FromResult(3); }
        }

        private static int RunSync()
        {
            PrayController controller = PrayController.Instance;
            PrayModel model = PrayModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            bool oldHasError = model.HasError;
            uint oldErrorCode = model.LastErrorCode;
            bool oldHasInfo = model.HasPrayInfo;
            var oldInfo = new List<PrayModel.PrayInfo>(model.PrayInfoList);
            FieldInfo interceptor = typeof(PrayController).GetField("s_prayInfoOutboundIntercept", SF);
            object oldInterceptor = interceptor?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var savedHandlers = new Dictionary<int, HandlerState>();
            int[] ownedProtocolIds = { 41500, 41501, 41502 };
            foreach (int id in ownedProtocolIds) SaveHandler(handlers, savedHandlers, id);

            bool pass = false;
            bool restored = false;
            try
            {
                if (controller.IsInitialized) controller.Dispose();
                foreach (int id in ownedProtocolIds) handlers?.Remove(id);

                controller.Init();
                model.Reset();
                MethodInfo on41500 = typeof(PrayController).GetMethod("On41500", F);
                MethodInfo on41501 = typeof(PrayController).GetMethod("On41501", F);
                pass = handlers != null && interceptor != null && on41500 != null && on41501 != null
                    && handlers.Contains(41500) && handlers.Contains(41501) && !handlers.Contains(41502)
                    && OnlyExpectedPublicRequest();

                object first41500 = handlers != null && handlers.Contains(41500) ? handlers[41500] : null;
                object first41501 = handlers != null && handlers.Contains(41501) ? handlers[41501] : null;
                controller.Init();
                pass &= controller.IsInitialized && first41500 != null && first41501 != null
                    && ReferenceEquals(handlers[41500], first41500) && ReferenceEquals(handlers[41501], first41501);

                var frames = new List<byte[]>();
                interceptor.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                controller.RequestPrayInfo();
                pass &= StrictEmptyFrame(frames, Proto.PRAY_INFO)
                    && !model.HasPrayInfo && model.PrayInfoList.Count == 0 && !model.HasError;
                frames.Clear();

                Feed(on41500, controller, new CliVerify.Pkt().I(uint.MaxValue).Bytes());
                pass &= model.HasError && model.LastErrorCode == uint.MaxValue
                    && !model.HasPrayInfo && model.PrayInfoList.Count == 0;

                byte[] many = new CliVerify.Pkt().H(3)
                    .C(byte.MaxValue).C(0).C(byte.MaxValue).I(uint.MaxValue)
                    .C(1).C(2).C(3).I(0)
                    .C(byte.MaxValue).C(4).C(5).I(6).Bytes();
                Feed(on41501, controller, many);
                pass &= model.HasPrayInfo && InfoMatches(model.PrayInfoList, new[]
                    {
                        new PrayModel.PrayInfo(byte.MaxValue, 0, byte.MaxValue, uint.MaxValue),
                        new PrayModel.PrayInfo(1, 2, 3, 0),
                        new PrayModel.PrayInfo(byte.MaxValue, 4, 5, 6),
                    })
                    && model.HasError && model.LastErrorCode == uint.MaxValue && frames.Count == 0;
                IReadOnlyList<PrayModel.PrayInfo> manySnapshot = model.PrayInfoList;

                controller.RequestPrayInfo();
                pass &= StrictEmptyFrame(frames, Proto.PRAY_INFO) && model.HasPrayInfo
                    && ReferenceEquals(model.PrayInfoList, manySnapshot) && model.PrayInfoList.Count == 3;
                frames.Clear();

                Feed(on41500, controller, new CliVerify.Pkt().I(0).Bytes());
                pass &= model.HasError && model.LastErrorCode == 0
                    && ReferenceEquals(model.PrayInfoList, manySnapshot) && model.PrayInfoList.Count == 3;

                Feed(on41501, controller, new CliVerify.Pkt().H(1).C(7).C(8).C(9).I(10).Bytes());
                pass &= InfoMatches(model.PrayInfoList, new[] { new PrayModel.PrayInfo(7, 8, 9, 10) })
                    && !ReferenceEquals(model.PrayInfoList, manySnapshot) && manySnapshot.Count == 3
                    && manySnapshot[0].Type == byte.MaxValue && manySnapshot[2].EndTime == 6
                    && model.HasError && model.LastErrorCode == 0;
                IReadOnlyList<PrayModel.PrayInfo> singleSnapshot = model.PrayInfoList;

                Feed(on41501, controller, new CliVerify.Pkt().H(0).Bytes());
                pass &= model.HasPrayInfo && model.PrayInfoList.Count == 0
                    && !ReferenceEquals(model.PrayInfoList, singleSnapshot)
                    && singleSnapshot.Count == 1 && singleSnapshot[0].Type == 7
                    && model.HasError && model.LastErrorCode == 0;

                var supplied = new List<PrayModel.PrayInfo> { new PrayModel.PrayInfo(11, 12, 13, 14) };
                model.ReplacePrayInfo(supplied);
                supplied[0] = new PrayModel.PrayInfo(0, 0, 0, 0);
                pass &= model.PrayInfoList[0].Type == 11 && model.PrayInfoList[0].EndTime == 14;
                bool addRejected = false;
                try { ((IList<PrayModel.PrayInfo>)model.PrayInfoList).Add(new PrayModel.PrayInfo(1, 1, 1, 1)); }
                catch (NotSupportedException) { addRejected = true; }
                pass &= addRejected;

                controller.Dispose();
                pass &= !controller.IsInitialized && !model.HasError && model.LastErrorCode == 0
                    && !model.HasPrayInfo && model.PrayInfoList.Count == 0
                    && !handlers.Contains(41500) && !handlers.Contains(41501) && !handlers.Contains(41502);
                Debug.Log("CLIVERIFY pray VERDICT pass=" + pass);
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                if (oldHasError) model.ReplaceError(oldErrorCode);
                if (oldHasInfo) model.ReplacePrayInfo(oldInfo);
                if (wasInitialized) controller.Init();
                foreach (int id in ownedProtocolIds) RestoreHandler(handlers, savedHandlers[id], id);
                if (interceptor != null) interceptor.SetValue(null, oldInterceptor);

                restored = ReferenceEquals(PrayController.Instance, controller)
                    && ReferenceEquals(PrayModel.Instance, model)
                    && controller.IsInitialized == wasInitialized
                    && model.HasError == oldHasError && model.LastErrorCode == oldErrorCode
                    && model.HasPrayInfo == oldHasInfo && InfoMatches(model.PrayInfoList, oldInfo)
                    && (interceptor == null || ReferenceEquals(interceptor.GetValue(null), oldInterceptor));
                foreach (int id in ownedProtocolIds) restored &= HandlerMatches(handlers, savedHandlers[id], id);
                Debug.Log("CLIVERIFY pray restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static void Feed(MethodInfo handler, PrayController controller, byte[] bytes)
        {
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            if (reader.Remaining != 0) throw new InvalidOperationException("Unread payload bytes: " + reader.Remaining);
        }

        private static bool StrictEmptyFrame(IReadOnlyList<byte[]> frames, int id)
        {
            return frames.Count == 1 && frames[0] != null && frames[0].Length == 6
                && frames[0][0] == 0 && frames[0][1] == 6 && frames[0][2] == 3 && frames[0][3] == 232
                && frames[0][4] == (byte)(id >> 8) && frames[0][5] == (byte)id;
        }

        private static bool OnlyExpectedPublicRequest()
        {
            int count = 0;
            foreach (MethodInfo method in typeof(PrayController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (method.Name.IndexOf("Request", StringComparison.OrdinalIgnoreCase) < 0
                    && method.Name.IndexOf("Send", StringComparison.OrdinalIgnoreCase) < 0) continue;
                count++;
                if (method.Name != nameof(PrayController.RequestPrayInfo) || method.GetParameters().Length != 0) return false;
            }
            return count == 1;
        }

        private static bool InfoMatches(IReadOnlyList<PrayModel.PrayInfo> actual, IReadOnlyList<PrayModel.PrayInfo> expected)
        {
            if (actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++)
            {
                if (actual[i].Type != expected[i].Type || actual[i].RemainTimes != expected[i].RemainTimes
                    || actual[i].FreeTimes != expected[i].FreeTimes || actual[i].EndTime != expected[i].EndTime) return false;
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
            if (handlers == null) return;
            if (savedHandler.Exists) handlers[id] = savedHandler.Value;
            else handlers.Remove(id);
        }

        private static bool HandlerMatches(IDictionary handlers, HandlerState savedHandler, int id)
        {
            return handlers != null && handlers.Contains(id) == savedHandler.Exists
                && (!savedHandler.Exists || ReferenceEquals(handlers[id], savedHandler.Value));
        }
    }
}
