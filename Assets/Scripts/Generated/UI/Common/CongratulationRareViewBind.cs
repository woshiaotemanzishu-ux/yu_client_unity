// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/common/CongratulationRareView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Common
{
    public partial class CongratulationRareViewBind : BaseView
    {
        public Image _img_matrix;
        public Image _img_horn_right;
        public Image _img_horn_left;
        public Image _img_leaf_right;
        public Image _img_leaf_left;
        public Image _img_top;
        public RectTransform _gp_effect;
        public TextMeshProUGUI _lb_time;
        public RectTransform _gp_item;
        public Image _special;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_matrix), _img_matrix);
            EnsureBound(nameof(_img_horn_right), _img_horn_right);
            EnsureBound(nameof(_img_horn_left), _img_horn_left);
            EnsureBound(nameof(_img_leaf_right), _img_leaf_right);
            EnsureBound(nameof(_img_leaf_left), _img_leaf_left);
            EnsureBound(nameof(_img_top), _img_top);
            EnsureBound(nameof(_gp_effect), _gp_effect);
            EnsureBound(nameof(_lb_time), _lb_time);
            EnsureBound(nameof(_gp_item), _gp_item);
            EnsureBound(nameof(_special), _special);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
