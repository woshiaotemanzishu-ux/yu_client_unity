// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/growthBenefits/GrowthBenefitsView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GrowthBenefits
{
    public partial class GrowthBenefitsViewBind : BaseView
    {
        public ScrollRect tabList;
        public ScrollRect taskList;
        public Image redImg;
        public Image arrowImg;
        public GameObject _tpl_GrowthBenefitTaskItem;
        public GameObject _tpl_GrowthBenefitsAwardItem;
        public GameObject _tpl_GrowthBenefitsTabItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(tabList), tabList);
            EnsureBound(nameof(taskList), taskList);
            EnsureBound(nameof(redImg), redImg);
            EnsureBound(nameof(arrowImg), arrowImg);
            EnsureBound(nameof(_tpl_GrowthBenefitTaskItem), _tpl_GrowthBenefitTaskItem);
            EnsureBound(nameof(_tpl_GrowthBenefitsAwardItem), _tpl_GrowthBenefitsAwardItem);
            EnsureBound(nameof(_tpl_GrowthBenefitsTabItem), _tpl_GrowthBenefitsTabItem);
        }
    }
}
