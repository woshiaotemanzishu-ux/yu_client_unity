// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/mainUI/DropProgress.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainUI
{
    public partial class DropProgressBind : BaseView
    {
        public Image _Image1;
        public RectTransform _gp_progress;
        public Image _progress_bar;
        public Image _img;
        public TextMeshProUGUI _lb_tips;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_gp_progress), _gp_progress);
            EnsureBound(nameof(_progress_bar), _progress_bar);
            EnsureBound(nameof(_img), _img);
            EnsureBound(nameof(_lb_tips), _lb_tips);
        }
    }
}
