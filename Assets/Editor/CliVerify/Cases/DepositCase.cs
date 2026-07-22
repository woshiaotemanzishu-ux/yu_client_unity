using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Deposit;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class DepositCase
    {
        private const BindingFlags InstanceNonPublic = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags StaticNonPublic = BindingFlags.NonPublic | BindingFlags.Static;

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunCore()); }
            catch (Exception exception)
            {
                Debug.LogError("CLIVERIFY deposit EXCEPTION " + exception);
                return Task.FromResult(3);
            }
        }

        private static int RunCore()
        {
            DepositController controller = DepositController.Instance;
            DepositModel model = DepositModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            bool oldHasData = model.HasData;
            uint oldDayCoin = model.DayCoin;
            uint oldOnhookCoin = model.OnhookCoin;
            var oldActivities = new List<DepositModel.ActivityEntry>(model.Activities);
            FieldInfo interceptField = typeof(DepositController).GetField("s_outboundIntercept", StaticNonPublic);
            object oldIntercept = interceptField == null ? null : interceptField.GetValue(null);
            try
            {
                controller.Init();
                model.Reset();
                MethodInfo handler = typeof(DepositController).GetMethod("On19201", InstanceNonPublic);
                IDictionary handlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                bool pass = interceptField != null && handler != null && handlers != null && handlers.Contains(19201);
                for (int proto = 19200; proto <= 19208; proto++) if (proto != 19201) pass &= !handlers.Contains(proto);
                if (!pass) return 3;

                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                controller.RequestActivityOnhook();
                pass &= ExactFrame(frames.Count == 1 ? frames[0] : null);
                frames.Clear();

                byte[] first = new CliVerify.Pkt().I(uint.MaxValue).I(4000000000L).H(2)
                    .H(0).H(ushort.MaxValue).I(uint.MaxValue).H(2).H(0).I(0).H(0).H(ushort.MaxValue).I(4000000000L).H(ushort.MaxValue)
                    .H(7).H(8).I(9).H(0).Bytes();
                pass &= Feed(handler, controller, first) && model.HasData && model.DayCoin == uint.MaxValue && model.OnhookCoin == 4000000000U
                    && model.Activities.Count == 2 && model.Activities[0].ModuleId == 0 && model.Activities[0].SubModule == ushort.MaxValue
                    && model.Activities[0].SelectTime == uint.MaxValue && model.Activities[0].Behaviours.Count == 2
                    && model.Activities[0].Behaviours[1].BehaviourId == ushort.MaxValue && model.Activities[0].Behaviours[1].SelectTime == 4000000000U
                    && model.Activities[0].Behaviours[1].Times == ushort.MaxValue && model.Activities[1].ModuleId == 7
                    && model.Activities[1].Behaviours.Count == 0 && frames.Count == 0;

                byte[] second = new CliVerify.Pkt().I(100).I(150).H(1).H(5).H(6).I(7).H(1).H(8).I(9).H(10).Bytes();
                pass &= Feed(handler, controller, second) && model.DayCoin == 100 && model.OnhookCoin == 150
                    && model.Activities.Count == 1 && model.Activities[0].ModuleId == 5 && model.Activities[0].Behaviours.Count == 1
                    && frames.Count == 0;
                byte[] third = new CliVerify.Pkt().I(0).I(0).H(0).Bytes();
                pass &= Feed(handler, controller, third) && model.HasData && model.DayCoin == 0 && model.OnhookCoin == 0
                    && model.Activities.Count == 0 && frames.Count == 0;
                controller.Dispose();
                pass &= !model.HasData && model.DayCoin == 0 && model.OnhookCoin == 0 && model.Activities.Count == 0;
                Debug.Log("CLIVERIFY deposit VERDICT pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                if (oldHasData) model.Replace(oldDayCoin, oldOnhookCoin, oldActivities);
                if (wasInitialized) controller.Init();
                if (interceptField != null) interceptField.SetValue(null, oldIntercept);
            }
        }

        private static bool Feed(MethodInfo handler, DepositController controller, byte[] bytes)
        {
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool ExactFrame(byte[] frame)
        {
            return frame != null && frame.Length == 6 && frame[0] == 0 && frame[1] == 6 && frame[2] == 3 && frame[3] == 0xE8
                && frame[4] == (byte)(Proto.DEPOSIT_ACTIVITY_ONHOOK >> 8)
                && frame[5] == (byte)(Proto.DEPOSIT_ACTIVITY_ONHOOK & 0xFF);
        }
    }
}
