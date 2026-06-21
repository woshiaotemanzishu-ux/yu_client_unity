// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/halo/HaloMainView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Halo
{
    public partial class HaloMainViewBind : BaseView
    {
        public Image img_bg;
        public Image img_btn_close;
        public RectTransform box_effect;
        public ScrollRect list_middle;
        public Image img_bottom_banner;
        public RectTransform box_buy;
        public Image img_btn_buy;
        public TextMeshProUGUI lable_buy;
        public Image img_now_price;
        public TextMeshProUGUI lable_now_price;
        public TextMeshProUGUI lable_original_price;
        public Image img_line;
        public TextMeshProUGUI html_time;
        public GameObject _tpl_HaloItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(img_bg), img_bg);
            EnsureBound(nameof(img_btn_close), img_btn_close);
            EnsureBound(nameof(box_effect), box_effect);
            EnsureBound(nameof(list_middle), list_middle);
            EnsureBound(nameof(img_bottom_banner), img_bottom_banner);
            EnsureBound(nameof(box_buy), box_buy);
            EnsureBound(nameof(img_btn_buy), img_btn_buy);
            EnsureBound(nameof(lable_buy), lable_buy);
            EnsureBound(nameof(img_now_price), img_now_price);
            EnsureBound(nameof(lable_now_price), lable_now_price);
            EnsureBound(nameof(lable_original_price), lable_original_price);
            EnsureBound(nameof(img_line), img_line);
            EnsureBound(nameof(html_time), html_time);
            EnsureBound(nameof(_tpl_HaloItem), _tpl_HaloItem);
        }
    }
}
