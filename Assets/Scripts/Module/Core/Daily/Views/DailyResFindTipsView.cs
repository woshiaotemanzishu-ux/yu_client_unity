using Shenxiao.Generated.UI.Daily;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Daily
{
    /// <summary>
    /// 资源找回·确认弹窗(对标老客户端 daily/DailyResFindTipsView.ts):标题(title)+ 价格(price)+ 数量滑条
    /// (slider_group/_tpl_WithBtnHSlider)+ 取消(cancleBtn)/确认(confirmBtn)/关闭(closeBtn)。由
    /// DailyResFindItem 的找回按钮经 DailyFlow.OpenSub 叠开。
    ///
    /// UI 简化(规格§0 裁决):老端"滑杆吃正常/超额次数二段配额+绑钻不足换算勾玉二次确认"未接线,
    /// 简化为"全额找回"一键——按 <see cref="Pending"/> 的 Lefttimes/LefttimesVip 全量传给 41903。
    /// ⚠轮10交叉验收 blocker 订正:type 默认改为 2=金币/免费(老端 resFindCheck 默认 [false,true] →
    /// money_type 默认 2,DailyResFindView.ts:48/76-91);此前误默认 1=绑钻,会在玩家毫无确认的情况下直接
    /// 扣绑钻。type=1 付费路径待 config_res_act 导入、能判定金币/绑钻分支后再开,且必须先加二次确认弹窗
    /// (老端付费路径必带 Alert)。滑条模板仍隐藏。
    /// </summary>
    public sealed class DailyResFindTipsView : DailyResFindTipsViewBind
    {
        /// <summary>由 DailyResFindItem.findBtn 点击前写入,弹窗读取后清空。</summary>
        public static DailyModel.ResFindVo Pending;

        private DailyModel.ResFindVo _vo;

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
            Pending = null;
            if (_vo == null)
            {
                if (title != null) title.text = "资源找回";
                if (price != null) price.text = "";
                return;
            }
            if (title != null) title.text = "资源找回 " + _vo.ActId + "@" + _vo.ActSub;
            if (price != null) price.text = "全额找回 " + (_vo.Lefttimes + _vo.LefttimesVip) + " 次";
        }

        private void OnConfirm()
        {
            if (_vo == null)
            {
                GameLog.Warn("Daily", "资源找回确认:未选中任何行");
                Hide();
                return;
            }
            DailyController.Instance.ResFind(_vo.ActId, _vo.ActSub, 2, _vo.Lefttimes, _vo.LefttimesVip);
            GameLog.Info("Daily", "点击[资源找回·确认] act={0}@{1} times={2}+{3}", _vo.ActId, _vo.ActSub, _vo.Lefttimes, _vo.LefttimesVip);
            Hide();
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
