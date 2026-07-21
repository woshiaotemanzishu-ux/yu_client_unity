using System;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Fashion;

namespace Shenxiao.Module.Core.Fashion
{
    /// <summary>套装页签。</summary>
    public sealed class FashionSuitTabItem : FashionSuitTabItemBind
    {
        private Action _onClick;
        private bool _bound;

        public void SetData(string title, bool selected, bool red, Action onClick)
        {
            if (!_bound)
            {
                _bound = true;
                if (_img_bg != null) UIUtil.AddClick(_img_bg, () => _onClick?.Invoke());
            }
            _onClick = onClick;
            if (_lb_name != null) _lb_name.text = title ?? string.Empty;
            if (_img_select != null) _img_select.gameObject.SetActive(selected);
            if (_img_red != null) _img_red.gameObject.SetActive(red);
        }
    }
}
