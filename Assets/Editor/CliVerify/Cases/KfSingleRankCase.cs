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
            var oldAreaTops = new Dictionary<byte, KfSingleRankModel.AreaSnapshot>(model.AreaTops);
            FieldInfo interceptField = typeof(KfSingleRankController).GetField("s_outboundIntercept", StaticNonPublic);
            object oldIntercept = interceptField == null ? null : interceptField.GetValue(null);

            try
            {
                controller.Init();
                model.Reset();

                MethodInfo onGameStart = typeof(KfSingleRankController).GetMethod("OnGameStart", InstanceNonPublic);
                MethodInfo on50701 = typeof(KfSingleRankController).GetMethod("On50701", InstanceNonPublic);
                MethodInfo on50703 = typeof(KfSingleRankController).GetMethod("On50703", InstanceNonPublic);
                IDictionary protocolHandlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                IDictionary eventHandlers = typeof(EventDispatcher).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                IList gameStartHandlers = eventHandlers?[GlobalEvent.EVT_GAME_START] as IList;
                int subscriptionCount = CountHandler(gameStartHandlers, controller, "OnGameStart");
                bool pass = interceptField != null && onGameStart != null && on50701 != null && on50703 != null
                    && protocolHandlers != null
                    && protocolHandlers.Contains(Proto.KF_SINGLE_RANK_INFO)
                    && protocolHandlers.Contains(Proto.KF_SINGLE_RANK_AREA_TOP)
                    && subscriptionCount == 1;
                int[] unsupported = { 50700, 50702, 50704, 50705 };
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
                model.ReplaceAreaTop(9, new List<KfSingleRankModel.AreaRankEntry>
                {
                    new KfSingleRankModel.AreaRankEntry(5, 4, "old", 3, 2)
                });
                onGameStart.Invoke(controller, null);
                pass &= !model.HasData && model.StartLevel == 0 && model.RewardState == 0
                    && model.Levels.Count == 0 && model.AreaTops.Count == 0
                    && InfoFrame(frames.Count == 1 ? frames[0] : null);
                frames.Clear();

                controller.RequestAreaTop(byte.MaxValue);
                pass &= AreaFrame(frames.Count == 1 ? frames[0] : null, byte.MaxValue);
                frames.Clear();

                pass &= Feed(on50701, controller, new CliVerify.Pkt().C(0).C(0).H(0).Bytes())
                    && model.HasData && model.StartLevel == 0 && model.RewardState == 0
                    && model.Levels.Count == 0 && model.AreaTops.Count == 0 && frames.Count == 0;

                byte[] firstInfo = new CliVerify.Pkt().C(0).C(byte.MaxValue).H(3)
                    .C(0).I(0)
                    .C(byte.MaxValue).I(4000000000L)
                    .C(byte.MaxValue).I(uint.MaxValue)
                    .Bytes();
                pass &= Feed(on50701, controller, firstInfo)
                    && model.HasData && model.StartLevel == 0 && model.RewardState == byte.MaxValue
                    && model.Levels.Count == 3
                    && model.Levels[0].Level == 0 && model.Levels[0].GoTime == 0
                    && model.Levels[1].Level == byte.MaxValue && model.Levels[1].GoTime == 4000000000U
                    && model.Levels[2].Level == byte.MaxValue && model.Levels[2].GoTime == uint.MaxValue
                    && frames.Count == 0;

                pass &= Feed(on50703, controller, new CliVerify.Pkt().C(1).H(0).Bytes())
                    && model.TryGetAreaTop(1, out KfSingleRankModel.AreaSnapshot areaOne)
                    && areaOne.AreaId == 1 && areaOne.Entries.Count == 0
                    && model.HasData && model.Levels.Count == 3 && frames.Count == 0;

                const string chineseName = "跨服中文名";
                byte[] areaTenBytes = new CliVerify.Pkt().C(10).H(3)
                    .C(0).L(0).S(string.Empty).H(0).I(0)
                    .C(byte.MaxValue).L(unchecked((long)ulong.MaxValue)).S(chineseName).H(ushort.MaxValue).I(4000000000L)
                    .C(byte.MaxValue).L(5000000001L).S("Dup").H(1).I(uint.MaxValue)
                    .Bytes();
                pass &= Feed(on50703, controller, areaTenBytes)
                    && model.TryGetAreaTop(10, out KfSingleRankModel.AreaSnapshot areaTen)
                    && areaTen.AreaId == 10 && areaTen.Entries.Count == 3
                    && areaTen.Entries[0].Level == 0 && areaTen.Entries[0].RoleId == 0
                    && areaTen.Entries[0].RoleName == string.Empty && areaTen.Entries[0].ServerNum == 0
                    && areaTen.Entries[0].GoTime == 0
                    && areaTen.Entries[1].Level == byte.MaxValue && areaTen.Entries[1].RoleId == ulong.MaxValue
                    && areaTen.Entries[1].RoleName == chineseName && areaTen.Entries[1].ServerNum == ushort.MaxValue
                    && areaTen.Entries[1].GoTime == 4000000000U
                    && areaTen.Entries[2].Level == byte.MaxValue && areaTen.Entries[2].RoleId == 5000000001UL
                    && areaTen.Entries[2].RoleName == "Dup" && areaTen.Entries[2].ServerNum == 1
                    && areaTen.Entries[2].GoTime == uint.MaxValue
                    && model.TryGetAreaTop(1, out areaOne) && areaOne.Entries.Count == 0
                    && model.HasData && model.Levels.Count == 3 && frames.Count == 0;

                pass &= Feed(on50701, controller, new CliVerify.Pkt().C(1).C(2).H(1).C(3).I(4).Bytes())
                    && model.HasData && model.StartLevel == 1 && model.RewardState == 2
                    && model.Levels.Count == 1 && model.Levels[0].Level == 3 && model.Levels[0].GoTime == 4
                    && model.AreaTops.Count == 2 && model.TryGetAreaTop(10, out areaTen)
                    && areaTen.Entries.Count == 3 && frames.Count == 0;

                byte[] replacement = new CliVerify.Pkt().C(10).H(1)
                    .C(5).L(6).S("替换").H(7).I(8).Bytes();
                pass &= Feed(on50703, controller, replacement)
                    && model.TryGetAreaTop(10, out KfSingleRankModel.AreaSnapshot replaced)
                    && replaced.Entries.Count == 1 && replaced.Entries[0].Level == 5
                    && replaced.Entries[0].RoleId == 6 && replaced.Entries[0].RoleName == "替换"
                    && replaced.Entries[0].ServerNum == 7 && replaced.Entries[0].GoTime == 8
                    && model.TryGetAreaTop(1, out _) && model.AreaTops.Count == 2
                    && model.HasData && model.Levels.Count == 1 && frames.Count == 0;

                pass &= Feed(on50703, controller, new CliVerify.Pkt().C(10).H(0).Bytes())
                    && model.TryGetAreaTop(10, out KfSingleRankModel.AreaSnapshot cleared)
                    && cleared.Entries.Count == 0 && model.TryGetAreaTop(1, out _)
                    && model.AreaTops.Count == 2 && model.HasData && model.Levels.Count == 1 && frames.Count == 0;

                pass &= Feed(on50701, controller, new CliVerify.Pkt().C(0).C(0).H(0).Bytes())
                    && model.HasData && model.StartLevel == 0 && model.RewardState == 0 && model.Levels.Count == 0
                    && model.AreaTops.Count == 2 && frames.Count == 0;

                controller.Dispose();
                gameStartHandlers = eventHandlers?[GlobalEvent.EVT_GAME_START] as IList;
                pass &= !controller.IsInitialized
                    && !protocolHandlers.Contains(Proto.KF_SINGLE_RANK_INFO)
                    && !protocolHandlers.Contains(Proto.KF_SINGLE_RANK_AREA_TOP)
                    && CountHandler(gameStartHandlers, controller, "OnGameStart") == 0
                    && !model.HasData && model.StartLevel == 0 && model.RewardState == 0
                    && model.Levels.Count == 0 && model.AreaTops.Count == 0;

                Debug.Log("CLIVERIFY kfsinglerank VERDICT pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                if (oldHasData) model.Replace(oldStartLevel, oldRewardState, oldLevels);
                foreach (KeyValuePair<byte, KfSingleRankModel.AreaSnapshot> entry in oldAreaTops)
                {
                    model.ReplaceAreaTop(entry.Key, new List<KfSingleRankModel.AreaRankEntry>(entry.Value.Entries));
                }
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

        private static bool InfoFrame(byte[] frame)
        {
            return frame != null && frame.Length == 6
                && frame[0] == 0 && frame[1] == 6 && frame[2] == 3 && frame[3] == 0xE8
                && frame[4] == (byte)(Proto.KF_SINGLE_RANK_INFO >> 8)
                && frame[5] == (byte)(Proto.KF_SINGLE_RANK_INFO & 0xFF);
        }

        private static bool AreaFrame(byte[] frame, byte areaId)
        {
            return frame != null && frame.Length == 7
                && frame[0] == 0 && frame[1] == 7 && frame[2] == 3 && frame[3] == 0xE8
                && frame[4] == (byte)(Proto.KF_SINGLE_RANK_AREA_TOP >> 8)
                && frame[5] == (byte)(Proto.KF_SINGLE_RANK_AREA_TOP & 0xFF)
                && frame[6] == areaId;
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
