// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/role/SkillInitiativeItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Role
{
    public partial class SkillInitiativeItemBind : BaseView
    {
        public Image _Image1;
        public Image _img_select;
        public Image _img_icon;
        public Image _img_black;
        public Image _img_lock;
        public TextMeshProUGUI _lb_level;
        public Image _reddot;
        public RectTransform _group_eff;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_img_select), _img_select);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_img_black), _img_black);
            EnsureBound(nameof(_img_lock), _img_lock);
            EnsureBound(nameof(_lb_level), _lb_level);
            EnsureBound(nameof(_reddot), _reddot);
            EnsureBound(nameof(_group_eff), _group_eff);
        }
    }
}
