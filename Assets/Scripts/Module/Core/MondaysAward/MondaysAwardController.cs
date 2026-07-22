using System;
using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.MondaysAward
{
    /// <summary>
    /// 周一嘉礼任务状态快照。仅请求/接收 17904；不复刻老端首次回包后自动请求 17907。
    /// </summary>
    public sealed class MondaysAwardController : BaseController
    {
        public static readonly MondaysAwardController Instance = new MondaysAwardController();

#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif

        private MondaysAwardController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.MONDAYS_AWARD_TASK_STATE, On17904);
        }

        public void RequestTaskState()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.MONDAYS_AWARD_TASK_STATE, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame))
            {
                return;
            }
#endif
            SendFmt(Proto.MONDAYS_AWARD_TASK_STATE);
        }

        private void On17904(NetReader reader)
        {
            int count = reader.ReadU16();
            var taskStates = new List<MondaysAwardModel.TaskStateEntry>(count);

            for (int i = 0; i < count; i++)
            {
                ushort taskId = reader.ReadU16();
                byte state = reader.ReadU8();
                taskStates.Add(new MondaysAwardModel.TaskStateEntry(taskId, state));
            }

            MondaysAwardModel.Instance.Replace(taskStates);
        }

        public override void Dispose()
        {
            MondaysAwardModel.Instance.Reset();
            base.Dispose();
        }
    }
}
