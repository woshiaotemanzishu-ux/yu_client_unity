using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.GhostWalk;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class GhostWalkCase
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
                Debug.LogError("CLIVERIFY ghostwalk EXCEPTION " + exception);
                return Task.FromResult(3);
            }
        }

        private static int RunCore()
        {
            GhostWalkController controller = GhostWalkController.Instance;
            GhostWalkModel model = GhostWalkModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            bool oldHasData = model.HasData;
            byte oldState = model.State;
            uint oldEndTime = model.EndTime;
            byte oldServerModule = model.ServerModule;
            uint oldGroupId = model.GroupId;
            ushort oldAverageWorldLevel = model.AverageWorldLevel;
            var oldServers = new List<GhostWalkModel.Server>(model.Servers);
            FieldInfo interceptField = typeof(GhostWalkController).GetField("s_outboundIntercept", StaticNonPublic);
            object oldIntercept = interceptField == null ? null : interceptField.GetValue(null);

            try
            {
                controller.Init();
                model.Reset();

                MethodInfo on20601 = typeof(GhostWalkController).GetMethod("On20601", InstanceNonPublic);
                IDictionary handlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                bool pass = interceptField != null && on20601 != null && handlers != null && handlers.Contains(20601);
                int[] unsupported = { 20600, 20602, 20603, 20604, 20605 };
                foreach (int proto in unsupported)
                {
                    pass &= !handlers.Contains(proto);
                }

                if (!pass)
                {
                    Debug.LogError("CLIVERIFY ghostwalk VERDICT pass=false (reflection/protocol registration missing)");
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
                    && frames[0][4] == (byte)(Proto.GHOST_WALK_INFO >> 8)
                    && frames[0][5] == (byte)(Proto.GHOST_WALK_INFO & 0xFF);
                frames.Clear();

                const string chineseName = "百鬼中文服";
                byte[] firstBytes = new CliVerify.Pkt()
                    .C(255).I(4294967295L).C(254).I(4000000000L).H(2)
                    .H(0).H(65535).S(chineseName).H(65535).H(0)
                    .H(65535).H(0).S("Second").H(1).H(65535)
                    .H(65535)
                    .Bytes();
                var firstReader = new NetReader(firstBytes, 0, firstBytes.Length);
                on20601.Invoke(controller, new object[] { firstReader });
                pass &= firstReader.Remaining == 0
                    && model.HasData && model.State == 255 && model.EndTime == uint.MaxValue
                    && model.ServerModule == 254 && model.GroupId == 4000000000U && model.Servers.Count == 2
                    && model.Servers[0].Id == 0 && model.Servers[0].Number == ushort.MaxValue
                    && model.Servers[0].Name == chineseName && model.Servers[0].OpenDay == ushort.MaxValue
                    && model.Servers[0].WorldLevel == 0
                    && model.Servers[1].Id == ushort.MaxValue && model.Servers[1].Number == 0
                    && model.Servers[1].Name == "Second" && model.Servers[1].OpenDay == 1
                    && model.Servers[1].WorldLevel == ushort.MaxValue
                    && model.AverageWorldLevel == ushort.MaxValue && frames.Count == 0;

                byte[] secondBytes = new CliVerify.Pkt()
                    .C(1).I(2).C(3).I(4).H(1)
                    .H(5).H(6).S("替换服").H(7).H(8)
                    .H(9)
                    .Bytes();
                var secondReader = new NetReader(secondBytes, 0, secondBytes.Length);
                on20601.Invoke(controller, new object[] { secondReader });
                pass &= secondReader.Remaining == 0
                    && model.HasData && model.State == 1 && model.EndTime == 2
                    && model.ServerModule == 3 && model.GroupId == 4 && model.Servers.Count == 1
                    && model.Servers[0].Id == 5 && model.Servers[0].Number == 6
                    && model.Servers[0].Name == "替换服" && model.Servers[0].OpenDay == 7
                    && model.Servers[0].WorldLevel == 8 && model.AverageWorldLevel == 9;

                byte[] thirdBytes = new CliVerify.Pkt().C(0).I(0).C(0).I(0).H(0).H(0).Bytes();
                var thirdReader = new NetReader(thirdBytes, 0, thirdBytes.Length);
                on20601.Invoke(controller, new object[] { thirdReader });
                pass &= thirdReader.Remaining == 0
                    && model.HasData && model.State == 0 && model.EndTime == 0
                    && model.ServerModule == 0 && model.GroupId == 0 && model.Servers.Count == 0
                    && model.AverageWorldLevel == 0;

                controller.Dispose();
                pass &= !model.HasData && model.State == 0 && model.EndTime == 0
                    && model.ServerModule == 0 && model.GroupId == 0 && model.Servers.Count == 0
                    && model.AverageWorldLevel == 0;

                Debug.Log("CLIVERIFY ghostwalk VERDICT pass=" + pass);
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
                    model.Replace(oldState, oldEndTime, oldServerModule, oldGroupId, oldServers, oldAverageWorldLevel);
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
