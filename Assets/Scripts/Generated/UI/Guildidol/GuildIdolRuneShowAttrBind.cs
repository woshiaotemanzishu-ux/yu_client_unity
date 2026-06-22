// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guildidol/GuildIdolRuneShowAttr.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Guildidol
{
    public partial class GuildIdolRuneShowAttrBind : BaseView
    {
        public RectTransform _Group1;
        public Image _bg;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_attr;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_bg), _bg);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_attr), _lb_attr);
        }
    }
}
