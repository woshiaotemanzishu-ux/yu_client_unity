// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/rune/RuneBagItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Rune
{
    public partial class RuneBagItemBind : BaseView
    {
        public Image bg;
        public Image _img_iconbg;
        public Image _img_icon;
        public Image _img_kuang;
        public TextMeshProUGUI goods_name;
        public TextMeshProUGUI goods_lv;
        public TextMeshProUGUI pro;
        public RectTransform insertBtn;
        public Image _Image1;
        public TextMeshProUGUI labelDisplay;
        public Image not_state;
        public Image not_suit;
        public Image awakeIcon;
        public Image _img_color;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(_img_iconbg), _img_iconbg);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_img_kuang), _img_kuang);
            EnsureBound(nameof(goods_name), goods_name);
            EnsureBound(nameof(goods_lv), goods_lv);
            EnsureBound(nameof(pro), pro);
            EnsureBound(nameof(insertBtn), insertBtn);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(not_state), not_state);
            EnsureBound(nameof(not_suit), not_suit);
            EnsureBound(nameof(awakeIcon), awakeIcon);
            EnsureBound(nameof(_img_color), _img_color);
        }
    }
}
