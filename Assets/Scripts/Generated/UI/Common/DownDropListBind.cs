// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/common/DownDropList.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Common
{
    public partial class DownDropListBind : BaseView
    {
        public Image _bg;
        public Image _img_bg;
        public ScrollRect _scroller;
        public RectTransform content;
        public GameObject _tpl_DownDropItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_bg), _bg);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_scroller), _scroller);
            EnsureBound(nameof(content), content);
            EnsureBound(nameof(_tpl_DownDropItem), _tpl_DownDropItem);
        }
    }
}
