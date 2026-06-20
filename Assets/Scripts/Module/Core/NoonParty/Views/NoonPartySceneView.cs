using System;
using Shenxiao.Generated.UI.NoonParty;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.NoonParty
{
    /// <summary>
    /// 午时狂欢场景界面(对标老客户端 noonParty/NoonPartySceneView.ts):活动场景 HUD —— 普通/稀有宝箱进度(nomalName/rareName/
    /// nomalBoxLb/rareBoxLb)+ 时间(timeLb)/经验(expLb)/复活(reliveLb)+ 说明(instructionBtn)/退出(exitBtn)。
    ///
    /// 降级:活动数据/协议(NoonPartyModel)未移植 → 文本默认、instructionBtn/exitBtn 点击打 TODO、OnShow TODO。
    /// 事件驱动,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class NoonPartySceneView : NoonPartySceneViewBind
    {
        protected override void OnInit()
        {
            BindBtn(exitBtn, () => GameLog.Info("NoonParty", "退出午时狂欢 → 待对接 退出协议"));
            BindBtn(instructionBtn, () => GameLog.Info("NoonParty", "玩法说明 → 待对接 说明面板"));
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("NoonParty", "午时狂欢场景 HUD 打开 → 待对接 活动数据(NoonPartyModel)");
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
