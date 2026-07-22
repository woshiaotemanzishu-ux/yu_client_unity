using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Res;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.DragonBall;
using Shenxiao.Module.Core.Game;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>14311 图标门槛、空包出站、等级/跨天事件和 Dispose 生命周期专项。</summary>
    public static class DragonBallCase
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;

        public static async Task<int> Run()
        {
            bool oldFallback = ResManager.EditorPreferFallback;
            ResManager.EditorPreferFallback = true;
            try { return await RunCore(); }
            finally { ResManager.EditorPreferFallback = oldFallback; }
        }

        private static async Task<int> RunCore()
        {
            await DragonBallConfigs.EnsureLoaded();
            await FuncOpenConfig.EnsureLoaded();

            DragonBallController controller = DragonBallController.Instance;
            DragonBallModel model = DragonBallModel.Instance;
            RoleModel role = RoleModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            int oldGift = model.GiftId;
            int oldBuy = model.BuyTimes;
            byte oldStatueStatus = model.StatueStatus;
            ulong oldStatuePower = model.StatuePreviewPower;
            bool oldHasStatueOverview = model.HasStatueOverview;
            int oldLevel = role.Level;
            bool oldHasBaseInfo = role.HasBaseInfo;
            bool oldAlpha = PlatformModel.IsAlpha;

            FieldInfo intercept = controller.GetType().GetField("s_outboundIntercept", PrivateStatic);
            FieldInfo lastLevel = controller.GetType().GetField("_lastLevel", PrivateInstance);
            FieldInfo generation = controller.GetType().GetField("_generation", PrivateInstance);
            FieldInfo hasBaseInfo = typeof(RoleModel).GetField("<HasBaseInfo>k__BackingField", PrivateInstance);
            MethodInfo on14311 = controller.GetType().GetMethod("On14311", PrivateInstance);
            MethodInfo on14310 = controller.GetType().GetMethod("On14310", PrivateInstance);
            MethodInfo onRole = controller.GetType().GetMethod("OnRoleInfoUpdate", PrivateInstance);
            object oldIntercept = intercept?.GetValue(null);
            int oldLastLevel = lastLevel == null ? -1 : (int)lastLevel.GetValue(controller);

            try
            {
                controller.Init();

                DragonBallConfigs.Row one = DragonBallConfigs.Get(1);
                DragonBallConfigs.Row eight = DragonBallConfigs.Get(8);
                bool configOk = FuncOpenConfig.IsLoaded && DragonBallConfigs.Count == 8
                    && DragonBallConfigs.HasOpenLevel(150) && !DragonBallConfigs.HasOpenLevel(149)
                    && one != null && one.OpenLevel == 150 && one.OpenDay == 1 && one.TimesLimit == 1
                    && eight != null && eight.OpenLevel == 150 && eight.OpenDay == 8 && eight.TimesLimit == 3;

                model.SetGiftInfo(0, 0);
                bool id0 = !model.GetGiftIconOpenState(150, 1, true, true, false);
                model.SetGiftInfo(999, 0);
                bool missingRow = !model.GetGiftIconOpenState(999, 99, true, true, false);
                model.SetGiftInfo(1, 0);
                bool function = !model.GetGiftIconOpenState(150, 1, true, false, false);
                bool alpha = !model.GetGiftIconOpenState(150, 1, true, true, true);
                bool levelGate = !model.GetGiftIconOpenState(149, 1, true, true, false);
                bool dayGate = !model.GetGiftIconOpenState(150, 0, true, true, false);
                model.SetGiftInfo(1, 1);
                bool exhausted = !model.GetGiftIconOpenState(150, 1, true, true, false);
                model.SetGiftInfo(1, 0);
                bool firstRecharge = !model.GetGiftIconOpenState(150, 1, false, true, false);
                bool success = model.GetGiftIconOpenState(150, 1, true, true, false);
                bool gatesOk = id0 && missingRow && function && alpha && levelGate && dayGate
                    && exhausted && firstRecharge && success;

                FieldInfo handlersField = typeof(NetManager).GetField("_handlers", PrivateStatic);
                var handlers = handlersField?.GetValue(null) as IDictionary;
                bool handlerOk = on14310 != null && on14311 != null && handlers != null
                    && handlers.Contains(Proto.DRAGONBALL_STATUE_OVERVIEW) && handlers.Contains(Proto.DRAGONBALL_GIFT_INFO);
                if (handlerOk)
                {
                    byte[] packet = new CliVerify.Pkt().I(1).H(1).Bytes();
                    var reader = new NetReader(packet, 0, packet.Length);
                    on14311.Invoke(controller, new object[] { reader });
                    handlerOk = reader.Remaining == 0 && model.GiftId == 1 && model.BuyTimes == 1
                        && DragonBallConfigs.Get(1).TimesLimit - model.BuyTimes == 0;
                }

                if (handlerOk)
                {
                    byte[] packet = new CliVerify.Pkt().C(0).L(5000000000L).Bytes();
                    var reader = new NetReader(packet, 0, packet.Length);
                    on14310.Invoke(controller, new object[] { reader });
                    handlerOk = reader.Remaining == 0 && model.HasStatueOverview && model.StatueStatus == 0 && model.StatuePreviewPower == 5000000000UL;
                    byte[] activePacket = new CliVerify.Pkt().C(1).L(0).Bytes();
                    var activeReader = new NetReader(activePacket, 0, activePacket.Length);
                    on14310.Invoke(controller, new object[] { activeReader });
                    handlerOk = handlerOk && activeReader.Remaining == 0 && model.StatueStatus == 1 && model.StatuePreviewPower == 0;
                }

                bool seamsOk = intercept != null && onRole != null && lastLevel != null
                    && generation != null && hasBaseInfo != null;
                var frames = new List<byte[]>();
                if (intercept != null)
                {
                    intercept.SetValue(null, new Func<byte[], bool>(frame =>
                    {
                        frames.Add(frame);
                        return true;
                    }));
                }

                role.Level = 149;
                hasBaseInfo?.SetValue(role, true);
                lastLevel?.SetValue(controller, -1);
                controller.RequestStartup();
                int startup = frames.Count;
                onRole?.Invoke(controller, null); // 149：不在配置的 open_lv 集合。
                int at149 = frames.Count;
                role.Level = 150;
                onRole?.Invoke(controller, null); // 150：精确命中，只发一次。
                int at150 = frames.Count;
                role.Level = 151;
                onRole?.Invoke(controller, null); // 151：跨过门槛也不能再发。
                int at151 = frames.Count;
                EventDispatcher.Emit(GlobalEvent.EVT_FIRST_RECHARGE_UPDATE); // 仅本地复评，不重拉。
                int firstRechargeRefresh = frames.Count;
                EventDispatcher.Emit(GlobalEvent.EVT_SERVER_DAY_CHANGE); // 跨天无条件重拉。
                int dayChange = frames.Count;
                onRole?.Invoke(controller, null); // 同级噪音不发。
                int sameLevel = frames.Count;

                bool outboundOk = seamsOk && startup == 2 && at149 == 2 && at150 == 3 && at151 == 3
                    && firstRechargeRefresh == 3 && dayChange == 4 && sameLevel == 4
                    && EmptyFrames(frames, Proto.DRAGONBALL_STATUE_OVERVIEW, Proto.DRAGONBALL_GIFT_INFO,
                        Proto.DRAGONBALL_GIFT_INFO, Proto.DRAGONBALL_GIFT_INFO);

                int generationBefore = generation == null ? -1 : (int)generation.GetValue(controller);
                controller.Dispose();
                EventDispatcher.Emit(GlobalEvent.EVT_SERVER_DAY_CHANGE);
                int generationAfter = generation == null ? -1 : (int)generation.GetValue(controller);
                bool disposeOk = !controller.IsInitialized && frames.Count == 4 && !model.HasStatueOverview
                    && model.StatueStatus == 0 && model.StatuePreviewPower == 0 && generationAfter == generationBefore + 1;

                bool pass = configOk && gatesOk && handlerOk && outboundOk && disposeOk;
                Debug.Log("CLIVERIFY dragonball config=" + configOk + " gates=" + gatesOk
                    + " handler=" + handlerOk + " outbound=" + outboundOk + " dispose=" + disposeOk
                    + " pass=" + pass);
                return pass ? 0 : 3;
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY dragonball EXCEPTION " + e);
                return 3;
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.SetGiftInfo(oldGift, oldBuy);
                if (oldHasStatueOverview) model.SetStatueOverview(oldStatueStatus, oldStatuePower);
                PlatformModel.IsAlpha = oldAlpha;
                role.Level = oldLevel;
                hasBaseInfo?.SetValue(role, oldHasBaseInfo);
                if (wasInitialized) controller.Init();
                lastLevel?.SetValue(controller, oldLastLevel);
                if (intercept != null) intercept.SetValue(null, oldIntercept);
            }
        }

        private static bool EmptyFrames(IReadOnlyList<byte[]> frames, params int[] protocols)
        {
            if (frames.Count != protocols.Length) return false;
            for (int i = 0; i < protocols.Length; i++)
            {
                byte[] frame = frames[i];
                if (frame == null || frame.Length != 6 || frame[0] != 0 || frame[1] != 6
                    || frame[2] != 3 || frame[3] != 232
                    || frame[4] != (byte)(protocols[i] >> 8)
                    || frame[5] != (byte)(protocols[i] & 0xFF))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
