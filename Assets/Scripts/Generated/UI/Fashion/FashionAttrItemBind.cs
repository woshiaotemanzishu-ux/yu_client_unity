// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/fashion/FashionAttrItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Fashion
{
    public partial class FashionAttrItemBind : BaseView
    {
        public Image bg;
        public TextMeshProUGUI _lb_att0;
        public Image _Image1;
        public TextMeshProUGUI _lb_att1;
        public TextMeshProUGUI name;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(_lb_att0), _lb_att0);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_lb_att1), _lb_att1);
            EnsureBound(nameof(name), name);
        }
    }
}
