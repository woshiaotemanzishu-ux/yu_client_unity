using System;
using Shenxiao.Generated.UI.Common;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Shenxiao.Module.Core.Common
{
    /// <summary>
    /// 绿色水平滑条(对标老客户端 common/GreenHSlider.json 皮肤 + common/CHSlider.ts 逻辑):
    /// track 底 + trackHighlight 填充 + thumb 滑块 + value 文本,可拖动 / 可程序设值。
    ///
    /// 自包含 UI(无后端)→ 值逻辑完整可用:Config(min,max,step,value) / SetValue / Value / OnValueChanged;
    /// 拖动 thumb 或轨道改值(钳 + 步进吸附)。视觉(trackHighlight 设 Filled-Horizontal 最佳、thumb 跟随填充边)
    /// 由用户在预制体调,我只驱动数值与跟随逻辑。
    /// </summary>
    public sealed class GreenHSlider : GreenHSliderBind, IPointerDownHandler, IDragHandler
    {
        [SerializeField] public float Minimum = 0f;
        [SerializeField] public float Maximum = 100f;
        [SerializeField] public float SnapInterval = 1f;

        public Action<float> OnValueChanged;

        private float _value;

        public float Value
        {
            get { return _value; }
            set { SetValue(value); }
        }

        protected override void OnInit()
        {
            if (_tpl_GoodsItem != null) _tpl_GoodsItem.SetActive(false);
            Refresh();
        }

        /// <summary>配置区间 + 初值(对标 CHSlider min/max/snapInterval/value)。不触发回调。</summary>
        public void Config(float min, float max, float step, float value)
        {
            Minimum = min;
            Maximum = max;
            SnapInterval = step;
            SetValue(value, false);
        }

        public void SetValue(float v, bool fireCallback = true)
        {
            _value = Clamp(v);
            Refresh();
            if (fireCallback && OnValueChanged != null) OnValueChanged(_value);
        }

        public void SetNumberVisible(bool visible)
        {
            if (value_text != null) value_text.gameObject.SetActive(visible);
            if (value_bg != null) value_bg.gameObject.SetActive(visible);
        }

        public void SetCustomWidth(float width)
        {
            if (width <= 0f) return;
            if (track != null) track.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            if (trackHighlight != null) trackHighlight.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            Refresh();
        }

        private float Clamp(float v)
        {
            if (SnapInterval > 0f)
                v = Minimum + Mathf.Round((v - Minimum) / SnapInterval) * SnapInterval;
            return Mathf.Clamp(v, Minimum, Maximum);
        }

        private float Normalized()
        {
            float range = Maximum - Minimum;
            return range > 0f ? Mathf.Clamp01((_value - Minimum) / range) : 1f;
        }

        private void Refresh()
        {
            float t = Normalized();
            if (value_text != null) value_text.text = Mathf.RoundToInt(_value).ToString();
            if (trackHighlight != null && trackHighlight.type == Image.Type.Filled)
                trackHighlight.fillAmount = t;
            if (thumb != null && track != null)
            {
                RectTransform tr = track.rectTransform;
                float w = tr.rect.width;
                float localX = -w * tr.pivot.x + t * w;
                Vector3 world = tr.TransformPoint(new Vector3(localX, 0f, 0f));
                Vector3 cur = thumb.transform.position;
                thumb.transform.position = new Vector3(world.x, cur.y, cur.z);
            }
        }

        public void OnPointerDown(PointerEventData e) { UpdateFromPointer(e); }
        public void OnDrag(PointerEventData e) { UpdateFromPointer(e); }

        private void UpdateFromPointer(PointerEventData e)
        {
            if (track == null) return;
            RectTransform tr = track.rectTransform;
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(tr, e.position, e.pressEventCamera, out local))
                return;
            float w = tr.rect.width;
            if (w <= 0f) return;
            float t = Mathf.Clamp01((local.x + w * tr.pivot.x) / w);
            SetValue(Minimum + t * (Maximum - Minimum));
        }
    }
}
