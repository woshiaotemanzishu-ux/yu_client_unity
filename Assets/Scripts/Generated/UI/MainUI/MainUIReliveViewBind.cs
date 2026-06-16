// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/mainUI/MainUIReliveView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainUI
{
    public partial class MainUIReliveViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_bg2;
        public TextMeshProUGUI _lb_left_count;
        public TextMeshProUGUI _lb_des2;
        public TextMeshProUGUI _lb_des;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_lb_left_count), _lb_left_count);
            EnsureBound(nameof(_lb_des2), _lb_des2);
            EnsureBound(nameof(_lb_des), _lb_des);
        }
    }
}
