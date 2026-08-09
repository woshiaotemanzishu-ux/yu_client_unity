using System;
using System.Collections.Generic;
using Shenxiao.Generated.UI.BossField;
using Shenxiao.Module.Core.Shop;
using UnityEngine;

namespace Shenxiao.Module.Core.Boss.Views.BossField
{
    public sealed class BossFieldSoulShopItem : BossFieldSoulShopItemBind
    {
        public sealed class Args
        {
            public readonly List<ShopModel.GoodsVo> Goods;
            public readonly GameObject Template;
            public Args(List<ShopModel.GoodsVo> goods, GameObject template) { Goods = goods; Template = template; }
        }
        private readonly List<BossFieldSoulShopSubItem> _items = new List<BossFieldSoulShopSubItem>();

        protected override void OnShow(object args)
        {
            Args data = args as Args;
            for (int i = 0; i < _items.Count; i++) _items[i].gameObject.SetActive(false);
            if (data == null || data.Template == null || _hbox_con == null) return;
            for (int i = 0; i < data.Goods.Count; i++)
            {
                BossFieldSoulShopSubItem item;
                if (i < _items.Count) item = _items[i];
                else
                {
                    GameObject go = Instantiate(data.Template, _hbox_con);
                    go.name = "BossFieldSoulShopSubItem_" + i;
                    item = go.GetComponent<BossFieldSoulShopSubItem>();
                    if (item == null)
                    {
                        Shenxiao.Framework.Util.GameLog.Error("BossField", "BossFieldSoulShopSubItem template is not runtime-subclass-owned; prefab GUID mismatch");
                        Destroy(go);
                        continue;
                    }
                    _items.Add(item);
                }
                item.gameObject.SetActive(true);
                item.Show(data.Goods[i]);
            }
        }
    }
}
