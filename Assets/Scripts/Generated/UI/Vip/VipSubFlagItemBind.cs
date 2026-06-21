// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/vip/VipSubFlagItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Vip
{
    public partial class VipSubFlagItemBind : BaseView
    {
        public Image image_flag;
        public TextMeshProUGUI html_lable_flag;

        protected override void BindNodes()
        {
            EnsureBound(nameof(image_flag), image_flag);
            EnsureBound(nameof(html_lable_flag), html_lable_flag);
        }
    }
}
