using System;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Dress;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Dress
{
    public sealed class DressTab : DressTabBind
    {
        private string _upIcon;
        private string _downIcon;
        private Action _onClick;
        private bool _selected;

        protected override void OnInit()
        {
            SetOnlyRaycast(_Image1);
            if (_Image1 != null) UIUtil.AddClick(_Image1, () => _onClick?.Invoke());
        }

        public void SetData(string label, string upIcon, string downIcon, bool selected, Action onClick)
        {
            _upIcon = upIcon;
            _downIcon = downIcon;
            _onClick = onClick;
            if (_lb != null) _lb.text = label ?? "";
            SetSelected(selected);
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            if (_lb != null) _lb.color = selected ? new Color32(255, 240, 185, 255) : new Color32(96, 59, 19, 255);
            if (redDisplay != null) redDisplay.gameObject.SetActive(false);
            if (_Image1 != null)
                _ = ResManager.SetImageAsync(_Image1, GameResPath.GetIcon("dress", selected ? "uian_003a" : "uian_003c"), nativeSize: false);
            if (iconDisplay != null)
                _ = ResManager.SetImageAsync(iconDisplay, GameResPath.GetIcon("dress", selected ? _downIcon : _upIcon), nativeSize: false);
        }

        private void SetOnlyRaycast(Graphic target)
        {
            foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = graphic == target;
        }
    }
}
