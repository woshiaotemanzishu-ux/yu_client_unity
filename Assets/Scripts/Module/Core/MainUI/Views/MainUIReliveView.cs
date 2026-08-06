using System.Collections;
using Shenxiao.Common.Audio;
using Shenxiao.Framework.Event;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Relive;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 复活倒计时窗(对标老客户端 MainUIReliveView.ts):角色战死后弹出,
    /// _lb_left_count 每秒倒数;非服控场景到点自动请求复活,服控场景等服务端下发的
    /// 倒计时/后续协议。收到 <see cref="GlobalEvent.EVT_RELIVE_SUCCESS"/> 关窗。
    ///
    /// 复活协议(<see cref="ReliveController"/>/<see cref="ReliveModel"/>)本轮已接入:
    ///   · 非服控(<see cref="ReliveModel.HasReviveInfo"/> 无值或 NextReviveTime&lt;=0):走老端「非服务器控制」
    ///     分支——本地倒计时 GetReliveDuration()=5s,到点调 <see cref="ReliveController.RequestRelive"/>
    ///     (对标 Fire(RELIVE_BY_TYPE, DEFAULT_RELIVE_TYPE)),不再本地直接 Hide,等 EVT_RELIVE_SUCCESS 关窗。
    ///   · 服控(20009 已回 NextReviveTime&gt;0):倒计时改由 <see cref="GlobalEvent.EVT_RELIVE_INFO"/> 驱动,
    ///     用 TimeUtil.NowSec()(GAME_START 已 SyncServerTime)与服务器时间戳算剩余秒;到 0 只 Hide()
    ///     (对标老端服务端控制场景倒计时到点直接 Close(),不自动请求)。
    ///
    /// 图(_img_bg/_img_bg2,老客户端 boss/com_up_bg2·up_title_3)与文案(_lb_des/_lb_des2)
    /// 归预制体/用户调,代码不设。
    /// </summary>
    public sealed class MainUIReliveView : MainUIReliveViewBind
    {
        /// <summary>默认本地复活时长(秒),对标老客户端 GetReliveDuration()=5。</summary>
        [SerializeField] private int _defaultReliveSeconds = 5;

        /// <summary>复活类型,对标老客户端 DEFALUT_RELIVE_TYPE=22;倒计时到点(非服控)回传该值。</summary>
        [SerializeField] private int _reliveType = ReliveModel.DEFAULT_RELIVE_TYPE;

        private Coroutine _countdown;

        protected override void OnInit()
        {
            EventDispatcher.On<long>(GlobalEvent.EVT_RELIVE_INFO, OnReliveInfo);
            EventDispatcher.On<int>(GlobalEvent.EVT_RELIVE_SUCCESS, OnReliveSuccess);
        }

        /// <summary>args 传 int = 剩余秒数覆盖(死亡事件可给);缺省走本地 GetReliveDuration。
        /// 服控场景(ReliveModel 已有 NextReviveTime)忽略 args,改走服务端时间戳驱动。</summary>
        protected override void OnShow(object args)
        {
            _ = AudioManager.PlayFightingVoice(RoleModel.Instance.Sex, 1);
            ReliveModel model = ReliveModel.Instance;
            if (model.HasReviveInfo && model.NextReviveTime > 0)
            {
                StartServerCountdown();
            }
            else
            {
                int seconds = args is int s && s > 0 ? s : _defaultReliveSeconds;
                StartLocalCountdown(seconds);
            }
        }

        protected override void OnHide()
        {
            StopCountdown();
        }

        protected override void OnDispose()
        {
            StopCountdown();
            EventDispatcher.Off<long>(GlobalEvent.EVT_RELIVE_INFO, OnReliveInfo);
            EventDispatcher.Off<int>(GlobalEvent.EVT_RELIVE_SUCCESS, OnReliveSuccess);
        }

        /// <summary>服控场景服务端刷新过一次 next_relive_time(可能在窗口打开期间又推一次 20009)→ 重启倒计时。</summary>
        private void OnReliveInfo(long nextReviveTime)
        {
            if (!IsShown) return;
            StartServerCountdown();
        }

        /// <summary>复活成功 → 关窗(对标老端 Fire(SHOWRELIVEWINDOW,1,type))。</summary>
        private void OnReliveSuccess(int type)
        {
            if (IsShown) Hide();
        }

        private void StartLocalCountdown(int seconds)
        {
            StopCountdown();
            _countdown = StartCoroutine(LocalCountdownRoutine(seconds));
        }

        private void StartServerCountdown()
        {
            StopCountdown();
            if (!RefreshServerLabel()) return;
            _countdown = StartCoroutine(ServerCountdownRoutine());
        }

        private void StopCountdown()
        {
            if (_countdown != null)
            {
                StopCoroutine(_countdown);
                _countdown = null;
            }
        }

        /// <summary>非服控本地倒计时:到点调用 ReliveController.RequestRelive(对标 Fire(RELIVE_BY_TYPE,22)),
        /// 不再本地直接 Hide——等服务端 20004 成功回包触发 EVT_RELIVE_SUCCESS 才关窗(对标老端真实流程)。</summary>
        private IEnumerator LocalCountdownRoutine(int seconds)
        {
            int left = seconds;
            while (left > 0)
            {
                if (_lb_left_count != null) _lb_left_count.text = left.ToString();
                yield return new WaitForSeconds(1f);
                left--;
            }
            if (_lb_left_count != null) _lb_left_count.text = "0";
            _countdown = null;
            GameLog.Info("MainUI", "复活倒计时结束(非服控)→ 请求复活 RequestRelive(type={0})", _reliveType);
            ReliveController.Instance.RequestRelive(_reliveType);
        }

        /// <summary>服控倒计时:每秒用 TimeUtil.NowSec() 与 ReliveModel.NextReviveTime 重算剩余秒(而非本地
        /// 独立递减,防止与服务端时间戳漂移);到 0 只 Hide(对标老端服务端控制场景倒计时到点直接 Close())。</summary>
        private IEnumerator ServerCountdownRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                if (!RefreshServerLabel())
                {
                    _countdown = null;
                    yield break;
                }
            }
        }

        /// <summary>刷新服控倒计时文案;剩余&lt;=0 时关窗并返回 false(供调用方停止协程)。</summary>
        private bool RefreshServerLabel()
        {
            long remain = ReliveModel.Instance.NextReviveTime - TimeUtil.NowSec();
            if (remain <= 0)
            {
                if (_lb_left_count != null) _lb_left_count.text = "0";
                GameLog.Info("MainUI", "复活倒计时结束(服控)→ 关闭窗口(对标老端 Close(),等待服务端后续协议)");
                Hide();
                return false;
            }
            if (_lb_left_count != null) _lb_left_count.text = remain.ToString();
            return true;
        }
    }
}
