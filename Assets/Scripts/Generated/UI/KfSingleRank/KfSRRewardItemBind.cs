// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kfSingleRank/kfSRRewardItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.KfSingleRank
{
    public partial class KfSRRewardItemBind : BaseView
    {
        public Image _Image1;
        public TextMeshProUGUI _lb_layer;
        public ScrollRect Content1;
        public ScrollRect Content2;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_lb_layer), _lb_layer);
            EnsureBound(nameof(Content1), Content1);
            EnsureBound(nameof(Content2), Content2);
        }
    }
}
