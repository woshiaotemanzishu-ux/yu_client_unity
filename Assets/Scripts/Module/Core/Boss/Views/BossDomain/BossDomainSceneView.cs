using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Bossdomain;
using UnityEngine;

namespace Shenxiao.Module.Core.Boss.Views.BossDomain
{
    public sealed class BossDomainSceneView : BossDomainSceneViewBind
    {
        private BossDomainDoubleView _doubleView;
        private BossDomainHelpAlert _alert;
        private BossDomainResultView _result;
        private bool _subscribed;

        protected override void OnInit()
        {
            if (_tpl_BossDomainScenePanel != null) _tpl_BossDomainScenePanel.SetActive(false);
            if (_btn_help != null) UIUtil.AddClick(_btn_help, KfBossController.Instance.RequestDecorationGuildHelp);
            if (gp_double != null) UIUtil.AddClick(gp_double, ShowDoubleView);
            Transform root = transform.root;
            _doubleView = root.GetComponentInChildren<BossDomainDoubleView>(true);
            _alert = root.GetComponentInChildren<BossDomainHelpAlert>(true);
            _result = root.GetComponentInChildren<BossDomainResultView>(true);
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            KfBossController.Instance.RequestDecorationSceneInfo();
            KfBossController.Instance.RequestDecorationRank();
        }

        protected override void OnHide()
        {
            Unsubscribe();
            if (_doubleView != null) _doubleView.Hide();
            if (_result != null) _result.Hide();
        }

        protected override void OnDispose() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On(GlobalEvent.EVT_KFBOSS_DECORATION_SETTLE, ShowResult);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off(GlobalEvent.EVT_KFBOSS_DECORATION_SETTLE, ShowResult);
            _subscribed = false;
        }

        private void ShowDoubleView()
        {
            if (_doubleView != null) _doubleView.Show();
        }

        private void ShowResult()
        {
            if (_result != null) _result.Show();
        }
    }
}
