using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.KfSingleRank;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class KfSingleRankCase
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
                Debug.LogError("CLIVERIFY kfsinglerank EXCEPTION " + exception);
                return Task.FromResult(3);
            }
        }

        private static int RunCore()
        {
            KfSingleRankController controller = KfSingleRankController.Instance;
            KfSingleRankModel model = KfSingleRankModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            bool oldHasData = model.HasData;
            byte oldStartLevel = model.StartLevel;
            byte oldRewardState = model.RewardState;
            var oldLevels = new List<KfSingleRankModel.LevelEntry>(model.Levels);
            FieldInfo interceptField = typeof(KfSingleRankController).GetField("s_outboundIntercept", StaticNonPublic);
            object oldIntercept = interceptField == null ? null : interceptField.GetValue(null);

            try
            {
                controller.Init();
                model.Reset();

                MethodInfo onGameStart = typeof(KfSingleRankController).GetMethod("OnGameStart", InstanceNonPublic);
                MethodInfo on50701 = typeof(KfSingleRankController).GetMethod("On50701", InstanceNonPublic);
                IDictionary protocolHandlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                IDictionary eventHandlers = typeof(EventDispatcher).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                IList gameStartHandlers = eventHandlers?[GlobalEvent.EVT_GAME_START] as IList;
                int subscriptionCount = CountHandler(gameStartHandlers, controller, "OnGameStart");
                bool pass = interceptField != null && onGameStart != null && on50701 != null
                    && protocolHandlers != null && protocolHandlers.Contains(Proto.KF_SINGLE_RANK_INFO)
                    && subscriptionCount == 1;
                int[] unsupported = { 50700, 50702, 50703, 50704, 50705 };
                foreach (int proto in unsupported) pass &= !protocolHandlers.Contains(proto);
                if (!pass) return 3;

                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));

                model.Replace(9, 8, new List<KfSingleRankModel.LevelEntry>
                {
                    new KfSingleRankModel.LevelEntry(7, 6)
                });
                onGameStart.Invoke(controller, null);
                pass &= !model.HasData && model.StartLevel == 0 && model.RewardState == 0 && model.Levels.Count == 0
                    && ExactFrame(frames.Count == 1 ? frames[0] : null);
                frames.Clear();

                pass &= Feed(on50701, controller, new CliVerify.Pkt().C(0).C(0).H(0).Bytes())
                    && model.HasData && model.StartLevel == 0 && model.RewardState == 0 && model.Levels.Count == 0
                    && frames.Count == 0;

                byte[] first = new CliVerify.Pkt().C(0).C(byte.MaxValue).H(3)
                    .C(0).I(0)
                    .C(byte.MaxValue).I(4000000000L)
                    .C(byte.MaxValue).I(uint.MaxValue)
                    .Bytes();
                pass &= Feed(on50701, controller, first)
                    && model.HasData && model.StartLevel == 0 && model.RewardState == byte.MaxValue
                    && model.Levels.Count == 3
                    && model.Levels[0].Level == 0 && model.Levels[0].GoTime == 0
                    && model.Levels[1].Level == byte.MaxValue && model.Levels[1].GoTime == 4000000000U
                    && model.Levels[2].Level == byte.MaxValue && model.Levels[2].GoTime == uint.MaxValue
                    && frames.Count == 0;

                pass &= Feed(on50701, controller, new CliVerify.Pkt().C(1).C(2).H(1).C(3).I(4).Bytes())
                    && model.HasData && model.StartLevel == 1 && model.RewardState == 2
                    && model.Levels.Count == 1 && model.Levels[0].Level == 3 && model.Levels[0].GoTime == 4
                    && frames.Count == 0;

                pass &= Feed(on50701, controller, new CliVerify.Pkt().C(0).C(0).H(0).Bytes())
                    && model.HasData && model.StartLevel == 0 && model.RewardState == 0 && model.Levels.Count == 0
                    && frames.Count == 0;

                controller.Dispose();
                gameStartHandlers = eventHandlers?[GlobalEvent.EVT_GAME_START] as IList;
                pass &= !controller.IsInitialized && !protocolHandlers.Contains(Proto.KF_SINGLE_RANK_INFO)
                    && CountHandler(gameStartHandlers, controller, "OnGameStart") == 0
                    && !model.HasData && model.StartLevel == 0 && model.RewardState == 0 && model.Levels.Count == 0;

                Debug.Log("CLIVERIFY kfsinglerank VERDICT pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                if (oldHasData) model.Replace(oldStartLevel, oldRewardState, oldLevels);
                if (wasInitialized) controller.Init();
                if (interceptField != null) interceptField.SetValue(null, oldIntercept);
            }
        }

        private static bool Feed(MethodInfo handler, KfSingleRankController controller, byte[] payload)
        {
            var reader = new NetReader(payload, 0, payload.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool ExactFrame(byte[] frame)
        {
            return frame != null && frame.Length == 6
                && frame[0] == 0 && frame[1] == 6 && frame[2] == 3 && frame[3] == 0xE8
                && frame[4] == (byte)(Proto.KF_SINGLE_RANK_INFO >> 8)
                && frame[5] == (byte)(Proto.KF_SINGLE_RANK_INFO & 0xFF);
        }

        private static int CountHandler(IList handlers, object target, string methodName)
        {
            if (handlers == null) return 0;
            int count = 0;
            foreach (object item in handlers)
            {
                if (item is Delegate handler && handler.Target == target && handler.Method.Name == methodName) count++;
            }
            return count;
        }
    }
}
