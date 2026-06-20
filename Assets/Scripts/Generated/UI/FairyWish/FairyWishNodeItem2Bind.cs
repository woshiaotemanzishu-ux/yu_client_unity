// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/fairyWish/FairyWishNodeItem2.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.FairyWish
{
    public partial class FairyWishNodeItem2Bind : BaseView
    {
        public RectTransform _click;
        public Image _bg2;
        public Image _bg;
        public Image _img_node;
        public Image _img_select;
        public Image _img_red;
        public TextMeshProUGUI _lb_text;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_click), _click);
            EnsureBound(nameof(_bg2), _bg2);
            EnsureBound(nameof(_bg), _bg);
            EnsureBound(nameof(_img_node), _img_node);
            EnsureBound(nameof(_img_select), _img_select);
            EnsureBound(nameof(_img_red), _img_red);
            EnsureBound(nameof(_lb_text), _lb_text);
        }
    }
}
