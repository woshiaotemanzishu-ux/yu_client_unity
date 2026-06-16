// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/mainUI/FuncBoardView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainUI
{
    public partial class FuncBoardViewBind : BaseView
    {
        public Image content_bg;
        public TextMeshProUGUI _lb_con;
        public TextMeshProUGUI _lb_time;

        protected override void BindNodes()
        {
            EnsureBound(nameof(content_bg), content_bg);
            EnsureBound(nameof(_lb_con), _lb_con);
            EnsureBound(nameof(_lb_time), _lb_time);
        }
    }
}
