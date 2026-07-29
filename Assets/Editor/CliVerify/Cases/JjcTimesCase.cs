using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Jjc;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>R165: 28004 次数快照独立 slice、启动/挑战追发与生命周期回归。</summary>
    public static class JjcTimesCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e) { Debug.LogError("CLIVERIFY jjctimes EXCEPTION " + e); return Task.FromResult(3); }
        }

        private static int RunSync()
        {
            JjcController ctrl = JjcController.Instance;
            bool wasInitialized = ctrl.IsInitialized;
            ctrl.Init();
            JjcModel model = JjcModel.Instance;
            var savedBreaks = new List<int>(model.BreakIdList);
            var savedRivals = new List<JjcModel.RivalVo>(model.Rivals);
            var savedResults = new List<JjcModel.RivalVo>(model.LastChallengeRoleList);
            var savedRecords = new List<JjcModel.RecordVo>(model.ChallengeRecords);
            bool savedInfo = model.HasInfo, savedRivalsFlag = model.HasRivals, savedResult = model.HasChallengeResult, savedTimes = model.HasTimesInfo;
            int rank = model.Rank, history = model.HistoryRank, reward = model.RewardRank, hp = model.Hp, num = model.Num, refresh = model.NumRefresh, honour = model.Honour, pet = model.PetId;
            long combat = model.Combat;
            bool isReward = model.IsReward, win = model.LastChallengeWin;
            int timesErr = model.TimesErrCode; ushort left = model.LeftNum, canBuy = model.CanBuyNum; uint timesAt = model.TimesRefreshAt;
            int recordsErr = model.RecordsErrCode; bool savedRecordsFlag = model.HasChallengeRecords;
            JjcModel.ErrorSnapshot savedError = model.Error;
            JjcModel.HonourQuerySnapshot savedHonourQuery = model.HonourQuery;
            JjcModel.BattleParticipantsSnapshot savedParticipants = model.BattleParticipants;
            JjcModel.BattleStageSnapshot savedStage = model.BattleStage;
            try { return RunIsolated(ctrl, model); }
            finally
            {
                if (ctrl.IsInitialized) ctrl.Dispose();
                model.Clear();
                if (savedInfo) model.Apply28001(rank, history, reward, combat, hp, num, refresh, honour, isReward, pet, savedBreaks);
                if (savedRivalsFlag) model.Apply28002(savedRivals);
                if (savedResult) model.Apply28003(win ? 1 : 0, savedResults);
                if (savedTimes) model.Apply28004(timesErr, left, timesAt, canBuy);
                if (savedRecordsFlag) model.Apply28009(recordsErr, savedRecords);
                SetAuto(model, "Error", savedError);
                SetAuto(model, "HonourQuery", savedHonourQuery);
                SetAuto(model, "BattleParticipants", savedParticipants);
                SetAuto(model, "BattleStage", savedStage);
                if (wasInitialized) ctrl.Init();
            }
        }

        private static int RunIsolated(JjcController ctrl, JjcModel model)
        {
            MethodInfo on01 = ctrl.GetType().GetMethod("On28001", F);
            MethodInfo on04 = ctrl.GetType().GetMethod("On28004", F);
            MethodInfo on03 = ctrl.GetType().GetMethod("On28003", F);
            FieldInfo intercept = ctrl.GetType().GetField("s_outboundIntercept", SF);
            FieldInfo handlersField = typeof(NetManager).GetField("_handlers", SF);
            var handlers = handlersField?.GetValue(null) as IDictionary;
            bool pass = on01 != null && on04 != null && on03 != null && intercept != null && handlers != null;
            void Check(string tag, bool ok) { Debug.Log("CLIVERIFY jjctimes " + tag + " ok=" + ok); if (!ok) pass = false; }
            bool registrationsExact = pass;
            for (int proto = 28000; proto <= 28018; proto++)
                registrationsExact &= handlers.Contains(proto) == IsRegistered(proto);
            Check("only registers audited read slices", registrationsExact);
            if (!pass) return 3;

            object oldIntercept = intercept.GetValue(null);
            var frames = new List<byte[]>();
            try
            {
                intercept.SetValue(null, new Func<byte[], bool>(f => { frames.Add(f); return true; }));
                model.Clear();
                ctrl.RequestInfo(); ctrl.RequestRivals(); ctrl.RequestTimesInfo();
                Check("three explicit strict empty requests", Frames(frames, 28001, 28002, 28004));

                model.Apply28001(9, 8, 7, 6, 5, 4, 3, 2, true, 1, new List<int> { 99 });
                model.Apply28002(new List<JjcModel.RivalVo> { new JjcModel.RivalVo { Rank = 1 } });
                model.Apply28003(1, new List<JjcModel.RivalVo> { new JjcModel.RivalVo { Rank = 2 } });
                model.Apply28004(-1, ushort.MaxValue, uint.MaxValue, ushort.MaxValue);
                model.Apply28009(1, new List<JjcModel.RecordVo> { new JjcModel.RecordVo { RoleId = 1 } });
                model.ReplaceError(1); model.ReplaceHonourQuery(2, 3);
                model.ReplaceBattleParticipants(4, 5, 6, 7); model.ReplaceBattleStage(8, 9);
                frames.Clear(); EventDispatcher.Emit(GlobalEvent.EVT_GAME_START);
                Check("game start clears all then 28004-28001", !model.HasInfo && !model.HasRivals && !model.HasChallengeResult && !model.HasTimesInfo && !model.HasChallengeRecords
                    && model.Error == null && model.HonourQuery == null && model.BattleParticipants == null && model.BattleStage == null
                    && Frames(frames, 28004, 28001));

                Feed(on04, ctrl, new CliVerify.Pkt().I(0).H(0).I(0).H(0).Bytes(), out NetReader zero);
                Check("28004 zero/read-end", zero.Remaining == 0 && model.HasTimesInfo && model.TimesErrCode == 0 && model.LeftNum == 0 && model.TimesRefreshAt == 0 && model.CanBuyNum == 0);
                Feed(on04, ctrl, new CliVerify.Pkt().I(uint.MaxValue).H(ushort.MaxValue).I(uint.MaxValue).H(ushort.MaxValue).Bytes(), out NetReader max);
                Check("28004 full width boundary", max.Remaining == 0 && model.TimesErrCode == -1 && model.LeftNum == ushort.MaxValue && model.TimesRefreshAt == uint.MaxValue && model.CanBuyNum == ushort.MaxValue);
                Feed(on04, ctrl, new CliVerify.Pkt().I(7).H(2).I(3).H(4).Bytes(), out NetReader small);
                Check("28004 whole replacement", small.Remaining == 0 && model.TimesErrCode == 7 && model.LeftNum == 2 && model.TimesRefreshAt == 3 && model.CanBuyNum == 4);

                Feed(on01, ctrl, new CliVerify.Pkt().I(1).I(2).I(3).L(4).I(5).H(6).I(7).I(8).C(1).I(9).H(0).Bytes(), out NetReader info);
                Check("28001 does not overwrite 28004", info.Remaining == 0 && model.Num == 6 && model.NumRefresh == 7 && model.LeftNum == 2 && model.TimesRefreshAt == 3);
                Feed(on04, ctrl, new CliVerify.Pkt().I(8).H(9).I(10).H(11).Bytes(), out NetReader isolated);
                Check("28004 does not overwrite 28001", isolated.Remaining == 0 && model.Num == 6 && model.NumRefresh == 7 && model.LeftNum == 9 && model.TimesRefreshAt == 10);

                frames.Clear(); Feed(on03, ctrl, new CliVerify.Pkt().H(0).C(1).H(0).H(0).Bytes(), out NetReader challenge);
                Check("28003 read-end/result/followups", challenge.Remaining == 0 && model.HasChallengeResult && model.LastChallengeWin && Frames(frames, 28004, 28002));
                int before = frames.Count; // no response must leave latest snapshot unchanged.
                Check("no response retains state", before == 2 && model.LeftNum == 9 && model.TimesRefreshAt == 10);

                ctrl.Dispose(); frames.Clear(); EventDispatcher.Emit(GlobalEvent.EVT_GAME_START);
                bool removed = true;
                for (int proto = 28000; proto <= 28018; proto++) if (IsRegistered(proto)) removed &= !handlers.Contains(proto);
                Check("dispose unregisters handlers/event and clears", frames.Count == 0 && removed && !model.HasInfo && !model.HasRivals && !model.HasChallengeResult && !model.HasTimesInfo && !model.HasChallengeRecords
                    && model.Error == null && model.HonourQuery == null && model.BattleParticipants == null && model.BattleStage == null);
            }
            finally { intercept.SetValue(null, oldIntercept); }
            Debug.Log("CLIVERIFY jjctimes VERDICT pass=" + pass);
            return pass ? 0 : 3;
        }

        private static void Feed(MethodInfo method, JjcController ctrl, byte[] bytes, out NetReader reader)
        {
            reader = new NetReader(bytes, 0, bytes.Length);
            method.Invoke(ctrl, new object[] { reader });
        }

        private static bool Frames(IReadOnlyList<byte[]> frames, params int[] ids)
        {
            if (frames.Count != ids.Length) return false;
            for (int i = 0; i < ids.Length; i++)
            {
                byte[] f = frames[i];
                if (f == null || f.Length != 6 || f[0] != 0 || f[1] != 6 || f[2] != 3 || f[3] != 232 || f[4] != (byte)(ids[i] >> 8) || f[5] != (byte)ids[i]) return false;
            }
            return true;
        }

        private static bool IsRegistered(int proto) => proto == 28000 || (proto >= 28001 && proto <= 28004)
            || proto == 28009 || proto == 28010 || proto == 28013 || proto == 28014;

        private static void SetAuto(JjcModel model, string property, object value)
        {
            FieldInfo field = typeof(JjcModel).GetField("<" + property + ">k__BackingField", F);
            if (field == null) throw new MissingFieldException(typeof(JjcModel).FullName, property);
            field.SetValue(model, value);
        }
    }
}
