// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guild/GuildMainItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Guild
{
    public partial class GuildMainItemBind : BaseView
    {
        public Image _img_icon;
        public TextMeshProUGUI _lb_name;
        public Image _reddot;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_reddot), _reddot);
        }
    }
}
