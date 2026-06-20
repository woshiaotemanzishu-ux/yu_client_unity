using Shenxiao.Generated.UI.BrightSea;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Daily
{
    /// <summary>
    /// 无尽之海·入口页(对标老客户端 brightSea/BrightSeaEnterView.ts,DailyView 标签2「无尽之海」内容):角色位(_item1.._item8)+
    /// 巡航记录(_gp_cruise_log)/抢夺列表(_gp_plunder_list)+ 倒计时(_con_time)+ 进入/操作(_btn_opt)+ 红点(_img_red_plunder)+ 跨服模式(_box_kf)。
    ///
    /// 降级:BrightSeaModel/无尽之海协议、BrightSeaRoleItem 列表项未移植 → 红点/模板隐藏、角色位空;巡航/抢夺/进入按钮打日志降级。
    /// 无独立关闭按钮(每日窗框统一关闭)→ 作 DailyView 标签2 内容由 DailyFlow 跨模块 reparent 进窗框内容区。事件驱动,不进 FirstPass。
    /// </summary>
    public sealed class BrightSeaEnterView : BrightSeaEnterViewBind
    {
        protected override void OnInit()
        {
            HideNode(_img_red_plunder);
            if (_tpl_BrightSeaRoleItem != null) _tpl_BrightSeaRoleItem.SetActive(false);
            BindBtn(_btn_opt, "无尽之海·进入/操作");
            BindBtn(_gp_cruise_log, "无尽之海·巡航记录");
            BindBtn(_gp_plunder_list, "无尽之海·抢夺列表");
        }

        protected override void OnShow(object args)
        {
            // 老端 open → 读 BrightSeaModel 铺角色位/记录/倒计时。数据未移植 → 默认降级。
            GameLog.Info("Daily", "无尽之海页打开 → 待对接 BrightSeaModel(默认降级)");
        }

        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("Daily", "点击[{0}] → 待对接", label));
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
