using System;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Fashion;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using UnityEngine;

namespace Shenxiao.Module.Core.Fashion
{
    /// <summary>部位升级材料格；发送时保留 <see cref="BagGoods.GoodsId"/>，绝不把类型 id 当实例 id。</summary>
    public sealed class FasBagItemRenderer : FasBagItemRendererBind
    {
        private Action _onClick;
        private int _typeId;
        private bool _bound;

        public void SetData(BagGoods goods, int count, bool selected, Action onClick)
        {
            EnsureClick();
            _onClick = onClick;
            _typeId = goods?.TypeId ?? 0;
            bool has = goods != null && goods.GoodsId > 0 && goods.TypeId > 0 && count > 0;
            if (fashion_image != null) fashion_image.gameObject.SetActive(has);
            if (num_label != null)
            {
                num_label.gameObject.SetActive(has);
                num_label.text = has ? count.ToString() : string.Empty;
            }
            if (select_image != null) select_image.gameObject.SetActive(has && selected);
            if (!has) return;
            _ = RefreshImages(goods.TypeId);
        }

        private void EnsureClick()
        {
            if (_bound) return;
            _bound = true;
            if (_Group2 != null) UIUtil.AddClick(_Group2, () => _onClick?.Invoke());
        }

        private async System.Threading.Tasks.Task RefreshImages(int typeId)
        {
            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(typeId);
            if (basic == null) return;
            if (item_image != null)
            {
                string plate = GameResPath.GetIcon("common", "com_goods_plate_" + basic.Color);
                await ResManager.SetImageAsync(item_image, plate, false, false);
                if (_typeId != typeId) return;
            }
            if (fashion_image != null && !string.IsNullOrEmpty(basic.Icon))
            {
                await ResManager.SetImageAsync(fashion_image, GameResPath.GetGoodsIconPath(basic.Icon), false, false);
                if (_typeId != typeId) return;
            }
        }
    }
}
