using System;
using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.Deposit
{
    public sealed class DepositController : BaseController
    {
        public static readonly DepositController Instance = new DepositController();

#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif

        private DepositController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.DEPOSIT_ACTIVITY_ONHOOK, On19201);
            RegisterProtocal(Proto.DEPOSIT_COINS_PUSH, On19208);
        }

        public void RequestActivityOnhook()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.DEPOSIT_ACTIVITY_ONHOOK, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.DEPOSIT_ACTIVITY_ONHOOK);
        }

        private void On19201(NetReader reader)
        {
            uint dayCoin = reader.ReadU32();
            uint onhookCoin = reader.ReadU32();
            int activityCount = reader.ReadU16();
            var activities = new List<DepositModel.ActivityEntry>(activityCount);
            for (int i = 0; i < activityCount; i++)
            {
                ushort moduleId = reader.ReadU16();
                ushort subModule = reader.ReadU16();
                uint selectTime = reader.ReadU32();
                int behaviourCount = reader.ReadU16();
                var behaviours = new List<DepositModel.BehaviourEntry>(behaviourCount);
                for (int j = 0; j < behaviourCount; j++)
                {
                    behaviours.Add(new DepositModel.BehaviourEntry(reader.ReadU16(), reader.ReadU32(), reader.ReadU16()));
                }
                activities.Add(new DepositModel.ActivityEntry(moduleId, subModule, selectTime, behaviours));
            }
            DepositModel.Instance.Replace(dayCoin, onhookCoin, activities);
        }

        private void On19208(NetReader reader)
        {
            DepositModel.Instance.ReplaceCoins(reader.ReadU32(), reader.ReadU32());
        }

        public override void Dispose()
        {
            DepositModel.Instance.Reset();
            base.Dispose();
        }
    }
}
