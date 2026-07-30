using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.TempleAwaken
{
    /// <summary>
    /// 天命觉醒(神殿觉醒之路)协议控制器(对标老端 TempleAwakenEnterView.ts:220-223 点击发 42900;
    /// 服务端 pt_429 lib_temple_awaken:finish_initial_task → lib_task_api:open_temple_awaken)。
    /// 42900 完成初始任务(C2S 无参)，42901 是全量章节/子章/阶段快照，42909 是前置完成态。
    /// 42902 是显式章节全量查询；42903/04/05 是章节、子章、阶段的服务端增量。
    /// GAME_START 空发 42901→42909；领奖、穿戴、场景同步和完整界面仍留后。
    /// </summary>
    public sealed class TempleAwakenController : BaseController
    {
        public static readonly TempleAwakenController Instance = new TempleAwakenController();
#if UNITY_EDITOR
        private static System.Func<byte[], bool> s_outboundIntercept;
#endif

        private TempleAwakenController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.TEMPLE_AWAKEN_FINISH_INITIAL, On42900);
            RegisterProtocal(Proto.TEMPLE_AWAKEN_INFO, On42901);
            RegisterProtocal(Proto.TEMPLE_AWAKEN_CHAPTER_INFO, On42902);
            RegisterProtocal(Proto.TEMPLE_AWAKEN_CHAPTER_STATUS, On42903);
            RegisterProtocal(Proto.TEMPLE_AWAKEN_SUB_STATUS, On42904);
            RegisterProtocal(Proto.TEMPLE_AWAKEN_STAGE_PROGRESS, On42905);
            RegisterProtocal(Proto.TEMPLE_AWAKEN_PRE_STATE, On42909);
        }

        public void RequestStartup()
        {
            SendEmpty(Proto.TEMPLE_AWAKEN_INFO);
            SendEmpty(Proto.TEMPLE_AWAKEN_PRE_STATE);
        }
        private void SendEmpty(int protoId)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(protoId, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(protoId);
        }

        public void RequestChapter(ushort chapter)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.TEMPLE_AWAKEN_CHAPTER_INFO, "h", new object[] { (int)chapter });
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.TEMPLE_AWAKEN_CHAPTER_INFO, "h", (int)chapter);
        }

        public override void Dispose()
        {
            TempleAwakenModel.Instance.Clear();
            base.Dispose();
        }

        /// <summary>发起完成觉醒之路初始任务(对标 TempleAwakenEnterView 点击按钮 → SendFmtToGame(42900));
        /// C2S 无参,结果经 <see cref="On42900"/>。</summary>
        public void FinishInitial()
        {
            SendFmt(Proto.TEMPLE_AWAKEN_FINISH_INITIAL);
            GameLog.Info("TempleAwaken", "request 42900 finish_initial_task");
        }

        /// <summary>42900 回包:error_code:i。==1 成功 → 开启觉醒之路(服务端 open_temple_awaken 推进主线 100590,
        /// 由通用 30001 任务推送自动刷新);否则显码,不造假成功。</summary>
        private void On42900(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            if (errorCode != 1)
            {
                TipsManager.Toast("开启失败(" + errorCode + ")");   // 错误码表未移植,显码降级
                GameLog.Info("TempleAwaken", "42900 fail error_code={0}", errorCode);
                return;
            }
            TempleAwakenModel.Instance.SetOpened(true);
            SendEmpty(Proto.TEMPLE_AWAKEN_INFO);
            TipsManager.Toast("觉醒之路已开启");
            TempleAwakenShellView.Close();
            GameLog.Info("TempleAwaken", "42900 ok 觉醒之路已开启");
            EventDispatcher.Emit(GlobalEvent.EVT_TEMPLE_AWAKEN_UPDATE);
        }

        private void On42901(NetReader r)
        {
            byte taskComplete = r.ReadU8(); int chapterCount = r.ReadU16(); var chapters = new System.Collections.Generic.List<TempleAwakenModel.ChapterEntry>(chapterCount);
            for (int i = 0; i < chapterCount; i++)
            {
                ushort chapter = r.ReadU16(); byte status = r.ReadU8(); byte isWear = r.ReadU8(); int subCount = r.ReadU16(); var subs = new System.Collections.Generic.List<TempleAwakenModel.SubChapterEntry>(subCount);
                for (int j = 0; j < subCount; j++) { ushort sub = r.ReadU16(); byte subStatus = r.ReadU8(); int stageCount = r.ReadU16(); var stages = new System.Collections.Generic.List<TempleAwakenModel.StageEntry>(stageCount); for (int k = 0; k < stageCount; k++) stages.Add(new TempleAwakenModel.StageEntry(r.ReadU16(), r.ReadU8(), unchecked((ulong)r.ReadU64()))); subs.Add(new TempleAwakenModel.SubChapterEntry(sub, subStatus, stages)); }
                chapters.Add(new TempleAwakenModel.ChapterEntry(chapter, status, isWear, subs));
            }
            TempleAwakenModel.Instance.ReplaceInfo(taskComplete, chapters);
            EventDispatcher.Emit(GlobalEvent.EVT_TEMPLE_AWAKEN_UPDATE);
        }

        private void On42902(NetReader r)
        {
            ushort chapter = r.ReadU16();
            byte status = r.ReadU8();
            int subCount = r.ReadU16();
            var subs = new System.Collections.Generic.List<TempleAwakenModel.SubChapterEntry>(subCount);
            for (int i = 0; i < subCount; i++)
            {
                ushort subChapter = r.ReadU16();
                byte subStatus = r.ReadU8();
                int stageCount = r.ReadU16();
                var stages = new System.Collections.Generic.List<TempleAwakenModel.StageEntry>(stageCount);
                for (int j = 0; j < stageCount; j++)
                    stages.Add(new TempleAwakenModel.StageEntry(r.ReadU16(), r.ReadU8(), unchecked((ulong)r.ReadU64())));
                subs.Add(new TempleAwakenModel.SubChapterEntry(subChapter, subStatus, stages));
            }
            TempleAwakenModel.Instance.ReplaceChapterDetail(chapter, status, subs);
            EventDispatcher.Emit(GlobalEvent.EVT_TEMPLE_AWAKEN_UPDATE);
        }

        private void On42903(NetReader r)
        {
            ushort chapter = r.ReadU16();
            byte status = r.ReadU8();
            TempleAwakenModel.Instance.ApplyChapterStatus(chapter, status);
            RequestChapter(chapter);
        }

        private void On42904(NetReader r)
        {
            ushort chapter = r.ReadU16();
            ushort subChapter = r.ReadU16();
            byte status = r.ReadU8();
            TempleAwakenModel.Instance.ApplySubStatus(chapter, subChapter, status);
            EventDispatcher.Emit(GlobalEvent.EVT_TEMPLE_AWAKEN_UPDATE);
        }

        private void On42905(NetReader r)
        {
            ushort chapter = r.ReadU16();
            ushort subChapter = r.ReadU16();
            ushort stage = r.ReadU16();
            ulong process = unchecked((ulong)r.ReadU64());
            byte status = r.ReadU8();
            TempleAwakenModel.Instance.ApplyStageProgress(chapter, subChapter, stage, process, status);
            EventDispatcher.Emit(GlobalEvent.EVT_TEMPLE_AWAKEN_UPDATE);
        }

        /// <summary>42909 前置任务完成态推送:is_finish:c(对标老端 TempleAwakenModel 接收前置完成态)。</summary>
        private void On42909(NetReader r)
        {
            bool finished = r.ReadU8() != 0;
            TempleAwakenModel.Instance.SetPreTaskFinished(finished);
            GameLog.Info("TempleAwaken", "42909 preTaskFinished={0}", finished);
            EventDispatcher.Emit(GlobalEvent.EVT_TEMPLE_AWAKEN_UPDATE);
        }
    }
}
