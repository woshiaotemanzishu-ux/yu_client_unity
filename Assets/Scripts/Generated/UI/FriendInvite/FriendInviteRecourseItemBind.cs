// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/friendInvite/FriendInviteRecourseItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.FriendInvite
{
    public partial class FriendInviteRecourseItemBind : BaseView
    {
        public Image _Image1;
        public Image _img_title_bg;
        public TextMeshProUGUI _lb_desc;
        public ScrollRect _Scroller1;
        public RectTransform Content;
        public Image _img_receive;
        public RectTransform _btn_get;
        public Image _Image11;
        public TextMeshProUGUI labelDisplay;
        public Image _img_red;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_img_title_bg), _img_title_bg);
            EnsureBound(nameof(_lb_desc), _lb_desc);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_img_receive), _img_receive);
            EnsureBound(nameof(_btn_get), _btn_get);
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(_img_red), _img_red);
        }
    }
}
