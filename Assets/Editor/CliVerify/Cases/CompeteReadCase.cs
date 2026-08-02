using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Compete;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class CompeteReadCase
    {
        private const BindingFlags InstanceNonPublic = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags StaticNonPublic = BindingFlags.Static | BindingFlags.NonPublic;

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunCore()); }
            catch (Exception exception)
            {
                Debug.LogError("CLIVERIFY compete-read EXCEPTION " + exception);
                return Task.FromResult(3);
            }
        }

        private static int RunCore()
        {
            CompeteController controller = CompeteController.Instance;
            CompeteModel model = CompeteModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            var oldActList = new List<CompeteModel.RaceActInfo>(model.ActList);
            var oldViews = new Dictionary<uint, CompeteModel.ViewSnapshot>(model.Views);
            var oldRanks = new Dictionary<uint, CompeteModel.RankSnapshot>(model.Ranks);
            FieldInfo intercept = typeof(CompeteController).GetField("s_readOutboundIntercept", StaticNonPublic);
            object oldIntercept = intercept?.GetValue(null);
            var frames = new List<byte[]>();
            bool pass = true;

            try
            {
                controller.Init();
                model.Reset();
                MethodInfo on33801 = typeof(CompeteController).GetMethod("On33801", InstanceNonPublic);
                MethodInfo on33802 = typeof(CompeteController).GetMethod("On33802", InstanceNonPublic);
                IDictionary handlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                pass &= intercept != null && on33801 != null && on33802 != null && handlers != null;
                pass &= handlers.Contains(Proto.COMPETE_ACT_LIST)
                    && handlers.Contains(Proto.COMPETE_VIEW_INFO)
                    && handlers.Contains(Proto.COMPETE_RANK_INFO);
                pass &= !handlers.Contains(33804) && !handlers.Contains(33807);
                pass &= typeof(CompeteController).GetMethod("On33803", InstanceNonPublic) == null
                    && typeof(CompeteController).GetMethod("On33807", InstanceNonPublic) == null
                    && typeof(CompeteController).GetMethod("RequestReward", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly) == null;

                intercept.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                controller.RequestViewInfo(0, ushort.MaxValue);
                pass &= frames.Count == 1 && KeyFrame(frames[0], Proto.COMPETE_VIEW_INFO, 0, ushort.MaxValue)
                    && model.Views.Count == 0 && model.Ranks.Count == 0;
                frames.Clear();
                controller.RequestRankInfo(ushort.MaxValue, 0);
                pass &= frames.Count == 1 && KeyFrame(frames[0], Proto.COMPETE_RANK_INFO, ushort.MaxValue, 0)
                    && model.Views.Count == 0 && model.Ranks.Count == 0;
                frames.Clear();

                model.SetActList(new List<CompeteModel.RaceActInfo>
                {
                    new CompeteModel.RaceActInfo { Type = 9, Subtype = 8, ShowId = 7, StartTime = 6, EndTime = 5, BuyEndTime = 4 }
                });

                ObjectSpec maximum = new ObjectSpec(byte.MaxValue, uint.MaxValue, uint.MaxValue);
                ObjectSpec zero = new ObjectSpec(0, 0, 0);
                CompeteModel.ViewSnapshot fullView = null;
                CompeteModel.ViewSnapshot beforeNoReply = null;
                byte[] viewFull = ViewPacket(
                    1, 2, byte.MaxValue, uint.MaxValue, 4000000000U,
                    new[] { maximum, zero, maximum },
                    new[] { new ObjectSpec(3, 4, 5) },
                    new ushort[] { ushort.MaxValue, 0, ushort.MaxValue },
                    new[] { new StageSpec(ushort.MaxValue, byte.MaxValue), new StageSpec(0, 0), new StageSpec(ushort.MaxValue, byte.MaxValue) },
                    uint.MaxValue);
                pass &= Feed(on33801, controller, viewFull) && frames.Count == 0
                    && model.ActList.Count == 1 && model.Ranks.Count == 0
                    && model.TryGetViewInfo(1, 2, out fullView)
                    && SameView(fullView, 1, 2, byte.MaxValue, uint.MaxValue, 4000000000U, 3, 1, 3, 3, uint.MaxValue)
                    && SameObject(fullView.Cost[0], maximum) && SameObject(fullView.Cost[1], zero) && SameObject(fullView.Cost[2], maximum)
                    && SameObject(fullView.TenCost[0], new ObjectSpec(3, 4, 5))
                    && fullView.RewardIds[0] == ushort.MaxValue && fullView.RewardIds[1] == 0 && fullView.RewardIds[2] == ushort.MaxValue
                    && fullView.Stages[0].Id == ushort.MaxValue && fullView.Stages[0].GotType == byte.MaxValue
                    && fullView.Stages[1].Id == 0 && fullView.Stages[1].GotType == 0;

                pass &= Feed(on33801, controller, ViewPacket(3, 4, 0, 0, 0,
                        Array.Empty<ObjectSpec>(), Array.Empty<ObjectSpec>(), Array.Empty<ushort>(), Array.Empty<StageSpec>(), 0))
                    && model.Views.Count == 2 && model.TryGetViewInfo(3, 4, out CompeteModel.ViewSnapshot emptyOther)
                    && SameView(emptyOther, 3, 4, 0, 0, 0, 0, 0, 0, 0, 0)
                    && model.TryGetViewInfo(1, 2, out beforeNoReply)
                    && ReferenceEquals(beforeNoReply, fullView) && model.Ranks.Count == 0;

                controller.RequestViewInfo(1, 2);
                pass &= frames.Count == 1 && KeyFrame(frames[0], Proto.COMPETE_VIEW_INFO, 1, 2)
                    && model.TryGetViewInfo(1, 2, out CompeteModel.ViewSnapshot afterNoReply)
                    && ReferenceEquals(afterNoReply, beforeNoReply);
                frames.Clear();

                pass &= Feed(on33801, controller, ViewPacket(1, 2, 0, 0, 0,
                        Array.Empty<ObjectSpec>(), Array.Empty<ObjectSpec>(), Array.Empty<ushort>(), Array.Empty<StageSpec>(), 0))
                    && model.TryGetViewInfo(1, 2, out CompeteModel.ViewSnapshot replacedView)
                    && !ReferenceEquals(replacedView, fullView)
                    && SameView(replacedView, 1, 2, 0, 0, 0, 0, 0, 0, 0, 0)
                    && SameView(fullView, 1, 2, byte.MaxValue, uint.MaxValue, 4000000000U, 3, 1, 3, 3, uint.MaxValue)
                    && model.Views.Count == 2 && model.Ranks.Count == 0 && model.ActList.Count == 1 && frames.Count == 0;

                const string chinese = "\u7ade\u699c\u73a9\u5bb6";
                RankSpec maxRank = new RankSpec(ushort.MaxValue, uint.MaxValue, ulong.MaxValue, chinese, uint.MaxValue);
                RankSpec zeroRank = new RankSpec(0, 0, 0, string.Empty, 0);
                CompeteModel.RankSnapshot fullRank = null;
                CompeteModel.RankSnapshot rankBeforeNoReply = null;
                pass &= Feed(on33802, controller, RankPacket(1, 2, uint.MaxValue, ushort.MaxValue,
                        new[] { maxRank, zeroRank, maxRank }))
                    && model.TryGetRankInfo(1, 2, out fullRank)
                    && SameRank(fullRank, 1, 2, uint.MaxValue, ushort.MaxValue, 3)
                    && SameRankEntry(fullRank.Entries[0], maxRank)
                    && SameRankEntry(fullRank.Entries[1], zeroRank)
                    && SameRankEntry(fullRank.Entries[2], maxRank)
                    && model.Views.Count == 2 && model.Ranks.Count == 1 && model.ActList.Count == 1 && frames.Count == 0;

                pass &= Feed(on33802, controller, RankPacket(3, 4, 0, 0, Array.Empty<RankSpec>()))
                    && model.Ranks.Count == 2 && model.TryGetRankInfo(3, 4, out CompeteModel.RankSnapshot emptyRank)
                    && SameRank(emptyRank, 3, 4, 0, 0, 0)
                    && model.TryGetRankInfo(1, 2, out rankBeforeNoReply)
                    && ReferenceEquals(rankBeforeNoReply, fullRank);

                controller.RequestRankInfo(1, 2);
                pass &= frames.Count == 1 && KeyFrame(frames[0], Proto.COMPETE_RANK_INFO, 1, 2)
                    && model.TryGetRankInfo(1, 2, out CompeteModel.RankSnapshot rankAfterNoReply)
                    && ReferenceEquals(rankAfterNoReply, rankBeforeNoReply);
                frames.Clear();

                pass &= Feed(on33802, controller, RankPacket(1, 2, 0, 0, Array.Empty<RankSpec>()))
                    && model.TryGetRankInfo(1, 2, out CompeteModel.RankSnapshot replacedRank)
                    && !ReferenceEquals(replacedRank, fullRank) && SameRank(replacedRank, 1, 2, 0, 0, 0)
                    && SameRank(fullRank, 1, 2, uint.MaxValue, ushort.MaxValue, 3)
                    && SameRankEntry(fullRank.Entries[0], maxRank)
                    && model.TryGetViewInfo(3, 4, out emptyOther) && emptyOther.Cost.Count == 0
                    && model.Views.Count == 2 && model.Ranks.Count == 2 && model.ActList.Count == 1 && frames.Count == 0;

                pass &= CompeteModel.MakeKey(ushort.MaxValue, 0) != CompeteModel.MakeKey(0, ushort.MaxValue)
                    && !model.TryGetViewInfo(ushort.MaxValue, ushort.MaxValue, out _)
                    && !model.TryGetRankInfo(ushort.MaxValue, ushort.MaxValue, out _);

                model.Reset();
                pass &= model.ActList.Count == 0 && model.Views.Count == 0 && model.Ranks.Count == 0 && frames.Count == 0;
            }
            finally
            {
                model.Reset();
                model.SetActList(oldActList);
                foreach (KeyValuePair<uint, CompeteModel.ViewSnapshot> pair in oldViews)
                    model.ReplaceViewInfo(pair.Value);
                foreach (KeyValuePair<uint, CompeteModel.RankSnapshot> pair in oldRanks)
                    model.ReplaceRankInfo(pair.Value);
                if (!wasInitialized && controller.IsInitialized) controller.Dispose();
                if (intercept != null) intercept.SetValue(null, oldIntercept);
            }

            Debug.Log("CLIVERIFY compete-read VERDICT pass=" + pass);
            return pass ? 0 : 3;
        }

        private static bool Feed(MethodInfo method, CompeteController controller, byte[] payload)
        {
            var reader = new NetReader(payload, 0, payload.Length);
            method.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool KeyFrame(byte[] frame, int protocol, ushort type, ushort subtype)
        {
            return frame != null && frame.Length == 10
                && frame[0] == 0 && frame[1] == 10 && frame[2] == 3 && frame[3] == 232
                && frame[4] == (byte)(protocol >> 8) && frame[5] == (byte)protocol
                && frame[6] == (byte)(type >> 8) && frame[7] == (byte)type
                && frame[8] == (byte)(subtype >> 8) && frame[9] == (byte)subtype;
        }

        private static bool SameView(CompeteModel.ViewSnapshot snapshot, ushort type, ushort subtype, byte isOpen,
            uint score, uint todayScore, int cost, int tenCost, int rewards, int stages, uint worldLevel)
        {
            return snapshot != null && snapshot.Type == type && snapshot.Subtype == subtype
                && snapshot.IsOpen == isOpen && snapshot.Score == score && snapshot.TodayScore == todayScore
                && snapshot.Cost.Count == cost && snapshot.TenCost.Count == tenCost
                && snapshot.RewardIds.Count == rewards && snapshot.Stages.Count == stages
                && snapshot.WorldLevel == worldLevel;
        }

        private static bool SameObject(CompeteModel.ObjectEntry entry, ObjectSpec expected)
            => entry != null && entry.Style == expected.Style && entry.TypeId == expected.TypeId && entry.Num == expected.Num;

        private static bool SameRank(CompeteModel.RankSnapshot snapshot, ushort type, ushort subtype,
            uint score, ushort rank, int count)
            => snapshot != null && snapshot.Type == type && snapshot.Subtype == subtype
                && snapshot.Score == score && snapshot.Rank == rank && snapshot.Entries.Count == count;

        private static bool SameRankEntry(CompeteModel.RankEntry entry, RankSpec expected)
            => entry != null && entry.Rank == expected.Rank && entry.ServerId == expected.ServerId
                && entry.RoleId == expected.RoleId && entry.RoleName == expected.RoleName && entry.RoleScore == expected.RoleScore;

        private static byte[] ViewPacket(ushort type, ushort subtype, byte isOpen, uint score, uint todayScore,
            ObjectSpec[] cost, ObjectSpec[] tenCost, ushort[] rewardIds, StageSpec[] stages, uint worldLevel)
        {
            var packet = new CliVerify.Pkt().H(type).H(subtype).C(isOpen).I(score).I(todayScore);
            AppendObjects(packet, cost);
            AppendObjects(packet, tenCost);
            packet.H(rewardIds.Length);
            foreach (ushort id in rewardIds) packet.H(id);
            packet.H(stages.Length);
            foreach (StageSpec stage in stages) packet.H(stage.Id).C(stage.GotType);
            return packet.I(worldLevel).Bytes();
        }

        private static void AppendObjects(CliVerify.Pkt packet, ObjectSpec[] entries)
        {
            packet.H(entries.Length);
            foreach (ObjectSpec entry in entries) packet.C(entry.Style).I(entry.TypeId).I(entry.Num);
        }

        private static byte[] RankPacket(ushort type, ushort subtype, uint score, ushort rank, RankSpec[] entries)
        {
            var packet = new CliVerify.Pkt().H(type).H(subtype).I(score).H(rank).H(entries.Length);
            foreach (RankSpec entry in entries)
                packet.H(entry.Rank).I(entry.ServerId).L(unchecked((long)entry.RoleId)).S(entry.RoleName).I(entry.RoleScore);
            return packet.Bytes();
        }

        private readonly struct ObjectSpec
        {
            public readonly byte Style;
            public readonly uint TypeId;
            public readonly uint Num;
            public ObjectSpec(byte style, uint typeId, uint num) { Style = style; TypeId = typeId; Num = num; }
        }

        private readonly struct StageSpec
        {
            public readonly ushort Id;
            public readonly byte GotType;
            public StageSpec(ushort id, byte gotType) { Id = id; GotType = gotType; }
        }

        private readonly struct RankSpec
        {
            public readonly ushort Rank;
            public readonly uint ServerId;
            public readonly ulong RoleId;
            public readonly string RoleName;
            public readonly uint RoleScore;
            public RankSpec(ushort rank, uint serverId, ulong roleId, string roleName, uint roleScore)
            { Rank = rank; ServerId = serverId; RoleId = roleId; RoleName = roleName; RoleScore = roleScore; }
        }
    }
}
