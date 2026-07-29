using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Kf1vn;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>621族安全读侧：精确注册、请求线序、全字段解析、增量、清空与ambient恢复。</summary>
    public static class Kf1vnActivityInfoCase
    {
        private const BindingFlags F = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags SF = BindingFlags.Static | BindingFlags.NonPublic;

        private static readonly int[] RegisteredIds =
        {
            62100, 62101, 62103, 62104, 62105, 62108, 62109, 62110, 62112,
            62113, 62116, 62117, 62119, 62120, 62123, 62132, 62133, 62135
        };

        private static readonly int[] ExcludedIds = { 62102, 62107, 62111, 62118, 62121, 62134 };

        private sealed class EntryState
        {
            public bool Exists;
            public object Value;
        }

        private sealed class FieldState
        {
            public FieldInfo Field;
            public object Value;
            public bool IsDictionary;
            public readonly Dictionary<object, object> Entries = new Dictionary<object, object>();
        }

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY kf1vn-family EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            Kf1vnController controller = Kf1vnController.Instance;
            Kf1vnModel model = Kf1vnModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            List<FieldState> oldModel = CaptureModel(model);
            FieldInfo readInterceptor = typeof(Kf1vnController).GetField("s_readOutboundIntercept", SF);
            FieldInfo activityInterceptor = typeof(Kf1vnController).GetField("s_activityInfoOutboundIntercept", SF);
            FieldInfo exitInterceptor = typeof(Kf1vnController).GetField("s_exitOutboundIntercept", SF);
            FieldInfo lastLevel = typeof(Kf1vnController).GetField("_lastLevel", F);
            object oldReadInterceptor = readInterceptor?.GetValue(null);
            object oldActivityInterceptor = activityInterceptor?.GetValue(null);
            object oldExitInterceptor = exitInterceptor?.GetValue(null);
            object oldLastLevel = lastLevel?.GetValue(controller);

            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var oldHandlers = new Dictionary<int, EntryState>();
            for (int id = 62100; id <= 62136; id++) SaveEntry(handlers, oldHandlers, id);

            ActivityIconManager iconManager = ActivityIconManager.Instance;
            var mainIcons = typeof(ActivityIconManager).GetField("_iconInfoByType", F)?.GetValue(iconManager) as IDictionary;
            var boxIcons = typeof(ActivityIconManager).GetField("_iconBoxInfoByType", F)?.GetValue(iconManager) as IDictionary;
            EntryState oldMainIcon = SaveEntry(mainIcons, Kf1vnController.ICON_TYPE);
            EntryState oldBoxIcon = SaveEntry(boxIcons, Kf1vnController.ICON_TYPE);

            bool pass = false;
            bool restored = false;
            try
            {
                if (controller.IsInitialized) controller.Dispose();
                if (handlers != null)
                    for (int id = 62100; id <= 62136; id++) handlers.Remove(id);

                controller.Init();
                model.Reset();
                var frames = new List<byte[]>();
                readInterceptor?.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                activityInterceptor?.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));

                bool a = handlers != null && readInterceptor != null && activityInterceptor != null
                    && exitInterceptor != null && lastLevel != null && mainIcons != null && boxIcons != null
                    && ExactHandlers(handlers) && OnlySafePublicMethods();
                object firstHandler = handlers != null && handlers.Contains(62100) ? handlers[62100] : null;
                controller.Init();
                a &= firstHandler != null && ReferenceEquals(firstHandler, handlers[62100]);

                SeedForReset(controller);
                frames.Clear();
                controller.RequestStartup();
                bool b = ModelIsEmpty(model) && FramesAre(frames, EmptyFrame(62101), EmptyFrame(62133));
                frames.Clear();
                controller.RequestActivityInfo();
                controller.RequestWaitInfo();
                controller.RequestQualificationBattle();
                controller.RequestQualificationRank(byte.MaxValue);
                controller.RequestLeaderBattle();
                controller.RequestLeaderRank(254);
                controller.RequestQuizInfo();
                controller.RequestWaitingRank();
                controller.RequestQuizHistory();
                b &= FramesAre(frames, EmptyFrame(62100), EmptyFrame(62104), EmptyFrame(62105),
                    U8Frame(62110, byte.MaxValue), EmptyFrame(62112), U8Frame(62116, 254),
                    EmptyFrame(62117), EmptyFrame(62119), EmptyFrame(62133));

                frames.Clear();
                model.Reset();
                bool c = Invoke(controller, 62101, new CliVerify.Pkt().C(1).H(2).I(3).C(4).I(5).Bytes())
                    && model.HasStageInfo && model.Stage == 1 && model.Turn == 2 && model.Edtime == 3
                    && model.SubStage == 4 && model.SubEdtime == 5
                    && FramesAre(frames, EmptyFrame(62100));
                frames.Clear();
                c &= Invoke(controller, 62101, new CliVerify.Pkt().C(1).H(6).I(7).C(8).I(9).Bytes())
                    && frames.Count == 0;
                c &= Invoke(controller, 62101, new CliVerify.Pkt().C(2).H(10).I(11).C(12).I(13).Bytes())
                    && FramesAre(frames, EmptyFrame(62100), EmptyFrame(62104));
                frames.Clear();
                c &= Invoke(controller, 62100, new CliVerify.Pkt().C(byte.MaxValue).I(uint.MaxValue)
                        .H(ushort.MaxValue).C(254).Bytes())
                    && model.HasActivityInfo && model.IsSign == byte.MaxValue
                    && model.SignNum == uint.MaxValue && model.DefNum == ushort.MaxValue && model.Zone == 254;
                c &= Invoke(controller, 62104, WaitPacket()) && model.WaitInfo != null
                    && model.WaitInfo.LeftTimes == byte.MaxValue && model.WaitInfo.Score == uint.MaxValue
                    && model.WaitInfo.Time == 4000000000U && model.WaitInfo.Win == ushort.MaxValue
                    && model.WaitInfo.Lose == 253 && model.WaitInfo.ExpSum == ulong.MaxValue
                    && model.WaitInfo.DefNum == 50000;
                c &= Invoke(controller, 62119, new CliVerify.Pkt().C(252).S("榜首").Bytes())
                    && model.WaitingRank != null && model.WaitingRank.Rank == 252
                    && model.WaitingRank.TopName == "榜首";

                bool d = Invoke(controller, 62105, QualificationBattlePacket())
                    && model.QualificationBattle != null && model.QualificationBattle.Roles.Count == 2
                    && model.QualificationBattle.Roles[0].PlayerId == ulong.MaxValue
                    && model.QualificationBattle.Roles[0].ServerName == "服甲"
                    && model.QualificationBattle.Roles[0].CombatPower == 0xFEDCBA9876543210UL
                    && model.QualificationBattle.Roles[0].PictureVer == uint.MaxValue
                    && model.QualificationBattle.Roles[1].PlayerId == ulong.MaxValue
                    && model.QualificationBattle.Roles[1].Name == "乙"
                    && model.QualificationBattle.LoadingTime == uint.MaxValue
                    && model.QualificationBattle.BattleTime == 4000000000U;
                d &= Invoke(controller, 62108, QualificationResultPacket())
                    && model.LastQualificationResult != null
                    && model.LastQualificationResult.Result == byte.MaxValue
                    && model.LastQualificationResult.OldScore == uint.MaxValue
                    && model.LastQualificationResult.AddScore == ushort.MaxValue
                    && model.LastQualificationResult.Roles.Count == 1
                    && model.LastQualificationResult.Roles[0].Hp == ulong.MaxValue
                    && model.LastQualificationResult.Roles[0].HpLimit == 0xFEDCBA9876543210UL;
                d &= Invoke(controller, 62109, QualificationSettlementPacket())
                    && model.LastQualificationSettlement != null
                    && model.LastQualificationSettlement.IsDef == 251
                    && model.LastQualificationSettlement.Rank == ushort.MaxValue
                    && model.LastQualificationSettlement.Score == uint.MaxValue
                    && model.LastQualificationSettlement.Award.Count == 2
                    && model.LastQualificationSettlement.Award[0].TypeId == uint.MaxValue
                    && model.LastQualificationSettlement.Award[1].TypeId == uint.MaxValue;
                d &= Invoke(controller, 62110, QualificationRankPacket(7, false))
                    && model.TryGetQualificationRank(7, out Kf1vnModel.QualificationRankSnapshot qRank)
                    && qRank.Entries.Count == 2 && qRank.Entries[0].PlayerId == ulong.MaxValue
                    && qRank.Entries[0].GuildName == "会甲" && qRank.Entries[0].CombatPower == ulong.MaxValue
                    && qRank.Entries[1].PlayerId == ulong.MaxValue && qRank.Entries[1].Name == "榜乙";
                d &= Invoke(controller, 62110, QualificationRankPacket(8, true))
                    && model.TryGetQualificationRank(8, out Kf1vnModel.QualificationRankSnapshot qEmpty)
                    && qEmpty.Entries.Count == 0 && model.QualificationRanks.Count == 2;

                bool e = Invoke(controller, 62112, LeaderBattlePacket()) && model.LeaderBattle != null
                    && model.LeaderBattle.PlayerId == ulong.MaxValue
                    && model.LeaderBattle.CombatPower == 0xFEDCBA9876543210UL
                    && model.LeaderBattle.Challengers.Count == 2
                    && model.LeaderBattle.Challengers[0].PlayerId == ulong.MaxValue
                    && model.LeaderBattle.Challengers[1].PlayerId == ulong.MaxValue
                    && model.LeaderBattle.LoadingTime == uint.MaxValue
                    && model.LeaderBattle.BattleTime == 4000000000U;
                e &= Invoke(controller, 62113, LeaderResultPacket()) && model.LastLeaderResult != null
                    && model.LastLeaderResult.RoleId == ulong.MaxValue
                    && model.LastLeaderResult.Hp == ulong.MaxValue
                    && model.LastLeaderResult.HpLimit == 0xFEDCBA9876543210UL
                    && model.LastLeaderResult.Award.Count == 2;
                e &= Invoke(controller, 62116, LeaderRankPacket(9, false))
                    && model.TryGetLeaderRank(9, out Kf1vnModel.LeaderRankSnapshot lRank)
                    && lRank.Entries.Count == 2 && lRank.Entries[0].ServerId == ushort.MaxValue
                    && lRank.Entries[0].PlayerId == ulong.MaxValue
                    && lRank.Entries[0].Hp == ulong.MaxValue
                    && lRank.Entries[0].HpLimit == 0xFEDCBA9876543210UL
                    && lRank.DailyAward.Count == 2;
                e &= Invoke(controller, 62116, LeaderRankPacket(10, true))
                    && model.TryGetLeaderRank(10, out Kf1vnModel.LeaderRankSnapshot lEmpty)
                    && lEmpty.Entries.Count == 0 && lEmpty.DailyAward.Count == 0
                    && model.LeaderRanks.Count == 2;
                e &= Invoke(controller, 62120, LeaderSettlementPacket())
                    && model.LastLeaderSettlement != null && model.LastLeaderSettlement.Rank == 250
                    && model.LastLeaderSettlement.Score == ushort.MaxValue
                    && model.LastLeaderSettlement.Award.Count == 2
                    && model.LastLeaderSettlement.Turn == 249;

                bool f = Invoke(controller, 62117, QuizPacket()) && model.Quiz != null
                    && model.Quiz.Battles.Count == 2 && model.Quiz.DefNum == ushort.MaxValue
                    && model.Quiz.BetNum == 248 && model.Quiz.Battles[0].BattleId == 77
                    && model.Quiz.Battles[1].BattleId == 77
                    && model.Quiz.Battles[0].PlayerId == ulong.MaxValue
                    && model.Quiz.Battles[0].Challengers.Count == 2
                    && model.Quiz.Battles[0].Challengers[0].CombatPower == ulong.MaxValue;
                Kf1vnModel.QuizBattleEntry secondBefore = model.Quiz.Battles[1];
                f &= Invoke(controller, 62123, new CliVerify.Pkt().H(77).C(6).C(7).Bytes())
                    && model.LastQuizResult != null && model.LastQuizResult.BattleId == 77
                    && model.Quiz.Battles[0].Status == 2 && model.Quiz.Battles[0].BattleResult == 6
                    && model.Quiz.Battles[0].IsBet == 1 && model.Quiz.Battles[0].BetResult == 7
                    && ReferenceEquals(model.Quiz.Battles[1], secondBefore);
                f &= Invoke(controller, 62123, new CliVerify.Pkt().H(77).C(9).C(0).Bytes())
                    && model.LastQuizResult.BetResult == 0 && model.Quiz.Battles[0].BattleResult == 6;
                f &= Invoke(controller, 62135, new CliVerify.Pkt().H(77).C(0).Bytes())
                    && model.LastBattleResult != null && model.LastBattleResult.BattleResult == 0
                    && model.Quiz.Battles[0].Status == 2 && model.Quiz.Battles[0].BattleResult == 0;
                f &= Invoke(controller, 62135, new CliVerify.Pkt().H(77).C(8).Bytes())
                    && model.Quiz.Battles[0].Status == 2 && model.Quiz.Battles[0].BattleResult == 8
                    && ReferenceEquals(model.Quiz.Battles[1], secondBefore);
                f &= Invoke(controller, 62133, QuizHistoryPacket(false)) && model.QuizHistory != null
                    && model.QuizHistory.Entries.Count == 2
                    && model.QuizHistory.Entries[0].Key == ulong.MaxValue
                    && model.QuizHistory.Entries[1].Key == ulong.MaxValue
                    && model.QuizHistory.Entries[0].Name == "历史甲";
                f &= Invoke(controller, 62133, QuizHistoryPacket(true))
                    && model.QuizHistory != null && model.QuizHistory.Entries.Count == 0;
                f &= Invoke(controller, 62117, new CliVerify.Pkt().H(0).H(0).C(0).Bytes())
                    && model.Quiz != null && model.Quiz.Battles.Count == 0
                    && model.LastQuizResult != null && model.LastBattleResult != null;

                controller.Dispose();
                bool g = !controller.IsInitialized && ModelIsEmpty(model) && NoFamilyHandlers(handlers)
                    && frames.Count == 0;

                pass = a && b && c && d && e && f && g;
                Debug.Log("CLIVERIFY kf1vn-family A=" + a + " B=" + b + " C=" + c
                    + " D=" + d + " E=" + e + " F=" + f + " G=" + g + " pass=" + pass);
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                RestoreModel(model, oldModel);
                if (wasInitialized) controller.Init();
                for (int id = 62100; id <= 62136; id++) RestoreEntry(handlers, id, oldHandlers[id]);
                if (readInterceptor != null) readInterceptor.SetValue(null, oldReadInterceptor);
                if (activityInterceptor != null) activityInterceptor.SetValue(null, oldActivityInterceptor);
                if (exitInterceptor != null) exitInterceptor.SetValue(null, oldExitInterceptor);
                if (lastLevel != null) lastLevel.SetValue(controller, oldLastLevel);
                RestoreEntry(mainIcons, Kf1vnController.ICON_TYPE, oldMainIcon);
                RestoreEntry(boxIcons, Kf1vnController.ICON_TYPE, oldBoxIcon);

                restored = controller.IsInitialized == wasInitialized && ModelMatches(model, oldModel)
                    && (readInterceptor == null || ReferenceEquals(readInterceptor.GetValue(null), oldReadInterceptor))
                    && (activityInterceptor == null || ReferenceEquals(activityInterceptor.GetValue(null), oldActivityInterceptor))
                    && (exitInterceptor == null || ReferenceEquals(exitInterceptor.GetValue(null), oldExitInterceptor))
                    && (lastLevel == null || Equals(lastLevel.GetValue(controller), oldLastLevel))
                    && EntryMatches(mainIcons, Kf1vnController.ICON_TYPE, oldMainIcon)
                    && EntryMatches(boxIcons, Kf1vnController.ICON_TYPE, oldBoxIcon);
                for (int id = 62100; id <= 62136; id++)
                    restored &= EntryMatches(handlers, id, oldHandlers[id]);
                Debug.Log("CLIVERIFY kf1vn-family restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static bool ExactHandlers(IDictionary handlers)
        {
            var expected = new HashSet<int>(RegisteredIds);
            for (int id = 62100; id <= 62136; id++)
                if (handlers.Contains(id) != expected.Contains(id)) return false;
            return true;
        }

        private static bool NoFamilyHandlers(IDictionary handlers)
        {
            if (handlers == null) return false;
            for (int id = 62100; id <= 62136; id++)
                if (handlers.Contains(id)) return false;
            return true;
        }

        private static bool OnlySafePublicMethods()
        {
            var expected = new HashSet<string>
            {
                "RequestStartup", "RequestStage", "RequestActivityInfo", "RequestWaitInfo",
                "RequestQualificationBattle", "RequestQualificationRank", "RequestLeaderBattle",
                "RequestLeaderRank", "RequestQuizInfo", "RequestWaitingRank", "RequestQuizHistory",
                "RequestExit", "Dispose"
            };
            foreach (MethodInfo method in typeof(Kf1vnController).GetMethods(
                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                if (!expected.Remove(method.Name)) return false;
            return expected.Count == 0;
        }

        private static void SeedForReset(Kf1vnController controller)
        {
            Kf1vnModel model = Kf1vnModel.Instance;
            model.SetStageInfo(5, 6, 7, 8, 9);
            model.SetActivityInfo(1, 2, 3, 4);
            Invoke(controller, 62104, WaitPacket());
            Invoke(controller, 62105, QualificationBattlePacket());
            Invoke(controller, 62108, QualificationResultPacket());
            Invoke(controller, 62109, QualificationSettlementPacket());
            Invoke(controller, 62110, QualificationRankPacket(1, false));
            Invoke(controller, 62112, LeaderBattlePacket());
            Invoke(controller, 62113, LeaderResultPacket());
            Invoke(controller, 62116, LeaderRankPacket(1, false));
            Invoke(controller, 62117, QuizPacket());
            Invoke(controller, 62119, new CliVerify.Pkt().C(1).S("x").Bytes());
            Invoke(controller, 62120, LeaderSettlementPacket());
            Invoke(controller, 62123, new CliVerify.Pkt().H(77).C(1).C(1).Bytes());
            Invoke(controller, 62133, QuizHistoryPacket(false));
            Invoke(controller, 62135, new CliVerify.Pkt().H(77).C(1).Bytes());
        }

        private static bool ModelIsEmpty(Kf1vnModel m)
        {
            return !m.HasStageInfo && !m.HasActivityInfo && m.Stage == 0 && m.Turn == 0
                && m.Edtime == 0 && m.SubStage == 0 && m.SubEdtime == 0
                && m.IsSign == 0 && m.SignNum == 0 && m.DefNum == 0 && m.Zone == 0
                && m.WaitInfo == null && m.QualificationBattle == null
                && m.LastQualificationResult == null && m.LastQualificationSettlement == null
                && m.QualificationRanks.Count == 0 && m.LeaderBattle == null
                && m.LastLeaderResult == null && m.LeaderRanks.Count == 0 && m.Quiz == null
                && m.WaitingRank == null && m.LastLeaderSettlement == null
                && m.LastQuizResult == null && m.QuizHistory == null && m.LastBattleResult == null;
        }

        private static bool Invoke(Kf1vnController controller, int protocolId, byte[] bytes)
        {
            MethodInfo method = typeof(Kf1vnController).GetMethod("On" + protocolId,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null) return false;
            var reader = new NetReader(bytes, 0, bytes.Length);
            method.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static byte[] WaitPacket() => new CliVerify.Pkt().C(byte.MaxValue).I(uint.MaxValue)
            .I(4000000000L).H(ushort.MaxValue).C(253).L(unchecked((long)ulong.MaxValue)).H(50000).Bytes();

        private static byte[] QualificationBattlePacket()
        {
            var p = new CliVerify.Pkt().H(2);
            QualificationRole(p, ulong.MaxValue, "甲", true);
            QualificationRole(p, ulong.MaxValue, "乙", false);
            return p.I(uint.MaxValue).I(4000000000L).Bytes();
        }

        private static void QualificationRole(CliVerify.Pkt p, ulong id, string suffix, bool max)
        {
            p.L(unchecked((long)id)).S("平台" + suffix).H(max ? ushort.MaxValue : 2)
                .S("服" + suffix).S(suffix).C(max ? byte.MaxValue : 3)
                .L(unchecked((long)(max ? 0xFEDCBA9876543210UL : 4UL)))
                .H(max ? ushort.MaxValue : 5).C(max ? 252 : 6).C(max ? 251 : 7)
                .S("图" + suffix).I(max ? uint.MaxValue : 8).H(max ? ushort.MaxValue : 9);
        }

        private static byte[] QualificationResultPacket()
        {
            var p = new CliVerify.Pkt().C(byte.MaxValue).I(uint.MaxValue).H(ushort.MaxValue).C(250).C(249).H(1);
            p.L(unchecked((long)ulong.MaxValue)).S("结果平台").H(ushort.MaxValue).S("结果名")
                .C(248).C(247).S("结果图").I(uint.MaxValue).H(ushort.MaxValue)
                .L(unchecked((long)ulong.MaxValue)).L(unchecked((long)0xFEDCBA9876543210UL));
            return p.Bytes();
        }

        private static byte[] QualificationSettlementPacket()
        {
            var p = new CliVerify.Pkt().C(251).H(ushort.MaxValue).I(uint.MaxValue);
            ObjectList(p, false);
            return p.Bytes();
        }

        private static byte[] QualificationRankPacket(byte area, bool empty)
        {
            var p = new CliVerify.Pkt().C(area).H(empty ? 0 : 2);
            if (empty) return p.Bytes();
            QualificationRankEntry(p, "甲", true);
            QualificationRankEntry(p, "乙", false);
            return p.Bytes();
        }

        private static void QualificationRankEntry(CliVerify.Pkt p, string suffix, bool max)
        {
            p.C(max ? byte.MaxValue : 2).L(unchecked((long)ulong.MaxValue)).S("平台" + suffix)
                .H(max ? ushort.MaxValue : 3).S("服" + suffix).S("榜" + suffix).S("会" + suffix)
                .C(max ? 250 : 4).I(max ? uint.MaxValue : 5).H(max ? ushort.MaxValue : 6)
                .C(max ? 249 : 7).L(unchecked((long)(max ? ulong.MaxValue : 8UL)))
                .C(max ? 248 : 9).H(max ? ushort.MaxValue : 10);
        }

        private static byte[] LeaderBattlePacket()
        {
            var p = new CliVerify.Pkt().L(unchecked((long)ulong.MaxValue)).S("擂平台")
                .H(ushort.MaxValue).S("擂服").S("擂主").C(byte.MaxValue)
                .L(unchecked((long)0xFEDCBA9876543210UL)).H(ushort.MaxValue).C(250).C(249)
                .S("擂图").I(uint.MaxValue).H(ushort.MaxValue).H(2);
            Challenger(p, "甲", true);
            Challenger(p, "乙", false);
            return p.I(uint.MaxValue).I(4000000000L).Bytes();
        }

        private static void Challenger(CliVerify.Pkt p, string suffix, bool max)
        {
            p.L(unchecked((long)ulong.MaxValue)).S("挑平台" + suffix).H(max ? ushort.MaxValue : 2)
                .S("挑服" + suffix).S("挑" + suffix).C(max ? byte.MaxValue : 3)
                .C(max ? 248 : 4).C(max ? 247 : 5).S("挑图" + suffix)
                .I(max ? uint.MaxValue : 6).H(max ? ushort.MaxValue : 7)
                .L(unchecked((long)(max ? ulong.MaxValue : 8UL)));
        }

        private static byte[] LeaderResultPacket()
        {
            var p = new CliVerify.Pkt().C(byte.MaxValue).C(250).C(249)
                .L(unchecked((long)ulong.MaxValue)).S("结平台").H(ushort.MaxValue).S("结名")
                .C(248).C(247).S("结图").I(uint.MaxValue).H(ushort.MaxValue)
                .L(unchecked((long)ulong.MaxValue)).L(unchecked((long)0xFEDCBA9876543210UL));
            ObjectList(p, false);
            return p.Bytes();
        }

        private static byte[] LeaderRankPacket(byte area, bool empty)
        {
            var p = new CliVerify.Pkt().C(area).H(empty ? 0 : 2);
            if (!empty)
            {
                LeaderRankEntry(p, "甲", true);
                LeaderRankEntry(p, "乙", false);
            }
            ObjectList(p, empty);
            return p.Bytes();
        }

        private static void LeaderRankEntry(CliVerify.Pkt p, string suffix, bool max)
        {
            p.C(max ? byte.MaxValue : 2).H(max ? ushort.MaxValue : 3)
                .L(unchecked((long)ulong.MaxValue)).S("擂榜平台" + suffix).H(max ? ushort.MaxValue : 4)
                .S("擂榜服" + suffix).S("擂榜名" + suffix).S("擂榜会" + suffix)
                .C(max ? 250 : 5).I(max ? uint.MaxValue : 6).C(max ? 249 : 7)
                .L(unchecked((long)(max ? ulong.MaxValue : 8UL))).C(max ? 248 : 9)
                .H(max ? ushort.MaxValue : 10).C(max ? 247 : 11).H(max ? ushort.MaxValue : 12)
                .L(unchecked((long)(max ? ulong.MaxValue : 13UL)))
                .L(unchecked((long)(max ? 0xFEDCBA9876543210UL : 14UL)));
        }

        private static byte[] LeaderSettlementPacket()
        {
            var p = new CliVerify.Pkt().C(250).H(ushort.MaxValue);
            ObjectList(p, false);
            return p.C(249).Bytes();
        }

        private static byte[] QuizPacket()
        {
            var p = new CliVerify.Pkt().H(2);
            QuizBattle(p, "甲", true);
            QuizBattle(p, "乙", false);
            return p.H(ushort.MaxValue).C(248).Bytes();
        }

        private static void QuizBattle(CliVerify.Pkt p, string suffix, bool max)
        {
            p.H(77).C(max ? byte.MaxValue : 2).L(unchecked((long)ulong.MaxValue))
                .S("猜平台" + suffix).H(max ? ushort.MaxValue : 3).S("猜服" + suffix)
                .S("猜名" + suffix).C(max ? 250 : 4).C(max ? 249 : 5).C(max ? 248 : 6)
                .H(max ? ushort.MaxValue : 7).S("猜图" + suffix).I(max ? uint.MaxValue : 8)
                .L(unchecked((long)(max ? ulong.MaxValue : 9UL))).H(2);
            QuizChallenger(p, suffix + "一", true);
            QuizChallenger(p, suffix + "二", false);
            p.C(max ? 247 : 10).C(max ? 246 : 11).C(max ? 245 : 12);
        }

        private static void QuizChallenger(CliVerify.Pkt p, string suffix, bool max)
        {
            p.L(unchecked((long)ulong.MaxValue)).S("猜挑平台" + suffix)
                .H(max ? ushort.MaxValue : 2).S("猜挑服" + suffix).S("猜挑名" + suffix)
                .C(max ? byte.MaxValue : 3).C(max ? 250 : 4).C(max ? 249 : 5)
                .H(max ? ushort.MaxValue : 6).S("猜挑图" + suffix).I(max ? uint.MaxValue : 7)
                .L(unchecked((long)(max ? ulong.MaxValue : 8UL)));
        }

        private static byte[] QuizHistoryPacket(bool empty)
        {
            var p = new CliVerify.Pkt().H(empty ? 0 : 2);
            if (empty) return p.Bytes();
            QuizHistoryEntry(p, "甲", true);
            QuizHistoryEntry(p, "乙", false);
            return p.Bytes();
        }

        private static void QuizHistoryEntry(CliVerify.Pkt p, string suffix, bool max)
        {
            p.L(unchecked((long)ulong.MaxValue)).S("历史平台" + suffix)
                .H(max ? ushort.MaxValue : 2).S("历史" + suffix).C(max ? byte.MaxValue : 3)
                .C(max ? 250 : 4).C(max ? 249 : 5).C(max ? 248 : 6);
        }

        private static void ObjectList(CliVerify.Pkt p, bool empty)
        {
            p.H(empty ? 0 : 2);
            if (empty) return;
            p.C(byte.MaxValue).I(uint.MaxValue).I(uint.MaxValue)
                .C(1).I(uint.MaxValue).I(2);
        }

        private static byte[] EmptyFrame(int id) => new byte[]
            { 0, 6, 3, 232, (byte)(id >> 8), (byte)id };

        private static byte[] U8Frame(int id, byte value) => new byte[]
            { 0, 7, 3, 232, (byte)(id >> 8), (byte)id, value };

        private static bool FramesAre(IReadOnlyList<byte[]> actual, params byte[][] expected)
        {
            if (actual.Count != expected.Length) return false;
            for (int i = 0; i < expected.Length; i++)
            {
                if (actual[i] == null || actual[i].Length != expected[i].Length) return false;
                for (int j = 0; j < expected[i].Length; j++)
                    if (actual[i][j] != expected[i][j]) return false;
            }
            return true;
        }

        private static List<FieldState> CaptureModel(Kf1vnModel model)
        {
            var states = new List<FieldState>();
            foreach (FieldInfo field in typeof(Kf1vnModel).GetFields(F))
            {
                if (field.IsStatic) continue;
                object value = field.GetValue(model);
                var state = new FieldState { Field = field, Value = value, IsDictionary = value is IDictionary };
                if (value is IDictionary dictionary)
                    foreach (DictionaryEntry entry in dictionary) state.Entries[entry.Key] = entry.Value;
                states.Add(state);
            }
            return states;
        }

        private static void RestoreModel(Kf1vnModel model, IEnumerable<FieldState> states)
        {
            foreach (FieldState state in states)
            {
                if (state.IsDictionary)
                {
                    if (!(state.Field.GetValue(model) is IDictionary dictionary)) continue;
                    dictionary.Clear();
                    foreach (KeyValuePair<object, object> entry in state.Entries)
                        dictionary[entry.Key] = entry.Value;
                }
                else if (!state.Field.IsInitOnly) state.Field.SetValue(model, state.Value);
            }
        }

        private static bool ModelMatches(Kf1vnModel model, IEnumerable<FieldState> states)
        {
            foreach (FieldState state in states)
            {
                object current = state.Field.GetValue(model);
                if (state.IsDictionary)
                {
                    if (!(current is IDictionary dictionary) || dictionary.Count != state.Entries.Count) return false;
                    foreach (KeyValuePair<object, object> entry in state.Entries)
                        if (!dictionary.Contains(entry.Key) || !ReferenceEquals(dictionary[entry.Key], entry.Value)) return false;
                }
                else if (state.Value != null && !state.Value.GetType().IsValueType)
                {
                    if (!ReferenceEquals(current, state.Value)) return false;
                }
                else if (!Equals(current, state.Value)) return false;
            }
            return true;
        }

        private static EntryState SaveEntry(IDictionary map, object key)
        {
            bool exists = map != null && map.Contains(key);
            return new EntryState { Exists = exists, Value = exists ? map[key] : null };
        }

        private static void SaveEntry(IDictionary map, IDictionary<int, EntryState> states, int key) =>
            states[key] = SaveEntry(map, key);

        private static void RestoreEntry(IDictionary map, object key, EntryState state)
        {
            if (map == null || state == null) return;
            if (state.Exists) map[key] = state.Value;
            else map.Remove(key);
        }

        private static bool EntryMatches(IDictionary map, object key, EntryState state)
        {
            return map != null && state != null && map.Contains(key) == state.Exists
                && (!state.Exists || ReferenceEquals(map[key], state.Value));
        }
    }
}
