// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/vip/VipWeekGiftView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Vip
{
    public partial class VipWeekGiftViewBind : BaseView
    {
        public Image newbg;
        public Image bg;
        public Image _Image1;
        public Image _Image2;
        public TextMeshProUGUI title_text;
        public Image image6;
        public Image image;
        public Image _Image3;
        public Image _Image4;
        public Image close_btn;
        public Image _Image5;
        public ScrollRect reward_scroller;
        public RectTransform Content;
        public TextMeshProUGUI bottom_text;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(newbg), newbg);
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(title_text), title_text);
            EnsureBound(nameof(image6), image6);
            EnsureBound(nameof(image), image);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(_Image4), _Image4);
            EnsureBound(nameof(close_btn), close_btn);
            EnsureBound(nameof(_Image5), _Image5);
            EnsureBound(nameof(reward_scroller), reward_scroller);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(bottom_text), bottom_text);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
