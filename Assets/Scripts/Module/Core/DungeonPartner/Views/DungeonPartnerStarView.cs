using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.DungeonPartner;

namespace Shenxiao.Module.Core.DungeonPartner.Views
{
    public sealed class DungeonPartnerStarView : DungeonPartnerStarViewBind
    {
        private byte _level = 1;
        private int _totalScore;
        private DungeonPartnerStageRewardView _stageRewardView;
        private bool _subscribed;

        protected override void OnInit()
        {
            if (_img_reward1 != null) UIUtil.AddClick(_img_reward1, () => OpenReward(0));
            if (_img_reward2 != null) UIUtil.AddClick(_img_reward2, () => OpenReward(1));
            if (_img_reward3 != null) UIUtil.AddClick(_img_reward3, () => OpenReward(2));
            _stageRewardView = transform.parent != null
                ? transform.parent.GetComponentInChildren<DungeonPartnerStageRewardView>(true)
                : null;
            if (_stageRewardView != null) _stageRewardView.Hide();
        }

        public void Configure(byte level, int totalScore)
        {
            _level = level;
            _totalScore = totalScore;
            Refresh(level);
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            DungeonPartnerController.Instance.RequestStageRewards(_level);
            Refresh(_level);
        }

        protected override void OnHide()
        {
            Unsubscribe();
            if (_stageRewardView != null) _stageRewardView.Hide();
        }

        protected override void OnDispose() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed) return;
            DungeonPartnerModel.Instance.StageRewardsChanged += Refresh;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            DungeonPartnerModel.Instance.StageRewardsChanged -= Refresh;
            _subscribed = false;
        }

        private void Refresh(byte level)
        {
            if (level != _level) return;
            if (_img_progress != null) _img_progress.fillAmount = UnityEngine.Mathf.Clamp01(_totalScore / 27f);
            if (!DungeonPartnerModel.Instance.TryGetStageRewards(level, out DungeonPartnerModel.StageRewardSnapshot snapshot)) return;
            for (int i = 0; i < 3; i++)
            {
                byte status = i < snapshot.Entries.Count ? snapshot.Entries[i].Status : (byte)0;
                SetRewardState(i, status);
            }
        }

        private void SetRewardState(int index, byte status)
        {
            UnityEngine.UI.Image got = index == 0 ? _img_reward_get1 : index == 1 ? _img_reward_get2 : _img_reward_get3;
            UnityEngine.UI.Image red = index == 0 ? _img_reward_red1 : index == 1 ? _img_reward_red2 : _img_reward_red3;
            if (got != null) got.gameObject.SetActive(status == 2);
            if (red != null) red.gameObject.SetActive(status == 1);
        }

        private void OpenReward(int index)
        {
            if (_stageRewardView == null || !DungeonPartnerModel.Instance.TryGetStageRewards(_level, out DungeonPartnerModel.StageRewardSnapshot snapshot)) return;
            if (index < 0 || index >= snapshot.Entries.Count) return;
            DungeonPartnerModel.StageRewardEntry entry = snapshot.Entries[index];
            if (entry.Status == 1) return;
            _stageRewardView.Configure(entry.Score, entry.Status);
            _stageRewardView.Show();
        }
    }
}
