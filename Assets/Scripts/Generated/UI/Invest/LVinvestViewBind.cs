// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/invest/LVinvestView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Invest
{
    public partial class LVinvestViewBind : BaseView
    {
        public Image _bg;
        public Image _banner_bg;
        public Image _title_img;
        public ScrollRect Content1;
        public RectTransform Content;
        public RectTransform _gp_btn;
        public Image _Image1;
        public TextMeshProUGUI _lb_btn;
        public TextMeshProUGUI _lb_small_btn;
        public Image _Image2;
        public TextMeshProUGUI _desc_lb;
        public Image buy;
        public GameObject _tpl_LVinvestListView;
        public GameObject _tpl_LVinvestSelectItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_bg), _bg);
            EnsureBound(nameof(_banner_bg), _banner_bg);
            EnsureBound(nameof(_title_img), _title_img);
            EnsureBound(nameof(Content1), Content1);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_gp_btn), _gp_btn);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_lb_btn), _lb_btn);
            EnsureBound(nameof(_lb_small_btn), _lb_small_btn);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_desc_lb), _desc_lb);
            EnsureBound(nameof(buy), buy);
            EnsureBound(nameof(_tpl_LVinvestListView), _tpl_LVinvestListView);
            EnsureBound(nameof(_tpl_LVinvestSelectItem), _tpl_LVinvestSelectItem);
        }
    }
}
