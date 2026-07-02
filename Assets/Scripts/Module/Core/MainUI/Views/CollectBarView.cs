using System;
using System.Collections;
using Shenxiao.Generated.UI.MainUI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 采集进度条(对标老客户端 CollectBarView.ts)。它不在 MainUIFlow 首批打开之列,而是采集事件触发时
    /// 由 <see cref="Scene.CollectController"/> 取到本视图实例后驱动:服务端回 20008 flag=1(START)时
    /// <see cref="BeginCollect"/> 显示并在 collect_time 秒内分 40 步填满进度;满了回调 onComplete
    /// (CollectController 据此向服务端发 20008 flag=2 请求完成)。完成/取消时 <see cref="StopCollect"/> 隐藏。
    ///
    /// 进度表现:老端用 Laya.Sprite mask 自下而上按高度比例显示(DrawMask);本端等价为把 _img_progress
    /// 设为竖直 Filled(下origin)按 fillAmount 填充——无后端依赖的纯表现,逐步对齐老端 40 步节奏。
    /// 海域"修复中 %"文本分支(_txt_progress/_img_num_bg)非任务采集所需,默认隐藏(对标老端 is_sea 才显)。
    /// </summary>
    public sealed class CollectBarView : CollectBarViewBind
    {
        private const int TotalSteps = 40; // 对标老端 CollectBarView.collect_total_step = 40

        private Coroutine _run;
        private Action _onComplete;

        protected override void OnInit()
        {
            // 进度条做成竖直填充(对标老端 DrawMask:自下而上按高度比例)。无论预制体原始 Image 类型如何,
            // 这里强制设为 Filled/Vertical/Bottom,保证表现一致。
            if (_img_progress != null)
            {
                _img_progress.type = Image.Type.Filled;
                _img_progress.fillMethod = Image.FillMethod.Vertical;
                _img_progress.fillOrigin = (int)Image.OriginVertical.Bottom;
                _img_progress.fillAmount = 0f;
            }
            // 海域修复进度文本/底:任务采集不用,默认隐藏(对标老端 _img_num_bg.visible=_txt_progress.visible=is_sea)。
            if (_txt_progress != null) _txt_progress.gameObject.SetActive(false);
            if (_img_num_bg != null) _img_num_bg.gameObject.SetActive(false);
        }

        /// <summary>
        /// 开始采集进度(对标老端 StartCollect):显示进度条,在 <paramref name="seconds"/> 秒内分 40 步填满。
        /// 本视图只管表现;采集"完成请求"由 <see cref="Scene.CollectController"/> 计时驱动(保证缺本视图时也能闭环),
        /// 故 <paramref name="onComplete"/> 默认 null。重复调用以最新一次为准。
        /// </summary>
        public void BeginCollect(float seconds, Action onComplete = null)
        {
            _onComplete = onComplete;
            Show();                       // 置顶 + 激活;首次激活触发 OnInit
            SetProgress(0f);
            StopRun();
            _run = StartCoroutine(RunRoutine(Mathf.Max(0.2f, seconds)));
        }

        /// <summary>停止并隐藏(采集完成/取消/被打断时调用)。</summary>
        public void StopCollect()
        {
            StopRun();
            _onComplete = null;
            Hide();
        }

        private IEnumerator RunRoutine(float seconds)
        {
            float stepTime = seconds / TotalSteps;
            for (int step = 1; step <= TotalSteps; step++)
            {
                float t = 0f;
                while (t < stepTime) { t += Time.deltaTime; yield return null; }
                SetProgress((float)step / TotalSteps);
            }
            _run = null;
            Action cb = _onComplete;
            _onComplete = null;
            cb?.Invoke(); // 进度满 → CollectController 发 20008 flag=2 请求完成
        }

        private void SetProgress(float ratio)
        {
            if (_img_progress != null) _img_progress.fillAmount = Mathf.Clamp01(ratio);
        }

        private void StopRun()
        {
            if (_run != null) { StopCoroutine(_run); _run = null; }
        }

        protected override void OnHide() => StopRun();
        protected override void OnDispose() => StopRun();
    }
}
