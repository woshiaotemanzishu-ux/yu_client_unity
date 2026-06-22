// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/bossdomain/BossDomainRewardItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Bossdomain
{
    public partial class BossDomainRewardItemBind : BaseView
    {
        public Image box;
        public RectTransform gpReward;
        public Image icon1;
        public Image icon2;

        protected override void BindNodes()
        {
            EnsureBound(nameof(box), box);
            EnsureBound(nameof(gpReward), gpReward);
            EnsureBound(nameof(icon1), icon1);
            EnsureBound(nameof(icon2), icon2);
        }
    }
}
