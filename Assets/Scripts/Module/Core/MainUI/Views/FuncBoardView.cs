using System.Collections;
using Shenxiao.Generated.UI.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 功能说明气泡(对标老客户端 FuncBoardView.ts):在功能/活动图标旁弹出的提示气泡,显示文案 + 方向指向,
    /// 老端有滑入 + 持续浮动(Shake)补间 + 可选倒计时自动消失。
    ///
    /// 降级:文案配置(Config.config_popup[uid])、定位锚点(图标屏幕坐标 pos)、滑入/浮动补间(SYTweenLite)、
    /// MainUIModel.UPDATE_ACTIVITY_STATE 联动均未移植 → 用最小 DTO 直接填文案 + 方向翻转 content_bg;
    /// 滑入/浮动动画先省(纯 polish);保留倒计时自动消失(is_auto 时)。事件驱动(图标提示时建),默认关闭、
    /// 不进 FirstPass;配置/定位接上后再补 SetData(真实 cfg/pos)+ 滑入浮动。
    /// </summary>
    public sealed class FuncBoardView : FuncBoardViewBind
    {
        public const int DIR_LEFT = 1;   // 指向左
        public const int DIR_RIGHT = 2;  // 指向右

        /// <summary>默认倒计时秒数(对标老端 _time=10)。</summary>
        [SerializeField] private float _defaultSeconds = 10f;

        private Coroutine _timer;

        protected override void OnHide() => StopTimer();
        protected override void OnDispose() => StopTimer();

        /// <summary>填气泡文案 + 方向 + 可选倒计时(对标 SetData + InitImg)。</summary>
        public void SetData(FuncBoardData data)
        {
            if (data == null) return;

            if (_lb_con != null) _lb_con.text = data.Content ?? "";

            // 方向:左指向时 content_bg 水平翻转(对标 InitImg 的 scaleX=-1)。
            if (content_bg != null)
            {
                Vector3 s = content_bg.transform.localScale;
                s.x = data.DirType == DIR_LEFT ? -Mathf.Abs(s.x) : Mathf.Abs(s.x);
                content_bg.transform.localScale = s;
            }

            bool isAuto = data.IsAuto;
            if (_lb_time != null) _lb_time.gameObject.SetActive(isAuto);
            if (isAuto) StartTimer(data.Seconds > 0f ? data.Seconds : _defaultSeconds);
            else StopTimer();
        }

        private void StartTimer(float seconds)
        {
            StopTimer();
            _timer = StartCoroutine(TimerRoutine(Mathf.CeilToInt(seconds)));
        }

        private void StopTimer()
        {
            if (_timer != null) { StopCoroutine(_timer); _timer = null; }
        }

        private IEnumerator TimerRoutine(int seconds)
        {
            int t = seconds;
            while (t > 0)
            {
                if (_lb_time != null) _lb_time.text = t + "s后消失";
                yield return new WaitForSeconds(1f);
                t--;
            }
            _timer = null;
            Hide();  // 对标 onTimer 到点 DeleteMe
        }
    }

    /// <summary>功能气泡最小数据(待 config_popup 配置 + 定位移植后填充)。</summary>
    public sealed class FuncBoardData
    {
        public string Content;
        public int DirType = FuncBoardView.DIR_RIGHT;
        public bool IsAuto;
        public float Seconds;
    }
}
