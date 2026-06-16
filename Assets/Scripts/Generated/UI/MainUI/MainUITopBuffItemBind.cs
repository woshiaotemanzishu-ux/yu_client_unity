// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/mainUI/MainUITopBuffItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainUI
{
    public partial class MainUITopBuffItemBind : BaseView
    {
        public Image _img_bg;
        public Image _img_mask;
        public TextMeshProUGUI _lb_time;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_mask), _img_mask);
            EnsureBound(nameof(_lb_time), _lb_time);
        }
    }
}
