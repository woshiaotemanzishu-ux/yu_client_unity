using System;
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
        private Task _bannerTimerTask;
        private Task _refreshTask;
        private int _refreshGeneration;
        private int _notifySentGeneration = -1;
        private bool _hasAppliedIconState;
        private bool _appliedShow;
        private bool _appliedBanner;
        private long _appliedEndTime;
        private string _appliedText;
        private bool _appliedRedDot;
        private bool _lastRoleReady;
        private long _lastRegisterTime = -1;

#if UNITY_EDITOR
        // 仅供 CliVerify 控制配置等待、时钟及外部副作用；生产仍走 MainUI/TimeUtil/NetManager。
        private static Func<Task> s_configLoadOverride;
        private static Func<int, CancellationToken, Task> s_delayOverride;
        private static Func<long> s_nowSecOverride;
        private static Func<bool, bool, long, string, Task> s_refreshIconOverride;
        private static Action<bool> s_redDotOverride;
        private static Action<int> s_notifyOutboundOverride;
        private static Action s_updateEventOverride;
#endif

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
            InvalidateRefreshState();
            SetIconRedDot(false);
            FirstRechargeModel.Instance.Clear();
            _notifySentGeneration = -1;
            ClearAppliedIconState();
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
            _notifySentGeneration = -1;
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
            BuildIconState(out bool show, out bool banner, out long endTime, out string text, out bool redDot);
            bool needsNotify = NeedsBannerFinishedNotification(show, banner, endTime, GetNowSec());
            if (IsAppliedIconState(show, banner, endTime, text, redDot)
                && !needsNotify && (!banner || _bannerTimerTask != null)) return;
            int generation = unchecked(++_refreshGeneration);
            CancelBannerTimer();
            var owner = new TaskCompletionSource<bool>();
            _refreshTask = owner.Task; // runner 可能同步完成，owner 必须先可见。
            _ = RunRefreshAsync(generation, owner);
        }

        private async Task RunRefreshAsync(int generation, TaskCompletionSource<bool> owner)
        {
            try
            {
                await RefreshMainUIIconsAsync(generation);
                owner.TrySetResult(true);
            }
            catch (System.Exception exception)
            {
                GameLog.Error("FirstRecharge", "首充图标刷新失败: " + exception);
                owner.TrySetResult(false);
            }
            finally
            {
                if (ReferenceEquals(_refreshTask, owner.Task) && generation == _refreshGeneration)
                    _refreshTask = null;
            }
        }

        private async Task RefreshMainUIIconsAsync(int generation)
        {
            await EnsureIconConfigLoadedAsync();
            if (!IsCurrentGeneration(generation)) return;

            BuildIconState(out bool show, out bool showBanner, out long bannerEndTime, out string text, out bool redDot);
            long now = GetNowSec();

            if (NeedsBannerFinishedNotification(show, showBanner, bannerEndTime, now))
            {
                if (NotifyBannerFinished(generation)) EmitUpdateEvent();
            }

            SetIconRedDot(redDot);
            await RefreshIconAsync(show, showBanner, bannerEndTime, text);
            if (!IsCurrentGeneration(generation)) return;
            ApplyIconState(show, showBanner, bannerEndTime, text, redDot);

            if (showBanner && bannerEndTime > now) StartBannerTimer(bannerEndTime - now, generation);
        }

        private bool NotifyBannerFinished(int generation)
        {
            FirstRechargeModel model = FirstRechargeModel.Instance;
            if (!IsCurrentGeneration(generation) || model.IsNotify || _notifySentGeneration == generation) return false;
            _notifySentGeneration = generation;
            model.IsNotify = true;
            SendBannerFinished();
            return true;
        }

        private void StartBannerTimer(long remainingSeconds, int generation)
        {
            if (remainingSeconds <= 0 || !IsCurrentGeneration(generation)) return;
            var cts = new CancellationTokenSource();
            var owner = new TaskCompletionSource<bool>();
            _bannerTimerCts = cts;
            _bannerTimerTask = owner.Task; // Delay 同步完成/重入前先建立可见 owner。
            _ = RunBannerTimerAsync(remainingSeconds, generation, cts, owner);
        }

        private async Task RunBannerTimerAsync(long remainingSeconds, int generation, CancellationTokenSource cts, TaskCompletionSource<bool> owner)
        {
            try
            {
                await DelayAsync((int)(remainingSeconds * 1000L), cts.Token);
                if (cts.IsCancellationRequested || !IsCurrentGeneration(generation)) return;
                bool notified = NotifyBannerFinished(generation);
                if (!IsCurrentGeneration(generation)) return;
                if (notified) EmitUpdateEvent(); // 有效到期只在本地状态翻转时通知；重评会生成下一代。
                if (!IsCurrentGeneration(generation)) return;
                RefreshMainUIIcons();
            }
            catch (System.OperationCanceledException)
            {
                // 取消是正常生命周期，不向 owner 泄漏异常。
            }
            catch (System.Exception exception)
            {
                GameLog.Error("FirstRecharge", "首充横幅计时器失败: " + exception);
                owner.TrySetResult(false);
                return;
            }
            finally
            {
                // 旧 timer 即使在取消后才完成，也不得清掉新 owner 的字段。
                if (ReferenceEquals(_bannerTimerCts, cts) && ReferenceEquals(_bannerTimerTask, owner.Task)
                    && generation == _refreshGeneration)
                {
                    _bannerTimerCts = null;
                    _bannerTimerTask = null;
                    cts.Dispose();
                }
                owner.TrySetResult(true);
            }
        }

        private void CancelBannerTimer()
        {
            CancellationTokenSource cts = _bannerTimerCts;
            _bannerTimerCts = null;
            _bannerTimerTask = null;
            if (cts == null) return;
            cts.Cancel();
            cts.Dispose();
        }

        private void InvalidateRefreshState()
        {
            unchecked { _refreshGeneration++; }
            CancelBannerTimer();
            _refreshTask = null;
            ClearAppliedIconState();
        }

        private bool IsCurrentGeneration(int generation) => generation == _refreshGeneration;
        private static bool NeedsBannerFinishedNotification(bool show, bool banner, long endTime, long now)
        {
            FirstRechargeModel model = FirstRechargeModel.Instance;
            return show && !banner && !model.IsNotify
                && (model.IsDoneFirstRecharge() || (endTime > 0 && now >= endTime));
        }
        private static void BuildIconState(out bool show, out bool banner, out long endTime, out string text, out bool redDot)
        {
            FirstRechargeModel model = FirstRechargeModel.Instance;
            RoleModel role = RoleModel.Instance;
            long now = GetNowSec();
            FirstRechargeModel.MainIconPresentation presentation = model.ResolveMainIconPresentation(now, role.HasBaseInfo, role.RegisterTime);
            endTime = model.GetNewRoleBannerEndTime(role.RegisterTime);
            show = presentation != FirstRechargeModel.MainIconPresentation.Hidden;
            banner = presentation == FirstRechargeModel.MainIconPresentation.NewRoleBanner;
            text = model.HasTomorrowReward() && !model.HasClaimableReward() ? "明天可领" : "";
            redDot = model.HasClaimableReward();
        }

        private bool IsAppliedIconState(bool show, bool banner, long endTime, string text, bool redDot) =>
            _hasAppliedIconState && _appliedShow == show && _appliedBanner == banner && _appliedEndTime == endTime
            && _appliedText == text && _appliedRedDot == redDot;

        private void ApplyIconState(bool show, bool banner, long endTime, string text, bool redDot)
        {
            _hasAppliedIconState = true; _appliedShow = show; _appliedBanner = banner; _appliedEndTime = endTime;
            _appliedText = text; _appliedRedDot = redDot;
        }

        private void ClearAppliedIconState()
        {
            _hasAppliedIconState = false; _appliedShow = false; _appliedBanner = false; _appliedEndTime = 0;
            _appliedText = null; _appliedRedDot = false;
        }
        private static long GetNowSec()
        {
#if UNITY_EDITOR
            if (s_nowSecOverride != null) return s_nowSecOverride();
#endif
            return TimeUtil.NowSec();
        }

        private static Task EnsureIconConfigLoadedAsync()
        {
#if UNITY_EDITOR
            if (s_configLoadOverride != null) return s_configLoadOverride();
#endif
            return MainUIConfigs.EnsureLoaded();
        }

        private static Task RefreshIconAsync(bool show, bool banner, long endTime, string text)
        {
#if UNITY_EDITOR
            if (s_refreshIconOverride != null) return s_refreshIconOverride(show, banner, endTime, text);
#endif
            return ActivityIconManager.Instance.RefreshFirstRechargeIconAsync(show, banner, endTime, text);
        }

        private static Task DelayAsync(int milliseconds, CancellationToken token)
        {
#if UNITY_EDITOR
            if (s_delayOverride != null) return s_delayOverride(milliseconds, token);
#endif
            return TimeUtil.Delay(milliseconds, token);
        }

        private static void SetIconRedDot(bool show)
        {
#if UNITY_EDITOR
            if (s_redDotOverride != null) { s_redDotOverride(show); return; }
#endif
            ActivityIconManager.Instance.SetIconRedDot("159", show);
        }

        private void SendBannerFinished()
        {
#if UNITY_EDITOR
            if (s_notifyOutboundOverride != null) { s_notifyOutboundOverride(Proto.FIRST_RECHARGE_NOTIFY); return; }
#endif
            SendFmt(Proto.FIRST_RECHARGE_NOTIFY);
        }

        private static void EmitUpdateEvent()
        {
#if UNITY_EDITOR
            if (s_updateEventOverride != null) { s_updateEventOverride(); return; }
#endif
            EventDispatcher.Emit(GlobalEvent.EVT_FIRST_RECHARGE_UPDATE);
        }
    }
}
