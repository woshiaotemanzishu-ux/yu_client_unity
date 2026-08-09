using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Generated.UI.Activity;
using Shenxiao.Module.Core.CustomActivity;
using UnityEngine;

namespace Shenxiao.Module.Core.Activity
{
    public sealed class DailySupplyView : DailySupplyViewBind
    {
        private readonly List<GameObject> _cells = new List<GameObject>();
        private CustomActivityModel.ActEntry _info;

        protected override void OnInit()
        {
            if (_tpl_DailySupplyItem != null) _tpl_DailySupplyItem.SetActive(false);
        }

        protected override void OnShow(object args)
        {
            _info = args as CustomActivityModel.ActEntry;
            EventDispatcher.On<int, int>(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, OnDetail);
            EventDispatcher.On<int, int, int>(GlobalEvent.EVT_CUSTOMACT_RESULT, OnResult);
            if (_info != null) CustomActivityController.Instance.RequestActDetail(_info.BaseType, _info.SubType);
            CustomActivityController.Instance.RequestDailySupplyLiveness();
            Refresh();
        }

        protected override void OnHide()
        {
            EventDispatcher.Off<int, int>(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, OnDetail);
            EventDispatcher.Off<int, int, int>(GlobalEvent.EVT_CUSTOMACT_RESULT, OnResult);
            ActivityViewUtil.HideAndDestroy(_cells);
        }

        private void OnDetail(int baseType, int subType)
        {
            if (baseType == 61) Refresh();
        }

        private void OnResult(int baseType, int subType, int code)
        {
            if (_info != null && baseType == _info.BaseType && subType == _info.SubType) Refresh();
        }

        private void Refresh()
        {
            ActivityViewUtil.HideAndDestroy(_cells);
            int liveness = CustomActivityModel.Instance.DailySupplyLiveness;
            if (dailyLb != null) dailyLb.text = liveness.ToString();
            if (tipsLabel != null) tipsLabel.text = _info?.Desc ?? string.Empty;
            if (_info == null || _Scroller1 == null || _Scroller1.content == null || _tpl_DailySupplyItem == null) return;
            foreach (CustomActivityModel.DetailReward reward in ActivityViewUtil.Ordered(CustomActivityModel.Instance.GetDetail(_info.BaseType, _info.SubType)))
            {
                GameObject go = Instantiate(_tpl_DailySupplyItem, _Scroller1.content);
                go.name = "DailySupplyItem_" + reward.Grade;
                DailySupplyItem item = go.GetComponent<DailySupplyItem>();
                if (item != null) { item.Show(); item.SetData(_info, reward, liveness); }
                else go.SetActive(true);
                _cells.Add(go);
            }
            ActivityViewUtil.ResetTop(_Scroller1);
        }
    }
}
