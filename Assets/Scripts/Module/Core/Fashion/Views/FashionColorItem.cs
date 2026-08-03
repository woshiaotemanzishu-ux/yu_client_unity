using System;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Fashion;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Fashion
{
    /// <summary>
    /// 颜色档位一格(对标老端 fashion/FashionColorItem.ts,4 节点 bg/select/red/lock):
    /// bg 按 config_fashion_model.show_color 使用老端 color_{id} 图；lock/select/red 分别表达
    /// 未解锁、当前选中和可操作状态。
    /// </summary>
    public sealed class FashionColorItem : FashionColorItemBind
    {
        private bool _inited;
        private Action _onClick;
        private int _skinVersion;
        private Sprite _skin;
        private int _showColor = -1;
        private int _colorId;
        private Graphic _clickSurface;

        public int ColorId => _colorId;

        public Graphic ClickSurface
        {
            get
            {
                EnsureInit();
                return _clickSurface;
            }
        }

        public bool IsVisualReady => bg != null && bg.enabled && bg.sprite != null
            && ClickSurface != null && ClickSurface.enabled && ClickSurface.raycastTarget;

        public void SetClick(Action onClick)
        {
            EnsureInit();
            _onClick = onClick;
        }

        private void EnsureInit()
        {
            if (_inited) return;
            _inited = true;
            // 老端唯一点击面就是颜色圆片 bg。根节点只负责被 VerticalLayoutGroup 排布；
            // 若把根透明 Image 当点击面，布局刷新后它的几何与可见圆片可能短暂不同步，连续点色会漏击。
            Image rootImage = GetComponent<Image>();
            if (rootImage != null) rootImage.raycastTarget = false;
            _clickSurface = bg;
            if (_clickSurface != null) UIUtil.AddClick(_clickSurface, () => _onClick?.Invoke());
        }

        /// <summary>locked=该颜色未解锁(点击应发 41301);selected=是否当前穿的颜色;hasRed=可进阶/可解锁红点。</summary>
        public void SetData(int colorId, int showColor, bool locked, bool selected, bool hasRed)
        {
            EnsureInit();
            _colorId = colorId;
            RefreshSkin(showColor);
            if (@lock != null) @lock.gameObject.SetActive(locked);
            if (select != null) select.gameObject.SetActive(selected);
            if (red != null) red.gameObject.SetActive(hasRed);
        }

        private async void RefreshSkin(int showColor)
        {
            if (_showColor == showColor && _skin != null) return;
            _showColor = showColor;
            int version = ++_skinVersion;
            Sprite next = await ResManager.LoadAsync<Sprite>(GameResPath.GetIcon("fashion", "color_" + showColor));
            if (version != _skinVersion || this == null)
            {
                if (next != null) ResManager.Release(next);
                return;
            }
            if (next == null) return;
            if (_skin != null) ResManager.Release(_skin);
            _skin = next;
            if (bg != null)
            {
                bg.sprite = next;
                // 转换来的空 Image 在 Prefab 中按规则保持 disabled，避免资源到达前绘制白块；
                // 真正拿到颜色片后再启用；可见圆片 bg 是唯一点击面，其他装饰图不抢射线。
                bg.enabled = true;
                bg.raycastTarget = true;
            }
        }

        private void OnDestroy()
        {
            ++_skinVersion;
            if (_skin != null) ResManager.Release(_skin);
            _skin = null;
        }
    }
}
