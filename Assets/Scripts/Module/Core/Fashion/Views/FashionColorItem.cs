using System;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Fashion;

namespace Shenxiao.Module.Core.Fashion
{
    /// <summary>
    /// 颜色档位一格(对标老端 fashion/FashionColorItem.ts,4 节点 bg/select/red/lock):
    /// 本轮"能点能用"简化——不做每色专属换图(染色贴图/色卡资产未导入,见 r21_fashion.md §4.5),
    /// 只用既有 lock/select/red 三个覆盖件的显隐表达"未解锁/当前选中/可操作"三态,bg 保留烤制原图。
    /// </summary>
    public sealed class FashionColorItem : FashionColorItemBind
    {
        private bool _inited;
        private Action _onClick;

        public void SetClick(Action onClick)
        {
            EnsureInit();
            _onClick = onClick;
        }

        private void EnsureInit()
        {
            if (_inited) return;
            _inited = true;
            if (bg != null) UIUtil.AddClick(bg, () => _onClick?.Invoke());
        }

        /// <summary>locked=该颜色未解锁(点击应发 41301);selected=是否当前穿的颜色;hasRed=可进阶/可解锁红点。</summary>
        public void SetData(bool locked, bool selected, bool hasRed)
        {
            EnsureInit();
            if (@lock != null) @lock.gameObject.SetActive(locked);
            if (select != null) select.gameObject.SetActive(selected);
            if (red != null) red.gameObject.SetActive(hasRed);
        }
    }
}
