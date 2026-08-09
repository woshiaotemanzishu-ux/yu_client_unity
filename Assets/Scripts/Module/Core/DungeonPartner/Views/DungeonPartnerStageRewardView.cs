using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.DungeonPartner;

namespace Shenxiao.Module.Core.DungeonPartner.Views
{
    public sealed class DungeonPartnerStageRewardView : DungeonPartnerStageRewardViewBind
    {
        private ushort _score;
        private byte _status;

        protected override void OnInit()
        {
            if (_tpl_EquipmentItem != null) _tpl_EquipmentItem.SetActive(false);
            if (_img_close != null) UIUtil.AddClick(_img_close, Hide);
        }

        public void Configure(ushort score, byte status)
        {
            _score = score;
            _status = status;
            Refresh();
        }

        protected override void OnShow(object args) => Refresh();

        private void Refresh()
        {
            if (_lb_title != null) _lb_title.text = _score + "星奖励";
            if (_lb_tip != null) _lb_tip.text = "本局总星数达到" + _score + "星即可领取";
            if (_lb_tip != null) _lb_tip.gameObject.SetActive(_status == 0);
        }
    }
}
