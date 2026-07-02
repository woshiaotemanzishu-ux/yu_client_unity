using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.TempleAwaken
{
    /// <summary>
    /// 天命觉醒(神殿觉醒之路)协议控制器(对标老端 TempleAwakenEnterView.ts:220-223 点击发 42900;
    /// 服务端 pt_429 lib_temple_awaken:finish_initial_task → lib_task_api:open_temple_awaken)。
    /// 42900 完成初始任务(C2S 无参),42909 前置完成态推送(服务端主动下发,客户端不轮询)。
    /// 42901+(神殿主界面/章节/奖励)留后,本轮只做开启最小闭环。
    /// </summary>
    public sealed class TempleAwakenController : BaseController
    {
        public static readonly TempleAwakenController Instance = new TempleAwakenController();

        private TempleAwakenController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.TEMPLE_AWAKEN_FINISH_INITIAL, On42900);
            RegisterProtocal(Proto.TEMPLE_AWAKEN_PRE_STATE, On42909);
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
            TipsManager.Toast("觉醒之路已开启");
            TempleAwakenShellView.Close();
            GameLog.Info("TempleAwaken", "42900 ok 觉醒之路已开启");
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
