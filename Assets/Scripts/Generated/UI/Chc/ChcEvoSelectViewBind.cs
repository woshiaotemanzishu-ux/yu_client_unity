// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/chc/chcEvoSelectView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Chc
{
    public partial class ChcEvoSelectViewBind : BaseView
    {
        public Image _Image1;
        public Image _Image2;
        public Image _Image3;
        public Image _btn_close;
        public TextMeshProUGUI _lb_title;
        public TextMeshProUGUI _lb_tips;
        public TextMeshProUGUI _lb_ratio;
        public RectTransform _btn_go;
        public Image _Image5;
        public TextMeshProUGUI _Label1;
        public ScrollRect _Scroller1;
        public GameObject _tpl_chcEvoSelectItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(_btn_close), _btn_close);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_lb_tips), _lb_tips);
            EnsureBound(nameof(_lb_ratio), _lb_ratio);
            EnsureBound(nameof(_btn_go), _btn_go);
            EnsureBound(nameof(_Image5), _Image5);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(_tpl_chcEvoSelectItem), _tpl_chcEvoSelectItem);
        }
    }
}
