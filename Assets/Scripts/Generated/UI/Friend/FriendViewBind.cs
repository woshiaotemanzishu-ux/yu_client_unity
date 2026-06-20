// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/friend/FriendView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Friend
{
    public partial class FriendViewBind : BaseView
    {
        public Image _Image1;
        public RectTransform nullGroup;
        public Image _Image2;
        public Image _Image3;
        public TextMeshProUGUI _Label1;
        public TextMeshProUGUI _Label2;
        public Image _Image4;
        public ScrollRect itemScroller;
        public RectTransform Content;
        public TextMeshProUGUI numlabel;
        public RectTransform btnBlacklist;
        public RectTransform btnAdd;
        public RectTransform btnAplly;
        public Image redDot;
        public GameObject _tpl_FriendListItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(nullGroup), nullGroup);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_Label2), _Label2);
            EnsureBound(nameof(_Image4), _Image4);
            EnsureBound(nameof(itemScroller), itemScroller);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(numlabel), numlabel);
            EnsureBound(nameof(btnBlacklist), btnBlacklist);
            EnsureBound(nameof(btnAdd), btnAdd);
            EnsureBound(nameof(btnAplly), btnAplly);
            EnsureBound(nameof(redDot), redDot);
            EnsureBound(nameof(_tpl_FriendListItem), _tpl_FriendListItem);
        }
    }
}
