// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/mainUI/MainUIFightModeItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainUI
{
    public partial class MainUIFightModeItemBind : BaseView
    {
        public Image _img_bg;
        public TextMeshProUGUI _lb_desc;
        public Image _img_status;
        public Image _img_select;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_lb_desc), _lb_desc);
            EnsureBound(nameof(_img_status), _img_status);
            EnsureBound(nameof(_img_select), _img_select);
        }
    }
}
