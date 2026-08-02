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
            try { return Task.FromResult(RunCore()); }
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
            var oldAreaTowers = new Dictionary<byte, KfSingleRankModel.AreaTowerSnapshot>(model.AreaTowers);
            bool oldHasSettlement = model.HasSettlement;
            byte oldSettlementResultType = model.SettlementResultType;
            byte oldSettlementLevel = model.SettlementLevel;
            uint oldSettlementGoTime = model.SettlementGoTime;
            byte oldSettlementBecameChallenger = model.SettlementBecameChallenger;
            var oldSettlementRewards = new List<KfSingleRankModel.SettlementReward>(model.SettlementRewards);
            FieldInfo interceptField = typeof(KfSingleRankController).GetField("s_outboundIntercept", StaticNonPublic);
            object oldIntercept = interceptField == null ? null : interceptField.GetValue(null);

            try
            {
                controller.Init();
                model.Reset();
                MethodInfo onGameStart = Handler("OnGameStart");
                MethodInfo on50701 = Handler("On50701");
                MethodInfo on50702 = Handler("On50702");
                MethodInfo on50703 = Handler("On50703");
                MethodInfo on50705 = Handler("On50705");
                IDictionary protocols = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                IDictionary events = typeof(EventDispatcher).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                IList gameStartHandlers = events?[GlobalEvent.EVT_GAME_START] as IList;
                bool pass = interceptField != null && onGameStart != null && on50701 != null && on50702 != null && on50703 != null && on50705 != null
                    && protocols != null && protocols.Contains(Proto.KF_SINGLE_RANK_INFO)
                    && protocols.Contains(Proto.KF_SINGLE_RANK_AREA_TOWERS)
                    && protocols.Contains(Proto.KF_SINGLE_RANK_AREA_TOP)
                    && protocols.Contains(Proto.KF_SINGLE_RANK_SETTLEMENT)
                    && typeof(KfSingleRankController).GetMethod("RequestSettlement", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly) == null
                    && CountHandler(gameStartHandlers, controller, "OnGameStart") == 1;
                foreach (int protocol in new[] { 50700, 50704 }) pass &= !protocols.Contains(protocol);
                if (!pass) return 3;

                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                model.Replace(9, 8, new List<KfSingleRankModel.LevelEntry> { new KfSingleRankModel.LevelEntry(7, 6) });
                model.ReplaceAreaTop(9, new List<KfSingleRankModel.AreaRankEntry> { new KfSingleRankModel.AreaRankEntry(5, 4, "old", 3, 2) });
                model.ReplaceAreaTowers(9, new List<KfSingleRankModel.AreaTowerEntry> { Tower(5, 4, "old", 3, 2, 1, 1, 1, 1, "pic", 1, 2) });
                model.ReplaceSettlement(1, 2, 3, 4, new List<KfSingleRankModel.SettlementReward> { Reward(5, 6, 7) });
                onGameStart.Invoke(controller, null);
                pass &= !model.HasData && model.StartLevel == 0 && model.RewardState == 0
                    && model.Levels.Count == 0 && model.AreaTops.Count == 0 && model.AreaTowers.Count == 0
                    && !model.HasSettlement && model.SettlementRewards.Count == 0
                    && InfoFrame(frames.Count == 1 ? frames[0] : null);
                frames.Clear();

                controller.RequestAreaTowers(byte.MaxValue);
                pass &= AreaFrame(frames.Count == 1 ? frames[0] : null, Proto.KF_SINGLE_RANK_AREA_TOWERS, byte.MaxValue);
                frames.Clear();
                controller.RequestAreaTop(byte.MaxValue);
                pass &= AreaFrame(frames.Count == 1 ? frames[0] : null, Proto.KF_SINGLE_RANK_AREA_TOP, byte.MaxValue);
                frames.Clear();

                pass &= Feed(on50701, controller, new CliVerify.Pkt().C(0).C(0).H(0).Bytes())
                    && Feed(on50702, controller, new CliVerify.Pkt().C(1).H(0).Bytes())
                    && Feed(on50703, controller, new CliVerify.Pkt().C(1).H(0).Bytes())
                    && model.TryGetAreaTowers(1, out KfSingleRankModel.AreaTowerSnapshot emptyTowers) && emptyTowers.Entries.Count == 0
                    && model.TryGetAreaTop(1, out KfSingleRankModel.AreaSnapshot emptyTop) && emptyTop.Entries.Count == 0 && frames.Count == 0;

                pass &= Feed(on50701, controller, new CliVerify.Pkt().C(0).C(byte.MaxValue).H(3)
                    .C(0).I(0).C(byte.MaxValue).I(4000000000L).C(byte.MaxValue).I(uint.MaxValue).Bytes())
                    && model.HasData && model.StartLevel == 0 && model.RewardState == byte.MaxValue
                    && model.Levels.Count == 3 && model.Levels[0].Level == 0 && model.Levels[0].GoTime == 0
                    && model.Levels[1].Level == byte.MaxValue && model.Levels[1].GoTime == 4000000000U
                    && model.Levels[2].Level == byte.MaxValue && model.Levels[2].GoTime == uint.MaxValue
                    && model.TryGetAreaTowers(1, out emptyTowers) && emptyTowers.Entries.Count == 0
                    && model.TryGetAreaTop(1, out emptyTop) && emptyTop.Entries.Count == 0;

                byte[] settlementMax = new CliVerify.Pkt().C(byte.MaxValue).C(byte.MaxValue).I(uint.MaxValue).C(byte.MaxValue).H(3)
                    .C(0).I(0).I(0)
                    .C(byte.MaxValue).I(uint.MaxValue).I(uint.MaxValue)
                    .C(byte.MaxValue).I(uint.MaxValue).I(uint.MaxValue).Bytes();
                pass &= Feed(on50705, controller, settlementMax)
                    && model.HasSettlement && model.SettlementResultType == byte.MaxValue && model.SettlementLevel == byte.MaxValue
                    && model.SettlementGoTime == uint.MaxValue && model.SettlementBecameChallenger == byte.MaxValue
                    && model.SettlementRewards.Count == 3
                    && SameReward(model.SettlementRewards[0], 0, 0, 0)
                    && SameReward(model.SettlementRewards[1], byte.MaxValue, uint.MaxValue, uint.MaxValue)
                    && SameReward(model.SettlementRewards[2], byte.MaxValue, uint.MaxValue, uint.MaxValue)
                    && model.HasData && model.Levels.Count == 3 && frames.Count == 0;
                KfSingleRankModel.SettlementReward oldFirstReward = model.SettlementRewards[0];

                const string chinese = "\u4e2d\u6587";
                byte[] areaTen = new CliVerify.Pkt().C(10).H(2)
                    .C(byte.MaxValue).L(0).S(string.Empty).H(0).H(0).H(0).C(0).C(0).C(0).S(chinese).C(0).I(0)
                    .C(byte.MaxValue).L(unchecked((long)ulong.MaxValue)).S(chinese).H(ushort.MaxValue).H(ushort.MaxValue).H(ushort.MaxValue)
                    .C(byte.MaxValue).C(byte.MaxValue).C(byte.MaxValue).S(string.Empty).C(byte.MaxValue).I(uint.MaxValue).Bytes();
                pass &= Feed(on50702, controller, areaTen)
                    && model.TryGetAreaTowers(10, out KfSingleRankModel.AreaTowerSnapshot towers) && towers.Entries.Count == 2
                    && towers.Entries[0].Level == byte.MaxValue && towers.Entries[0].RoleId == 0
                    && towers.Entries[0].RoleName == string.Empty && towers.Entries[0].ServerId == 0
                    && towers.Entries[0].ServerNum == 0 && towers.Entries[0].LevelValue == 0
                    && towers.Entries[0].Career == 0 && towers.Entries[0].Sex == 0 && towers.Entries[0].Turn == 0
                    && towers.Entries[0].Picture == chinese && towers.Entries[0].PictureVer == 0 && towers.Entries[0].GoTime == 0
                    && towers.Entries[1].Level == byte.MaxValue && towers.Entries[1].RoleId == ulong.MaxValue
                    && towers.Entries[1].RoleName == chinese && towers.Entries[1].ServerId == ushort.MaxValue
                    && towers.Entries[1].ServerNum == ushort.MaxValue && towers.Entries[1].LevelValue == ushort.MaxValue
                    && towers.Entries[1].Career == byte.MaxValue && towers.Entries[1].Sex == byte.MaxValue && towers.Entries[1].Turn == byte.MaxValue
                    && towers.Entries[1].Picture == string.Empty && towers.Entries[1].PictureVer == byte.MaxValue && towers.Entries[1].GoTime == uint.MaxValue
                    && model.TryGetAreaTowers(1, out emptyTowers) && emptyTowers.Entries.Count == 0;

                pass &= Feed(on50702, controller, TowerPacket(10, Tower(7, 6, "one", 5, 4, 3, 2, 1, 0, "p", 9, 8)))
                    && model.TryGetAreaTowers(10, out towers) && towers.Entries.Count == 1 && towers.Entries[0].Level == 7
                    && Feed(on50702, controller, new CliVerify.Pkt().C(10).H(0).Bytes())
                    && model.TryGetAreaTowers(10, out towers) && towers.Entries.Count == 0;

                byte[] areaTop = new CliVerify.Pkt().C(10).H(2)
                    .C(0).L(0).S(string.Empty).H(0).I(0)
                    .C(byte.MaxValue).L(unchecked((long)ulong.MaxValue)).S(chinese).H(ushort.MaxValue).I(uint.MaxValue).Bytes();
                pass &= Feed(on50703, controller, areaTop)
                    && model.TryGetAreaTop(10, out KfSingleRankModel.AreaSnapshot top) && top.Entries.Count == 2
                    && top.Entries[0].Level == 0 && top.Entries[0].RoleId == 0
                    && top.Entries[0].RoleName == string.Empty && top.Entries[0].ServerNum == 0 && top.Entries[0].GoTime == 0
                    && top.Entries[1].Level == byte.MaxValue && top.Entries[1].RoleId == ulong.MaxValue
                    && top.Entries[1].RoleName == chinese && top.Entries[1].ServerNum == ushort.MaxValue
                    && top.Entries[1].GoTime == uint.MaxValue
                    && model.TryGetAreaTowers(10, out towers) && towers.Entries.Count == 0
                    && Feed(on50701, controller, new CliVerify.Pkt().C(1).C(2).H(1).C(3).I(4).Bytes())
                    && model.AreaTops.Count == 2 && model.AreaTowers.Count == 2 && model.Levels.Count == 1
                    && model.TryGetAreaTop(10, out top) && top.Entries.Count == 2
                    && Feed(on50703, controller, new CliVerify.Pkt().C(10).H(1).C(5).L(6).S("top").H(7).I(8).Bytes())
                    && model.TryGetAreaTop(10, out top) && top.Entries.Count == 1
                    && top.Entries[0].Level == 5 && top.Entries[0].RoleId == 6
                    && top.Entries[0].RoleName == "top" && top.Entries[0].ServerNum == 7 && top.Entries[0].GoTime == 8
                    && Feed(on50703, controller, new CliVerify.Pkt().C(10).H(0).Bytes())
                    && model.TryGetAreaTop(10, out KfSingleRankModel.AreaSnapshot clearedTop) && clearedTop.Entries.Count == 0
                    && model.TryGetAreaTowers(10, out towers) && towers.Entries.Count == 0 && frames.Count == 0;

                pass &= model.HasSettlement && model.SettlementRewards.Count == 3
                    && SameReward(oldFirstReward, 0, 0, 0);

                pass &= Feed(on50705, controller, new CliVerify.Pkt().C(0).C(0).I(0).C(0).H(0).Bytes())
                    && model.HasSettlement && model.SettlementResultType == 0 && model.SettlementLevel == 0
                    && model.SettlementGoTime == 0 && model.SettlementBecameChallenger == 0 && model.SettlementRewards.Count == 0
                    && SameReward(oldFirstReward, 0, 0, 0)
                    && model.HasData && model.Levels.Count == 1 && model.AreaTops.Count == 2 && model.AreaTowers.Count == 2
                    && frames.Count == 0;

                controller.Dispose();
                gameStartHandlers = events?[GlobalEvent.EVT_GAME_START] as IList;
                pass &= !controller.IsInitialized && !protocols.Contains(Proto.KF_SINGLE_RANK_INFO)
                    && !protocols.Contains(Proto.KF_SINGLE_RANK_AREA_TOWERS) && !protocols.Contains(Proto.KF_SINGLE_RANK_AREA_TOP)
                    && !protocols.Contains(Proto.KF_SINGLE_RANK_SETTLEMENT)
                    && CountHandler(gameStartHandlers, controller, "OnGameStart") == 0
                    && !model.HasData && model.StartLevel == 0 && model.RewardState == 0
                    && model.Levels.Count == 0 && model.AreaTops.Count == 0 && model.AreaTowers.Count == 0
                    && !model.HasSettlement && model.SettlementResultType == 0 && model.SettlementLevel == 0
                    && model.SettlementGoTime == 0 && model.SettlementBecameChallenger == 0 && model.SettlementRewards.Count == 0;
                Debug.Log("CLIVERIFY kfsinglerank VERDICT pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                if (oldHasData) model.Replace(oldStartLevel, oldRewardState, oldLevels);
                foreach (KeyValuePair<byte, KfSingleRankModel.AreaSnapshot> pair in oldAreaTops)
                    model.ReplaceAreaTop(pair.Key, new List<KfSingleRankModel.AreaRankEntry>(pair.Value.Entries));
                foreach (KeyValuePair<byte, KfSingleRankModel.AreaTowerSnapshot> pair in oldAreaTowers)
                    model.ReplaceAreaTowers(pair.Key, new List<KfSingleRankModel.AreaTowerEntry>(pair.Value.Entries));
                if (oldHasSettlement)
                    model.ReplaceSettlement(oldSettlementResultType, oldSettlementLevel, oldSettlementGoTime, oldSettlementBecameChallenger, oldSettlementRewards);
                if (wasInitialized) controller.Init();
                if (interceptField != null) interceptField.SetValue(null, oldIntercept);
            }
        }

        private static MethodInfo Handler(string name) => typeof(KfSingleRankController).GetMethod(name, InstanceNonPublic);
        private static KfSingleRankModel.AreaTowerEntry Tower(byte level, ulong id, string name, ushort serverId, ushort serverNum, ushort lv, byte career, byte sex, byte turn, string picture, byte version, uint time)
            => new KfSingleRankModel.AreaTowerEntry(level, id, name, serverId, serverNum, lv, career, sex, turn, picture, version, time);
        private static KfSingleRankModel.SettlementReward Reward(byte type, uint typeId, uint num)
            => new KfSingleRankModel.SettlementReward(type, typeId, num);
        private static bool SameReward(KfSingleRankModel.SettlementReward reward, byte type, uint typeId, uint num)
            => reward != null && reward.Type == type && reward.TypeId == typeId && reward.Num == num;
        private static byte[] TowerPacket(byte area, KfSingleRankModel.AreaTowerEntry entry)
            => new CliVerify.Pkt().C(area).H(1).C(entry.Level).L(unchecked((long)entry.RoleId)).S(entry.RoleName).H(entry.ServerId).H(entry.ServerNum).H(entry.LevelValue).C(entry.Career).C(entry.Sex).C(entry.Turn).S(entry.Picture).C(entry.PictureVer).I(entry.GoTime).Bytes();
        private static bool Feed(MethodInfo method, KfSingleRankController controller, byte[] payload) { var reader = new NetReader(payload, 0, payload.Length); method.Invoke(controller, new object[] { reader }); return reader.Remaining == 0; }
        private static bool InfoFrame(byte[] frame) => frame != null && frame.Length == 6 && frame[0] == 0 && frame[1] == 6 && frame[2] == 3 && frame[3] == 0xE8 && frame[4] == (byte)(Proto.KF_SINGLE_RANK_INFO >> 8) && frame[5] == (byte)(Proto.KF_SINGLE_RANK_INFO & 0xFF);
        private static bool AreaFrame(byte[] frame, int protocol, byte areaId) => frame != null && frame.Length == 7 && frame[0] == 0 && frame[1] == 7 && frame[2] == 3 && frame[3] == 0xE8 && frame[4] == (byte)(protocol >> 8) && frame[5] == (byte)(protocol & 0xFF) && frame[6] == areaId;
        private static int CountHandler(IList handlers, object target, string methodName) { if (handlers == null) return 0; int count = 0; foreach (object item in handlers) if (item is Delegate handler && handler.Target == target && handler.Method.Name == methodName) count++; return count; }
    }
}
