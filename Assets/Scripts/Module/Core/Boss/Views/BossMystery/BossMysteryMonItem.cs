using System;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.BossMystery;

namespace Shenxiao.Module.Core.Boss.Views.BossMystery
{
    public sealed class BossMysteryMonItem : BossMysteryMonItemBind
    {
        private BossModel.BossEntry _data;

        protected override void OnInit()
        {
            if (_img_bg != null) UIUtil.AddClick(_img_bg, () => _click?.Invoke());
        }

        private Action _click;

        public void SetData(BossModel.BossEntry data, bool selected, Action click)
        {
            _data = data;
            _click = click;
            if (_lb_bossName != null) _lb_bossName.text = "Boss " + data.BossId;
            if (_lb_level != null) _lb_level.text = "";
            if (_img_dead != null) _img_dead.gameObject.SetActive(!data.IsAlive);
            if (refresh_con != null) refresh_con.gameObject.SetActive(!data.IsAlive);
            if (refresh_time != null) refresh_time.text = data.IsAlive ? "00:00" : data.RebornTime.ToString();
            SetSelected(selected);
        }

        public void SetSelected(bool selected)
        {
            if (_img_select != null) _img_select.gameObject.SetActive(selected);
        }
    }
}
