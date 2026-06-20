// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/friend/EmailView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Friend
{
    public partial class EmailViewBind : BaseView
    {
        public Image _Image1;
        public RectTransform nullGroup;
        public Image _Image2;
        public TextMeshProUGUI _Label1;
        public ScrollRect itemScroller;
        public RectTransform Content;
        public RectTransform btnDelet;
        public RectTransform btnGet;
        public Image _Image3;
        public TextMeshProUGUI _Label2;
        public GameObject _tpl_EmailItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(nullGroup), nullGroup);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(itemScroller), itemScroller);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(btnDelet), btnDelet);
            EnsureBound(nameof(btnGet), btnGet);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(_Label2), _Label2);
            EnsureBound(nameof(_tpl_EmailItem), _tpl_EmailItem);
        }
    }
}
