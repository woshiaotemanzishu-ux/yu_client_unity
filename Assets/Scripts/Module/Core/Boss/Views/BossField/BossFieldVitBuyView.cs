using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.BossField;

namespace Shenxiao.Module.Core.Boss.Views.BossField
{
    public sealed class BossFieldVitBuyView : BossFieldVitBuyViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
            if (_tpl_WithBtnHSlider != null) _tpl_WithBtnHSlider.SetActive(false);
            if (btn_close != null) UIUtil.AddClick(btn_close, Hide);
            if (btn_cancel != null) UIUtil.AddClick(btn_cancel, Hide);
            if (btn_enter != null) UIUtil.AddClick(btn_enter,
                () => GameLog.Info("BossField", "体力找回缺少真实 slider 值，拒绝猜测发送 46045"));
        }
        protected override void OnShow(object args)
        {
            BossModel.VitInfo vit = BossModel.Instance.GetVit(BossModel.BossType.Field);
            int back = vit?.BackVit ?? 0;
            if (lb_1 != null) lb_1.text = "可找回体力 " + back;
            if (lb_2 != null) lb_2.text = back > 0 ? "请选择找回数量" : "暂无可找回体力";
            if (lb_price != null) lb_price.text = "--";
            if (lb_price_2 != null) lb_price_2.text = "--";
        }
    }
}
