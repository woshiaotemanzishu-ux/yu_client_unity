// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/holyTerritory/HTGuildRankRenderer.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.HolyTerritory
{
    public partial class HTGuildRankRendererBind : BaseView
    {
        public RectTransform _Group1;
        public Image _Image1;
        public Image topImg;
        public TextMeshProUGUI rankLabel;
        public TextMeshProUGUI nameLabel;
        public TextMeshProUGUI chiefLabel;
        public TextMeshProUGUI memberLabel;
        public TextMeshProUGUI fightLabel;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(topImg), topImg);
            EnsureBound(nameof(rankLabel), rankLabel);
            EnsureBound(nameof(nameLabel), nameLabel);
            EnsureBound(nameof(chiefLabel), chiefLabel);
            EnsureBound(nameof(memberLabel), memberLabel);
            EnsureBound(nameof(fightLabel), fightLabel);
        }
    }
}
