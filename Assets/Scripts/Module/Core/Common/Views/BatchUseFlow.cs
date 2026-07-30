using System;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Common;
using Shenxiao.Module.Core.Bag;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Common
{
    /// <summary>
    /// 堆叠物品批量使用。复用 CommonModule.prefab/GoodsFuncView 与 WithBtnHSlider，
    /// 确认时按当前背包权威数量二次夹紧后发送 15050。
    /// </summary>
    public static class BatchUseFlow
    {
        private static GameObject _moduleRoot;
        private static GoodsFuncViewBind _view;
        private static BaseAwardItem _item;
        private static WithBtnHSlider _slider;
        private static BagGoods _goods;
        private static bool _loading;
        private static int _epoch;

        public static void Show(BagGoods goods)
        {
            if (goods == null || goods.GoodsId <= 0 || goods.GoodsNum <= 0) return;
            _goods = goods;
            _ = ShowAsync(++_epoch);
        }

        private static async Task ShowAsync(int epoch)
        {
            if (!await EnsureBuilt()) return;
            if (epoch != _epoch || _goods == null) return;

            BagGoods current = BagModel.Instance.FindContainerGoods(BagModel.POS_BAG, _goods.GoodsId);
            if (current == null || current.GoodsNum <= 0) return;
            _goods = current;

            if (_view.gp_price != null) _view.gp_price.gameObject.SetActive(false);
            if (_view.lb_name != null)
            {
                GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(current.TypeId);
                _view.lb_name.text = basic?.Name ?? ("#" + current.TypeId);
            }
            _item?.SetData(current.TypeId, current.GoodsNum, current.Bind != 0);
            int max = current.GoodsNum > int.MaxValue ? int.MaxValue : (int)current.GoodsNum;
            _slider?.SetData(max, 1f, 1f, max, null);

            BindUnique(_view.btn_close, Hide);
            BindUnique(_view.btn_cancel, Hide);
            BindUnique(_view.btn_enter, Confirm);
            _view.Show();
            _view.transform.SetAsLastSibling();
        }

        private static async Task<bool> EnsureBuilt()
        {
            if (_view != null && _item != null && _slider != null) return true;
            if (_loading) return false;
            _loading = true;
            try
            {
                Transform parent = ViewManager.GetLayer(UILayer.Popup);
                if (parent == null) return false;
                _moduleRoot = await ResManager.InstantiateAsync(
                    GameResPath.GetUIPrefab("common", "CommonModule"), parent);
                if (_moduleRoot == null) return false;
                _moduleRoot.name = "CommonModule(BatchUse)";
                _view = _moduleRoot.GetComponentInChildren<GoodsFuncViewBind>(true);
                if (_view == null)
                {
                    ResManager.ReleaseInstance(_moduleRoot);
                    _moduleRoot = null;
                    return false;
                }

                _view.gameObject.SetActive(false);
                if (_view._tpl_BaseAwardItem != null && _view.gp_item != null)
                {
                    GameObject go = UnityEngine.Object.Instantiate(_view._tpl_BaseAwardItem, _view.gp_item);
                    go.name = "BatchUseItem_Runtime";
                    go.SetActive(true);
                    _item = go.GetComponent<BaseAwardItem>();
                    if (_item != null)
                    {
                        _item.Show();
                        _item.SetScale(0.7f);
                        _item.SetClickCallBack(() => { });
                    }
                }
                if (_view._tpl_WithBtnHSlider != null && _view.gp_slider != null)
                {
                    GameObject go = UnityEngine.Object.Instantiate(_view._tpl_WithBtnHSlider, _view.gp_slider);
                    go.name = "BatchUseSlider_Runtime";
                    go.SetActive(true);
                    _slider = go.GetComponent<WithBtnHSlider>();
                    _slider?.Show();
                }
                return _item != null && _slider != null;
            }
            finally
            {
                _loading = false;
            }
        }

        private static void Confirm()
        {
            BagGoods goods = _goods;
            if (goods == null) return;
            BagGoods current = BagModel.Instance.FindContainerGoods(BagModel.POS_BAG, goods.GoodsId);
            if (current == null || current.GoodsNum <= 0)
            {
                Hide();
                return;
            }
            int owned = current.GoodsNum > int.MaxValue ? int.MaxValue : (int)current.GoodsNum;
            int count = Mathf.Clamp(Mathf.RoundToInt(_slider != null ? _slider.GetValue() : 1f), 1, owned);
            BagController.Instance.UseGoods(current.GoodsId, count);
            Hide();
        }

        private static void Hide()
        {
            _epoch++;
            _goods = null;
            _view?.Hide();
        }

        private static void BindUnique(Component target, Action action)
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
            UIUtil.ClearClicks(image);
            UIUtil.AddClick(image, action);
        }
    }
}
