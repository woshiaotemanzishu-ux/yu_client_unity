// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guildFight/GuildFightWiningRewardItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GuildFight
{
    public partial class GuildFightWiningRewardItemBind : BaseView
    {
        public Image _Image1;
        public TextMeshProUGUI title;
        public RectTransform btn;
        public Image _Image11;
        public TextMeshProUGUI labelDisplay;
        public ScrollRect scroll;
        public RectTransform Content;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(title), title);
            EnsureBound(nameof(btn), btn);
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(scroll), scroll);
            EnsureBound(nameof(Content), Content);
        }
    }
}
