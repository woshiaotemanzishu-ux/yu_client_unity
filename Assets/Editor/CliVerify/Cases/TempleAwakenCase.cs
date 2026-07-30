using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Res;
using Shenxiao.Module.Core.TempleAwaken;
using TMPro;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 天命觉醒实证：保留既有 42900/42901/42909 与临时 UI 回归，并覆盖 42902 章节全量、
    /// 42903/04/05 三层增量、精确补查帧、早包/upsert、u64、空快照和环境恢复。
    /// </summary>
    public static class TempleAwakenCase
    {
        private const BindingFlags InstanceFlags = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags StaticFlags = BindingFlags.NonPublic | BindingFlags.Static;

        public static async Task<int> Run()
        {
            bool oldFallback = ResManager.EditorPreferFallback;
            CliVerify.Stage stage = CliVerify.Stage.Create();
            TempleAwakenController controller = TempleAwakenController.Instance;
            TempleAwakenModel model = TempleAwakenModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            bool oldPreTaskFinished = model.PreTaskFinished;
            bool oldOpened = model.Opened;
            bool oldHasInfo = model.HasInfo;
            byte oldTaskComplete = model.TaskComplete;
            List<TempleAwakenModel.ChapterEntry> oldChapters = CloneChapters(model.Chapters);
            var oldLoadedDetails = new HashSet<ushort>();
            foreach (TempleAwakenModel.ChapterEntry chapter in model.Chapters)
                if (model.HasChapterDetail(chapter.Chapter)) oldLoadedDetails.Add(chapter.Chapter);
            bool oldHasChapterStatusDelta = model.HasChapterStatusDelta;
            ushort oldLastChapterStatusChapter = model.LastChapterStatusChapter;
            byte oldLastChapterStatus = model.LastChapterStatus;
            bool oldHasSubStatusDelta = model.HasSubStatusDelta;
            ushort oldLastSubStatusChapter = model.LastSubStatusChapter;
            ushort oldLastSubStatusSubChapter = model.LastSubStatusSubChapter;
            byte oldLastSubStatus = model.LastSubStatus;
            bool oldHasStageProgressDelta = model.HasStageProgressDelta;
            ushort oldLastStageProgressChapter = model.LastStageProgressChapter;
            ushort oldLastStageProgressSubChapter = model.LastStageProgressSubChapter;
            ushort oldLastStageProgressStage = model.LastStageProgressStage;
            ulong oldLastStageProgress = model.LastStageProgress;
            byte oldLastStageProgressStatus = model.LastStageProgressStatus;
            FieldInfo outbound = typeof(TempleAwakenController).GetField("s_outboundIntercept", StaticFlags);
            object oldOutbound = outbound?.GetValue(null);

            try
            {
                await TempleAwakenConfigs.EnsureLoaded();
                if (!TempleAwakenConfigs.IsLoaded)
                {
                    Debug.LogError("CLIVERIFY templeawaken FAIL config_temple_awaken_kv not loaded");
                    return 3;
                }

                MethodInfo on42900 = typeof(TempleAwakenController).GetMethod("On42900", InstanceFlags);
                MethodInfo on42901 = typeof(TempleAwakenController).GetMethod("On42901", InstanceFlags);
                MethodInfo on42902 = typeof(TempleAwakenController).GetMethod("On42902", InstanceFlags);
                MethodInfo on42903 = typeof(TempleAwakenController).GetMethod("On42903", InstanceFlags);
                MethodInfo on42904 = typeof(TempleAwakenController).GetMethod("On42904", InstanceFlags);
                MethodInfo on42905 = typeof(TempleAwakenController).GetMethod("On42905", InstanceFlags);
                MethodInfo on42909 = typeof(TempleAwakenController).GetMethod("On42909", InstanceFlags);
                controller.Init();
                var handlers = typeof(NetManager).GetField("_handlers", StaticFlags)?.GetValue(null) as IDictionary;
                bool seamsOk = outbound != null && on42900 != null && on42901 != null && on42902 != null
                    && on42903 != null && on42904 != null && on42905 != null && on42909 != null
                    && handlers != null && handlers.Contains(Proto.TEMPLE_AWAKEN_FINISH_INITIAL)
                    && handlers.Contains(Proto.TEMPLE_AWAKEN_INFO)
                    && handlers.Contains(Proto.TEMPLE_AWAKEN_CHAPTER_INFO)
                    && handlers.Contains(Proto.TEMPLE_AWAKEN_CHAPTER_STATUS)
                    && handlers.Contains(Proto.TEMPLE_AWAKEN_SUB_STATUS)
                    && handlers.Contains(Proto.TEMPLE_AWAKEN_STAGE_PROGRESS)
                    && handlers.Contains(Proto.TEMPLE_AWAKEN_PRE_STATE)
                    && !handlers.Contains(42906) && !handlers.Contains(42907)
                    && !handlers.Contains(42908) && !handlers.Contains(42910);
                if (!seamsOk)
                {
                    Debug.LogError("CLIVERIFY templeawaken handlers/register missing");
                    return 3;
                }

                model.Clear();
                var frames = new List<byte[]>();
                outbound.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                controller.RequestStartup();
                bool startupOk = frames.Count == 2
                    && Frame(frames[0], Proto.TEMPLE_AWAKEN_INFO)
                    && Frame(frames[1], Proto.TEMPLE_AWAKEN_PRE_STATE);
                frames.Clear();

                byte[] infoPacket = new CliVerify.Pkt().C(1).H(2)
                    .H(1).C(2).C(1).H(2)
                        .H(10).C(3).H(2).H(100).C(4).L(5000000000L).H(101).C(5).L(6)
                        .H(11).C(7).H(0)
                    .H(2).C(8).C(0).H(0)
                    .Bytes();
                var infoReader = new NetReader(infoPacket, 0, infoPacket.Length);
                on42901.Invoke(controller, new object[] { infoReader });
                bool infoOk = infoReader.Remaining == 0 && frames.Count == 0
                    && model.HasInfo && model.IsTaskComplete && model.TaskComplete == 1
                    && model.Chapters.Count == 2
                    && model.Chapters[0].Chapter == 1 && model.Chapters[0].Status == 2 && model.Chapters[0].IsWear == 1
                    && model.Chapters[0].Subs.Count == 2
                    && model.Chapters[0].Subs[0].SubChapter == 10 && model.Chapters[0].Subs[0].Status == 3
                    && model.Chapters[0].Subs[0].Stages.Count == 2
                    && model.Chapters[0].Subs[0].Stages[0].Stage == 100 && model.Chapters[0].Subs[0].Stages[0].Status == 4
                    && model.Chapters[0].Subs[0].Stages[0].Process == 5000000000UL
                    && model.Chapters[0].Subs[0].Stages[1].Stage == 101 && model.Chapters[0].Subs[0].Stages[1].Status == 5
                    && model.Chapters[0].Subs[0].Stages[1].Process == 6UL
                    && model.Chapters[0].Subs[1].SubChapter == 11 && model.Chapters[0].Subs[1].Status == 7
                    && model.Chapters[0].Subs[1].Stages.Count == 0
                    && model.Chapters[1].Chapter == 2 && model.Chapters[1].Status == 8 && model.Chapters[1].IsWear == 0
                    && model.Chapters[1].Subs.Count == 0
                    && model.HasChapterDetail(1) && model.HasChapterDetail(2);

                controller.RequestChapter(1);
                bool chapterRequestOk = frames.Count == 1 && FrameH(frames[0], Proto.TEMPLE_AWAKEN_CHAPTER_INFO, 1);
                frames.Clear();
                byte[] chapterPacket = new CliVerify.Pkt().H(1).C(9).H(2)
                    .H(10).C(8).H(2)
                        .H(100).C(7).L(-1)
                        .H(100).C(6).L(0)
                    .H(10).C(4).H(0)
                    .Bytes();
                Feed(on42902, controller, chapterPacket);
                TempleAwakenModel.ChapterEntry chapterOne = model.Chapters[0];
                bool chapterInfoOk = model.HasInfo && model.HasChapterDetail(1) && model.Chapters.Count == 2
                    && chapterOne.Chapter == 1 && chapterOne.Status == 9 && chapterOne.IsWear == 1
                    && chapterOne.Subs.Count == 2 && chapterOne.Subs[0].SubChapter == 10
                    && chapterOne.Subs[1].SubChapter == 10 && chapterOne.Subs[0].Stages.Count == 2
                    && chapterOne.Subs[0].Stages[0].Stage == 100
                    && chapterOne.Subs[0].Stages[0].Process == ulong.MaxValue
                    && chapterOne.Subs[0].Stages[1].Stage == 100
                    && chapterOne.Subs[0].Stages[1].Process == 0;
                TempleAwakenModel.ChapterEntry preservedChapter = model.Chapters[0];
                controller.RequestChapter(2);
                bool noReplyPreserves = frames.Count == 1
                    && FrameH(frames[0], Proto.TEMPLE_AWAKEN_CHAPTER_INFO, 2)
                    && ReferenceEquals(preservedChapter, model.Chapters[0]);
                frames.Clear();

                Feed(on42903, controller, new CliVerify.Pkt().H(3).C(2).Bytes());
                bool chapterDeltaOk = model.HasChapterStatusDelta && model.LastChapterStatusChapter == 3
                    && model.LastChapterStatus == 2 && model.Chapters.Count == 3
                    && model.Chapters[2].Chapter == 3 && model.Chapters[2].Status == 2
                    && model.Chapters[2].Subs.Count == 0 && frames.Count == 1
                    && FrameH(frames[0], Proto.TEMPLE_AWAKEN_CHAPTER_INFO, 3);
                frames.Clear();
                Feed(on42902, controller, new CliVerify.Pkt().H(3).C(2).H(0).Bytes());
                Feed(on42904, controller, new CliVerify.Pkt().H(3).H(30).C(4).Bytes());
                bool subDeltaOk = model.HasSubStatusDelta && model.LastSubStatusChapter == 3
                    && model.LastSubStatusSubChapter == 30 && model.LastSubStatus == 4
                    && model.Chapters[2].Subs.Count == 1 && model.Chapters[2].Subs[0].SubChapter == 30
                    && model.Chapters[2].Subs[0].Status == 4;
                Feed(on42905, controller, new CliVerify.Pkt().H(3).H(30).H(7).L(-1).C(5).Bytes());
                bool stageDeltaMaxOk = model.HasStageProgressDelta && model.LastStageProgressChapter == 3
                    && model.LastStageProgressSubChapter == 30 && model.LastStageProgressStage == 7
                    && model.LastStageProgress == ulong.MaxValue && model.LastStageProgressStatus == 5
                    && model.Chapters[2].Subs[0].Stages.Count == 1
                    && model.Chapters[2].Subs[0].Stages[0].Process == ulong.MaxValue;
                Feed(on42905, controller, new CliVerify.Pkt().H(3).H(30).H(7).L(0).C(0).Bytes());
                bool stageDeltaReplaceOk = model.Chapters[2].Subs[0].Stages.Count == 1
                    && model.Chapters[2].Subs[0].Stages[0].Stage == 7
                    && model.Chapters[2].Subs[0].Stages[0].Process == 0
                    && model.Chapters[2].Subs[0].Stages[0].Status == 0
                    && model.LastStageProgress == 0 && model.LastStageProgressStatus == 0;
                bool continuationOk = chapterRequestOk && chapterInfoOk && noReplyPreserves
                    && chapterDeltaOk && subDeltaOk && stageDeltaMaxOk && stageDeltaReplaceOk;
                Debug.Log("CLIVERIFY templeawaken continuation request=" + chapterRequestOk
                    + " chapter=" + chapterInfoOk + " noReply=" + noReplyPreserves
                    + " mainDelta=" + chapterDeltaOk + " subDelta=" + subDeltaOk
                    + " stageMax=" + stageDeltaMaxOk + " stageReplace=" + stageDeltaReplaceOk);

                model.Clear();
                Feed(on42905, controller, new CliVerify.Pkt().H(9).H(8).H(7).L(123).C(1).Bytes());
                bool earlyDeltaOk = !model.HasInfo && model.Chapters.Count == 1
                    && model.Chapters[0].Chapter == 9 && model.Chapters[0].Subs.Count == 1
                    && model.Chapters[0].Subs[0].Stages.Count == 1
                    && model.Chapters[0].Subs[0].Stages[0].Process == 123;

                byte[] emptyPacket = new CliVerify.Pkt().C(0).H(0).Bytes();
                var emptyReader = new NetReader(emptyPacket, 0, emptyPacket.Length);
                on42901.Invoke(controller, new object[] { emptyReader });
                bool emptyOk = emptyReader.Remaining == 0 && frames.Count == 0
                    && model.HasInfo && !model.IsTaskComplete && model.TaskComplete == 0 && model.Chapters.Count == 0
                    && !model.HasChapterDetail(9) && model.HasStageProgressDelta;
                Debug.Log("CLIVERIFY templeawaken 42901 info=" + infoOk + " empty=" + emptyOk);

                Feed(on42909, controller, new CliVerify.Pkt().C(1).Bytes());
                bool preTaskOk = model.PreTaskFinished && frames.Count == 0;

                bool failNoThrow = true;
                try { Feed(on42900, controller, new CliVerify.Pkt().I(300).Bytes()); }
                catch (Exception e) { failNoThrow = false; Debug.LogError("CLIVERIFY templeawaken 42900 fail threw: " + e); }
                bool failNotOpened = !model.Opened && frames.Count == 0;

                TempleAwakenShellView.Show();
                stage.ForceCjkFont();
                Canvas.ForceUpdateCanvases();
                string png = stage.Capture("Temp/round17_templeawaken_shell.png");
                Transform openButton = CliVerify.FindDeep(stage.CanvasRoot, "BtnOpen");
                bool openButtonOk = openButton != null && openButton.gameObject.activeInHierarchy;
                Debug.Log("CLIVERIFY templeawaken shell openBtnOk=" + openButtonOk + " shot=" + png);

                frames.Clear();
                Feed(on42900, controller, new CliVerify.Pkt().I(1).Bytes());
                bool openedOk = model.Opened;
                bool refreshOk = frames.Count == 1 && Frame(frames[0], Proto.TEMPLE_AWAKEN_INFO);

                controller.Dispose();
                bool disposeOk = !controller.IsInitialized && !model.PreTaskFinished && !model.Opened
                    && !model.HasInfo && model.TaskComplete == 0 && model.Chapters.Count == 0
                    && !model.HasChapterStatusDelta && !model.HasSubStatusDelta && !model.HasStageProgressDelta
                    && !handlers.Contains(Proto.TEMPLE_AWAKEN_FINISH_INITIAL)
                    && !handlers.Contains(Proto.TEMPLE_AWAKEN_INFO)
                    && !handlers.Contains(Proto.TEMPLE_AWAKEN_CHAPTER_INFO)
                    && !handlers.Contains(Proto.TEMPLE_AWAKEN_CHAPTER_STATUS)
                    && !handlers.Contains(Proto.TEMPLE_AWAKEN_SUB_STATUS)
                    && !handlers.Contains(Proto.TEMPLE_AWAKEN_STAGE_PROGRESS)
                    && !handlers.Contains(Proto.TEMPLE_AWAKEN_PRE_STATE);

                bool pass = seamsOk && startupOk && infoOk && continuationOk && earlyDeltaOk
                    && emptyOk && preTaskOk && failNoThrow
                    && failNotOpened && openButtonOk && openedOk && refreshOk && disposeOk;
                Debug.Log("CLIVERIFY templeawaken VERDICT seams=" + seamsOk + " startup=" + startupOk
                    + " info=" + infoOk + " continuation=" + continuationOk + " early=" + earlyDeltaOk
                    + " empty=" + emptyOk + " preTask=" + preTaskOk
                    + " failNoThrow=" + failNoThrow + " failNotOpened=" + failNotOpened
                    + " openBtn=" + openButtonOk + " opened=" + openedOk + " refresh=" + refreshOk
                    + " dispose=" + disposeOk + " pass=" + pass);
                return pass ? 0 : 3;
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY templeawaken EXCEPTION " + e);
                return 3;
            }
            finally
            {
                if (outbound != null) outbound.SetValue(null, oldOutbound);
                TempleAwakenShellView.Close();
                if (controller.IsInitialized) controller.Dispose();
                model.Clear();
                model.SetPreTaskFinished(oldPreTaskFinished);
                model.SetOpened(oldOpened);
                var chaptersField = typeof(TempleAwakenModel).GetField("_chapters", InstanceFlags);
                var detailsField = typeof(TempleAwakenModel).GetField("_loadedChapterDetails", InstanceFlags);
                var restoredChapters = chaptersField?.GetValue(model) as List<TempleAwakenModel.ChapterEntry>;
                var restoredDetails = detailsField?.GetValue(model) as HashSet<ushort>;
                restoredChapters?.AddRange(CloneChapters(oldChapters));
                restoredDetails?.UnionWith(oldLoadedDetails);
                SetBacking(model, "TaskComplete", oldTaskComplete);
                SetBacking(model, "HasInfo", oldHasInfo);
                SetBacking(model, "HasChapterStatusDelta", oldHasChapterStatusDelta);
                SetBacking(model, "LastChapterStatusChapter", oldLastChapterStatusChapter);
                SetBacking(model, "LastChapterStatus", oldLastChapterStatus);
                SetBacking(model, "HasSubStatusDelta", oldHasSubStatusDelta);
                SetBacking(model, "LastSubStatusChapter", oldLastSubStatusChapter);
                SetBacking(model, "LastSubStatusSubChapter", oldLastSubStatusSubChapter);
                SetBacking(model, "LastSubStatus", oldLastSubStatus);
                SetBacking(model, "HasStageProgressDelta", oldHasStageProgressDelta);
                SetBacking(model, "LastStageProgressChapter", oldLastStageProgressChapter);
                SetBacking(model, "LastStageProgressSubChapter", oldLastStageProgressSubChapter);
                SetBacking(model, "LastStageProgressStage", oldLastStageProgressStage);
                SetBacking(model, "LastStageProgress", oldLastStageProgress);
                SetBacking(model, "LastStageProgressStatus", oldLastStageProgressStatus);
                if (wasInitialized) controller.Init();
                stage.Dispose();
                ResManager.EditorPreferFallback = oldFallback;
                var currentLoadedDetails = new HashSet<ushort>();
                foreach (TempleAwakenModel.ChapterEntry chapter in model.Chapters)
                    if (model.HasChapterDetail(chapter.Chapter)) currentLoadedDetails.Add(chapter.Chapter);
                bool restored = controller.IsInitialized == wasInitialized
                    && (outbound == null || ReferenceEquals(outbound.GetValue(null), oldOutbound))
                    && model.PreTaskFinished == oldPreTaskFinished && model.Opened == oldOpened
                    && model.HasInfo == oldHasInfo && model.TaskComplete == oldTaskComplete
                    && ChaptersEqual(model.Chapters, oldChapters)
                    && currentLoadedDetails.SetEquals(oldLoadedDetails)
                    && model.HasChapterStatusDelta == oldHasChapterStatusDelta
                    && model.LastChapterStatusChapter == oldLastChapterStatusChapter
                    && model.LastChapterStatus == oldLastChapterStatus
                    && model.HasSubStatusDelta == oldHasSubStatusDelta
                    && model.LastSubStatusChapter == oldLastSubStatusChapter
                    && model.LastSubStatusSubChapter == oldLastSubStatusSubChapter
                    && model.LastSubStatus == oldLastSubStatus
                    && model.HasStageProgressDelta == oldHasStageProgressDelta
                    && model.LastStageProgressChapter == oldLastStageProgressChapter
                    && model.LastStageProgressSubChapter == oldLastStageProgressSubChapter
                    && model.LastStageProgressStage == oldLastStageProgressStage
                    && model.LastStageProgress == oldLastStageProgress
                    && model.LastStageProgressStatus == oldLastStageProgressStatus
                    && ResManager.EditorPreferFallback == oldFallback;
                Debug.Log("CLIVERIFY templeawaken restored=" + restored);
                if (!restored) throw new InvalidOperationException("TempleAwakenCase ambient state restore failed");
            }
        }

        private static void Feed(MethodInfo handler, TempleAwakenController controller, byte[] packet)
        {
            var reader = new NetReader(packet, 0, packet.Length);
            handler.Invoke(controller, new object[] { reader });
            if (reader.Remaining != 0) throw new InvalidOperationException("handler did not consume packet");
        }

        private static bool Frame(byte[] frame, int id)
        {
            return frame != null && frame.Length == 6
                && frame[0] == 0 && frame[1] == 6 && frame[2] == 3 && frame[3] == 232
                && frame[4] == (byte)(id >> 8) && frame[5] == (byte)id;
        }

        private static bool FrameH(byte[] frame, int id, ushort value)
        {
            return frame != null && frame.Length == 8
                && frame[0] == 0 && frame[1] == 8 && frame[2] == 3 && frame[3] == 232
                && frame[4] == (byte)(id >> 8) && frame[5] == (byte)id
                && frame[6] == (byte)(value >> 8) && frame[7] == (byte)value;
        }

        private static void SetBacking(TempleAwakenModel model, string property, object value)
        {
            typeof(TempleAwakenModel).GetField("<" + property + ">k__BackingField", InstanceFlags)
                ?.SetValue(model, value);
        }

        private static List<TempleAwakenModel.ChapterEntry> CloneChapters(
            IReadOnlyList<TempleAwakenModel.ChapterEntry> source)
        {
            var chapters = new List<TempleAwakenModel.ChapterEntry>(source.Count);
            foreach (TempleAwakenModel.ChapterEntry chapter in source)
            {
                var subs = new List<TempleAwakenModel.SubChapterEntry>(chapter.Subs.Count);
                foreach (TempleAwakenModel.SubChapterEntry sub in chapter.Subs)
                {
                    var stages = new List<TempleAwakenModel.StageEntry>(sub.Stages.Count);
                    foreach (TempleAwakenModel.StageEntry stage in sub.Stages)
                        stages.Add(new TempleAwakenModel.StageEntry(stage.Stage, stage.Status, stage.Process));
                    subs.Add(new TempleAwakenModel.SubChapterEntry(sub.SubChapter, sub.Status, stages));
                }
                chapters.Add(new TempleAwakenModel.ChapterEntry(chapter.Chapter, chapter.Status, chapter.IsWear, subs));
            }
            return chapters;
        }

        private static bool ChaptersEqual(IReadOnlyList<TempleAwakenModel.ChapterEntry> a,
            IReadOnlyList<TempleAwakenModel.ChapterEntry> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                TempleAwakenModel.ChapterEntry ac = a[i];
                TempleAwakenModel.ChapterEntry bc = b[i];
                if (ac.Chapter != bc.Chapter || ac.Status != bc.Status || ac.IsWear != bc.IsWear
                    || ac.Subs.Count != bc.Subs.Count) return false;
                for (int j = 0; j < ac.Subs.Count; j++)
                {
                    TempleAwakenModel.SubChapterEntry ass = ac.Subs[j];
                    TempleAwakenModel.SubChapterEntry bss = bc.Subs[j];
                    if (ass.SubChapter != bss.SubChapter || ass.Status != bss.Status
                        || ass.Stages.Count != bss.Stages.Count) return false;
                    for (int k = 0; k < ass.Stages.Count; k++)
                    {
                        TempleAwakenModel.StageEntry ast = ass.Stages[k];
                        TempleAwakenModel.StageEntry bst = bss.Stages[k];
                        if (ast.Stage != bst.Stage || ast.Status != bst.Status || ast.Process != bst.Process)
                            return false;
                    }
                }
            }
            return true;
        }
    }
}
