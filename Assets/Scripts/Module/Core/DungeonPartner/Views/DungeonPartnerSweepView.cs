using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.DungeonPartner;

namespace Shenxiao.Module.Core.DungeonPartner.Views
{
    public sealed class DungeonPartnerSweepView : DungeonPartnerSweepViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_DungeonPartnerSweepItem != null) _tpl_DungeonPartnerSweepItem.SetActive(false);
            if (_img_close != null) UIUtil.AddClick(_img_close, Hide);
        }
    }
}
