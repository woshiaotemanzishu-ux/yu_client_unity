using System;
using Shenxiao.Generated.UI.Pray;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Pray
{
    /// <summary>
    /// 祈福项(对标老客户端 pray/PrayItemView.ts):祈福按钮(_gp_btn,免费/金币祈愿)+ 获得(_lb_gain)/花费(_lb_goldcost)/
    /// 剩余次数(_lb_rem_num)/总次数(_lb_total_num)/倒计时(_lb_countdown)/红点(_img_red)/图标(_img_icon)。
    ///
    /// 降级:PrayModel/祈福协议/配置未移植 → 红点/特效隐藏、文本默认、_gp_btn 点击打 TODO。由 PrayMainView 排布。
    /// </summary>
    public sealed class PrayItemView : PrayItemViewBind
    {
        protected override void OnInit()
        {
            if (_img_red != null) _img_red.gameObject.SetActive(false);
            if (_gp_effect != null) _gp_effect.gameObject.SetActive(false);
            if (_gp_strike_effect != null) _gp_strike_effect.gameObject.SetActive(false);
            BindBtn(_gp_btn, () => GameLog.Info("Pray", "祈福 → 待对接 PrayModel 祈福协议"));
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
