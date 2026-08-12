using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Common;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Shop
{
    /// <summary>
    /// 复用 CommonModule.prefab/QuickBuyView 的正式速购流。视觉与全部控件来自 Prefab；
    /// 本类只做价格投影、数量/币种状态、15304 与关闭链。用于培养材料不足的传递弹窗。
    /// </summary>
    public static class QuickBuyFlow
    {
        public sealed class State
        {
            public int GoodsId;
            public int Count;
            public int BuyType;
            public int GoldPrice;
            public int BGoldPrice;
            public int UnitPrice;
            public long TotalPrice;
            public int MaxAffordable;
            public bool CanBuy;
            public string BlockReason = string.Empty;
        }

        private static GameObject _root;
        private static QuickBuyViewBind _view;
        private static CalculatorViewBind _calculator;
        private static BaseAwardItem _item;
        private static QuickBuySourceItem _sourceTemplate;
        private static readonly List<QuickBuySourceItem> SourceItems = new List<QuickBuySourceItem>();
        private static bool _loading;
        private static int _requestId;
        private static int _goodsId;
        private static int _count = 1;
        private static int _buyType = 2;
        private static int _goldPrice;
        private static int _bgoldPrice;

        public static State CurrentState => Project(_goodsId, _count, _buyType, _goldPrice, _bgoldPrice,
            RoleModel.Instance.Gold, RoleModel.Instance.BGold);

        public static void Show(int goodsId, int initialCount = 1)
        {
            if (goodsId <= 0) return;
            _goodsId = goodsId;
            _count = Math.Max(1, initialCount);
            _ = ShowAsync(++_requestId);
        }

        public static void Close()
        {
            ++_requestId;
            CalculatorFlow.Close(false);
            if (_view != null && _view.IsShown) _view.Hide();
            if (_root != null) _root.SetActive(false);
        }

        public static void Reset()
        {
            Close();
            if (_root != null) ResManager.ReleaseInstance(_root);
            _root = null;
            _view = null;
            CalculatorFlow.Detach(_calculator);
            _calculator = null;
            _item = null;
            _sourceTemplate = null;
            SourceItems.Clear();
            _loading = false;
            _goodsId = 0;
            _goldPrice = 0;
            _bgoldPrice = 0;
        }

        public static State Project(int goodsId, int count, int buyType, int goldPrice, int bgoldPrice,
            int ownedGold, int ownedBGold)
        {
            count = Math.Max(1, count);
            buyType = buyType == 1 ? 1 : 2;
            int unit = buyType == 1 ? goldPrice : bgoldPrice;
            int owned = buyType == 1 ? ownedGold : ownedBGold;
            int max = unit > 0 ? Math.Max(0, owned / unit) : 0;
            bool configured = goodsId > 0 && unit > 0;
            return new State
            {
                GoodsId = goodsId,
                Count = count,
                BuyType = buyType,
                GoldPrice = Math.Max(0, goldPrice),
                BGoldPrice = Math.Max(0, bgoldPrice),
                UnitPrice = Math.Max(0, unit),
                TotalPrice = (long)Math.Max(0, unit) * count,
                MaxAffordable = max,
                CanBuy = configured && count <= max,
                BlockReason = !configured ? "quick-buy-price-missing"
                    : count > max ? (buyType == 1 ? "gold-insufficient" : "bound-gold-insufficient") : string.Empty,
            };
        }

        private static async Task ShowAsync(int requestId)
        {
            await Task.WhenAll(ShopConfigs.EnsureLoaded(), GoodsModel.EnsureLoaded());
            if (requestId != _requestId || _goodsId <= 0 || !await EnsureViewAsync()) return;
            JObject row = ShopConfigs.GetQuickBuyPrice(_goodsId);
            _goldPrice = ShopConfigs.ReadInt(row, "gold_price");
            _bgoldPrice = ShopConfigs.ReadInt(row, "bgold_price");
            if (_goldPrice <= 0 && _bgoldPrice <= 0)
            {
                TipsManager.Toast("该材料暂不支持快速购买");
                GameLog.Warn("QuickBuy", "price missing goods={0}", _goodsId);
                return;
            }
            _buyType = _bgoldPrice > 0 ? 2 : 1;
            _root.SetActive(true);
            foreach (BaseView other in _root.GetComponentsInChildren<BaseView>(true))
                if (other != _view && other != _item) other.gameObject.SetActive(false);
            _view.Show(_goodsId);
            _view.transform.SetAsLastSibling();
            if (_view.name_label != null) _view.name_label.text = GoodsModel.GetGoodsName(_goodsId);
            if (_view.diamond_label != null) _view.diamond_label.text = _goldPrice.ToString();
            if (_view.binddiamond_label != null) _view.binddiamond_label.text = _bgoldPrice.ToString();
            if (_view.diamond_group != null) _view.diamond_group.gameObject.SetActive(_goldPrice > 0);
            if (_view.binddiamond_group != null) _view.binddiamond_group.gameObject.SetActive(_bgoldPrice > 0);
            RebuildItem();
            RebuildSources();
            Refresh();
        }

        private static async Task<bool> EnsureViewAsync()
        {
            if (_root != null && _view != null) return true;
            if (_loading) return false;
            _loading = true;
            try
            {
                Transform parent = ViewManager.GetLayer(UILayer.Popup);
                if (parent == null) return false;
                _root = await ResManager.InstantiateAsync(
                    GameResPath.GetUIPrefab("common", "CommonModule"), parent);
                if (_root == null) return false;
                _root.name = "CommonModule(OutWardQuickBuy)";
                foreach (BaseView view in _root.GetComponentsInChildren<BaseView>(true))
                    view.gameObject.SetActive(false);
                _view = _root.GetComponentInChildren<QuickBuyViewBind>(true);
                if (_view == null)
                {
                    GameLog.Error("QuickBuy", "CommonModule missing QuickBuyViewBind");
                    Reset();
                    return false;
                }
                Bind(_view.close_btn, Close);
                Bind(_view.reduce_btn, () => ChangeCount(_count - 1));
                Bind(_view.increase_btn, () => ChangeCount(_count + 1));
                Bind(_view.max_btn, SetMax);
                Bind(_view.diamond_group, () => SelectBuyType(1));
                Bind(_view.binddiamond_group, () => SelectBuyType(2));
                Bind(_view.buy_btn, Buy);
                Bind(_view.recharge_btn, OpenRecharge);
                Bind(_view.num_touch, OpenCalculator);
                _calculator = _root.GetComponentInChildren<CalculatorViewBind>(true);
                if (_calculator != null) CalculatorFlow.Attach(_calculator);
                else GameLog.Error("QuickBuy", "CommonModule missing CalculatorViewBind");
                _sourceTemplate = _view.icon_group != null
                    ? _view.icon_group.GetComponentInChildren<QuickBuySourceItem>(true)
                    : null;
                if (_sourceTemplate == null)
                    GameLog.Error("QuickBuy", "QuickBuy.icon_group missing _tpl_QuickBuySourceItem");
                _root.SetActive(false);
                return true;
            }
            finally { _loading = false; }
        }

        private static void RebuildItem()
        {
            if (_view?._tpl_BaseAwardItem == null || _view.item_group == null) return;
            if (_item != null) UnityEngine.Object.Destroy(_item.gameObject);
            GameObject go = UnityEngine.Object.Instantiate(_view._tpl_BaseAwardItem, _view.item_group, false);
            go.SetActive(true);
            _item = go.GetComponent<BaseAwardItem>();
            _item?.SetData(_goodsId, _count);
        }

        private static void RebuildSources()
        {
            for (int i = 0; i < SourceItems.Count; i++)
                if (SourceItems[i] != null) UnityEngine.Object.Destroy(SourceItems[i].gameObject);
            SourceItems.Clear();
            if (_sourceTemplate == null || _view?.icon_group == null) return;

            List<GoodsModel.GoodsSourceEntry> entries = GoodsModel.GetGoodsSourceEntries(_goodsId);
            for (int i = 0; i < entries.Count; i++)
            {
                GoodsModel.GoodsSourceEntry entry = entries[i];
                if (ResolveGoodsSourceTab(entry.Id) < 0) continue; // 本路线只闭合140/141。
                QuickBuySourceItem item = UnityEngine.Object.Instantiate(_sourceTemplate, _view.icon_group, false);
                item.gameObject.name = $"QuickBuySourceItem_{entry.Id}";
                item.gameObject.SetActive(true);
                int openFunId = entry.Id;
                item.SetData(openFunId, entry.Name, () => OpenGoodsSource(openFunId));
                SourceItems.Add(item);
            }
            if (_view.other_group != null) _view.other_group.gameObject.SetActive(SourceItems.Count > 0);
            if (_view.icon_scroller != null)
            {
                _view.icon_scroller.StopMovement();
                _view.icon_scroller.horizontalNormalizedPosition = 0f;
            }
        }

        private static void ChangeCount(int value)
        {
            _count = Math.Max(1, value);
            Refresh();
        }

        private static void SetMax()
        {
            State state = CurrentState;
            _count = Math.Max(1, state.MaxAffordable);
            Refresh();
        }

        private static void SelectBuyType(int buyType)
        {
            if (buyType == 1 && _goldPrice <= 0 || buyType == 2 && _bgoldPrice <= 0) return;
            _buyType = buyType;
            Refresh();
        }

        private static void Refresh()
        {
            State state = CurrentState;
            if (_view == null) return;
            if (_view.cur_show_num != null) _view.cur_show_num.text = state.Count.ToString();
            if (_view.price_label != null) _view.price_label.text = state.UnitPrice.ToString();
            if (_view.total_label != null) _view.total_label.text = state.TotalPrice.ToString();
            if (_view._Image2 != null) _view._Image2.gameObject.SetActive(state.BuyType == 1);
            if (_view.diamond_img != null) _view.diamond_img.gameObject.SetActive(state.BuyType != 1);
            if (_view._Image4 != null) _view._Image4.gameObject.SetActive(state.BuyType == 2);
            if (_view.binddiamond_img != null) _view.binddiamond_img.gameObject.SetActive(state.BuyType != 2);
            SetInteractable(_view.buy_btn, state.CanBuy);
            _item?.SetCount(state.Count);
        }

        private static void Buy()
        {
            State state = CurrentState;
            if (!state.CanBuy)
            {
                TipsManager.Toast(state.BuyType == 1 ? "勾玉不足" : "绑玉不足");
                return;
            }
            ShopController.Instance.QuickBuy(state.GoodsId, state.Count, state.BuyType);
            Close(); // 对标老端：发15304后立即关闭，结果由Toast与父页事件反馈。
        }

        private static void OpenRecharge()
        {
            // 老端 QuickBuyView.recharge_btn -> OpenFun(21)；Unity 由 VipBootstrap 注册正式充值入口。
            // 未注册时明确留在当前弹窗，禁止进入 MainUIRoutePlaceholder 冒充成功跳转。
            if (!MainUIRouter.IsRegistered("recharge"))
            {
                GameLog.Error("QuickBuy", "recharge route is not registered (OpenFun 21)");
                return;
            }
            Close();
            MainUIRouter.Open("recharge");
        }

        private static void OpenCalculator()
        {
            State state = CurrentState;
            if (!CalculatorFlow.Show(state.MaxAffordable, ChangeCount))
                GameLog.Error("QuickBuy", "CalculatorFlow is unavailable");
        }

        /// <summary>本路线getway_url只接受老端140/141：分别打开灵玉/绑玉商城页；不扩展通用OpenFun。</summary>
        public static bool OpenGoodsSource(int openFunId)
        {
            int tab = ResolveGoodsSourceTab(openFunId);
            if (tab < 0) return false;
            Close();
            ShopFlow.OpenTab(tab);
            return true;
        }

        public static int ResolveGoodsSourceTab(int openFunId) => openFunId == 140 ? 1 : openFunId == 141 ? 2 : -1;


        private static void Bind(Component component, Action action)
        {
            if (component == null) return;
            Graphic graphic = component as Graphic ?? component.GetComponent<Graphic>();
            if (graphic != null) UIUtil.ClearClicks(graphic);
            UIUtil.AddClick(component, action);
        }

        private static void SetInteractable(Component component, bool value)
        {
            if (component == null) return;
            Button button = component.GetComponent<Button>();
            if (button != null) button.interactable = value;
            Graphic graphic = component as Graphic ?? component.GetComponent<Graphic>();
            if (graphic != null) graphic.raycastTarget = true; // 禁用仍保留反馈点击面。
        }
    }
}
