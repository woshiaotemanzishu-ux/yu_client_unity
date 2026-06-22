// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonPolar/DungeonPolarRewardItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonPolar
{
    public partial class DungeonPolarRewardItemBind : BaseView
    {
        public Image _bg;
        public Image _Image1_1;
        public ScrollRect _Scroller1;
        public RectTransform Content;
        public TextMeshProUGUI _lb_name;
        public Image _icon;
        public Image _icon_get;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_bg), _bg);
            EnsureBound(nameof(_Image1_1), _Image1_1);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_icon), _icon);
            EnsureBound(nameof(_icon_get), _icon_get);
        }
    }
}
