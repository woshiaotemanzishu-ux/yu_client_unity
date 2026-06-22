// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/eternity/EternityCadreItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Eternity
{
    public partial class EternityCadreItemBind : BaseView
    {
        public RectTransform click;
        public Image _Image1;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_lv;
        public TextMeshProUGUI _lb_state;
        public Image _img_select;

        protected override void BindNodes()
        {
            EnsureBound(nameof(click), click);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_lv), _lb_lv);
            EnsureBound(nameof(_lb_state), _lb_state);
            EnsureBound(nameof(_img_select), _img_select);
        }
    }
}
