using System.Collections;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 新手引导箭头(对标老客户端 common/ArrowComponent.ts):指向某 UI 目标,显示引导文案气泡 + 方向箭头
    /// (按方向旋转 + 来回浮动)+ 可选「N 秒后自动继续」倒计时并自动点目标。
    ///
    /// 降级:引导步骤配置(StoryModel / TaskModel.GetNowGuideCfg)、目标元素定位(target_obj 屏幕坐标)、
    /// 自动继续(server time + 自动点目标)均未移植 → 用最小 DTO 填文案 + 按方向转箭头 + 来回浮动;
    /// 定位归预制体/target;自动继续 autoImg/autoLb 先隐藏。事件驱动(引导时建),默认关闭、不进 FirstPass。
    /// 引导/任务系统接上后再补 SetPos(target/cfg) + 自动继续。
    /// </summary>
    public sealed class ArrowComponent : ArrowComponentBind
    {
        // 老端键盘数字方向:2 下 / 4 左 / 8 上 / 6 右。
        public const int DIR_DOWN = 2;
        public const int DIR_LEFT = 4;
        public const int DIR_UP = 8;
        public const int DIR_RIGHT = 6;

        /// <summary>浮动幅度(对标老端 ±10)。</summary>
        [SerializeField] private float _bobDistance = 10f;
        /// <summary>浮动单程时长(秒,对标老端 1s ping-pong)。</summary>
        [SerializeField] private float _bobDuration = 1f;

        private Vector2 _aniBasePos;
        private bool _hasBase;
        private Coroutine _bob;

        protected override void OnInit()
        {
            if (aniGp != null) { _aniBasePos = aniGp.anchoredPosition; _hasBase = true; }
            // 自动继续依赖任务系统,未移植 → 先隐藏。
            if (autoImg != null) autoImg.gameObject.SetActive(false);
        }

        protected override void OnHide() => StopBob();
        protected override void OnDispose() => StopBob();

        /// <summary>填引导文案 + 方向(对标 SetPos+ShowEffect+SetArrowAnimation 的展示部分)。</summary>
        public void SetData(ArrowData data)
        {
            if (data == null) return;
            if (content != null) content.text = data.Content ?? "";
            ShowEffect(data.Direction);
            StartBob(data.Direction);

            if (autoImg != null) autoImg.gameObject.SetActive(false);
            GameLog.Info("MainUI", "引导箭头 dir={0} → 定位/自动继续待对接(StoryModel/TaskModel)", data.Direction);
        }

        /// <summary>对标 ShowEffect:按方向转箭头(下 -90 / 右 180 / 上 90 / 左 0)。</summary>
        private void ShowEffect(int direction)
        {
            if (arrow_effect == null) return;
            float rot = 0f;
            if (direction == DIR_DOWN) rot = -90f;
            else if (direction == DIR_RIGHT) rot = 180f;
            else if (direction == DIR_UP) rot = 90f;
            arrow_effect.localRotation = Quaternion.Euler(0f, 0f, rot);
        }

        /// <summary>对标 SetArrowAnimation:aniGp 沿方向轴 ±_bobDistance 来回浮动。</summary>
        private void StartBob(int direction)
        {
            StopBob();
            if (aniGp == null) return;
            if (!_hasBase) { _aniBasePos = aniGp.anchoredPosition; _hasBase = true; }
            bool horizontal = direction == DIR_LEFT || direction == DIR_RIGHT;
            float sign = (direction == DIR_LEFT || direction == DIR_UP) ? -1f : 1f;
            _bob = StartCoroutine(BobRoutine(horizontal, sign * _bobDistance));
        }

        private IEnumerator BobRoutine(bool horizontal, float amount)
        {
            float t = 0f;
            while (true)
            {
                t += Time.deltaTime / Mathf.Max(0.01f, _bobDuration);
                float ping = Mathf.PingPong(t, 1f);
                Vector2 p = _aniBasePos;
                if (horizontal) p.x += amount * ping;
                else p.y += amount * ping;
                aniGp.anchoredPosition = p;
                yield return null;
            }
        }

        private void StopBob()
        {
            if (_bob != null) { StopCoroutine(_bob); _bob = null; }
            if (aniGp != null && _hasBase) aniGp.anchoredPosition = _aniBasePos;
        }
    }

    /// <summary>引导箭头最小数据(待 StoryModel/TaskModel 引导配置 + target 定位移植后填充)。</summary>
    public sealed class ArrowData
    {
        public string Content;
        public int Direction = ArrowComponent.DIR_DOWN;
    }
}
