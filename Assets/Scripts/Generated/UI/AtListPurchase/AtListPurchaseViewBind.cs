// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/atListPurchase/AtListPurchaseView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.AtListPurchase
{
    public partial class AtListPurchaseViewBind : BaseView
    {
        public Image _img_title;
        public RectTransform _gp_time;
        public Image _img_item_bg;
        public ScrollRect item_panel;
        public RectTransform _item_group;
        public RectTransform _lb_group;
        public TextMeshProUGUI personal_num;
        public TextMeshProUGUI all_num;
        public RectTransform price;
        public TextMeshProUGUI old_price;
        public TextMeshProUGUI new_price;
        public Image money_1;
        public Image money_2;
        public RectTransform _buy_btn;
        public Image _btn_bg;
        public TextMeshProUGUI _lb_btn;
        public Image rare;
        public RectTransform point_gp;
        public Image big_num;
        public Image small_num;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_gp_time), _gp_time);
            EnsureBound(nameof(_img_item_bg), _img_item_bg);
            EnsureBound(nameof(item_panel), item_panel);
            EnsureBound(nameof(_item_group), _item_group);
            EnsureBound(nameof(_lb_group), _lb_group);
            EnsureBound(nameof(personal_num), personal_num);
            EnsureBound(nameof(all_num), all_num);
            EnsureBound(nameof(price), price);
            EnsureBound(nameof(old_price), old_price);
            EnsureBound(nameof(new_price), new_price);
            EnsureBound(nameof(money_1), money_1);
            EnsureBound(nameof(money_2), money_2);
            EnsureBound(nameof(_buy_btn), _buy_btn);
            EnsureBound(nameof(_btn_bg), _btn_bg);
            EnsureBound(nameof(_lb_btn), _lb_btn);
            EnsureBound(nameof(rare), rare);
            EnsureBound(nameof(point_gp), point_gp);
            EnsureBound(nameof(big_num), big_num);
            EnsureBound(nameof(small_num), small_num);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
