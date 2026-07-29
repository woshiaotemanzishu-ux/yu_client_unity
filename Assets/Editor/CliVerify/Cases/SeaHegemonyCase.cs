using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.SeaHegemony;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>四海争霸186安全读侧的wire、全量/增量、请求顺序、排除边界与环境恢复专项。</summary>
    public static class SeaHegemonyCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;

        private static readonly int[] RegisteredIds =
        {
            18600, 18601, 18604, 18607, 18608, 18609, 18611, 18612, 18614, 18615, 18616,
            18617, 18618, 18622, 18623, 18624, 18625, 18626, 18651, 18653, 18654, 18655, 18656
        };

        private static readonly int[] ExcludedIds =
            { 18602, 18603, 18605, 18606, 18610, 18613, 18619, 18620, 18621, 18650, 18652 };

        private sealed class EntryState
        {
            public bool Exists;
            public object Value;
            public readonly Dictionary<FieldInfo, object> ObjectFields = new Dictionary<FieldInfo, object>();
        }

        private sealed class FieldState
        {
            public FieldInfo Field;
            public object Value;
            public readonly Dictionary<object, object> Dictionary = new Dictionary<object, object>();
            public bool IsDictionary;
        }

        private sealed class ModelState
        {
            public readonly List<FieldState> Fields = new List<FieldState>();
        }

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY seahegemony EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            SeaHegemonyController controller = SeaHegemonyController.Instance;
            SeaHegemonyModel model = SeaHegemonyModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            ModelState oldModel = CaptureModel(model);

            FieldInfo interceptor = typeof(SeaHegemonyController).GetField("s_outboundIntercept", SF);
            object oldInterceptor = interceptor?.GetValue(null);
            FieldInfo lastLevel = typeof(SeaHegemonyController).GetField("_lastLevel", F);
            object oldLastLevel = lastLevel?.GetValue(controller);

            IDictionary handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var savedHandlers = new Dictionary<int, EntryState>();
            for (int id = 18600; id <= 18656; id++) SaveEntry(handlers, savedHandlers, id);
            SaveEntry(handlers, savedHandlers, 18700);

            var events = typeof(EventDispatcher).GetField("_handlers", SF)?.GetValue(null)
                as Dictionary<string, List<Delegate>>;
            bool hadRoleEvent = events != null && events.ContainsKey(GlobalEvent.EVT_ROLE_INFO_UPDATE);
            var oldRoleSubscribers = hadRoleEvent
                ? new List<Delegate>(events[GlobalEvent.EVT_ROLE_INFO_UPDATE])
                : new List<Delegate>();

            ActivityIconManager iconManager = ActivityIconManager.Instance;
            IDictionary icons = typeof(ActivityIconManager).GetField("_iconInfoByType", F)?.GetValue(iconManager)
                as IDictionary;
            IDictionary boxIcons = typeof(ActivityIconManager).GetField("_iconBoxInfoByType", F)?.GetValue(iconManager)
                as IDictionary;
            IDictionary redDots = typeof(ActivityIconManager).GetField("_redDotByType", F)?.GetValue(iconManager)
                as IDictionary;
            EntryState oldIcon = CaptureEntry(icons, SeaHegemonyModel.ICON_TYPE);
            EntryState oldBoxIcon = CaptureEntry(boxIcons, SeaHegemonyModel.ICON_TYPE);
            EntryState oldRedIcon = CaptureEntry(icons, SeaHegemonyModel.RED_ICON_TYPE);
            EntryState oldRedBoxIcon = CaptureEntry(boxIcons, SeaHegemonyModel.RED_ICON_TYPE);
            EntryState oldRedDot = CaptureEntry(redDots, SeaHegemonyModel.RED_ICON_TYPE);

            bool pass = false;
            bool restored = false;
            try
            {
                controller.Init();
                model.Reset();
                var methods = new Dictionary<int, MethodInfo>();
                foreach (int id in RegisteredIds)
                    methods[id] = typeof(SeaHegemonyController).GetMethod("On" + id, F);

                var frames = new List<byte[]>();
                interceptor?.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));

                bool a = handlers != null && interceptor != null;
                foreach (int id in RegisteredIds) a &= methods[id] != null && handlers.Contains(id);
                foreach (int id in ExcludedIds) a &= !handlers.Contains(id);
                a &= handlers.Contains(18700) && OnlySafePublicRequests();

                bool oldRed = iconManager.GetIconRedDot(SeaHegemonyModel.RED_ICON_TYPE);
                byte rewardStatus = oldRed ? (byte)0 : (byte)1;
                frames.Clear();
                bool b = Invoke(methods[18600], controller, new CliVerify.Pkt()
                        .I(uint.MaxValue).I(4000000000L).H(ushort.MaxValue)
                        .L(unchecked((long)ulong.MaxValue)).S("公会甲").S("海王甲")
                        .L(unchecked((long)0xFEDCBA9876543210UL)).L(unchecked((long)ulong.MaxValue))
                        .H(1).C(rewardStatus).Bytes())
                    && model.HasInfo && model.Info.Camp == uint.MaxValue
                    && model.Info.ServerId == 4000000000U && model.Info.ServerNumber == ushort.MaxValue
                    && model.Info.GuildId == ulong.MaxValue && model.Info.GuildName == "公会甲"
                    && model.Info.KingName == "海王甲" && model.Info.Fight == 0xFEDCBA9876543210UL
                    && model.Info.Count == ulong.MaxValue && model.Info.SelfLevel == 1
                    && model.Info.RewardStatus == rewardStatus
                    && FramesAre(frames, EmptyFrame(18625), EmptyFrame(18604), EmptyFrame(18656));

                b &= Invoke(methods[18601], controller, GuardPacket())
                    && model.HasGuard && model.Guard.LimitNumber == ushort.MaxValue
                    && model.Guard.Number == 2 && model.Guard.HasJoin == byte.MaxValue
                    && model.Guard.Members.Count == 2
                    && model.Guard.Members[0].RoleId == ulong.MaxValue
                    && model.Guard.Members[0].Power == 0xFEDCBA9876543210UL
                    && model.Guard.Members[1].RoleId == ulong.MaxValue;
                b &= Invoke(methods[18604], controller, ApplicationsPacket())
                    && model.HasApplications && model.Applications.Applications.Count == 2
                    && model.Applications.Applications[0].Picture == "头像甲"
                    && model.Applications.Applications[0].RoleId == ulong.MaxValue
                    && model.Applications.Applications[1].RoleId == ulong.MaxValue;
                b &= Invoke(methods[18607], controller,
                        new CliVerify.Pkt().C(byte.MaxValue).C(1).I(uint.MaxValue).I(4000000000L).C(1).Bytes())
                    && model.HasActivity && model.Activity.ActivityType == byte.MaxValue
                    && model.Activity.HasFight == 1 && model.Activity.StartTime == uint.MaxValue
                    && model.Activity.EndTime == 4000000000U && model.Activity.CanEnter == 1;
                b &= Invoke(methods[18614], controller, new CliVerify.Pkt().I(1).Bytes())
                    && model.HasExitResult && model.LastExitCode == 1;
                b &= Invoke(methods[18615], controller, KingPacket())
                    && model.HasKing && model.King.Camp == uint.MaxValue
                    && model.King.GuildId == ulong.MaxValue && model.King.GuildName == "霸主会"
                    && model.King.Times == ushort.MaxValue && model.King.RewardStatuses.Count == 2
                    && model.King.RewardStatuses[0].Times == byte.MaxValue;
                b &= Invoke(methods[18616], controller, new CliVerify.Pkt().I(1).Bytes())
                    && model.HasDivideResult && model.LastDivideCode == 1;
                b &= Invoke(methods[18618], controller, CampsPacket())
                    && model.HasCamps && model.Camps.Camps.Count == 2
                    && model.Camps.Camps[0].Camp == 3 && model.Camps.Camps[0].GuildId == ulong.MaxValue
                    && model.Camps.Camps[0].Power == 0xFEDCBA9876543210UL
                    && model.Camps.Camps[1].Camp == 3;
                b &= Invoke(methods[18622], controller,
                        new CliVerify.Pkt().H(ushort.MaxValue).L(unchecked((long)ulong.MaxValue)).C(byte.MaxValue).Bytes())
                    && model.HasApplyLimit && model.ApplyLimit.RoleLevel == ushort.MaxValue
                    && model.ApplyLimit.Power == ulong.MaxValue && model.ApplyLimit.Auto == byte.MaxValue;
                frames.Clear();
                b &= Invoke(methods[18623], controller, new CliVerify.Pkt().I(0).Bytes())
                    && model.HasActivityNotice && model.LastActivityNotice.Code == 0 && frames.Count == 0;
                b &= Invoke(methods[18624], controller, ActivityTimesPacket())
                    && model.HasActivityTimes && model.ActivityTimes.Times.Count == 2
                    && model.ActivityTimes.Times[0].ActivityType == 2
                    && model.ActivityTimes.Times[0].StartTime == uint.MaxValue
                    && model.ActivityTimes.Times[1].ActivityType == 2;
                b &= Invoke(methods[18625], controller, new CliVerify.Pkt().I(0).Bytes())
                    && model.HasSignupEndTime && model.SignupEndTime == 0;
                frames.Clear();
                b &= Invoke(methods[18626], controller, new CliVerify.Pkt().C(0).Bytes())
                    && model.HasJobNotice && model.LastJobNotice.Code == 0 && frames.Count == 0;
                b &= Invoke(methods[18653], controller,
                        new CliVerify.Pkt().H(ushort.MaxValue).I(uint.MaxValue).Bytes())
                    && model.HasMerit && model.Merit.Level == ushort.MaxValue
                    && model.Merit.Exploit == uint.MaxValue;
                b &= Invoke(methods[18656], controller, new CliVerify.Pkt().H(ushort.MaxValue).Bytes())
                    && model.HasOldJob && model.OldJobLevel == ushort.MaxValue;

                SeaHegemonyModel.GuildsSnapshot guilds7 = null;
                bool c = Invoke(methods[18608], controller, GuildsPacket(7, 2))
                    && model.TryGetGuilds(7, out guilds7)
                    && guilds7.Guilds.Count == 2 && guilds7.Guilds[0].Rank == ushort.MaxValue
                    && guilds7.Guilds[0].GuildId == ulong.MaxValue
                    && guilds7.Guilds[0].GuildPower == 0xFEDCBA9876543210UL
                    && guilds7.Guilds[1].GuildId == ulong.MaxValue;
                SeaHegemonyModel.GuildsSnapshot oldGuilds7 = guilds7;
                c &= Invoke(methods[18608], controller, GuildsPacket(8, 0))
                    && model.TryGetGuilds(8, out SeaHegemonyModel.GuildsSnapshot guilds8)
                    && guilds8.Guilds.Count == 0
                    && model.TryGetGuilds(7, out guilds7) && ReferenceEquals(guilds7, oldGuilds7);
                c &= Invoke(methods[18608], controller, GuildsPacket(7, 0))
                    && model.TryGetGuilds(7, out guilds7) && guilds7.Guilds.Count == 0
                    && !ReferenceEquals(guilds7, oldGuilds7);

                SeaHegemonyModel.MemberPageSnapshot page200 = null;
                c &= Invoke(methods[18654], controller, MembersPacket(200, 1, 2))
                    && model.TryGetMemberPage(200, 1, out page200)
                    && page200.PageTotal == 9 && page200.Members.Count == 2
                    && page200.Members[0].RoleId == ulong.MaxValue
                    && page200.Members[0].Fight == 0xFEDCBA9876543210UL
                    && page200.Members[1].RoleId == ulong.MaxValue;
                SeaHegemonyModel.MemberPageSnapshot oldPage200 = page200;
                c &= Invoke(methods[18654], controller, MembersPacket(1, 1, 0))
                    && model.TryGetMemberPage(1, 1, out SeaHegemonyModel.MemberPageSnapshot gatePage)
                    && gatePage.Members.Count == 0
                    && model.TryGetMemberPage(200, 1, out page200) && ReferenceEquals(page200, oldPage200);
                c &= Invoke(methods[18654], controller, MembersPacket(200, 1, 0))
                    && model.TryGetMemberPage(200, 1, out page200) && page200.Members.Count == 0
                    && !ReferenceEquals(page200, oldPage200);

                SeaHegemonyModel.MonsterEntry monster10 = null;
                SeaHegemonyModel.MonsterEntry monster11 = null;
                bool d = Invoke(methods[18609], controller, MonstersPacket(2, false))
                    && model.HasMonsters && model.LastMonsterPacket.Entries.Count == 2
                    && model.TryGetMonster(10, out monster10)
                    && monster10.Hp == ulong.MaxValue && monster10.HpMax == 0xFEDCBA9876543210UL
                    && model.TryGetMonster(11, out monster11)
                    && monster11.Hp == 77;
                SeaHegemonyModel.MonsterEntry oldMonster11 = monster11;
                d &= Invoke(methods[18609], controller, MonstersPacket(1, true))
                    && model.TryGetMonster(10, out monster10) && monster10.Hp == 0
                    && model.TryGetMonster(11, out monster11) && ReferenceEquals(monster11, oldMonster11)
                    && model.Monsters.Count == 2;
                d &= Invoke(methods[18609], controller, new CliVerify.Pkt().H(0).Bytes())
                    && model.HasMonsters && model.LastMonsterPacket.Entries.Count == 0
                    && model.Monsters.Count == 2;

                bool e = Invoke(methods[18611], controller, ScorePacket())
                    && model.HasScore && model.Score.Groups.Count == 2
                    && model.Score.Groups[0].GuildId == ulong.MaxValue
                    && model.Score.Groups[0].Members.Count == 2
                    && model.Score.Groups[0].Members[0].RoleId == ulong.MaxValue
                    && model.Score.Groups[1].GuildId == ulong.MaxValue;
                e &= Invoke(methods[18612], controller, ResultPacket())
                    && model.HasResult && model.LastResult.Status == byte.MaxValue
                    && model.LastResult.GuildRank == ushort.MaxValue
                    && model.LastResult.RankReward.Count == 2
                    && model.LastResult.RankReward[0].TypeId == uint.MaxValue
                    && model.LastResult.Reward.Count == 1;
                e &= Invoke(methods[18617], controller, SidesPacket())
                    && model.HasSides && model.Sides.Attackers.Count == 2
                    && model.Sides.Defenders.Count == 1
                    && model.Sides.Attackers[0].GuildId == ulong.MaxValue
                    && model.Sides.Attackers[1].GuildId == ulong.MaxValue;
                e &= Invoke(methods[18651], controller, PrivilegesPacket())
                    && model.HasPrivileges && model.Privileges.Privileges.Count == 2
                    && model.Privileges.Privileges[0].EndTime == ulong.MaxValue
                    && model.Privileges.Privileges[0].NeedJobs.Count == 2
                    && model.Privileges.Privileges[0].NeedJobs[0] == ushort.MaxValue
                    && model.Privileges.Privileges[0].NeedJobs[1] == ushort.MaxValue;
                e &= Invoke(methods[18655], controller, DistributionPacket())
                    && model.HasDistribution && model.Distribution.Guilds.Count == 2
                    && model.Distribution.Guilds[0].Fight == 1
                    && model.Distribution.Guilds[1].Fight == ulong.MaxValue;

                frames.Clear();
                controller.RequestStartup();
                bool f = FramesAre(frames, EmptyFrame(18600), EmptyFrame(18607), EmptyFrame(18615),
                    EmptyFrame(18617), EmptyFrame(18624), U16U16Frame(18654, 1, 1));

                frames.Clear();
                controller.RequestInfo();
                controller.RequestGuard();
                controller.RequestApplications();
                controller.RequestActivity();
                controller.RequestGuilds(uint.MaxValue);
                controller.RequestMonsters();
                controller.RequestScore();
                controller.RequestKing();
                controller.RequestSides();
                controller.RequestCamps();
                controller.RequestApplyLimit();
                controller.RequestNextTimes();
                controller.RequestSignup();
                controller.RequestPrivileges();
                controller.RequestMerit();
                controller.RequestMembers(ushort.MaxValue, 40000);
                controller.RequestDistribution();
                controller.RequestOldJob();
                f &= FramesAre(frames,
                    EmptyFrame(18600), EmptyFrame(18601), EmptyFrame(18604), EmptyFrame(18607),
                    U32Frame(18608, uint.MaxValue), EmptyFrame(18609), EmptyFrame(18611),
                    EmptyFrame(18615), EmptyFrame(18617), EmptyFrame(18618), EmptyFrame(18622),
                    EmptyFrame(18624), EmptyFrame(18625), EmptyFrame(18651), EmptyFrame(18653),
                    U16U16Frame(18654, ushort.MaxValue, 40000), EmptyFrame(18655), EmptyFrame(18656));

                frames.Clear();
                f &= Invoke(methods[18623], controller, new CliVerify.Pkt().I(1).Bytes())
                    && FramesAre(frames, EmptyFrame(18607), EmptyFrame(18624), EmptyFrame(18625));
                frames.Clear();
                f &= Invoke(methods[18626], controller, new CliVerify.Pkt().C(1).Bytes())
                    && FramesAre(frames, EmptyFrame(18600));

                var mutable = new List<SeaHegemonyModel.CampEntry>
                {
                    new SeaHegemonyModel.CampEntry(1, 2, 3, 4, "x", 5, 6, "y")
                };
                var immutable = new SeaHegemonyModel.CampsSnapshot(mutable);
                mutable.Clear();
                ModelState beforeRequests = CaptureModel(model);
                frames.Clear();
                controller.RequestGuard();
                controller.RequestScore();
                controller.RequestPrivileges();
                bool g = immutable.Camps.Count == 1 && ModelMatches(model, beforeRequests)
                    && FramesAre(frames, EmptyFrame(18601), EmptyFrame(18611), EmptyFrame(18651));

                controller.Dispose();
                bool h = !controller.IsInitialized && IsModelEmpty(model);
                foreach (int id in RegisteredIds) h &= !handlers.Contains(id);
                foreach (int id in ExcludedIds) h &= !handlers.Contains(id);
                h &= !handlers.Contains(18700);

                pass = a && b && c && d && e && f && g && h;
                Debug.Log("CLIVERIFY seahegemony A=" + a + " B=" + b + " C=" + c
                    + " D=" + d + " E=" + e + " F=" + f + " G=" + g + " H=" + h
                    + " pass=" + pass);
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                RestoreModel(model, oldModel);
                if (wasInitialized) controller.Init();
                for (int id = 18600; id <= 18656; id++)
                    RestoreEntry(handlers, id, savedHandlers[id]);
                RestoreEntry(handlers, 18700, savedHandlers[18700]);
                interceptor?.SetValue(null, oldInterceptor);
                lastLevel?.SetValue(controller, oldLastLevel);
                RestoreEvent(events, hadRoleEvent, oldRoleSubscribers);
                RestoreEntry(icons, SeaHegemonyModel.ICON_TYPE, oldIcon);
                RestoreEntry(boxIcons, SeaHegemonyModel.ICON_TYPE, oldBoxIcon);
                RestoreEntry(icons, SeaHegemonyModel.RED_ICON_TYPE, oldRedIcon);
                RestoreEntry(boxIcons, SeaHegemonyModel.RED_ICON_TYPE, oldRedBoxIcon);
                RestoreEntry(redDots, SeaHegemonyModel.RED_ICON_TYPE, oldRedDot);

                restored = controller.IsInitialized == wasInitialized
                    && ModelMatches(model, oldModel)
                    && (interceptor == null || ReferenceEquals(interceptor.GetValue(null), oldInterceptor))
                    && (lastLevel == null || Equals(lastLevel.GetValue(controller), oldLastLevel))
                    && EventMatches(events, hadRoleEvent, oldRoleSubscribers)
                    && EntryMatches(icons, SeaHegemonyModel.ICON_TYPE, oldIcon)
                    && EntryMatches(boxIcons, SeaHegemonyModel.ICON_TYPE, oldBoxIcon)
                    && EntryMatches(icons, SeaHegemonyModel.RED_ICON_TYPE, oldRedIcon)
                    && EntryMatches(boxIcons, SeaHegemonyModel.RED_ICON_TYPE, oldRedBoxIcon)
                    && EntryMatches(redDots, SeaHegemonyModel.RED_ICON_TYPE, oldRedDot);
                for (int id = 18600; id <= 18656; id++)
                    restored &= EntryMatches(handlers, id, savedHandlers[id]);
                restored &= EntryMatches(handlers, 18700, savedHandlers[18700]);
                Debug.Log("CLIVERIFY seahegemony restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static bool OnlySafePublicRequests()
        {
            var allowed = new HashSet<string>
            {
                "RequestStartup", "RequestInfo", "RequestGuard", "RequestApplications",
                "RequestActivity", "RequestGuilds", "RequestMonsters", "RequestScore",
                "RequestKing", "RequestSides", "RequestCamps", "RequestApplyLimit",
                "RequestNextTimes", "RequestSignup", "RequestPrivileges", "RequestMerit",
                "RequestMembers", "RequestDistribution", "RequestOldJob", "Dispose"
            };
            foreach (MethodInfo method in typeof(SeaHegemonyController).GetMethods(
                         BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                if (!allowed.Contains(method.Name)) return false;
            return true;
        }

        private static byte[] GuardPacket()
        {
            var p = new CliVerify.Pkt().H(ushort.MaxValue).H(2).C(byte.MaxValue).H(2);
            for (int i = 0; i < 2; i++)
                p.H(ushort.MaxValue).I(uint.MaxValue).H(ushort.MaxValue)
                    .L(unchecked((long)ulong.MaxValue)).S("禁卫甲").H(ushort.MaxValue).S("头像甲")
                    .H(ushort.MaxValue).L(unchecked((long)0xFEDCBA9876543210UL));
            return p.Bytes();
        }

        private static byte[] ApplicationsPacket()
        {
            var p = new CliVerify.Pkt().H(2);
            for (int i = 0; i < 2; i++)
                p.S("头像甲").H(ushort.MaxValue).H(ushort.MaxValue)
                    .L(unchecked((long)ulong.MaxValue)).S("申请甲")
                    .L(unchecked((long)0xFEDCBA9876543210UL));
            return p.Bytes();
        }

        private static byte[] KingPacket() => new CliVerify.Pkt()
            .I(uint.MaxValue).I(4000000000L).H(ushort.MaxValue)
            .L(unchecked((long)ulong.MaxValue)).S("霸主会").H(ushort.MaxValue)
            .I(uint.MaxValue).I(4000000000L).H(2)
            .C(byte.MaxValue).C(1).C(byte.MaxValue).C(2).Bytes();

        private static byte[] CampsPacket()
        {
            var p = new CliVerify.Pkt().H(2);
            for (int i = 0; i < 2; i++)
                p.I(3).I(uint.MaxValue).H(ushort.MaxValue).L(unchecked((long)ulong.MaxValue))
                    .S("势力会").L(unchecked((long)0xFEDCBA9876543210UL))
                    .L(unchecked((long)ulong.MaxValue)).S("会长甲");
            return p.Bytes();
        }

        private static byte[] ActivityTimesPacket() => new CliVerify.Pkt().H(2)
            .C(2).I(uint.MaxValue).I(4000000000L)
            .C(2).I(1).I(2).Bytes();

        private static byte[] GuildsPacket(uint camp, int count)
        {
            var p = new CliVerify.Pkt().I(camp).H(count);
            for (int i = 0; i < count; i++)
                p.H(ushort.MaxValue).I(uint.MaxValue).H(ushort.MaxValue)
                    .L(unchecked((long)ulong.MaxValue)).S("公会甲")
                    .L(unchecked((long)0xFEDCBA9876543210UL)).S("会长甲")
                    .L(unchecked((long)ulong.MaxValue));
            return p.Bytes();
        }

        private static byte[] MembersPacket(ushort pageSize, ushort pageNumber, int count)
        {
            var p = new CliVerify.Pkt().H(9).H(pageSize).H(pageNumber).H(count);
            for (int i = 0; i < count; i++)
                p.H(ushort.MaxValue).L(unchecked((long)ulong.MaxValue)).S("成员甲")
                    .I(uint.MaxValue).H(ushort.MaxValue).I(uint.MaxValue)
                    .L(unchecked((long)0xFEDCBA9876543210UL)).I(uint.MaxValue).S("公会甲");
            return p.Bytes();
        }

        private static byte[] MonstersPacket(int count, bool zeroHp)
        {
            var p = new CliVerify.Pkt().H(count);
            if (count >= 1)
                p.I(10).L(zeroHp ? 0 : unchecked((long)ulong.MaxValue))
                    .L(unchecked((long)0xFEDCBA9876543210UL)).C(byte.MaxValue).I(uint.MaxValue);
            if (count >= 2)
                p.I(11).L(77).L(88).C(1).I(12);
            return p.Bytes();
        }

        private static byte[] ScorePacket()
        {
            var p = new CliVerify.Pkt().H(2);
            for (int g = 0; g < 2; g++)
            {
                p.L(unchecked((long)ulong.MaxValue)).S("统计会").C(byte.MaxValue)
                    .C(byte.MaxValue).H(ushort.MaxValue).H(2);
                for (int m = 0; m < 2; m++)
                    p.H(ushort.MaxValue).L(unchecked((long)ulong.MaxValue)).S("成员甲")
                        .H(ushort.MaxValue).H(ushort.MaxValue);
            }
            return p.Bytes();
        }

        private static byte[] ResultPacket() => new CliVerify.Pkt()
            .C(byte.MaxValue).H(ushort.MaxValue).H(40000)
            .H(2).C(byte.MaxValue).I(uint.MaxValue).I(uint.MaxValue)
            .C(1).I(2).I(3)
            .H(1).C(4).I(5).I(6).Bytes();

        private static byte[] SidesPacket() => new CliVerify.Pkt()
            .H(2)
            .I(uint.MaxValue).H(ushort.MaxValue).L(unchecked((long)ulong.MaxValue)).S("攻甲")
            .I(uint.MaxValue).H(ushort.MaxValue).L(unchecked((long)ulong.MaxValue)).S("攻乙")
            .H(1)
            .I(4000000000L).H(40000).L(unchecked((long)0xFEDCBA9876543210UL)).S("守甲")
            .Bytes();

        private static byte[] PrivilegesPacket() => new CliVerify.Pkt()
            .H(2)
            .H(ushort.MaxValue).H(ushort.MaxValue).C(byte.MaxValue)
            .L(unchecked((long)ulong.MaxValue)).H(2).H(ushort.MaxValue).H(ushort.MaxValue)
            .H(ushort.MaxValue).H(0).C(0).L(0).H(0)
            .Bytes();

        private static byte[] DistributionPacket() => new CliVerify.Pkt()
            .H(2)
            .I(2).I(3).S("弱会").L(4).S("弱会长").L(1).I(5)
            .I(uint.MaxValue).I(uint.MaxValue).S("强会").L(unchecked((long)ulong.MaxValue))
            .S("强会长").L(unchecked((long)ulong.MaxValue)).I(uint.MaxValue)
            .Bytes();

        private static byte[] EmptyFrame(int id) => new CliVerify.Pkt().H(6).H(1000).H(id).Bytes();
        private static byte[] U32Frame(int id, uint value) =>
            new CliVerify.Pkt().H(10).H(1000).H(id).I(value).Bytes();
        private static byte[] U16U16Frame(int id, ushort first, ushort second) =>
            new CliVerify.Pkt().H(10).H(1000).H(id).H(first).H(second).Bytes();

        private static bool FramesAre(IReadOnlyList<byte[]> actual, params byte[][] expected)
        {
            if (actual == null || expected == null || actual.Count != expected.Length) return false;
            for (int i = 0; i < expected.Length; i++)
                if (!BytesEqual(actual[i], expected[i])) return false;
            return true;
        }

        private static bool BytesEqual(IReadOnlyList<byte> actual, IReadOnlyList<byte> expected)
        {
            if (actual == null || expected == null || actual.Count != expected.Count) return false;
            for (int i = 0; i < expected.Count; i++) if (actual[i] != expected[i]) return false;
            return true;
        }

        private static bool Invoke(MethodInfo handler, SeaHegemonyController controller, byte[] bytes)
        {
            if (handler == null) return false;
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool IsModelEmpty(SeaHegemonyModel model)
        {
            IDictionary guilds = typeof(SeaHegemonyModel).GetField("_guildsByCamp", F)?.GetValue(model)
                as IDictionary;
            IDictionary members = typeof(SeaHegemonyModel).GetField("_memberPages", F)?.GetValue(model)
                as IDictionary;
            return !model.HasInfo && !model.HasGuard && !model.HasApplications && !model.HasActivity
                && !model.HasMonsters && !model.HasScore && !model.HasResult && !model.HasKing
                && !model.HasSides && !model.HasCamps && !model.HasApplyLimit && !model.HasActivityNotice
                && !model.HasActivityTimes && !model.HasJobNotice && !model.HasPrivileges
                && !model.HasMerit && !model.HasDistribution && !model.HasSignupEndTime
                && !model.HasOldJob && !model.HasExitResult && !model.HasDivideResult
                && !model.HasDailyError && model.Monsters.Count == 0
                && guilds != null && guilds.Count == 0 && members != null && members.Count == 0;
        }

        private static ModelState CaptureModel(SeaHegemonyModel model)
        {
            var state = new ModelState();
            foreach (FieldInfo field in typeof(SeaHegemonyModel).GetFields(F | BindingFlags.Public))
            {
                object value = field.GetValue(model);
                var item = new FieldState { Field = field, Value = value };
                if (value is IDictionary dictionary)
                {
                    item.IsDictionary = true;
                    foreach (DictionaryEntry pair in dictionary)
                        item.Dictionary[pair.Key] = pair.Value;
                }
                state.Fields.Add(item);
            }
            return state;
        }

        private static void RestoreModel(SeaHegemonyModel model, ModelState state)
        {
            foreach (FieldState item in state.Fields)
            {
                if (item.IsDictionary && item.Field.GetValue(model) is IDictionary dictionary)
                {
                    dictionary.Clear();
                    foreach (KeyValuePair<object, object> pair in item.Dictionary)
                        dictionary[pair.Key] = pair.Value;
                }
                else item.Field.SetValue(model, item.Value);
            }
        }

        private static bool ModelMatches(SeaHegemonyModel model, ModelState state)
        {
            foreach (FieldState item in state.Fields)
            {
                object current = item.Field.GetValue(model);
                if (item.IsDictionary)
                {
                    if (!(current is IDictionary dictionary) || dictionary.Count != item.Dictionary.Count)
                        return false;
                    foreach (KeyValuePair<object, object> pair in item.Dictionary)
                        if (!dictionary.Contains(pair.Key) || !ReferenceEquals(dictionary[pair.Key], pair.Value))
                            return false;
                }
                else if (current is ValueType || current is string)
                {
                    if (!Equals(current, item.Value)) return false;
                }
                else if (!ReferenceEquals(current, item.Value)) return false;
            }
            return true;
        }

        private static void SaveEntry(IDictionary dictionary, IDictionary<int, EntryState> saved, int id)
        {
            bool exists = dictionary != null && dictionary.Contains(id);
            saved[id] = new EntryState
            {
                Exists = exists,
                Value = exists ? dictionary[id] : null
            };
        }

        private static EntryState CaptureEntry(IDictionary dictionary, object key)
        {
            bool exists = dictionary != null && dictionary.Contains(key);
            object value = exists ? dictionary[key] : null;
            var state = new EntryState { Exists = exists, Value = value };
            if (value != null)
                foreach (FieldInfo field in value.GetType().GetFields(
                             BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    state.ObjectFields[field] = field.GetValue(value);
            return state;
        }

        private static void RestoreEntry(IDictionary dictionary, object key, EntryState state)
        {
            if (dictionary == null) return;
            if (state.Exists)
            {
                dictionary[key] = state.Value;
                if (state.Value != null)
                    foreach (KeyValuePair<FieldInfo, object> field in state.ObjectFields)
                        field.Key.SetValue(state.Value, field.Value);
            }
            else dictionary.Remove(key);
        }

        private static bool EntryMatches(IDictionary dictionary, object key, EntryState state)
        {
            if (dictionary == null || dictionary.Contains(key) != state.Exists) return false;
            if (!state.Exists) return true;
            if (!ReferenceEquals(dictionary[key], state.Value)) return false;
            foreach (KeyValuePair<FieldInfo, object> field in state.ObjectFields)
                if (!Equals(field.Key.GetValue(state.Value), field.Value)) return false;
            return true;
        }

        private static void RestoreEvent(Dictionary<string, List<Delegate>> events, bool hadEvent,
            IReadOnlyList<Delegate> oldSubscribers)
        {
            if (events == null) return;
            events.Remove(GlobalEvent.EVT_ROLE_INFO_UPDATE);
            if (hadEvent) events[GlobalEvent.EVT_ROLE_INFO_UPDATE] = new List<Delegate>(oldSubscribers);
        }

        private static bool EventMatches(Dictionary<string, List<Delegate>> events, bool hadEvent,
            IReadOnlyList<Delegate> oldSubscribers)
        {
            if (events == null || events.ContainsKey(GlobalEvent.EVT_ROLE_INFO_UPDATE) != hadEvent)
                return false;
            if (!hadEvent) return true;
            List<Delegate> current = events[GlobalEvent.EVT_ROLE_INFO_UPDATE];
            if (current.Count != oldSubscribers.Count) return false;
            for (int i = 0; i < current.Count; i++)
                if (!ReferenceEquals(current[i], oldSubscribers[i])) return false;
            return true;
        }
    }
}
