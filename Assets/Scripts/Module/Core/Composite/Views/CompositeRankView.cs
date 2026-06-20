using Shenxiao.Generated.UI.Composite;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Composite
{
    /// <summary>
    /// 合成排行弹窗(对标老客户端 composite/CompositeRankView.ts):从装备合成主面板「排行」按钮(rankBtn)打开,
    /// 显合成榜玩家排名(_Label1.._Label10 等名次/名字/数据行)+ 标题(lb_title)+ 自身角色展示(_Imagerole/topimg)+ 关闭(closeBtn)。
    ///
    /// 降级:CompositeModel/排行协议数据未移植 → 名次行走默认(空/0)、角色展示默认;closeBtn → Hide 关闭返回合成主面板。
    /// 由 CompositeEquipView.rankBtn 经 CompositeFlow.OpenSub 打开(叠主面板上)。事件驱动窗口,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class CompositeRankView : CompositeRankViewBind
    {
        protected override void OnInit()
        {
            if (closeBtn != null)
            {
                closeBtn.raycastTarget = true;
                UIUtil.AddClick(closeBtn, Hide);
            }
        }

        protected override void OnShow(object args)
        {
            // 老端 open → 请求合成排行 + 铺名次行。数据未移植 → 名次/角色默认降级。
            GameLog.Info("Composite", "合成排行打开 → 待对接 CompositeModel 排行数据(默认降级)");
        }
    }
}
