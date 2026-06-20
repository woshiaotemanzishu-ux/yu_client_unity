// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/pet/OutwardLvSystem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Pet
{
    public partial class OutwardLvSystemBind : BaseView
    {
        public Image down_bg;
        public RectTransform lv_skill_group;
        public RectTransform lv_exp_group;
        public Image Round2;
        public RectTransform waterGp;
        public RectTransform runeGp;
        public RectTransform maxGp;
        public TextMeshProUGUI lb_exp_progress;
        public RectTransform btn_group3;
        public Image img_btn_group3;
        public TextMeshProUGUI lb_btn_group3;
        public Image img_btn_group3_red;
        public RectTransform goods_group;
        public TextMeshProUGUI goods_number;
        public Image goods_icon;
        public Image _gp_check;
        public Image _img_check;
        public TextMeshProUGUI _lb_check;
        public GameObject _tpl_BaseAwardItem;
        public GameObject _tpl_PetRoundItem;
        public GameObject _tpl_PetEquipOutItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(down_bg), down_bg);
            EnsureBound(nameof(lv_skill_group), lv_skill_group);
            EnsureBound(nameof(lv_exp_group), lv_exp_group);
            EnsureBound(nameof(Round2), Round2);
            EnsureBound(nameof(waterGp), waterGp);
            EnsureBound(nameof(runeGp), runeGp);
            EnsureBound(nameof(maxGp), maxGp);
            EnsureBound(nameof(lb_exp_progress), lb_exp_progress);
            EnsureBound(nameof(btn_group3), btn_group3);
            EnsureBound(nameof(img_btn_group3), img_btn_group3);
            EnsureBound(nameof(lb_btn_group3), lb_btn_group3);
            EnsureBound(nameof(img_btn_group3_red), img_btn_group3_red);
            EnsureBound(nameof(goods_group), goods_group);
            EnsureBound(nameof(goods_number), goods_number);
            EnsureBound(nameof(goods_icon), goods_icon);
            EnsureBound(nameof(_gp_check), _gp_check);
            EnsureBound(nameof(_img_check), _img_check);
            EnsureBound(nameof(_lb_check), _lb_check);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
            EnsureBound(nameof(_tpl_PetRoundItem), _tpl_PetRoundItem);
            EnsureBound(nameof(_tpl_PetEquipOutItem), _tpl_PetEquipOutItem);
        }
    }
}
