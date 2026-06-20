// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/marriage/MarriageDropList.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Marriage
{
    public partial class MarriageDropListBind : BaseView
    {
        public Image _img_bg;
        public ScrollRect _scroller;
        public RectTransform Content;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_scroller), _scroller);
            EnsureBound(nameof(Content), Content);
        }
    }
}
