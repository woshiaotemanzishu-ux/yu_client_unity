using System;
using Shenxiao.Generated.UI.Common;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Common
{
    /// <summary>
    /// 带加减按钮的滑条(对标老客户端 common/WithBtnHSlider.ts):slider_con 内放一个 GreenHSlider,
    /// reduce/increase 按钮 ∓/± step,到边界不再动(Laya 里弹"已到最小/最大")。
    ///
    /// SetData(default,step,min,max,callback)。自包含 UI → 完整可用(加减+拖动都改值并回调)。
    /// </summary>
    public sealed class WithBtnHSlider : WithBtnHSliderBind
    {
        [SerializeField] private float _step = 1f;

        private GreenHSlider _slider;
        private Action<float> _callback;

        protected override void OnInit()
        {
            if (_tpl_GreenHSlider != null) _tpl_GreenHSlider.SetActive(false);
            BuildSlider();
            BindBtn(reduce_btn, () => Step(-1));
            BindBtn(increase_btn, () => Step(1));
        }

        private void BuildSlider()
        {
            if (_tpl_GreenHSlider == null || slider_con == null) return;
            GameObject go = Instantiate(_tpl_GreenHSlider, slider_con);
            go.SetActive(true);
            _slider = go.GetComponent<GreenHSlider>();
            if (_slider != null)
            {
                _slider.Show();
                _slider.OnValueChanged = v => { if (_callback != null) _callback(v); };
            }
        }

        /// <summary>配置(对标 SetData):初值 + 步进 + 区间 + 值变回调。</summary>
        public void SetData(float defaultValue, float step, float min, float max, Action<float> callback)
        {
            _step = step;
            _callback = callback;
            if (_slider != null) _slider.Config(min, max, step, defaultValue);
        }

        /// <summary>当前值(对标 GetValue)。</summary>
        public float GetValue()
        {
            return _slider != null ? _slider.Value : 0f;
        }

        public void HideNumBtnAndNumBg()
        {
            if (reduce_btn != null) reduce_btn.gameObject.SetActive(false);
            if (increase_btn != null) increase_btn.gameObject.SetActive(false);
            if (_slider != null) _slider.SetNumberVisible(false);
        }

        public void SetCustomWidth(float width)
        {
            if (_slider != null) _slider.SetCustomWidth(width);
        }

        private void Step(int dir)
        {
            if (_slider == null) return;
            _slider.SetValue(_slider.Value + dir * _step);
        }

        private void BindBtn(Component target, Action onClick)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, onClick);
        }
    }
}
