// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonPartner/DungeonPartnerResultView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonPartner
{
    public partial class DungeonPartnerResultViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_bg2;
        public Image _img_bg3;
        public Image _img_bg4;
        public Image _img_star1;
        public Image _img_star2;
        public Image _img_star3;
        public TextMeshProUGUI _lb_reward_tip;
        public Image _img_exit;
        public TextMeshProUGUI _lb_ext;
        public TextMeshProUGUI _html_time;
        public ScrollRect _list_reward;
        public GameObject _tpl_CommonRewardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_img_bg3), _img_bg3);
            EnsureBound(nameof(_img_bg4), _img_bg4);
            EnsureBound(nameof(_img_star1), _img_star1);
            EnsureBound(nameof(_img_star2), _img_star2);
            EnsureBound(nameof(_img_star3), _img_star3);
            EnsureBound(nameof(_lb_reward_tip), _lb_reward_tip);
            EnsureBound(nameof(_img_exit), _img_exit);
            EnsureBound(nameof(_lb_ext), _lb_ext);
            EnsureBound(nameof(_html_time), _html_time);
            EnsureBound(nameof(_list_reward), _list_reward);
            EnsureBound(nameof(_tpl_CommonRewardItem), _tpl_CommonRewardItem);
        }
    }
}
