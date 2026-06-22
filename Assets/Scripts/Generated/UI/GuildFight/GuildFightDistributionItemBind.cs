// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guildFight/GuildFightDistributionItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GuildFight
{
    public partial class GuildFightDistributionItemBind : BaseView
    {
        public Image _Image1;
        public TextMeshProUGUI role_name;
        public TextMeshProUGUI pos_name;
        public TextMeshProUGUI fighting;
        public TextMeshProUGUI distribution;
        public RectTransform _Group1;
        public Image toggleBtn;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(role_name), role_name);
            EnsureBound(nameof(pos_name), pos_name);
            EnsureBound(nameof(fighting), fighting);
            EnsureBound(nameof(distribution), distribution);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(toggleBtn), toggleBtn);
        }
    }
}
