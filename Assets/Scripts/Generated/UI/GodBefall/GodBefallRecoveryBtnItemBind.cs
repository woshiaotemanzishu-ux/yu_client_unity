// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/godBefall/GodBefallRecoveryBtnItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GodBefall
{
    public partial class GodBefallRecoveryBtnItemBind : BaseView
    {
        public Image img_bg;
        public Image img_select;
        public TextMeshProUGUI lable_btn_name;
        public Image img_red;

        protected override void BindNodes()
        {
            EnsureBound(nameof(img_bg), img_bg);
            EnsureBound(nameof(img_select), img_select);
            EnsureBound(nameof(lable_btn_name), lable_btn_name);
            EnsureBound(nameof(img_red), img_red);
        }
    }
}
