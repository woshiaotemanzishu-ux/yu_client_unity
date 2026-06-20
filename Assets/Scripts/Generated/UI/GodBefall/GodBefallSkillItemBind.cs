// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/godBefall/GodBefallSkillItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GodBefall
{
    public partial class GodBefallSkillItemBind : BaseView
    {
        public RectTransform _box_con;
        public Image _img_icon;
        public Image _img_icon2;
        public RectTransform _box_lock;
        public TextMeshProUGUI _lb_lock;
        public Image _img_skill_tag;
        public Image _img_new;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_box_con), _box_con);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_img_icon2), _img_icon2);
            EnsureBound(nameof(_box_lock), _box_lock);
            EnsureBound(nameof(_lb_lock), _lb_lock);
            EnsureBound(nameof(_img_skill_tag), _img_skill_tag);
            EnsureBound(nameof(_img_new), _img_new);
        }
    }
}
