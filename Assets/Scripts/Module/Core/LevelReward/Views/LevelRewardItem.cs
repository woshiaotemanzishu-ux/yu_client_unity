using Shenxiao.Generated.UI.LevelReward;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.LevelReward
{
    /// <summary>
    /// 等级奖励行项(对标老客户端 levelReward/LevelRewardItem.ts):一个等级段——等级(_lb_lv)/剩余数(_lb_left_count)+
    /// 奖励格列表(_gp_item_con 克隆 LevelReward 奖励子项)+ 领取按钮(_img_get/_lb_get)/已领(_img_received)/红点(_img_red)/未达遮罩(_img_mask)。
    ///
    /// 降级:LevelRewardModel(领取状态/协议)、LevelReward 奖励子项均未移植 → 红点/已领/箭头模板隐藏;_img_get 领取打日志;
    /// SetData 仅落等级文本占位。由 LevelRewardView 列表克隆。
    /// </summary>
    public sealed class LevelRewardItem : LevelRewardItemBind
    {
        protected override void OnInit()
        {
            HideNode(_img_red);
            HideNode(_img_received);
            if (_tpl_ArrowComponent != null) _tpl_ArrowComponent.SetActive(false);
            BindBtn(_img_get, "领取等级奖励");
        }

        /// <summary>填等级奖励行(对标 SetData):等级/剩余数文本占位,奖励格与领取态待 LevelRewardModel。</summary>
        public void SetData(int level, int leftCount)
        {
            if (_lb_lv != null) _lb_lv.text = level.ToString();
            if (_lb_left_count != null) _lb_left_count.text = leftCount.ToString();
        }

        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("LevelReward", "点击[{0}] → 待对接", label));
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
