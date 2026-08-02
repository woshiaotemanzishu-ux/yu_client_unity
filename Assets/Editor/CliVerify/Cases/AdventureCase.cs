using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Adventure;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class AdventureCase
    {
        private const BindingFlags InstanceNonPublic = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags StaticNonPublic = BindingFlags.Static | BindingFlags.NonPublic;

        public static Task<int> Run()
        {
            try
            {
                return Task.FromResult(RunCore());
            }
            catch (Exception exception)
            {
                Debug.LogError("CLIVERIFY adventure EXCEPTION " + exception);
                return Task.FromResult(3);
            }
        }

        private static int RunCore()
        {
            AdventureController controller = AdventureController.Instance;
            AdventureModel model = AdventureModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            int oldStage = model.Stage;
            long oldStartTime = model.StartTime;
            long oldEndTime = model.EndTime;
            bool oldHasBoardState = model.HasBoardState;
            ushort oldCircle = model.Circle;
            ushort oldLocation = model.Location;
            ushort oldLeftTimes = model.LeftTimes;
            ushort oldThrowTimes = model.ThrowTimes;
            ushort oldFreeResetTimes = model.FreeResetTimes;
            ushort oldFreeThrowTimes = model.FreeThrowTimes;
            FieldInfo outboundIntercept = typeof(AdventureController).GetField("s_boardStateOutboundIntercept", StaticNonPublic);
            Delegate oldIntercept = outboundIntercept == null ? null : outboundIntercept.GetValue(null) as Delegate;
            MethodInfo on42701 = typeof(AdventureController).GetMethod("On42701", InstanceNonPublic);
            FieldInfo handlersField = typeof(NetManager).GetField("_handlers", StaticNonPublic);
            IDictionary handlers = handlersField == null ? null : handlersField.GetValue(null) as IDictionary;
            List<byte[]> outbound = new List<byte[]>();
            bool pass = true;

            try
            {
                if (controller.IsInitialized)
                    controller.Dispose();
                model.Reset();
                controller.Init();

                pass &= outboundIntercept != null && on42701 != null && handlers != null
                    && typeof(AdventureController).GetMethod("On42704", InstanceNonPublic) == null;
                pass &= handlers.Contains(Proto.ADVENTURE_INFO) && handlers.Contains(Proto.ADVENTURE_BOARD_STATE);
                for (int proto = 42702; proto <= 42706; proto++)
                    pass &= !handlers.Contains(proto);

                outboundIntercept.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    outbound.Add(frame);
                    return true;
                }));
                controller.RequestBoardState();
                pass &= outbound.Count == 1 && IsEmptyBoardRequest(outbound[0]) && !model.HasBoardState;
                outbound.Clear();

                pass &= Receive(on42701, controller, 0, 0, 0, 0, 0, 0)
                    && IsBoard(model, true, 0, 0, 0, 0, 0, 0) && outbound.Count == 0;

                pass &= Receive(on42701, controller, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue)
                    && IsBoard(model, true, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue) && outbound.Count == 0;

                pass &= Receive(on42701, controller, 1, 2, 3, 4, 5, 6)
                    && IsBoard(model, true, 1, 2, 3, 4, 5, 6) && outbound.Count == 0;

                controller.RequestBoardState();
                pass &= outbound.Count == 1 && IsEmptyBoardRequest(outbound[0]);
                pass &= IsBoard(model, true, 1, 2, 3, 4, 5, 6);
                outbound.Clear();

                model.SetTimeInfo(7, 8, 9);
                pass &= model.Stage == 7 && model.StartTime == 8 && model.EndTime == 9;
                pass &= IsBoard(model, true, 1, 2, 3, 4, 5, 6);
                pass &= Receive(on42701, controller, 10, 11, 12, 13, 14, 15)
                    && model.Stage == 7 && model.StartTime == 8 && model.EndTime == 9
                    && IsBoard(model, true, 10, 11, 12, 13, 14, 15) && outbound.Count == 0;

                pass &= Receive(on42701, controller, 0, 0, 0, 0, 0, 0)
                    && IsBoard(model, true, 0, 0, 0, 0, 0, 0) && outbound.Count == 0;

                controller.Dispose();
                pass &= !controller.IsInitialized;
                pass &= !handlers.Contains(Proto.ADVENTURE_INFO) && !handlers.Contains(Proto.ADVENTURE_BOARD_STATE);
                for (int proto = 42702; proto <= 42706; proto++)
                    pass &= !handlers.Contains(proto);
                pass &= model.Stage == 0 && model.StartTime == 0 && model.EndTime == 0;
                pass &= IsBoard(model, false, 0, 0, 0, 0, 0, 0);
            }
            finally
            {
                if (controller.IsInitialized)
                    controller.Dispose();
                model.Reset();
                model.SetTimeInfo(oldStage, oldStartTime, oldEndTime);
                if (oldHasBoardState)
                    model.ReplaceBoardState(oldCircle, oldLocation, oldLeftTimes, oldThrowTimes, oldFreeResetTimes, oldFreeThrowTimes);
                if (wasInitialized)
                    controller.Init();
                if (outboundIntercept != null)
                    outboundIntercept.SetValue(null, oldIntercept);
            }

            Debug.Log("CLIVERIFY adventure VERDICT pass=" + pass);
            return pass ? 0 : 3;
        }

        private static bool Receive(MethodInfo handler, AdventureController controller, ushort circle, ushort location, ushort leftTimes, ushort throwTimes, ushort freeResetTimes, ushort freeThrowTimes)
        {
            byte[] payload = new CliVerify.Pkt()
                .H(circle).H(location).H(leftTimes).H(throwTimes).H(freeResetTimes).H(freeThrowTimes).Bytes();
            var reader = new NetReader(payload, 0, payload.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool IsEmptyBoardRequest(byte[] frame)
        {
            return frame != null && frame.Length == 6
                && frame[0] == 0 && frame[1] == 6 && frame[2] == 3 && frame[3] == 0xE8
                && frame[4] == (byte)(Proto.ADVENTURE_BOARD_STATE >> 8)
                && frame[5] == (byte)(Proto.ADVENTURE_BOARD_STATE & 0xFF);
        }

        private static bool IsBoard(AdventureModel model, bool hasBoardState, ushort circle, ushort location, ushort leftTimes, ushort throwTimes, ushort freeResetTimes, ushort freeThrowTimes)
        {
            return model.HasBoardState == hasBoardState
                && model.Circle == circle && model.Location == location && model.LeftTimes == leftTimes
                && model.ThrowTimes == throwTimes && model.FreeResetTimes == freeResetTimes && model.FreeThrowTimes == freeThrowTimes;
        }
    }
}
