// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/shop/ShopMysteriousView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Shop
{
    public partial class ShopMysteriousViewBind : BaseView
    {
        public Image img_bg;
        public Image img_bg2;
        public ScrollRect scroll;
        public RectTransform Content;
        public RectTransform bottom_conta;
        public Image _Image11;
        public Image _Image2;
        public Image _Image3;
        public TextMeshProUGUI _Label1;
        public TextMeshProUGUI refreshTime;
        public RectTransform cost_conta;
        public RectTransform goods_icon;
        public TextMeshProUGUI num;
        public RectTransform refreshBtn;
        public Image _Image1;
        public TextMeshProUGUI labelDisplay;
        public GameObject _tpl_ShopMysteriouItem;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(img_bg), img_bg);
            EnsureBound(nameof(img_bg2), img_bg2);
            EnsureBound(nameof(scroll), scroll);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(bottom_conta), bottom_conta);
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(refreshTime), refreshTime);
            EnsureBound(nameof(cost_conta), cost_conta);
            EnsureBound(nameof(goods_icon), goods_icon);
            EnsureBound(nameof(num), num);
            EnsureBound(nameof(refreshBtn), refreshBtn);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(_tpl_ShopMysteriouItem), _tpl_ShopMysteriouItem);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
