// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/bossField/BossFieldVitItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.BossField
{
    public partial class BossFieldVitItemBind : BaseView
    {
        public Image _img_point;
        public TextMeshProUGUI _lb_desc;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_point), _img_point);
            EnsureBound(nameof(_lb_desc), _lb_desc);
        }
    }
}
