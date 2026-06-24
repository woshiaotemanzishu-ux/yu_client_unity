// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/composite/RuneComposeCostItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Composite
{
    public partial class RuneComposeCostItemBind : BaseView
    {
        public RectTransform conta;
        public TextMeshProUGUI num;
        public RectTransform click_bg;
        public Image _img_awaken;
        public Image _img_wear;
        public TextMeshProUGUI _lb_name;

        protected override void BindNodes()
        {
            EnsureBound(nameof(conta), conta);
            EnsureBound(nameof(num), num);
            EnsureBound(nameof(click_bg), click_bg);
            EnsureBound(nameof(_img_awaken), _img_awaken);
            EnsureBound(nameof(_img_wear), _img_wear);
            EnsureBound(nameof(_lb_name), _lb_name);
        }
    }
}
