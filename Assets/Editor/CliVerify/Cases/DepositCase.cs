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
            bool oldHasCoins = model.HasCoins;
            bool oldHasRecords = model.HasRecords;
            uint oldDayCoin = model.DayCoin;
            uint oldOnhookCoin = model.OnhookCoin;
            var oldActivities = new List<DepositModel.ActivityEntry>(model.Activities);
            var oldRecords = new List<DepositModel.RecordEntry>(model.Records);
            FieldInfo interceptField = typeof(DepositController).GetField("s_outboundIntercept", StaticNonPublic);
            object oldIntercept = interceptField == null ? null : interceptField.GetValue(null);
            try
            {
                controller.Init();
                model.Reset();
                MethodInfo handler = typeof(DepositController).GetMethod("On19201", InstanceNonPublic);
                MethodInfo coins = typeof(DepositController).GetMethod("On19208", InstanceNonPublic);
                MethodInfo records = typeof(DepositController).GetMethod("On19206", InstanceNonPublic);
                IDictionary handlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                bool pass = interceptField != null && handler != null && coins != null && records != null && handlers != null && handlers.Contains(19201) && handlers.Contains(19206) && handlers.Contains(19208);
                for (int proto = 19200; proto <= 19208; proto++) if (proto != 19201 && proto != 19206 && proto != 19208) pass &= !handlers.Contains(proto);
                if (!pass) return 3;

                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                controller.RequestActivityOnhook();
                pass &= ExactFrame(frames.Count == 1 ? frames[0] : null);
                frames.Clear();
                controller.RequestRecords();
                pass &= ExactFrame(frames.Count == 1 ? frames[0] : null, Proto.DEPOSIT_RECORDS);
                frames.Clear();
                pass &= Feed(records, controller, new CliVerify.Pkt().H(2)
                        .H(0).H(ushort.MaxValue).I(uint.MaxValue).I(4000000000L).H(ushort.MaxValue).I(0)
                        .H(0).H(ushort.MaxValue).I(9).I(10).H(11).I(12).Bytes())
                    && model.HasRecords && !model.HasData && !model.HasCoins && model.Records.Count == 2
                    && model.Records[0].ModuleId == 0 && model.Records[0].SubModule == ushort.MaxValue
                    && model.Records[0].OnhookTime == uint.MaxValue && model.Records[0].Result == 4000000000U
                    && model.Records[0].CostCoin == ushort.MaxValue && model.Records[0].Time == 0
                    && model.Records[1].ModuleId == 0 && model.Records[1].SubModule == ushort.MaxValue
                    && model.Records[1].OnhookTime == 9 && model.Records[1].Result == 10
                    && model.Records[1].CostCoin == 11 && model.Records[1].Time == 12 && frames.Count == 0;
                pass &= Feed(coins, controller, new CliVerify.Pkt().I(9).I(10).Bytes()) && model.HasCoins && !model.HasData && model.DayCoin == 9 && model.OnhookCoin == 10 && model.Activities.Count == 0 && frames.Count == 0;

                byte[] first = new CliVerify.Pkt().I(uint.MaxValue).I(4000000000L).H(2)
                    .H(0).H(ushort.MaxValue).I(uint.MaxValue).H(2).H(0).I(0).H(0).H(ushort.MaxValue).I(4000000000L).H(ushort.MaxValue)
                    .H(7).H(8).I(9).H(0).Bytes();
                pass &= Feed(handler, controller, first) && model.HasData && model.HasCoins && model.DayCoin == uint.MaxValue && model.OnhookCoin == 4000000000U
                    && model.HasRecords && model.Records.Count == 2
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
                pass &= Feed(handler, controller, first) && model.Activities.Count == 2;
                pass &= Feed(coins, controller, new CliVerify.Pkt().I(100).I(150).Bytes()) && model.DayCoin == 100 && model.OnhookCoin == 150 && model.Activities.Count == 2 && frames.Count == 0;
                pass &= Feed(coins, controller, new CliVerify.Pkt().I(80).I(200).Bytes()) && model.DayCoin == 80 && model.OnhookCoin == 200 && model.Activities.Count == 2 && frames.Count == 0;
                pass &= Feed(coins, controller, new CliVerify.Pkt().I(0).I(0).Bytes()) && model.DayCoin == 0 && model.OnhookCoin == 0 && model.Activities.Count == 2 && model.Records.Count == 2 && frames.Count == 0;
                pass &= Feed(records, controller, new CliVerify.Pkt().H(1).H(3).H(4).I(5).I(6).H(7).I(8).Bytes()) && model.Records.Count == 1 && model.Records[0].ModuleId == 3 && model.Activities.Count == 2 && model.HasCoins && frames.Count == 0;
                pass &= Feed(records, controller, new CliVerify.Pkt().H(0).Bytes()) && model.HasRecords && model.Records.Count == 0 && model.Activities.Count == 2 && frames.Count == 0;
                controller.Dispose();
                pass &= !model.HasData && !model.HasCoins && !model.HasRecords && model.DayCoin == 0 && model.OnhookCoin == 0 && model.Activities.Count == 0 && model.Records.Count == 0;
                Debug.Log("CLIVERIFY deposit VERDICT pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                if (oldHasData) model.Replace(oldDayCoin, oldOnhookCoin, oldActivities);
                else if (oldHasCoins) model.ReplaceCoins(oldDayCoin, oldOnhookCoin);
                if (oldHasRecords) model.ReplaceRecords(oldRecords);
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

        private static bool ExactFrame(byte[] frame, int proto = Proto.DEPOSIT_ACTIVITY_ONHOOK)
        {
            return frame != null && frame.Length == 6 && frame[0] == 0 && frame[1] == 6 && frame[2] == 3 && frame[3] == 0xE8
                && frame[4] == (byte)(proto >> 8)
                && frame[5] == (byte)(proto & 0xFF);
        }
    }
}
