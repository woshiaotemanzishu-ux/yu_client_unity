using System;
using Shenxiao.Common.Tips;
using Shenxiao.Generated.UI.Bag;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>背包/仓库扩容。每扩一格消耗 2 个 38250026；材料不足时由服务端自动购买链补足，最终以 15002 为准。</summary>
    public sealed class ExpandBagView : ExpandBagViewBind
    {
        public sealed class Presentation
        {
            public int BagPos;
            public int InitialCount;
        }

        public override UILayer Layer => UILayer.Popup;

        public const int ExpandGoodsTypeId = 38250026;
        public const int ExpandNeedPerCell = 2;

        [SerializeField] private int _inputLimit = 9999;
        private int _count = 1;
        private int _bagPos = BagModel.POS_BAG;
        private BaseAwardItem _item;
        private bool _subscribed;

        protected override void OnInit()
        {
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
            EnsureItem();

            BindBtn(cancel_btn, Hide);
            BindBtn(close_btn, Hide);
            BindBtn(enter_btn, OnEnter);
            BindBtn(reduce_btn, () => SetCount(_count - 1, true));
            BindBtn(increase_btn, () => SetCount(_count + 1, true));
            BindBtn(maxBtn, SetMaximumFromOwnedKeys);
        }

        protected override void OnShow(object args)
        {
            int initialCount = 0;
            if (args is Presentation presentation)
            {
                _bagPos = presentation.BagPos == BagModel.POS_WAREHOUSE ? BagModel.POS_WAREHOUSE : BagModel.POS_BAG;
                initialCount = presentation.InitialCount;
            }
            else
            {
                _bagPos = args is int pos && pos == BagModel.POS_WAREHOUSE ? BagModel.POS_WAREHOUSE : BagModel.POS_BAG;
            }
            long available = BagModel.Instance.GetTypeGoodsNum(ExpandGoodsTypeId);
            int maxByItem = (int)Math.Min(_inputLimit, available / ExpandNeedPerCell);
            SetCount(initialCount > 0 ? initialCount : (maxByItem > 0 ? maxByItem : 1), false);
            Subscribe();
        }

        protected override void OnHide()
        {
            Unsubscribe();
            BagFlow.NotifyActivitySubHidden(this);
        }
        protected override void OnDispose() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On(GlobalEvent.EVT_BAG_UPDATE, RefreshCost);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off(GlobalEvent.EVT_BAG_UPDATE, RefreshCost);
            _subscribed = false;
        }

        private void EnsureItem()
        {
            if (_item != null || _tpl_BaseAwardItem == null || itemGp == null) return;
            GameObject go = Instantiate(_tpl_BaseAwardItem, itemGp);
            go.name = "ExpandCostItem_Runtime";
            go.SetActive(true);
            _item = go.GetComponent<BaseAwardItem>();
            if (_item == null) return;
            _item.SetScale(0.7f);
            _item.SetData(ExpandGoodsTypeId, 1);
            _item.SetClickCallBack(() => ItemTipsView.Show(ExpandGoodsTypeId));
        }

        private void SetCount(int n, bool showBoundaryTip)
        {
            if (n < 1)
            {
                if (showBoundaryTip) TipsManager.Toast("已达最小扩充容量");
                n = 1;
            }
            _count = Mathf.Clamp(n, 1, Mathf.Max(1, _inputLimit));
            RefreshCost();
        }

        private void SetMaximumFromOwnedKeys()
        {
            int max = (int)Math.Min(_inputLimit,
                BagModel.Instance.GetTypeGoodsNum(ExpandGoodsTypeId) / ExpandNeedPerCell);
            if (max <= 0)
            {
                TipsManager.Toast("已达最大扩充容量");
                return;
            }
            SetCount(max, false);
        }

        private void RefreshCost()
        {
            long owned = BagModel.Instance.GetTypeGoodsNum(ExpandGoodsTypeId);
            long need = (long)_count * ExpandNeedPerCell;
            if (input_text != null) input_text.text = _count.ToString();
            if (left_cost != null) left_cost.text = "拥有";
            if (right_cost != null)
                right_cost.text = owned >= need ? owned + "/" + need : "<color=#ff4f50>" + owned + "/" + need + "</color>";
            if (tip_text != null) tip_text.text = _bagPos == BagModel.POS_WAREHOUSE ? "扩充仓库容量" : "扩充背包容量";
        }

        private void OnEnter()
        {
            long owned = BagModel.Instance.GetTypeGoodsNum(ExpandGoodsTypeId);
            long need = (long)_count * ExpandNeedPerCell;
            Action send = () =>
            {
                BagController.Instance.ExpandBag(_bagPos, _count);
                Hide();
            };
            if (owned >= need)
            {
                send();
                return;
            }

            long shortage = need - owned;
            TipsManager.Confirm("当前扩容道具不足 " + shortage + " 个，是否使用自动购买补足？", send);
            Hide();
        }

        private static void BindBtn(Component target, Action onClick)
        {
            if (target == null) return;
            GameObject go = target.gameObject;
            foreach (Graphic graphic in go.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            Image image = go.GetComponent<Image>();
            if (image == null)
            {
                image = go.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0f);
            }
            image.raycastTarget = true;
            UIUtil.AddClick(image, onClick);
        }
    }
}
