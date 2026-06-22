// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/evening/EveningRankRewardItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Evening
{
    public partial class EveningRankRewardItemBind : BaseView
    {
        public Image _Image1;
        public RectTransform _Group1;
        public Image _img_rank;
        public TextMeshProUGUI _lb_rank;
        public ScrollRect _Scroller1;
        public RectTransform Content;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_img_rank), _img_rank);
            EnsureBound(nameof(_lb_rank), _lb_rank);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(Content), Content);
        }
    }
}
