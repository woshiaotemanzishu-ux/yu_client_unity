using Shenxiao.Generated.UI.LevelReward;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.LevelReward
{
    /// <summary>
    /// 等级奖励主界面(对标老客户端 levelReward/LevelRewardView.ts):标题(_img_title)+ 提示(_img_tips/_Label1)+
    /// 等级奖励列表(_Scroller1/_list_item_con 克隆 LevelRewardItem,每行一个等级段的奖励)。
    ///
    /// 降级:LevelRewardModel/等级奖励配置/领取协议、LevelRewardItem 列表项均未移植 → 列表空(不铺行)、标题/提示走预制体默认;
    /// 本视图无关闭/动作按钮 → 由二级 HUD 等级奖励按钮再点关闭(LevelRewardFlow.Toggle)。事件驱动窗口,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class LevelRewardView : LevelRewardViewBind
    {
        protected override void OnShow(object args)
        {
            // 老端 open → 读 LevelRewardModel 铺等级奖励行。数据未移植 → 列表空降级。
            GameLog.Info("LevelReward", "等级奖励打开 → 待对接 LevelRewardModel(列表空降级)");
        }
    }
}
