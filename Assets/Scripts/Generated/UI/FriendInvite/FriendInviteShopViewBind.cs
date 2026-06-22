// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/friendInvite/FriendInviteShopView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.FriendInvite
{
    public partial class FriendInviteShopViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_close;
        public Image _img_sub_bg;
        public Image _img_title;
        public ScrollRect Content1;
        public RectTransform Content;
        public RectTransform _Group1;
        public TextMeshProUGUI _lb_count;
        public Image _img_icon;
        public GameObject _tpl_FriendInviteShopItem;
        public GameObject _tpl_FriendInviteTabItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_img_sub_bg), _img_sub_bg);
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(Content1), Content1);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_lb_count), _lb_count);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_tpl_FriendInviteShopItem), _tpl_FriendInviteShopItem);
            EnsureBound(nameof(_tpl_FriendInviteTabItem), _tpl_FriendInviteTabItem);
        }
    }
}
