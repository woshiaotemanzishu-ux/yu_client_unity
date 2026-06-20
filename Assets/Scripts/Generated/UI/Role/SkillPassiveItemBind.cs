// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/role/SkillPassiveItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Role
{
    public partial class SkillPassiveItemBind : BaseView
    {
        public Image _img_1;
        public TextMeshProUGUI _lb_name;
        public Image _img_2;
        public Image _img_icon;
        public Image _img_select;
        public Image _img_red;
        public RectTransform _gp_finger;
        public Image _Img_3;
        public TextMeshProUGUI _lb_lv;
        public GameObject _tpl_SkillPassiveSubItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_1), _img_1);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_img_2), _img_2);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_img_select), _img_select);
            EnsureBound(nameof(_img_red), _img_red);
            EnsureBound(nameof(_gp_finger), _gp_finger);
            EnsureBound(nameof(_Img_3), _Img_3);
            EnsureBound(nameof(_lb_lv), _lb_lv);
            EnsureBound(nameof(_tpl_SkillPassiveSubItem), _tpl_SkillPassiveSubItem);
        }
    }
}
