// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guildFight/GuildFightRewardView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GuildFight
{
    public partial class GuildFightRewardViewBind : BaseView
    {
        public Image _Image1;
        public Image _Image2_title;
        public TextMeshProUGUI _lb_win_name;
        public Image closeBtn;
        public ScrollRect _Scroller1;
        public RectTransform Content;
        public GameObject _tpl_GuildFightRewardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2_title), _Image2_title);
            EnsureBound(nameof(_lb_win_name), _lb_win_name);
            EnsureBound(nameof(closeBtn), closeBtn);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_tpl_GuildFightRewardItem), _tpl_GuildFightRewardItem);
        }
    }
}
