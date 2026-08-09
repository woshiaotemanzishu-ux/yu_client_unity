using System;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.BossPersonal;
using Shenxiao.Module.Core.Dungeon;

namespace Shenxiao.Module.Core.Boss.Views.BossPersonal
{
    public sealed class BossPersonalItem : BossPersonalItemBind
    {
        public sealed class Args
        {
            public readonly DungeonModel.DunState State;
            public readonly int Index;
            public readonly Action<DungeonModel.DunState> Click;
            public Args(DungeonModel.DunState state, int index, Action<DungeonModel.DunState> click)
            { State = state; Index = index; Click = click; }
        }

        private DungeonModel.DunState _state;
        private Action<DungeonModel.DunState> _click;
        public int DunId => _state?.DunId ?? 0;

        protected override void OnInit()
        {
            if (_Image1 != null) UIUtil.AddClick(_Image1, () => _click?.Invoke(_state));
        }

        protected override void OnShow(object args)
        {
            Args data = args as Args;
            if (data == null || data.State == null) return;
            _state = data.State;
            _click = data.Click;
            if (_lb_bossName != null) _lb_bossName.text = DungeonConfigs.GetName(_state.DunId);
            if (_lb_level != null) _lb_level.text = "";
            if (_lb_order != null) _lb_order.text = (data.Index + 1).ToString();
            // 老端使用 ex_data key=10/first_flag；当前 DunState 未保留该字段，不能用 DailyCount 猜测。
            if (_img_red != null) _img_red.gameObject.SetActive(false);
            if (_img_first != null) _img_first.gameObject.SetActive(false);
        }

        public void SetSelected(bool selected)
        {
            if (_img_select != null) _img_select.gameObject.SetActive(selected);
        }
    }
}
