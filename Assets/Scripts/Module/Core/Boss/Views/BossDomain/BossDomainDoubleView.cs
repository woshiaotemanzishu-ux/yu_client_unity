using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Bossdomain;

namespace Shenxiao.Module.Core.Boss.Views.BossDomain
{
    public sealed class BossDomainDoubleView : BossDomainDoubleViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
            if (close != null) UIUtil.AddClick(close, Hide);
        }

        protected override void OnShow(object args)
        {
            bool active = KfBossModel.Instance.DecorationInBuff;
            if (state != null) state.text = active ? "当前已使用双倍掉落卡" : "未使用";
            if (btn != null) btn.gameObject.SetActive(!active);
        }
    }
}
