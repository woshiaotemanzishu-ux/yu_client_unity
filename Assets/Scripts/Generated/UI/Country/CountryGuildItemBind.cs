// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/country/CountryGuildItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Country
{
    public partial class CountryGuildItemBind : BaseView
    {
        public Image bg;
        public Image rankImg;
        public TextMeshProUGUI rankLb;
        public TextMeshProUGUI guildLb;
        public TextMeshProUGUI nameLb;
        public TextMeshProUGUI fightLb;
        public TextMeshProUGUI numLb;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(rankImg), rankImg);
            EnsureBound(nameof(rankLb), rankLb);
            EnsureBound(nameof(guildLb), guildLb);
            EnsureBound(nameof(nameLb), nameLb);
            EnsureBound(nameof(fightLb), fightLb);
            EnsureBound(nameof(numLb), numLb);
        }
    }
}
