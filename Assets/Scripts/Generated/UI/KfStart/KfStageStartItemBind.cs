// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kfStart/kfStageStartItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.KfStart
{
    public partial class KfStageStartItemBind : BaseView
    {
        public TextMeshProUGUI _lb_msg;
        public RectTransform box_add;
        public Image _img_bg;
        public TextMeshProUGUI _lb_add;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_lb_msg), _lb_msg);
            EnsureBound(nameof(box_add), box_add);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_lb_add), _lb_add);
        }
    }
}
