using Shenxiao.Generated.UI.UiComponent;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.UiComponent
{
    /// <summary>
    /// 通用进度条(对标老客户端 uiComponent/ProgressBar2.ts):底(_img_bg)+ 前景填充(_img_front)+ 文字(_lb_content)。
    ///
    /// SetValue(curr, max) → 前景按比例 + "curr/max" 文字。自包含 UI → 完整可用。
    /// 视觉:前景条优先设 Filled-Horizontal(走 fillAmount);否则按 scaleX(需左轴点)——填充驱动归我,皮肤/轴点归用户。
    /// </summary>
    public sealed class ProgressBar2 : ProgressBar2Bind
    {
        public void SetValue(long curr, long max)
        {
            if (max <= 0) max = 1;
            if (curr < 0) curr = 0;
            if (curr > max) curr = max;
            float rate = (float)curr / max;

            if (_img_front != null)
            {
                if (_img_front.type == Image.Type.Filled)
                {
                    _img_front.fillAmount = rate;
                }
                else
                {
                    Vector3 s = _img_front.rectTransform.localScale;
                    s.x = rate;
                    _img_front.rectTransform.localScale = s;
                }
            }
            if (_lb_content != null) _lb_content.text = curr + "/" + max;
        }
    }
}
