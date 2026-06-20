// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/marriage/MarriageHonourView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Marriage
{
    public partial class MarriageHonourViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_title;
        public TextMeshProUGUI _lb_tips;
        public RectTransform _btn_go;
        public Image _img_go;
        public TextMeshProUGUI _lb_go;
        public Image _btn_close;
        public ScrollRect _gp_con;
        public Image _img_icon;
        public TextMeshProUGUI _lb_tips2;
        public TextMeshProUGUI _lb_honour;
        public GameObject _tpl_MarriageHonourItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_lb_tips), _lb_tips);
            EnsureBound(nameof(_btn_go), _btn_go);
            EnsureBound(nameof(_img_go), _img_go);
            EnsureBound(nameof(_lb_go), _lb_go);
            EnsureBound(nameof(_btn_close), _btn_close);
            EnsureBound(nameof(_gp_con), _gp_con);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_lb_tips2), _lb_tips2);
            EnsureBound(nameof(_lb_honour), _lb_honour);
            EnsureBound(nameof(_tpl_MarriageHonourItem), _tpl_MarriageHonourItem);
        }
    }
}
