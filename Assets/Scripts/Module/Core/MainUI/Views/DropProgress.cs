using System.Collections;
using Shenxiao.Generated.UI.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 掉落/采集进度条(对标老客户端 DropProgress.ts):开启后进度条在 left_time 内从最小填到满,
    /// 图标 _img 同步移动,满了自动关闭。老端 Open(end_time) 触发。
    ///
    /// 进度动画本身无后端依赖,直接用协程线性填充(对标 SYTweenLite WIDTH / ANCHORED_POSX 的线性补间),
    /// 故本视图功能完整。降级:触发源(掉落/采集开始 + Status.NowTime 算 left_time)未移植 → 改由
    /// Show(duration:float 秒) 驱动;无 duration 时用 _defaultSeconds。事件驱动弹层(掉落/采集时开),
    /// 默认关闭、不进 FirstPass;触发链路移植后由它 Show(剩余秒)。
    /// </summary>
    public sealed class DropProgress : DropProgressBind
    {
        /// <summary>默认进度时长(秒),无外部 duration 时用。</summary>
        [SerializeField] private float _defaultSeconds = 3f;
        /// <summary>条起始宽度(对标老端 _gp_progress.width=10)。</summary>
        [SerializeField] private float _startWidth = 10f;
        /// <summary>图标移动终点 x(对标老端 ANCHORED_POSX → 320)。</summary>
        [SerializeField] private float _iconEndX = 320f;

        private float _fullWidth;   // 满宽:OnInit 从预制体当前宽缓存
        private float _iconStartX;
        private Coroutine _run;

        protected override void OnInit()
        {
            if (_gp_progress != null) _fullWidth = _gp_progress.sizeDelta.x;
            if (_img != null) _iconStartX = _img.rectTransform.anchoredPosition.x;
        }

        protected override void OnShow(object args)
        {
            float seconds = args is float f && f > 0f ? f : _defaultSeconds;
            StartDrop(seconds);
        }

        protected override void OnHide() => StopRun();
        protected override void OnDispose() => StopRun();

        /// <summary>对标 StartDrop:seconds 内进度条从起始宽填到满 + 图标移动,满了关闭。</summary>
        public void StartDrop(float seconds)
        {
            StopRun();
            _run = StartCoroutine(RunRoutine(Mathf.Max(0.01f, seconds)));
        }

        private IEnumerator RunRoutine(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                float p = t / seconds;
                SetProgressWidth(Mathf.Lerp(_startWidth, _fullWidth, p));
                SetIconX(Mathf.Lerp(_iconStartX, _iconEndX, p));
                t += Time.deltaTime;
                yield return null;
            }
            SetProgressWidth(_fullWidth);
            SetIconX(_iconEndX);
            _run = null;
            Hide();
        }

        private void SetProgressWidth(float w)
        {
            if (_gp_progress == null) return;
            Vector2 s = _gp_progress.sizeDelta;
            s.x = w;
            _gp_progress.sizeDelta = s;
        }

        private void SetIconX(float x)
        {
            if (_img == null) return;
            Vector2 p = _img.rectTransform.anchoredPosition;
            p.x = x;
            _img.rectTransform.anchoredPosition = p;
        }

        private void StopRun()
        {
            if (_run != null) { StopCoroutine(_run); _run = null; }
        }
    }
}
