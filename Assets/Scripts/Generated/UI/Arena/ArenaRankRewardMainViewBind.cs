// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/arena/ArenaRankRewardMainView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Arena
{
    public partial class ArenaRankRewardMainViewBind : BaseView
    {
        public Image _bg_img;
        public Image _img_close;
        public RectTransform _Group1;
        public Image _Image1;
        public Image _top_img;
        public Image _Image2;
        public TextMeshProUGUI _lb_titile;
        public ScrollRect scroll;
        public RectTransform _view_group;
        public GameObject _tpl_ArenaRankTabItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_bg_img), _bg_img);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_top_img), _top_img);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_lb_titile), _lb_titile);
            EnsureBound(nameof(scroll), scroll);
            EnsureBound(nameof(_view_group), _view_group);
            EnsureBound(nameof(_tpl_ArenaRankTabItem), _tpl_ArenaRankTabItem);
        }
    }
}
