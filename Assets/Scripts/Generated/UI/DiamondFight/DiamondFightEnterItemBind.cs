// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/diamondFight/DiamondFightEnterItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DiamondFight
{
    public partial class DiamondFightEnterItemBind : BaseView
    {
        public Image _img_bg;
        public TextMeshProUGUI _lb_index;
        public TextMeshProUGUI _lb_value;
        public Image _img_icon;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_lb_index), _lb_index);
            EnsureBound(nameof(_lb_value), _lb_value);
            EnsureBound(nameof(_img_icon), _img_icon);
        }
    }
}
