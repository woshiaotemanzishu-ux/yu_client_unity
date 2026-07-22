using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.HolyBattle;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class HolyBattleCase
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
                Debug.LogError("CLIVERIFY holybattle EXCEPTION " + exception);
                return Task.FromResult(3);
            }
        }

        private static int RunCore()
        {
            HolyBattleController controller = HolyBattleController.Instance;
            HolyBattleModel model = HolyBattleModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            bool oldHasData = model.HasData;
            byte oldMod = model.Mod;
            byte oldStatus = model.Status;
            uint oldEndTime = model.EndTime;
            var oldServers = new List<HolyBattleModel.ServerEntry>(model.Servers);
            FieldInfo interceptField = typeof(HolyBattleController).GetField("s_outboundIntercept", StaticNonPublic);
            object oldIntercept = interceptField == null ? null : interceptField.GetValue(null);

            try
            {
                controller.Init();
                model.Reset();

                MethodInfo on21801 = typeof(HolyBattleController).GetMethod("On21801", InstanceNonPublic);
                IDictionary handlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                bool pass = interceptField != null && on21801 != null && handlers != null && handlers.Contains(21801);
                for (int proto = 21800; proto <= 21813; proto++)
                {
                    if (proto != 21801)
                    {
                        pass &= !handlers.Contains(proto);
                    }
                }

                if (!pass)
                {
                    Debug.LogError("CLIVERIFY holybattle VERDICT pass=false (reflection/protocol registration missing)");
                    return 3;
                }

                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));
                controller.RequestInfo();
                pass &= IsExactRequest(frames.Count == 1 ? frames[0] : null);
                frames.Clear();

                const string chineseName = "圣灵中文服";
                byte[] firstBytes = new CliVerify.Pkt()
                    .C(255).C(254).I(4294967295L).H(2)
                    .I(0).I(4000000000L).S(chineseName).I(4294967295L)
                    .I(4294967295L).I(0).S("Second").I(0)
                    .Bytes();
                var firstReader = new NetReader(firstBytes, 0, firstBytes.Length);
                on21801.Invoke(controller, new object[] { firstReader });
                pass &= firstReader.Remaining == 0
                    && model.HasData && model.Mod == 255 && model.Status == 254 && model.EndTime == uint.MaxValue
                    && model.Servers.Count == 2
                    && model.Servers[0].ServerId == 0 && model.Servers[0].ServerNumber == 4000000000U
                    && model.Servers[0].ServerName == chineseName && model.Servers[0].Level == uint.MaxValue
                    && model.Servers[1].ServerId == uint.MaxValue && model.Servers[1].ServerNumber == 0
                    && model.Servers[1].ServerName == "Second" && model.Servers[1].Level == 0
                    && frames.Count == 0;

                byte[] secondBytes = new CliVerify.Pkt()
                    .C(1).C(2).I(3).H(1).I(4).I(5).S("替换服").I(6)
                    .Bytes();
                var secondReader = new NetReader(secondBytes, 0, secondBytes.Length);
                on21801.Invoke(controller, new object[] { secondReader });
                pass &= secondReader.Remaining == 0
                    && model.HasData && model.Mod == 1 && model.Status == 2 && model.EndTime == 3
                    && model.Servers.Count == 1 && model.Servers[0].ServerId == 4
                    && model.Servers[0].ServerNumber == 5 && model.Servers[0].ServerName == "替换服"
                    && model.Servers[0].Level == 6;

                byte[] thirdBytes = new CliVerify.Pkt().C(0).C(0).I(0).H(0).Bytes();
                var thirdReader = new NetReader(thirdBytes, 0, thirdBytes.Length);
                on21801.Invoke(controller, new object[] { thirdReader });
                pass &= thirdReader.Remaining == 0
                    && model.HasData && model.Mod == 0 && model.Status == 0 && model.EndTime == 0 && model.Servers.Count == 0;

                controller.Dispose();
                pass &= !model.HasData && model.Mod == 0 && model.Status == 0 && model.EndTime == 0 && model.Servers.Count == 0;

                Debug.Log("CLIVERIFY holybattle VERDICT pass=" + pass);
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
                    model.Replace(oldMod, oldStatus, oldEndTime, oldServers);
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
                && frame[4] == (byte)(Proto.HOLY_BATTLE_INFO >> 8)
                && frame[5] == (byte)(Proto.HOLY_BATTLE_INFO & 0xFF);
        }
    }
}
