// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/baby/BabyChangedView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Baby
{
    public partial class BabyChangedViewBind : BaseView
    {
        public Image table_img;
        public RectTransform effect_group;
        public RectTransform image_text_group;
        public TextMeshProUGUI tipstext;
        public RectTransform image_bigglow_group;
        public Image success_image;
        public RectTransform image_arrary_group;
        public RectTransform _box_click;
        public RectTransform model_group;
        public RectTransform image_particle_group;
        public Image image_name;
        public TextMeshProUGUI name_label;

        protected override void BindNodes()
        {
            EnsureBound(nameof(table_img), table_img);
            EnsureBound(nameof(effect_group), effect_group);
            EnsureBound(nameof(image_text_group), image_text_group);
            EnsureBound(nameof(tipstext), tipstext);
            EnsureBound(nameof(image_bigglow_group), image_bigglow_group);
            EnsureBound(nameof(success_image), success_image);
            EnsureBound(nameof(image_arrary_group), image_arrary_group);
            EnsureBound(nameof(_box_click), _box_click);
            EnsureBound(nameof(model_group), model_group);
            EnsureBound(nameof(image_particle_group), image_particle_group);
            EnsureBound(nameof(image_name), image_name);
            EnsureBound(nameof(name_label), name_label);
        }
    }
}
