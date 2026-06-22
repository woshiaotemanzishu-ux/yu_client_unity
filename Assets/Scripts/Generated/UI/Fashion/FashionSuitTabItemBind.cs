// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/fashion/FashionSuitTabItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Fashion
{
    public partial class FashionSuitTabItemBind : BaseView
    {
        public Image _img_bg;
        public Image _img_icon;
        public Image _img_select;
        public TextMeshProUGUI _lb_name;
        public Image _img_red;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_img_select), _img_select);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_img_red), _img_red);
        }
    }
}
