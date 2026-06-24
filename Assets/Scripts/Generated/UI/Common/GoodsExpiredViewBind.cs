// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/common/GoodsExpiredView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Common
{
    public partial class GoodsExpiredViewBind : BaseView
    {
        public Image bg;
        public Image _Image1;
        public Image _Image2;
        public TextMeshProUGUI _lb_title;
        public TextMeshProUGUI contentText;
        public ScrollRect goods_scroller;
        public RectTransform Content;
        public Image _close_btn;
        public RectTransform _cancel_btn;
        public Image _img_cancel;
        public TextMeshProUGUI _lb_cancel;
        public RectTransform _ok_btn;
        public Image _img_ok;
        public TextMeshProUGUI _lb_ok;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(contentText), contentText);
            EnsureBound(nameof(goods_scroller), goods_scroller);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_close_btn), _close_btn);
            EnsureBound(nameof(_cancel_btn), _cancel_btn);
            EnsureBound(nameof(_img_cancel), _img_cancel);
            EnsureBound(nameof(_lb_cancel), _lb_cancel);
            EnsureBound(nameof(_ok_btn), _ok_btn);
            EnsureBound(nameof(_img_ok), _img_ok);
            EnsureBound(nameof(_lb_ok), _lb_ok);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
