// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/baby/BabyIlluItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Baby
{
    public partial class BabyIlluItemBind : BaseView
    {
        public RectTransform clickGp;
        public Image _Image1;
        public Image select_img;
        public Image unActive;
        public Image starbg;
        public Image box;
        public Image resImg;
        public RectTransform star_shadow_group;
        public RectTransform star_group;
        public Image red_dot;
        public TextMeshProUGUI stageLb;

        protected override void BindNodes()
        {
            EnsureBound(nameof(clickGp), clickGp);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(select_img), select_img);
            EnsureBound(nameof(unActive), unActive);
            EnsureBound(nameof(starbg), starbg);
            EnsureBound(nameof(box), box);
            EnsureBound(nameof(resImg), resImg);
            EnsureBound(nameof(star_shadow_group), star_shadow_group);
            EnsureBound(nameof(star_group), star_group);
            EnsureBound(nameof(red_dot), red_dot);
            EnsureBound(nameof(stageLb), stageLb);
        }
    }
}
