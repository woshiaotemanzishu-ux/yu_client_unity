using System;
using Shenxiao.Generated.UI.TestAct;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.TestAct
{
    /// <summary>
    /// 测试活动充值页(对标老客户端 testAct/TestActRecharge.ts):充值说明(_lb_desc..3/_lb_recharge)+ 前往充值(go_btn)+ 问号说明(_btn_ques)。
    ///
    /// SetDesc(text)。降级:充值协议未移植 → go_btn/_btn_ques 点击打 TODO、文本即时。由 TestActBaseView 克隆。
    /// </summary>
    public sealed class TestActRecharge : TestActRechargeBind
    {
        protected override void OnInit()
        {
            BindBtn(go_btn, () => GameLog.Info("TestAct", "前往充值 → 待对接 充值入口"));
            BindBtn(_btn_ques, () => GameLog.Info("TestAct", "充值说明 → 待对接 说明面板"));
        }

        public void SetDesc(string desc)
        {
            if (_lb_desc != null) _lb_desc.text = desc ?? "";
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
