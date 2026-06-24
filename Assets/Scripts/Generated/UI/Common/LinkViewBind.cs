// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/common/LinkView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Common
{
    public partial class LinkViewBind : BaseView
    {
        public Image _bg1;
        public Image _bg2;
        public RectTransform _Group1;
        public Image _Image2;
        public Image _Image3;
        public Image btnClose;
        public ScrollRect Content;
        public GameObject _tpl_LinkViewItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_bg1), _bg1);
            EnsureBound(nameof(_bg2), _bg2);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(btnClose), btnClose);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_tpl_LinkViewItem), _tpl_LinkViewItem);
        }
    }
}
