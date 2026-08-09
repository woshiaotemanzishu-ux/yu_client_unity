using System;
using Shenxiao.Common.Tips;
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
    /// 输入框由预制体上的 TMP_InputField 承载。本轮只接只读视觉和空输入语义；15087 兑换、
    /// 服务端结果与 CongratulationObtainView 均为真实账号事务，保持 blocked，不在本 View 发送。
    /// </summary>
    public sealed class ExchangeGiftView : ExchangeGiftViewBind
    {
        private const string DefaultGiftWxName = "永夜2.5d";
        private const string DefaultGiftWxMark = "yyhx25d";
        public sealed class Presentation
        {
            public string GiftWxName;
            public string GiftWxMark;
        }

        protected override void OnInit()
        {
            SetErrorVisible(false);
            BindBtn(_btn_receive, Receive);
        }

        protected override void OnShow(object args)
        {
            SetErrorVisible(false);

            Presentation presentation = args as Presentation;
            string name = presentation != null ? presentation.GiftWxName : DefaultGiftWxName;
            string mark = presentation != null ? presentation.GiftWxMark : DefaultGiftWxMark;
            ApplyChannelMessage(name, mark);
        }

        protected override void OnHide()
        {
            SetErrorVisible(false);
        }

        private void Receive()
        {
            string cardNo = _input_text != null ? _input_text.text : "";
            if (string.IsNullOrEmpty(cardNo))
            {
                TipsManager.Toast("请输入兑换码");
                return;
            }

            SetErrorVisible(false);
            TipsManager.Toast("兑换功能尚未完成安全接入");
            // 15087 是一次性兑换码写事务：仅在台账枚举并 blocked，本轮不发送。
        }

        private void SetErrorVisible(bool visible)
        {
            if (_lb_error == null) return;
            if (!visible) _lb_error.text = "";
            _lb_error.gameObject.SetActive(visible);
        }

        private void ApplyChannelMessage(string name, string mark)
        {
            if (_lb_url == null) return;
            bool visible = !string.IsNullOrEmpty(name);
            _lb_url.gameObject.SetActive(visible);
            if (!visible)
            {
                _lb_url.text = "";
                return;
            }

            _lb_url.text = "关注官方微信“<color=#cf7000>" + name +
                           "</color>”(<color=#cf7000>微信号:" + (mark ?? "") +
                           "</color>)了解最新消息和活动";
        }

        private static void BindBtn(Component target, Action onClick)
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
