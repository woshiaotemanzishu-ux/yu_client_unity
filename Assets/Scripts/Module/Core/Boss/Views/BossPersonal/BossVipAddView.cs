using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.BossPersonal;

namespace Shenxiao.Module.Core.Boss.Views.BossPersonal
{
    public sealed class BossVipAddView : BossVipAddViewBind
    {
        protected override void OnInit()
        {
            if (_btn_close != null) UIUtil.AddClick(_btn_close, Hide);
            if (_btn_go != null) UIUtil.AddClick(_btn_go, OpenVip);
        }

        protected override void OnShow(object args)
        {
            // 老端以 main_role.vip_flag/card 语义选择 VIP/SVIP 分支；当前 Unity 只有 VipLevel，
            // 两者不可互相代替。权威接口缺失时隐藏动态分支，不猜测玩家状态。
            if (_gp_vip != null) _gp_vip.gameObject.SetActive(false);
            if (_gp_svip != null) _gp_svip.gameObject.SetActive(false);
            if (_lb_tips != null) _lb_tips.text = "VIP挑战次数状态待权威接口";
        }

        private static void OpenVip()
        {
            GameLog.Info("BossPersonal", "VIP/SVIP 跳转属于跨模块路由，当前仅登记 blocker");
        }
    }
}
