// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guild/GuildDepotRecordItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Guild
{
    public partial class GuildDepotRecordItemBind : BaseView
    {
        public TextMeshProUGUI _lb_content;
        public TextMeshProUGUI euqip;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_lb_content), _lb_content);
            EnsureBound(nameof(euqip), euqip);
        }
    }
}
