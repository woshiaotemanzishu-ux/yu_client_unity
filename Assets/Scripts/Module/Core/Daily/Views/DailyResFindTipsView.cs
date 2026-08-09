using Shenxiao.Generated.UI.Daily;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using Shenxiao.Common.Tips;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Daily
{
    /// <summary>
    /// 资源找回·确认弹窗(对标老客户端 daily/DailyResFindTipsView.ts):标题(title)+ 价格(price)+ 数量滑条
    /// (slider_group/_tpl_WithBtnHSlider)+ 取消(cancleBtn)/确认(confirmBtn)/关闭(closeBtn)。由
    /// DailyResFindItem 的找回按钮经 DailyFlow.OpenSub 叠开。
    ///
    /// 当前保留“全额找回”简化路径：type 默认 2=免费，type=1=绑玉由父页选择；两种写入均在确认后发送，
    /// type=1 明确提示绑玉不足时可能消耗勾玉。老端滑杆的正常/超额次数分段和精确价格依赖 config_res_act，
    /// 该表尚未纳入 Daily 配置闭包，因此滑条模板仍隐藏并作为运行收口 blocker。
    /// </summary>
    public sealed class DailyResFindTipsView : DailyResFindTipsViewBind
    {
        /// <summary>由 DailyResFindItem.findBtn 点击前写入,弹窗读取后清空。</summary>
        public static DailyModel.ResFindVo Pending;
        public static int PendingType = 2;

        private DailyModel.ResFindVo _vo;
        private int _moneyType = 2;

        protected override void OnInit()
        {
            if (_tpl_WithBtnHSlider != null) _tpl_WithBtnHSlider.SetActive(false);
            if (closeBtn != null) { closeBtn.raycastTarget = true; UIUtil.AddClick(closeBtn, Hide); }
            BindClick(cancleBtn, Hide);
            BindClick(confirmBtn, OnConfirm);
        }

        protected override void OnShow(object args)
        {
            _vo = Pending;
            _moneyType = PendingType;
            Pending = null;
            PendingType = 2;
            if (_vo == null)
            {
                if (title != null) title.text = "资源找回";
                if (price != null) price.text = "";
                return;
            }
            if (title != null) title.text = "资源找回 " + _vo.ActId + "@" + _vo.ActSub;
            int times = _moneyType == 1 ? _vo.Lefttimes + _vo.LefttimesVip : _vo.Lefttimes;
            if (price != null) price.text = (_moneyType == 1 ? "绑玉找回 " : "免费找回 ") + times + " 次";
        }

        private void OnConfirm()
        {
            if (_vo == null)
            {
                GameLog.Warn("Daily", "资源找回确认:未选中任何行");
                Hide();
                return;
            }
            int displayTimes = _moneyType == 1 ? _vo.Lefttimes + _vo.LefttimesVip : _vo.Lefttimes;
            int times = _vo.Lefttimes;
            int vipTimes = _moneyType == 1 ? _vo.LefttimesVip : 0;
            string text = _moneyType == 1
                ? "是否使用绑玉找回该奖励？\n（绑玉不足时可能消耗勾玉代替）"
                : "是否免费找回该奖励？";
            TipsManager.Confirm(text, () =>
            {
                DailyController.Instance.ResFind(_vo.ActId, _vo.ActSub, _moneyType, times, vipTimes);
                GameLog.Info("Daily", "点击[资源找回·确认] act={0}@{1} type={2} display={3} times={4}+{5}", _vo.ActId, _vo.ActSub, _moneyType, displayTimes, times, vipTimes);
                Hide();
            });
        }

        private static void BindClick(Component target, System.Action onClick)
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
