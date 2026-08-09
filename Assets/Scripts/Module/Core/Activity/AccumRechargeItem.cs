using Shenxiao.Generated.UI.Activity;
using Shenxiao.Module.Core.CustomActivity;

namespace Shenxiao.Module.Core.Activity
{
    public sealed class AccumRechargeItem : AccumRechargeItemBind
    {
        private CustomActivityModel.ActEntry _info;
        private CustomActivityModel.DetailReward _reward;

        protected override void OnInit()
        {
            if (_tpl_CommonRewardItem != null) _tpl_CommonRewardItem.SetActive(false);
            if (_tpl_EquipmentItem != null) _tpl_EquipmentItem.SetActive(false);
            ActivityViewUtil.BindClick(_btn_get, Claim);
            ActivityViewUtil.BindClick(_btn_recharge, ActivityViewUtil.OpenRecharge);
        }

        public void SetData(CustomActivityModel.ActEntry info, CustomActivityModel.DetailReward reward)
        {
            _info = info;
            _reward = reward;
            if (_lb_title != null) _lb_title.text = string.IsNullOrEmpty(reward.Desc) ? reward.Name : reward.Desc;
            int need = ActivityViewUtil.ConditionInt(reward.Condition, "gold");
            int current = CustomActivityModel.Instance.TodayRechargeGold;
            if (_lb_progress != null) _lb_progress.text = need > 0 ? current + "/" + need : string.Empty;
            if (_lb_task_pg != null) _lb_task_pg.text = reward.ReceiveTimes > 0 ? "已领取 " + reward.ReceiveTimes + " 次" : string.Empty;
            SetVisible(_btn_get, reward.Status == 1);
            SetVisible(_btn_recharge, reward.Status == 0);
            SetVisible(_img_over, reward.Status == 2);
            SetVisible(_reddot, reward.Status == 1);
        }

        private void Claim()
        {
            if (_info != null && _reward != null && _reward.Status == 1)
                CustomActivityController.Instance.RequestClaim(_info.BaseType, _info.SubType, _reward.Grade);
        }

        private static void SetVisible(UnityEngine.Component component, bool visible)
        {
            if (component != null) component.gameObject.SetActive(visible);
        }
    }
}
