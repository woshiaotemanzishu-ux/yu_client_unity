// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/godCourt/GodCourtStageItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GodCourt
{
    public partial class GodCourtStageItemBind : BaseView
    {
        public RectTransform clickGp;
        public Image bg;
        public RectTransform _gp_effect;
        public Image colorImg;
        public Image icon;
        public Image selectImg;
        public TextMeshProUGUI stageLb;
        public Image redImg;

        protected override void BindNodes()
        {
            EnsureBound(nameof(clickGp), clickGp);
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(_gp_effect), _gp_effect);
            EnsureBound(nameof(colorImg), colorImg);
            EnsureBound(nameof(icon), icon);
            EnsureBound(nameof(selectImg), selectImg);
            EnsureBound(nameof(stageLb), stageLb);
            EnsureBound(nameof(redImg), redImg);
        }
    }
}
