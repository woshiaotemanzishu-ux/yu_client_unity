// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/friendInvite/FriendInviteBoostView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.FriendInvite
{
    public partial class FriendInviteBoostViewBind : BaseView
    {
        public Image _img_bg;
        public RectTransform _lb_exchange;
        public TextMeshProUGUI _Label1;
        public TextMeshProUGUI _Label2;
        public TextMeshProUGUI _Label3;
        public TextMeshProUGUI _lb_count;
        public Image goods;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_lb_exchange), _lb_exchange);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_Label2), _Label2);
            EnsureBound(nameof(_Label3), _Label3);
            EnsureBound(nameof(_lb_count), _lb_count);
            EnsureBound(nameof(goods), goods);
        }
    }
}
