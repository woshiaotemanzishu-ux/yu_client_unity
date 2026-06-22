// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/friendInvite/FriendInviteShopItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.FriendInvite
{
    public partial class FriendInviteShopItemBind : BaseView
    {
        public Image _Image1;
        public RectTransform _gp_award;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_tips;
        public TextMeshProUGUI _lb_count;
        public RectTransform _gp_buy;
        public Image _Image2;
        public RectTransform _Group1;
        public TextMeshProUGUI _lb_desc;
        public Image _img_icon;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_gp_award), _gp_award);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_tips), _lb_tips);
            EnsureBound(nameof(_lb_count), _lb_count);
            EnsureBound(nameof(_gp_buy), _gp_buy);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_lb_desc), _lb_desc);
            EnsureBound(nameof(_img_icon), _img_icon);
        }
    }
}
