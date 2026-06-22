// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kfSingleRank/kfSRMainRankItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.KfSingleRank
{
    public partial class KfSRMainRankItemBind : BaseView
    {
        public Image _Image1;
        public TextMeshProUGUI _lb_layer;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_time;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_lb_layer), _lb_layer);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_time), _lb_time);
        }
    }
}
