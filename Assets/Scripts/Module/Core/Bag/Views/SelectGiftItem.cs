using System;
using Shenxiao.Generated.UI.Bag;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 选择礼包项(对标老客户端 bag/SelectGiftItem.ts):一个可选礼包物品,含名字、物品图标、数量加减/最大、拥有数。
    ///
    /// 数量步进自包含 → 做成可用(本地计数 _count,reduce/increase/max,钳 [0, _maxCount],刷 num_label);
    /// 名字 SetData 填;物品图标(item_group)、拥有数(haveLb)依赖 GoodsModel 配置(未移植)→ 暂不设。
    /// 列表项,由 SelectGiftView 克隆 _tpl_SelectGiftItem。
    /// </summary>
    public sealed class SelectGiftItem : SelectGiftItemBind
    {
        /// <summary>可选数量上限(待礼包配置;先给默认)。</summary>
        [SerializeField] private int _maxCount = 99;

        private int _count;

        public int Count => _count;

        protected override void OnInit()
        {
            BindBtn(reduce_btn, () => SetCount(_count - 1));
            BindBtn(increase_btn, () => SetCount(_count + 1));
            BindBtn(max_btn, () => SetCount(_maxCount));
            SetCount(0);
        }

        /// <summary>填礼包项名字 + 可选上限(对标 SetData)。图标/拥有待配置。</summary>
        public void SetData(string name, int max)
        {
            if (name_label != null) name_label.text = name ?? "";
            _maxCount = Mathf.Max(0, max);
            SetCount(0);
        }

        private void SetCount(int n)
        {
            _count = Mathf.Clamp(n, 0, Mathf.Max(0, _maxCount));
            if (num_label != null) num_label.text = _count.ToString();
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
