// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kf1vn/Kf1vnTicketRewardItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Kf1vn
{
    public partial class Kf1vnTicketRewardItemBind : BaseView
    {
        public Image bg_img;
        public Image rank_icon;
        public TextMeshProUGUI rank_label;
        public ScrollRect _Scroller1;
        public RectTransform Content;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg_img), bg_img);
            EnsureBound(nameof(rank_icon), rank_icon);
            EnsureBound(nameof(rank_label), rank_label);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(Content), Content);
        }
    }
}
