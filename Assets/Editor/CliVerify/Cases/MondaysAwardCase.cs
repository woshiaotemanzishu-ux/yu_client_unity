using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.MondaysAward;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class MondaysAwardCase
    {
        private const BindingFlags InstanceNonPublic = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags StaticNonPublic = BindingFlags.NonPublic | BindingFlags.Static;

        public static Task<int> Run()
        {
            try
            {
                return Task.FromResult(RunCore());
            }
            catch (Exception exception)
            {
                Debug.LogError("CLIVERIFY mondaysaward EXCEPTION " + exception);
                return Task.FromResult(3);
            }
        }

        private static int RunCore()
        {
            MondaysAwardController controller = MondaysAwardController.Instance;
            MondaysAwardModel model = MondaysAwardModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            bool oldHasData = model.HasData;
            var oldTaskStates = new List<MondaysAwardModel.TaskStateEntry>(model.TaskStates);
            FieldInfo interceptField = typeof(MondaysAwardController).GetField("s_outboundIntercept", StaticNonPublic);
            object oldIntercept = interceptField == null ? null : interceptField.GetValue(null);

            try
            {
                controller.Init();
                model.Reset();

                MethodInfo on17904 = typeof(MondaysAwardController).GetMethod("On17904", InstanceNonPublic);
                IDictionary handlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                bool pass = interceptField != null && on17904 != null && handlers != null && handlers.Contains(17904);
                for (int proto = 17900; proto <= 17908; proto++)
                {
                    if (proto != 17904)
                    {
                        pass &= !handlers.Contains(proto);
                    }
                }

                if (!pass)
                {
                    Debug.LogError("CLIVERIFY mondaysaward VERDICT pass=false (reflection/protocol registration missing)");
                    return 3;
                }

                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));
                controller.RequestTaskState();
                pass &= IsExactRequest(frames.Count == 1 ? frames[0] : null);
                pass &= frames.Count == 1;
                frames.Clear();

                byte[] firstBytes = new CliVerify.Pkt().H(2).H(0).C(0).H(65535).C(255).Bytes();
                var firstReader = new NetReader(firstBytes, 0, firstBytes.Length);
                on17904.Invoke(controller, new object[] { firstReader });
                pass &= firstReader.Remaining == 0 && model.HasData && model.TaskStates.Count == 2
                    && model.TaskStates[0].TaskId == 0 && model.TaskStates[0].State == 0
                    && model.TaskStates[1].TaskId == ushort.MaxValue && model.TaskStates[1].State == byte.MaxValue
                    && frames.Count == 0;

                byte[] secondBytes = new CliVerify.Pkt().H(1).H(7).C(8).Bytes();
                var secondReader = new NetReader(secondBytes, 0, secondBytes.Length);
                on17904.Invoke(controller, new object[] { secondReader });
                pass &= secondReader.Remaining == 0 && model.HasData && model.TaskStates.Count == 1
                    && model.TaskStates[0].TaskId == 7 && model.TaskStates[0].State == 8
                    && frames.Count == 0;

                byte[] thirdBytes = new CliVerify.Pkt().H(0).Bytes();
                var thirdReader = new NetReader(thirdBytes, 0, thirdBytes.Length);
                on17904.Invoke(controller, new object[] { thirdReader });
                pass &= thirdReader.Remaining == 0 && model.HasData && model.TaskStates.Count == 0
                    && frames.Count == 0;

                controller.Dispose();
                pass &= !model.HasData && model.TaskStates.Count == 0;

                Debug.Log("CLIVERIFY mondaysaward VERDICT pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                if (controller.IsInitialized)
                {
                    controller.Dispose();
                }

                model.Reset();
                if (oldHasData)
                {
                    model.Replace(oldTaskStates);
                }

                if (wasInitialized)
                {
                    controller.Init();
                }

                if (interceptField != null)
                {
                    interceptField.SetValue(null, oldIntercept);
                }
            }
        }

        private static bool IsExactRequest(byte[] frame)
        {
            return frame != null
                && frame.Length == 6
                && frame[0] == 0 && frame[1] == 6
                && frame[2] == 0x03 && frame[3] == 0xE8
                && frame[4] == (byte)(Proto.MONDAYS_AWARD_TASK_STATE >> 8)
                && frame[5] == (byte)(Proto.MONDAYS_AWARD_TASK_STATE & 0xFF);
        }
    }
}
