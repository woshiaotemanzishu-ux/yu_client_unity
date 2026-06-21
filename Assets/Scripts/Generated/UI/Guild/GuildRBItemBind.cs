// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guild/GuildRBItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Guild
{
    public partial class GuildRBItemBind : BaseView
    {
        public Image _bg;
        public RectTransform _gp_reward;
        public Image _Image11;
        public TextMeshProUGUI _lb_title;
        public TextMeshProUGUI _lb_role;
        public TextMeshProUGUI _lb_type;
        public RectTransform _btn;
        public Image _Image1;
        public TextMeshProUGUI labelDisplay;
        public TextMeshProUGUI _lb_time;
        public Image _red;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_bg), _bg);
            EnsureBound(nameof(_gp_reward), _gp_reward);
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_lb_role), _lb_role);
            EnsureBound(nameof(_lb_type), _lb_type);
            EnsureBound(nameof(_btn), _btn);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(_lb_time), _lb_time);
            EnsureBound(nameof(_red), _red);
        }
    }
}
