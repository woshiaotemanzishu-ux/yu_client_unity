// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guildList/GuildListItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GuildList
{
    public partial class GuildListItemBind : BaseView
    {
        public Image _Image11;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_master;
        public TextMeshProUGUI _lb_member;
        public TextMeshProUGUI _lb_cond;
        public RectTransform _btn_apply;
        public Image _Image1;
        public TextMeshProUGUI labelDisplay;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_master), _lb_master);
            EnsureBound(nameof(_lb_member), _lb_member);
            EnsureBound(nameof(_lb_cond), _lb_cond);
            EnsureBound(nameof(_btn_apply), _btn_apply);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(labelDisplay), labelDisplay);
        }
    }
}
