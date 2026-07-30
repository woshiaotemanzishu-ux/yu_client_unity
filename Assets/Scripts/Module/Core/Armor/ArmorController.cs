using System;
using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.Armor
{
    /// <summary>不朽圣骸 14401 全量快照与 14402 权威打造事务。</summary>
    public sealed class ArmorController : BaseController
    {
        public static readonly ArmorController Instance = new ArmorController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif
        private ArmorController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.ARMOR_INFO, On14401);
            RegisterProtocal(Proto.ARMOR_MAKE, On14402);
        }
        public void RequestStartup() => RequestInfo(0, 0);
        public void RequestInfo(byte stage, byte type)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.ARMOR_INFO, "cc", new object[] { stage, type });
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.ARMOR_INFO, "cc", stage, type);
        }

        /// <summary>发送经 UI 配置、快照、等级、前阶和材料二次确认后的单次打造请求；本地绝不预扣或预置状态。</summary>
        public void RequestMake(byte stage, byte type, byte position)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.ARMOR_MAKE, "ccc", new object[] { stage, type, position });
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.ARMOR_MAKE, "ccc", stage, type, position);
        }

        private void On14401(NetReader r)
        {
            ArmorModel.Instance.ReplaceData(ReadStages(r));
            EventDispatcher.Emit(GlobalEvent.EVT_ARMOR_UPDATED);
        }

        private void On14402(NetReader r)
        {
            uint code = r.ReadU32();
            List<ArmorModel.StageEntry> delta = ReadStages(r);
            bool applied = ArmorModel.Instance.ApplyMakeResult(code, delta);
            if (applied) EventDispatcher.Emit(GlobalEvent.EVT_ARMOR_UPDATED);
            EventDispatcher.Emit(GlobalEvent.EVT_ARMOR_MAKE_RESULT, code);
            if (code != 1) TipsManager.Toast("圣骸打造失败(" + code + ")");
        }

        private static List<ArmorModel.StageEntry> ReadStages(NetReader r)
        {
            int stageCount = r.ReadU16();
            var stages = new List<ArmorModel.StageEntry>(stageCount);
            for (int i = 0; i < stageCount; i++)
            {
                byte stage = r.ReadU8();
                int typeCount = r.ReadU16();
                var types = new List<ArmorModel.TypeEntry>(typeCount);
                for (int j = 0; j < typeCount; j++)
                {
                    byte type = r.ReadU8();
                    byte status = r.ReadU8();
                    int positionCount = r.ReadU16();
                    var positions = new List<ArmorModel.PositionEntry>(positionCount);
                    for (int k = 0; k < positionCount; k++) positions.Add(new ArmorModel.PositionEntry(r.ReadU32(), r.ReadU8(), r.ReadU8()));
                    types.Add(new ArmorModel.TypeEntry(type, status, positions));
                }
                stages.Add(new ArmorModel.StageEntry(stage, types));
            }
            return stages;
        }

        public override void Dispose()
        {
            ArmorModel.Instance.Reset();
            base.Dispose();
        }
    }
}
