// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/ftvInvest/FtvInvestRewardView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.FtvInvest
{
    public partial class FtvInvestRewardViewBind : BaseView
    {
        public RectTransform _rew_box;
        public Image _img_get;
        public Image _img_red;
        public TextMeshProUGUI _lab_day;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_rew_box), _rew_box);
            EnsureBound(nameof(_img_get), _img_get);
            EnsureBound(nameof(_img_red), _img_red);
            EnsureBound(nameof(_lab_day), _lab_day);
        }
    }
}
