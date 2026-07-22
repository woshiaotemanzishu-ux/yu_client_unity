using Shenxiao.Common.Tips;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Baby;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.Module.Core.Baby
{
    /// <summary>宝宝未激活时的孕育页，仅负责激活请求。</summary>
    public partial class GestateBabyView : GestateBabyViewBind
    {
        private BaseAwardItem _costItem;
        private BabyValueConfigs.Cost _cost;
        private bool _pending;

        protected override async void OnShow(object args)
        {
            _cost = null;
            _pending = false;
            if (_costItem != null) _costItem.gameObject.SetActive(false);
            UIUtil.AddClick(gestateBtn, OnActivate);
            UIUtil.AddClick(closeBtn, BabyFlow.Close);
            await BabyValueConfigs.EnsureLoaded();
            await GoodsModel.EnsureLoaded();
            if (!IsShown) return;
            _cost = BabyValueConfigs.GestateCost;
            RenderCost();
        }

        protected override void OnHide()
        {
            _pending = false;
            UIUtil.ClearClicks(gestateBtn != null ? gestateBtn.GetComponent<UnityEngine.UI.Image>() : null);
            UIUtil.ClearClicks(closeBtn);
        }

        private void OnActivate()
        {
            if (_pending) return;
            if (_cost == null || _cost.Num <= 0)
            {
                TipsManager.Toast("宝宝孕育消耗配置未加载");
                return;
            }
            if (_cost.Type != 2 || _cost.TypeId != 0)
            {
                TipsManager.Toast("宝宝孕育消耗类型不支持");
                return;
            }
            if (RoleModel.Instance.BGold < _cost.Num)
            {
                string currency = GoodsModel.GetNotNormalDesc(_cost.Type, _cost.TypeId);
                TipsManager.Toast((string.IsNullOrEmpty(currency) ? "绑定灵玉" : currency) + "不足");
                return;
            }
            _pending = true;
            BabyController.Instance.RequestActivate();
            BabyFlow.Close();
        }

        private void RenderCost()
        {
            if (_costItem == null && _tpl_BaseAwardItem != null && itemGp != null)
            {
                GameObject go = Instantiate(_tpl_BaseAwardItem, itemGp, false);
                _costItem = go.GetComponent<BaseAwardItem>();
                if (_costItem != null) _costItem.SetScale(85f / 127f);
            }
            if (_costItem == null || _cost == null) return;
            (int goodsId, int _) = GoodsModel.GetMappingTypeId(_cost.Type, _cost.TypeId);
            _costItem.gameObject.SetActive(goodsId > 0);
            if (goodsId > 0) _costItem.SetData(goodsId, _cost.Num);
        }

    }
}
