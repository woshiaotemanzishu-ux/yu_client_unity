using System;
using Shenxiao.Generated.UI.MainStronger;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.MainStronger
{
    /// <summary>
    /// 变强引导界面(对标老客户端 mainUI/MainUIStrongerView.ts):变强功能列表(MainUIStrongerBtn)+ 广告位(_panel_ad)+
    /// 关闭(btnClose),点击背景关闭。
    ///
    /// 降级:功能列表数据(MainStronger 配置)+ LoopScrowViewMgr + 广告未移植 → 功能按钮模板隐藏、列表空、
    /// btnClose → Hide、OnShow 打 TODO。事件驱动弹窗,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class MainUIStrongerView : MainUIStrongerViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_MainUIStrongerBtn != null) _tpl_MainUIStrongerBtn.SetActive(false);
            BindBtn(btnClose, () => Hide());
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("MainStronger", "变强引导打开 → 待对接 变强功能列表(MainStronger 配置/LoopScrowViewMgr)");
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
