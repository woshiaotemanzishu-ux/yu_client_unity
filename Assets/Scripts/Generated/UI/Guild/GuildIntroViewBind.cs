// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guild/GuildIntroView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Guild
{
    public partial class GuildIntroViewBind : BaseView
    {
        public RectTransform _gp_click;
        public RectTransform _gp_data;
        public Image _Image1;
        public TextMeshProUGUI _lb_tips;
        public ScrollRect _list;
        public GameObject _tpl_GuildIntroItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_gp_click), _gp_click);
            EnsureBound(nameof(_gp_data), _gp_data);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_lb_tips), _lb_tips);
            EnsureBound(nameof(_list), _list);
            EnsureBound(nameof(_tpl_GuildIntroItem), _tpl_GuildIntroItem);
        }
    }
}
