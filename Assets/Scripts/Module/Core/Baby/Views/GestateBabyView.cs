using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Baby;

namespace Shenxiao.Module.Core.Baby
{
    /// <summary>宝宝未激活时的孕育页，仅负责激活请求。</summary>
    public partial class GestateBabyView : GestateBabyViewBind
    {
        protected override void OnShow(object args)
        {
            UIUtil.AddClick(gestateBtn, OnActivate);
            UIUtil.AddClick(closeBtn, BabyFlow.Close);
        }

        protected override void OnHide()
        {
            UIUtil.ClearClicks(gestateBtn != null ? gestateBtn.GetComponent<UnityEngine.UI.Image>() : null);
            UIUtil.ClearClicks(closeBtn);
        }

        private void OnActivate()
        {
            BabyController.Instance.RequestActivate();
        }

    }
}
