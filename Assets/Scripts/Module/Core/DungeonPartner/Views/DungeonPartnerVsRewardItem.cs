using Shenxiao.Generated.UI.DungeonPartner;

namespace Shenxiao.Module.Core.DungeonPartner.Views
{
    public sealed class DungeonPartnerVsRewardItem : DungeonPartnerVsRewardItemBind
    {
        protected override void OnInit()
        {
            if (_tpl_EquipmentItem != null) _tpl_EquipmentItem.SetActive(false);
        }

        public void SetClaimed(bool claimed)
        {
            if (_img_get != null) _img_get.gameObject.SetActive(claimed);
        }
    }
}
