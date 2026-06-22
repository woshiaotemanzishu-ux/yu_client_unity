// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/competelist/CompetelistRankItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Competelist
{
    public partial class CompetelistRankItemBind : BaseView
    {
        public Image _Image1;
        public Image _rank_icon;
        public TextMeshProUGUI _lb_rank;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_1;
        public TextMeshProUGUI _lb_integral;
        public ScrollRect _Scroller1;
        public RectTransform _gp_reward;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_rank_icon), _rank_icon);
            EnsureBound(nameof(_lb_rank), _lb_rank);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_1), _lb_1);
            EnsureBound(nameof(_lb_integral), _lb_integral);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(_gp_reward), _gp_reward);
        }
    }
}
