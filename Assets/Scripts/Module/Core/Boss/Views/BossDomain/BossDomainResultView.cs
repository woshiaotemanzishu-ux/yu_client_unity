using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Bossdomain;

namespace Shenxiao.Module.Core.Boss.Views.BossDomain
{
    public sealed class BossDomainResultView : BossDomainResultViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_EquipmentItem != null) _tpl_EquipmentItem.SetActive(false);
            if (_bg != null) UIUtil.AddClick(_bg, Hide);
        }

        protected override void OnShow(object args)
        {
            KfBossModel.DecorationSettleResult result = KfBossModel.Instance.LastDecorationSettle;
            if (result == null) return;
            if (_gp_tip_img != null) _gp_tip_img.gameObject.SetActive(result.IsBelong);
            if (_gp_tip_img0 != null) _gp_tip_img0.gameObject.SetActive(result.IsDouble);
        }
    }
}
