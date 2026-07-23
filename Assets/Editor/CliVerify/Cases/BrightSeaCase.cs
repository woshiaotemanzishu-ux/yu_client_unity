using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.BrightSea;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class BrightSeaCase
    {
        private const BindingFlags InstanceNonPublic = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags StaticNonPublic = BindingFlags.Static | BindingFlags.NonPublic;

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunCore()); }
            catch (Exception e) { Debug.LogError("CLIVERIFY brightsea EXCEPTION " + e); return Task.FromResult(3); }
        }

        private static int RunCore()
        {
            BrightSeaController controller = BrightSeaController.Instance;
            BrightSeaModel model = BrightSeaModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            bool oldHasInfo = model.HasInfo;
            string oldPicture = model.Picture;
            uint oldPictureVersion = model.PictureVersion;
            byte oldRewardTimes = model.RewardTimes, oldTotalRewardTimes = model.TotalRewardTimes;
            byte oldRobTimes = model.RobTimes, oldTotalRobTimes = model.TotalRobTimes, oldStatus = model.Status;
            ulong oldAutoId = model.AutoId;
            var oldList = new List<BrightSeaModel.ShippingEntry>(model.SendList);
            FieldInfo intercept = typeof(BrightSeaController).GetField("s_outboundIntercept", StaticNonPublic);
            object oldIntercept = intercept == null ? null : intercept.GetValue(null);

            try
            {
                controller.Init();
                model.Clear();
                MethodInfo on18900 = typeof(BrightSeaController).GetMethod("On18900", InstanceNonPublic);
                IDictionary handlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                bool pass = intercept != null && on18900 != null && handlers != null && handlers.Contains(18900);
                for (int proto = 18901; proto <= 18920; proto++) pass &= !handlers.Contains(proto);

                var frames = new List<byte[]>();
                intercept.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                controller.RequestInfo();
                pass &= Frames(frames, 18900);

                frames.Clear();
                model.Replace("stale", 1, 1, 1, 1, 1, 1, 1, new List<BrightSeaModel.ShippingEntry> { new BrightSeaModel.ShippingEntry() });
                EventDispatcher.Emit(GlobalEvent.EVT_GAME_START);
                pass &= Frames(frames, 18900) && !model.HasInfo && model.SendList.Count == 0;

                string chinese = "海域中文";
                var firstReader = new NetReader(FirstPacket(chinese), 0, FirstPacket(chinese).Length);
                on18900.Invoke(controller, new object[] { firstReader });
                pass &= firstReader.Remaining == 0 && model.HasInfo && model.Picture == chinese
                    && model.PictureVersion == uint.MaxValue && model.RewardTimes == byte.MaxValue
                    && model.TotalRewardTimes == 0 && model.RobTimes == 254 && model.TotalRobTimes == 1
                    && model.AutoId == ulong.MaxValue && model.Status == 253 && model.SendList.Count == 2
                    && model.SendList[0].AutoId == 77 && model.SendList[1].AutoId == 77
                    && model.SendList[0].GuildName == chinese && model.SendList[0].RoleName == ""
                    && model.SendList[0].ServerId == uint.MaxValue && model.SendList[0].ServerNumber == 4000000000U
                    && model.SendList[0].GuildId == ulong.MaxValue && model.SendList[0].RoleId == ulong.MaxValue
                    && model.SendList[0].RoleLevel == ushort.MaxValue && model.SendList[0].Power == ulong.MaxValue
                    && model.SendList[0].Sex == 255 && model.SendList[0].Career == ushort.MaxValue
                    && model.SendList[0].Turn == 254 && model.SendList[0].Picture == "" && model.SendList[0].PictureVersion == uint.MaxValue
                    && model.SendList[0].EndTime == uint.MaxValue && model.SendList[0].RobTimes == 255;

                byte[] second = SinglePacket();
                var secondReader = new NetReader(second, 0, second.Length);
                on18900.Invoke(controller, new object[] { secondReader });
                pass &= secondReader.Remaining == 0 && model.SendList.Count == 1 && model.Picture == "next"
                    && model.AutoId == 2 && model.SendList[0].AutoId == 3 && model.SendList[0].RoleName == "solo";

                byte[] empty = new CliVerify.Pkt().S("").I(0).C(0).C(0).C(0).C(0).L(0).C(0).H(0).Bytes();
                var emptyReader = new NetReader(empty, 0, empty.Length);
                on18900.Invoke(controller, new object[] { emptyReader });
                pass &= emptyReader.Remaining == 0 && model.HasInfo && model.SendList.Count == 0 && model.Picture == "";

                model.Replace("keep", 4, 5, 6, 7, 8, 9, 10, new List<BrightSeaModel.ShippingEntry> { new BrightSeaModel.ShippingEntry { AutoId = 11 } });
                pass &= model.HasInfo && model.AutoId == 9 && model.SendList.Count == 1 && model.SendList[0].AutoId == 11;

                controller.Dispose();
                frames.Clear();
                EventDispatcher.Emit(GlobalEvent.EVT_GAME_START);
                pass &= !controller.IsInitialized && !handlers.Contains(18900) && !model.HasInfo && model.SendList.Count == 0 && frames.Count == 0;
                Debug.Log("CLIVERIFY brightsea VERDICT pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Clear();
                if (oldHasInfo) model.Replace(oldPicture, oldPictureVersion, oldRewardTimes, oldTotalRewardTimes,
                    oldRobTimes, oldTotalRobTimes, oldAutoId, oldStatus, oldList);
                if (wasInitialized) controller.Init();
                if (intercept != null) intercept.SetValue(null, oldIntercept);
            }
        }

        private static bool Frames(List<byte[]> frames, int proto) => frames.Count == 1 && frames[0].Length == 6
            && frames[0][0] == 0 && frames[0][1] == 6 && frames[0][2] == 3 && frames[0][3] == 232
            && frames[0][4] == (byte)(proto >> 8) && frames[0][5] == (byte)proto;

        private static byte[] FirstPacket(string chinese)
        {
            return new CliVerify.Pkt().S(chinese).I(4294967295L).C(255).C(0).C(254).C(1).L(-1).C(253).H(2)
                .L(77).C(1).I(4294967295L).I(4000000000L).L(-1).S(chinese).L(-1).S("").H(65535).L(-1)
                .C(255).H(65535).C(254).S("").I(4294967295L).I(4294967295L).C(255)
                .L(77).C(2).I(3).I(4).L(5).S("g").L(6).S("r").H(7).L(8).C(9).H(10).C(11).S("p").I(12).I(13).C(14).Bytes();
        }

        private static byte[] SinglePacket()
        {
            return new CliVerify.Pkt().S("next").I(1).C(2).C(3).C(4).C(5).L(2).C(6).H(1)
                .L(3).C(4).I(5).I(6).L(7).S("guild").L(8).S("solo").H(9).L(10).C(11).H(12).C(13).S("pic").I(14).I(15).C(16).Bytes();
        }
    }
}
