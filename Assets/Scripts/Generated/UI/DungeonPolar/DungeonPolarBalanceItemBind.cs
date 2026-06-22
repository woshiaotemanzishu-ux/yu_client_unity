// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonPolar/DungeonPolarBalanceItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonPolar
{
    public partial class DungeonPolarBalanceItemBind : BaseView
    {
        public Image _Image1;
        public TextMeshProUGUI _lb_name;
        public Image _Image2;
        public ScrollRect Content;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(Content), Content);
        }
    }
}
