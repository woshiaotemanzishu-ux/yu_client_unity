// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/pet/IllusionItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Pet
{
    public partial class IllusionItemBind : BaseView
    {
        public Image bg;
        public Image select_bg;
        public RectTransform group;
        public RectTransform _Group1;
        public Image icon_bg;
        public Image icon_image;
        public TextMeshProUGUI icon_stage;
        public Image red_dot;
        public TextMeshProUGUI res_name;
        public TextMeshProUGUI state_text;
        public Image using_tip;
        public RectTransform shadow_group;
        public RectTransform star_group;
        public Image click_bg;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(select_bg), select_bg);
            EnsureBound(nameof(group), group);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(icon_bg), icon_bg);
            EnsureBound(nameof(icon_image), icon_image);
            EnsureBound(nameof(icon_stage), icon_stage);
            EnsureBound(nameof(red_dot), red_dot);
            EnsureBound(nameof(res_name), res_name);
            EnsureBound(nameof(state_text), state_text);
            EnsureBound(nameof(using_tip), using_tip);
            EnsureBound(nameof(shadow_group), shadow_group);
            EnsureBound(nameof(star_group), star_group);
            EnsureBound(nameof(click_bg), click_bg);
        }
    }
}
