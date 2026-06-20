using System;
using Shenxiao.Generated.UI.VipActive;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.VipActive
{
    /// <summary>
    /// VIP活动界面(对标老客户端 vipActive/VipActiveView.ts):页签(VipActiveTab)+ 价格(oldPriceLabel/nowPriceLabel)/
    /// 倒计时(_lb_count_time)+ 模型/特效展示 + 进入/购买(enterBtn)+ 关闭(closeBtn)。
    ///
    /// 降级:VIP活动数据/购买协议未移植 → 页签模板隐藏、closeBtn → Hide、enterBtn → TODO、OnShow TODO。
    /// 事件驱动弹窗,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class VipActiveView : VipActiveViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_vipActiveTab != null) _tpl_vipActiveTab.SetActive(false);
            BindBtn(closeBtn, () => Hide());
            BindBtn(enterBtn, () => GameLog.Info("VipActive", "进入/购买 VIP活动 → 待对接 购买协议"));
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("VipActive", "VIP活动界面打开 → 待对接 VIP活动数据(VipActiveModel)");
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
