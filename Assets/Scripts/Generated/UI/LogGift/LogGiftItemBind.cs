// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/logGift/LogGiftItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.LogGift
{
    public partial class LogGiftItemBind : BaseView
    {
        public Image _Image1;
        public Image _Image2;
        public TextMeshProUGUI _lb_desc;
        public ScrollRect _Scroller1;
        public RectTransform Content;
        public RectTransform _gp_btn;
        public RectTransform _btn_draw;
        public Image _Image;
        public TextMeshProUGUI labelDisplay;
        public Image _img_red;
        public Image _img_is_draw;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_lb_desc), _lb_desc);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_gp_btn), _gp_btn);
            EnsureBound(nameof(_btn_draw), _btn_draw);
            EnsureBound(nameof(_Image), _Image);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(_img_red), _img_red);
            EnsureBound(nameof(_img_is_draw), _img_is_draw);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
