using System.Collections;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 复活倒计时窗(对标老客户端 MainUIReliveView.ts):角色战死后弹出,
    /// _lb_left_count 每秒倒数,到点自动关闭并触发复活。
    ///
    /// 降级说明:复活后端(ReliveModel / SceneManager.next_relive_time /
    /// 事件 UPDATE_RELIVE_INFO·RELIVE_BY_TYPE / 服务器时间)尚未移植 → 走老客户端
    /// 「非服务器控制」分支:本地倒计时 GetReliveDuration()=5s,到点 Hide() + 打日志
    /// 「待对接复活协议」。事件驱动弹层,默认关闭、不进 MainUIFlow FirstPass;
    /// 死亡/复活链路移植后再:① 订阅 UPDATE_RELIVE_INFO 刷新;② 由死亡事件 Show(剩余秒);
    /// ③ 倒计时结束 Fire(RELIVE_BY_TYPE)。
    ///
    /// 图(_img_bg/_img_bg2,老客户端 boss/com_up_bg2·up_title_3)与文案(_lb_des/_lb_des2)
    /// 归预制体/用户调,代码不设。
    /// </summary>
    public sealed class MainUIReliveView : MainUIReliveViewBind
    {
        /// <summary>默认本地复活时长(秒),对标老客户端 GetReliveDuration()=5。</summary>
        [SerializeField] private int _defaultReliveSeconds = 5;

        /// <summary>复活类型,对标老客户端 DEFALUT_RELIVE_TYPE=22;触发复活时回传(待协议接入)。</summary>
        [SerializeField] private int _reliveType = 22;

        private Coroutine _countdown;

        protected override void OnInit()
        {
            // TODO 待接:ReliveModel 移植后订阅 UPDATE_RELIVE_INFO → 刷新倒计时。
        }

        /// <summary>args 传 int = 剩余秒数(死亡事件给);缺省走本地 GetReliveDuration。</summary>
        protected override void OnShow(object args)
        {
            int seconds = args is int s && s > 0 ? s : _defaultReliveSeconds;
            StartCountdown(seconds);
        }

        protected override void OnHide()
        {
            StopCountdown();
        }

        protected override void OnDispose()
        {
            StopCountdown();
        }

        private void StartCountdown(int seconds)
        {
            StopCountdown();
            _countdown = StartCoroutine(CountdownRoutine(seconds));
        }

        private void StopCountdown()
        {
            if (_countdown != null)
            {
                StopCoroutine(_countdown);
                _countdown = null;
            }
        }

        private IEnumerator CountdownRoutine(int seconds)
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
            // 老客户端:倒计时结束 → 关闭 + 非服务器控制时 Fire(RELIVE_BY_TYPE, relive_type)。
            GameLog.Info("MainUI", "复活倒计时结束 → 待对接复活协议 RELIVE_BY_TYPE(type={0})", _reliveType);
            Hide();
        }
    }
}
