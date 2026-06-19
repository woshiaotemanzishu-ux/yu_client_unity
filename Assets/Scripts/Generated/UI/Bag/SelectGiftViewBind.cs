// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/bag/SelectGiftView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Bag
{
    public partial class SelectGiftViewBind : BaseView
    {
        public Image _Image1;
        public Image _Image2;
        public Image _Image3;
        public TextMeshProUGUI _lb_title;
        public RectTransform ok_btn;
        public Image _Image4;
        public TextMeshProUGUI labelDisplay;
        public Image close_btn;
        public TextMeshProUGUI num_text;
        public ScrollRect item_scroller;
        public RectTransform Content;
        public GameObject _tpl_SelectGiftItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(ok_btn), ok_btn);
            EnsureBound(nameof(_Image4), _Image4);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(close_btn), close_btn);
            EnsureBound(nameof(num_text), num_text);
            EnsureBound(nameof(item_scroller), item_scroller);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_tpl_SelectGiftItem), _tpl_SelectGiftItem);
        }
    }
}
