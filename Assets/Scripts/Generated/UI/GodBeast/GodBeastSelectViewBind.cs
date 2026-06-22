// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/godBeast/GodBeastSelectView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GodBeast
{
    public partial class GodBeastSelectViewBind : BaseView
    {
        public Image _Image1;
        public Image _Image2;
        public Image _Image3;
        public Image _btn_close;
        public TextMeshProUGUI _lb_tip;
        public ScrollRect _Scroller1;
        public ScrollRect _group_item;
        public RectTransform _group_empty;
        public Image _Image6;
        public TextMeshProUGUI _Label1;
        public RectTransform _btn_get;
        public Image _Image11;
        public TextMeshProUGUI labelDisplay;
        public GameObject _tpl_GodBeastSelectItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(_btn_close), _btn_close);
            EnsureBound(nameof(_lb_tip), _lb_tip);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(_group_item), _group_item);
            EnsureBound(nameof(_group_empty), _group_empty);
            EnsureBound(nameof(_Image6), _Image6);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_btn_get), _btn_get);
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(_tpl_GodBeastSelectItem), _tpl_GodBeastSelectItem);
        }
    }
}
