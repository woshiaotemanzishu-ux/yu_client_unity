using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.NineSky;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class NineSkyCase
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
                Debug.LogError("CLIVERIFY ninesky EXCEPTION " + exception);
                return Task.FromResult(3);
            }
        }

        private static int RunCore()
        {
            NineSkyController controller = NineSkyController.Instance;
            NineSkyModel model = NineSkyModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            bool oldHasData = model.HasData;
            byte oldState = model.State;
            uint oldLeftTime = model.LeftTime;
            uint oldMod = model.Mod;
            uint oldGroupId = model.GroupId;
            ulong oldAverageLevel = model.AverageLevel;
            var oldServers = new List<NineSkyModel.ServerEntry>(model.Servers);
            FieldInfo interceptField = typeof(NineSkyController).GetField("s_outboundIntercept", StaticNonPublic);
            object oldIntercept = interceptField == null ? null : interceptField.GetValue(null);

            try
            {
                controller.Init();
                model.Reset();

                MethodInfo on13500 = typeof(NineSkyController).GetMethod("On13500", InstanceNonPublic);
                IDictionary handlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                bool pass = interceptField != null && on13500 != null && handlers != null && handlers.Contains(13500);
                for (int proto = 13501; proto <= 13510; proto++)
                {
                    pass &= !handlers.Contains(proto);
                }

                if (!pass)
                {
                    Debug.LogError("CLIVERIFY ninesky VERDICT pass=false (reflection/protocol registration missing)");
                    return 3;
                }

                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));
                controller.RequestInfo();
                pass &= frames.Count == 1
                    && frames[0].Length == 6
                    && frames[0][0] == 0 && frames[0][1] == 6
                    && frames[0][2] == 0x03 && frames[0][3] == 0xE8
                    && frames[0][4] == (byte)(Proto.NINE_SKY_INFO >> 8)
                    && frames[0][5] == (byte)(Proto.NINE_SKY_INFO & 0xFF);
                frames.Clear();

                const ulong max = ulong.MaxValue;
                const ulong highBit = 0x8000000000000000UL;
                const ulong averageAboveLongMax = 0x8000000000000001UL;
                const string chineseName = "九天中文服";
                byte[] firstBytes = new CliVerify.Pkt()
                    .C(255).I(4000000000L).I(2).I(3).H(2)
                    .L(unchecked((long)max)).L(unchecked((long)highBit)).S(chineseName).L(10)
                    .L(11).L(12).S("Second").L(unchecked((long)highBit))
                    .L(unchecked((long)averageAboveLongMax))
                    .Bytes();
                var firstReader = new NetReader(firstBytes, 0, firstBytes.Length);
                on13500.Invoke(controller, new object[] { firstReader });
                pass &= firstReader.Remaining == 0
                    && model.HasData && model.State == 255 && model.LeftTime == 4000000000U
                    && model.Mod == 2 && model.GroupId == 3 && model.Servers.Count == 2
                    && model.Servers[0].ServerId == max && model.Servers[0].ServerNumber == highBit
                    && model.Servers[0].ServerName == chineseName && model.Servers[0].WorldLevel == 10
                    && model.Servers[1].ServerId == 11 && model.Servers[1].ServerNumber == 12
                    && model.Servers[1].ServerName == "Second" && model.Servers[1].WorldLevel == highBit
                    && model.AverageLevel == averageAboveLongMax && frames.Count == 0;

                byte[] secondBytes = new CliVerify.Pkt()
                    .C(1).I(21).I(22).I(23).H(1)
                    .L(24).L(25).S("替换服").L(26).L(27)
                    .Bytes();
                var secondReader = new NetReader(secondBytes, 0, secondBytes.Length);
                on13500.Invoke(controller, new object[] { secondReader });
                pass &= secondReader.Remaining == 0
                    && model.HasData && model.State == 1 && model.LeftTime == 21
                    && model.Mod == 22 && model.GroupId == 23 && model.Servers.Count == 1
                    && model.Servers[0].ServerId == 24 && model.Servers[0].ServerNumber == 25
                    && model.Servers[0].ServerName == "替换服" && model.Servers[0].WorldLevel == 26
                    && model.AverageLevel == 27;

                byte[] thirdBytes = new CliVerify.Pkt().C(0).I(0).I(0).I(0).H(0).L(0).Bytes();
                var thirdReader = new NetReader(thirdBytes, 0, thirdBytes.Length);
                on13500.Invoke(controller, new object[] { thirdReader });
                pass &= thirdReader.Remaining == 0
                    && model.HasData && model.State == 0 && model.LeftTime == 0
                    && model.Mod == 0 && model.GroupId == 0 && model.Servers.Count == 0
                    && model.AverageLevel == 0;

                controller.Dispose();
                pass &= !model.HasData && model.State == 0 && model.LeftTime == 0
                    && model.Mod == 0 && model.GroupId == 0 && model.Servers.Count == 0
                    && model.AverageLevel == 0;

                Debug.Log("CLIVERIFY ninesky VERDICT pass=" + pass);
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
                    model.Replace(oldState, oldLeftTime, oldMod, oldGroupId, oldServers, oldAverageLevel);
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
    }
}
