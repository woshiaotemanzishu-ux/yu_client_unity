using System;
using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.BossField;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Shop;
using UnityEngine;

namespace Shenxiao.Module.Core.Boss.Views.BossField
{
    public sealed class BossFieldSoulShopView : BossFieldSoulShopViewBind
    {
        private const int SoulCurrency = 36240001;
        private readonly List<BossFieldSoulShopItem> _rows = new List<BossFieldSoulShopItem>();
        private bool _subscribed;

        protected override void OnInit()
        {
            if (_tpl_BossFieldSoulShopItem != null) _tpl_BossFieldSoulShopItem.SetActive(false);
            if (_tpl_BossFieldSoulShopSubItem != null) _tpl_BossFieldSoulShopSubItem.SetActive(false);
            if (_img_close != null) UIUtil.AddClick(_img_close, Hide);
            if (_img_add != null) UIUtil.AddClick(_img_add, () => BossFieldFlow.OpenPopupForSoulItem());
            if (_img_question != null) UIUtil.AddClick(_img_question,
                () => GameLog.Info("BossField", "SoulOfWar 说明弹窗属于 Common blocker"));
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            ShopController.Instance.RequestShopType(ShopModel.TYPE_SOUL_OF_WAR);
            Rebuild();
        }

        protected override void OnHide() => Unsubscribe();
        protected override void OnDispose() => Unsubscribe();

        private void Rebuild()
        {
            List<ShopModel.GoodsVo> goods = ShopModel.Instance.GetShopDataByType(ShopModel.TYPE_SOUL_OF_WAR);
            int rowCount = (goods.Count + 2) / 3;
            for (int i = 0; i < _rows.Count; i++) _rows[i].gameObject.SetActive(false);
            for (int row = 0; row < rowCount; row++)
            {
                BossFieldSoulShopItem item = GetOrCreate(row);
                if (item == null) continue;
                item.gameObject.SetActive(true);
                var slice = new List<ShopModel.GoodsVo>();
                for (int j = row * 3; j < Math.Min(goods.Count, row * 3 + 3); j++) slice.Add(goods[j]);
                item.Show(new BossFieldSoulShopItem.Args(slice, _tpl_BossFieldSoulShopSubItem));
            }
            if (_lb_goods_num != null) _lb_goods_num.text = BagModel.Instance.GetTypeGoodsNum(SoulCurrency).ToString();
            if (_lb_time != null) _lb_time.text = "";
            if (_list_item != null) _list_item.verticalNormalizedPosition = 1f;
        }

        private BossFieldSoulShopItem GetOrCreate(int index)
        {
            if (index < _rows.Count) return _rows[index];
            if (_tpl_BossFieldSoulShopItem == null || _list_item == null || _list_item.content == null)
                throw new InvalidOperationException("SoulShop row template/content is not bound");
            GameObject go = Instantiate(_tpl_BossFieldSoulShopItem, _list_item.content);
            go.name = "BossFieldSoulShopItem_" + index;
            BossFieldSoulShopItem row = go.GetComponent<BossFieldSoulShopItem>();
            if (row == null)
            {
                GameLog.Error("BossField", "BossFieldSoulShopItem template is not runtime-subclass-owned; prefab GUID mismatch");
                Destroy(go);
                return null;
            }
            _rows.Add(row);
            return row;
        }

        private void OnData(int type) { if (type == ShopModel.TYPE_SOUL_OF_WAR) Rebuild(); }
        private void OnOne(int keyId) => Rebuild();
        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On<int>(GlobalEvent.EVT_SHOP_DATA_UPDATE, OnData);
            EventDispatcher.On<int>(GlobalEvent.EVT_SHOP_ONE_UPDATE, OnOne);
            EventDispatcher.On<int>(GlobalEvent.EVT_SHOP_BUY_SUCCESS, OnOne);
            EventDispatcher.On(GlobalEvent.EVT_BAG_UPDATE, Rebuild);
            _subscribed = true;
        }
        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off<int>(GlobalEvent.EVT_SHOP_DATA_UPDATE, OnData);
            EventDispatcher.Off<int>(GlobalEvent.EVT_SHOP_ONE_UPDATE, OnOne);
            EventDispatcher.Off<int>(GlobalEvent.EVT_SHOP_BUY_SUCCESS, OnOne);
            EventDispatcher.Off(GlobalEvent.EVT_BAG_UPDATE, Rebuild);
            _subscribed = false;
        }
    }
}
