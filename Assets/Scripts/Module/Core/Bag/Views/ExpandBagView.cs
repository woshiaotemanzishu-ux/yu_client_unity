using System;
using Shenxiao.Generated.UI.Bag;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 扩展背包格子弹窗(对标老客户端 bag/ExpandBagView.ts):购买/扩展背包格子,含数量加减/最大、消耗物品、确定/取消。
    ///
    /// 数量加减是自包含 UI 逻辑(无后端)→ 做成可用(本地计数,reduce/increase/max,钳 ≥1);取消/关闭 → Hide;
    /// 确定 → 打日志(购买/扩展协议未移植)。降级:扩展上限/消耗/cost 依赖 BagModel,先按默认上限;消耗物品模板隐藏。
    /// 事件驱动弹窗,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class ExpandBagView : ExpandBagViewBind
    {
        /// <summary>数量上限(待 BagModel 扩展上限;先给默认)。</summary>
        [SerializeField] private int _maxCount = 99;

        private int _count = 1;

        protected override void OnInit()
        {
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);

            BindBtn(cancel_btn, OnClose);
            BindBtn(close_btn, OnClose);
            BindBtn(enter_btn, OnEnter);
            BindBtn(reduce_btn, () => SetCount(_count - 1));
            BindBtn(increase_btn, () => SetCount(_count + 1));
            BindBtn(maxBtn, () => SetCount(_maxCount));
        }

        protected override void OnShow(object args)
        {
            SetCount(1);
        }

        /// <summary>本地数量加减(对标 reduce/increase/max 步进),钳 [1, _maxCount]。</summary>
        private void SetCount(int n)
        {
            _count = Mathf.Clamp(n, 1, Mathf.Max(1, _maxCount));
            if (input_text != null) input_text.text = _count.ToString();
        }

        private void OnClose() => Hide();

        private void OnEnter()
        {
            GameLog.Info("Bag", "扩展背包 确定 count={0} → 待对接 购买/扩展协议(BagModel)", _count);
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
