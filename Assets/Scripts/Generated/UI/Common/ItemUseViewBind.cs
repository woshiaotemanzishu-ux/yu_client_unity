// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/common/ItemUseView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Common
{
    public partial class ItemUseViewBind : BaseView
    {
        public RectTransform _gp_con;
        public Image _Image1;
        public RectTransform item_group;
        public TextMeshProUGUI name_label;
        public Image _Image2;
        public RectTransform enter_btn;
        public Image _Image11;
        public TextMeshProUGUI enter_btn_text;
        public TextMeshProUGUI bottom_label;
        public Image close_btn;
        public Image up_arrow;
        public GameObject _tpl_EquipmentItem;
        public GameObject _tpl_CompositeRuneView;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_gp_con), _gp_con);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(item_group), item_group);
            EnsureBound(nameof(name_label), name_label);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(enter_btn), enter_btn);
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(enter_btn_text), enter_btn_text);
            EnsureBound(nameof(bottom_label), bottom_label);
            EnsureBound(nameof(close_btn), close_btn);
            EnsureBound(nameof(up_arrow), up_arrow);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
            EnsureBound(nameof(_tpl_CompositeRuneView), _tpl_CompositeRuneView);
        }
    }
}
