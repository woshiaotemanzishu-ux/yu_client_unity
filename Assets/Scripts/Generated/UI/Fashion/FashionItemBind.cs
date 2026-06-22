// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/fashion/FashionItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Fashion
{
    public partial class FashionItemBind : BaseView
    {
        public RectTransform fashion_group;
        public RectTransform fashion_icon_group;
        public Image fashion_plate_image;
        public Image fashion_icon_image;
        public Image select;
        public Image fashion_red_image;
        public Image fashion_waer_image;
        public Image Image;
        public RectTransform name_box;
        public TextMeshProUGUI fashion_name_label;

        protected override void BindNodes()
        {
            EnsureBound(nameof(fashion_group), fashion_group);
            EnsureBound(nameof(fashion_icon_group), fashion_icon_group);
            EnsureBound(nameof(fashion_plate_image), fashion_plate_image);
            EnsureBound(nameof(fashion_icon_image), fashion_icon_image);
            EnsureBound(nameof(select), select);
            EnsureBound(nameof(fashion_red_image), fashion_red_image);
            EnsureBound(nameof(fashion_waer_image), fashion_waer_image);
            EnsureBound(nameof(Image), Image);
            EnsureBound(nameof(name_box), name_box);
            EnsureBound(nameof(fashion_name_label), fashion_name_label);
        }
    }
}
