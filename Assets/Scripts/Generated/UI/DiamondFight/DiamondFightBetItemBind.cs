// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/diamondFight/DiamondFightBetItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DiamondFight
{
    public partial class DiamondFightBetItemBind : BaseView
    {
        public Image _img_bg;
        public Image _img_bg2;
        public Image _img_right;
        public Image _img_beted;
        public TextMeshProUGUI _lb_name0;
        public TextMeshProUGUI _lb_name1;
        public RectTransform _box_head0;
        public RectTransform _box_head1;
        public RectTransform _box_bet;
        public Image _img_bet;
        public TextMeshProUGUI _lb_bet;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_img_right), _img_right);
            EnsureBound(nameof(_img_beted), _img_beted);
            EnsureBound(nameof(_lb_name0), _lb_name0);
            EnsureBound(nameof(_lb_name1), _lb_name1);
            EnsureBound(nameof(_box_head0), _box_head0);
            EnsureBound(nameof(_box_head1), _box_head1);
            EnsureBound(nameof(_box_bet), _box_bet);
            EnsureBound(nameof(_img_bet), _img_bet);
            EnsureBound(nameof(_lb_bet), _lb_bet);
        }
    }
}
