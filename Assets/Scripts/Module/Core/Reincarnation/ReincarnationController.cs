using System;
using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Reincarnation
{
    /// <summary>16400 天命觉醒基础快照控制器；同号推送只更新模型，不形成回环。</summary>
    public sealed class ReincarnationController : BaseController
    {
        public static readonly ReincarnationController Instance = new ReincarnationController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif
        private ReincarnationController() { }
        protected override void Register()
        {
            RegisterProtocal(Proto.REINCARNATION_AWAKEN_INFO, On16400);
            RegisterProtocal(Proto.REINCARNATION_STAGE_UPDATE, On13041);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, SyncStageFromRoleFigure);
            SyncStageFromRoleFigure();
        }
        public void RequestStartup() => SendEmpty();
        private void SendEmpty()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.REINCARNATION_AWAKEN_INFO, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.REINCARNATION_AWAKEN_INFO);
        }
        private void On16400(NetReader r)
        {
            int count = r.ReadU16(); var ids = new List<uint>(count);
            for (int i = 0; i < count; i++) ids.Add(r.ReadU32());
            ReincarnationModel.Instance.ReplaceData(ids);
        }

        private void On13041(NetReader r)
        {
            if (r.Remaining < 1)
            {
                GameLog.Warn("Reincarnation", "13041 stage update too short remaining={0}", r.Remaining);
                return;
            }

            byte stage = r.ReadU8();
            bool changed = ReincarnationModel.Instance.SetCurrentStage(stage);
            if (RoleModel.Instance.Figure != null)
                RoleModel.Instance.Figure.Raw["turn_stage"] = stage;
            if (changed) EventDispatcher.Emit(GlobalEvent.EVT_ROLE_INFO_UPDATE);
        }

        private void SyncStageFromRoleFigure()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo || role.Figure == null) return;
            if (!role.Figure.Raw.TryGetValue("turn_stage", out object rawStage)) return;
            ReincarnationModel.Instance.SetCurrentStage(Convert.ToByte(rawStage));
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, SyncStageFromRoleFigure);
            ReincarnationModel.Instance.Reset();
            base.Dispose();
        }
    }
}
