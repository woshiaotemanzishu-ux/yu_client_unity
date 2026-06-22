// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/arena/ArenaRankRewardView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Arena
{
    public partial class ArenaRankRewardViewBind : BaseView
    {
        public Image _Image1;
        public ScrollRect _Scroller1;
        public RectTransform _Group1;
        public TextMeshProUGUI _Label1;
        public TextMeshProUGUI _lb_my_rank;
        public TextMeshProUGUI _Label2;
        public GameObject _tpl_ArenaRankRewardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_lb_my_rank), _lb_my_rank);
            EnsureBound(nameof(_Label2), _Label2);
            EnsureBound(nameof(_tpl_ArenaRankRewardItem), _tpl_ArenaRankRewardItem);
        }
    }
}
