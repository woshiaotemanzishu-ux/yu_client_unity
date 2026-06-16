// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/mainUI/FightingUpView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainUI
{
    public partial class FightingUpViewBind : BaseView
    {
        public Image _img_bg;
        public TextMeshProUGUI _lb_fight;
        public TextMeshProUGUI _lb_add_fight;
        public RectTransform _box_effect;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_lb_fight), _lb_fight);
            EnsureBound(nameof(_lb_add_fight), _lb_add_fight);
            EnsureBound(nameof(_box_effect), _box_effect);
        }
    }
}
