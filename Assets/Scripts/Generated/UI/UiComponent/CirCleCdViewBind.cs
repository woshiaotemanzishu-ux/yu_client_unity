// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/uiComponent/CirCleCdView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.UiComponent
{
    public partial class CirCleCdViewBind : BaseView
    {
        public Image _img_mask;
        public TextMeshProUGUI _lb_cd;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_mask), _img_mask);
            EnsureBound(nameof(_lb_cd), _lb_cd);
        }
    }
}
