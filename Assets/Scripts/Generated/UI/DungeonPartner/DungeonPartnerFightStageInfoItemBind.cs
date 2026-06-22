// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonPartner/DungeonPartnerFightStageInfoItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonPartner
{
    public partial class DungeonPartnerFightStageInfoItemBind : BaseView
    {
        public Image _img_bg;
        public TextMeshProUGUI _lb_title;
        public ScrollRect _panel_reward;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_panel_reward), _panel_reward);
        }
    }
}
