// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/demon/DemonFettersItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Demon
{
    public partial class DemonFettersItemBind : BaseView
    {
        public Image _img_bg;
        public TextMeshProUGUI _lb_desc;
        public Image _img_activation;
        public Image _Image1;
        public RectTransform _gp_head_con;
        public TextMeshProUGUI _lb_attr;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_lb_desc), _lb_desc);
            EnsureBound(nameof(_img_activation), _img_activation);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_gp_head_con), _gp_head_con);
            EnsureBound(nameof(_lb_attr), _lb_attr);
        }
    }
}
