using Shenxiao.Generated.UI.Activity;
using Shenxiao.Module.Core.CustomActivity;

namespace Shenxiao.Module.Core.Activity
{
    public sealed class DailySupplyItem : DailySupplyItemBind
    {
        private CustomActivityModel.ActEntry _info;
        private CustomActivityModel.DetailReward _reward;

        protected override void OnInit()
        {
            if (_tpl_EquipmentItem != null) _tpl_EquipmentItem.SetActive(false);
            ActivityViewUtil.BindClick(getBtn, OnClick);
        }

        public void SetData(CustomActivityModel.ActEntry info, CustomActivityModel.DetailReward reward, int liveness)
        {
            _info = info;
            _reward = reward;
            int need = ActivityViewUtil.ConditionInt(reward.Condition, "liveness", ActivityViewUtil.ConditionInt(reward.Condition, "active"));
            if (title != null) title.text = string.IsNullOrEmpty(reward.Desc) ? reward.Name : reward.Desc;
            if (labelDisplay != null) labelDisplay.text = reward.Status == 1 ? "领取" : "前往";
            SetVisible(getBtn, reward.Status != 2);
            SetVisible(recImg, reward.Status == 2);
            SetVisible(reddot, reward.Status == 1);
            SetVisible(UnfinishImg, reward.Status == 0 && need > 0 && liveness < need);
        }

        private void OnClick()
        {
            if (_info == null || _reward == null) return;
            if (_reward.Status == 1) CustomActivityController.Instance.RequestClaim(_info.BaseType, _info.SubType, _reward.Grade);
            else if (_reward.Status == 0) ActivityViewUtil.OpenDaily();
        }

        private static void SetVisible(UnityEngine.Component component, bool visible)
        {
            if (component != null) component.gameObject.SetActive(visible);
        }
    }
}
