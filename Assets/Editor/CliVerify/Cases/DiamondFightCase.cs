using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.DiamondFight;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>灵玉大战137安全读侧的wire、全量/增量、请求顺序、排除边界与生命周期专项。</summary>
    public static class DiamondFightCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;
        private static readonly int[] RegisteredIds =
            { 13700, 13701, 13703, 13704, 13705, 13708, 13710, 13711, 13714, 13716, 13718, 13719, 13721, 13722, 13724 };
        private static readonly int[] ExcludedIds =
            { 13702, 13706, 13707, 13709, 13712, 13713, 13715, 13717, 13720, 13723 };

        private sealed class HandlerState
        {
            public bool Exists;
            public object Value;
        }

        private sealed class ModelState
        {
            public DiamondFightModel.StageSnapshot Stage;
            public DiamondFightModel.SignSnapshot Sign;
            public DiamondFightModel.CountdownSnapshot Countdown;
            public DiamondFightModel.WaitingSnapshot Waiting;
            public DiamondFightModel.EnterResultSnapshot Enter;
            public DiamondFightModel.BattleResultSnapshot Battle;
            public DiamondFightModel.LivesSnapshot Lives;
            public readonly List<KeyValuePair<byte, DiamondFightModel.HistorySnapshot>> Histories =
                new List<KeyValuePair<byte, DiamondFightModel.HistorySnapshot>>();
            public DiamondFightModel.FakeRoleSnapshot FakeRole;
            public DiamondFightModel.ZoneSnapshot Zone;
            public DiamondFightModel.UpdateNoticeSnapshot Notice;
            public DiamondFightModel.BettingSnapshot Betting;
            public DiamondFightModel.BetRecordsSnapshot Records;
            public DiamondFightModel.BetRecord RecordDelta;
            public DiamondFightModel.WinnerSnapshot Winner;
        }

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e) { Debug.LogError("CLIVERIFY diamondfight EXCEPTION " + e); return Task.FromResult(3); }
        }

        private static int RunSync()
        {
            DiamondFightController controller = DiamondFightController.Instance;
            DiamondFightModel model = DiamondFightModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            ModelState oldModel = CaptureModel(model);
            FieldInfo interceptor = typeof(DiamondFightController).GetField("s_outboundIntercept", SF);
            object oldInterceptor = interceptor?.GetValue(null);
            FieldInfo lastLevel = typeof(DiamondFightController).GetField("_lastLevel", F);
            object oldLastLevel = lastLevel?.GetValue(controller);

            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var savedHandlers = new Dictionary<int, HandlerState>();
            for (int id = 13700; id <= 13724; id++) SaveHandler(handlers, savedHandlers, id);

            var events = typeof(EventDispatcher).GetField("_handlers", SF)?.GetValue(null)
                as Dictionary<string, List<Delegate>>;
            bool hadRoleEvent = events != null && events.ContainsKey(GlobalEvent.EVT_ROLE_INFO_UPDATE);
            var oldRoleSubscribers = hadRoleEvent
                ? new List<Delegate>(events[GlobalEvent.EVT_ROLE_INFO_UPDATE])
                : new List<Delegate>();

            IDictionary icons = typeof(ActivityIconManager).GetField("_iconInfoByType", F)
                ?.GetValue(ActivityIconManager.Instance) as IDictionary;
            IDictionary boxIcons = typeof(ActivityIconManager).GetField("_iconBoxInfoByType", F)
                ?.GetValue(ActivityIconManager.Instance) as IDictionary;
            HandlerState oldIcon = CaptureEntry(icons, DiamondFightModel.ICON_TYPE);
            HandlerState oldBoxIcon = CaptureEntry(boxIcons, DiamondFightModel.ICON_TYPE);

            bool pass = false;
            bool restored = false;
            try
            {
                controller.Init();
                model.Reset();
                var methods = new Dictionary<int, MethodInfo>();
                foreach (int id in RegisteredIds)
                    methods[id] = typeof(DiamondFightController).GetMethod("On" + id, F);

                bool a = handlers != null && interceptor != null;
                foreach (int id in RegisteredIds) a &= methods[id] != null && handlers.Contains(id);
                foreach (int id in ExcludedIds) a &= !handlers.Contains(id);
                a &= OnlySafePublicRequests();

                bool b = Invoke(methods[13701], controller, new CliVerify.Pkt().C(byte.MaxValue).Bytes())
                    && model.HasSign && model.IsSign == byte.MaxValue;
                b &= Invoke(methods[13700], controller,
                        new CliVerify.Pkt().C(5).I(uint.MaxValue).Bytes())
                    && model.HasStage && model.WarState == 5 && model.EndTime == uint.MaxValue
                    && model.HasSign && model.IsSign == 0;
                model.ReplaceStage(1, 1); model.ReplaceSign(1);
                b &= !model.GetIconOpenState();
                model.ReplaceStage(2, 2);
                b &= model.GetIconOpenState();
                b &= Invoke(methods[13703], controller,
                        new CliVerify.Pkt().C(byte.MaxValue).C(4).I(uint.MaxValue).Bytes())
                    && model.LastCountdown.Action == byte.MaxValue && model.LastCountdown.Type == 4
                    && model.LastCountdown.EndTime == uint.MaxValue;
                b &= Invoke(methods[13705], controller,
                        new CliVerify.Pkt().C(1).C(2).C(3).C(4).C(5).C(6).Bytes())
                    && model.Waiting.IsOut == 1 && model.Waiting.Zone == 2 && model.Waiting.Stage == 3
                    && model.Waiting.WinCount == 4 && model.Waiting.LoseCount == 5 && model.Waiting.LifeCount == 6;
                b &= Invoke(methods[13704], controller, new CliVerify.Pkt().I(1).Bytes())
                    && model.HasEnterResult && model.LastEnterResult.Code == 1;
                b &= Invoke(methods[13708], controller,
                        new CliVerify.Pkt().C(1).C(byte.MaxValue).C(13).Bytes())
                    && model.LastBattleResult.Settlement == 1
                    && model.LastBattleResult.Result == byte.MaxValue
                    && model.LastBattleResult.ActionId == 13;
                b &= Invoke(methods[13710], controller,
                        new CliVerify.Pkt().C(byte.MaxValue).C(0).Bytes())
                    && model.Lives.SelfLife == byte.MaxValue && model.Lives.OtherLife == 0;
                b &= Invoke(methods[13714], controller, new CliVerify.Pkt()
                        .L(unchecked((long)ulong.MaxValue)).I(uint.MaxValue).I(4000000000L).S("跨服甲").Bytes())
                    && model.FakeRole.Power == ulong.MaxValue && model.FakeRole.ServerId == uint.MaxValue
                    && model.FakeRole.ServerNumber == 4000000000U && model.FakeRole.ServerName == "跨服甲";
                b &= Invoke(methods[13716], controller, new CliVerify.Pkt().C(byte.MaxValue).Bytes())
                    && model.Zone.Zone == byte.MaxValue;
                b &= Invoke(methods[13718], controller,
                        new CliVerify.Pkt().I(uint.MaxValue).C(byte.MaxValue).Bytes())
                    && model.LastUpdateNotice.EndTime == uint.MaxValue
                    && model.LastUpdateNotice.Update == byte.MaxValue;

                DiamondFightModel.HistorySnapshot history = null;
                bool c = Invoke(methods[13711], controller, HistoryPacket(7, 2))
                    && model.TryGetHistory(7, out history)
                    && history.Entries.Count == 2
                    && history.Entries[0].Zone == 3 && history.Entries[0].Rank == byte.MaxValue
                    && history.Entries[0].RoleId == ulong.MaxValue
                    && history.Entries[0].ServerId == uint.MaxValue
                    && history.Entries[0].Platform == "平台甲"
                    && history.Entries[0].PlatformId == 4000000000U
                    && history.Entries[0].RoleName == "角色甲" && history.Entries[0].GuildName == "帮会甲"
                    && history.Entries[0].Vip == byte.MaxValue
                    && history.Entries[0].Power == 0xFEDCBA9876543210UL
                    && history.Entries[0].Career == byte.MaxValue
                    && history.Entries[1].RoleId == ulong.MaxValue;
                DiamondFightModel.HistorySnapshot oldHistory = history;
                c &= Invoke(methods[13711], controller, new CliVerify.Pkt().C(8).H(0).Bytes())
                    && model.TryGetHistory(8, out DiamondFightModel.HistorySnapshot emptyHistory)
                    && emptyHistory.Entries.Count == 0 && ReferenceEquals(oldHistory, model.Histories[7]);
                c &= Invoke(methods[13711], controller, new CliVerify.Pkt().C(7).H(0).Bytes())
                    && model.TryGetHistory(7, out history) && history.Entries.Count == 0
                    && !ReferenceEquals(history, oldHistory);

                bool d = Invoke(methods[13719], controller, BettingPacket())
                    && model.HasBetting && model.Betting.EndTime == uint.MaxValue
                    && model.Betting.Actions.Count == 2
                    && model.Betting.Actions[0].ActionId == 9
                    && model.Betting.Actions[0].Matches.Count == 2
                    && model.Betting.Actions[0].Matches[0].SupporterId == ulong.MaxValue
                    && model.Betting.Actions[0].Matches[0].ARoleId == 0xFEDCBA9876543210UL
                    && model.Betting.Actions[0].Matches[0].AServerId == ushort.MaxValue
                    && model.Betting.Actions[0].Matches[0].AName == "甲"
                    && model.Betting.Actions[0].Matches[0].APictureVersion == byte.MaxValue
                    && model.Betting.Actions[0].Matches[0].ALevel == uint.MaxValue
                    && model.Betting.Actions[0].Matches[0].APower == ulong.MaxValue
                    && model.Betting.Actions[0].Matches[0].BRoleId == 77
                    && model.Betting.Actions[1].ActionId == 9
                    && model.Betting.Actions[1].Matches.Count == 0;
                DiamondFightModel.BettingSnapshot beforeWinner = model.Betting;
                d &= Invoke(methods[13724], controller,
                        new CliVerify.Pkt().C(4).C(9).L(77).Bytes())
                    && model.HasWinner && model.LastWinner.Zone == 4 && model.LastWinner.Action == 9
                    && model.LastWinner.Winner == 77
                    && !ReferenceEquals(model.Betting, beforeWinner)
                    && model.Betting.Actions[0].Matches[0].Winner == 77
                    && model.Betting.Actions[0].Matches[1].Winner == 0;
                DiamondFightModel.BettingSnapshot patched = model.Betting;
                d &= Invoke(methods[13724], controller,
                        new CliVerify.Pkt().C(4).C(10).L(999).Bytes())
                    && model.LastWinner.Winner == 999 && ReferenceEquals(model.Betting, patched);
                d &= Invoke(methods[13719], controller,
                        new CliVerify.Pkt().I(0).H(0).Bytes())
                    && model.HasBetting && model.Betting.EndTime == 0 && model.Betting.Actions.Count == 0;

                bool e = Invoke(methods[13721], controller, RecordsPacket(2))
                    && model.HasBetRecords && model.BetRecords.Records.Count == 2
                    && model.BetRecords.Records[0].Zone == 2
                    && model.BetRecords.Records[0].Action == 11
                    && model.BetRecords.Records[0].SupporterId == ulong.MaxValue
                    && model.BetRecords.Records[0].GuessType == byte.MaxValue
                    && model.BetRecords.Records[0].RewardState == 2
                    && model.BetRecords.Records[0].Winner == 0xFEDCBA9876543210UL
                    && model.BetRecords.Records[0].AServerId == ushort.MaxValue
                    && model.BetRecords.Records[0].ALevel == uint.MaxValue
                    && model.BetRecords.Records[0].ASex == byte.MaxValue
                    && model.BetRecords.Records[0].APower == ulong.MaxValue
                    && model.BetRecords.Records[0].BName == "乙"
                    && model.BetRecords.Records[1].SupporterId == ulong.MaxValue;
                DiamondFightModel.BetRecordsSnapshot fullRecords = model.BetRecords;
                e &= Invoke(methods[13722], controller, RecordPayload(new CliVerify.Pkt(), 3).Bytes())
                    && model.HasRecordDelta && model.LastRecordDelta.Zone == 3
                    && model.BetRecords.Records.Count == 3
                    && ReferenceEquals(model.BetRecords.Records[0], fullRecords.Records[0])
                    && ReferenceEquals(model.BetRecords.Records[1], fullRecords.Records[1]);
                e &= Invoke(methods[13721], controller, new CliVerify.Pkt().H(0).Bytes())
                    && model.HasBetRecords && model.BetRecords.Records.Count == 0
                    && model.HasRecordDelta && model.LastRecordDelta.Zone == 3;
                model.Reset();
                e &= Invoke(methods[13722], controller, RecordPayload(new CliVerify.Pkt(), 5).Bytes())
                    && model.HasBetRecords && model.BetRecords.Records.Count == 1
                    && model.BetRecords.Records[0].Zone == 5;
                var mutable = new List<DiamondFightModel.BetRecord> { model.BetRecords.Records[0] };
                var immutable = new DiamondFightModel.BetRecordsSnapshot(mutable);
                mutable.Clear();
                e &= immutable.Records.Count == 1;

                SeedAll(model);
                var frames = new List<byte[]>();
                interceptor.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                controller.RequestStartup();
                bool f = IsEmpty(model) && FramesAre(frames,
                    EmptyFrame(13700), EmptyFrame(13703), EmptyFrame(13716), EmptyFrame(13721));

                SeedAll(model);
                ModelState seeded = CaptureModel(model);
                frames.Clear();
                controller.RequestStage();
                controller.RequestSign();
                controller.RequestCountdown();
                controller.RequestLives();
                controller.RequestHistory(byte.MaxValue);
                controller.RequestZone();
                controller.RequestBetting();
                controller.RequestBetRecords();
                bool g = FramesAre(frames,
                        EmptyFrame(13700), EmptyFrame(13701), EmptyFrame(13703), EmptyFrame(13710),
                        ByteFrame(13711, byte.MaxValue), EmptyFrame(13716), EmptyFrame(13719), EmptyFrame(13721))
                    && ModelMatches(model, seeded);

                controller.Dispose();
                bool h = !controller.IsInitialized && IsEmpty(model);
                foreach (int id in RegisteredIds) h &= !handlers.Contains(id);
                foreach (int id in ExcludedIds) h &= !handlers.Contains(id);

                pass = a && b && c && d && e && f && g && h;
                Debug.Log("CLIVERIFY diamondfight A=" + a + " B=" + b + " C=" + c
                    + " D=" + d + " E=" + e + " F=" + f + " G=" + g + " H=" + h
                    + " pass=" + pass);
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                RestoreModel(model, oldModel);
                if (wasInitialized) controller.Init();
                for (int id = 13700; id <= 13724; id++)
                    RestoreHandler(handlers, savedHandlers[id], id);
                if (interceptor != null) interceptor.SetValue(null, oldInterceptor);
                if (lastLevel != null) lastLevel.SetValue(controller, oldLastLevel);
                RestoreEvent(events, hadRoleEvent, oldRoleSubscribers);
                RestoreEntry(icons, DiamondFightModel.ICON_TYPE, oldIcon);
                RestoreEntry(boxIcons, DiamondFightModel.ICON_TYPE, oldBoxIcon);

                restored = controller.IsInitialized == wasInitialized
                    && ModelMatches(model, oldModel)
                    && (interceptor == null || ReferenceEquals(interceptor.GetValue(null), oldInterceptor))
                    && (lastLevel == null || Equals(lastLevel.GetValue(controller), oldLastLevel))
                    && EventMatches(events, hadRoleEvent, oldRoleSubscribers)
                    && EntryMatches(icons, DiamondFightModel.ICON_TYPE, oldIcon)
                    && EntryMatches(boxIcons, DiamondFightModel.ICON_TYPE, oldBoxIcon);
                for (int id = 13700; id <= 13724; id++)
                    restored &= HandlerMatches(handlers, savedHandlers[id], id);
                Debug.Log("CLIVERIFY diamondfight restored=" + restored);
            }
            return pass && restored ? 0 : 3;
        }

        private static bool OnlySafePublicRequests()
        {
            var allowed = new HashSet<string>
            {
                "RequestStartup", "RequestStage", "RequestSign", "RequestCountdown", "RequestLives",
                "RequestHistory", "RequestZone", "RequestBetting", "RequestBetRecords", "Dispose"
            };
            foreach (MethodInfo method in typeof(DiamondFightController).GetMethods(
                         BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                if (!allowed.Contains(method.Name)) return false;
            return true;
        }

        private static byte[] HistoryPacket(byte warNumber, int count)
        {
            var p = new CliVerify.Pkt().C(warNumber).H(count);
            for (int i = 0; i < count; i++)
                p.C(3).C(byte.MaxValue).L(unchecked((long)ulong.MaxValue)).I(uint.MaxValue)
                    .S("平台甲").I(4000000000L).S("角色甲").S("帮会甲").C(byte.MaxValue)
                    .L(unchecked((long)0xFEDCBA9876543210UL)).C(byte.MaxValue);
            return p.Bytes();
        }

        private static byte[] BettingPacket()
        {
            var p = new CliVerify.Pkt().I(uint.MaxValue).H(2).C(9).H(2);
            MatchPayload(p, ulong.MaxValue, 0xFEDCBA9876543210UL, 77);
            MatchPayload(p, 0, 88, 99);
            return p.C(9).H(0).Bytes();
        }

        private static void MatchPayload(CliVerify.Pkt p, ulong supporter, ulong aRole, ulong bRole)
        {
            p.L(unchecked((long)supporter))
                .L(unchecked((long)aRole)).H(ushort.MaxValue).H(40000).S("甲").S("图甲")
                .C(byte.MaxValue).I(uint.MaxValue).C(byte.MaxValue).L(unchecked((long)ulong.MaxValue))
                .L(unchecked((long)bRole)).H(2).H(3).S("乙").S("图乙")
                .C(4).I(4000000000L).C(5).L(unchecked((long)0xFEDCBA9876543210UL)).L(0);
        }

        private static byte[] RecordsPacket(int count)
        {
            var p = new CliVerify.Pkt().H(count);
            for (int i = 0; i < count; i++) RecordPayload(p, 2);
            return p.Bytes();
        }

        private static CliVerify.Pkt RecordPayload(CliVerify.Pkt p, byte zone)
        {
            return p.C(zone).C(11).L(unchecked((long)ulong.MaxValue)).C(byte.MaxValue).C(2)
                .L(unchecked((long)0xFEDCBA9876543210UL))
                .L(1).H(ushort.MaxValue).H(40000).S("甲").I(uint.MaxValue).C(byte.MaxValue)
                .C(byte.MaxValue).S("图甲").C(byte.MaxValue).L(unchecked((long)ulong.MaxValue))
                .L(2).H(3).H(4).S("乙").I(4000000000L).C(5).C(6).S("图乙").C(7)
                .L(unchecked((long)0xFEDCBA9876543210UL));
        }

        private static void SeedAll(DiamondFightModel model)
        {
            model.Reset();
            model.ReplaceStage(2, 3);
            model.ReplaceSign(1);
            model.ReplaceCountdown(new DiamondFightModel.CountdownSnapshot(1, 2, 3));
            model.ReplaceWaiting(new DiamondFightModel.WaitingSnapshot(1, 2, 3, 4, 5, 6));
            model.ReplaceEnterResult(1);
            model.ReplaceBattleResult(new DiamondFightModel.BattleResultSnapshot(1, 2, 3));
            model.ReplaceLives(new DiamondFightModel.LivesSnapshot(1, 2));
            model.ReplaceHistory(new DiamondFightModel.HistorySnapshot(1, Array.Empty<DiamondFightModel.HistoryEntry>()));
            model.ReplaceFakeRole(new DiamondFightModel.FakeRoleSnapshot(1, 2, 3, "seed"));
            model.ReplaceZone(1);
            model.ReplaceUpdateNotice(new DiamondFightModel.UpdateNoticeSnapshot(1, 2));
            model.ReplaceBetting(new DiamondFightModel.BettingSnapshot(1,
                Array.Empty<DiamondFightModel.BettingAction>()));
            model.ReplaceBetRecords(new DiamondFightModel.BetRecordsSnapshot(
                new[] { CreateSeedRecord(1) }));
            model.ApplyRecordDelta(CreateSeedRecord(2));
            model.ApplyWinner(new DiamondFightModel.WinnerSnapshot(1, 2, 3));
        }

        private static DiamondFightModel.BetRecord CreateSeedRecord(byte zone) =>
            new DiamondFightModel.BetRecord(zone, 1, 2, 3, 4, 5, 6, 7, 8, "a", 9, 10, 11,
                "ap", 12, 13, 14, 15, 16, "b", 17, 18, 19, "bp", 20, 21);

        private static bool IsEmpty(DiamondFightModel model) =>
            !model.HasStage && !model.HasSign && !model.HasCountdown && !model.HasWaiting
            && !model.HasEnterResult && !model.HasBattleResult && !model.HasLives
            && model.Histories.Count == 0 && !model.HasFakeRole && !model.HasZone
            && !model.HasUpdateNotice && !model.HasBetting && !model.HasBetRecords
            && !model.HasRecordDelta && !model.HasWinner;

        private static byte[] EmptyFrame(int id) =>
            new CliVerify.Pkt().H(6).H(1000).H(id).Bytes();
        private static byte[] ByteFrame(int id, byte value) =>
            new CliVerify.Pkt().H(7).H(1000).H(id).C(value).Bytes();

        private static bool FramesAre(IReadOnlyList<byte[]> actual, params byte[][] expected)
        {
            if (actual == null || expected == null || actual.Count != expected.Length) return false;
            for (int i = 0; i < expected.Length; i++)
                if (!BytesEqual(actual[i], expected[i])) return false;
            return true;
        }

        private static bool Invoke(MethodInfo handler, DiamondFightController controller, byte[] bytes)
        {
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool BytesEqual(IReadOnlyList<byte> actual, IReadOnlyList<byte> expected)
        {
            if (actual == null || expected == null || actual.Count != expected.Count) return false;
            for (int i = 0; i < expected.Count; i++) if (actual[i] != expected[i]) return false;
            return true;
        }

        private static ModelState CaptureModel(DiamondFightModel model)
        {
            var state = new ModelState
            {
                Stage = model.Stage,
                Sign = model.Sign,
                Countdown = model.LastCountdown,
                Waiting = model.Waiting,
                Enter = model.LastEnterResult,
                Battle = model.LastBattleResult,
                Lives = model.Lives,
                FakeRole = model.FakeRole,
                Zone = model.Zone,
                Notice = model.LastUpdateNotice,
                Betting = model.Betting,
                Records = model.BetRecords,
                RecordDelta = model.LastRecordDelta,
                Winner = model.LastWinner,
            };
            foreach (KeyValuePair<byte, DiamondFightModel.HistorySnapshot> pair in model.Histories)
                state.Histories.Add(pair);
            return state;
        }

        private static void RestoreModel(DiamondFightModel model, ModelState state)
        {
            model.Reset();
            RestoreProperty(model, "Stage", state.Stage);
            RestoreProperty(model, "Sign", state.Sign);
            RestoreProperty(model, "LastCountdown", state.Countdown);
            RestoreProperty(model, "Waiting", state.Waiting);
            RestoreProperty(model, "LastEnterResult", state.Enter);
            RestoreProperty(model, "LastBattleResult", state.Battle);
            RestoreProperty(model, "Lives", state.Lives);
            RestoreProperty(model, "FakeRole", state.FakeRole);
            RestoreProperty(model, "Zone", state.Zone);
            RestoreProperty(model, "LastUpdateNotice", state.Notice);
            RestoreProperty(model, "Betting", state.Betting);
            RestoreProperty(model, "BetRecords", state.Records);
            RestoreProperty(model, "LastRecordDelta", state.RecordDelta);
            RestoreProperty(model, "LastWinner", state.Winner);
            var histories = typeof(DiamondFightModel).GetField("_historyByWar", F)?.GetValue(model) as IDictionary;
            if (histories != null)
                foreach (KeyValuePair<byte, DiamondFightModel.HistorySnapshot> pair in state.Histories)
                    histories[pair.Key] = pair.Value;
        }

        private static bool ModelMatches(DiamondFightModel model, ModelState state)
        {
            if (!ReferenceEquals(model.Stage, state.Stage) || !ReferenceEquals(model.Sign, state.Sign)
                || !ReferenceEquals(model.LastCountdown, state.Countdown)
                || !ReferenceEquals(model.Waiting, state.Waiting)
                || !ReferenceEquals(model.LastEnterResult, state.Enter)
                || !ReferenceEquals(model.LastBattleResult, state.Battle)
                || !ReferenceEquals(model.Lives, state.Lives)
                || !ReferenceEquals(model.FakeRole, state.FakeRole)
                || !ReferenceEquals(model.Zone, state.Zone)
                || !ReferenceEquals(model.LastUpdateNotice, state.Notice)
                || !ReferenceEquals(model.Betting, state.Betting)
                || !ReferenceEquals(model.BetRecords, state.Records)
                || !ReferenceEquals(model.LastRecordDelta, state.RecordDelta)
                || !ReferenceEquals(model.LastWinner, state.Winner)
                || model.Histories.Count != state.Histories.Count) return false;
            foreach (KeyValuePair<byte, DiamondFightModel.HistorySnapshot> pair in state.Histories)
                if (!model.Histories.TryGetValue(pair.Key, out DiamondFightModel.HistorySnapshot actual)
                    || !ReferenceEquals(actual, pair.Value)) return false;
            return true;
        }

        private static void RestoreProperty(object target, string name, object value) =>
            target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.SetValue(target, value);

        private static void SaveHandler(IDictionary handlers, IDictionary<int, HandlerState> saved, int id)
        {
            bool exists = handlers != null && handlers.Contains(id);
            saved[id] = new HandlerState { Exists = exists, Value = exists ? handlers[id] : null };
        }

        private static void RestoreHandler(IDictionary handlers, HandlerState saved, int id)
        {
            if (handlers == null) return;
            if (saved.Exists) handlers[id] = saved.Value;
            else handlers.Remove(id);
        }

        private static bool HandlerMatches(IDictionary handlers, HandlerState saved, int id) =>
            handlers != null && handlers.Contains(id) == saved.Exists
            && (!saved.Exists || ReferenceEquals(handlers[id], saved.Value));

        private static HandlerState CaptureEntry(IDictionary dictionary, object key)
        {
            bool exists = dictionary != null && dictionary.Contains(key);
            return new HandlerState { Exists = exists, Value = exists ? dictionary[key] : null };
        }

        private static void RestoreEntry(IDictionary dictionary, object key, HandlerState state)
        {
            if (dictionary == null) return;
            if (state.Exists) dictionary[key] = state.Value;
            else dictionary.Remove(key);
        }

        private static bool EntryMatches(IDictionary dictionary, object key, HandlerState state) =>
            dictionary != null && dictionary.Contains(key) == state.Exists
            && (!state.Exists || ReferenceEquals(dictionary[key], state.Value));

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
            if (events == null || events.ContainsKey(GlobalEvent.EVT_ROLE_INFO_UPDATE) != hadEvent) return false;
            if (!hadEvent) return true;
            List<Delegate> current = events[GlobalEvent.EVT_ROLE_INFO_UPDATE];
            if (current.Count != oldSubscribers.Count) return false;
            for (int i = 0; i < current.Count; i++)
                if (!ReferenceEquals(current[i], oldSubscribers[i])) return false;
            return true;
        }
    }
}
