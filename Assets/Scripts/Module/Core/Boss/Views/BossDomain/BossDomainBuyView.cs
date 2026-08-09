using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Bossdomain;

namespace Shenxiao.Module.Core.Boss.Views.BossDomain
{
    public sealed class BossDomainBuyView : BossDomainBuyViewBind
    {
        protected override void OnInit()
        {
            if (closeBtn != null) UIUtil.AddClick(closeBtn, Hide);
            if (_btn_cancal != null) UIUtil.AddClick(_btn_cancal, Hide);
            if (_btn_ok != null) UIUtil.AddClick(_btn_ok, Buy);
        }

        private void Buy()
        {
            KfBossController.Instance.BuyDecorationCount();
            Hide();
        }
    }
}
