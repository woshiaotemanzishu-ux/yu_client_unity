// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kf1vn/Kf1vnRoleHpItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Kf1vn
{
    public partial class Kf1vnRoleHpItemBind : BaseView
    {
        public Image progress_bg;
        public Image progress_front;
        public TextMeshProUGUI txt;

        protected override void BindNodes()
        {
            EnsureBound(nameof(progress_bg), progress_bg);
            EnsureBound(nameof(progress_front), progress_front);
            EnsureBound(nameof(txt), txt);
        }
    }
}
