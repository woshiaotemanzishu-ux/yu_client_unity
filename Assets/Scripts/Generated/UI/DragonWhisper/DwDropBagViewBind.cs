// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dragonWhisper/dwDropBagView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DragonWhisper
{
    public partial class DwDropBagViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_bg2;
        public Image _btn_close;
        public Image _gp_title;
        public TextMeshProUGUI _lb_title;
        public ScrollRect _Scroller1;
        public GameObject _tpl_dwDropBagItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_btn_close), _btn_close);
            EnsureBound(nameof(_gp_title), _gp_title);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(_tpl_dwDropBagItem), _tpl_dwDropBagItem);
        }
    }
}
