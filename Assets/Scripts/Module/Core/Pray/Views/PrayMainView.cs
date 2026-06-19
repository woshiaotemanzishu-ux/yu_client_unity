using Shenxiao.Generated.UI.Pray;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Pray
{
    /// <summary>
    /// 祈福主界面(对标老客户端 pray/PrayMainView.ts):祈福项列表(_gp_pray_item)+ 等级(_bit_lv)/经验条(_img_mask/_lb_exp)
    /// + 铜币/经验暴击标(money_baoji/exp_baoji)。
    ///
    /// 降级:PrayModel/role 信息(等级/经验/祈福项配置)未移植 → 等级/经验默认、暴击标隐藏、OnShow 打 TODO。
    /// 事件驱动弹窗,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class PrayMainView : PrayMainViewBind
    {
        protected override void OnInit()
        {
            if (money_baoji != null) money_baoji.gameObject.SetActive(false);
            if (exp_baoji != null) exp_baoji.gameObject.SetActive(false);
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("Pray", "祈福界面打开 → 待对接 祈福数据(PrayModel/role 信息)");
        }
    }
}
