using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.CycleimpActlist;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class CycleRankProgressCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;

        private sealed class HandlerState { public bool Exists; public object Value; }

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e) { Debug.LogError("CLIVERIFY cycle-rank-progress EXCEPTION " + e); return Task.FromResult(3); }
        }

        private static int RunSync()
        {
            CycleimpActlistController controller = CycleimpActlistController.Instance;
            CycleimpActlistModel model = CycleimpActlistModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            int oldType = model.Type;
            int oldSubtype = model.Subtype;
            long oldStartTime = model.StartTime;
            long oldEndTime = model.EndTime;
            long oldUponEndTime = model.UponEndTime;
            var oldRanks = CopyRanks(model.NowRankList);
            bool oldHasNotice = model.HasRankProgressNotice;
            CycleimpActlistModel.RankProgressNotice oldNotice = CopyNotice(model.LastRankProgressNotice);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var savedHandlers = new Dictionary<int, HandlerState>();
            int[] ownedProtocolIds = { 22700, 22701, 22702, 22703, 22704, 22705, 22706 };
            foreach (int id in ownedProtocolIds) SaveHandler(handlers, savedHandlers, id);

            bool pass = false;
            bool restored = false;
            try
            {
                if (controller.IsInitialized) controller.Dispose();
                foreach (int id in ownedProtocolIds) handlers?.Remove(id);

                controller.Init();
                model.Reset();
                MethodInfo on22700 = typeof(CycleimpActlistController).GetMethod("On22700", F);
                MethodInfo on22705 = typeof(CycleimpActlistController).GetMethod("On22705", F);
                pass = handlers != null && on22700 != null && on22705 != null
                    && handlers.Contains(22700) && handlers.Contains(22701) && handlers.Contains(22702)
                    && handlers.Contains(22703) && !handlers.Contains(22704)
                    && handlers.Contains(22705) && handlers.Contains(22706)
                    && OnlyExpectedPublicRequest();

                object first22705 = handlers != null && handlers.Contains(22705) ? handlers[22705] : null;
                controller.Init();
                pass &= controller.IsInitialized && first22705 != null
                    && ReferenceEquals(handlers[22705], first22705);

                Feed(on22705, controller, new CliVerify.Pkt()
                    .H(ushort.MaxValue).H(0).I(uint.MaxValue).I(0).I(uint.MaxValue).Bytes());
                CycleimpActlistModel.RankProgressNotice maxSnapshot = model.LastRankProgressNotice;
                pass &= model.HasRankProgressNotice && NoticeMatches(maxSnapshot,
                    ushort.MaxValue, 0, uint.MaxValue, 0, uint.MaxValue);

                model.SetActTime(3, 4, 5, 6, 7);
                model.SetRankList(new[]
                {
                    new CycleimpActlistModel.RankRoleVo
                    {
                        Rank = 1, ServerId = 8, RoleId = 9, RoleName = "甲", RoleScore = 10,
                    },
                });
                Feed(on22705, controller, new CliVerify.Pkt().H(11).H(12).I(13).I(14).I(15).Bytes());
                CycleimpActlistModel.RankProgressNotice nextSnapshot = model.LastRankProgressNotice;
                pass &= NoticeMatches(nextSnapshot, 11, 12, 13, 14, 15)
                    && !ReferenceEquals(nextSnapshot, maxSnapshot)
                    && NoticeMatches(maxSnapshot, ushort.MaxValue, 0, uint.MaxValue, 0, uint.MaxValue)
                    && model.Type == 3 && model.Subtype == 4 && model.StartTime == 5
                    && model.EndTime == 6 && model.UponEndTime == 7
                    && model.NowRankList.Count == 1 && model.NowRankList[0].RoleName == "甲";

                Feed(on22700, controller, new CliVerify.Pkt().H(0).H(0).I(0).I(0).I(0).Bytes());
                pass &= model.Type == 0 && model.Subtype == 0 && model.NowRankList.Count == 0
                    && model.HasRankProgressNotice && ReferenceEquals(model.LastRankProgressNotice, nextSnapshot)
                    && NoticeMatches(nextSnapshot, 11, 12, 13, 14, 15);

                Feed(on22705, controller, new CliVerify.Pkt().H(0).H(0).I(0).I(0).I(0).Bytes());
                pass &= model.HasRankProgressNotice
                    && NoticeMatches(model.LastRankProgressNotice, 0, 0, 0, 0, 0)
                    && NoticeMatches(nextSnapshot, 11, 12, 13, 14, 15);

                controller.Dispose();
                pass &= !controller.IsInitialized && !model.HasRankProgressNotice
                    && model.LastRankProgressNotice == null && model.Type == 0 && model.Subtype == 0
                    && model.NowRankList.Count == 0;
                foreach (int id in ownedProtocolIds) pass &= !handlers.Contains(id);
                Debug.Log("CLIVERIFY cycle-rank-progress VERDICT pass=" + pass);
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                model.SetActTime(oldType, oldSubtype, oldStartTime, oldEndTime, oldUponEndTime);
                model.SetRankList(oldRanks);
                if (oldHasNotice && oldNotice != null)
                {
                    model.ReplaceRankProgressNotice(oldNotice.RankType, oldNotice.RankSubtype,
                        oldNotice.Type, oldNotice.Rank, oldNotice.Value);
                }
                if (wasInitialized) controller.Init();
                foreach (int id in ownedProtocolIds) RestoreHandler(handlers, savedHandlers[id], id);

                restored = ReferenceEquals(CycleimpActlistController.Instance, controller)
                    && ReferenceEquals(CycleimpActlistModel.Instance, model)
                    && controller.IsInitialized == wasInitialized
                    && model.Type == oldType && model.Subtype == oldSubtype
                    && model.StartTime == oldStartTime && model.EndTime == oldEndTime
                    && model.UponEndTime == oldUponEndTime && RanksMatch(model.NowRankList, oldRanks)
                    && model.HasRankProgressNotice == oldHasNotice
                    && NoticesEqual(model.LastRankProgressNotice, oldNotice);
                foreach (int id in ownedProtocolIds) restored &= HandlerMatches(handlers, savedHandlers[id], id);
                Debug.Log("CLIVERIFY cycle-rank-progress restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static void Feed(MethodInfo handler, CycleimpActlistController controller, byte[] bytes)
        {
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            if (reader.Remaining != 0) throw new InvalidOperationException("Unread payload bytes: " + reader.Remaining);
        }

        private static bool OnlyExpectedPublicRequest()
        {
            int count = 0;
            foreach (MethodInfo method in typeof(CycleimpActlistController)
                         .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (method.Name.IndexOf("Request", StringComparison.OrdinalIgnoreCase) < 0
                    && method.Name.IndexOf("Send", StringComparison.OrdinalIgnoreCase) < 0) continue;
                count++;
                if (method.Name != nameof(CycleimpActlistController.RequestStartup)
                    || method.GetParameters().Length != 0) return false;
            }
            return count == 1;
        }

        private static bool NoticeMatches(CycleimpActlistModel.RankProgressNotice notice,
            ushort rankType, ushort rankSubtype, uint type, uint rank, uint value)
        {
            return notice != null && notice.RankType == rankType && notice.RankSubtype == rankSubtype
                && notice.Type == type && notice.Rank == rank && notice.Value == value;
        }

        private static bool NoticesEqual(CycleimpActlistModel.RankProgressNotice a,
            CycleimpActlistModel.RankProgressNotice b)
        {
            if (a == null || b == null) return a == null && b == null;
            return NoticeMatches(a, b.RankType, b.RankSubtype, b.Type, b.Rank, b.Value);
        }

        private static CycleimpActlistModel.RankProgressNotice CopyNotice(
            CycleimpActlistModel.RankProgressNotice notice)
        {
            return notice == null ? null : new CycleimpActlistModel.RankProgressNotice(
                notice.RankType, notice.RankSubtype, notice.Type, notice.Rank, notice.Value);
        }

        private static List<CycleimpActlistModel.RankRoleVo> CopyRanks(
            IReadOnlyList<CycleimpActlistModel.RankRoleVo> ranks)
        {
            var copy = new List<CycleimpActlistModel.RankRoleVo>(ranks.Count);
            for (int i = 0; i < ranks.Count; i++)
            {
                CycleimpActlistModel.RankRoleVo rank = ranks[i];
                copy.Add(new CycleimpActlistModel.RankRoleVo
                {
                    Rank = rank.Rank,
                    ServerId = rank.ServerId,
                    RoleId = rank.RoleId,
                    RoleName = rank.RoleName,
                    RoleScore = rank.RoleScore,
                });
            }
            return copy;
        }

        private static bool RanksMatch(IReadOnlyList<CycleimpActlistModel.RankRoleVo> a,
            IReadOnlyList<CycleimpActlistModel.RankRoleVo> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i].Rank != b[i].Rank || a[i].ServerId != b[i].ServerId
                    || a[i].RoleId != b[i].RoleId || a[i].RoleName != b[i].RoleName
                    || a[i].RoleScore != b[i].RoleScore) return false;
            }
            return true;
        }

        private static void SaveHandler(IDictionary handlers, IDictionary<int, HandlerState> savedHandlers, int id)
        {
            bool exists = handlers != null && handlers.Contains(id);
            savedHandlers[id] = new HandlerState { Exists = exists, Value = exists ? handlers[id] : null };
        }

        private static void RestoreHandler(IDictionary handlers, HandlerState savedHandler, int id)
        {
            if (handlers == null) return;
            if (savedHandler.Exists) handlers[id] = savedHandler.Value;
            else handlers.Remove(id);
        }

        private static bool HandlerMatches(IDictionary handlers, HandlerState savedHandler, int id)
        {
            return handlers != null && handlers.Contains(id) == savedHandler.Exists
                && (!savedHandler.Exists || ReferenceEquals(handlers[id], savedHandler.Value));
        }
    }
}
