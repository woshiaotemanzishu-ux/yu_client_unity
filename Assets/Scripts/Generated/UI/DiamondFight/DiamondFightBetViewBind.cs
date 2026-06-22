// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/diamondFight/DiamondFightBetView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DiamondFight
{
    public partial class DiamondFightBetViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_bg2;
        public TextMeshProUGUI _lb_desc;
        public TextMeshProUGUI _lb_left_time;
        public Image _img_title;
        public Image _img_title2;
        public TextMeshProUGUI _lb_title;
        public TextMeshProUGUI _lb_cost_desc;
        public TextMeshProUGUI _lb_get;
        public RectTransform _box_cost;
        public Image _img_icon;
        public Image _img_close;
        public ScrollRect _panel_item;
        public GameObject _tpl_DiamondFightBetItem;
        public GameObject _tpl_DiamondFightRadioItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_lb_desc), _lb_desc);
            EnsureBound(nameof(_lb_left_time), _lb_left_time);
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_img_title2), _img_title2);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_lb_cost_desc), _lb_cost_desc);
            EnsureBound(nameof(_lb_get), _lb_get);
            EnsureBound(nameof(_box_cost), _box_cost);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_panel_item), _panel_item);
            EnsureBound(nameof(_tpl_DiamondFightBetItem), _tpl_DiamondFightBetItem);
            EnsureBound(nameof(_tpl_DiamondFightRadioItem), _tpl_DiamondFightRadioItem);
        }
    }
}
