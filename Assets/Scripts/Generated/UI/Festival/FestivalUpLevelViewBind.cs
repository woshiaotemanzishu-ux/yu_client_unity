// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/festival/FestivalUpLevelView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Festival
{
    public partial class FestivalUpLevelViewBind : BaseView
    {
        public RectTransform cancelBtn;
        public RectTransform buyBtn;
        public RectTransform closeBtn;
        public Image close;
        public TextMeshProUGUI hasNumLab;
        public TextMeshProUGUI numLab;
        public TextMeshProUGUI curLvLab;
        public TextMeshProUGUI nextLvLab;
        public RectTransform barHSlider;
        public Image barImg;

        protected override void BindNodes()
        {
            EnsureBound(nameof(cancelBtn), cancelBtn);
            EnsureBound(nameof(buyBtn), buyBtn);
            EnsureBound(nameof(closeBtn), closeBtn);
            EnsureBound(nameof(close), close);
            EnsureBound(nameof(hasNumLab), hasNumLab);
            EnsureBound(nameof(numLab), numLab);
            EnsureBound(nameof(curLvLab), curLvLab);
            EnsureBound(nameof(nextLvLab), nextLvLab);
            EnsureBound(nameof(barHSlider), barHSlider);
            EnsureBound(nameof(barImg), barImg);
        }
    }
}
