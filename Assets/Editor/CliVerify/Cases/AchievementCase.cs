using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Net;
using Shenxiao.Generated.UI.Achv;
using Shenxiao.Module.Core.Achievement;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

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
            var oldCategories = new Dictionary<byte, List<AchievementModel.Entry>>();
            foreach (KeyValuePair<byte, IReadOnlyList<AchievementModel.Entry>> pair in model.CategoryEntries)
                oldCategories[pair.Key] = new List<AchievementModel.Entry>(pair.Value);
            AchievementModel.StageRewardUpdateSnapshot oldStageUpdate = model.LastStageRewardUpdate;
            FieldInfo interceptField = typeof(AchievementController).GetField("s_outboundIntercept", SF);
            object oldIntercept = interceptField?.GetValue(null);
            Action<AchievementModel.OperationResult> captureOperation = null;
            bool pass = false;
            int result = 3;

            try
            {
                controller.Init();
                model.Reset();
                MethodInfo on40901 = typeof(AchievementController).GetMethod("On40901", F);
                MethodInfo on40902 = typeof(AchievementController).GetMethod("On40902", F);
                MethodInfo on40903 = typeof(AchievementController).GetMethod("On40903", F);
                MethodInfo on40904 = typeof(AchievementController).GetMethod("On40904", F);
                MethodInfo on40905 = typeof(AchievementController).GetMethod("On40905", F);
                MethodInfo on40906 = typeof(AchievementController).GetMethod("On40906", F);
                MethodInfo on40907 = typeof(AchievementController).GetMethod("On40907", F);
                MethodInfo on40908 = typeof(AchievementController).GetMethod("On40908", F);
                MethodInfo on40909 = typeof(AchievementController).GetMethod("On40909", F);
                var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
                pass = interceptField != null
                    && on40901 != null && on40902 != null && on40903 != null && on40904 != null
                    && on40905 != null && on40906 != null && on40907 != null && on40908 != null
                    && on40909 != null
                    && handlers != null
                    && handlers.Contains(40901) && handlers.Contains(40902) && handlers.Contains(40903)
                    && handlers.Contains(40904) && handlers.Contains(40905) && handlers.Contains(40906)
                    && handlers.Contains(40907) && handlers.Contains(40908) && handlers.Contains(40909)
                    && !handlers.Contains(40900);

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

                string uiReason;
                Check("ui prefab scroll/tab geometry", ValidateUiPrefab(out uiReason));
                if (!string.IsNullOrEmpty(uiReason))
                    Debug.LogError("CLIVERIFY achievement ui reason=" + uiReason);

                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));
                controller.RequestStartup();
                Check("startup exact frames", Frames(frames, 40901, 40903, 40906, 40908));
                frames.Clear();

                AchievementModel.OperationResult operation = null;
                captureOperation = value => operation = value;
                model.OperationCompleted += captureOperation;
                controller.RequestCategory(17);
                Check("40909 category request", frames.Count == 1
                    && FrameU16(frames[0], 40909, 17));
                frames.Clear();

                bool firstStageSend = controller.RequestStageClaim(2);
                bool duplicateStageSend = controller.RequestStageClaim(3);
                Check("40902 single flight frame", firstStageSend && !duplicateStageSend
                    && controller.IsStageClaimPending && frames.Count == 1
                    && FrameU32(frames[0], 40902, 2));
                frames.Clear();
                NetReader stageFailure = Feed(on40902, new CliVerify.Pkt()
                    .C(1).C(0).I(7001).H(1).Bytes());
                Check("40902 failure authority", stageFailure.Remaining == 0
                    && !controller.IsStageClaimPending && frames.Count == 0
                    && operation != null
                    && operation.Kind == AchievementModel.OperationKind.StageClaim
                    && operation.TargetId == 2 && !operation.Success && operation.ErrorCode == 7001);

                operation = null;
                frames.Clear();
                Check("40902 retry accepted", controller.RequestStageClaim(3)
                    && frames.Count == 1 && FrameU32(frames[0], 40902, 3));
                frames.Clear();
                NetReader stageSuccess = Feed(on40902, new CliVerify.Pkt()
                    .C(2).C(1).I(1).H(3).Bytes());
                Check("40902 success authoritative requery", stageSuccess.Remaining == 0
                    && controller.IsStageClaimPending
                    && operation != null && operation.Success && operation.TargetId == 3
                    && Frames(frames, 40901, 40906)
                    && !controller.RequestStageClaim(4) && frames.Count == 2);
                NetReader staleStageAuthority = Feed(on40901, new CliVerify.Pkt()
                    .C(2).H(0).H(3).Bytes());
                Check("40902 stale snapshot keeps single flight", staleStageAuthority.Remaining == 0
                    && controller.IsStageClaimPending);
                NetReader stageAuthority = Feed(on40907, new CliVerify.Pkt()
                    .H(1).I(3).C(2).C(3).H(3).Bytes());
                Check("40902 unlocks on authoritative snapshot", stageAuthority.Remaining == 0
                    && !controller.IsStageClaimPending);

                operation = null;
                frames.Clear();
                bool firstEntrySend = controller.RequestEntryClaim(101001, 7);
                bool duplicateEntrySend = controller.RequestEntryClaim(101002, 7);
                Check("40905 single flight frame", firstEntrySend && !duplicateEntrySend
                    && controller.IsEntryClaimPending && frames.Count == 1
                    && FrameU32(frames[0], 40905, 101001));
                frames.Clear();
                NetReader entryFailure = Feed(on40905, new CliVerify.Pkt().C(0).I(8002).Bytes());
                Check("40905 failure authority", entryFailure.Remaining == 0
                    && !controller.IsEntryClaimPending && frames.Count == 0
                    && operation != null
                    && operation.Kind == AchievementModel.OperationKind.EntryClaim
                    && operation.TargetId == 101001 && !operation.Success && operation.ErrorCode == 8002);

                operation = null;
                frames.Clear();
                Check("40905 retry accepted", controller.RequestEntryClaim(101002, 7)
                    && frames.Count == 1 && FrameU32(frames[0], 40905, 101002));
                frames.Clear();
                NetReader entrySuccess = Feed(on40905, new CliVerify.Pkt().C(1).I(1).Bytes());
                Check("40905 success authoritative requery", entrySuccess.Remaining == 0
                    && controller.IsEntryClaimPending
                    && operation != null && operation.Success && operation.TargetId == 101002
                    && frames.Count == 5
                    && FramesPrefix(frames, 40901, 40903, 40906, 40908)
                    && FrameU16(frames[4], 40909, 7)
                    && !controller.RequestEntryClaim(101003, 7) && frames.Count == 5);
                model.OperationCompleted -= captureOperation;
                frames.Clear();

                NetReader categoryReader = Feed(on40909, new CliVerify.Pkt()
                    .C(7).H(2)
                    .I(101001).L(12).C(0)
                    .I(101002).L(34).C(2).Bytes());
                Check("40909 category snapshot", categoryReader.Remaining == 0
                    && !controller.IsEntryClaimPending
                    && model.TryGetCategory(7, out IReadOnlyList<AchievementModel.Entry> category)
                    && category.Count == 2
                    && category[0].Category == 7 && category[0].Id == 101001
                    && category[0].Progress == 12 && category[0].Status == 0
                    && category[1].Id == 101002 && category[1].Progress == 34
                    && category[1].Status == 2);

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
                if (captureOperation != null) model.OperationCompleted -= captureOperation;
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
                foreach (KeyValuePair<byte, List<AchievementModel.Entry>> pair in oldCategories)
                    model.ReplaceCategory(pair.Key, pair.Value);
                if (wasInitialized) controller.Init();
                if (interceptField != null) interceptField.SetValue(null, oldIntercept);

                var restoredHandlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
                bool registrationRestored = restoredHandlers != null
                    && restoredHandlers.Contains(40901) == wasInitialized
                    && restoredHandlers.Contains(40902) == wasInitialized
                    && restoredHandlers.Contains(40903) == wasInitialized
                    && restoredHandlers.Contains(40904) == wasInitialized
                    && restoredHandlers.Contains(40905) == wasInitialized
                    && restoredHandlers.Contains(40906) == wasInitialized
                    && restoredHandlers.Contains(40907) == wasInitialized
                    && restoredHandlers.Contains(40908) == wasInitialized
                    && restoredHandlers.Contains(40909) == wasInitialized;
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
                    && SameCategories(model.CategoryEntries, oldCategories)
                    && ReferenceEquals(interceptField?.GetValue(null), oldIntercept);
                Debug.Log("CLIVERIFY achievement restored=" + restored);
                if (!restored) result = 3;
            }

            return result;
        }

        private static bool ValidateUiPrefab(out string reason)
        {
            reason = null;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/UI/Achv/AchvModule.prefab");
            AchvMainViewBind view = prefab != null
                ? prefab.GetComponentInChildren<AchvMainViewBind>(true)
                : null;
            AchvTabBarBind tabBar = view != null && view._tpl_AchvTabBar != null
                ? view._tpl_AchvTabBar.GetComponent<AchvTabBarBind>()
                : null;
            RectTransform tabRect = tabBar != null ? tabBar.transform as RectTransform : null;
            if (tabRect == null
                || tabRect.anchorMin != Vector2.zero
                || tabRect.anchorMax != Vector2.zero
                || tabRect.pivot != new Vector2(0f, 1f)
                || tabRect.anchoredPosition != new Vector2(0f, 220f)
                || tabRect.sizeDelta != new Vector2(680f, 220f))
            {
                reason = "AchvTabBar must preserve old-client bottom=0 geometry";
                return false;
            }

            AchvTabBtnBind topTemplate = view._tpl_AchvTabBtn != null
                ? view._tpl_AchvTabBtn.GetComponent<AchvTabBtnBind>()
                : null;
            RectTransform topRoot = topTemplate != null ? topTemplate.transform as RectTransform : null;
            if (topRoot == null
                || topRoot.anchorMin != new Vector2(0f, 1f)
                || topRoot.anchorMax != new Vector2(0f, 1f)
                || topRoot.pivot != new Vector2(0f, 1f)
                || topRoot.anchoredPosition != Vector2.zero
                || topRoot.sizeDelta != new Vector2(236f, 210f)
                || topTemplate.conta == null
                || topTemplate.conta.anchorMin != Vector2.zero
                || topTemplate.conta.anchorMax != Vector2.zero
                || topTemplate.conta.pivot != new Vector2(0f, 1f)
                || topTemplate.conta.anchoredPosition != new Vector2(0f, 155f)
                || topTemplate.conta.sizeDelta != new Vector2(155f, 155f)
                || topTemplate.tab == null
                || topTemplate.tab.anchorMin != new Vector2(0f, 1f)
                || topTemplate.tab.anchorMax != new Vector2(0f, 1f)
                || topTemplate.tab.pivot != new Vector2(0f, 1f)
                || topTemplate.tab.anchoredPosition != Vector2.zero
                || topTemplate.tab.sizeDelta != new Vector2(155f, 155f)
                || topTemplate.subCon == null
                || topTemplate.subCon.anchorMin != new Vector2(0f, 1f)
                || topTemplate.subCon.anchorMax != new Vector2(0f, 1f)
                || topTemplate.subCon.pivot != new Vector2(0f, 1f)
                || topTemplate.subCon.anchoredPosition != new Vector2(77f, 55f)
                || topTemplate.subCon.sizeDelta != new Vector2(160f, 211f))
            {
                reason = "achievement curved tab root/conta/tab/subCon geometry must preserve old page-space origins";
                return false;
            }
            RectTransform topText = topTemplate != null && topTemplate.tab_txt != null
                ? topTemplate.tab_txt.rectTransform
                : null;
            if (topText == null
                || topText.anchorMin != new Vector2(0.5f, 0.5f)
                || topText.anchorMax != new Vector2(0.5f, 0.5f)
                || topText.pivot != new Vector2(0.5f, 0.5f)
                || topText.anchoredPosition != Vector2.zero
                || topText.sizeDelta != new Vector2(22f, 91f))
            {
                reason = "primary achievement tab text must match old runtime 22x91 centered geometry";
                return false;
            }

            AchvTabSubBtnBind subTemplate = view._tpl_AchvTabSubBtn != null
                ? view._tpl_AchvTabSubBtn.GetComponent<AchvTabSubBtnBind>()
                : null;
            RectTransform subText = subTemplate != null && subTemplate.btn_text != null
                ? subTemplate.btn_text.rectTransform
                : null;
            if (subText == null
                || subText.anchorMin != new Vector2(0.5f, 0.5f)
                || subText.anchorMax != new Vector2(0.5f, 0.5f)
                || subText.pivot != new Vector2(0.5f, 0.5f)
                || subText.anchoredPosition != new Vector2(0f, -4f)
                || subText.sizeDelta != new Vector2(22f, 46f))
            {
                reason = "secondary achievement tab text must match old runtime 22x46 centered geometry";
                return false;
            }

            AchvPropItemBind propTemplate = view._tpl_AchvPropItem != null
                ? view._tpl_AchvPropItem.GetComponent<AchvPropItemBind>()
                : null;
            HorizontalLayoutGroup propLayout = propTemplate != null && propTemplate._Group1 != null
                ? propTemplate._Group1.GetComponent<HorizontalLayoutGroup>()
                : null;
            if (propLayout == null || !propLayout.childControlWidth
                || propLayout.childForceExpandWidth || propLayout.spacing != 10f
                || propLayout.childAlignment != TextAnchor.MiddleLeft)
            {
                reason = "achievement property row must use TMP preferred widths with middle-left alignment";
                return false;
            }

            if (topTemplate.subCon == null || topTemplate.subCon.childCount != 4)
            {
                reason = "achievement curved tab requires four prefab-owned sub-tab slots";
                return false;
            }
            Vector2[] expectedSlotPositions =
            {
                new Vector2(61f, -141f),
                new Vector2(70f, -70f),
                new Vector2(30f, -6f),
                new Vector2(-41f, 11f)
            };
            for (int i = 0; i < expectedSlotPositions.Length; i++)
            {
                RectTransform slot = topTemplate.subCon.GetChild(i) as RectTransform;
                if (slot == null || slot.name != "__SubSlot" + i
                    || slot.anchorMin != new Vector2(0f, 1f)
                    || slot.anchorMax != new Vector2(0f, 1f)
                    || slot.pivot != new Vector2(0f, 1f)
                    || slot.anchoredPosition != expectedSlotPositions[i]
                    || slot.sizeDelta != new Vector2(65f, 70f))
                {
                    reason = "achievement curved sub-tab slot " + i
                        + " must interpret the old coordinates as top-left positions";
                    return false;
                }
            }

            UIEffectProfileCatalog effectCatalog = AssetDatabase.LoadAssetAtPath<UIEffectProfileCatalog>(
                "Assets/Resources/UIEffectProfileCatalog.asset");
            if (!HasClippedRewardFlyProfile(effectCatalog, "ui_bangyu_1")
                || !HasClippedRewardFlyProfile(effectCatalog, "ui_bangyu_2"))
            {
                reason = "reward fly currency effects must restore the old 100x100 private-RT clipping";
                return false;
            }

            ScrollRect overview = view.Content1;
            ScrollRect detail = FindNestedVerticalScroll(view.dispersionGp);
            if (!ValidateVerticalScroll(overview, out reason)) return false;
            if (!ValidateVerticalScroll(detail, out reason)) return false;
            return true;
        }

        private static bool HasClippedRewardFlyProfile(UIEffectProfileCatalog catalog, string effectName)
        {
            if (catalog == null || catalog.profiles == null) return false;
            int matches = 0;
            for (int i = 0; i < catalog.profiles.Count; i++)
            {
                UIEffectProfile profile = catalog.profiles[i];
                if (profile == null || !string.Equals(profile.effectName, effectName,
                        StringComparison.OrdinalIgnoreCase)) continue;
                matches++;
                if (!profile.clipToRenderRect
                    || profile.channel != UIEffectChannelOverride.Top
                    || profile.scaleMultiplier != Vector3.one) return false;
            }
            return matches == 1;
        }

        private static ScrollRect FindNestedVerticalScroll(RectTransform scope)
        {
            if (scope == null) return null;
            ScrollRect[] values = scope.GetComponentsInChildren<ScrollRect>(true);
            for (int i = 0; i < values.Length; i++)
            {
                ScrollRect value = values[i];
                if (value != null && value.enabled && value.vertical && !value.horizontal
                    && value.content != null && value.viewport != null)
                    return value;
            }
            return null;
        }

        private static bool ValidateVerticalScroll(ScrollRect scroll, out string reason)
        {
            reason = null;
            if (scroll == null || !scroll.enabled || !scroll.vertical || scroll.horizontal
                || scroll.movementType != ScrollRect.MovementType.Clamped
                || scroll.viewport == null || scroll.content == null)
            {
                reason = "vertical ScrollRect wiring is incomplete";
                return false;
            }
            if (scroll.viewport.GetComponent<RectMask2D>() == null
                || scroll.content.GetComponent<VerticalLayoutGroup>() == null
                || scroll.content.GetComponent<ContentSizeFitter>() == null)
            {
                reason = "ScrollRect viewport/content structure is incomplete";
                return false;
            }
            Image hitSurface = scroll.viewport.GetComponent<Image>();
            if (hitSurface == null || !hitSurface.raycastTarget || hitSurface.color.a > 0.001f)
            {
                reason = "ScrollRect viewport lacks an invisible raycast surface";
                return false;
            }
            return true;
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

        private static bool FramesPrefix(IReadOnlyList<byte[]> frames, params int[] ids)
        {
            if (frames.Count < ids.Length) return false;
            var prefix = new List<byte[]>(ids.Length);
            for (int i = 0; i < ids.Length; i++) prefix.Add(frames[i]);
            return Frames(prefix, ids);
        }

        private static bool FrameU16(byte[] frame, int id, ushort value)
        {
            return frame != null && frame.Length == 8
                && frame[0] == 0 && frame[1] == 8
                && frame[2] == 3 && frame[3] == 232
                && frame[4] == (byte)(id >> 8) && frame[5] == (byte)id
                && frame[6] == (byte)(value >> 8) && frame[7] == (byte)value;
        }

        private static bool FrameU32(byte[] frame, int id, uint value)
        {
            return frame != null && frame.Length == 10
                && frame[0] == 0 && frame[1] == 10
                && frame[2] == 3 && frame[3] == 232
                && frame[4] == (byte)(id >> 8) && frame[5] == (byte)id
                && frame[6] == (byte)(value >> 24)
                && frame[7] == (byte)(value >> 16)
                && frame[8] == (byte)(value >> 8)
                && frame[9] == (byte)value;
        }

        private static bool SameCategories(
            IReadOnlyDictionary<byte, IReadOnlyList<AchievementModel.Entry>> left,
            IReadOnlyDictionary<byte, List<AchievementModel.Entry>> right)
        {
            if (left.Count != right.Count) return false;
            foreach (KeyValuePair<byte, List<AchievementModel.Entry>> pair in right)
            {
                if (!left.TryGetValue(pair.Key, out IReadOnlyList<AchievementModel.Entry> value)
                    || !SameEntries(value, pair.Value)) return false;
            }
            return true;
        }
    }
}
