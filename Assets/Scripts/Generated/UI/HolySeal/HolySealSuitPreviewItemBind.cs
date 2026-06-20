// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/holySeal/HolySealSuitPreviewItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.HolySeal
{
    public partial class HolySealSuitPreviewItemBind : BaseView
    {
        public Image _img_bg;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_count;
        public Image _img_select;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_count), _lb_count);
            EnsureBound(nameof(_img_select), _img_select);
        }
    }
}
