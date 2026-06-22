// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/eudaemon/EudaemonShopTaskItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Eudaemon
{
    public partial class EudaemonShopTaskItemBind : BaseView
    {
        public Image _img_bg;
        public Image _img_bg2;
        public Image _img_icon;
        public Image _img_bg3;
        public TextMeshProUGUI _lb_count;
        public TextMeshProUGUI _html_tips;
        public RectTransform _box_go;
        public Image _img_go;
        public TextMeshProUGUI _lb_go;
        public Image _img_go_red;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_img_bg3), _img_bg3);
            EnsureBound(nameof(_lb_count), _lb_count);
            EnsureBound(nameof(_html_tips), _html_tips);
            EnsureBound(nameof(_box_go), _box_go);
            EnsureBound(nameof(_img_go), _img_go);
            EnsureBound(nameof(_lb_go), _lb_go);
            EnsureBound(nameof(_img_go_red), _img_go_red);
        }
    }
}
