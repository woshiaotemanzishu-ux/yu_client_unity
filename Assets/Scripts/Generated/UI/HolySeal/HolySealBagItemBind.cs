// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/holySeal/HolySealBagItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.HolySeal
{
    public partial class HolySealBagItemBind : BaseView
    {
        public RectTransform _gp_con;
        public Image _img_bg;
        public Image _img_icon;
        public RectTransform _gp_stage;
        public Image _img_stage_bg;
        public TextMeshProUGUI _lb_stage;
        public RectTransform _gp_effect;
        public Image _img_bind;
        public TextMeshProUGUI _lb_count;
        public RectTransform _gp_effect2;
        public Image _img_up;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_gp_con), _gp_con);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_gp_stage), _gp_stage);
            EnsureBound(nameof(_img_stage_bg), _img_stage_bg);
            EnsureBound(nameof(_lb_stage), _lb_stage);
            EnsureBound(nameof(_gp_effect), _gp_effect);
            EnsureBound(nameof(_img_bind), _img_bind);
            EnsureBound(nameof(_lb_count), _lb_count);
            EnsureBound(nameof(_gp_effect2), _gp_effect2);
            EnsureBound(nameof(_img_up), _img_up);
        }
    }
}
