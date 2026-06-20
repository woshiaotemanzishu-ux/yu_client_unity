using System;
using Shenxiao.Generated.UI.TestAct;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.TestAct
{
    /// <summary>
    /// 测试活动基础界面(对标老客户端 testAct/TestActBaseView.ts):登录/充值两子页(loginGp/rechargeGp,含 TestActLoginView/TestActRecharge)
    /// + 页签切换(btn_1/btn_2)+ 关闭(_btn_close)。
    ///
    /// 降级:活动数据未移植 → 子页模板隐藏、btn_1/btn_2 切页 → TODO、_btn_close → Hide、OnShow TODO。
    /// 事件驱动弹窗,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class TestActBaseView : TestActBaseViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_TestActLoginView != null) _tpl_TestActLoginView.SetActive(false);
            if (_tpl_TestActRecharge != null) _tpl_TestActRecharge.SetActive(false);
            BindBtn(_btn_close, () => Hide());
            BindBtn(btn_1, () => GameLog.Info("TestAct", "切登录页 → 待对接 活动数据"));
            BindBtn(btn_2, () => GameLog.Info("TestAct", "切充值页 → 待对接 活动数据"));
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("TestAct", "测试活动界面打开 → 待对接 活动数据");
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
