// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/halo/HaloItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Halo
{
    public partial class HaloItemBind : BaseView
    {
        public Image img_bg;
        public RectTransform box_parent;
        public Image img_get;
        public Image img_getted;
        public Image img_mask;
        public TextMeshProUGUI lable_mask_desc;
        public Image img_flag;
        public RectTransform box_desc;

        protected override void BindNodes()
        {
            EnsureBound(nameof(img_bg), img_bg);
            EnsureBound(nameof(box_parent), box_parent);
            EnsureBound(nameof(img_get), img_get);
            EnsureBound(nameof(img_getted), img_getted);
            EnsureBound(nameof(img_mask), img_mask);
            EnsureBound(nameof(lable_mask_desc), lable_mask_desc);
            EnsureBound(nameof(img_flag), img_flag);
            EnsureBound(nameof(box_desc), box_desc);
        }
    }
}
