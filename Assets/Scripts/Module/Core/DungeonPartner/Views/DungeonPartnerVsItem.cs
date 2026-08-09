using Shenxiao.Generated.UI.DungeonPartner;

namespace Shenxiao.Module.Core.DungeonPartner.Views
{
    public sealed class DungeonPartnerVsItem : DungeonPartnerVsItemBind
    {
        protected override void OnInit()
        {
            if (_tpl_DungeonPartnerVsRewardItem != null) _tpl_DungeonPartnerVsRewardItem.SetActive(false);
        }

        public void SetStar(byte star)
        {
            if (_lb_title != null) _lb_title.text = "首次" + star + "星通关";
        }
    }
}
