// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/growthForce/GrowthForceTabItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GrowthForce
{
    public partial class GrowthForceTabItemBind : BaseView
    {
        public RectTransform btnBox;
        public Image bgImg;
        public TextMeshProUGUI nameLab;
        public Image redImg;

        protected override void BindNodes()
        {
            EnsureBound(nameof(btnBox), btnBox);
            EnsureBound(nameof(bgImg), bgImg);
            EnsureBound(nameof(nameLab), nameLab);
            EnsureBound(nameof(redImg), redImg);
        }
    }
}
