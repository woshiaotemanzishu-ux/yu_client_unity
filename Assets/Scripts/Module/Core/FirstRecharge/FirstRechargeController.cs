using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;

namespace Shenxiao.Module.Core.FirstRecharge
{
    /// <summary>
    /// 首充协议（对标老客户端 FirstRechargeController / yu_server pt_159 子集）：
    ///   15905 首充信息（h + {Open:c, Index:c}×N + ProductId:i + IsNotify:c）；
    ///   15906 领取结果（Errcode:i, Index:c）；15908 是否已购（IsBuy:c）。
    /// 解析落 <see cref="FirstRechargeModel"/> 并发 EVT_FIRST_RECHARGE_UPDATE；面板/红点 UI 待用户验收。
    /// </summary>
    public sealed class FirstRechargeController : BaseController
    {
        public static readonly FirstRechargeController Instance = new FirstRechargeController();
        private FirstRechargeController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.FIRST_RECHARGE_INFO, On15905);
            RegisterProtocal(Proto.FIRST_RECHARGE_CLAIM, On15906);
            RegisterProtocal(Proto.FIRST_RECHARGE_ISBUY, On15908);
        }

        public void RequestInfo() => SendFmt(Proto.FIRST_RECHARGE_INFO);
        public void Claim(int index) => SendFmt(Proto.FIRST_RECHARGE_CLAIM, "c", index);
        public void QueryBuy() => SendFmt(Proto.FIRST_RECHARGE_ISBUY);
        public void RequestStartupState()
        {
            RequestInfo();
            QueryBuy();
        }

        private void On15905(NetReader r)
        {
            int count = r.ReadU16();
            var slots = new List<FirstRechargeModel.Slot>(count);
            for (int i = 0; i < count; i++)
            {
                int open = r.ReadU8();
                int index = r.ReadU8();
                slots.Add(new FirstRechargeModel.Slot(open, index));
            }
            int productId = (int)r.ReadU32();
            int isNotify = r.ReadU8();
            FirstRechargeModel.Instance.SetInfo(slots, productId, isNotify != 0);
            GameLog.Info("FirstRecharge", "15905 首充信息: 档位={0} productId={1}", count, productId);
            RefreshMainUIIcons();
            EventDispatcher.Emit(GlobalEvent.EVT_FIRST_RECHARGE_UPDATE);
        }

        private void On15906(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            int index = r.ReadU8();
            GameLog.Info("FirstRecharge", "15906 领取结果: errcode={0} index={1}", errcode, index);
            if (errcode == 1)
            {
                RequestInfo();
            }
        }

        private void On15908(NetReader r)
        {
            FirstRechargeModel.Instance.IsBuy = r.ReadU8() != 0;
            RefreshMainUIIcons();
            EventDispatcher.Emit(GlobalEvent.EVT_FIRST_RECHARGE_UPDATE);
        }

        private static async void RefreshMainUIIcons()
        {
            FirstRechargeModel model = FirstRechargeModel.Instance;
            string text = model.HasTomorrowReward() && !model.HasClaimableReward() ? "明天可领" : "";
            await ActivityIconManager.Instance.RefreshFirstRechargeIconAsync(model.ShouldShowMainIcon(), text);
        }
    }
}
