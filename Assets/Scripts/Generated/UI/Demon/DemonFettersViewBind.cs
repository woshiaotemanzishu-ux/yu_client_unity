// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/demon/DemonFettersView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Demon
{
    public partial class DemonFettersViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_close;
        public Image _Image1;
        public Image _Image4;
        public TextMeshProUGUI title;
        public ScrollRect _Scroller1;
        public GameObject _tpl_DemonFettersItem;
        public GameObject _tpl_DemonFettersHeadItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image4), _Image4);
            EnsureBound(nameof(title), title);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(_tpl_DemonFettersItem), _tpl_DemonFettersItem);
            EnsureBound(nameof(_tpl_DemonFettersHeadItem), _tpl_DemonFettersHeadItem);
        }
    }
}
