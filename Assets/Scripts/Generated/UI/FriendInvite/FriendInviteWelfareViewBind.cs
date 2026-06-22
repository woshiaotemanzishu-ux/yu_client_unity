// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/friendInvite/FriendInviteWelfareView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.FriendInvite
{
    public partial class FriendInviteWelfareViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_role;
        public Image _img_title;
        public RectTransform _gp_get;
        public Image _Image1;
        public TextMeshProUGUI get_label;
        public Image red_dot;
        public RectTransform _gp_hook;
        public Image _Image2;
        public Image _img_hook;
        public TextMeshProUGUI _Label1;
        public ScrollRect Content;
        public Image _img_close;
        public RectTransform _gp_show_effect;
        public GameObject _tpl_FriendInviteWelfareItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_role), _img_role);
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_gp_get), _gp_get);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(get_label), get_label);
            EnsureBound(nameof(red_dot), red_dot);
            EnsureBound(nameof(_gp_hook), _gp_hook);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_img_hook), _img_hook);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_gp_show_effect), _gp_show_effect);
            EnsureBound(nameof(_tpl_FriendInviteWelfareItem), _tpl_FriendInviteWelfareItem);
        }
    }
}
