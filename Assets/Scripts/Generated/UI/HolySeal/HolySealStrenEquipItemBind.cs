// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/holySeal/HolySealStrenEquipItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.HolySeal
{
    public partial class HolySealStrenEquipItemBind : BaseView
    {
        public RectTransform _gp_con;
        public Image _img_bg;
        public Image _img_icon;
        public RectTransform _gp_selcet;
        public RectTransform _gp_stage;
        public Image _img_stage_bg;
        public TextMeshProUGUI _lb_stage;
        public TextMeshProUGUI _lb_stren;
        public Image _img_red;
        public RectTransform group_eff;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_gp_con), _gp_con);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_gp_selcet), _gp_selcet);
            EnsureBound(nameof(_gp_stage), _gp_stage);
            EnsureBound(nameof(_img_stage_bg), _img_stage_bg);
            EnsureBound(nameof(_lb_stage), _lb_stage);
            EnsureBound(nameof(_lb_stren), _lb_stren);
            EnsureBound(nameof(_img_red), _img_red);
            EnsureBound(nameof(group_eff), group_eff);
        }
    }
}
