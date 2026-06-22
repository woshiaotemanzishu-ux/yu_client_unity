// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonPartner/DungeonPartnerStageRewardView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonPartner
{
    public partial class DungeonPartnerStageRewardViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_title;
        public TextMeshProUGUI _lb_title;
        public Image _img_close;
        public Image _img_tip;
        public TextMeshProUGUI _lb_tip;
        public ScrollRect _panel_reward;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_img_tip), _img_tip);
            EnsureBound(nameof(_lb_tip), _lb_tip);
            EnsureBound(nameof(_panel_reward), _panel_reward);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
