using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Eternity;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class EternityCase
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
                Debug.LogError("CLIVERIFY eternity EXCEPTION " + exception);
                return Task.FromResult(3);
            }
        }

        private static int RunCore()
        {
            EternityController controller = EternityController.Instance;
            EternityModel model = EternityModel.Instance;
            RoleModel role = RoleModel.Instance;
            bool pass = false;
            bool restored = false;
            bool wasInitialized = controller.IsInitialized;
            bool oldHasData = model.HasData;
            uint oldOpenTime = model.OpenTime;
            uint oldEnterTime = model.EnterTime;
            uint oldEndTime = model.EndTime;
            bool oldHasJoinInfo = model.HasJoinInfo;
            byte oldCanEnterScene = model.CanEnterScene;
            var oldJoinList = new List<EternityModel.JoinEntry>(model.JoinList);
            bool oldHasReliveInfo = model.HasReliveInfo;
            ushort oldDieTimes = model.DieTimes;
            uint oldTime = model.Time;
            uint oldDieTime = model.DieTime;
            uint oldSafeTime = model.SafeTime;
            bool oldHasMonsterInfo = model.HasMonsterInfo;
            ushort oldMonsterScene = model.MonsterScene;
            var oldMonsterInfo = new List<EternityModel.MonsterEntry>(model.MonsterInfo);
            bool oldHasDamageRank = model.HasDamageRank;
            ushort oldDamageScene = model.DamageScene;
            uint oldDamageMonId = model.DamageMonId;
            var oldDamageRank = new List<EternityModel.DamageEntry>(model.DamageRank);
            bool oldHasBossStates = model.HasBossStates;
            var oldBossStates = new List<EternityModel.BossStateEntry>(model.BossStates.Values);
            bool oldHasError = model.HasError;
            uint oldLastErrorCode = model.LastErrorCode;
            int oldLevel = role.Level;
            FieldInfo hasBaseInfoField = typeof(RoleModel).GetField("<HasBaseInfo>k__BackingField", InstanceNonPublic);
            bool oldHasBaseInfo = hasBaseInfoField != null && (bool)hasBaseInfoField.GetValue(role);
            FieldInfo interceptField = typeof(EternityController).GetField("s_outboundIntercept", StaticNonPublic);
            FieldInfo lastLevelField = typeof(EternityController).GetField("_lastLevel", InstanceNonPublic);
            object oldIntercept = interceptField == null ? null : interceptField.GetValue(null);
            int oldLastLevel = lastLevelField == null ? -1 : (int)lastLevelField.GetValue(controller);
            IDictionary handlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
            var oldHandlers = new Dictionary<int, object>();
            if (handlers != null)
            {
                foreach (int proto in new[] { 27900, 27901, 27904, 27905, 27906, 27907, 27908, 27909 })
                {
                    if (handlers.Contains(proto)) oldHandlers[proto] = handlers[proto];
                }
            }
            IDictionary eventHandlers = typeof(EventDispatcher).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
            bool oldHadRoleEvent = eventHandlers != null && eventHandlers.Contains(GlobalEvent.EVT_ROLE_INFO_UPDATE);
            var oldRoleSubscribers = oldHadRoleEvent
                ? new List<Delegate>((List<Delegate>)eventHandlers[GlobalEvent.EVT_ROLE_INFO_UPDATE])
                : new List<Delegate>();

            try
            {
                controller.Init();
                model.Reset();

                MethodInfo on27900 = typeof(EternityController).GetMethod("On27900", InstanceNonPublic);
                MethodInfo on27901 = typeof(EternityController).GetMethod("On27901", InstanceNonPublic);
                MethodInfo on27904 = typeof(EternityController).GetMethod("On27904", InstanceNonPublic);
                MethodInfo on27905 = typeof(EternityController).GetMethod("On27905", InstanceNonPublic);
                MethodInfo on27906 = typeof(EternityController).GetMethod("On27906", InstanceNonPublic);
                MethodInfo on27907 = typeof(EternityController).GetMethod("On27907", InstanceNonPublic);
                MethodInfo on27908 = typeof(EternityController).GetMethod("On27908", InstanceNonPublic);
                MethodInfo on27909 = typeof(EternityController).GetMethod("On27909", InstanceNonPublic);
                MethodInfo onRoleInfoUpdate = typeof(EternityController).GetMethod("OnRoleInfoUpdate", InstanceNonPublic);
                pass = hasBaseInfoField != null && interceptField != null && lastLevelField != null
                    && on27900 != null && on27901 != null && on27904 != null && on27905 != null && on27906 != null && on27907 != null && on27908 != null && on27909 != null && onRoleInfoUpdate != null && handlers != null
                    && typeof(EternityController).GetMethod("RequestMonsterInfo", BindingFlags.Public | BindingFlags.Instance) != null
                    && typeof(EternityController).GetMethod("RequestDamageRank", BindingFlags.Public | BindingFlags.Instance) != null
                    && typeof(EternityController).GetMethod("RequestMonsterReborn", BindingFlags.Public | InstanceNonPublic) == null
                    && typeof(EternityController).GetMethod("Request27908", BindingFlags.Public | InstanceNonPublic) == null
                    && typeof(EternityController).GetMethod("Request27909", BindingFlags.Public | InstanceNonPublic) == null
                    && eventHandlers != null;
                for (int proto = 27900; proto <= 27909; proto++)
                {
                    pass &= (proto == 27900 || proto == 27901 || proto == 27904 || proto == 27905 || proto == 27906 || proto == 27907 || proto == 27908 || proto == 27909) == handlers.Contains(proto);
                }

                if (!pass)
                {
                    Debug.LogError("CLIVERIFY eternity VERDICT pass=false (reflection/protocol registration missing)");
                    return 3;
                }

                byte[] unloadedRebornBytes = new CliVerify.Pkt().I(uint.MaxValue).Bytes();
                var unloadedRebornReader = new NetReader(unloadedRebornBytes, 0, unloadedRebornBytes.Length);
                on27907.Invoke(controller, new object[] { unloadedRebornReader });
                pass &= unloadedRebornReader.Remaining == 0 && !model.HasMonsterInfo && model.MonsterScene == 0 && model.MonsterInfo.Count == 0;
                var unloadedErrorReader = new NetReader(new CliVerify.Pkt().I(0).Bytes(), 0, 4);
                on27909.Invoke(controller, new object[] { unloadedErrorReader });
                pass &= unloadedErrorReader.Remaining == 0 && model.HasError && model.LastErrorCode == 0
                    && !model.HasData && !model.HasJoinInfo && !model.HasReliveInfo && !model.HasMonsterInfo && !model.HasDamageRank && !model.HasBossStates;
                model.Reset();

                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));
                hasBaseInfoField.SetValue(role, true);

                model.Replace(1, 2, 3);
                model.ReplaceJoinInfo(1, new List<EternityModel.JoinEntry> { new EternityModel.JoinEntry(4, 5, 6) });
                model.ReplaceReliveInfo(7, 8, 9, 10);
                role.Level = 479;
                controller.RequestStartup();
                pass &= frames.Count == 0 && !model.HasData && !model.HasJoinInfo && !model.HasReliveInfo && model.OpenTime == 0 && model.EnterTime == 0 && model.EndTime == 0
                    && model.CanEnterScene == 0 && model.JoinList.Count == 0 && model.DieTimes == 0 && model.Time == 0 && model.DieTime == 0 && model.SafeTime == 0 && !model.HasMonsterInfo && model.MonsterInfo.Count == 0 && !model.HasDamageRank && model.DamageRank.Count == 0 && !model.HasBossStates && model.BossStates.Count == 0 && !model.HasError && model.LastErrorCode == 0;

                model.Replace(4, 5, 6);
                role.Level = 480;
                controller.RequestStartup();
                pass &= frames.Count == 1 && !model.HasData && !HasProtocol(frames, 27904) && !HasProtocol(frames, 27905) && !HasProtocol(frames, 27907) && !HasProtocol(frames, 27908) && !HasProtocol(frames, 27909);
                pass &= IsExactRequest(frames[0]);
                frames.Clear();

                role.Level = 479;
                controller.RequestStartup();
                role.Level = 480;
                onRoleInfoUpdate.Invoke(controller, null);
                pass &= frames.Count == 1 && IsExactRequest(frames[0]) && !HasProtocol(frames, 27904) && !HasProtocol(frames, 27905) && !HasProtocol(frames, 27907) && !HasProtocol(frames, 27908) && !HasProtocol(frames, 27909);
                onRoleInfoUpdate.Invoke(controller, null);
                pass &= frames.Count == 1;
                role.Level = 481;
                onRoleInfoUpdate.Invoke(controller, null);
                pass &= frames.Count == 1;

                frames.Clear();
                role.Level = 479;
                controller.RequestStartup();
                role.Level = 481;
                onRoleInfoUpdate.Invoke(controller, null);
                pass &= frames.Count == 0 && !HasProtocol(frames, 27904) && !HasProtocol(frames, 27905) && !HasProtocol(frames, 27907) && !HasProtocol(frames, 27908) && !HasProtocol(frames, 27909);

                byte[] firstBytes = new CliVerify.Pkt().I(0).I(4000000000L).I(4294967295L).Bytes();
                var firstReader = new NetReader(firstBytes, 0, firstBytes.Length);
                on27900.Invoke(controller, new object[] { firstReader });
                pass &= firstReader.Remaining == 0 && model.HasData
                    && model.OpenTime == 0 && model.EnterTime == 4000000000U && model.EndTime == uint.MaxValue
                    && frames.Count == 0;

                byte[] secondBytes = new CliVerify.Pkt().I(7).I(8).I(9).Bytes();
                var secondReader = new NetReader(secondBytes, 0, secondBytes.Length);
                on27900.Invoke(controller, new object[] { secondReader });
                pass &= secondReader.Remaining == 0 && model.HasData
                    && model.OpenTime == 7 && model.EnterTime == 8 && model.EndTime == 9;

                controller.RequestJoinInfo();
                pass &= frames.Count == 1 && IsExactJoinRequest(frames[0]) && !model.HasJoinInfo;
                frames.Clear();

                byte[] emptyJoinBytes = JoinPacket(0, new JoinSpec[0]);
                var emptyJoinReader = new NetReader(emptyJoinBytes, 0, emptyJoinBytes.Length);
                on27901.Invoke(controller, new object[] { emptyJoinReader });
                pass &= emptyJoinReader.Remaining == 0 && model.HasJoinInfo && model.CanEnterScene == 0 && model.JoinList.Count == 0 && frames.Count == 0;

                JoinSpec firstJoin = new JoinSpec(uint.MaxValue, ushort.MaxValue, 0);
                JoinSpec secondJoin = new JoinSpec(uint.MaxValue, 1, ushort.MaxValue);
                byte[] fullJoinBytes = JoinPacket(byte.MaxValue, new[] { firstJoin, secondJoin });
                var fullJoinReader = new NetReader(fullJoinBytes, 0, fullJoinBytes.Length);
                on27901.Invoke(controller, new object[] { fullJoinReader });
                pass &= fullJoinReader.Remaining == 0 && model.HasJoinInfo && model.CanEnterScene == byte.MaxValue && model.JoinList.Count == 2
                    && IsJoin(model.JoinList[0], firstJoin) && IsJoin(model.JoinList[1], secondJoin) && frames.Count == 0;

                controller.RequestJoinInfo();
                pass &= frames.Count == 1 && IsExactJoinRequest(frames[0]) && model.JoinList.Count == 2 && IsJoin(model.JoinList[0], firstJoin);
                frames.Clear();

                byte[] isolatedTimeBytes = new CliVerify.Pkt().I(10).I(11).I(12).Bytes();
                var isolatedTimeReader = new NetReader(isolatedTimeBytes, 0, isolatedTimeBytes.Length);
                on27900.Invoke(controller, new object[] { isolatedTimeReader });
                pass &= isolatedTimeReader.Remaining == 0 && model.HasData && model.OpenTime == 10 && model.EnterTime == 11 && model.EndTime == 12 && model.JoinList.Count == 2;
                byte[] lessJoinBytes = JoinPacket(0, new[] { new JoinSpec(13, 14, 15) });
                var lessJoinReader = new NetReader(lessJoinBytes, 0, lessJoinBytes.Length);
                on27901.Invoke(controller, new object[] { lessJoinReader });
                pass &= lessJoinReader.Remaining == 0 && model.HasData && model.OpenTime == 10 && model.EnterTime == 11 && model.EndTime == 12
                    && model.CanEnterScene == 0 && model.JoinList.Count == 1 && model.JoinList[0].Scene == 13 && frames.Count == 0;

                byte[] clearJoinBytes = JoinPacket(byte.MaxValue, new JoinSpec[0]);
                var clearJoinReader = new NetReader(clearJoinBytes, 0, clearJoinBytes.Length);
                on27901.Invoke(controller, new object[] { clearJoinReader });
                pass &= clearJoinReader.Remaining == 0 && model.HasJoinInfo && model.CanEnterScene == byte.MaxValue && model.JoinList.Count == 0 && model.HasData && model.OpenTime == 10 && frames.Count == 0;

                controller.RequestReliveInfo();
                pass &= frames.Count == 1 && IsExactReliveRequest(frames[0]) && !model.HasReliveInfo;
                frames.Clear();
                byte[] fullReliveBytes = new CliVerify.Pkt().H(ushort.MaxValue).I(uint.MaxValue).I(4000000000L).I(1).Bytes();
                var fullReliveReader = new NetReader(fullReliveBytes, 0, fullReliveBytes.Length);
                on27906.Invoke(controller, new object[] { fullReliveReader });
                pass &= fullReliveReader.Remaining == 0 && model.HasReliveInfo && model.DieTimes == ushort.MaxValue
                    && model.Time == uint.MaxValue && model.DieTime == 4000000000U && model.SafeTime == 1 && model.HasData && model.HasJoinInfo && frames.Count == 0;
                byte[] smallReliveBytes = new CliVerify.Pkt().H(2).I(3).I(4).I(5).Bytes();
                var smallReliveReader = new NetReader(smallReliveBytes, 0, smallReliveBytes.Length);
                on27906.Invoke(controller, new object[] { smallReliveReader });
                pass &= smallReliveReader.Remaining == 0 && model.DieTimes == 2 && model.Time == 3 && model.DieTime == 4 && model.SafeTime == 5;
                controller.RequestReliveInfo();
                pass &= frames.Count == 1 && IsExactReliveRequest(frames[0]) && model.DieTimes == 2 && model.Time == 3 && model.DieTime == 4 && model.SafeTime == 5;
                frames.Clear();
                byte[] reliveIsolatedTimeBytes = new CliVerify.Pkt().I(20).I(21).I(22).Bytes();
                var reliveIsolatedTimeReader = new NetReader(reliveIsolatedTimeBytes, 0, reliveIsolatedTimeBytes.Length);
                on27900.Invoke(controller, new object[] { reliveIsolatedTimeReader });
                pass &= reliveIsolatedTimeReader.Remaining == 0 && model.OpenTime == 20 && model.EnterTime == 21 && model.EndTime == 22
                    && model.DieTimes == 2 && model.Time == 3 && model.DieTime == 4 && model.SafeTime == 5;
                byte[] reliveIsolatedJoinBytes = JoinPacket(1, new[] { new JoinSpec(23, 24, 25) });
                var reliveIsolatedJoinReader = new NetReader(reliveIsolatedJoinBytes, 0, reliveIsolatedJoinBytes.Length);
                on27901.Invoke(controller, new object[] { reliveIsolatedJoinReader });
                pass &= reliveIsolatedJoinReader.Remaining == 0 && model.OpenTime == 20 && model.EnterTime == 21 && model.EndTime == 22
                    && model.CanEnterScene == 1 && model.JoinList.Count == 1 && model.JoinList[0].Scene == 23
                    && model.DieTimes == 2 && model.Time == 3 && model.DieTime == 4 && model.SafeTime == 5 && frames.Count == 0;
                byte[] zeroReliveBytes = new CliVerify.Pkt().H(0).I(0).I(0).I(0).Bytes();
                var zeroReliveReader = new NetReader(zeroReliveBytes, 0, zeroReliveBytes.Length);
                on27906.Invoke(controller, new object[] { zeroReliveReader });
                pass &= zeroReliveReader.Remaining == 0 && model.HasReliveInfo && model.DieTimes == 0 && model.Time == 0 && model.DieTime == 0 && model.SafeTime == 0;

                MonsterSpec firstMonster = new MonsterSpec(uint.MaxValue, ushort.MaxValue, byte.MaxValue, uint.MaxValue, "归属Ω", uint.MaxValue, uint.MaxValue);
                MonsterSpec duplicateMonster = new MonsterSpec(uint.MaxValue, 1, 2, 3, string.Empty, 4, 5);
                byte[] multiMonsterBytes = MonsterPacket(ushort.MaxValue, new[] { firstMonster, duplicateMonster });
                var multiMonsterReader = new NetReader(multiMonsterBytes, 0, multiMonsterBytes.Length);
                on27904.Invoke(controller, new object[] { multiMonsterReader });
                pass &= multiMonsterReader.Remaining == 0 && model.HasMonsterInfo && model.MonsterScene == ushort.MaxValue && model.MonsterInfo.Count == 2
                    && IsMonster(model.MonsterInfo[0], firstMonster) && IsMonster(model.MonsterInfo[1], duplicateMonster);

                controller.RequestMonsterInfo(ushort.MaxValue);
                pass &= frames.Count == 1 && IsExactMonsterRequest(frames[0], ushort.MaxValue)
                    && model.MonsterScene == ushort.MaxValue && model.MonsterInfo.Count == 2 && IsMonster(model.MonsterInfo[0], firstMonster);
                frames.Clear();
                controller.RequestMonsterInfo(0);
                pass &= frames.Count == 1 && IsExactMonsterRequest(frames[0], 0)
                    && model.MonsterScene == ushort.MaxValue && model.MonsterInfo.Count == 2 && IsMonster(model.MonsterInfo[1], duplicateMonster);
                frames.Clear();

                var duplicateRebornReader = new NetReader(new CliVerify.Pkt().I(uint.MaxValue).Bytes(), 0, 4);
                on27907.Invoke(controller, new object[] { duplicateRebornReader });
                pass &= duplicateRebornReader.Remaining == 0 && model.MonsterScene == ushort.MaxValue && model.MonsterInfo.Count == 2
                    && IsMonster(model.MonsterInfo[0], new MonsterSpec(firstMonster.MonId, firstMonster.MonLv, firstMonster.MonType, firstMonster.BlServer, firstMonster.BlServerName, firstMonster.BlServerNum, 0))
                    && IsMonster(model.MonsterInfo[1], duplicateMonster);

                MonsterSpec singleMonster = new MonsterSpec(0, 0, 0, 0, string.Empty, 0, 0);
                byte[] singleMonsterBytes = MonsterPacket(0, new[] { singleMonster });
                var singleMonsterReader = new NetReader(singleMonsterBytes, 0, singleMonsterBytes.Length);
                on27904.Invoke(controller, new object[] { singleMonsterReader });
                pass &= singleMonsterReader.Remaining == 0 && model.HasMonsterInfo && model.MonsterScene == 0 && model.MonsterInfo.Count == 1 && IsMonster(model.MonsterInfo[0], singleMonster)
                    && model.OpenTime == 20 && model.EnterTime == 21 && model.EndTime == 22 && model.CanEnterScene == 1 && model.JoinList.Count == 1 && model.JoinList[0].Scene == 23
                    && model.DieTimes == 0 && model.Time == 0 && model.DieTime == 0 && model.SafeTime == 0;

                var monsterIsolatedTimeReader = new NetReader(reliveIsolatedTimeBytes, 0, reliveIsolatedTimeBytes.Length);
                on27900.Invoke(controller, new object[] { monsterIsolatedTimeReader });
                var monsterIsolatedJoinReader = new NetReader(reliveIsolatedJoinBytes, 0, reliveIsolatedJoinBytes.Length);
                on27901.Invoke(controller, new object[] { monsterIsolatedJoinReader });
                var monsterIsolatedReliveReader = new NetReader(zeroReliveBytes, 0, zeroReliveBytes.Length);
                on27906.Invoke(controller, new object[] { monsterIsolatedReliveReader });
                pass &= monsterIsolatedTimeReader.Remaining == 0 && monsterIsolatedJoinReader.Remaining == 0 && monsterIsolatedReliveReader.Remaining == 0
                    && model.HasMonsterInfo && model.MonsterScene == 0 && model.MonsterInfo.Count == 1 && IsMonster(model.MonsterInfo[0], singleMonster);

                DamageSpec firstDamage = new DamageSpec(uint.MaxValue, ushort.MaxValue, "服Ω", uint.MaxValue, "甲Ω", ushort.MaxValue);
                DamageSpec duplicateDamage = new DamageSpec(1, 2, "乙服", uint.MaxValue, "乙", 3);
                byte[] multiDamageBytes = DamagePacket(ushort.MaxValue, uint.MaxValue, new[] { firstDamage, duplicateDamage });
                var multiDamageReader = new NetReader(multiDamageBytes, 0, multiDamageBytes.Length);
                on27905.Invoke(controller, new object[] { multiDamageReader });
                pass &= multiDamageReader.Remaining == 0 && model.HasDamageRank && model.DamageScene == ushort.MaxValue && model.DamageMonId == uint.MaxValue && model.DamageRank.Count == 2
                    && IsDamage(model.DamageRank[0], firstDamage) && IsDamage(model.DamageRank[1], duplicateDamage);

                controller.RequestDamageRank(ushort.MaxValue, uint.MaxValue);
                pass &= frames.Count == 1 && IsExactDamageRequest(frames[0], ushort.MaxValue, uint.MaxValue)
                    && model.DamageScene == ushort.MaxValue && model.DamageMonId == uint.MaxValue && model.DamageRank.Count == 2 && IsDamage(model.DamageRank[0], firstDamage);
                frames.Clear();
                controller.RequestDamageRank(0, 0);
                pass &= frames.Count == 1 && IsExactDamageRequest(frames[0], 0, 0)
                    && model.DamageScene == ushort.MaxValue && model.DamageMonId == uint.MaxValue && model.DamageRank.Count == 2 && IsDamage(model.DamageRank[1], duplicateDamage);
                frames.Clear();

                DamageSpec singleDamage = new DamageSpec(0, 0, string.Empty, 0, string.Empty, 0);
                byte[] singleDamageBytes = DamagePacket(0, 0, new[] { singleDamage });
                var singleDamageReader = new NetReader(singleDamageBytes, 0, singleDamageBytes.Length);
                on27905.Invoke(controller, new object[] { singleDamageReader });
                pass &= singleDamageReader.Remaining == 0 && model.HasDamageRank && model.DamageScene == 0 && model.DamageMonId == 0 && model.DamageRank.Count == 1 && IsDamage(model.DamageRank[0], singleDamage)
                    && model.OpenTime == 20 && model.EnterTime == 21 && model.EndTime == 22 && model.CanEnterScene == 1 && model.JoinList.Count == 1 && model.JoinList[0].Scene == 23
                    && model.DieTimes == 0 && model.Time == 0 && model.DieTime == 0 && model.SafeTime == 0 && model.MonsterScene == 0 && model.MonsterInfo.Count == 1 && IsMonster(model.MonsterInfo[0], singleMonster);

                byte[] maxBossBytes = BossPacket(uint.MaxValue, uint.MaxValue, 4000000000L, uint.MaxValue, "永恒Ω");
                var maxBossReader = new NetReader(maxBossBytes, 0, maxBossBytes.Length);
                on27908.Invoke(controller, new object[] { maxBossReader });
                pass &= maxBossReader.Remaining == 0 && model.HasBossStates && model.BossStates.Count == 1
                    && IsBoss(model.BossStates[uint.MaxValue], uint.MaxValue, uint.MaxValue, 4000000000U, uint.MaxValue, "永恒Ω") && frames.Count == 0
                    && model.HasDamageRank && model.DamageScene == 0 && model.DamageMonId == 0 && model.DamageRank.Count == 1 && IsDamage(model.DamageRank[0], singleDamage)
                    && model.HasMonsterInfo && model.MonsterScene == 0 && model.MonsterInfo.Count == 1 && IsMonster(model.MonsterInfo[0], singleMonster);

                byte[] overwriteBossBytes = BossPacket(uint.MaxValue, 1, 2, 3, "small");
                var overwriteBossReader = new NetReader(overwriteBossBytes, 0, overwriteBossBytes.Length);
                on27908.Invoke(controller, new object[] { overwriteBossReader });
                pass &= overwriteBossReader.Remaining == 0 && model.BossStates.Count == 1 && IsBoss(model.BossStates[uint.MaxValue], uint.MaxValue, 1, 2, 3, "small");

                byte[] zeroBossBytes = BossPacket(0, 0, 0, 0, string.Empty);
                var zeroBossReader = new NetReader(zeroBossBytes, 0, zeroBossBytes.Length);
                on27908.Invoke(controller, new object[] { zeroBossReader });
                pass &= zeroBossReader.Remaining == 0 && model.HasBossStates && model.BossStates.Count == 2 && IsBoss(model.BossStates[0], 0, 0, 0, 0, string.Empty);

                byte[] otherBossBytes = BossPacket(77, 88, 99, 100, "other");
                var otherBossReader = new NetReader(otherBossBytes, 0, otherBossBytes.Length);
                on27908.Invoke(controller, new object[] { otherBossReader });
                pass &= otherBossReader.Remaining == 0 && model.BossStates.Count == 3 && IsBoss(model.BossStates[77], 77, 88, 99, 100, "other")
                    && IsBoss(model.BossStates[uint.MaxValue], uint.MaxValue, 1, 2, 3, "small");

                var zeroErrorReader = new NetReader(new CliVerify.Pkt().I(0).Bytes(), 0, 4);
                on27909.Invoke(controller, new object[] { zeroErrorReader });
                pass &= zeroErrorReader.Remaining == 0 && model.HasError && model.LastErrorCode == 0
                    && model.OpenTime == 20 && model.EnterTime == 21 && model.EndTime == 22 && model.CanEnterScene == 1 && model.JoinList.Count == 1 && model.JoinList[0].Scene == 23
                    && model.DieTimes == 0 && model.Time == 0 && model.DieTime == 0 && model.SafeTime == 0 && model.MonsterScene == 0 && model.MonsterInfo.Count == 1 && IsMonster(model.MonsterInfo[0], singleMonster)
                    && model.DamageScene == 0 && model.DamageMonId == 0 && model.DamageRank.Count == 1 && IsDamage(model.DamageRank[0], singleDamage) && model.BossStates.Count == 3;
                var successErrorReader = new NetReader(new CliVerify.Pkt().I(1).Bytes(), 0, 4);
                on27909.Invoke(controller, new object[] { successErrorReader });
                pass &= successErrorReader.Remaining == 0 && model.HasError && model.LastErrorCode == 0;
                var maxErrorReader = new NetReader(new CliVerify.Pkt().I(uint.MaxValue).Bytes(), 0, 4);
                on27909.Invoke(controller, new object[] { maxErrorReader });
                pass &= maxErrorReader.Remaining == 0 && model.HasError && model.LastErrorCode == uint.MaxValue;
                var smallErrorReader = new NetReader(new CliVerify.Pkt().I(7).Bytes(), 0, 4);
                on27909.Invoke(controller, new object[] { smallErrorReader });
                pass &= smallErrorReader.Remaining == 0 && model.HasError && model.LastErrorCode == 7;

                byte[] bossIsolatedTimeBytes = new CliVerify.Pkt().I(31).I(32).I(33).Bytes();
                var bossIsolatedTimeReader = new NetReader(bossIsolatedTimeBytes, 0, bossIsolatedTimeBytes.Length);
                on27900.Invoke(controller, new object[] { bossIsolatedTimeReader });
                pass &= bossIsolatedTimeReader.Remaining == 0 && model.OpenTime == 31 && model.EnterTime == 32 && model.EndTime == 33
                    && model.BossStates.Count == 3 && IsBoss(model.BossStates[77], 77, 88, 99, 100, "other") && model.HasError && model.LastErrorCode == 7;
                byte[] bossIsolatedJoinBytes = JoinPacket(2, new[] { new JoinSpec(34, 35, 36) });
                var bossIsolatedJoinReader = new NetReader(bossIsolatedJoinBytes, 0, bossIsolatedJoinBytes.Length);
                on27901.Invoke(controller, new object[] { bossIsolatedJoinReader });
                pass &= bossIsolatedJoinReader.Remaining == 0 && model.CanEnterScene == 2 && model.JoinList.Count == 1 && model.JoinList[0].Scene == 34
                    && model.BossStates.Count == 3 && IsBoss(model.BossStates[0], 0, 0, 0, 0, string.Empty) && model.HasError && model.LastErrorCode == 7;
                byte[] bossIsolatedReliveBytes = new CliVerify.Pkt().H(37).I(38).I(39).I(40).Bytes();
                var bossIsolatedReliveReader = new NetReader(bossIsolatedReliveBytes, 0, bossIsolatedReliveBytes.Length);
                on27906.Invoke(controller, new object[] { bossIsolatedReliveReader });
                pass &= bossIsolatedReliveReader.Remaining == 0 && model.DieTimes == 37 && model.Time == 38 && model.DieTime == 39 && model.SafeTime == 40
                    && model.BossStates.Count == 3 && IsBoss(model.BossStates[uint.MaxValue], uint.MaxValue, 1, 2, 3, "small") && model.HasError && model.LastErrorCode == 7;

                byte[] bossBidirectionalBytes = BossPacket(77, 41, 42, 43, "later");
                var bossBidirectionalReader = new NetReader(bossBidirectionalBytes, 0, bossBidirectionalBytes.Length);
                on27908.Invoke(controller, new object[] { bossBidirectionalReader });
                pass &= bossBidirectionalReader.Remaining == 0 && IsBoss(model.BossStates[77], 77, 41, 42, 43, "later")
                    && model.OpenTime == 31 && model.EnterTime == 32 && model.EndTime == 33 && model.CanEnterScene == 2 && model.JoinList.Count == 1 && model.JoinList[0].Scene == 34
                    && model.DieTimes == 37 && model.Time == 38 && model.DieTime == 39 && model.SafeTime == 40 && model.DamageRank.Count == 1 && IsDamage(model.DamageRank[0], singleDamage) && model.HasError && model.LastErrorCode == 7;

                MonsterSpec rebornMonster = new MonsterSpec(44, 45, 46, 47, "reborn", 48, 49);
                byte[] rebornMonsterInfoBytes = MonsterPacket(9, new[] { rebornMonster });
                var rebornMonsterInfoReader = new NetReader(rebornMonsterInfoBytes, 0, rebornMonsterInfoBytes.Length);
                on27904.Invoke(controller, new object[] { rebornMonsterInfoReader });
                var unknownRebornReader = new NetReader(new CliVerify.Pkt().I(999).Bytes(), 0, 4);
                on27907.Invoke(controller, new object[] { unknownRebornReader });
                pass &= rebornMonsterInfoReader.Remaining == 0 && unknownRebornReader.Remaining == 0 && model.MonsterScene == 9 && model.MonsterInfo.Count == 1
                    && IsMonster(model.MonsterInfo[0], rebornMonster) && model.HasError && model.LastErrorCode == 7;
                var matchedRebornReader = new NetReader(new CliVerify.Pkt().I(44).Bytes(), 0, 4);
                on27907.Invoke(controller, new object[] { matchedRebornReader });
                pass &= matchedRebornReader.Remaining == 0 && model.HasMonsterInfo && model.MonsterScene == 9 && model.MonsterInfo.Count == 1
                    && IsMonster(model.MonsterInfo[0], new MonsterSpec(44, 45, 46, 47, "reborn", 48, 0))
                    && model.OpenTime == 31 && model.EnterTime == 32 && model.EndTime == 33 && model.CanEnterScene == 2 && model.JoinList.Count == 1 && model.JoinList[0].Scene == 34
                    && model.DieTimes == 37 && model.Time == 38 && model.DieTime == 39 && model.SafeTime == 40 && model.DamageRank.Count == 1 && IsDamage(model.DamageRank[0], singleDamage)
                    && IsBoss(model.BossStates[77], 77, 41, 42, 43, "later") && model.HasError && model.LastErrorCode == 7;

                byte[] emptyDamageBytes = DamagePacket(7, 8, new DamageSpec[0]);
                var emptyDamageReader = new NetReader(emptyDamageBytes, 0, emptyDamageBytes.Length);
                on27905.Invoke(controller, new object[] { emptyDamageReader });
                pass &= emptyDamageReader.Remaining == 0 && model.HasDamageRank && model.DamageScene == 7 && model.DamageMonId == 8 && model.DamageRank.Count == 0
                    && model.OpenTime == 31 && model.EnterTime == 32 && model.EndTime == 33 && model.CanEnterScene == 2 && model.JoinList.Count == 1 && model.JoinList[0].Scene == 34
                    && model.DieTimes == 37 && model.Time == 38 && model.DieTime == 39 && model.SafeTime == 40 && IsBoss(model.BossStates[77], 77, 41, 42, 43, "later") && model.MonsterScene == 9 && model.MonsterInfo.Count == 1 && IsMonster(model.MonsterInfo[0], new MonsterSpec(44, 45, 46, 47, "reborn", 48, 0)) && model.HasError && model.LastErrorCode == 7;

                byte[] emptyMonsterBytes = MonsterPacket(7, new MonsterSpec[0]);
                var emptyMonsterReader = new NetReader(emptyMonsterBytes, 0, emptyMonsterBytes.Length);
                on27904.Invoke(controller, new object[] { emptyMonsterReader });
                pass &= emptyMonsterReader.Remaining == 0 && model.HasMonsterInfo && model.MonsterScene == 7 && model.MonsterInfo.Count == 0
                    && model.OpenTime == 31 && model.EnterTime == 32 && model.EndTime == 33 && model.CanEnterScene == 2 && model.JoinList.Count == 1 && model.JoinList[0].Scene == 34
                    && model.DieTimes == 37 && model.Time == 38 && model.DieTime == 39 && model.SafeTime == 40 && model.DamageScene == 7 && model.DamageMonId == 8 && model.DamageRank.Count == 0 && IsBoss(model.BossStates[77], 77, 41, 42, 43, "later") && model.HasError && model.LastErrorCode == 7;

                controller.Dispose();
                pass &= !controller.IsInitialized && !handlers.Contains(27900) && !handlers.Contains(27901) && !handlers.Contains(27904) && !handlers.Contains(27905) && !handlers.Contains(27906) && !handlers.Contains(27907) && !handlers.Contains(27908) && !handlers.Contains(27909)
                    && !model.HasData && !model.HasJoinInfo && !model.HasReliveInfo && model.OpenTime == 0 && model.EnterTime == 0 && model.EndTime == 0 && model.CanEnterScene == 0 && model.JoinList.Count == 0
                    && model.DieTimes == 0 && model.Time == 0 && model.DieTime == 0 && model.SafeTime == 0 && !model.HasMonsterInfo && model.MonsterScene == 0 && model.MonsterInfo.Count == 0 && !model.HasDamageRank && model.DamageScene == 0 && model.DamageMonId == 0 && model.DamageRank.Count == 0 && !model.HasBossStates && model.BossStates.Count == 0 && !model.HasError && model.LastErrorCode == 0;

                Debug.Log("CLIVERIFY eternity VERDICT pass=" + pass);
            }
            finally
            {
                try
                {
                    if (controller.IsInitialized)
                    {
                        controller.Dispose();
                    }

                    model.Reset();
                    if (oldHasData)
                    {
                        model.Replace(oldOpenTime, oldEnterTime, oldEndTime);
                    }
                    if (oldHasJoinInfo)
                    {
                        model.ReplaceJoinInfo(oldCanEnterScene, oldJoinList);
                    }
                    if (oldHasReliveInfo)
                    {
                        model.ReplaceReliveInfo(oldDieTimes, oldTime, oldDieTime, oldSafeTime);
                    }
                    if (oldHasMonsterInfo)
                    {
                        model.ReplaceMonsterInfo(oldMonsterScene, oldMonsterInfo);
                    }
                    if (oldHasDamageRank)
                    {
                        model.ReplaceDamageRank(oldDamageScene, oldDamageMonId, oldDamageRank);
                    }
                    if (oldHasBossStates)
                    {
                        foreach (EternityModel.BossStateEntry bossState in oldBossStates)
                        {
                            model.ReplaceBossState(bossState);
                        }
                    }
                    if (oldHasError)
                    {
                        model.SetError(oldLastErrorCode);
                    }

                    role.Level = oldLevel;
                    if (hasBaseInfoField != null)
                    {
                        hasBaseInfoField.SetValue(role, oldHasBaseInfo);
                    }

                    if (wasInitialized)
                    {
                        controller.Init();
                    }

                    if (lastLevelField != null)
                    {
                        lastLevelField.SetValue(controller, oldLastLevel);
                    }

                    if (interceptField != null)
                    {
                        interceptField.SetValue(null, oldIntercept);
                    }

                    if (handlers != null)
                    {
                        foreach (int proto in new[] { 27900, 27901, 27904, 27905, 27906, 27907, 27908, 27909 })
                        {
                            if (oldHandlers.TryGetValue(proto, out object handler)) handlers[proto] = handler;
                            else handlers.Remove(proto);
                        }
                    }

                    if (eventHandlers != null)
                    {
                        eventHandlers.Remove(GlobalEvent.EVT_ROLE_INFO_UPDATE);
                        if (oldHadRoleEvent)
                        {
                            eventHandlers[GlobalEvent.EVT_ROLE_INFO_UPDATE] = new List<Delegate>(oldRoleSubscribers);
                        }
                    }

                    restored = controller.IsInitialized == wasInitialized
                        && model.HasData == oldHasData && (!oldHasData || model.OpenTime == oldOpenTime && model.EnterTime == oldEnterTime && model.EndTime == oldEndTime)
                        && model.HasJoinInfo == oldHasJoinInfo && (!oldHasJoinInfo || model.CanEnterScene == oldCanEnterScene && SameJoins(model.JoinList, oldJoinList))
                        && model.HasReliveInfo == oldHasReliveInfo && (!oldHasReliveInfo || model.DieTimes == oldDieTimes && model.Time == oldTime && model.DieTime == oldDieTime && model.SafeTime == oldSafeTime)
                        && model.HasMonsterInfo == oldHasMonsterInfo && (oldHasMonsterInfo ? model.MonsterScene == oldMonsterScene && SameMonsterInfo(model.MonsterInfo, oldMonsterInfo) : model.MonsterScene == 0 && model.MonsterInfo.Count == 0)
                        && model.HasDamageRank == oldHasDamageRank && (oldHasDamageRank ? model.DamageScene == oldDamageScene && model.DamageMonId == oldDamageMonId && SameDamageRank(model.DamageRank, oldDamageRank) : model.DamageScene == 0 && model.DamageMonId == 0 && model.DamageRank.Count == 0)
                        && model.HasBossStates == oldHasBossStates && (oldHasBossStates ? SameBossStates(model.BossStates, oldBossStates) : model.BossStates.Count == 0)
                        && model.HasError == oldHasError && (!oldHasError || model.LastErrorCode == oldLastErrorCode)
                        && role.Level == oldLevel && hasBaseInfoField != null && (bool)hasBaseInfoField.GetValue(role) == oldHasBaseInfo
                        && lastLevelField != null && (int)lastLevelField.GetValue(controller) == oldLastLevel
                        && interceptField != null && ReferenceEquals(interceptField.GetValue(null), oldIntercept)
                        && HandlersMatch(handlers, oldHandlers)
                        && RoleSubscribersMatch(eventHandlers, oldHadRoleEvent, oldRoleSubscribers);
                }
                catch (Exception exception)
                {
                    Debug.LogError("CLIVERIFY eternity restore EXCEPTION " + exception);
                    restored = false;
                }
                Debug.Log("CLIVERIFY eternity restored=" + restored + " pass=" + pass);
            }
            return pass && restored ? 0 : 3;
        }

        private static bool IsExactRequest(byte[] frame)
        {
            return frame != null
                && frame.Length == 6
                && frame[0] == 0 && frame[1] == 6
                && frame[2] == 0x03 && frame[3] == 0xE8
                && frame[4] == (byte)(Proto.ETERNITY_TIME_INFO >> 8)
                && frame[5] == (byte)(Proto.ETERNITY_TIME_INFO & 0xFF);
        }

        private static bool IsExactJoinRequest(byte[] frame)
        {
            return frame != null && frame.Length == 6 && frame[0] == 0 && frame[1] == 6 && frame[2] == 0x03 && frame[3] == 0xE8
                && frame[4] == (byte)(Proto.ETERNITY_JOIN_INFO >> 8) && frame[5] == (byte)(Proto.ETERNITY_JOIN_INFO & 0xFF);
        }

        private static bool IsExactReliveRequest(byte[] frame)
        {
            return frame != null && frame.Length == 6 && frame[0] == 0 && frame[1] == 6 && frame[2] == 0x03 && frame[3] == 0xE8
                && frame[4] == (byte)(Proto.ETERNITY_RELIVE_INFO >> 8) && frame[5] == (byte)(Proto.ETERNITY_RELIVE_INFO & 0xFF);
        }

        private static bool IsExactDamageRequest(byte[] frame, ushort scene, uint monId)
        {
            return frame != null && frame.Length == 12 && frame[0] == 0 && frame[1] == 12 && frame[2] == 0x03 && frame[3] == 0xE8
                && frame[4] == (byte)(Proto.ETERNITY_DAMAGE_RANK >> 8) && frame[5] == (byte)(Proto.ETERNITY_DAMAGE_RANK & 0xFF)
                && frame[6] == (byte)(scene >> 8) && frame[7] == (byte)scene
                && frame[8] == (byte)(monId >> 24) && frame[9] == (byte)(monId >> 16) && frame[10] == (byte)(monId >> 8) && frame[11] == (byte)monId;
        }

        private static bool IsExactMonsterRequest(byte[] frame, ushort scene)
        {
            return frame != null && frame.Length == 8 && frame[0] == 0 && frame[1] == 8 && frame[2] == 0x03 && frame[3] == 0xE8
                && frame[4] == (byte)(Proto.ETERNITY_MONSTER_INFO >> 8) && frame[5] == (byte)(Proto.ETERNITY_MONSTER_INFO & 0xFF)
                && frame[6] == (byte)(scene >> 8) && frame[7] == (byte)scene;
        }

        private static byte[] JoinPacket(byte canEnterScene, JoinSpec[] joins)
        {
            var packet = new CliVerify.Pkt().C(canEnterScene).H(joins.Length);
            foreach (JoinSpec join in joins) packet.I(join.Scene).H(join.SelfServerNum).H(join.SceneNum);
            return packet.Bytes();
        }

        private static bool IsJoin(EternityModel.JoinEntry actual, JoinSpec expected)
        {
            return actual.Scene == expected.Scene && actual.SelfServerNum == expected.SelfServerNum && actual.SceneNum == expected.SceneNum;
        }

        private static byte[] MonsterPacket(ushort scene, MonsterSpec[] entries)
        {
            var packet = new CliVerify.Pkt().H(scene).H(entries.Length);
            foreach (MonsterSpec entry in entries) packet.I(entry.MonId).H(entry.MonLv).C(entry.MonType).I(entry.BlServer).S(entry.BlServerName).I(entry.BlServerNum).I(entry.RebornTime);
            return packet.Bytes();
        }

        private static bool IsMonster(EternityModel.MonsterEntry actual, MonsterSpec expected)
        {
            return actual.MonId == expected.MonId && actual.MonLv == expected.MonLv && actual.MonType == expected.MonType && actual.BlServer == expected.BlServer
                && actual.BlServerName == expected.BlServerName && actual.BlServerNum == expected.BlServerNum && actual.RebornTime == expected.RebornTime;
        }

        private static bool SameMonsterInfo(IReadOnlyList<EternityModel.MonsterEntry> actual, IReadOnlyList<EternityModel.MonsterEntry> expected)
        {
            if (actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++)
            {
                MonsterSpec entry = new MonsterSpec(expected[i].MonId, expected[i].MonLv, expected[i].MonType, expected[i].BlServer, expected[i].BlServerName, expected[i].BlServerNum, expected[i].RebornTime);
                if (!IsMonster(actual[i], entry)) return false;
            }
            return true;
        }

        private static byte[] DamagePacket(ushort scene, uint monId, DamageSpec[] entries)
        {
            var packet = new CliVerify.Pkt().H(scene).I(monId).H(entries.Length);
            foreach (DamageSpec entry in entries) packet.I(entry.ServerId).H(entry.ServerNum).S(entry.ServerName).I(entry.PlayerId).S(entry.PlayerName).H(entry.Damage);
            return packet.Bytes();
        }

        private static bool IsDamage(EternityModel.DamageEntry actual, DamageSpec expected)
        {
            return actual.ServerId == expected.ServerId && actual.ServerNum == expected.ServerNum && actual.ServerName == expected.ServerName
                && actual.PlayerId == expected.PlayerId && actual.PlayerName == expected.PlayerName && actual.Damage == expected.Damage;
        }

        private static bool SameDamageRank(IReadOnlyList<EternityModel.DamageEntry> actual, IReadOnlyList<EternityModel.DamageEntry> expected)
        {
            if (actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++)
            {
                DamageSpec entry = new DamageSpec(expected[i].ServerId, expected[i].ServerNum, expected[i].ServerName, expected[i].PlayerId, expected[i].PlayerName, expected[i].Damage);
                if (!IsDamage(actual[i], entry)) return false;
            }
            return true;
        }

        private static byte[] BossPacket(long monId, long rebornTime, long blServer, long blServerNum, string blServerName)
        {
            return new CliVerify.Pkt().I(monId).I(rebornTime).I(blServer).I(blServerNum).S(blServerName).Bytes();
        }

        private static bool IsBoss(EternityModel.BossStateEntry actual, uint monId, uint rebornTime, uint blServer, uint blServerNum, string blServerName)
        {
            return actual.MonId == monId && actual.RebornTime == rebornTime && actual.BlServer == blServer && actual.BlServerNum == blServerNum && actual.BlServerName == blServerName;
        }

        private static bool HasProtocol(IReadOnlyList<byte[]> frames, int proto)
        {
            foreach (byte[] frame in frames)
            {
                if (frame != null && frame.Length >= 6 && frame[4] == (byte)(proto >> 8) && frame[5] == (byte)(proto & 0xFF)) return true;
            }
            return false;
        }

        private static bool SameBossStates(IReadOnlyDictionary<uint, EternityModel.BossStateEntry> actual, IReadOnlyList<EternityModel.BossStateEntry> expected)
        {
            if (actual.Count != expected.Count) return false;
            foreach (EternityModel.BossStateEntry expectedState in expected)
            {
                if (!actual.TryGetValue(expectedState.MonId, out EternityModel.BossStateEntry actualState)
                    || !IsBoss(actualState, expectedState.MonId, expectedState.RebornTime, expectedState.BlServer, expectedState.BlServerNum, expectedState.BlServerName)) return false;
            }
            return true;
        }

        private static bool SameJoins(IReadOnlyList<EternityModel.JoinEntry> actual, IReadOnlyList<EternityModel.JoinEntry> expected)
        {
            if (actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++)
            {
                if (actual[i].Scene != expected[i].Scene || actual[i].SelfServerNum != expected[i].SelfServerNum || actual[i].SceneNum != expected[i].SceneNum) return false;
            }
            return true;
        }

        private static bool HandlersMatch(IDictionary handlers, Dictionary<int, object> expected)
        {
            if (handlers == null) return false;
            foreach (int proto in new[] { 27900, 27901, 27904, 27905, 27906, 27907, 27908, 27909 })
            {
                bool had = expected.TryGetValue(proto, out object handler);
                if (handlers.Contains(proto) != had || had && !ReferenceEquals(handlers[proto], handler)) return false;
            }
            return true;
        }

        private static bool RoleSubscribersMatch(IDictionary handlers, bool expectedHadEvent, List<Delegate> expected)
        {
            if (handlers == null || handlers.Contains(GlobalEvent.EVT_ROLE_INFO_UPDATE) != expectedHadEvent) return false;
            if (!expectedHadEvent) return true;
            var actual = handlers[GlobalEvent.EVT_ROLE_INFO_UPDATE] as List<Delegate>;
            if (actual == null || actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++)
            {
                if (!ReferenceEquals(actual[i], expected[i])) return false;
            }
            return true;
        }

        private struct JoinSpec
        {
            public readonly uint Scene; public readonly ushort SelfServerNum; public readonly ushort SceneNum;
            public JoinSpec(uint scene, ushort selfServerNum, ushort sceneNum) { Scene = scene; SelfServerNum = selfServerNum; SceneNum = sceneNum; }
        }

        private struct DamageSpec
        {
            public readonly uint ServerId; public readonly ushort ServerNum; public readonly string ServerName; public readonly uint PlayerId; public readonly string PlayerName; public readonly ushort Damage;
            public DamageSpec(uint serverId, ushort serverNum, string serverName, uint playerId, string playerName, ushort damage)
            {
                ServerId = serverId; ServerNum = serverNum; ServerName = serverName; PlayerId = playerId; PlayerName = playerName; Damage = damage;
            }
        }

        private struct MonsterSpec
        {
            public readonly uint MonId; public readonly ushort MonLv; public readonly byte MonType; public readonly uint BlServer; public readonly string BlServerName; public readonly uint BlServerNum; public readonly uint RebornTime;
            public MonsterSpec(uint monId, ushort monLv, byte monType, uint blServer, string blServerName, uint blServerNum, uint rebornTime)
            {
                MonId = monId; MonLv = monLv; MonType = monType; BlServer = blServer; BlServerName = blServerName; BlServerNum = blServerNum; RebornTime = rebornTime;
            }
        }
    }
}
