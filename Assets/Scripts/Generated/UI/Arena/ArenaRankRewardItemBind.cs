// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/arena/ArenaRankRewardItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Arena
{
    public partial class ArenaRankRewardItemBind : BaseView
    {
        public Image _img_bg;
        public Image _img_box;
        public RectTransform _Group1;
        public Image _Image1;
        public Image _img_now_tips;
        public Image _state_img;
        public RectTransform _receive_gp;
        public Image _receive_img;
        public Image _Image2;
        public TextMeshProUGUI _Label1;
        public TextMeshProUGUI _lb_rank;
        public RectTransform _Scroller1;
        public Image _Viewport;
        public RectTransform _gp_award_con;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_box), _img_box);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_img_now_tips), _img_now_tips);
            EnsureBound(nameof(_state_img), _state_img);
            EnsureBound(nameof(_receive_gp), _receive_gp);
            EnsureBound(nameof(_receive_img), _receive_img);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_lb_rank), _lb_rank);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(_Viewport), _Viewport);
            EnsureBound(nameof(_gp_award_con), _gp_award_con);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
