using System;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.DungeonTower;

namespace Shenxiao.Module.Core.BaseDungeon
{
    /// <summary>限时塔关卡格业务接管；目录配置未入库前仅提供真实选择/通过态承载，不生成假条目。</summary>
    public sealed class DungeonTowerItemView : DungeonTowerItemBind
    {
        private uint _dungeonId;
        private Action<uint> _selected;

        protected override void OnInit()
        {
            if (_box_pos != null) UIUtil.AddClick(_box_pos, OnClick);
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
        }

        public void SetData(uint dungeonId, int index, bool isPassed, Action<uint> selected)
        {
            _dungeonId = dungeonId;
            _selected = selected;
            if (_lb_title != null) _lb_title.text = "第" + index + "关";
            if (_img_got != null) _img_got.gameObject.SetActive(isPassed);
            if (_img_select != null) _img_select.gameObject.SetActive(false);
            if (_box_reward != null) _box_reward.gameObject.SetActive(false);
        }

        public void SetSelected(bool selected)
        {
            if (_img_select != null) _img_select.gameObject.SetActive(selected);
        }

        private void OnClick()
        {
            if (_dungeonId != 0) _selected?.Invoke(_dungeonId);
        }
    }
}
