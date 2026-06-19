// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/mainStronger/MainUIStrongerBtn.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainStronger
{
    public partial class MainUIStrongerBtnBind : BaseView
    {
        public Image imgBtn;
        public TextMeshProUGUI lblName;

        protected override void BindNodes()
        {
            EnsureBound(nameof(imgBtn), imgBtn);
            EnsureBound(nameof(lblName), lblName);
        }
    }
}
