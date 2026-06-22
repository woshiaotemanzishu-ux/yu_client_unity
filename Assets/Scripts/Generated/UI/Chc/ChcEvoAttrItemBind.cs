// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/chc/chcEvoAttrItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Chc
{
    public partial class ChcEvoAttrItemBind : BaseView
    {
        public Image _img_star;
        public Image _img_tips;
        public TextMeshProUGUI _lb_attr;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_star), _img_star);
            EnsureBound(nameof(_img_tips), _img_tips);
            EnsureBound(nameof(_lb_attr), _lb_attr);
        }
    }
}
