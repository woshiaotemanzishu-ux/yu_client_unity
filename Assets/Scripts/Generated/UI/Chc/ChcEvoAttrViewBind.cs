// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/chc/chcEvoAttrView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Chc
{
    public partial class ChcEvoAttrViewBind : BaseView
    {
        public Image _Image1;
        public Image _Image2;
        public Image _Image3;
        public Image _btn_close;
        public ScrollRect _Scroller1;
        public TextMeshProUGUI _lb_title;
        public GameObject _tpl_chcEvoAttrItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(_btn_close), _btn_close);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_tpl_chcEvoAttrItem), _tpl_chcEvoAttrItem);
        }
    }
}
