using System;
using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.MondaysAward
{
    /// <summary>
    /// 周一嘉礼任务、记录和当前奖池快照。仅请求/接收 17904/17905/17908；不复刻老端首次回包后自动请求 17907。
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
            RegisterProtocal(Proto.MONDAYS_AWARD_RECORDS, On17905);
            RegisterProtocal(Proto.MONDAYS_AWARD_POOLS, On17908);
            RegisterProtocal(Proto.MONDAYS_AWARD_DRAW_STATE, On17907);
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

        public void RequestRecords()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.MONDAYS_AWARD_RECORDS, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame))
            {
                return;
            }
#endif
            SendFmt(Proto.MONDAYS_AWARD_RECORDS);
        }

        private void On17905(NetReader reader)
        {
            int count = reader.ReadU16();
            var records = new List<MondaysAwardModel.RecordEntry>(count);
            for (int i = 0; i < count; i++)
            {
                records.Add(new MondaysAwardModel.RecordEntry(
                    reader.ReadU32(), reader.ReadU16(), unchecked((ulong)reader.ReadU64()), reader.ReadString(),
                    reader.ReadU8(), reader.ReadU16(), reader.ReadU32(), reader.ReadString(), reader.ReadU32(),
                    reader.ReadU16(), reader.ReadU16()));
            }

            MondaysAwardModel.Instance.ReplaceRecords(records);
        }

        public void RequestPools()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.MONDAYS_AWARD_POOLS, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame))
            {
                return;
            }
#endif
            SendFmt(Proto.MONDAYS_AWARD_POOLS);
        }

        private void On17908(NetReader reader)
        {
            int count = reader.ReadU16();
            var pools = new List<MondaysAwardModel.PoolEntry>(count);
            for (int i = 0; i < count; i++)
            {
                ushort id = reader.ReadU16();
                int ridCount = reader.ReadU16();
                var rids = new List<ushort>(ridCount);
                for (int j = 0; j < ridCount; j++)
                {
                    rids.Add(reader.ReadU16());
                }

                pools.Add(new MondaysAwardModel.PoolEntry(id, rids));
            }

            MondaysAwardModel.Instance.ReplacePools(pools);
        }

        public void RequestDrawState()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.MONDAYS_AWARD_DRAW_STATE, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.MONDAYS_AWARD_DRAW_STATE);
        }

        private void On17907(NetReader reader)
        {
            MondaysAwardModel.Instance.ReplaceDrawState(reader.ReadU8(), reader.ReadU16());
        }

        public override void Dispose()
        {
            MondaysAwardModel.Instance.Reset();
            base.Dispose();
        }
    }
}
