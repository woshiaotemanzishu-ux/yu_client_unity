using Shenxiao.Generated.UI.LevelReward;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.LevelReward
{
    /// <summary>
    /// 等级奖励·单个奖励格(对标老客户端 levelReward/LevelReward.ts):一个奖励图标(_gp_reward 克隆 EquipmentItem)+
    /// 已领遮罩(_img_mask)/可领标(_img_draw)/限制标(_img_limit)。由 LevelRewardItem 行内克隆铺设。
    ///
    /// 降级:GoodsModel/领取状态未移植 → 遮罩/可领/限制标隐藏、奖励图标模板(_tpl_EquipmentItem)隐藏;SetData 待对接。
    /// </summary>
    public sealed class LevelReward : LevelRewardBind
    {
        protected override void OnInit()
        {
            HideNode(_img_mask);
            HideNode(_img_draw);
            HideNode(_img_limit);
            if (_tpl_EquipmentItem != null) _tpl_EquipmentItem.SetActive(false);
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
