// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/suitboss/BossHelpItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Suitboss
{
    public partial class BossHelpItemBind : BaseView
    {
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_ratio;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_ratio), _lb_ratio);
        }
    }
}
