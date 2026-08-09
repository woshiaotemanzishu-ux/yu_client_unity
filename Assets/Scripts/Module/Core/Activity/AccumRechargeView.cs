using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Generated.UI.Activity;
using Shenxiao.Module.Core.CustomActivity;
using UnityEngine;

namespace Shenxiao.Module.Core.Activity
{
    public sealed class AccumRechargeView : AccumRechargeViewBind
    {
        private readonly List<GameObject> _cells = new List<GameObject>();
        private CustomActivityModel.ActEntry _info;

        protected override void OnInit()
        {
            if (_tpl_AccumRechargeItem != null) _tpl_AccumRechargeItem.SetActive(false);
        }

        protected override void OnShow(object args)
        {
            _info = args as CustomActivityModel.ActEntry;
            EventDispatcher.On<int, int>(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, OnDetail);
            EventDispatcher.On<int, int, int>(GlobalEvent.EVT_CUSTOMACT_RESULT, OnResult);
            if (_lb_desc != null) _lb_desc.text = _info?.Desc ?? string.Empty;
            if (_info != null) CustomActivityController.Instance.RequestActDetail(_info.BaseType, _info.SubType);
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
            if (_info != null && baseType == _info.BaseType && subType == _info.SubType) Refresh();
        }

        private void OnResult(int baseType, int subType, int code)
        {
            if (_info != null && baseType == _info.BaseType && subType == _info.SubType) Refresh();
        }

        private void Refresh()
        {
            ActivityViewUtil.HideAndDestroy(_cells);
            if (_info == null || _group_item == null || _group_item.content == null || _tpl_AccumRechargeItem == null) return;
            foreach (CustomActivityModel.DetailReward reward in ActivityViewUtil.Ordered(CustomActivityModel.Instance.GetDetail(_info.BaseType, _info.SubType)))
            {
                GameObject go = Instantiate(_tpl_AccumRechargeItem, _group_item.content);
                go.name = "AccumRechargeItem_" + reward.Grade;
                AccumRechargeItem item = go.GetComponent<AccumRechargeItem>();
                if (item != null) { item.Show(); item.SetData(_info, reward); }
                else go.SetActive(true);
                _cells.Add(go);
            }
            ActivityViewUtil.ResetTop(_group_item);
        }
    }
}
