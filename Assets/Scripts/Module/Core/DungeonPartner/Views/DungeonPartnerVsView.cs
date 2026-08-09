using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.DungeonPartner;

namespace Shenxiao.Module.Core.DungeonPartner.Views
{
    public sealed class DungeonPartnerVsView : DungeonPartnerVsViewBind
    {
        private DungeonPartnerModel.DungeonEntry _entry;

        protected override void OnInit()
        {
            if (_img_close != null) UIUtil.AddClick(_img_close, Hide);
        }

        public void Configure(DungeonPartnerModel.DungeonEntry entry) => _entry = entry;
    }
}
