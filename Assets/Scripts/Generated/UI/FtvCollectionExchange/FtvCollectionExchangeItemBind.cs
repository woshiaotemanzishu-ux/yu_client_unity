// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/ftvCollectionExchange/FtvCollectionExchangeItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.FtvCollectionExchange
{
    public partial class FtvCollectionExchangeItemBind : BaseView
    {
        public Image bg_img;
        public ScrollRect _scl_goods;
        public RectTransform _gp_goods;
        public RectTransform _gp_label;
        public TextMeshProUGUI label_person;
        public TextMeshProUGUI _lb_one;
        public TextMeshProUGUI label_all;
        public TextMeshProUGUI _lb_all;
        public RectTransform _gp_btn_go;
        public RectTransform _btn_go;
        public Image _Image1;
        public TextMeshProUGUI _lb_go;
        public RectTransform _gp_btn_exchange;
        public RectTransform _btn_exchange;
        public Image _Image2;
        public TextMeshProUGUI _lb_exchange;
        public Image redDot;
        public RectTransform _Group0;
        public Image _img_symbol;
        public RectTransform _gp_reward;
        public GameObject _tpl_FtvCollectionGoodItem;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg_img), bg_img);
            EnsureBound(nameof(_scl_goods), _scl_goods);
            EnsureBound(nameof(_gp_goods), _gp_goods);
            EnsureBound(nameof(_gp_label), _gp_label);
            EnsureBound(nameof(label_person), label_person);
            EnsureBound(nameof(_lb_one), _lb_one);
            EnsureBound(nameof(label_all), label_all);
            EnsureBound(nameof(_lb_all), _lb_all);
            EnsureBound(nameof(_gp_btn_go), _gp_btn_go);
            EnsureBound(nameof(_btn_go), _btn_go);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_lb_go), _lb_go);
            EnsureBound(nameof(_gp_btn_exchange), _gp_btn_exchange);
            EnsureBound(nameof(_btn_exchange), _btn_exchange);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_lb_exchange), _lb_exchange);
            EnsureBound(nameof(redDot), redDot);
            EnsureBound(nameof(_Group0), _Group0);
            EnsureBound(nameof(_img_symbol), _img_symbol);
            EnsureBound(nameof(_gp_reward), _gp_reward);
            EnsureBound(nameof(_tpl_FtvCollectionGoodItem), _tpl_FtvCollectionGoodItem);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
