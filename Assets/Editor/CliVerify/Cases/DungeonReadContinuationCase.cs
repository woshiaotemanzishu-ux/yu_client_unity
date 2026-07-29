using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Dungeon;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// R504 / 61031-35、61041-42 专项：六种精确请求、61033 S2C-only、完整替换/首项增量、
    /// u8/u16/u32/u64 边界、有序重复项、空表已加载、分桶隔离、无回包保留及 ambient 深恢复。
    /// </summary>
    public static class DungeonReadContinuationCase
    {
        private const BindingFlags IF = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags SF = BindingFlags.Static | BindingFlags.NonPublic;

        private sealed class RankWire
        {
            public ulong RoleId;
            public string Name;
            public ulong Hurt;
        }

        private sealed class HpWire
        {
            public uint AutoId;
            public uint MonTypeId;
            public ulong Hp;
            public ulong HpLimit;
        }

        private sealed class WaveWire
        {
            public ushort Type;
            public string Args;
            public ushort Subtype;
            public ushort Cycle;
            public ushort MaxCycle;
            public uint NextTime;
        }

        private sealed class RewardWire
        {
            public uint DunId;
            public byte Type;
            public byte Status;
        }

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY dungeon-read-continuation EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            DungeonController controller = DungeonController.Instance;
            DungeonModel model = DungeonModel.Instance;
            bool oldInitialized = controller.IsInitialized;
            bool oldHasKill = model.HasGuildGuardKillCount;
            uint oldKill = model.GuildGuardKillCount;
            DungeonModel.GuildGuardDamageRankSnapshot oldRank = model.GuildGuardDamageRank;
            bool oldHasHp = model.HasGuildGuardBossHp;
            IReadOnlyList<DungeonModel.GuildGuardBossHpEntry> oldHp = model.GuildGuardBossHp;
            var oldWaves = new Dictionary<uint, DungeonModel.GuildGuardWaveSnapshot>(model.GuildGuardWavesByDunId);
            var oldExp = new Dictionary<uint, ulong>(model.AccumulatedExpByDunId);
            var oldRewards = new Dictionary<byte, DungeonModel.ExtraRewardSnapshot>(model.ExtraRewardsByDunType);

            FieldInfo intercept = typeof(DungeonController).GetField(
                "s_dungeonReadContinuationOutboundIntercept", SF);
            object oldIntercept = intercept?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            int[] commands = { 61031, 61032, 61033, 61034, 61035, 61041, 61042 };
            var oldHandlers = new Dictionary<int, object>();
            if (handlers != null)
                foreach (int cmd in commands)
                    if (handlers.Contains(cmd)) oldHandlers[cmd] = handlers[cmd];

            bool pass = false;
            bool restored = false;
            try
            {
                model.ClearDungeonReadContinuationState();
                MethodInfo h31 = Handler("On61031");
                MethodInfo h32 = Handler("On61032");
                MethodInfo h33 = Handler("On61033");
                MethodInfo h34 = Handler("On61034");
                MethodInfo h35 = Handler("On61035");
                MethodInfo h41 = Handler("On61041");
                MethodInfo h42 = Handler("On61042");
                pass = Proto.DUNGEON_GUILD_GUARD_KILL_COUNT == 61031
                    && Proto.DUNGEON_GUILD_GUARD_DAMAGE_RANK == 61032
                    && Proto.DUNGEON_GUILD_GUARD_BOSS_HP_PUSH == 61033
                    && Proto.DUNGEON_GUILD_GUARD_BOSS_HP == 61034
                    && Proto.DUNGEON_GUILD_GUARD_WAVE_INFO == 61035
                    && Proto.DUNGEON_ACCUMULATED_EXP == 61041
                    && Proto.DUNGEON_EXTRA_REWARD_INFO == 61042
                    && h31 != null && h32 != null && h33 != null && h34 != null && h35 != null
                    && h41 != null && h42 != null && intercept != null
                    && (!oldInitialized || oldHandlers.Count == commands.Length)
                    && typeof(DungeonController).GetMethod("RequestGuildGuardBossHpPush") == null;
                Check(ref pass, "constants/registration/61033-s2c-only", pass);

                model.ApplyGuildGuardKillCount(77);
                model.ApplyAccumulatedExp(88, 99);
                var frames = new List<byte[]>();
                intercept.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                controller.RequestGuildGuardKillCount();
                controller.RequestGuildGuardDamageRank();
                controller.RequestGuildGuardBossHp();
                controller.RequestGuildGuardWaveInfo();
                controller.RequestAccumulatedExp(uint.MaxValue);
                controller.RequestExtraRewardInfo(byte.MaxValue);
                Check(ref pass, "exact request frames/no-reply-preserves", frames.Count == 6
                    && Frame(frames[0], 61031)
                    && Frame(frames[1], 61032)
                    && Frame(frames[2], 61034)
                    && Frame(frames[3], 61035)
                    && Frame(frames[4], 61041, 255, 255, 255, 255)
                    && Frame(frames[5], 61042, 255)
                    && model.HasGuildGuardKillCount && model.GuildGuardKillCount == 77
                    && model.TryGetAccumulatedExp(88, out ulong preserved) && preserved == 99);

                Check(ref pass, "61031 u32 absolute replace/read-end",
                    Feed(h31, controller, new CliVerify.Pkt().I(uint.MaxValue))
                    && model.HasGuildGuardKillCount && model.GuildGuardKillCount == uint.MaxValue
                    && Feed(h31, controller, new CliVerify.Pkt().I(0))
                    && model.GuildGuardKillCount == 0);

                var ranks = new[]
                {
                    new RankWire { RoleId = ulong.MaxValue, Name = "甲", Hurt = 0 },
                    new RankWire { RoleId = 0, Name = string.Empty, Hurt = ulong.MaxValue },
                    new RankWire { RoleId = ulong.MaxValue, Name = "重", Hurt = 7 },
                };
                Check(ref pass, "61032 full ordered duplicate/u64/utf8",
                    FeedRank(h32, controller, 255, ulong.MaxValue, ranks)
                    && RankSnapshot(model.GuildGuardDamageRank, 255, ulong.MaxValue, ranks));
                DungeonModel.GuildGuardDamageRankSnapshot rankBefore = model.GuildGuardDamageRank;
                Check(ref pass, "61032 multi-to-empty loaded replace",
                    FeedRank(h32, controller, 0, 0, Array.Empty<RankWire>())
                    && model.GuildGuardDamageRank != rankBefore
                    && RankSnapshot(model.GuildGuardDamageRank, 0, 0, Array.Empty<RankWire>()));

                var hpRows = new[]
                {
                    new HpWire { AutoId = 7, MonTypeId = 70, Hp = 700, HpLimit = 701 },
                    new HpWire { AutoId = 7, MonTypeId = 71, Hp = 710, HpLimit = 711 },
                    new HpWire { AutoId = uint.MaxValue, MonTypeId = 0, Hp = ulong.MaxValue, HpLimit = 0 },
                };
                Check(ref pass, "61034 full ordered duplicate/bounds",
                    FeedHpList(h34, controller, hpRows) && HpSnapshot(model, hpRows));
                Check(ref pass, "61033 replaces first matching auto-id only",
                    FeedHp(h33, controller, new HpWire { AutoId = 7, MonTypeId = 99, Hp = 1, HpLimit = 2 })
                    && model.GuildGuardBossHp.Count == 3
                    && HpEquals(model.GuildGuardBossHp[0], new HpWire { AutoId = 7, MonTypeId = 99, Hp = 1, HpLimit = 2 })
                    && HpEquals(model.GuildGuardBossHp[1], hpRows[1]));
                Check(ref pass, "61033 unknown appends",
                    FeedHp(h33, controller, new HpWire { AutoId = 8, MonTypeId = 80, Hp = 0, HpLimit = ulong.MaxValue })
                    && model.GuildGuardBossHp.Count == 4
                    && HpEquals(model.GuildGuardBossHp[3], new HpWire { AutoId = 8, MonTypeId = 80, Hp = 0, HpLimit = ulong.MaxValue }));
                Check(ref pass, "61034 empty clears but remains loaded",
                    FeedHpList(h34, controller, Array.Empty<HpWire>())
                    && model.HasGuildGuardBossHp && model.GuildGuardBossHp.Count == 0);

                var waves = new[]
                {
                    new WaveWire { Type = 0, Args = "[{甲,1}]", Subtype = 65535, Cycle = 1, MaxCycle = 2, NextTime = uint.MaxValue },
                    new WaveWire { Type = 0, Args = string.Empty, Subtype = 0, Cycle = 0, MaxCycle = 0, NextTime = 0 },
                };
                Check(ref pass, "61035 keyed full ordered duplicate/utf8",
                    FeedWaves(h35, controller, uint.MaxValue, waves)
                    && WaveSnapshot(model, uint.MaxValue, waves));
                Check(ref pass, "61035 other-key isolation/same-key empty",
                    FeedWaves(h35, controller, 0, new[] { waves[1] })
                    && WaveSnapshot(model, uint.MaxValue, waves)
                    && FeedWaves(h35, controller, uint.MaxValue, Array.Empty<WaveWire>())
                    && WaveSnapshot(model, uint.MaxValue, Array.Empty<WaveWire>())
                    && WaveSnapshot(model, 0, new[] { waves[1] }));

                Check(ref pass, "61041 keyed zero/max/replace",
                    Feed(h41, controller, new CliVerify.Pkt().I(0).L(unchecked((long)ulong.MaxValue)))
                    && model.TryGetAccumulatedExp(0, out ulong exp0) && exp0 == ulong.MaxValue
                    && Feed(h41, controller, new CliVerify.Pkt().I(uint.MaxValue).L(0))
                    && model.TryGetAccumulatedExp(uint.MaxValue, out ulong expMax) && expMax == 0
                    && Feed(h41, controller, new CliVerify.Pkt().I(0).L(5))
                    && model.TryGetAccumulatedExp(0, out exp0) && exp0 == 5
                    && model.TryGetAccumulatedExp(uint.MaxValue, out expMax) && expMax == 0);

                var rewards = new[]
                {
                    new RewardWire { DunId = uint.MaxValue, Type = 0, Status = 255 },
                    new RewardWire { DunId = uint.MaxValue, Type = 0, Status = 7 },
                    new RewardWire { DunId = 0, Type = 255, Status = 0 },
                };
                Check(ref pass, "61042 keyed ordered duplicate/bounds",
                    FeedRewards(h42, controller, 255, rewards) && RewardSnapshot(model, 255, rewards));
                Check(ref pass, "61042 other-key isolation/same-key empty",
                    FeedRewards(h42, controller, 0, new[] { rewards[2] })
                    && RewardSnapshot(model, 255, rewards)
                    && FeedRewards(h42, controller, 255, Array.Empty<RewardWire>())
                    && RewardSnapshot(model, 255, Array.Empty<RewardWire>())
                    && RewardSnapshot(model, 0, new[] { rewards[2] }));

                model.ClearDungeonReadContinuationState();
                Check(ref pass, "clear owns all seven slices",
                    !model.HasGuildGuardKillCount && model.GuildGuardDamageRank == null
                    && !model.HasGuildGuardBossHp && model.GuildGuardBossHp.Count == 0
                    && model.GuildGuardWavesByDunId.Count == 0 && model.AccumulatedExpByDunId.Count == 0
                    && model.ExtraRewardsByDunType.Count == 0);
                Check(ref pass, "ambient controller/handlers untouched",
                    controller.IsInitialized == oldInitialized && SameHandlers(handlers, commands, oldHandlers));
                Debug.Log("CLIVERIFY dungeon-read-continuation VERDICT pass=" + pass);
            }
            finally
            {
                model.ClearDungeonReadContinuationState();
                SetAuto(model, "HasGuildGuardKillCount", oldHasKill);
                SetAuto(model, "GuildGuardKillCount", oldKill);
                SetAuto(model, "GuildGuardDamageRank", oldRank);
                SetAuto(model, "HasGuildGuardBossHp", oldHasHp);
                SetAuto(model, "GuildGuardBossHp", oldHp);
                foreach (var pair in oldWaves) model.GuildGuardWavesByDunId.Add(pair.Key, pair.Value);
                foreach (var pair in oldExp) model.AccumulatedExpByDunId.Add(pair.Key, pair.Value);
                foreach (var pair in oldRewards) model.ExtraRewardsByDunType.Add(pair.Key, pair.Value);
                if (intercept != null) intercept.SetValue(null, oldIntercept);
                restored = controller.IsInitialized == oldInitialized
                    && model.HasGuildGuardKillCount == oldHasKill && model.GuildGuardKillCount == oldKill
                    && ReferenceEquals(model.GuildGuardDamageRank, oldRank)
                    && model.HasGuildGuardBossHp == oldHasHp && ReferenceEquals(model.GuildGuardBossHp, oldHp)
                    && SameDictionary(model.GuildGuardWavesByDunId, oldWaves)
                    && SameDictionary(model.AccumulatedExpByDunId, oldExp)
                    && SameDictionary(model.ExtraRewardsByDunType, oldRewards)
                    && SameHandlers(handlers, commands, oldHandlers)
                    && (intercept == null || ReferenceEquals(intercept.GetValue(null), oldIntercept));
                Debug.Log("CLIVERIFY dungeon-read-continuation restored=" + restored);
            }
            return pass && restored ? 0 : 3;
        }

        private static MethodInfo Handler(string name) => typeof(DungeonController).GetMethod(name, IF);

        private static bool Feed(MethodInfo method, DungeonController controller, CliVerify.Pkt packet)
        {
            byte[] bytes = packet.Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            method.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool FeedRank(MethodInfo method, DungeonController controller, byte myRank,
            ulong myHurt, IReadOnlyList<RankWire> rows)
        {
            var p = new CliVerify.Pkt().C(myRank).L(unchecked((long)myHurt)).H(rows.Count);
            for (int i = 0; i < rows.Count; i++)
                p.L(unchecked((long)rows[i].RoleId)).S(rows[i].Name).L(unchecked((long)rows[i].Hurt));
            return Feed(method, controller, p);
        }

        private static bool FeedHp(MethodInfo method, DungeonController controller, HpWire row) =>
            Feed(method, controller, HpPacket(new CliVerify.Pkt(), row));

        private static bool FeedHpList(MethodInfo method, DungeonController controller, IReadOnlyList<HpWire> rows)
        {
            var p = new CliVerify.Pkt().H(rows.Count);
            for (int i = 0; i < rows.Count; i++) HpPacket(p, rows[i]);
            return Feed(method, controller, p);
        }

        private static CliVerify.Pkt HpPacket(CliVerify.Pkt p, HpWire row) =>
            p.I(row.AutoId).I(row.MonTypeId).L(unchecked((long)row.Hp)).L(unchecked((long)row.HpLimit));

        private static bool FeedWaves(MethodInfo method, DungeonController controller, uint dunId,
            IReadOnlyList<WaveWire> rows)
        {
            var p = new CliVerify.Pkt().I(dunId).H(rows.Count);
            for (int i = 0; i < rows.Count; i++)
            {
                WaveWire row = rows[i];
                p.H(row.Type).S(row.Args).H(row.Subtype).H(row.Cycle).H(row.MaxCycle).I(row.NextTime);
            }
            return Feed(method, controller, p);
        }

        private static bool FeedRewards(MethodInfo method, DungeonController controller, byte dunType,
            IReadOnlyList<RewardWire> rows)
        {
            var p = new CliVerify.Pkt().C(dunType).H(rows.Count);
            for (int i = 0; i < rows.Count; i++) p.I(rows[i].DunId).C(rows[i].Type).C(rows[i].Status);
            return Feed(method, controller, p);
        }

        private static bool RankSnapshot(DungeonModel.GuildGuardDamageRankSnapshot got, byte rank,
            ulong hurt, IReadOnlyList<RankWire> expected)
        {
            if (got == null || !got.Loaded || got.MyRank != rank || got.MyHurt != hurt
                || got.RankList.Count != expected.Count) return false;
            for (int i = 0; i < expected.Count; i++)
                if (got.RankList[i].RoleId != expected[i].RoleId || got.RankList[i].RoleName != expected[i].Name
                    || got.RankList[i].HurtValue != expected[i].Hurt) return false;
            return true;
        }

        private static bool HpSnapshot(DungeonModel model, IReadOnlyList<HpWire> expected)
        {
            if (!model.HasGuildGuardBossHp || model.GuildGuardBossHp.Count != expected.Count) return false;
            for (int i = 0; i < expected.Count; i++) if (!HpEquals(model.GuildGuardBossHp[i], expected[i])) return false;
            return true;
        }

        private static bool HpEquals(DungeonModel.GuildGuardBossHpEntry got, HpWire expected) =>
            got.AutoId == expected.AutoId && got.MonTypeId == expected.MonTypeId
            && got.Hp == expected.Hp && got.HpLimit == expected.HpLimit;

        private static bool WaveSnapshot(DungeonModel model, uint dunId, IReadOnlyList<WaveWire> expected)
        {
            if (!model.TryGetGuildGuardWaves(dunId, out var got) || !got.Loaded
                || got.WaveList.Count != expected.Count) return false;
            for (int i = 0; i < expected.Count; i++)
            {
                DungeonModel.GuildGuardWaveEntry a = got.WaveList[i];
                WaveWire e = expected[i];
                if (a.WaveType != e.Type || a.WaveTypeArgs != e.Args || a.WaveSubtype != e.Subtype
                    || a.CycleNum != e.Cycle || a.MaxCycleNum != e.MaxCycle || a.NextWaveTime != e.NextTime)
                    return false;
            }
            return true;
        }

        private static bool RewardSnapshot(DungeonModel model, byte dunType, IReadOnlyList<RewardWire> expected)
        {
            if (!model.TryGetExtraRewardInfo(dunType, out var got) || !got.Loaded
                || got.DunList.Count != expected.Count) return false;
            for (int i = 0; i < expected.Count; i++)
                if (got.DunList[i].DunId != expected[i].DunId || got.DunList[i].RewardType != expected[i].Type
                    || got.DunList[i].RewardStatus != expected[i].Status) return false;
            return true;
        }

        private static bool Frame(byte[] frame, int command, params byte[] payload)
        {
            if (frame == null || frame.Length != 6 + payload.Length) return false;
            int length = (frame[0] << 8) | frame[1];
            int actualCommand = (frame[4] << 8) | frame[5];
            if (length != frame.Length || frame[2] != 3 || frame[3] != 232 || actualCommand != command)
                return false;
            for (int i = 0; i < payload.Length; i++) if (frame[6 + i] != payload[i]) return false;
            return true;
        }

        private static void SetAuto<T>(DungeonModel model, string property, T value)
        {
            FieldInfo field = typeof(DungeonModel).GetField("<" + property + ">k__BackingField", IF);
            if (field == null) throw new MissingFieldException(typeof(DungeonModel).FullName, property);
            field.SetValue(model, value);
        }

        private static bool SameDictionary<TKey, TValue>(Dictionary<TKey, TValue> actual,
            Dictionary<TKey, TValue> expected)
        {
            if (actual.Count != expected.Count) return false;
            foreach (var pair in expected)
                if (!actual.TryGetValue(pair.Key, out TValue value)
                    || !EqualityComparer<TValue>.Default.Equals(value, pair.Value)) return false;
            return true;
        }

        private static bool SameHandlers(IDictionary handlers, IReadOnlyList<int> commands,
            Dictionary<int, object> oldHandlers)
        {
            if (handlers == null) return oldHandlers.Count == 0;
            for (int i = 0; i < commands.Count; i++)
            {
                int cmd = commands[i];
                bool oldExists = oldHandlers.TryGetValue(cmd, out object oldValue);
                if (handlers.Contains(cmd) != oldExists || (oldExists && !ReferenceEquals(handlers[cmd], oldValue)))
                    return false;
            }
            return true;
        }

        private static void Check(ref bool pass, string name, bool ok)
        {
            Debug.Log("CLIVERIFY dungeon-read-continuation " + name + " ok=" + ok);
            pass &= ok;
        }
    }
}
