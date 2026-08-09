using Shenxiao.Generated.UI.Bossdomain;

namespace Shenxiao.Module.Core.Boss.Views.BossDomain
{
    public sealed class BossDomainRewardItem : BossDomainRewardItemBind
    {
        public void SetState(bool available)
        {
            if (icon1 != null) icon1.gameObject.SetActive(available);
            if (icon2 != null) icon2.gameObject.SetActive(!available);
        }
    }
}
