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
            bool oldHasExperience = model.HasExperience;
            ulong oldAllExperience = model.AllExperience;
            bool oldHasScore = model.HasScore;
            uint oldPoint = model.Point;
            var oldServers = new List<HolyBattleModel.ServerEntry>(model.Servers);
            var oldRewards = new List<HolyBattleModel.RewardEntry>(model.Rewards);
            bool oldHasRecordStats = model.HasRecordStats;
            var oldRecordStats = new List<HolyBattleModel.RecordGroupEntry>(model.RecordStats);
            bool oldHasPhaseTime = model.HasPhaseTime;
            byte oldPhaseStatus = model.PhaseStatus;
            uint oldPhaseEndTime = model.PhaseEndTime;
            bool oldHasFightState = model.HasFightState;
            ushort oldFightPoint = model.FightPoint;
            ushort oldSingleRank = model.SingleRank;
            byte oldGroupRank = model.GroupRank;
            byte oldAnger = model.Anger;
            uint oldAngerEnd = model.AngerEnd;
            var oldBuffs = new List<HolyBattleModel.BuffEntry>(model.Buffs);
            bool oldHasMonsterInfo = model.HasMonsterInfo;
            var oldMonsters = new List<HolyBattleModel.MonsterEntry>(model.MonstersByCfgId.Values);
            bool oldHasDeathInfo = model.HasDeathInfo;
            string oldDeathRoleName = model.DeathRoleName;
            ulong oldDeathRoleId = model.DeathRoleId;
            ushort oldDeathLevel = model.DeathLevel;
            ulong oldDeathPower = model.DeathPower;
            uint oldDeathPictureVersion = model.DeathPictureVersion;
            string oldDeathPicture = model.DeathPicture;
            uint oldDeathAnger = model.DeathAnger;
            uint oldDeathServerId = model.DeathServerId;
            byte oldDeathCareer = model.DeathCareer;
            byte oldDeathTurn = model.DeathTurn;
            bool oldHasResultInfo = model.HasResultInfo;
            byte oldResultCode = model.ResultCode;
            byte oldResultMyGroupId = model.ResultMyGroupId;
            byte oldResultMyRank = model.ResultMyRank;
            var oldResultGroups = new List<HolyBattleModel.ResultGroupEntry>(model.ResultGroups);
            FieldInfo interceptField = typeof(HolyBattleController).GetField("s_outboundIntercept", StaticNonPublic);
            object oldIntercept = interceptField == null ? null : interceptField.GetValue(null);
            int[] handlerIds = { 21801, 21804, 21805, 21807, 21808, 21809, 21810, 21811, 21813 };
            IDictionary originalHandlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
            var handlerSnapshot = new Dictionary<int, object>();
            if (originalHandlers != null)
            {
                foreach (int id in handlerIds)
                {
                    if (originalHandlers.Contains(id))
                    {
                        handlerSnapshot[id] = originalHandlers[id];
                    }
                }
            }

            try
            {
                controller.Init();
                model.Reset();

                MethodInfo on21801 = typeof(HolyBattleController).GetMethod("On21801", InstanceNonPublic);
                MethodInfo on21804 = typeof(HolyBattleController).GetMethod("On21804", InstanceNonPublic);
                MethodInfo on21805 = typeof(HolyBattleController).GetMethod("On21805", InstanceNonPublic);
                MethodInfo on21808 = typeof(HolyBattleController).GetMethod("On21808", InstanceNonPublic);
                MethodInfo on21811 = typeof(HolyBattleController).GetMethod("On21811", InstanceNonPublic);
                MethodInfo on21807 = typeof(HolyBattleController).GetMethod("On21807", InstanceNonPublic);
                MethodInfo on21809 = typeof(HolyBattleController).GetMethod("On21809", InstanceNonPublic);
                MethodInfo on21810 = typeof(HolyBattleController).GetMethod("On21810", InstanceNonPublic);
                MethodInfo on21813 = typeof(HolyBattleController).GetMethod("On21813", InstanceNonPublic);
                IDictionary handlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                bool pass = interceptField != null && on21801 != null && on21804 != null && on21805 != null && on21807 != null && on21808 != null && on21809 != null && on21810 != null && on21811 != null && on21813 != null && handlers != null
                    && handlers.Contains(21801) && handlers.Contains(21804) && handlers.Contains(21805) && handlers.Contains(21807) && handlers.Contains(21808) && handlers.Contains(21809) && handlers.Contains(21810) && handlers.Contains(21811) && handlers.Contains(21813)
                    && typeof(HolyBattleController).GetMethod("RequestFightState") == null && typeof(HolyBattleController).GetMethod("RequestDeathInfo") == null && typeof(HolyBattleController).GetMethod("RequestResultInfo") == null;
                for (int proto = 21800; proto <= 21813; proto++)
                {
                    if (proto != 21801 && proto != 21804 && proto != 21805 && proto != 21807 && proto != 21808 && proto != 21809 && proto != 21810 && proto != 21811 && proto != 21813)
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

                controller.RequestExperience();
                pass &= IsExactExperienceRequest(frames.Count == 1 ? frames[0] : null)
                    && !model.HasExperience && model.AllExperience == 0;
                frames.Clear();

                controller.RequestScore();
                pass &= IsExactScoreRequest(frames.Count == 1 ? frames[0] : null)
                    && !model.HasScore && model.Point == 0 && model.Rewards.Count == 0;
                frames.Clear();

                controller.RequestRecordStats();
                pass &= IsExactRecordStatsRequest(frames.Count == 1 ? frames[0] : null)
                    && !model.HasRecordStats && model.RecordStats.Count == 0;
                frames.Clear();

                controller.RequestPhaseTime();
                pass &= IsExactPhaseTimeRequest(frames.Count == 1 ? frames[0] : null)
                    && !model.HasPhaseTime && model.PhaseStatus == 0 && model.PhaseEndTime == 0;
                frames.Clear();

                controller.RequestMonsterInfo();
                pass &= IsExactMonsterInfoRequest(frames.Count == 1 ? frames[0] : null)
                    && !model.HasMonsterInfo && model.MonstersByCfgId.Count == 0;
                frames.Clear();

                var phaseZeroReader = new NetReader(new CliVerify.Pkt().C(0).I(0).Bytes(), 0, 5);
                on21811.Invoke(controller, new object[] { phaseZeroReader });
                pass &= phaseZeroReader.Remaining == 0 && model.HasPhaseTime && model.PhaseStatus == 0 && model.PhaseEndTime == 0;
                var phaseWaitReader = new NetReader(new CliVerify.Pkt().C(1).I(uint.MaxValue).Bytes(), 0, 5);
                on21811.Invoke(controller, new object[] { phaseWaitReader });
                pass &= phaseWaitReader.Remaining == 0 && model.HasPhaseTime && model.PhaseStatus == 1 && model.PhaseEndTime == uint.MaxValue;
                var phaseFightReader = new NetReader(new CliVerify.Pkt().C(2).I(7).Bytes(), 0, 5);
                on21811.Invoke(controller, new object[] { phaseFightReader });
                pass &= phaseFightReader.Remaining == 0 && model.HasPhaseTime && model.PhaseStatus == 2 && model.PhaseEndTime == 7 && frames.Count == 0;

                byte[] fightBytes = new CliVerify.Pkt().H(ushort.MaxValue).H(ushort.MaxValue).C(byte.MaxValue).C(byte.MaxValue).I(uint.MaxValue).H(3)
                    .H(0).I(0).H(ushort.MaxValue).I(uint.MaxValue).H(ushort.MaxValue).I(uint.MaxValue).Bytes();
                var fightReader = new NetReader(fightBytes, 0, fightBytes.Length);
                on21807.Invoke(controller, new object[] { fightReader });
                pass &= fightReader.Remaining == 0 && model.HasFightState && model.FightPoint == ushort.MaxValue && model.SingleRank == ushort.MaxValue
                    && model.GroupRank == byte.MaxValue && model.Anger == byte.MaxValue && model.AngerEnd == uint.MaxValue && model.Buffs.Count == 3
                    && model.Buffs[0].AttrId == 0 && model.Buffs[0].Value == 0
                    && model.Buffs[1].AttrId == ushort.MaxValue && model.Buffs[1].Value == uint.MaxValue
                    && model.Buffs[2].AttrId == ushort.MaxValue && model.Buffs[2].Value == uint.MaxValue && frames.Count == 0;

                controller.RequestPhaseTime();
                pass &= IsExactPhaseTimeRequest(frames.Count == 1 ? frames[0] : null)
                    && model.HasPhaseTime && model.PhaseStatus == 2 && model.PhaseEndTime == 7
                    && model.HasFightState && model.Buffs.Count == 3;
                frames.Clear();

                byte[] statsBytes = new CliVerify.Pkt().H(2)
                    .C(byte.MaxValue).C(0).I(0).C(0).H(0)
                    .C(byte.MaxValue).C(byte.MaxValue).I(uint.MaxValue).C(byte.MaxValue).H(3)
                    .L(1).C(1).I(2).I(3).S("\u4e2d").I(10).H(0).H(ushort.MaxValue)
                    .L(unchecked((long)ulong.MaxValue)).C(byte.MaxValue).I(uint.MaxValue).I(uint.MaxValue).S("\u4e2d").I(uint.MaxValue).H(ushort.MaxValue).H(ushort.MaxValue)
                    .L(1).C(2).I(2).I(3).S("same").I(uint.MaxValue).H(1).H(2).Bytes();
                var statsReader = new NetReader(statsBytes, 0, statsBytes.Length);
                on21808.Invoke(controller, new object[] { statsReader });
                pass &= statsReader.Remaining == 0 && model.HasRecordStats && model.RecordStats.Count == 2
                    && model.RecordStats[0].GroupId == byte.MaxValue && model.RecordStats[0].Roles.Count == 0
                    && model.RecordStats[1].GroupId == byte.MaxValue && model.RecordStats[1].TowerNum == byte.MaxValue
                    && model.RecordStats[1].Point == uint.MaxValue && model.RecordStats[1].Rank == byte.MaxValue
                    && model.RecordStats[1].Roles.Count == 3
                    && model.RecordStats[1].Roles[0].RoleId == ulong.MaxValue && model.RecordStats[1].Roles[0].Rank == byte.MaxValue
                    && model.RecordStats[1].Roles[0].ServerId == uint.MaxValue && model.RecordStats[1].Roles[0].ServerNum == uint.MaxValue
                    && model.RecordStats[1].Roles[0].Name == "\u4e2d" && model.RecordStats[1].Roles[0].Point == uint.MaxValue
                    && model.RecordStats[1].Roles[0].Kill == ushort.MaxValue && model.RecordStats[1].Roles[0].Assists == ushort.MaxValue
                    && model.RecordStats[1].Roles[1].RoleId == 1 && model.RecordStats[1].Roles[1].Rank == 2
                    && model.RecordStats[1].Roles[1].ServerId == 2 && model.RecordStats[1].Roles[1].ServerNum == 3
                    && model.RecordStats[1].Roles[1].Name == "same" && model.RecordStats[1].Roles[1].Point == uint.MaxValue
                    && model.RecordStats[1].Roles[1].Kill == 1 && model.RecordStats[1].Roles[1].Assists == 2
                    && model.RecordStats[1].Roles[2].RoleId == 1 && model.RecordStats[1].Roles[2].Rank == 1
                    && model.RecordStats[1].Roles[2].ServerId == 2 && model.RecordStats[1].Roles[2].ServerNum == 3
                    && model.RecordStats[1].Roles[2].Name == "\u4e2d" && model.RecordStats[1].Roles[2].Point == 10
                    && model.RecordStats[1].Roles[2].Kill == 0 && model.RecordStats[1].Roles[2].Assists == ushort.MaxValue
                    && model.HasPhaseTime && model.PhaseStatus == 2 && model.PhaseEndTime == 7
                    && model.HasFightState && model.Buffs.Count == 3
                    && frames.Count == 0;

                var zeroExperienceReader = new NetReader(new CliVerify.Pkt().L(0).Bytes(), 0, 8);
                on21804.Invoke(controller, new object[] { zeroExperienceReader });
                pass &= zeroExperienceReader.Remaining == 0 && model.HasExperience && model.AllExperience == 0
                    && model.HasRecordStats && model.RecordStats.Count == 2
                    && model.HasPhaseTime && model.PhaseStatus == 2 && model.PhaseEndTime == 7
                    && model.HasFightState && model.Buffs.Count == 3 && frames.Count == 0;

                var firstExperienceReader = new NetReader(new CliVerify.Pkt().L(100).Bytes(), 0, 8);
                on21804.Invoke(controller, new object[] { firstExperienceReader });
                var replacementExperienceReader = new NetReader(new CliVerify.Pkt().L(130).Bytes(), 0, 8);
                on21804.Invoke(controller, new object[] { replacementExperienceReader });
                pass &= firstExperienceReader.Remaining == 0 && replacementExperienceReader.Remaining == 0
                    && model.HasExperience && model.AllExperience == 130;

                controller.RequestExperience();
                pass &= IsExactExperienceRequest(frames.Count == 1 ? frames[0] : null)
                    && model.HasExperience && model.AllExperience == 130;
                frames.Clear();

                var highExperienceReader = new NetReader(new CliVerify.Pkt().L(5000000001L).Bytes(), 0, 8);
                on21804.Invoke(controller, new object[] { highExperienceReader });
                pass &= highExperienceReader.Remaining == 0 && model.AllExperience == 5000000001UL && frames.Count == 0;

                byte[] firstScoreBytes = new CliVerify.Pkt().I(0).H(3)
                    .H(0).C(0).H(ushort.MaxValue).C(1).H(ushort.MaxValue).C(2).Bytes();
                var firstScoreReader = new NetReader(firstScoreBytes, 0, firstScoreBytes.Length);
                on21805.Invoke(controller, new object[] { firstScoreReader });
                pass &= firstScoreReader.Remaining == 0 && model.HasScore && model.Point == 0 && model.Rewards.Count == 3
                    && model.Rewards[0].Stage == 0 && model.Rewards[0].Status == 0
                    && model.Rewards[1].Stage == ushort.MaxValue && model.Rewards[1].Status == 1
                    && model.Rewards[2].Stage == ushort.MaxValue && model.Rewards[2].Status == 2
                    && model.HasExperience && model.AllExperience == 5000000001UL && !model.HasData
                    && model.HasRecordStats && model.RecordStats.Count == 2
                    && model.HasPhaseTime && model.PhaseStatus == 2 && model.PhaseEndTime == 7
                    && model.HasFightState && model.Buffs.Count == 3 && frames.Count == 0;

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
                    && model.HasExperience && model.AllExperience == 5000000001UL
                    && model.HasScore && model.Point == 0 && model.Rewards.Count == 3
                    && model.HasRecordStats && model.RecordStats.Count == 2
                    && model.HasPhaseTime && model.PhaseStatus == 2 && model.PhaseEndTime == 7
                    && model.HasFightState && model.Buffs.Count == 3
                    && frames.Count == 0;

                byte[] monsterBytes = new CliVerify.Pkt().H(2)
                    .I(uint.MaxValue).I(uint.MaxValue).I(uint.MaxValue).I(0).C(byte.MaxValue)
                    .I(0).I(1).I(2).I(uint.MaxValue).C(0).Bytes();
                var monsterReader = new NetReader(monsterBytes, 0, monsterBytes.Length);
                on21813.Invoke(controller, new object[] { monsterReader });
                pass &= monsterReader.Remaining == 0 && model.HasMonsterInfo && model.MonstersByCfgId.Count == 2
                    && model.MonstersByCfgId[uint.MaxValue].MonAuto == uint.MaxValue && model.MonstersByCfgId[uint.MaxValue].MonCfgId == uint.MaxValue
                    && model.MonstersByCfgId[uint.MaxValue].Hp == uint.MaxValue && model.MonstersByCfgId[uint.MaxValue].HpAll == 0 && model.MonstersByCfgId[uint.MaxValue].GroupId == byte.MaxValue
                    && model.MonstersByCfgId[1].MonAuto == 0 && model.MonstersByCfgId[1].Hp == 2 && model.MonstersByCfgId[1].HpAll == uint.MaxValue && model.MonstersByCfgId[1].GroupId == 0
                    && model.HasData && model.Mod == 255 && model.Status == 254 && model.EndTime == uint.MaxValue && model.Servers.Count == 2
                    && model.HasExperience && model.AllExperience == 5000000001UL
                    && model.HasScore && model.Point == 0 && model.Rewards.Count == 3
                    && model.HasRecordStats && model.RecordStats.Count == 2
                    && model.HasPhaseTime && model.PhaseStatus == 2 && model.PhaseEndTime == 7
                    && model.HasFightState && model.FightPoint == ushort.MaxValue && model.SingleRank == ushort.MaxValue
                    && model.GroupRank == byte.MaxValue && model.Anger == byte.MaxValue && model.AngerEnd == uint.MaxValue && model.Buffs.Count == 3
                    && frames.Count == 0;

                byte[] replacementMonsterBytes = new CliVerify.Pkt().H(1).I(7).I(1).I(10).I(20).C(3).Bytes();
                var replacementMonsterReader = new NetReader(replacementMonsterBytes, 0, replacementMonsterBytes.Length);
                on21813.Invoke(controller, new object[] { replacementMonsterReader });
                pass &= replacementMonsterReader.Remaining == 0 && model.HasMonsterInfo && model.MonstersByCfgId.Count == 2
                    && model.MonstersByCfgId[1].MonAuto == 7 && model.MonstersByCfgId[1].Hp == 10 && model.MonstersByCfgId[1].HpAll == 20 && model.MonstersByCfgId[1].GroupId == 3
                    && model.MonstersByCfgId.ContainsKey(uint.MaxValue) && model.HasData && model.HasExperience && model.HasScore && model.HasRecordStats && model.HasPhaseTime && model.HasFightState;

                byte[] deletedMonsterBytes = new CliVerify.Pkt().H(2).I(8).I(1).I(0).I(20).C(3).I(9).I(99).I(0).I(0).C(0).Bytes();
                var deletedMonsterReader = new NetReader(deletedMonsterBytes, 0, deletedMonsterBytes.Length);
                on21813.Invoke(controller, new object[] { deletedMonsterReader });
                pass &= deletedMonsterReader.Remaining == 0 && model.HasMonsterInfo && model.MonstersByCfgId.Count == 1
                    && !model.MonstersByCfgId.ContainsKey(1) && !model.MonstersByCfgId.ContainsKey(99) && model.MonstersByCfgId.ContainsKey(uint.MaxValue);

                var emptyMonsterReader = new NetReader(new CliVerify.Pkt().H(0).Bytes(), 0, 2);
                on21813.Invoke(controller, new object[] { emptyMonsterReader });
                pass &= emptyMonsterReader.Remaining == 0 && model.HasMonsterInfo && model.MonstersByCfgId.Count == 1 && model.MonstersByCfgId.ContainsKey(uint.MaxValue);
                controller.RequestMonsterInfo();
                pass &= IsExactMonsterInfoRequest(frames.Count == 1 ? frames[0] : null)
                    && model.HasMonsterInfo && model.MonstersByCfgId.Count == 1 && model.MonstersByCfgId.ContainsKey(uint.MaxValue);
                frames.Clear();

                const string deathName = "死亡🙂";
                const string deathPicture = "图像";
                byte[] deathBytes = new CliVerify.Pkt().S(deathName).L(unchecked((long)ulong.MaxValue)).H(ushort.MaxValue)
                    .L(unchecked((long)ulong.MaxValue)).I(uint.MaxValue).S(deathPicture).I(uint.MaxValue).I(uint.MaxValue).C(byte.MaxValue).C(byte.MaxValue).Bytes();
                var deathReader = new NetReader(deathBytes, 0, deathBytes.Length);
                on21809.Invoke(controller, new object[] { deathReader });
                pass &= deathReader.Remaining == 0 && model.HasDeathInfo && model.DeathRoleName == deathName && model.DeathRoleId == ulong.MaxValue
                    && model.DeathLevel == ushort.MaxValue && model.DeathPower == ulong.MaxValue && model.DeathPictureVersion == uint.MaxValue
                    && model.DeathPicture == deathPicture && model.DeathAnger == uint.MaxValue && model.DeathServerId == uint.MaxValue
                    && model.DeathCareer == byte.MaxValue && model.DeathTurn == byte.MaxValue
                    && model.HasData && model.Mod == 255 && model.Status == 254 && model.EndTime == uint.MaxValue && model.Servers.Count == 2
                    && model.HasExperience && model.AllExperience == 5000000001UL && model.HasScore && model.Point == 0 && model.Rewards.Count == 3
                    && model.HasRecordStats && model.RecordStats.Count == 2 && model.RecordStats[1].Roles.Count == 3
                    && model.HasPhaseTime && model.PhaseStatus == 2 && model.PhaseEndTime == 7
                    && model.HasFightState && model.FightPoint == ushort.MaxValue && model.SingleRank == ushort.MaxValue
                    && model.GroupRank == byte.MaxValue && model.Anger == byte.MaxValue && model.AngerEnd == uint.MaxValue && model.Buffs.Count == 3
                    && model.HasMonsterInfo && model.MonstersByCfgId.Count == 1 && model.MonstersByCfgId.ContainsKey(uint.MaxValue)
                    && model.MonstersByCfgId[uint.MaxValue].MonAuto == uint.MaxValue && model.MonstersByCfgId[uint.MaxValue].Hp == uint.MaxValue
                    && model.MonstersByCfgId[uint.MaxValue].HpAll == 0 && model.MonstersByCfgId[uint.MaxValue].GroupId == byte.MaxValue
                    && frames.Count == 0;

                byte[] smallDeathBytes = new CliVerify.Pkt().S("A").L(1).H(2).L(3).I(4).S("p").I(5).I(6).C(7).C(8).Bytes();
                var smallDeathReader = new NetReader(smallDeathBytes, 0, smallDeathBytes.Length);
                on21809.Invoke(controller, new object[] { smallDeathReader });
                pass &= smallDeathReader.Remaining == 0 && model.HasDeathInfo && model.DeathRoleName == "A" && model.DeathRoleId == 1 && model.DeathLevel == 2
                    && model.DeathPower == 3 && model.DeathPictureVersion == 4 && model.DeathPicture == "p" && model.DeathAnger == 5
                    && model.DeathServerId == 6 && model.DeathCareer == 7 && model.DeathTurn == 8;

                byte[] zeroDeathBytes = new CliVerify.Pkt().S("").L(0).H(0).L(0).I(0).S("").I(0).I(0).C(0).C(0).Bytes();
                var zeroDeathReader = new NetReader(zeroDeathBytes, 0, zeroDeathBytes.Length);
                on21809.Invoke(controller, new object[] { zeroDeathReader });
                pass &= zeroDeathReader.Remaining == 0 && model.HasDeathInfo && model.DeathRoleName == "" && model.DeathRoleId == 0 && model.DeathLevel == 0
                    && model.DeathPower == 0 && model.DeathPictureVersion == 0 && model.DeathPicture == "" && model.DeathAnger == 0
                    && model.DeathServerId == 0 && model.DeathCareer == 0 && model.DeathTurn == 0;

                byte[] resultBytes = new CliVerify.Pkt().C(byte.MaxValue).H(2)
                    .C(byte.MaxValue).C(byte.MaxValue).I(uint.MaxValue).C(byte.MaxValue).C(0).I(0)
                    .C(byte.MaxValue).C(byte.MaxValue).Bytes();
                var resultReader = new NetReader(resultBytes, 0, resultBytes.Length);
                on21810.Invoke(controller, new object[] { resultReader });
                pass &= resultReader.Remaining == 0 && model.HasResultInfo && model.ResultCode == byte.MaxValue && model.ResultMyGroupId == byte.MaxValue && model.ResultMyRank == byte.MaxValue
                    && model.ResultGroups.Count == 2 && model.ResultGroups[0].GroupId == byte.MaxValue && model.ResultGroups[0].TowerNum == byte.MaxValue && model.ResultGroups[0].Point == uint.MaxValue
                    && model.ResultGroups[1].GroupId == byte.MaxValue && model.ResultGroups[1].TowerNum == 0 && model.ResultGroups[1].Point == 0
                    && model.HasData && model.Mod == 255 && model.Status == 254 && model.EndTime == uint.MaxValue && model.Servers.Count == 2
                    && model.HasExperience && model.AllExperience == 5000000001UL && model.HasScore && model.Point == 0 && model.Rewards.Count == 3
                    && model.HasRecordStats && model.RecordStats.Count == 2 && model.RecordStats[1].Roles.Count == 3
                    && model.HasPhaseTime && model.PhaseStatus == 2 && model.PhaseEndTime == 7
                    && model.HasFightState && model.FightPoint == ushort.MaxValue && model.SingleRank == ushort.MaxValue
                    && model.GroupRank == byte.MaxValue && model.Anger == byte.MaxValue && model.AngerEnd == uint.MaxValue && model.Buffs.Count == 3
                    && model.HasMonsterInfo && model.MonstersByCfgId.Count == 1 && model.MonstersByCfgId.ContainsKey(uint.MaxValue)
                    && model.MonstersByCfgId[uint.MaxValue].MonAuto == uint.MaxValue && model.MonstersByCfgId[uint.MaxValue].Hp == uint.MaxValue
                    && model.MonstersByCfgId[uint.MaxValue].HpAll == 0 && model.MonstersByCfgId[uint.MaxValue].GroupId == byte.MaxValue
                    && model.HasDeathInfo && model.DeathRoleName == "" && model.DeathRoleId == 0 && model.DeathLevel == 0 && model.DeathPower == 0
                    && model.DeathPictureVersion == 0 && model.DeathPicture == "" && model.DeathAnger == 0 && model.DeathServerId == 0 && model.DeathCareer == 0 && model.DeathTurn == 0 && frames.Count == 0;

                byte[] smallResultBytes = new CliVerify.Pkt().C(1).H(1).C(2).C(3).I(4).C(5).C(6).Bytes();
                var smallResultReader = new NetReader(smallResultBytes, 0, smallResultBytes.Length);
                on21810.Invoke(controller, new object[] { smallResultReader });
                pass &= smallResultReader.Remaining == 0 && model.HasResultInfo && model.ResultCode == 1 && model.ResultMyGroupId == 5 && model.ResultMyRank == 6
                    && model.ResultGroups.Count == 1 && model.ResultGroups[0].GroupId == 2 && model.ResultGroups[0].TowerNum == 3 && model.ResultGroups[0].Point == 4;

                var emptyResultReader = new NetReader(new CliVerify.Pkt().C(0).H(0).C(7).C(8).Bytes(), 0, 5);
                on21810.Invoke(controller, new object[] { emptyResultReader });
                pass &= emptyResultReader.Remaining == 0 && model.HasResultInfo && model.ResultCode == 0 && model.ResultGroups.Count == 0 && model.ResultMyGroupId == 7 && model.ResultMyRank == 8;

                var maxExperienceReader = new NetReader(new CliVerify.Pkt().L(unchecked((long)ulong.MaxValue)).Bytes(), 0, 8);
                on21804.Invoke(controller, new object[] { maxExperienceReader });
                pass &= maxExperienceReader.Remaining == 0 && model.HasExperience && model.AllExperience == ulong.MaxValue
                    && model.HasData && model.Mod == 255 && model.Servers.Count == 2 && model.HasDeathInfo && model.DeathRoleName == ""
                    && model.DeathRoleId == 0 && model.DeathLevel == 0 && model.DeathPower == 0 && model.DeathPictureVersion == 0 && model.DeathPicture == ""
                    && model.DeathAnger == 0 && model.DeathServerId == 0 && model.DeathCareer == 0 && model.DeathTurn == 0
                    && model.HasResultInfo && model.ResultCode == 0 && model.ResultGroups.Count == 0 && model.ResultMyGroupId == 7 && model.ResultMyRank == 8 && frames.Count == 0;

                byte[] secondBytes = new CliVerify.Pkt()
                    .C(1).C(2).I(3).H(1).I(4).I(5).S("替换服").I(6)
                    .Bytes();
                var secondReader = new NetReader(secondBytes, 0, secondBytes.Length);
                on21801.Invoke(controller, new object[] { secondReader });
                pass &= secondReader.Remaining == 0
                    && model.HasData && model.Mod == 1 && model.Status == 2 && model.EndTime == 3
                    && model.Servers.Count == 1 && model.Servers[0].ServerId == 4
                    && model.Servers[0].ServerNumber == 5 && model.Servers[0].ServerName == "替换服"
                    && model.Servers[0].Level == 6 && model.HasExperience && model.AllExperience == ulong.MaxValue
                    && model.HasScore && model.Rewards.Count == 3;

                byte[] replacementScoreBytes = new CliVerify.Pkt().I(uint.MaxValue).H(1).H(9).C(2).Bytes();
                var replacementScoreReader = new NetReader(replacementScoreBytes, 0, replacementScoreBytes.Length);
                on21805.Invoke(controller, new object[] { replacementScoreReader });
                pass &= replacementScoreReader.Remaining == 0 && model.HasScore && model.Point == uint.MaxValue
                    && model.Rewards.Count == 1 && model.Rewards[0].Stage == 9 && model.Rewards[0].Status == 2
                    && model.HasData && model.Mod == 1 && model.HasExperience && model.AllExperience == ulong.MaxValue
                    && model.HasRecordStats && model.RecordStats.Count == 2 && frames.Count == 0;

                byte[] replacementStatsBytes = new CliVerify.Pkt().H(1).C(1).C(2).I(3).C(4).H(1)
                    .L(5).C(6).I(7).I(8).S("one").I(9).H(10).H(11).Bytes();
                var replacementStatsReader = new NetReader(replacementStatsBytes, 0, replacementStatsBytes.Length);
                on21808.Invoke(controller, new object[] { replacementStatsReader });
                pass &= replacementStatsReader.Remaining == 0 && model.HasRecordStats && model.RecordStats.Count == 1
                    && model.RecordStats[0].GroupId == 1 && model.RecordStats[0].Roles.Count == 1
                    && model.HasData && model.Servers.Count == 1 && model.HasExperience && model.AllExperience == ulong.MaxValue
                    && model.HasScore && model.Rewards.Count == 1 && frames.Count == 0;

                var replacementPhaseReader = new NetReader(new CliVerify.Pkt().C(1).I(9).Bytes(), 0, 5);
                on21811.Invoke(controller, new object[] { replacementPhaseReader });
                pass &= replacementPhaseReader.Remaining == 0 && model.HasPhaseTime && model.PhaseStatus == 1 && model.PhaseEndTime == 9
                    && model.HasData && model.Servers.Count == 1 && model.HasExperience && model.AllExperience == ulong.MaxValue
                    && model.HasScore && model.Rewards.Count == 1 && model.HasRecordStats && model.RecordStats.Count == 1
                    && model.HasFightState && model.Buffs.Count == 3 && model.HasMonsterInfo && model.MonstersByCfgId.Count == 1 && model.MonstersByCfgId.ContainsKey(uint.MaxValue) && frames.Count == 0;

                byte[] replacementFightBytes = new CliVerify.Pkt().H(1).H(2).C(3).C(4).I(5).H(1).H(6).I(7).Bytes();
                var replacementFightReader = new NetReader(replacementFightBytes, 0, replacementFightBytes.Length);
                on21807.Invoke(controller, new object[] { replacementFightReader });
                pass &= replacementFightReader.Remaining == 0 && model.HasFightState && model.FightPoint == 1 && model.SingleRank == 2
                    && model.GroupRank == 3 && model.Anger == 4 && model.AngerEnd == 5
                    && model.Buffs.Count == 1 && model.Buffs[0].AttrId == 6 && model.Buffs[0].Value == 7
                    && model.HasData && model.Servers.Count == 1 && model.HasExperience && model.AllExperience == ulong.MaxValue
                    && model.HasScore && model.Rewards.Count == 1 && model.HasRecordStats && model.RecordStats.Count == 1
                    && model.HasPhaseTime && model.PhaseStatus == 1 && model.PhaseEndTime == 9 && frames.Count == 0;

                var emptyScoreReader = new NetReader(new CliVerify.Pkt().I(0).H(0).Bytes(), 0, 6);
                on21805.Invoke(controller, new object[] { emptyScoreReader });
                pass &= emptyScoreReader.Remaining == 0 && model.HasScore && model.Point == 0 && model.Rewards.Count == 0
                    && model.HasData && model.Mod == 1 && model.HasExperience && model.AllExperience == ulong.MaxValue
                    && model.HasRecordStats && model.RecordStats.Count == 1
                    && model.HasPhaseTime && model.PhaseStatus == 1 && model.PhaseEndTime == 9
                    && model.HasFightState && model.Buffs.Count == 1 && frames.Count == 0;

                byte[] thirdBytes = new CliVerify.Pkt().C(0).C(0).I(0).H(0).Bytes();
                var thirdReader = new NetReader(thirdBytes, 0, thirdBytes.Length);
                on21801.Invoke(controller, new object[] { thirdReader });
                pass &= thirdReader.Remaining == 0
                    && model.HasData && model.Mod == 0 && model.Status == 0 && model.EndTime == 0 && model.Servers.Count == 0
                    && model.HasExperience && model.AllExperience == ulong.MaxValue && model.HasScore && model.Rewards.Count == 0
                    && model.HasRecordStats && model.RecordStats.Count == 1
                    && model.HasPhaseTime && model.PhaseStatus == 1 && model.PhaseEndTime == 9
                    && model.HasFightState && model.Buffs.Count == 1;

                var emptyStatsReader = new NetReader(new CliVerify.Pkt().H(0).Bytes(), 0, 2);
                on21808.Invoke(controller, new object[] { emptyStatsReader });
                pass &= emptyStatsReader.Remaining == 0 && model.HasRecordStats && model.RecordStats.Count == 0
                    && model.HasData && model.HasExperience && model.HasScore
                    && model.HasPhaseTime && model.PhaseStatus == 1 && model.PhaseEndTime == 9
                    && model.HasFightState && model.Buffs.Count == 1 && frames.Count == 0;

                byte[] emptyFightBytes = new CliVerify.Pkt().H(0).H(0).C(0).C(0).I(0).H(0).Bytes();
                var emptyFightReader = new NetReader(emptyFightBytes, 0, emptyFightBytes.Length);
                on21807.Invoke(controller, new object[] { emptyFightReader });
                pass &= emptyFightReader.Remaining == 0 && model.HasFightState && model.FightPoint == 0 && model.SingleRank == 0
                    && model.GroupRank == 0 && model.Anger == 0 && model.AngerEnd == 0 && model.Buffs.Count == 0
                    && model.HasData && model.HasExperience && model.HasScore && model.HasRecordStats && model.RecordStats.Count == 0
                    && model.HasPhaseTime && model.PhaseStatus == 1 && model.PhaseEndTime == 9 && frames.Count == 0;

                controller.Dispose();
                pass &= !controller.IsInitialized && !handlers.Contains(21801) && !handlers.Contains(21804) && !handlers.Contains(21805)
                    && !handlers.Contains(21807) && !handlers.Contains(21808) && !handlers.Contains(21809) && !handlers.Contains(21810) && !handlers.Contains(21811) && !handlers.Contains(21813)
                    && !model.HasData && model.Mod == 0 && model.Status == 0 && model.EndTime == 0 && model.Servers.Count == 0
                    && !model.HasExperience && model.AllExperience == 0 && !model.HasScore && model.Point == 0 && model.Rewards.Count == 0
                    && !model.HasRecordStats && model.RecordStats.Count == 0 && !model.HasPhaseTime && model.PhaseStatus == 0 && model.PhaseEndTime == 0
                    && !model.HasFightState && model.FightPoint == 0 && model.SingleRank == 0 && model.GroupRank == 0
                    && model.Anger == 0 && model.AngerEnd == 0 && model.Buffs.Count == 0 && !model.HasMonsterInfo && model.MonstersByCfgId.Count == 0
                    && !model.HasDeathInfo && model.DeathRoleName == null && model.DeathRoleId == 0 && model.DeathLevel == 0 && model.DeathPower == 0
                    && model.DeathPictureVersion == 0 && model.DeathPicture == null && model.DeathAnger == 0 && model.DeathServerId == 0 && model.DeathCareer == 0 && model.DeathTurn == 0
                    && !model.HasResultInfo && model.ResultCode == 0 && model.ResultGroups.Count == 0 && model.ResultMyGroupId == 0 && model.ResultMyRank == 0;

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

                if (oldHasExperience)
                {
                    model.ReplaceExperience(oldAllExperience);
                }

                if (oldHasScore)
                {
                    model.ReplaceScore(oldPoint, oldRewards);
                }

                if (oldHasRecordStats)
                {
                    model.ReplaceRecordStats(oldRecordStats);
                }

                if (oldHasPhaseTime)
                {
                    model.ReplacePhaseTime(oldPhaseStatus, oldPhaseEndTime);
                }

                if (oldHasFightState)
                {
                    model.ReplaceFightState(oldFightPoint, oldSingleRank, oldGroupRank, oldAnger, oldAngerEnd, oldBuffs);
                }

                if (oldHasMonsterInfo)
                {
                    model.ApplyMonsterInfo(oldMonsters);
                }

                if (oldHasDeathInfo)
                {
                    model.ReplaceDeathInfo(oldDeathRoleName, oldDeathRoleId, oldDeathLevel, oldDeathPower, oldDeathPictureVersion, oldDeathPicture, oldDeathAnger, oldDeathServerId, oldDeathCareer, oldDeathTurn);
                }

                if (oldHasResultInfo)
                {
                    model.ReplaceResultInfo(oldResultCode, oldResultGroups, oldResultMyGroupId, oldResultMyRank);
                }

                if (wasInitialized)
                {
                    controller.Init();
                }

                IDictionary finalHandlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                if (finalHandlers == null)
                {
                    throw new InvalidOperationException("HolyBattle handlers unavailable during restore");
                }

                foreach (int id in handlerIds)
                {
                    if (handlerSnapshot.TryGetValue(id, out object handler))
                    {
                        finalHandlers[id] = handler;
                    }
                    else
                    {
                        finalHandlers.Remove(id);
                    }
                }

                if (interceptField != null)
                {
                    interceptField.SetValue(null, oldIntercept);
                }

                bool restored = controller.IsInitialized == wasInitialized
                    && model.HasMonsterInfo == oldHasMonsterInfo
                    && SameMonsters(model.MonstersByCfgId, oldMonsters)
                    && model.HasDeathInfo == oldHasDeathInfo
                    && model.DeathRoleName == oldDeathRoleName && model.DeathRoleId == oldDeathRoleId && model.DeathLevel == oldDeathLevel
                    && model.DeathPower == oldDeathPower && model.DeathPictureVersion == oldDeathPictureVersion && model.DeathPicture == oldDeathPicture
                    && model.DeathAnger == oldDeathAnger && model.DeathServerId == oldDeathServerId && model.DeathCareer == oldDeathCareer && model.DeathTurn == oldDeathTurn
                    && model.HasResultInfo == oldHasResultInfo && model.ResultCode == oldResultCode && model.ResultMyGroupId == oldResultMyGroupId && model.ResultMyRank == oldResultMyRank
                    && SameResultGroups(model.ResultGroups, oldResultGroups)
                    && (interceptField == null || ReferenceEquals(interceptField.GetValue(null), oldIntercept));
                foreach (int id in handlerIds)
                {
                    bool existed = handlerSnapshot.TryGetValue(id, out object expected);
                    if (finalHandlers.Contains(id) != existed || (existed && !ReferenceEquals(finalHandlers[id], expected)))
                    {
                        restored = false;
                    }
                }

                if (!restored)
                {
                    throw new InvalidOperationException("HolyBattle ambient state restore failed");
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

        private static bool IsExactExperienceRequest(byte[] frame)
        {
            return frame != null
                && frame.Length == 6
                && frame[0] == 0 && frame[1] == 6
                && frame[2] == 0x03 && frame[3] == 0xE8
                && frame[4] == (byte)(Proto.HOLY_BATTLE_EXPERIENCE >> 8)
                && frame[5] == (byte)(Proto.HOLY_BATTLE_EXPERIENCE & 0xFF);
        }

        private static bool IsExactScoreRequest(byte[] frame)
        {
            return frame != null
                && frame.Length == 6
                && frame[0] == 0 && frame[1] == 6
                && frame[2] == 0x03 && frame[3] == 0xE8
                && frame[4] == (byte)(Proto.HOLY_BATTLE_SCORE >> 8)
                && frame[5] == (byte)(Proto.HOLY_BATTLE_SCORE & 0xFF);
        }

        private static bool IsExactRecordStatsRequest(byte[] frame)
        {
            return frame != null && frame.Length == 6
                && frame[0] == 0 && frame[1] == 6 && frame[2] == 0x03 && frame[3] == 0xE8
                && frame[4] == (byte)(Proto.HOLY_BATTLE_RECORD_STATS >> 8)
                && frame[5] == (byte)(Proto.HOLY_BATTLE_RECORD_STATS & 0xFF);
        }

        private static bool IsExactPhaseTimeRequest(byte[] frame)
        {
            return frame != null && frame.Length == 6 && frame[0] == 0 && frame[1] == 6
                && frame[2] == 0x03 && frame[3] == 0xE8
                && frame[4] == (byte)(Proto.HOLY_BATTLE_PHASE_TIME >> 8)
                && frame[5] == (byte)(Proto.HOLY_BATTLE_PHASE_TIME & 0xFF);
        }

        private static bool IsExactMonsterInfoRequest(byte[] frame)
        {
            return frame != null && frame.Length == 6 && frame[0] == 0 && frame[1] == 6
                && frame[2] == 0x03 && frame[3] == 0xE8
                && frame[4] == (byte)(Proto.HOLY_BATTLE_MONSTER_INFO >> 8)
                && frame[5] == (byte)(Proto.HOLY_BATTLE_MONSTER_INFO & 0xFF);
        }

        private static bool SameMonsters(IReadOnlyDictionary<uint, HolyBattleModel.MonsterEntry> actual, List<HolyBattleModel.MonsterEntry> expected)
        {
            if (actual.Count != expected.Count)
            {
                return false;
            }

            foreach (HolyBattleModel.MonsterEntry item in expected)
            {
                if (!actual.TryGetValue(item.MonCfgId, out HolyBattleModel.MonsterEntry restored)
                    || restored.MonAuto != item.MonAuto
                    || restored.MonCfgId != item.MonCfgId
                    || restored.Hp != item.Hp
                    || restored.HpAll != item.HpAll
                    || restored.GroupId != item.GroupId)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool SameResultGroups(IReadOnlyList<HolyBattleModel.ResultGroupEntry> actual, List<HolyBattleModel.ResultGroupEntry> expected)
        {
            if (actual.Count != expected.Count)
            {
                return false;
            }

            for (int i = 0; i < actual.Count; i++)
            {
                if (actual[i].GroupId != expected[i].GroupId || actual[i].TowerNum != expected[i].TowerNum || actual[i].Point != expected[i].Point)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
