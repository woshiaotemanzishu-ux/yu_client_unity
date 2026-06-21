// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/innateSkill/InnateTypeItemRenderer.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.InnateSkill
{
    public partial class InnateTypeItemRendererBind : BaseView
    {
        public Image _img_skill_icon;
        public RectTransform _lb_data;
        public TextMeshProUGUI typeLb;
        public TextMeshProUGUI _lb_lv;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_skill_icon), _img_skill_icon);
            EnsureBound(nameof(_lb_data), _lb_data);
            EnsureBound(nameof(typeLb), typeLb);
            EnsureBound(nameof(_lb_lv), _lb_lv);
        }
    }
}
