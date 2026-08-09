using Shenxiao.Generated.UI.DungeonRune;

namespace Shenxiao.Module.Core.Dungeon.Views.DungeonRune
{
    public sealed class DungeonRuneTargetItem : DungeonRuneTargetItemBind
    {
        public void SetData(DungeonModel.RuneRewardEntry entry)
        {
            if (entry == null) return;
            // 文案来自老端专属配置 desc；协议原始 dun_id/reward_type 不能拼成玩家文案。
            if (_html_desc != null) _html_desc.gameObject.SetActive(false);
            if (_img_received != null) _img_received.gameObject.SetActive(entry.RewardStatus != 0);
            if (_box_get != null) _box_get.gameObject.SetActive(false);
            if (_panel_item != null) _panel_item.gameObject.SetActive(false);
        }
    }
}
