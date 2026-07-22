using System;
using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.Armor
{
    /// <summary>不朽圣骸 14401 基础快照控制器；不接 14402，不驱动 UI。</summary>
    public sealed class ArmorController : BaseController
    {
        public static readonly ArmorController Instance = new ArmorController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif
        private ArmorController() { }

        protected override void Register() => RegisterProtocal(Proto.ARMOR_INFO, On14401);
        public void RequestStartup() => RequestInfo(0, 0);
        public void RequestInfo(byte stage, byte type)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.ARMOR_INFO, "cc", new object[] { stage, type });
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.ARMOR_INFO, "cc", stage, type);
        }

        private void On14401(NetReader r)
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
            ArmorModel.Instance.ReplaceData(stages);
        }

        public override void Dispose()
        {
            ArmorModel.Instance.Reset();
            base.Dispose();
        }
    }
}
