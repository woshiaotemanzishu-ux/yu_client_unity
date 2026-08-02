using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Achievement;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class AchievementCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY achievement EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            AchievementController controller = AchievementController.Instance;
            AchievementModel model = AchievementModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            bool hadStage = model.HasStageData;
            bool hadEntries = model.HasEntriesData;
            bool hadEntryUpdate = model.HasEntryUpdateData;
            bool hadStar = model.HasStarData;
            bool hadStageUpdate = model.HasStageRewardUpdateData;
            bool hadTypes = model.HasTypesData;
            byte oldStage = model.CurrentStage;
            ushort oldNewStage = model.NewCurrentStage;
            uint oldStar = model.Star;
            var oldRewards = new List<AchievementModel.Reward>(model.Rewards);
            var oldEntries = new List<AchievementModel.Entry>(model.Entries);
            var oldEntryUpdates = new List<AchievementModel.EntryUpdate>(model.EntryUpdates);
            var oldTypes = new List<AchievementModel.TypeStar>(model.Types);
            AchievementModel.StageRewardUpdateSnapshot oldStageUpdate = model.LastStageRewardUpdate;
            FieldInfo interceptField = typeof(AchievementController).GetField("s_outboundIntercept", SF);
            object oldIntercept = interceptField?.GetValue(null);
            bool pass = false;
            int result = 3;

            try
            {
                controller.Init();
                model.Reset();
                MethodInfo on40901 = typeof(AchievementController).GetMethod("On40901", F);
                MethodInfo on40903 = typeof(AchievementController).GetMethod("On40903", F);
                MethodInfo on40904 = typeof(AchievementController).GetMethod("On40904", F);
                MethodInfo on40906 = typeof(AchievementController).GetMethod("On40906", F);
                MethodInfo on40907 = typeof(AchievementController).GetMethod("On40907", F);
                MethodInfo on40908 = typeof(AchievementController).GetMethod("On40908", F);
                var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
                pass = interceptField != null
                    && on40901 != null && on40903 != null && on40904 != null
                    && on40906 != null && on40907 != null && on40908 != null
                    && handlers != null
                    && handlers.Contains(40901) && handlers.Contains(40903) && handlers.Contains(40904)
                    && handlers.Contains(40906) && handlers.Contains(40907) && handlers.Contains(40908)
                    && !handlers.Contains(40900) && !handlers.Contains(40902)
                    && !handlers.Contains(40905) && !handlers.Contains(40909);

                void Check(string tag, bool value)
                {
                    Debug.Log("CLIVERIFY achievement " + tag + " ok=" + value);
                    if (!value) pass = false;
                }

                NetReader Feed(MethodInfo method, byte[] bytes)
                {
                    var reader = new NetReader(bytes, 0, bytes.Length);
                    method.Invoke(controller, new object[] { reader });
                    return reader;
                }

                Check("seams/register", pass);
                if (!pass) throw new InvalidOperationException("Achievement verification seams are incomplete.");

                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));
                controller.RequestStartup();
                Check("startup exact frames", Frames(frames, 40901, 40903, 40906, 40908));
                frames.Clear();

                NetReader preEntry = Feed(on40904, new CliVerify.Pkt()
                    .H(1).I(77).C(1).L(88).Bytes());
                NetReader preStage = Feed(on40907, new CliVerify.Pkt()
                    .H(1).I(9).C(2).C(5).H(6).Bytes());
                Check("pre-authority raw only", preEntry.Remaining == 0 && preStage.Remaining == 0
                    && model.HasEntryUpdateData && model.EntryUpdates.Count == 1
                    && model.EntryUpdates[0].Id == 77 && model.EntryUpdates[0].Status == 1
                    && model.EntryUpdates[0].Progress == 88
                    && model.HasStageRewardUpdateData && model.LastStageRewardUpdate != null
                    && model.LastStageRewardUpdate.Rewards.Count == 1
                    && model.LastStageRewardUpdate.Rewards[0].NeedStar == 9
                    && model.LastStageRewardUpdate.CurrentStage == 5
                    && model.LastStageRewardUpdate.NewCurrentStage == 6
                    && !model.HasEntriesData && model.Entries.Count == 0
                    && !model.HasStageData && model.Rewards.Count == 0
                    && model.CurrentStage == 0 && model.NewCurrentStage == 0
                    && frames.Count == 0);

                NetReader stageReader = Feed(on40901, new CliVerify.Pkt()
                    .C(255).H(2)
                    .I(4000000000L).C(255)
                    .I(2).C(0)
                    .H(65535).Bytes());
                NetReader entriesReader = Feed(on40903, new CliVerify.Pkt()
                    .H(2)
                    .C(255).I(4000000000L).L(5000000000L).C(255)
                    .C(0).I(2).L(3).C(0).Bytes());
                NetReader starReader = Feed(on40906, new CliVerify.Pkt().I(4000000000L).Bytes());
                NetReader typesReader = Feed(on40908, new CliVerify.Pkt()
                    .H(2)
                    .H(65535).I(4000000000L).I(3)
                    .H(1).I(4).I(5).Bytes());
                Check("stage fields/order", stageReader.Remaining == 0
                    && model.HasStageData && model.CurrentStage == 255 && model.NewCurrentStage == 65535
                    && model.Rewards.Count == 2
                    && model.Rewards[0].NeedStar == 4000000000U && model.Rewards[0].Status == 255
                    && model.Rewards[1].NeedStar == 2 && model.Rewards[1].Status == 0);
                Check("entries fields/order", entriesReader.Remaining == 0
                    && model.HasEntriesData && model.Entries.Count == 2
                    && model.Entries[0].Category == 255 && model.Entries[0].Id == 4000000000U
                    && model.Entries[0].Progress == 5000000000UL && model.Entries[0].Status == 255
                    && model.Entries[1].Category == 0 && model.Entries[1].Id == 2
                    && model.Entries[1].Progress == 3 && model.Entries[1].Status == 0);
                Check("star fields", starReader.Remaining == 0
                    && model.HasStarData && model.Star == 4000000000U);
                Check("types fields/order", typesReader.Remaining == 0
                    && model.HasTypesData && model.Types.Count == 2
                    && model.Types[0].Type == 65535 && model.Types[0].TotalStar == 4000000000U
                    && model.Types[0].NowStar == 3
                    && model.Types[1].Type == 1 && model.Types[1].TotalStar == 4
                    && model.Types[1].NowStar == 5
                    && model.HasAllStartupData && frames.Count == 0);

                NetReader entryUpdateReader = Feed(on40904, new CliVerify.Pkt()
                    .H(3)
                    .I(4000000000L).C(1).L(-1)
                    .I(99).C(2).L(8)
                    .I(4000000000L).C(2).L(10).Bytes());
                IReadOnlyList<AchievementModel.EntryUpdate> capturedEntryPacket = model.EntryUpdates;
                Check("40904 raw and loaded-only merge", entryUpdateReader.Remaining == 0
                    && capturedEntryPacket.Count == 3
                    && capturedEntryPacket[0].Id == 4000000000U
                    && capturedEntryPacket[0].Status == 1
                    && capturedEntryPacket[0].Progress == ulong.MaxValue
                    && capturedEntryPacket[1].Id == 99 && capturedEntryPacket[1].Progress == 8
                    && capturedEntryPacket[2].Id == 4000000000U
                    && capturedEntryPacket[2].Status == 2 && capturedEntryPacket[2].Progress == 10
                    && model.Entries.Count == 2
                    && model.Entries[0].Category == 255 && model.Entries[0].Id == 4000000000U
                    && model.Entries[0].Status == 2 && model.Entries[0].Progress == 10
                    && model.Entries[1].Id == 2 && model.Entries[1].Status == 0
                    && model.Entries[1].Progress == 3 && frames.Count == 0);

                NetReader stageUpdateReader = Feed(on40907, new CliVerify.Pkt()
                    .H(3)
                    .I(4000000000L).C(1)
                    .I(9).C(2)
                    .I(4000000000L).C(2)
                    .C(7).H(65535).Bytes());
                AchievementModel.StageRewardUpdateSnapshot capturedStagePacket = model.LastStageRewardUpdate;
                Check("40907 raw and ordered merge", stageUpdateReader.Remaining == 0
                    && capturedStagePacket != null && capturedStagePacket.Rewards.Count == 3
                    && capturedStagePacket.Rewards[0].NeedStar == 4000000000U
                    && capturedStagePacket.Rewards[0].Status == 1
                    && capturedStagePacket.Rewards[1].NeedStar == 9
                    && capturedStagePacket.Rewards[2].NeedStar == 4000000000U
                    && capturedStagePacket.Rewards[2].Status == 2
                    && capturedStagePacket.CurrentStage == 7
                    && capturedStagePacket.NewCurrentStage == 65535
                    && model.CurrentStage == 7 && model.NewCurrentStage == 65535
                    && model.Rewards.Count == 3
                    && model.Rewards[0].NeedStar == 4000000000U && model.Rewards[0].Status == 2
                    && model.Rewards[1].NeedStar == 2 && model.Rewards[1].Status == 0
                    && model.Rewards[2].NeedStar == 9 && model.Rewards[2].Status == 2
                    && frames.Count == 0);

                NetReader emptyEntryUpdate = Feed(on40904, new CliVerify.Pkt().H(0).Bytes());
                NetReader emptyStageUpdate = Feed(on40907, new CliVerify.Pkt().H(0).C(0).H(0).Bytes());
                Check("empty delta and prior raw immutability", emptyEntryUpdate.Remaining == 0
                    && emptyStageUpdate.Remaining == 0
                    && model.EntryUpdates.Count == 0
                    && model.LastStageRewardUpdate != null
                    && model.LastStageRewardUpdate.Rewards.Count == 0
                    && capturedEntryPacket.Count == 3
                    && capturedEntryPacket[0].Progress == ulong.MaxValue
                    && capturedStagePacket.Rewards.Count == 3
                    && capturedStagePacket.CurrentStage == 7
                    && model.Entries.Count == 2 && model.Entries[0].Progress == 10
                    && model.Rewards.Count == 3
                    && model.CurrentStage == 0 && model.NewCurrentStage == 0
                    && frames.Count == 0);

                NetReader emptyStage = Feed(on40901, new CliVerify.Pkt().C(1).H(0).H(2).Bytes());
                NetReader emptyEntries = Feed(on40903, new CliVerify.Pkt().H(0).Bytes());
                NetReader newStar = Feed(on40906, new CliVerify.Pkt().I(7).Bytes());
                NetReader emptyTypes = Feed(on40908, new CliVerify.Pkt().H(0).Bytes());
                Check("authoritative empty replacement isolated", emptyStage.Remaining == 0
                    && emptyEntries.Remaining == 0 && newStar.Remaining == 0 && emptyTypes.Remaining == 0
                    && model.HasAllStartupData
                    && model.CurrentStage == 1 && model.NewCurrentStage == 2
                    && model.Rewards.Count == 0 && model.Entries.Count == 0
                    && model.Star == 7 && model.Types.Count == 0
                    && model.HasEntryUpdateData && model.EntryUpdates.Count == 0
                    && model.HasStageRewardUpdateData && model.LastStageRewardUpdate != null
                    && model.LastStageRewardUpdate.Rewards.Count == 0
                    && frames.Count == 0);

                controller.Dispose();
                Check("dispose reset", !controller.IsInitialized
                    && !model.HasStageData && !model.HasEntriesData && !model.HasEntryUpdateData
                    && !model.HasStarData && !model.HasStageRewardUpdateData && !model.HasTypesData
                    && model.Rewards.Count == 0 && model.Entries.Count == 0
                    && model.EntryUpdates.Count == 0 && model.Types.Count == 0
                    && model.LastStageRewardUpdate == null && model.Star == 0);
                Debug.Log("CLIVERIFY achievement VERDICT pass=" + pass);
                result = pass ? 0 : 3;
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                if (hadEntryUpdate) model.ApplyEntryUpdates(oldEntryUpdates);
                if (hadStageUpdate && oldStageUpdate != null)
                {
                    model.ApplyStageRewardUpdate(
                        new List<AchievementModel.Reward>(oldStageUpdate.Rewards),
                        oldStageUpdate.CurrentStage,
                        oldStageUpdate.NewCurrentStage);
                }
                if (hadStage) model.ReplaceStage(oldStage, oldRewards, oldNewStage);
                if (hadEntries) model.ReplaceEntries(oldEntries);
                if (hadStar) model.ReplaceStar(oldStar);
                if (hadTypes) model.ReplaceTypes(oldTypes);
                if (wasInitialized) controller.Init();
                if (interceptField != null) interceptField.SetValue(null, oldIntercept);

                var restoredHandlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
                bool registrationRestored = restoredHandlers != null
                    && restoredHandlers.Contains(40901) == wasInitialized
                    && restoredHandlers.Contains(40903) == wasInitialized
                    && restoredHandlers.Contains(40904) == wasInitialized
                    && restoredHandlers.Contains(40906) == wasInitialized
                    && restoredHandlers.Contains(40907) == wasInitialized
                    && restoredHandlers.Contains(40908) == wasInitialized;
                bool restored = controller.IsInitialized == wasInitialized
                    && registrationRestored
                    && model.HasStageData == hadStage
                    && model.HasEntriesData == hadEntries
                    && model.HasEntryUpdateData == hadEntryUpdate
                    && model.HasStarData == hadStar
                    && model.HasStageRewardUpdateData == hadStageUpdate
                    && model.HasTypesData == hadTypes
                    && model.CurrentStage == oldStage
                    && model.NewCurrentStage == oldNewStage
                    && model.Star == oldStar
                    && SameRewards(model.Rewards, oldRewards)
                    && SameEntries(model.Entries, oldEntries)
                    && SameEntryUpdates(model.EntryUpdates, oldEntryUpdates)
                    && SameStageUpdate(model.LastStageRewardUpdate, oldStageUpdate)
                    && SameTypes(model.Types, oldTypes)
                    && ReferenceEquals(interceptField?.GetValue(null), oldIntercept);
                Debug.Log("CLIVERIFY achievement restored=" + restored);
                if (!restored) result = 3;
            }

            return result;
        }

        private static bool SameRewards(IReadOnlyList<AchievementModel.Reward> left, IReadOnlyList<AchievementModel.Reward> right)
        {
            if (left.Count != right.Count) return false;
            for (int i = 0; i < left.Count; i++)
            {
                if (left[i].NeedStar != right[i].NeedStar || left[i].Status != right[i].Status) return false;
            }
            return true;
        }

        private static bool SameEntries(IReadOnlyList<AchievementModel.Entry> left, IReadOnlyList<AchievementModel.Entry> right)
        {
            if (left.Count != right.Count) return false;
            for (int i = 0; i < left.Count; i++)
            {
                if (left[i].Category != right[i].Category || left[i].Id != right[i].Id
                    || left[i].Progress != right[i].Progress || left[i].Status != right[i].Status) return false;
            }
            return true;
        }

        private static bool SameEntryUpdates(IReadOnlyList<AchievementModel.EntryUpdate> left, IReadOnlyList<AchievementModel.EntryUpdate> right)
        {
            if (left.Count != right.Count) return false;
            for (int i = 0; i < left.Count; i++)
            {
                if (left[i].Id != right[i].Id || left[i].Status != right[i].Status
                    || left[i].Progress != right[i].Progress) return false;
            }
            return true;
        }

        private static bool SameStageUpdate(
            AchievementModel.StageRewardUpdateSnapshot left,
            AchievementModel.StageRewardUpdateSnapshot right)
        {
            if (left == null || right == null) return left == null && right == null;
            return left.CurrentStage == right.CurrentStage
                && left.NewCurrentStage == right.NewCurrentStage
                && SameRewards(left.Rewards, right.Rewards);
        }

        private static bool SameTypes(IReadOnlyList<AchievementModel.TypeStar> left, IReadOnlyList<AchievementModel.TypeStar> right)
        {
            if (left.Count != right.Count) return false;
            for (int i = 0; i < left.Count; i++)
            {
                if (left[i].Type != right[i].Type || left[i].TotalStar != right[i].TotalStar
                    || left[i].NowStar != right[i].NowStar) return false;
            }
            return true;
        }

        private static bool Frames(IReadOnlyList<byte[]> frames, params int[] ids)
        {
            if (frames.Count != ids.Length) return false;
            for (int i = 0; i < ids.Length; i++)
            {
                byte[] frame = frames[i];
                if (frame == null || frame.Length != 6
                    || frame[0] != 0 || frame[1] != 6
                    || frame[2] != 3 || frame[3] != 232
                    || frame[4] != (byte)(ids[i] >> 8)
                    || frame[5] != (byte)(ids[i] & 0xFF))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
