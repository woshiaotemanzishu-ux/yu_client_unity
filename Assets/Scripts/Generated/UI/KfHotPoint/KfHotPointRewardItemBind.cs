// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kfHotPoint/kfHotPointRewardItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.KfHotPoint
{
    public partial class KfHotPointRewardItemBind : BaseView
    {
        public Image _img_bg;
        public Image _Image1;
        public TextMeshProUGUI _lb_desc;
        public TextMeshProUGUI _lb_desc1;
        public ScrollRect _Scroller1;
        public RectTransform _gp_reward;
        public RectTransform _btn_draw;
        public Image _img_draw;
        public TextMeshProUGUI _lb_draw;
        public Image _img_red;
        public Image _img_had_get;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_lb_desc), _lb_desc);
            EnsureBound(nameof(_lb_desc1), _lb_desc1);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(_gp_reward), _gp_reward);
            EnsureBound(nameof(_btn_draw), _btn_draw);
            EnsureBound(nameof(_img_draw), _img_draw);
            EnsureBound(nameof(_lb_draw), _lb_draw);
            EnsureBound(nameof(_img_red), _img_red);
            EnsureBound(nameof(_img_had_get), _img_had_get);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
