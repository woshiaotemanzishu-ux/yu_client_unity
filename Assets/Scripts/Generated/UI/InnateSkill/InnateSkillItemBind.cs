// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/innateSkill/InnateSkillItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.InnateSkill
{
    public partial class InnateSkillItemBind : BaseView
    {
        public RectTransform _group;
        public Image _Image1;
        public Image _img_mask;
        public Image _img_icon;
        public Image _img_select;
        public RectTransform _gp_lv;
        public Image _Image2;
        public TextMeshProUGUI _lb_lv;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_group), _group);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_img_mask), _img_mask);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_img_select), _img_select);
            EnsureBound(nameof(_gp_lv), _gp_lv);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_lb_lv), _lb_lv);
        }
    }
}
