// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/brightSea/BrightSeaBattleResultView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.BrightSea
{
    public partial class BrightSeaBattleResultViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_title;
        public RectTransform _gp_base;
        public RectTransform _gp_victroy;
        public RectTransform _gp_success;
        public RectTransform _Group4;
        public RectTransform _Group3;
        public Image _img_bg_group3;
        public Image _img_fail_reward;
        public Image _img_win_reward;
        public ScrollRect _Scroller1;
        public RectTransform _gp_reward_con;
        public RectTransform _con_left;
        public RectTransform _con_right;
        public Image _img_vs;
        public TextMeshProUGUI _lb_count_time;
        public RectTransform _ok_btn;
        public Image _Image1;
        public TextMeshProUGUI labelDisplay;
        public GameObject _tpl_BrightSeaBattleResultItem;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_gp_base), _gp_base);
            EnsureBound(nameof(_gp_victroy), _gp_victroy);
            EnsureBound(nameof(_gp_success), _gp_success);
            EnsureBound(nameof(_Group4), _Group4);
            EnsureBound(nameof(_Group3), _Group3);
            EnsureBound(nameof(_img_bg_group3), _img_bg_group3);
            EnsureBound(nameof(_img_fail_reward), _img_fail_reward);
            EnsureBound(nameof(_img_win_reward), _img_win_reward);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(_gp_reward_con), _gp_reward_con);
            EnsureBound(nameof(_con_left), _con_left);
            EnsureBound(nameof(_con_right), _con_right);
            EnsureBound(nameof(_img_vs), _img_vs);
            EnsureBound(nameof(_lb_count_time), _lb_count_time);
            EnsureBound(nameof(_ok_btn), _ok_btn);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(_tpl_BrightSeaBattleResultItem), _tpl_BrightSeaBattleResultItem);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
