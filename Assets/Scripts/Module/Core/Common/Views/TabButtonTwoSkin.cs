using System;
using Shenxiao.Generated.UI.Common;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Common
{
    /// <summary>
    /// 六标签窗·标签按钮(对标老端 WindowComponentTabButton 渲染器;BaseWindowSkin 的 _tpl_TabButtonTwoSkin)。
    ///
    /// 图标式标签:`_Image1` 为按钮图(选中/未选中 切 sprite,无 sprite 时退化为 tint),`redDisplay` 红点。
    /// 老端无文本节点 → 标签名由 sprite 体现(每页 icon_up/down,属美术/用户配的「效果」;此处仅管「结构」:可点击+选中态+红点)。
    /// 由 <see cref="BaseWindowSkinView"/> 克隆入 _list_tab_con 后调用 SetData/SetSelected。
    /// </summary>
    public sealed class TabButtonTwoSkin : TabButtonTwoSkinBind
    {
        private static readonly Color SelectedColor = Color.white;
        private static readonly Color UnselectedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
        private static readonly Color DisabledColor = new Color(0.42f, 0.42f, 0.42f, 1f);

        private int _index;
        private Action<int> _onClick;
        private Sprite _up;
        private Sprite _down;
        private bool _enabled = true;

        /// <summary>克隆后配置:索引 + 点击回调 + 可选 选中/未选中 sprite。点击绑定在此完成(不依赖 OnInit 生命周期,克隆件更稳)。</summary>
        public void SetData(int index, Action<int> onClick, Sprite up = null, Sprite down = null)
        {
            _index = index;
            _onClick = onClick;
            _up = up;
            _down = down;
            if (redDisplay != null) redDisplay.gameObject.SetActive(false);
            if (_Image1 != null)
            {
                _Image1.raycastTarget = true;
                UIUtil.AddClick(_Image1, OnClick);
            }
            SetSelected(false);
        }

        private void OnClick()
        {
            if (!_enabled) return;
            _onClick?.Invoke(_index);
        }

        public void SetSelected(bool selected)
        {
            if (_Image1 == null) return;
            if (_up != null && _down != null)
            {
                _Image1.sprite = selected ? _down : _up;
                _Image1.color = _enabled ? Color.white : DisabledColor;
            }
            else
            {
                _Image1.color = !_enabled ? DisabledColor : (selected ? SelectedColor : UnselectedColor);
            }
        }

        public void SetRed(bool on)
        {
            if (redDisplay != null) redDisplay.gameObject.SetActive(on);
        }

        public void SetEnabledVisual(bool enabled)
        {
            _enabled = enabled;
            SetSelected(false);
        }
    }
}
