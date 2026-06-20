// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/friend/FriendApllyPopView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Friend
{
    public partial class FriendApllyPopViewBind : BaseView
    {
        public Image _Image1;
        public Image _Image3;
        public RectTransform nullGroup;
        public Image _Image4;
        public TextMeshProUGUI _Label1;
        public RectTransform _Group1;
        public Image _Image5;
        public TextMeshProUGUI _Label2;
        public RectTransform btnClose;
        public ScrollRect itemScroller;
        public RectTransform Content;
        public RectTransform passBtn;
        public RectTransform rejectBtn;
        public GameObject _tpl_FriendApllyPopItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(nullGroup), nullGroup);
            EnsureBound(nameof(_Image4), _Image4);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_Image5), _Image5);
            EnsureBound(nameof(_Label2), _Label2);
            EnsureBound(nameof(btnClose), btnClose);
            EnsureBound(nameof(itemScroller), itemScroller);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(passBtn), passBtn);
            EnsureBound(nameof(rejectBtn), rejectBtn);
            EnsureBound(nameof(_tpl_FriendApllyPopItem), _tpl_FriendApllyPopItem);
        }
    }
}
