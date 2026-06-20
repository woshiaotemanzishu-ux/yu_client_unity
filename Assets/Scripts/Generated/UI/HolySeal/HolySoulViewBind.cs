// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/holySeal/HolySoulView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.HolySeal
{
    public partial class HolySoulViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_bg2;
        public Image _img_bg3;
        public Image _img_bg4;
        public Image _img_close;
        public ScrollRect _scroll_item;
        public RectTransform _gp_item_con;
        public TextMeshProUGUI _lb_title;
        public GameObject _tpl_HolySoulItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_img_bg3), _img_bg3);
            EnsureBound(nameof(_img_bg4), _img_bg4);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_scroll_item), _scroll_item);
            EnsureBound(nameof(_gp_item_con), _gp_item_con);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_tpl_HolySoulItem), _tpl_HolySoulItem);
        }
    }
}
