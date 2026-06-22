// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/arena/ArenaRankTabItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Arena
{
    public partial class ArenaRankTabItemBind : BaseView
    {
        public Image _Image1;
        public Image _Image2;
        public TextMeshProUGUI _lb;
        public Image redDot_img;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_lb), _lb);
            EnsureBound(nameof(redDot_img), redDot_img);
        }
    }
}
