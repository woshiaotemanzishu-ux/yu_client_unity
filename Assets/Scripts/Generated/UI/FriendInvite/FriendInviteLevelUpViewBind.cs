// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/friendInvite/FriendInviteLevelUpView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.FriendInvite
{
    public partial class FriendInviteLevelUpViewBind : BaseView
    {
        public Image _img_bg;
        public ScrollRect Content;
        public RectTransform _Group2;
        public Image _Image1;
        public TextMeshProUGUI _Label1;
        public TextMeshProUGUI _Label2;
        public Image _img_box_shop;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_Group2), _Group2);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_Label2), _Label2);
            EnsureBound(nameof(_img_box_shop), _img_box_shop);
        }
    }
}
