// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/longlanguage/longlangSBaseView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Longlanguage
{
    public partial class LonglangSBaseViewBind : BaseView
    {
        public Image _bg;
        public Image _img_title;
        public TextMeshProUGUI _lb_title;
        public Image _btn_close;
        public ScrollRect _scroller;
        public RectTransform sub_group;
        public GameObject _tpl_longlangDecTab;
        public GameObject _tpl_longlangDecView;
        public GameObject _tpl_longlangDisView;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_bg), _bg);
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_btn_close), _btn_close);
            EnsureBound(nameof(_scroller), _scroller);
            EnsureBound(nameof(sub_group), sub_group);
            EnsureBound(nameof(_tpl_longlangDecTab), _tpl_longlangDecTab);
            EnsureBound(nameof(_tpl_longlangDecView), _tpl_longlangDecView);
            EnsureBound(nameof(_tpl_longlangDisView), _tpl_longlangDisView);
        }
    }
}
