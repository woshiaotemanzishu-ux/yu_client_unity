using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.DungeonPartner;
using UnityEngine;

namespace Shenxiao.Module.Core.DungeonPartner.Views
{
    public sealed class DungeonPartnerView : DungeonPartnerViewBind
    {
        private DungeonPartnerStarView _starView;
        private DungeonPartnerSweepView _sweepView;
        private DungeonPartnerVsView _vsView;
        private DungeonPartnerFirstKillView _firstKillView;
        private int _totalScore;
        private bool _subscribed;

        protected override void OnInit()
        {
            if (_tpl_DungeonPartnerItem != null) _tpl_DungeonPartnerItem.SetActive(false);
            if (_tpl_EquipmentItem != null) _tpl_EquipmentItem.SetActive(false);
            if (_img_star != null) UIUtil.AddClick(_img_star, ShowStarView);
            if (_box_sweep != null) UIUtil.AddClick(_box_sweep, ShowSweepView);

            Transform moduleRoot = transform.parent;
            _starView = moduleRoot != null ? moduleRoot.GetComponentInChildren<DungeonPartnerStarView>(true) : null;
            _sweepView = moduleRoot != null ? moduleRoot.GetComponentInChildren<DungeonPartnerSweepView>(true) : null;
            _vsView = moduleRoot != null ? moduleRoot.GetComponentInChildren<DungeonPartnerVsView>(true) : null;
            _firstKillView = moduleRoot != null ? moduleRoot.GetComponentInChildren<DungeonPartnerFirstKillView>(true) : null;
            if (_starView != null) _starView.Hide();
            if (_sweepView != null) _sweepView.Hide();
            if (_vsView != null) _vsView.Hide();
            if (_firstKillView != null) _firstKillView.Hide();
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            byte page = DungeonPartnerModel.Instance.CurrentPage;
            DungeonPartnerController.Instance.RequestDungeons(page);
            Refresh(page);
        }

        protected override void OnHide()
        {
            Unsubscribe();
            if (_starView != null) _starView.Hide();
            if (_sweepView != null) _sweepView.Hide();
            if (_vsView != null) _vsView.Hide();
            if (_firstKillView != null) _firstKillView.Hide();
        }

        protected override void OnDispose() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed) return;
            DungeonPartnerModel.Instance.DungeonsChanged += Refresh;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            DungeonPartnerModel.Instance.DungeonsChanged -= Refresh;
            _subscribed = false;
        }

        private void Refresh(byte level)
        {
            if (level != DungeonPartnerModel.Instance.CurrentPage) return;
            _totalScore = DungeonPartnerModel.Instance.GetTotalScore(level);
            if (_starView != null) _starView.Configure(level, _totalScore);
        }

        private void ShowStarView()
        {
            if (_starView == null) return;
            byte level = DungeonPartnerModel.Instance.CurrentPage;
            _starView.Configure(level, _totalScore);
            _starView.Show();
        }

        private void ShowSweepView()
        {
            if (_sweepView != null) _sweepView.Show();
        }
    }
}
