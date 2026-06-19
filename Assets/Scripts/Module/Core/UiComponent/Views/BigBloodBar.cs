using System.Collections;
using Shenxiao.Generated.UI.UiComponent;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.UiComponent
{
    /// <summary>
    /// 大血条 / BOSS 血条(对标老客户端 uiComponent/BigBloodBar.ts):实血(_img_realy,即时)+
    /// 虚血(_img_virtual,受击后约 0.2s 缓动追上,掉血拖尾)。
    ///
    /// SetValue(curr, max) → 实血即时按比例,虚血缓动收缩(easeIn);加血则直接同步。自包含 UI → 完整可用。
    /// 视觉:两层血条优先 Filled-Horizontal(走 fillAmount),否则 scaleX。
    /// </summary>
    public sealed class BigBloodBar : BigBloodBarBind
    {
        [SerializeField] private float _virtualEaseTime = 0.2f;

        private float _virtualRate = 1f;
        private Coroutine _co;

        public void SetValue(long curr, long max)
        {
            if (max <= 0) max = 1;
            if (curr < 0) curr = 0;
            if (curr > max) curr = max;
            float rate = (float)curr / max;

            SetFill(_img_realy, rate); // 实血即时

            if (_co != null) { StopCoroutine(_co); _co = null; }
            if (rate < _virtualRate)
            {
                _co = StartCoroutine(EaseVirtual(rate)); // 掉血:虚血缓动追
            }
            else
            {
                _virtualRate = rate; // 加血/初始:直接同步
                SetFill(_img_virtual, rate);
            }
        }

        private IEnumerator EaseVirtual(float target)
        {
            float start = _virtualRate;
            float t = 0f;
            while (t < _virtualEaseTime)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / _virtualEaseTime);
                _virtualRate = Mathf.Lerp(start, target, k * k); // easeIn
                SetFill(_img_virtual, _virtualRate);
                yield return null;
            }
            _virtualRate = target;
            SetFill(_img_virtual, target);
            _co = null;
        }

        private void SetFill(Image img, float rate)
        {
            if (img == null) return;
            if (img.type == Image.Type.Filled)
            {
                img.fillAmount = rate;
            }
            else
            {
                Vector3 s = img.rectTransform.localScale;
                s.x = rate;
                img.rectTransform.localScale = s;
            }
        }
    }
}
