// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/login/LoginLoadingView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Login
{
    public partial class LoginLoadingViewBind : BaseView
    {
        public Image _img_bg;
        public RectTransform _box_con;
        public TextMeshProUGUI _lb_first_load;
        public TextMeshProUGUI _lb_load_progress;
        public TextMeshProUGUI _lb_version;
        public RectTransform _box_progress;
        public Image _img_back;
        public RectTransform _mask_box;
        public Image _img_front;
        public Image _img_progress_end;
        public GameObject _tpl_EyouThAdultItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_box_con), _box_con);
            EnsureBound(nameof(_lb_first_load), _lb_first_load);
            EnsureBound(nameof(_lb_load_progress), _lb_load_progress);
            EnsureBound(nameof(_lb_version), _lb_version);
            EnsureBound(nameof(_box_progress), _box_progress);
            EnsureBound(nameof(_img_back), _img_back);
            EnsureBound(nameof(_mask_box), _mask_box);
            EnsureBound(nameof(_img_front), _img_front);
            EnsureBound(nameof(_img_progress_end), _img_progress_end);
            EnsureBound(nameof(_tpl_EyouThAdultItem), _tpl_EyouThAdultItem);
        }
    }
}
