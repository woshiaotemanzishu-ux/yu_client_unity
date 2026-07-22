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
            bool oldHasBoxCounts = model.HasBoxCounts;
            uint oldLowBoxCount = model.LowBoxCount;
            uint oldHighBoxCount = model.HighBoxCount;
            bool oldHasRebornDeadline = model.HasRebornDeadline;
            uint oldRebornDeadline = model.RebornDeadline;
            bool oldHasEndDeadline = model.HasEndDeadline;
            uint oldEndDeadline = model.EndDeadline;
            FieldInfo interceptField = typeof(NoonPartyController).GetField("s_outboundIntercept", StaticNonPublic);
            object oldIntercept = interceptField == null ? null : interceptField.GetValue(null);

            try
            {
                controller.Init();
                model.Reset();

                MethodInfo on28503 = typeof(NoonPartyController).GetMethod("On28503", InstanceNonPublic);
                MethodInfo on28504 = typeof(NoonPartyController).GetMethod("On28504", InstanceNonPublic);
                MethodInfo on28505 = typeof(NoonPartyController).GetMethod("On28505", InstanceNonPublic);
                MethodInfo on28506 = typeof(NoonPartyController).GetMethod("On28506", InstanceNonPublic);
                IDictionary handlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                bool pass = interceptField != null && on28503 != null && on28504 != null && on28505 != null && on28506 != null && handlers != null
                    && handlers.Contains(28503) && handlers.Contains(28504) && handlers.Contains(28505) && handlers.Contains(28506);
                for (int proto = 28500; proto <= 28506; proto++)
                {
                    if (proto != 28503 && proto != 28504 && proto != 28505 && proto != 28506)
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
                pass &= IsExactRequest(frames.Count == 1 ? frames[0] : null, Proto.NOON_PARTY_TOTAL_EXP);
                frames.Clear();
                controller.RequestBoxCounts();
                pass &= IsExactRequest(frames.Count == 1 ? frames[0] : null, Proto.NOON_PARTY_BOX_COUNTS);
                frames.Clear();
                controller.RequestRebornDeadline();
                pass &= IsExactRequest(frames.Count == 1 ? frames[0] : null, Proto.NOON_PARTY_REBORN_DEADLINE);
                frames.Clear();
                controller.RequestEndDeadline();
                pass &= IsExactRequest(frames.Count == 1 ? frames[0] : null, Proto.NOON_PARTY_END_DEADLINE);
                frames.Clear();

                pass &= Feed(on28503, controller, 0) && model.HasData && model.TotalExp == 0 && frames.Count == 0;
                pass &= Feed(on28503, controller, 100) && model.TotalExp == 100 && frames.Count == 0;
                pass &= Feed(on28503, controller, 150) && model.TotalExp == 150 && frames.Count == 0;
                pass &= Feed(on28503, controller, uint.MaxValue) && model.TotalExp == uint.MaxValue && frames.Count == 0;
                pass &= FeedBoxCounts(on28504, controller, 0, 0) && model.HasBoxCounts && model.LowBoxCount == 0 && model.HighBoxCount == 0 && frames.Count == 0;
                pass &= FeedBoxCounts(on28504, controller, 1, 0) && model.LowBoxCount == 1 && model.HighBoxCount == 0 && frames.Count == 0;
                pass &= FeedBoxCounts(on28504, controller, 1, 1) && model.LowBoxCount == 1 && model.HighBoxCount == 1 && frames.Count == 0;
                pass &= FeedBoxCounts(on28504, controller, uint.MaxValue, 4000000000U) && model.LowBoxCount == uint.MaxValue && model.HighBoxCount == 4000000000U && frames.Count == 0;
                pass &= FeedBoxCounts(on28504, controller, 0, 0) && model.LowBoxCount == 0 && model.HighBoxCount == 0 && frames.Count == 0;
                pass &= Feed(on28505, controller, 0) && model.HasRebornDeadline && model.RebornDeadline == 0 && frames.Count == 0;
                pass &= Feed(on28505, controller, 200) && model.RebornDeadline == 200 && frames.Count == 0;
                pass &= Feed(on28505, controller, 250) && model.RebornDeadline == 250 && frames.Count == 0;
                pass &= Feed(on28505, controller, uint.MaxValue) && model.RebornDeadline == uint.MaxValue && frames.Count == 0;
                pass &= Feed(on28506, controller, 0) && model.HasEndDeadline && model.EndDeadline == 0 && frames.Count == 0;
                pass &= Feed(on28506, controller, 300) && model.EndDeadline == 300 && frames.Count == 0;
                pass &= Feed(on28506, controller, 350) && model.EndDeadline == 350 && frames.Count == 0;
                pass &= Feed(on28506, controller, uint.MaxValue) && model.EndDeadline == uint.MaxValue && frames.Count == 0;

                controller.Dispose();
                pass &= !model.HasData && model.TotalExp == 0 && !model.HasBoxCounts && model.LowBoxCount == 0 && model.HighBoxCount == 0 && !model.HasRebornDeadline && model.RebornDeadline == 0
                    && !model.HasEndDeadline && model.EndDeadline == 0;

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

                if (oldHasBoxCounts)
                {
                    model.ReplaceBoxCounts(oldLowBoxCount, oldHighBoxCount);
                }

                if (oldHasRebornDeadline)
                {
                    model.ReplaceRebornDeadline(oldRebornDeadline);
                }

                if (oldHasEndDeadline)
                {
                    model.ReplaceEndDeadline(oldEndDeadline);
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

        private static bool FeedBoxCounts(MethodInfo handler, NoonPartyController controller, uint lowBoxCount, uint highBoxCount)
        {
            byte[] packet = new CliVerify.Pkt().I(lowBoxCount).I(highBoxCount).Bytes();
            var reader = new NetReader(packet, 0, packet.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool IsExactRequest(byte[] frame, int proto)
        {
            return frame != null
                && frame.Length == 6
                && frame[0] == 0 && frame[1] == 6
                && frame[2] == 0x03 && frame[3] == 0xE8
                && frame[4] == (byte)(proto >> 8)
                && frame[5] == (byte)(proto & 0xFF);
        }
    }
}
