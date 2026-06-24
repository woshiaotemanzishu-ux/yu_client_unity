// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/common/ExchangeViewOne.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Common
{
    public partial class ExchangeViewOneBind : BaseView
    {
        public Image _bg;
        public Image close;
        public Image Image2;
        public Image Image3;
        public RectTransform goods_item;
        public TextMeshProUGUI goods_name;
        public TextMeshProUGUI Text1;
        public Image price_icon;
        public TextMeshProUGUI price_label;
        public Image Image4;
        public TextMeshProUGUI Text_num;
        public Image reduce_btn;
        public Image num_touch;
        public Image increase_btn;
        public Image max_btn;
        public TextMeshProUGUI _lb_cur_num;
        public TextMeshProUGUI Text_price;
        public Image allprice_icon;
        public TextMeshProUGUI allprice_label;
        public TextMeshProUGUI _lb_have_num;
        public RectTransform _btn_cancel;
        public Image cancel;
        public TextMeshProUGUI _lb_cancel;
        public RectTransform _btn_ok;
        public Image ok;
        public TextMeshProUGUI _lb_ok;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_bg), _bg);
            EnsureBound(nameof(close), close);
            EnsureBound(nameof(Image2), Image2);
            EnsureBound(nameof(Image3), Image3);
            EnsureBound(nameof(goods_item), goods_item);
            EnsureBound(nameof(goods_name), goods_name);
            EnsureBound(nameof(Text1), Text1);
            EnsureBound(nameof(price_icon), price_icon);
            EnsureBound(nameof(price_label), price_label);
            EnsureBound(nameof(Image4), Image4);
            EnsureBound(nameof(Text_num), Text_num);
            EnsureBound(nameof(reduce_btn), reduce_btn);
            EnsureBound(nameof(num_touch), num_touch);
            EnsureBound(nameof(increase_btn), increase_btn);
            EnsureBound(nameof(max_btn), max_btn);
            EnsureBound(nameof(_lb_cur_num), _lb_cur_num);
            EnsureBound(nameof(Text_price), Text_price);
            EnsureBound(nameof(allprice_icon), allprice_icon);
            EnsureBound(nameof(allprice_label), allprice_label);
            EnsureBound(nameof(_lb_have_num), _lb_have_num);
            EnsureBound(nameof(_btn_cancel), _btn_cancel);
            EnsureBound(nameof(cancel), cancel);
            EnsureBound(nameof(_lb_cancel), _lb_cancel);
            EnsureBound(nameof(_btn_ok), _btn_ok);
            EnsureBound(nameof(ok), ok);
            EnsureBound(nameof(_lb_ok), _lb_ok);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
