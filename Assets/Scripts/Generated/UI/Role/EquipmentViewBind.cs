// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/role/EquipmentView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Role
{
    public partial class EquipmentViewBind : BaseView
    {
        public Image Image_right;
        public Image _img_bg_info;
        public Image model_bg;
        public Image name_bg;
        public RectTransform main_role;
        public RectTransform model_gp;
        public TextMeshProUGUI top_levelLb;
        public TextMeshProUGUI _lb_name;
        public RectTransform _gp_fight;
        public RectTransform secondary_menu;
        public RectTransform icon_gp;
        public Image _Group1;
        public Image skill_red;
        public Image _Group5;
        public Image suit_red;
        public Image fashion_gp;
        public Image fashion_red;
        public Image _Group2;
        public Image achv_red;
        public Image _Group3;
        public Image medal_red;
        public RectTransform _gp_attribute;
        public Image _btn_attribute;
        public Image attribute_red;
        public Image _Group4;
        public Image dsgt_red;
        public Image _Group6;
        public Image unreal_red;
        public Image _Image1;
        public Image _img_title_base;
        public Image _img_change_btn;
        public Image _img_title_best;
        public Image _Image6;
        public Image expImg;
        public Image Image;
        public RectTransform _Group7;
        public Image destiny_img;
        public TextMeshProUGUI levelLb;
        public TextMeshProUGUI expLb;
        public ScrollRect _Scroller1;
        public Image tipsImg;
        public RectTransform worldGp;
        public Image worldBg;
        public TextMeshProUGUI worldTips;
        public TextMeshProUGUI worldLb;
        public RectTransform _right_btn;
        public RectTransform _gp_fame;
        public Image _btn_fame;
        public Image _red_fame;
        public Image worldBtn;
        public GameObject _tpl_DsgtView;
        public GameObject _tpl_InnateSkillView;
        public GameObject _tpl_InnateListItem;
        public GameObject _tpl_InnateSkillItem;
        public GameObject _tpl_InnateTypeItemRenderer;
        public GameObject _tpl_InnateUpInfoItem;
        public GameObject _tpl_InnateUpCondItem;
        public GameObject _tpl_MedalView;
        public GameObject _tpl_MedalCostItem;
        public GameObject _tpl_RolePropertyItemRenderer;
        public GameObject _tpl_FightingShowSmallItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(Image_right), Image_right);
            EnsureBound(nameof(_img_bg_info), _img_bg_info);
            EnsureBound(nameof(model_bg), model_bg);
            EnsureBound(nameof(name_bg), name_bg);
            EnsureBound(nameof(main_role), main_role);
            EnsureBound(nameof(model_gp), model_gp);
            EnsureBound(nameof(top_levelLb), top_levelLb);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_gp_fight), _gp_fight);
            EnsureBound(nameof(secondary_menu), secondary_menu);
            EnsureBound(nameof(icon_gp), icon_gp);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(skill_red), skill_red);
            EnsureBound(nameof(_Group5), _Group5);
            EnsureBound(nameof(suit_red), suit_red);
            EnsureBound(nameof(fashion_gp), fashion_gp);
            EnsureBound(nameof(fashion_red), fashion_red);
            EnsureBound(nameof(_Group2), _Group2);
            EnsureBound(nameof(achv_red), achv_red);
            EnsureBound(nameof(_Group3), _Group3);
            EnsureBound(nameof(medal_red), medal_red);
            EnsureBound(nameof(_gp_attribute), _gp_attribute);
            EnsureBound(nameof(_btn_attribute), _btn_attribute);
            EnsureBound(nameof(attribute_red), attribute_red);
            EnsureBound(nameof(_Group4), _Group4);
            EnsureBound(nameof(dsgt_red), dsgt_red);
            EnsureBound(nameof(_Group6), _Group6);
            EnsureBound(nameof(unreal_red), unreal_red);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_img_title_base), _img_title_base);
            EnsureBound(nameof(_img_change_btn), _img_change_btn);
            EnsureBound(nameof(_img_title_best), _img_title_best);
            EnsureBound(nameof(_Image6), _Image6);
            EnsureBound(nameof(expImg), expImg);
            EnsureBound(nameof(Image), Image);
            EnsureBound(nameof(_Group7), _Group7);
            EnsureBound(nameof(destiny_img), destiny_img);
            EnsureBound(nameof(levelLb), levelLb);
            EnsureBound(nameof(expLb), expLb);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(tipsImg), tipsImg);
            EnsureBound(nameof(worldGp), worldGp);
            EnsureBound(nameof(worldBg), worldBg);
            EnsureBound(nameof(worldTips), worldTips);
            EnsureBound(nameof(worldLb), worldLb);
            EnsureBound(nameof(_right_btn), _right_btn);
            EnsureBound(nameof(_gp_fame), _gp_fame);
            EnsureBound(nameof(_btn_fame), _btn_fame);
            EnsureBound(nameof(_red_fame), _red_fame);
            EnsureBound(nameof(worldBtn), worldBtn);
            EnsureBound(nameof(_tpl_DsgtView), _tpl_DsgtView);
            EnsureBound(nameof(_tpl_InnateSkillView), _tpl_InnateSkillView);
            EnsureBound(nameof(_tpl_InnateListItem), _tpl_InnateListItem);
            EnsureBound(nameof(_tpl_InnateSkillItem), _tpl_InnateSkillItem);
            EnsureBound(nameof(_tpl_InnateTypeItemRenderer), _tpl_InnateTypeItemRenderer);
            EnsureBound(nameof(_tpl_InnateUpInfoItem), _tpl_InnateUpInfoItem);
            EnsureBound(nameof(_tpl_InnateUpCondItem), _tpl_InnateUpCondItem);
            EnsureBound(nameof(_tpl_MedalView), _tpl_MedalView);
            EnsureBound(nameof(_tpl_MedalCostItem), _tpl_MedalCostItem);
            EnsureBound(nameof(_tpl_RolePropertyItemRenderer), _tpl_RolePropertyItemRenderer);
            EnsureBound(nameof(_tpl_FightingShowSmallItem), _tpl_FightingShowSmallItem);
        }
    }
}
