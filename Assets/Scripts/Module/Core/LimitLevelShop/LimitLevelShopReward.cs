using Shenxiao.Generated.UI.LimitLevelShop;
using Shenxiao.Module.Core.Common;
using UnityEngine;

namespace Shenxiao.Module.Core.LimitLevelShop
{
    public sealed class LimitLevelShopReward : LimitLevelShopRewardBind
    {
        private GameObject _cell;

        protected override void OnInit()
        {
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
        }

        public void SetData(int style, int typeId, long count)
        {
            if (!IsInitialized) Show();
            if (_cell != null) Destroy(_cell);
            if (_gp_con == null || _tpl_BaseAwardItem == null) return;
            (int goodsId, int locked) = GoodsModel.GetMappingTypeId(style, typeId);
            _cell = Instantiate(_tpl_BaseAwardItem, _gp_con);
            _cell.SetActive(true);
            _cell.GetComponent<BaseAwardItem>()?.SetData(goodsId, count, locked != 0);
        }

        protected override void OnDispose()
        {
            if (_cell != null) Destroy(_cell);
            _cell = null;
        }
    }
}
