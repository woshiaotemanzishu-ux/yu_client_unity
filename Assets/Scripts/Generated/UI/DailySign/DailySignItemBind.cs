// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dailySign/DailySignItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DailySign
{
    public partial class DailySignItemBind : BaseView
    {
        public Image top_img;
        public RectTransform click_group;
        public Image _Image1;
        public Image _red;
        public RectTransform _gp_effect;
        public RectTransform item_group;
        public TextMeshProUGUI day_label;
        public Image img_mask;
        public RectTransform sign_group;
        public Image _Image2;
        public Image _Image3;
        public RectTransform patch_group;
        public Image tick_img;
        public Image tips_bg;
        public TextMeshProUGUI _lb_vip;
        public Image vip_img;
        public Image nulti_img;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(top_img), top_img);
            EnsureBound(nameof(click_group), click_group);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_red), _red);
            EnsureBound(nameof(_gp_effect), _gp_effect);
            EnsureBound(nameof(item_group), item_group);
            EnsureBound(nameof(day_label), day_label);
            EnsureBound(nameof(img_mask), img_mask);
            EnsureBound(nameof(sign_group), sign_group);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(patch_group), patch_group);
            EnsureBound(nameof(tick_img), tick_img);
            EnsureBound(nameof(tips_bg), tips_bg);
            EnsureBound(nameof(_lb_vip), _lb_vip);
            EnsureBound(nameof(vip_img), vip_img);
            EnsureBound(nameof(nulti_img), nulti_img);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
