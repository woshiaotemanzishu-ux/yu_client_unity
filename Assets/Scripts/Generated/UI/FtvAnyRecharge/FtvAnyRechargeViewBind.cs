// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/ftvAnyRecharge/ftvAnyRechargeView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.FtvAnyRecharge
{
    public partial class FtvAnyRechargeViewBind : BaseView
    {
        public Image _img_desc;
        public RectTransform _gp_time;
        public RectTransform _btn;
        public Image _Image1;
        public TextMeshProUGUI labelDisplay;
        public Image _img_red;
        public Image _Image11;
        public RectTransform _gp_reward;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_desc), _img_desc);
            EnsureBound(nameof(_gp_time), _gp_time);
            EnsureBound(nameof(_btn), _btn);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(_img_red), _img_red);
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(_gp_reward), _gp_reward);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
