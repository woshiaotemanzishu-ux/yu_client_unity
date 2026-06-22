// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/alert/TheCheckBox.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Alert
{
    public partial class TheCheckBoxBind : BaseView
    {
        public Image checkImg;
        public TextMeshProUGUI checkLabel;

        protected override void BindNodes()
        {
            EnsureBound(nameof(checkImg), checkImg);
            EnsureBound(nameof(checkLabel), checkLabel);
        }
    }
}
