// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/godBefall/GodBefallRecoveryView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GodBefall
{
    public partial class GodBefallRecoveryViewBind : BaseView
    {
        public Image img_bg;
        public Image img_top_bg;
        public Image img_bottom_bg;
        public RectTransform box_top;
        public ScrollRect list_equip;
        public TextMeshProUGUI lable_title;
        public Image img_btn_select;
        public Image img_btn_streng;
        public TextMeshProUGUI lable_streng;
        public RectTransform box_god;
        public ScrollRect list_btn;
        public RectTransform _gp_empty;
        public Image _Image5;
        public TextMeshProUGUI _Label5;
        public GameObject _tpl_GodBefallRecoveryBtnItem;
        public GameObject _tpl_GodBefallRecoveryTopView;
        public GameObject _tpl_GodBefallRecoveryPropertyItem;
        public GameObject _tpl_EquipmentItem;
        public GameObject _tpl_GodBefallEquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(img_bg), img_bg);
            EnsureBound(nameof(img_top_bg), img_top_bg);
            EnsureBound(nameof(img_bottom_bg), img_bottom_bg);
            EnsureBound(nameof(box_top), box_top);
            EnsureBound(nameof(list_equip), list_equip);
            EnsureBound(nameof(lable_title), lable_title);
            EnsureBound(nameof(img_btn_select), img_btn_select);
            EnsureBound(nameof(img_btn_streng), img_btn_streng);
            EnsureBound(nameof(lable_streng), lable_streng);
            EnsureBound(nameof(box_god), box_god);
            EnsureBound(nameof(list_btn), list_btn);
            EnsureBound(nameof(_gp_empty), _gp_empty);
            EnsureBound(nameof(_Image5), _Image5);
            EnsureBound(nameof(_Label5), _Label5);
            EnsureBound(nameof(_tpl_GodBefallRecoveryBtnItem), _tpl_GodBefallRecoveryBtnItem);
            EnsureBound(nameof(_tpl_GodBefallRecoveryTopView), _tpl_GodBefallRecoveryTopView);
            EnsureBound(nameof(_tpl_GodBefallRecoveryPropertyItem), _tpl_GodBefallRecoveryPropertyItem);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
            EnsureBound(nameof(_tpl_GodBefallEquipmentItem), _tpl_GodBefallEquipmentItem);
        }
    }
}
