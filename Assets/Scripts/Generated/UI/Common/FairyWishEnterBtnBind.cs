// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/common/FairyWishEnterBtn.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Common
{
    public partial class FairyWishEnterBtnBind : BaseView
    {
        public RectTransform effect_con;
        public Image img_btn;
        public Image img_red;
        public RectTransform box_pop;
        public Image img_popo;
        public TextMeshProUGUI htmlContent;

        protected override void BindNodes()
        {
            EnsureBound(nameof(effect_con), effect_con);
            EnsureBound(nameof(img_btn), img_btn);
            EnsureBound(nameof(img_red), img_red);
            EnsureBound(nameof(box_pop), box_pop);
            EnsureBound(nameof(img_popo), img_popo);
            EnsureBound(nameof(htmlContent), htmlContent);
        }
    }
}
