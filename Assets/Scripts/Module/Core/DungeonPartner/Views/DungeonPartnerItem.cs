using System;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.DungeonPartner;

namespace Shenxiao.Module.Core.DungeonPartner.Views
{
    public sealed class DungeonPartnerItem : DungeonPartnerItemBind
    {
        private DungeonPartnerModel.DungeonEntry _entry;
        private Action<DungeonPartnerModel.DungeonEntry> _challenge;
        private Action<DungeonPartnerModel.DungeonEntry> _firstKill;

        protected override void OnInit()
        {
            if (_box_model != null) UIUtil.AddClick(_box_model, Challenge);
            if (_box_vs != null) UIUtil.AddClick(_box_vs, Challenge);
            if (_img_first_kill != null) UIUtil.AddClick(_img_first_kill, FirstKill);
        }

        public void SetData(DungeonPartnerModel.DungeonEntry entry,
            Action<DungeonPartnerModel.DungeonEntry> challenge,
            Action<DungeonPartnerModel.DungeonEntry> firstKill)
        {
            _entry = entry;
            _challenge = challenge;
            _firstKill = firstKill;
            if (_img_finish != null) _img_finish.gameObject.SetActive(entry != null && entry.Score == 3);
        }

        private void Challenge() { if (_entry != null) _challenge?.Invoke(_entry); }
        private void FirstKill() { if (_entry != null) _firstKill?.Invoke(_entry); }
    }
}
