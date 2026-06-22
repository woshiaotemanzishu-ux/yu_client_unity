// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/holyTerritory/HTRoleRankRenderer.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.HolyTerritory
{
    public partial class HTRoleRankRendererBind : BaseView
    {
        public RectTransform _Group1;
        public Image _Image1;
        public TextMeshProUGUI rankLabel;
        public TextMeshProUGUI nameLabel;
        public TextMeshProUGUI fightLabel;
        public TextMeshProUGUI dsgtLabel;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(rankLabel), rankLabel);
            EnsureBound(nameof(nameLabel), nameLabel);
            EnsureBound(nameof(fightLabel), fightLabel);
            EnsureBound(nameof(dsgtLabel), dsgtLabel);
        }
    }
}
