// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kfStart/kfStageView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.KfStart
{
    public partial class KfStageViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_bg2;
        public Image _img_title;
        public TextMeshProUGUI _lb_title;
        public Image _btn_close;
        public Image _img_title2;
        public TextMeshProUGUI _lb_title1;
        public TextMeshProUGUI _lb_title2;
        public TextMeshProUGUI _lb_title3;
        public TextMeshProUGUI _lb_title4;
        public ScrollRect _gp_con;
        public GameObject _tpl_kfStageItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_btn_close), _btn_close);
            EnsureBound(nameof(_img_title2), _img_title2);
            EnsureBound(nameof(_lb_title1), _lb_title1);
            EnsureBound(nameof(_lb_title2), _lb_title2);
            EnsureBound(nameof(_lb_title3), _lb_title3);
            EnsureBound(nameof(_lb_title4), _lb_title4);
            EnsureBound(nameof(_gp_con), _gp_con);
            EnsureBound(nameof(_tpl_kfStageItem), _tpl_kfStageItem);
        }
    }
}
