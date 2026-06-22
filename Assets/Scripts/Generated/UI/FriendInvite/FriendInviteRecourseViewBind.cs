// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/friendInvite/FriendInviteRecourseView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.FriendInvite
{
    public partial class FriendInviteRecourseViewBind : BaseView
    {
        public Image _img_bg;
        public RectTransform _Group3;
        public Image _Image1;
        public RectTransform _Group1;
        public Image _Image2;
        public Image _img_progress;
        public TextMeshProUGUI _lb_count;
        public TextMeshProUGUI _Label1;
        public TextMeshProUGUI _Label2;
        public Image _img_box_shop;
        public ScrollRect Content;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_Group3), _Group3);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_img_progress), _img_progress);
            EnsureBound(nameof(_lb_count), _lb_count);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_Label2), _Label2);
            EnsureBound(nameof(_img_box_shop), _img_box_shop);
            EnsureBound(nameof(Content), Content);
        }
    }
}
