// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/brightSea/BrightSeaRoleItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.BrightSea
{
    public partial class BrightSeaRoleItemBind : BaseView
    {
        public RectTransform _con_head;
        public Image head_bg;
        public RectTransform _head;
        public Image bg;
        public TextMeshProUGUI _lb_name;
        public RectTransform _con_model;
        public RectTransform _con_eff2;
        public RectTransform _con_eff1;
        public RectTransform _img_ship;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_con_head), _con_head);
            EnsureBound(nameof(head_bg), head_bg);
            EnsureBound(nameof(_head), _head);
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_con_model), _con_model);
            EnsureBound(nameof(_con_eff2), _con_eff2);
            EnsureBound(nameof(_con_eff1), _con_eff1);
            EnsureBound(nameof(_img_ship), _img_ship);
        }
    }
}
