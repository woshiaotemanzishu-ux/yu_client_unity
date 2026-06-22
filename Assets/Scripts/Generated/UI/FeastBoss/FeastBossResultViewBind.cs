// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/feastBoss/FeastBossResultView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.FeastBoss
{
    public partial class FeastBossResultViewBind : BaseView
    {
        public Image bg;
        public RectTransform _btn_right;
        public Image _Image1;
        public TextMeshProUGUI labelDisplay;
        public TextMeshProUGUI _lb_left_time;
        public RectTransform _gp_victory;
        public RectTransform _gp_fly;
        public RectTransform _gp_reward_con;
        public Image _img_reward_bg;
        public ScrollRect _sc_reward_col;
        public RectTransform Content;
        public RectTransform _gp_tip_1;
        public Image _Image11;
        public TextMeshProUGUI _lb_tip_1;
        public TextMeshProUGUI none_label;
        public GameObject _tpl_AwardItemRenderer;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(_btn_right), _btn_right);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(_lb_left_time), _lb_left_time);
            EnsureBound(nameof(_gp_victory), _gp_victory);
            EnsureBound(nameof(_gp_fly), _gp_fly);
            EnsureBound(nameof(_gp_reward_con), _gp_reward_con);
            EnsureBound(nameof(_img_reward_bg), _img_reward_bg);
            EnsureBound(nameof(_sc_reward_col), _sc_reward_col);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_gp_tip_1), _gp_tip_1);
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(_lb_tip_1), _lb_tip_1);
            EnsureBound(nameof(none_label), none_label);
            EnsureBound(nameof(_tpl_AwardItemRenderer), _tpl_AwardItemRenderer);
        }
    }
}
