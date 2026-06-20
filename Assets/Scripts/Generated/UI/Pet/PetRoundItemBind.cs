// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/pet/PetRoundItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Pet
{
    public partial class PetRoundItemBind : BaseView
    {
        public RectTransform click_group;
        public Image icon_bg;
        public Image icon_bg_mask;
        public Image icon;
        public TextMeshProUGUI bottom_text;
        public Image red_dot;
        public RectTransform skill_info_gp;
        public TextMeshProUGUI skill_lv;
        public Image up_arrow1;
        public RectTransform num_group;
        public RectTransform effBox;
        public GameObject _tpl_FightingUpItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(click_group), click_group);
            EnsureBound(nameof(icon_bg), icon_bg);
            EnsureBound(nameof(icon_bg_mask), icon_bg_mask);
            EnsureBound(nameof(icon), icon);
            EnsureBound(nameof(bottom_text), bottom_text);
            EnsureBound(nameof(red_dot), red_dot);
            EnsureBound(nameof(skill_info_gp), skill_info_gp);
            EnsureBound(nameof(skill_lv), skill_lv);
            EnsureBound(nameof(up_arrow1), up_arrow1);
            EnsureBound(nameof(num_group), num_group);
            EnsureBound(nameof(effBox), effBox);
            EnsureBound(nameof(_tpl_FightingUpItem), _tpl_FightingUpItem);
        }
    }
}
