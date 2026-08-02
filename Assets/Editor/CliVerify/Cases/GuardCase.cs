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

        public static Task<int> Run()
        {
            try
            {
                return Task.FromResult(RunSync());
            }
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
            FieldInfo interceptField = typeof(GuardController).GetField("s_outboundIntercept", StaticFlags);
            object oldIntercept = interceptField?.GetValue(null);
            IDictionary handlers = typeof(NetManager).GetField("_handlers", StaticFlags)?.GetValue(null) as IDictionary;
            bool oldHandlerExists = handlers != null && handlers.Contains(Proto.GUARD_INFO);
            object oldHandler = oldHandlerExists ? handlers[Proto.GUARD_INFO] : null;
            bool pass = false;
            bool restored = false;

            try
            {
                controller.Init();
                model.Reset();

                MethodInfo handler = typeof(GuardController).GetMethod("On21601", InstanceFlags);
                handlers = typeof(NetManager).GetField("_handlers", StaticFlags)?.GetValue(null) as IDictionary;
                pass = interceptField != null
                    && handler != null
                    && handlers != null
                    && handlers.Contains(21601)
                    && !handlers.Contains(21600)
                    && !handlers.Contains(21602)
                    && !handlers.Contains(21603)
                    && !handlers.Contains(21604)
                    && !handlers.Contains(21605)
                    && !handlers.Contains(21606);
                if (!pass) throw new InvalidOperationException("Guard handler/interceptor precondition failed.");

                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));
                controller.RequestInfo();
                pass &= IsRequestFrame(frames);
                frames.Clear();

                byte[] first = new CliVerify.Pkt()
                    .H(2)
                    .C(255).C(0).I(4000000000L).C(255).C(0)
                    .C(1).C(255).I(2).C(0).C(255)
                    .Bytes();
                var firstReader = new NetReader(first, 0, first.Length);
                handler.Invoke(controller, new object[] { firstReader });
                pass &= firstReader.Remaining == 0
                    && model.HasData
                    && model.Circles.Count == 2
                    && model.Circles[0].Status == 255
                    && model.Circles[0].Level == 0
                    && model.Circles[0].EndTime == 4000000000U
                    && model.Circles[0].Show == 255
                    && model.Circles[0].FreeFlag == 0
                    && model.Circles[1].Status == 1
                    && model.Circles[1].Level == 255
                    && model.Circles[1].EndTime == 2
                    && model.Circles[1].Show == 0
                    && model.Circles[1].FreeFlag == 255
                    && frames.Count == 0;

                byte[] second = new CliVerify.Pkt().H(1).C(2).C(3).I(4).C(5).C(6).Bytes();
                var secondReader = new NetReader(second, 0, second.Length);
                handler.Invoke(controller, new object[] { secondReader });
                pass &= secondReader.Remaining == 0
                    && model.Circles.Count == 1
                    && model.Circles[0].Status == 2;

                byte[] empty = new CliVerify.Pkt().H(0).Bytes();
                var emptyReader = new NetReader(empty, 0, empty.Length);
                handler.Invoke(controller, new object[] { emptyReader });
                pass &= emptyReader.Remaining == 0
                    && model.HasData
                    && model.Circles.Count == 0;

                controller.Dispose();
                pass &= !controller.IsInitialized && !handlers.Contains(Proto.GUARD_INFO)
                    && !model.HasData && model.Circles.Count == 0;
            }
            finally
            {
                try
                {
                    if (controller.IsInitialized) controller.Dispose();
                    model.Reset();
                    if (oldHasData) model.Replace(oldCircles);
                    if (wasInitialized) controller.Init();
                    handlers = typeof(NetManager).GetField("_handlers", StaticFlags)?.GetValue(null) as IDictionary;
                    if (handlers == null) throw new InvalidOperationException("Guard handlers unavailable during restore.");
                    if (oldHandlerExists) handlers[Proto.GUARD_INFO] = oldHandler;
                    else handlers.Remove(Proto.GUARD_INFO);
                    if (interceptField != null) interceptField.SetValue(null, oldIntercept);

                    restored = controller.IsInitialized == wasInitialized
                        && model.HasData == oldHasData && CirclesMatch(model.Circles, oldCircles)
                        && handlers.Contains(Proto.GUARD_INFO) == oldHandlerExists
                        && (!oldHandlerExists || ReferenceEquals(handlers[Proto.GUARD_INFO], oldHandler))
                        && (interceptField == null || ReferenceEquals(interceptField.GetValue(null), oldIntercept));
                }
                catch (Exception exception)
                {
                    Debug.LogError("CLIVERIFY guard restore " + exception);
                    restored = false;
                }
            }

            Debug.Log("CLIVERIFY guard restored=" + restored + " VERDICT pass=" + pass);
            return pass && restored ? 0 : 3;
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

        private static bool CirclesMatch(IReadOnlyList<GuardModel.Circle> actual, IReadOnlyList<GuardModel.Circle> expected)
        {
            if (actual.Count != expected.Count) return false;
            for (int index = 0; index < expected.Count; index++)
            {
                GuardModel.Circle a = actual[index];
                GuardModel.Circle b = expected[index];
                if (a.Status != b.Status || a.Level != b.Level || a.EndTime != b.EndTime
                    || a.Show != b.Show || a.FreeFlag != b.FreeFlag)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
