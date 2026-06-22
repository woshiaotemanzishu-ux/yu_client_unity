// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonPolar/DungeonPolarPanelItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonPolar
{
    public partial class DungeonPolarPanelItemBind : BaseView
    {
        public RectTransform _box;
        public Image _Image1;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_count;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_box), _box);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_count), _lb_count);
        }
    }
}
