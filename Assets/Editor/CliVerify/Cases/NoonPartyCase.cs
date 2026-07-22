using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.NoonParty;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class NoonPartyCase
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
                Debug.LogError("CLIVERIFY noonparty EXCEPTION " + exception);
                return Task.FromResult(3);
            }
        }

        private static int RunCore()
        {
            NoonPartyController controller = NoonPartyController.Instance;
            NoonPartyModel model = NoonPartyModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            bool oldHasData = model.HasData;
            uint oldTotalExp = model.TotalExp;
            FieldInfo interceptField = typeof(NoonPartyController).GetField("s_outboundIntercept", StaticNonPublic);
            object oldIntercept = interceptField == null ? null : interceptField.GetValue(null);

            try
            {
                controller.Init();
                model.Reset();

                MethodInfo on28503 = typeof(NoonPartyController).GetMethod("On28503", InstanceNonPublic);
                IDictionary handlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                bool pass = interceptField != null && on28503 != null && handlers != null && handlers.Contains(28503);
                for (int proto = 28500; proto <= 28506; proto++)
                {
                    if (proto != 28503)
                    {
                        pass &= !handlers.Contains(proto);
                    }
                }

                if (!pass)
                {
                    Debug.LogError("CLIVERIFY noonparty VERDICT pass=false (reflection/protocol registration missing)");
                    return 3;
                }

                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));
                controller.RequestExp();
                pass &= IsExactRequest(frames.Count == 1 ? frames[0] : null);
                frames.Clear();

                pass &= Feed(on28503, controller, 0) && model.HasData && model.TotalExp == 0 && frames.Count == 0;
                pass &= Feed(on28503, controller, 100) && model.TotalExp == 100 && frames.Count == 0;
                pass &= Feed(on28503, controller, 150) && model.TotalExp == 150 && frames.Count == 0;
                pass &= Feed(on28503, controller, uint.MaxValue) && model.TotalExp == uint.MaxValue && frames.Count == 0;

                controller.Dispose();
                pass &= !model.HasData && model.TotalExp == 0;

                Debug.Log("CLIVERIFY noonparty VERDICT pass=" + pass);
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
                    model.Replace(oldTotalExp);
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

        private static bool Feed(MethodInfo handler, NoonPartyController controller, uint totalExp)
        {
            byte[] packet = new CliVerify.Pkt().I(totalExp).Bytes();
            var reader = new NetReader(packet, 0, packet.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool IsExactRequest(byte[] frame)
        {
            return frame != null
                && frame.Length == 6
                && frame[0] == 0 && frame[1] == 6
                && frame[2] == 0x03 && frame[3] == 0xE8
                && frame[4] == (byte)(Proto.NOON_PARTY_TOTAL_EXP >> 8)
                && frame[5] == (byte)(Proto.NOON_PARTY_TOTAL_EXP & 0xFF);
        }
    }
}
