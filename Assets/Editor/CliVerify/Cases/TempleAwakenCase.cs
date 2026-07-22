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
    /// 天命觉醒实证：保留既有 42900/42909 与临时 UI 回归，并覆盖 42901 全量状态树、
    /// GAME_START 双空包、成功重拉、空快照清旧和 Dispose 清理。
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
                MethodInfo on42909 = typeof(TempleAwakenController).GetMethod("On42909", InstanceFlags);
                controller.Init();
                var handlers = typeof(NetManager).GetField("_handlers", StaticFlags)?.GetValue(null) as IDictionary;
                bool seamsOk = outbound != null && on42900 != null && on42901 != null && on42909 != null
                    && handlers != null && handlers.Contains(Proto.TEMPLE_AWAKEN_FINISH_INITIAL)
                    && handlers.Contains(Proto.TEMPLE_AWAKEN_INFO) && handlers.Contains(Proto.TEMPLE_AWAKEN_PRE_STATE)
                    && !handlers.Contains(42902);
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
                    && model.Chapters[1].Subs.Count == 0;

                byte[] emptyPacket = new CliVerify.Pkt().C(0).H(0).Bytes();
                var emptyReader = new NetReader(emptyPacket, 0, emptyPacket.Length);
                on42901.Invoke(controller, new object[] { emptyReader });
                bool emptyOk = emptyReader.Remaining == 0 && frames.Count == 0
                    && model.HasInfo && !model.IsTaskComplete && model.TaskComplete == 0 && model.Chapters.Count == 0;
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
                    && !handlers.Contains(Proto.TEMPLE_AWAKEN_FINISH_INITIAL)
                    && !handlers.Contains(Proto.TEMPLE_AWAKEN_INFO)
                    && !handlers.Contains(Proto.TEMPLE_AWAKEN_PRE_STATE);

                bool pass = seamsOk && startupOk && infoOk && emptyOk && preTaskOk && failNoThrow
                    && failNotOpened && openButtonOk && openedOk && refreshOk && disposeOk;
                Debug.Log("CLIVERIFY templeawaken VERDICT seams=" + seamsOk + " startup=" + startupOk
                    + " info=" + infoOk + " empty=" + emptyOk + " preTask=" + preTaskOk
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
                if (oldHasInfo) model.ReplaceInfo(oldTaskComplete, oldChapters);
                if (wasInitialized) controller.Init();
                stage.Dispose();
                ResManager.EditorPreferFallback = oldFallback;
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
    }
}
