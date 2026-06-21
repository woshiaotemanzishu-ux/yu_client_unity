// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guild/GuildRBSendItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Guild
{
    public partial class GuildRBSendItemBind : BaseView
    {
        public Image _bg;
        public Image _Image1;
        public TextMeshProUGUI _lb_title;
        public TextMeshProUGUI _lb_desc;
        public RectTransform _btn;
        public Image _Image11;
        public TextMeshProUGUI labelDisplay;
        public RectTransform _gp_reward;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_bg), _bg);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_lb_desc), _lb_desc);
            EnsureBound(nameof(_btn), _btn);
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(_gp_reward), _gp_reward);
        }
    }
}
