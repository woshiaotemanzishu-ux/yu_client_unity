using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Bossdomain;

namespace Shenxiao.Module.Core.Boss.Views.BossDomain
{
    public sealed class BossDomainStageShow : BossDomainStageShowBind
    {
        protected override void OnInit()
        {
            if (_tpl_EquipmentItem != null) _tpl_EquipmentItem.SetActive(false);
            if (bg != null) UIUtil.AddClick(bg, Hide);
        }

        protected override void OnShow(object args)
        {
            if (scroller != null) scroller.verticalNormalizedPosition = 1f;
        }
    }
}
