// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/brightSea/BrightSeaSchItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.BrightSea
{
    public partial class BrightSeaSchItemBind : BaseView
    {
        public Image _img_bg;
        public Image _img_icon;
        public TextMeshProUGUI _html_server;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_html_server), _html_server);
        }
    }
}
