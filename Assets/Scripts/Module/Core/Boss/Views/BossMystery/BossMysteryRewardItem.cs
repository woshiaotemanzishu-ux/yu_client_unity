using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.BossMystery;

namespace Shenxiao.Module.Core.Boss.Views.BossMystery
{
    public sealed class BossMysteryRewardItem : BossMysteryRewardItemBind
    {
        private int _rewardId;

        protected override void OnInit()
        {
            if (getBtn != null) UIUtil.AddClick(getBtn, Claim);
        }

        public void SetData(int rewardId, int killBossNum)
        {
            _rewardId = rewardId;
            KfBossModel model = KfBossModel.Instance;
            bool claimed = model.GreatDemonHadRewardStages.Contains(rewardId);
            bool claimable = model.GreatDemonKillNum >= killBossNum && !claimed;
            if (tips != null) tips.text = $"击杀{killBossNum}个首领";
            if (getBtn != null) getBtn.gameObject.SetActive(claimable);
            if (red_dot != null) red_dot.gameObject.SetActive(claimable);
            if (get_state != null) get_state.gameObject.SetActive(claimed);
            if (none != null) none.gameObject.SetActive(!claimed && !claimable);
        }

        private void Claim()
        {
            if (_rewardId > 0) KfBossController.Instance.TakeGreatDemonReward(_rewardId);
        }
    }
}
