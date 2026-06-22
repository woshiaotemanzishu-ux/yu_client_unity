// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonPartner/DungeonPartnerFirstKillView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonPartner
{
    public partial class DungeonPartnerFirstKillViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_bg2;
        public TextMeshProUGUI _lb_title;
        public Image _img_close;
        public Image _img_tips;
        public ScrollRect _panel_reward;
        public GameObject _tpl_DungeonPartnerVsRewardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_img_tips), _img_tips);
            EnsureBound(nameof(_panel_reward), _panel_reward);
            EnsureBound(nameof(_tpl_DungeonPartnerVsRewardItem), _tpl_DungeonPartnerVsRewardItem);
        }
    }
}
