// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/marriage/MarriageHonourItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Marriage
{
    public partial class MarriageHonourItemBind : BaseView
    {
        public Image _img_bg;
        public Image _img_icon;
        public TextMeshProUGUI _lb_honour;
        public TextMeshProUGUI _lb_attr;
        public Image _img_title;
        public Image _img_unlock;
        public TextMeshProUGUI _lb_title;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_lb_honour), _lb_honour);
            EnsureBound(nameof(_lb_attr), _lb_attr);
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_img_unlock), _img_unlock);
            EnsureBound(nameof(_lb_title), _lb_title);
        }
    }
}
