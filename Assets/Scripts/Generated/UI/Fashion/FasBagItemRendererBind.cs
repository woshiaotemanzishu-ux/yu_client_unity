// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/fashion/FasBagItemRenderer.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Fashion
{
    public partial class FasBagItemRendererBind : BaseView
    {
        public RectTransform _Group2;
        public Image item_image;
        public RectTransform _Group1;
        public Image fashion_image;
        public TextMeshProUGUI num_label;
        public Image select_image;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Group2), _Group2);
            EnsureBound(nameof(item_image), item_image);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(fashion_image), fashion_image);
            EnsureBound(nameof(num_label), num_label);
            EnsureBound(nameof(select_image), select_image);
        }
    }
}
