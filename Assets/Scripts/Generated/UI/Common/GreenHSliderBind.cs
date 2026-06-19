// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/common/GreenHSlider.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Common
{
    public partial class GreenHSliderBind : BaseView
    {
        public Image track;
        public Image trackHighlight;
        public Image thumb;
        public Image value_bg;
        public TextMeshProUGUI value_text;
        public GameObject _tpl_GoodsItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(track), track);
            EnsureBound(nameof(trackHighlight), trackHighlight);
            EnsureBound(nameof(thumb), thumb);
            EnsureBound(nameof(value_bg), value_bg);
            EnsureBound(nameof(value_text), value_text);
            EnsureBound(nameof(_tpl_GoodsItem), _tpl_GoodsItem);
        }
    }
}
