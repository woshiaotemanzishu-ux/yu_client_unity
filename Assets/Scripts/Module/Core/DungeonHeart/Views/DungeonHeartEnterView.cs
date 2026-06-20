using System;
using Shenxiao.Generated.UI.DungeonHeart;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.DungeonHeart
{
    /// <summary>
    /// 心魔副本入口(对标老客户端 dungeonHeart/DungeonHeartEnterView.ts):BOSS 名(bossName)/技能介绍(skillIcon/skillName/skillDes)/
    /// 怪物说明(monsterText1..3)+ 进入(enterBtn)/关闭(closeBtn)。
    ///
    /// 降级:副本数据(DungeonHeartModel)/进入协议未移植 → 文本默认、closeBtn → Hide、enterBtn → TODO、OnShow TODO。
    /// 事件驱动弹窗,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class DungeonHeartEnterView : DungeonHeartEnterViewBind
    {
        protected override void OnInit()
        {
            BindBtn(closeBtn, () => Hide());
            BindBtn(enterBtn, () => GameLog.Info("DungeonHeart", "进入心魔副本 → 待对接 进入协议"));
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("DungeonHeart", "心魔副本入口打开 → 待对接 副本数据(DungeonHeartModel)");
        }

        private void BindBtn(Component target, Action onClick)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, onClick);
        }
    }
}
