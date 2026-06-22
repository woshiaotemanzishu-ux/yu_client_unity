// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dragonWhisper/dwTab.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DragonWhisper
{
    public partial class DwTabBind : BaseView
    {
        public Image _img_bg;
        public TextMeshProUGUI _lb_floor;
        public Image _img_red;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_lb_floor), _lb_floor);
            EnsureBound(nameof(_img_red), _img_red);
        }
    }
}
