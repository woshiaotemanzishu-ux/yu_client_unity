using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.DungeonRune;

namespace Shenxiao.Module.Core.Dungeon.Views.DungeonRune
{
    public sealed class DungeonRuneDailyRewardView : DungeonRuneDailyRewardViewBind
    {
        private bool _subscribed;

        protected override void OnInit()
        {
            if (_img_close != null) UIUtil.AddClick(_img_close, Hide);
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
            if (_img_get != null) _img_get.gameObject.SetActive(false);
            if (_img_continue != null) _img_continue.gameObject.SetActive(false);
            if (_panel_reward != null) _panel_reward.gameObject.SetActive(false);
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            DungeonController.Instance.RequestDungeonRuneDailyStatus();
            Refresh();
        }

        protected override void OnHide() => Unsubscribe();
        protected override void OnDispose() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On(GlobalEvent.EVT_DUNGEON_UPDATE, Refresh);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off(GlobalEvent.EVT_DUNGEON_UPDATE, Refresh);
            _subscribed = false;
        }

        private void Refresh()
        {
            if (!IsShown) return;
            // 老端这里展示“当前 rune floor”和当前关推荐战力比较；61115 的 unlock_level
            // 是另一条解锁状态，不能代替这两个语义。缺权威字段时安全隐藏，不写调试占位。
            if (_lb_floor != null) _lb_floor.gameObject.SetActive(false);
            if (_img_red != null) _img_red.gameObject.SetActive(false);
        }
    }
}
