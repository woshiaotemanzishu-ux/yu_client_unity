using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

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

        private CancellationTokenSource _bannerTimerCts;
        private bool _notifySent;
        private bool _lastRoleReady;
        private long _lastRegisterTime = -1;

        protected override void Register()
        {
            RegisterProtocal(Proto.FIRST_RECHARGE_INFO, On15905);
            RegisterProtocal(Proto.FIRST_RECHARGE_CLAIM, On15906);
            RegisterProtocal(Proto.FIRST_RECHARGE_ISBUY, On15908);
            // 对标老端 FirstRechargeController.ts:196-202(dayChange):跨天后若 HasNoRecevie() 才复请求 15905。
            EventDispatcher.On(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            CancelBannerTimer();
            ActivityIconManager.Instance.SetIconRedDot("159", false);
            FirstRechargeModel.Instance.Clear();
            _notifySent = false;
            _lastRoleReady = false;
            _lastRegisterTime = -1;
            base.Dispose();
        }

        // 对标老端 HasNoRecevie()(FirstRechargeModel.ts:106-116):reward_list 任一 open==2("明天可领")才复请求。
        // Unity FirstRechargeModel.HasTomorrowReward() 同判 Slots[i].Open==2,语义等价。
        private void OnServerDayChange()
        {
            if (FirstRechargeModel.Instance.HasTomorrowReward()) RequestInfo();
        }

        public void RequestInfo() => SendFmt(Proto.FIRST_RECHARGE_INFO);
        public void Claim(int index) => SendFmt(Proto.FIRST_RECHARGE_CLAIM, "c", index);
        public void QueryBuy() => SendFmt(Proto.FIRST_RECHARGE_ISBUY);
        public void RequestStartupState()
        {
            _notifySent = false;
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

        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (_lastRoleReady == role.HasBaseInfo && _lastRegisterTime == role.RegisterTime) return;
            _lastRoleReady = role.HasBaseInfo;
            _lastRegisterTime = role.RegisterTime;
            RefreshMainUIIcons();
        }

        private void RefreshMainUIIcons()
        {
            _ = RefreshMainUIIconsAsync();
        }

        private async Task RefreshMainUIIconsAsync()
        {
            FirstRechargeModel model = FirstRechargeModel.Instance;
            RoleModel role = RoleModel.Instance;
            long now = TimeUtil.NowSec();
            FirstRechargeModel.MainIconPresentation presentation =
                model.ResolveMainIconPresentation(now, role.HasBaseInfo, role.RegisterTime);
            long bannerEndTime = model.GetNewRoleBannerEndTime(role.RegisterTime);

            CancelBannerTimer();
            if (presentation == FirstRechargeModel.MainIconPresentation.StandardIcon
                && !model.IsNotify
                && (model.IsDoneFirstRecharge() || (bannerEndTime > 0 && now >= bannerEndTime)))
            {
                NotifyBannerFinished();
            }

            string text = model.HasTomorrowReward() && !model.HasClaimableReward() ? "明天可领" : "";
            ActivityIconManager.Instance.SetIconRedDot("159", model.HasClaimableReward());
            bool show = presentation != FirstRechargeModel.MainIconPresentation.Hidden;
            bool showBanner = presentation == FirstRechargeModel.MainIconPresentation.NewRoleBanner;
            await ActivityIconManager.Instance.RefreshFirstRechargeIconAsync(show, showBanner, bannerEndTime, text);

            if (showBanner && bannerEndTime > now) StartBannerTimer(bannerEndTime - now);
        }

        private void NotifyBannerFinished()
        {
            FirstRechargeModel model = FirstRechargeModel.Instance;
            if (model.IsNotify || _notifySent) return;
            _notifySent = true;
            model.IsNotify = true;
            SendFmt(Proto.FIRST_RECHARGE_NOTIFY);
        }

        private void StartBannerTimer(long remainingSeconds)
        {
            if (remainingSeconds <= 0) return;
            _bannerTimerCts = new CancellationTokenSource();
            _ = WaitForBannerEndAsync(remainingSeconds, _bannerTimerCts.Token);
        }

        private async Task WaitForBannerEndAsync(long remainingSeconds, CancellationToken token)
        {
            try
            {
                await TimeUtil.Delay((int)(remainingSeconds * 1000L), token);
            }
            catch (System.OperationCanceledException)
            {
                return;
            }

            if (token.IsCancellationRequested) return;
            NotifyBannerFinished();
            RefreshMainUIIcons();
            EventDispatcher.Emit(GlobalEvent.EVT_FIRST_RECHARGE_UPDATE);
        }

        private void CancelBannerTimer()
        {
            if (_bannerTimerCts == null) return;
            _bannerTimerCts.Cancel();
            _bannerTimerCts.Dispose();
            _bannerTimerCts = null;
        }
    }
}
