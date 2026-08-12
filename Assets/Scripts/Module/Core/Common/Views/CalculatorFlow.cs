using System;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Common
{
    /// <summary>CommonModule/CalculatorViewBind 的正式数字输入消费者；不创建运行时视觉节点。</summary>
    public static class CalculatorFlow
    {
        private static CalculatorViewBind _view;
        private static Action<int> _onChanged;
        private static int _max;
        private static int _value;

        public static bool IsAvailable => _view != null;

        public static void Attach(CalculatorViewBind view)
        {
            if (view == null || ReferenceEquals(_view, view)) return;
            _view = view;
            Bind(view.key_1, () => Input(1)); Bind(view.key_2, () => Input(2)); Bind(view.key_3, () => Input(3));
            Bind(view.key_4, () => Input(4)); Bind(view.key_5, () => Input(5)); Bind(view.key_6, () => Input(6));
            Bind(view.key_7, () => Input(7)); Bind(view.key_8, () => Input(8)); Bind(view.key_9, () => Input(9));
            Bind(view.key_10, () => Input(0));
            Bind(view.key_11, Backspace);
            Bind(view.key_12, () => Close(true));
            Bind(view.click_bg, () => Close(true));
            view.gameObject.SetActive(false);
        }

        public static void Detach(CalculatorViewBind view)
        {
            if (!ReferenceEquals(_view, view)) return;
            Close(false);
            _view = null;
        }

        public static bool Show(int max, Action<int> onChanged)
        {
            if (_view == null || onChanged == null) return false;
            _max = Math.Max(0, max);
            _value = 0;
            _onChanged = onChanged;
            _view.Show();
            return true;
        }

        public static void Close(bool commit)
        {
            if (_view != null && _view.IsShown) _view.Hide();
            Action<int> callback = _onChanged;
            int value = Normalize(_value);
            _onChanged = null;
            _max = 0;
            _value = 0;
            if (commit) callback?.Invoke(value);
        }

        public static int ProjectDigit(int current, int digit, int max)
        {
            long next = (long)Math.Max(0, current) * 10L + Mathf.Clamp(digit, 0, 9);
            return (int)Math.Min(Math.Max(0, max), next);
        }

        public static int ProjectBackspace(int current) => Math.Max(0, current) / 10;

        private static void Input(int digit)
        {
            long raw = (long)_value * 10L + digit;
            int next = ProjectDigit(_value, digit, _max);
            if (raw > _max) TipsManager.Toast(_max > 0 ? $"超过上限 {_max}" : "超过上限");
            _value = next;
            _onChanged?.Invoke(Normalize(_value));
        }

        private static void Backspace()
        {
            _value = ProjectBackspace(_value);
            _onChanged?.Invoke(Normalize(_value));
        }

        private static int Normalize(int value) => value <= 0 ? 1 : value;

        private static void Bind(Component component, Action action)
        {
            if (component == null) return;
            Graphic graphic = component as Graphic ?? component.GetComponent<Graphic>();
            if (graphic != null) UIUtil.ClearClicks(graphic);
            UIUtil.AddClick(component, action);
        }
    }
}
