using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.TopPk;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>巅峰对决281安全读侧的wire、全量替换、推送隔离、请求顺序与生命周期专项。</summary>
    public static class TopPkCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;
        private static readonly int[] RegisteredIds =
            { 28100, 28101, 28105, 28107, 28111, 28112, 28113, 28115, 28117 };
        private static readonly int[] ExcludedIds =
            { 28102, 28103, 28104, 28106, 28108, 28109, 28110, 28114, 28116 };

        private sealed class HandlerState
        {
            public bool Exists;
            public object Value;
        }

        private sealed class ModelState
        {
            public bool HasError;
            public uint ErrorCode;
            public string ErrorArgs;
            public TopPkModel.InfoSnapshot Info;
            public TopPkModel.LevelRewardsSnapshot LevelRewards;
            public TopPkModel.ActivitySnapshot Activity;
            public TopPkModel.MatchSnapshot Match;
            public TopPkModel.StageSnapshot Stage;
            public TopPkModel.ResultSnapshot Result;
            public TopPkModel.RanksSnapshot Ranks;
            public TopPkModel.PromotionSnapshot Promotion;
        }

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e) { Debug.LogError("CLIVERIFY toppk EXCEPTION " + e); return Task.FromResult(3); }
        }

        private static int RunSync()
        {
            TopPkController controller = TopPkController.Instance;
            TopPkModel model = TopPkModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            ModelState oldModel = CaptureModel(model);
            FieldInfo interceptor = typeof(TopPkController).GetField("s_outboundIntercept", SF);
            object oldInterceptor = interceptor?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var savedHandlers = new Dictionary<int, HandlerState>();
            for (int id = 28100; id <= 28117; id++) SaveHandler(handlers, savedHandlers, id);

            bool pass = false;
            bool restored = false;
            try
            {
                controller.Init();
                model.Reset();
                var methods = new Dictionary<int, MethodInfo>();
                foreach (int id in RegisteredIds)
                    methods[id] = typeof(TopPkController).GetMethod("On" + id, F);

                bool a = handlers != null && interceptor != null;
                foreach (int id in RegisteredIds)
                    a &= methods[id] != null && handlers.Contains(id);
                foreach (int id in ExcludedIds) a &= !handlers.Contains(id);
                a &= OnlySafePublicRequests();

                bool b = Invoke(methods[28100], controller,
                        new CliVerify.Pkt().I(uint.MaxValue).S("错误参数").Bytes())
                    && model.HasError && model.LastErrorCode == uint.MaxValue
                    && model.LastErrorArgs == "错误参数";
                b &= Invoke(methods[28101], controller, new CliVerify.Pkt()
                        .H(ushort.MaxValue).I(uint.MaxValue).C(byte.MaxValue).I(4000000000L)
                        .I(uint.MaxValue).I(0).I(uint.MaxValue).C(2).H(ushort.MaxValue).H(2)
                        .C(7).C(0).C(7).C(byte.MaxValue).H(ushort.MaxValue).C(byte.MaxValue).Bytes())
                    && model.HasInfo && model.Info.SeasonNumber == ushort.MaxValue
                    && model.Info.SeasonEndTime == uint.MaxValue
                    && model.Info.RankLevel == byte.MaxValue && model.Info.Point == 4000000000U
                    && model.Info.SeasonCount == uint.MaxValue && model.Info.SeasonWinCount == 0
                    && model.Info.DailyHonorValue == uint.MaxValue && model.Info.HonorState == 2
                    && model.Info.DailyCount == ushort.MaxValue && model.Info.DailyBuyCount == ushort.MaxValue
                    && model.Info.YesterdayRankLevel == byte.MaxValue
                    && model.Info.DailyRewards.Count == 2
                    && model.Info.DailyRewards[0].Count == 7 && model.Info.DailyRewards[0].State == 0
                    && model.Info.DailyRewards[1].Count == 7
                    && model.Info.DailyRewards[1].State == byte.MaxValue
                    && model.HasError && model.LastErrorCode == uint.MaxValue;
                TopPkModel.InfoSnapshot oldInfo = model.Info;
                b &= Invoke(methods[28101], controller, new CliVerify.Pkt()
                        .H(1).I(2).C(3).I(4).I(5).I(6).I(7).C(8).H(9).H(0).H(10).C(11).Bytes())
                    && !ReferenceEquals(model.Info, oldInfo) && model.Info.SeasonNumber == 1
                    && model.Info.Point == 4 && model.Info.DailyRewards.Count == 0
                    && model.Info.DailyBuyCount == 10 && model.Info.YesterdayRankLevel == 11;

                bool c = Invoke(methods[28105], controller, new CliVerify.Pkt().H(3)
                        .C(4).C(0).C(4).C(2).C(byte.MaxValue).C(byte.MaxValue).Bytes())
                    && model.HasLevelRewards && model.LevelRewards.Rewards.Count == 3
                    && model.LevelRewards.Rewards[0].RankLevel == 4
                    && model.LevelRewards.Rewards[1].RankLevel == 4
                    && model.LevelRewards.Rewards[1].State == 2
                    && model.LevelRewards.Rewards[2].RankLevel == byte.MaxValue;
                c &= Invoke(methods[28107], controller,
                        new CliVerify.Pkt().C(byte.MaxValue).I(uint.MaxValue).I(4000000000L).Bytes())
                    && model.HasActivity && model.Activity.State == byte.MaxValue
                    && model.Activity.StartTime == uint.MaxValue && model.Activity.EndTime == 4000000000U
                    && model.LevelRewards.Rewards.Count == 3;
                c &= Invoke(methods[28105], controller, new CliVerify.Pkt().H(0).Bytes())
                    && model.HasLevelRewards && model.LevelRewards.Rewards.Count == 0
                    && model.Activity.State == byte.MaxValue;
                c &= Invoke(methods[28107], controller, new CliVerify.Pkt().C(0).I(0).I(0).Bytes())
                    && model.Activity.State == 0 && model.Activity.StartTime == 0 && model.Activity.EndTime == 0;

                bool d = Invoke(methods[28111], controller, new CliVerify.Pkt()
                        .C(1).C(9).C(10).L(unchecked((long)ulong.MaxValue)).Bytes())
                    && model.HasMatch && model.LastMatch.Result == 1
                    && model.LastMatch.MyRankLevel == 9 && model.LastMatch.EnemyRankLevel == 10
                    && model.LastMatch.FakeManPower == ulong.MaxValue;
                d &= Invoke(methods[28112], controller,
                        new CliVerify.Pkt().C(byte.MaxValue).I(uint.MaxValue).Bytes())
                    && model.HasStage && model.LastStage.Stage == byte.MaxValue
                    && model.LastStage.Time == uint.MaxValue && model.LastMatch.Result == 1;
                d &= Invoke(methods[28113], controller, new CliVerify.Pkt()
                        .C(0).I(uint.MaxValue).C(byte.MaxValue).I(4000000000L).Bytes())
                    && model.HasResult && model.LastResult.Result == 0
                    && model.LastResult.Honor == uint.MaxValue
                    && model.LastResult.PointSign == byte.MaxValue
                    && model.LastResult.PointDelta == 4000000000U;
                d &= Invoke(methods[28117], controller, new CliVerify.Pkt()
                        .C(1).I(uint.MaxValue).C(byte.MaxValue).I(0).Bytes())
                    && model.HasPromotion && model.LastPromotion.OldRankLevel == 1
                    && model.LastPromotion.OldPoint == uint.MaxValue
                    && model.LastPromotion.NewRankLevel == byte.MaxValue
                    && model.LastPromotion.NewPoint == 0 && model.LastResult.PointDelta == 4000000000U;

                bool e = Invoke(methods[28115], controller, new CliVerify.Pkt().H(2)
                        .L(unchecked((long)ulong.MaxValue)).S("甲").C(byte.MaxValue)
                        .L(unchecked((long)0xFEDCBA9876543210UL)).S("帮会").S("平台")
                        .H(ushort.MaxValue).C(byte.MaxValue).I(uint.MaxValue)
                        .L(unchecked((long)ulong.MaxValue)).S(string.Empty).C(0)
                        .L(0).S(string.Empty).S(string.Empty).H(0).C(0).I(0).Bytes())
                    && model.HasRanks && model.Ranks.Ranks.Count == 2
                    && model.Ranks.Ranks[0].RoleId == ulong.MaxValue
                    && model.Ranks.Ranks[0].RoleName == "甲"
                    && model.Ranks.Ranks[0].Career == byte.MaxValue
                    && model.Ranks.Ranks[0].Power == 0xFEDCBA9876543210UL
                    && model.Ranks.Ranks[0].GuildName == "帮会"
                    && model.Ranks.Ranks[0].Platform == "平台"
                    && model.Ranks.Ranks[0].ServerNumber == ushort.MaxValue
                    && model.Ranks.Ranks[0].RankLevel == byte.MaxValue
                    && model.Ranks.Ranks[0].Point == uint.MaxValue
                    && model.Ranks.Ranks[1].RoleId == ulong.MaxValue
                    && model.Ranks.Ranks[1].RoleName == string.Empty;
                TopPkModel.RanksSnapshot oldRanks = model.Ranks;
                e &= Invoke(methods[28115], controller, new CliVerify.Pkt().H(0).Bytes())
                    && model.HasRanks && model.Ranks.Ranks.Count == 0
                    && !ReferenceEquals(model.Ranks, oldRanks);
                var source = new List<TopPkModel.RankEntry>
                    { new TopPkModel.RankEntry(1, "不可变", 1, 1, "", "", 1, 1, 1) };
                var immutable = new TopPkModel.RanksSnapshot(source);
                source.Clear();
                e &= immutable.Ranks.Count == 1;

                SeedAll(model);
                ModelState seeded = CaptureModel(model);
                var frames = new List<byte[]>();
                interceptor.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));
                controller.RequestStartup();
                bool f = FramesAre(frames, EmptyFrame(28101), EmptyFrame(28105), EmptyFrame(28107))
                    && ModelMatches(model, seeded);

                frames.Clear();
                controller.RequestInfo();
                controller.RequestLevelRewards();
                controller.RequestActivity();
                controller.RequestRanks();
                bool g = FramesAre(frames, EmptyFrame(28101), EmptyFrame(28105),
                        EmptyFrame(28107), EmptyFrame(28115))
                    && ModelMatches(model, seeded);

                controller.Dispose();
                bool h = !controller.IsInitialized && IsEmpty(model);
                foreach (int id in RegisteredIds) h &= !handlers.Contains(id);
                foreach (int id in ExcludedIds) h &= !handlers.Contains(id);

                pass = a && b && c && d && e && f && g && h;
                Debug.Log("CLIVERIFY toppk A=" + a + " B=" + b + " C=" + c
                    + " D=" + d + " E=" + e + " F=" + f + " G=" + g + " H=" + h
                    + " pass=" + pass);
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                RestoreModel(model, oldModel);
                if (wasInitialized) controller.Init();
                for (int id = 28100; id <= 28117; id++)
                    RestoreHandler(handlers, savedHandlers[id], id);
                if (interceptor != null) interceptor.SetValue(null, oldInterceptor);

                restored = controller.IsInitialized == wasInitialized
                    && ModelMatches(model, oldModel)
                    && (interceptor == null || ReferenceEquals(interceptor.GetValue(null), oldInterceptor));
                for (int id = 28100; id <= 28117; id++)
                    restored &= HandlerMatches(handlers, savedHandlers[id], id);
                Debug.Log("CLIVERIFY toppk restored=" + restored);
            }
            return pass && restored ? 0 : 3;
        }

        private static bool OnlySafePublicRequests()
        {
            var allowed = new HashSet<string>
                { "RequestStartup", "RequestInfo", "RequestLevelRewards", "RequestActivity", "RequestRanks", "Dispose" };
            foreach (MethodInfo method in typeof(TopPkController).GetMethods(
                         BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                if (!allowed.Contains(method.Name)) return false;
            return true;
        }

        private static void SeedAll(TopPkModel model)
        {
            model.Reset();
            model.SetError(1, "seed");
            model.ReplaceInfo(new TopPkModel.InfoSnapshot(1, 2, 3, 4, 5, 6, 7, 8, 9,
                new[] { new TopPkModel.DailyCountReward(10, 11) }, 12, 13));
            model.ReplaceLevelRewards(new TopPkModel.LevelRewardsSnapshot(
                new[] { new TopPkModel.LevelReward(1, 2) }));
            model.ReplaceActivity(new TopPkModel.ActivitySnapshot(1, 2, 3));
            model.ReplaceMatch(new TopPkModel.MatchSnapshot(1, 2, 3, 4));
            model.ReplaceStage(new TopPkModel.StageSnapshot(1, 2));
            model.ReplaceResult(new TopPkModel.ResultSnapshot(1, 2, 3, 4));
            model.ReplaceRanks(new TopPkModel.RanksSnapshot(new[]
                { new TopPkModel.RankEntry(1, "seed", 2, 3, "g", "p", 4, 5, 6) }));
            model.ReplacePromotion(new TopPkModel.PromotionSnapshot(1, 2, 3, 4));
        }

        private static bool IsEmpty(TopPkModel model) =>
            !model.HasError && model.LastErrorCode == 0 && model.LastErrorArgs == null
            && !model.HasInfo && !model.HasLevelRewards && !model.HasActivity && !model.HasMatch
            && !model.HasStage && !model.HasResult && !model.HasRanks && !model.HasPromotion;

        private static byte[] EmptyFrame(int id) =>
            new CliVerify.Pkt().H(6).H(1000).H(id).Bytes();

        private static bool FramesAre(IReadOnlyList<byte[]> actual, params byte[][] expected)
        {
            if (actual == null || expected == null || actual.Count != expected.Length) return false;
            for (int i = 0; i < expected.Length; i++)
                if (!BytesEqual(actual[i], expected[i])) return false;
            return true;
        }

        private static bool Invoke(MethodInfo handler, TopPkController controller, byte[] bytes)
        {
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool BytesEqual(IReadOnlyList<byte> actual, IReadOnlyList<byte> expected)
        {
            if (actual == null || expected == null || actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++) if (actual[i] != expected[i]) return false;
            return true;
        }

        private static ModelState CaptureModel(TopPkModel model) => new ModelState
        {
            HasError = model.HasError,
            ErrorCode = model.LastErrorCode,
            ErrorArgs = model.LastErrorArgs,
            Info = model.Info,
            LevelRewards = model.LevelRewards,
            Activity = model.Activity,
            Match = model.LastMatch,
            Stage = model.LastStage,
            Result = model.LastResult,
            Ranks = model.Ranks,
            Promotion = model.LastPromotion,
        };

        private static void RestoreModel(TopPkModel model, ModelState state)
        {
            model.Reset();
            if (state.HasError) model.SetError(state.ErrorCode, state.ErrorArgs);
            if (state.Info != null) model.ReplaceInfo(state.Info);
            if (state.LevelRewards != null) model.ReplaceLevelRewards(state.LevelRewards);
            if (state.Activity != null) model.ReplaceActivity(state.Activity);
            if (state.Match != null) model.ReplaceMatch(state.Match);
            if (state.Stage != null) model.ReplaceStage(state.Stage);
            if (state.Result != null) model.ReplaceResult(state.Result);
            if (state.Ranks != null) model.ReplaceRanks(state.Ranks);
            if (state.Promotion != null) model.ReplacePromotion(state.Promotion);
        }

        private static bool ModelMatches(TopPkModel model, ModelState state) =>
            model.HasError == state.HasError && model.LastErrorCode == state.ErrorCode
            && model.LastErrorArgs == state.ErrorArgs && ReferenceEquals(model.Info, state.Info)
            && ReferenceEquals(model.LevelRewards, state.LevelRewards)
            && ReferenceEquals(model.Activity, state.Activity)
            && ReferenceEquals(model.LastMatch, state.Match)
            && ReferenceEquals(model.LastStage, state.Stage)
            && ReferenceEquals(model.LastResult, state.Result)
            && ReferenceEquals(model.Ranks, state.Ranks)
            && ReferenceEquals(model.LastPromotion, state.Promotion);

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
    }
}
