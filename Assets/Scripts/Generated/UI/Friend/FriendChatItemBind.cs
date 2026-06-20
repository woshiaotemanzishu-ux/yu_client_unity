// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/friend/FriendChatItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Friend
{
    public partial class FriendChatItemBind : BaseView
    {
        public RectTransform nameGroup;
        public RectTransform titleGroup;
        public Image _Image1;
        public TextMeshProUGUI chLabel;
        public Image _vip_icon;
        public TextMeshProUGUI nameLabel;
        public TextMeshProUGUI _lb_time;
        public Image chatBg_img;
        public RectTransform chatBg;
        public RectTransform SpriteGraphic;
        public TextMeshProUGUI contentLabel;
        public RectTransform head;
        public GameObject _tpl_CustomHeadItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(nameGroup), nameGroup);
            EnsureBound(nameof(titleGroup), titleGroup);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(chLabel), chLabel);
            EnsureBound(nameof(_vip_icon), _vip_icon);
            EnsureBound(nameof(nameLabel), nameLabel);
            EnsureBound(nameof(_lb_time), _lb_time);
            EnsureBound(nameof(chatBg_img), chatBg_img);
            EnsureBound(nameof(chatBg), chatBg);
            EnsureBound(nameof(SpriteGraphic), SpriteGraphic);
            EnsureBound(nameof(contentLabel), contentLabel);
            EnsureBound(nameof(head), head);
            EnsureBound(nameof(_tpl_CustomHeadItem), _tpl_CustomHeadItem);
        }
    }
}
