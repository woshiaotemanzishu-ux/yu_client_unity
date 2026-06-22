// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/growthBenefits/GrowthBenefitTaskItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GrowthBenefits
{
    public partial class GrowthBenefitTaskItemBind : BaseView
    {
        public Image bgImg;
        public Image bgImg1;
        public Image getImg;
        public TextMeshProUGUI nameLab;
        public TextMeshProUGUI numLab;
        public ScrollRect awardList;
        public RectTransform jumpBox;
        public RectTransform getBox;
        public Image redImg;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bgImg), bgImg);
            EnsureBound(nameof(bgImg1), bgImg1);
            EnsureBound(nameof(getImg), getImg);
            EnsureBound(nameof(nameLab), nameLab);
            EnsureBound(nameof(numLab), numLab);
            EnsureBound(nameof(awardList), awardList);
            EnsureBound(nameof(jumpBox), jumpBox);
            EnsureBound(nameof(getBox), getBox);
            EnsureBound(nameof(redImg), redImg);
        }
    }
}
