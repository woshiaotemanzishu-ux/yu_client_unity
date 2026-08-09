using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Activity;
using Shenxiao.Module.Core.CustomActivity;

namespace Shenxiao.Module.Core.Activity
{
    public sealed class CreatRoleGiftView : CreatRoleGiftViewBind
    {
        private CustomActivityModel.ActEntry _info;
        private CustomActivityModel.DetailReward _reward;
        private long _endTime;
        private bool _endRefreshRequested;

        protected override void OnInit()
        {
            if (_tpl_AccumRechargeItem != null) _tpl_AccumRechargeItem.SetActive(false);
            if (_tpl_CommonRewardItem != null) _tpl_CommonRewardItem.SetActive(false);
            ActivityViewUtil.BindClick(_gp_up, Claim);
        }

        protected override void OnShow(object args)
        {
            _info = args as CustomActivityModel.ActEntry;
            _endRefreshRequested = false;
            EventDispatcher.On<int, int>(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, OnDetail);
            EventDispatcher.On<int, int, int>(GlobalEvent.EVT_CUSTOMACT_RESULT, OnResult);
            if (_info != null) CustomActivityController.Instance.RequestActDetail(_info.BaseType, _info.SubType);
            Refresh();
        }

        protected override void OnHide()
        {
            EventDispatcher.Off<int, int>(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, OnDetail);
            EventDispatcher.Off<int, int, int>(GlobalEvent.EVT_CUSTOMACT_RESULT, OnResult);
        }

        private void Update()
        {
            if (!IsShown || _reward == null || _reward.Status == 2) return;
            if (!_endRefreshRequested && _reward.Status == 0 && _endTime > 0 && TimeUtil.NowSec() >= _endTime && _info != null)
            {
                _endRefreshRequested = true;
                CustomActivityController.Instance.RequestActDetail(_info.BaseType, _info.SubType);
            }
            UpdateState();
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
            CustomActivityModel.DetailData detail = _info == null ? null : CustomActivityModel.Instance.GetDetail(_info.BaseType, _info.SubType);
            _reward = detail != null && detail.RewardList.Count > 0 ? detail.RewardList[0] : null;
            _endTime = _reward == null ? 0 : ActivityViewUtil.FirstConditionInt(_reward.Condition);
            if (_endTime == 0 && _info != null) _endTime = _info.Etime;
            UpdateState();
        }

        private void UpdateState()
        {
            bool claimed = _reward != null && _reward.Status == 2;
            bool ready = _reward != null && _reward.Status == 1;
            if (_lb_time2 != null) _lb_time2.text = claimed ? string.Empty : ActivityViewUtil.Remaining(_endTime);
            if (_lb_btn != null) _lb_btn.text = ready ? "领取" : "等待开启";
            if (_gp_up != null) _gp_up.gameObject.SetActive(!claimed);
            if (_img_got != null) _img_got.gameObject.SetActive(claimed);
            if (_img_red != null) _img_red.gameObject.SetActive(ready && !claimed);
            if (_box_eff != null) _box_eff.gameObject.SetActive(ready && !claimed);
        }

        private void Claim()
        {
            if (_info == null || _reward == null || _reward.Status != 1) return;
            CustomActivityController.Instance.RequestClaim(_info.BaseType, _info.SubType, _reward.Grade);
        }
    }
}
