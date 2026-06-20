// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/pet/PetSkillView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Pet
{
    public partial class PetSkillViewBind : BaseView
    {
        public Image skill_bg;
        public Image icon_bg;
        public Image icon;
        public Image _Image1;
        public TextMeshProUGUI des_text0;
        public Image _bg_name;
        public TextMeshProUGUI name_text;
        public TextMeshProUGUI lv_text;
        public ScrollRect des_scroller;
        public RectTransform des_group;
        public TextMeshProUGUI des_text;
        public TextMeshProUGUI fight_text;
        public RectTransform enter_btn;
        public Image enter_img;
        public TextMeshProUGUI enter_lb;

        protected override void BindNodes()
        {
            EnsureBound(nameof(skill_bg), skill_bg);
            EnsureBound(nameof(icon_bg), icon_bg);
            EnsureBound(nameof(icon), icon);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(des_text0), des_text0);
            EnsureBound(nameof(_bg_name), _bg_name);
            EnsureBound(nameof(name_text), name_text);
            EnsureBound(nameof(lv_text), lv_text);
            EnsureBound(nameof(des_scroller), des_scroller);
            EnsureBound(nameof(des_group), des_group);
            EnsureBound(nameof(des_text), des_text);
            EnsureBound(nameof(fight_text), fight_text);
            EnsureBound(nameof(enter_btn), enter_btn);
            EnsureBound(nameof(enter_img), enter_img);
            EnsureBound(nameof(enter_lb), enter_lb);
        }
    }
}
