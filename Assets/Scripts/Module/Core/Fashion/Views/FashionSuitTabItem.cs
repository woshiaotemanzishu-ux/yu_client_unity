using System;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Fashion;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Fashion
{
    /// <summary>套装页签。</summary>
    public sealed class FashionSuitTabItem : FashionSuitTabItemBind
    {
        private Action _onClick;
        private bool _bound;

        public int SuitId { get; private set; }
        public Image ClickSurface => _img_bg;

        public void SetData(string title, bool selected, bool red, Action onClick)
        {
            if (!_bound)
            {
                _bound = true;
                if (_img_bg != null) UIUtil.AddClick(_img_bg, () => _onClick?.Invoke());
            }
            _onClick = onClick;
            if (_img_bg != null) _img_bg.raycastTarget = true;
            if (_img_icon != null)
            {
                _img_icon.raycastTarget = false;
                _ = ResManager.SetImageAsync(_img_icon, GameResPath.GetIcon("fashion", "tab_icon_" + SuitId), false, false);
            }
            if (_img_select != null) _img_select.raycastTarget = false;
            if (_lb_name != null) _lb_name.raycastTarget = false;
            if (_img_red != null) _img_red.raycastTarget = false;
            if (_lb_name != null) _lb_name.text = title ?? string.Empty;
            if (_img_select != null) _img_select.gameObject.SetActive(selected);
            if (_img_red != null) _img_red.gameObject.SetActive(red);
        }

        public void SetSuitId(int suitId)
        {
            SuitId = suitId;
        }
    }
}
