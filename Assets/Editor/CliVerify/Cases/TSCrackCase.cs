using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.TSCrack;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class TSCrackCase
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
                Debug.LogError("CLIVERIFY tscrack EXCEPTION " + exception);
                return Task.FromResult(3);
            }
        }

        private static int RunCore()
        {
            TSCrackController controller = TSCrackController.Instance;
            TSCrackModel model = TSCrackModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            bool oldHasData = model.HasData;
            byte oldStatus = model.Status;
            var oldServers = new List<TSCrackModel.ServerEntry>(model.Servers);
            FieldInfo interceptField = typeof(TSCrackController).GetField("s_outboundIntercept", StaticNonPublic);
            object oldIntercept = interceptField == null ? null : interceptField.GetValue(null);

            try
            {
                controller.Init();
                model.Reset();

                MethodInfo on20411 = typeof(TSCrackController).GetMethod("On20411", InstanceNonPublic);
                IDictionary handlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                bool pass = interceptField != null && on20411 != null && handlers != null && handlers.Contains(20411);
                for (int proto = 20400; proto <= 20410; proto++)
                {
                    pass &= !handlers.Contains(proto);
                }

                if (!pass)
                {
                    Debug.LogError("CLIVERIFY tscrack VERDICT pass=false (reflection/protocol registration missing)");
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
                    && frames[0][4] == (byte)(Proto.TS_CRACK_WORLD_INFO >> 8)
                    && frames[0][5] == (byte)(Proto.TS_CRACK_WORLD_INFO & 0xFF);
                frames.Clear();

                const string chineseName = "时空中文服";
                byte[] firstBytes = new CliVerify.Pkt()
                    .C(255).H(2)
                    .I(4294967295L).S(chineseName).H(65535)
                    .I(0).S("Second").H(0)
                    .Bytes();
                var firstReader = new NetReader(firstBytes, 0, firstBytes.Length);
                on20411.Invoke(controller, new object[] { firstReader });
                pass &= firstReader.Remaining == 0
                    && model.HasData && model.Status == 255 && model.Servers.Count == 2
                    && model.Servers[0].ServerNumber == uint.MaxValue
                    && model.Servers[0].ServerName == chineseName && model.Servers[0].Level == ushort.MaxValue
                    && model.Servers[1].ServerNumber == 0
                    && model.Servers[1].ServerName == "Second" && model.Servers[1].Level == 0
                    && frames.Count == 0;

                byte[] secondBytes = new CliVerify.Pkt()
                    .C(1).H(1).I(2).S("替换服").H(3)
                    .Bytes();
                var secondReader = new NetReader(secondBytes, 0, secondBytes.Length);
                on20411.Invoke(controller, new object[] { secondReader });
                pass &= secondReader.Remaining == 0
                    && model.HasData && model.Status == 1 && model.Servers.Count == 1
                    && model.Servers[0].ServerNumber == 2
                    && model.Servers[0].ServerName == "替换服" && model.Servers[0].Level == 3;

                byte[] thirdBytes = new CliVerify.Pkt().C(0).H(0).Bytes();
                var thirdReader = new NetReader(thirdBytes, 0, thirdBytes.Length);
                on20411.Invoke(controller, new object[] { thirdReader });
                pass &= thirdReader.Remaining == 0
                    && model.HasData && model.Status == 0 && model.Servers.Count == 0;

                controller.Dispose();
                pass &= !model.HasData && model.Status == 0 && model.Servers.Count == 0;

                Debug.Log("CLIVERIFY tscrack VERDICT pass=" + pass);
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
                    model.Replace(oldStatus, oldServers);
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
