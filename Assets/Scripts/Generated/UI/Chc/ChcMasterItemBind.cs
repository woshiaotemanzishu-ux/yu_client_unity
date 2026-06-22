// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/chc/chcMasterItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Chc
{
    public partial class ChcMasterItemBind : BaseView
    {
        public Image _Image1;
        public Image _Image2;
        public Image _Image3;
        public TextMeshProUGUI _lb_lv;
        public TextMeshProUGUI _lb_attr;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(_lb_lv), _lb_lv);
            EnsureBound(nameof(_lb_attr), _lb_attr);
        }
    }
}
