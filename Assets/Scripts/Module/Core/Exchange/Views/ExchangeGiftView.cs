using System;
using Shenxiao.Generated.UI.Exchange;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Exchange
{
    /// <summary>
    /// 兑换码界面(对标老客户端 exchange/ExchangeGiftView.ts):兑换码输入框(_ti_input + Placeholder/Text)+ 领取(_btn_receive)
    /// + 错误提示(_lb_error)+ 链接说明(_lb_url)。
    ///
    /// 降级:兑换协议(ExchangeModel)未移植 → _btn_receive 点击打 TODO、错误提示清空、OnShow 清错误。
    /// 输入框由预制体上的 TMP_InputField 承载(用户可输入)。事件驱动弹窗,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class ExchangeGiftView : ExchangeGiftViewBind
    {
        protected override void OnInit()
        {
            if (_lb_error != null) _lb_error.text = "";
            BindBtn(_btn_receive, () => GameLog.Info("Exchange", "兑换码领取 → 待对接 兑换协议(ExchangeModel)"));
        }

        protected override void OnShow(object args)
        {
            if (_lb_error != null) _lb_error.text = "";
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
