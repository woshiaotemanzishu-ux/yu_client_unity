// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/festival/FestivalUpLevelTipsView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Festival
{
    public partial class FestivalUpLevelTipsViewBind : BaseView
    {
        public TextMeshProUGUI costNum1Lab;
        public TextMeshProUGUI costNum2Lab;
        public TextMeshProUGUI levelLab;
        public RectTransform closeBtn;
        public RectTransform cancelBtn;
        public RectTransform buyBtn;
        public RectTransform selectBox;
        public Image select1Img;
        public Image select2Img;

        protected override void BindNodes()
        {
            EnsureBound(nameof(costNum1Lab), costNum1Lab);
            EnsureBound(nameof(costNum2Lab), costNum2Lab);
            EnsureBound(nameof(levelLab), levelLab);
            EnsureBound(nameof(closeBtn), closeBtn);
            EnsureBound(nameof(cancelBtn), cancelBtn);
            EnsureBound(nameof(buyBtn), buyBtn);
            EnsureBound(nameof(selectBox), selectBox);
            EnsureBound(nameof(select1Img), select1Img);
            EnsureBound(nameof(select2Img), select2Img);
        }
    }
}
