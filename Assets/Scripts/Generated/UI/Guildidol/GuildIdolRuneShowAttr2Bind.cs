// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guildidol/GuildIdolRuneShowAttr2.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Guildidol
{
    public partial class GuildIdolRuneShowAttr2Bind : BaseView
    {
        public Image _bg;
        public TextMeshProUGUI _lb_left;
        public RectTransform _gp_next;
        public TextMeshProUGUI _lb_right;
        public Image _img_add;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_bg), _bg);
            EnsureBound(nameof(_lb_left), _lb_left);
            EnsureBound(nameof(_gp_next), _gp_next);
            EnsureBound(nameof(_lb_right), _lb_right);
            EnsureBound(nameof(_img_add), _img_add);
        }
    }
}
