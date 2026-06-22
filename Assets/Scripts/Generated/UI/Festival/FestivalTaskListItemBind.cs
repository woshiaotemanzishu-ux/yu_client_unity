// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/festival/FestivalTaskListItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Festival
{
    public partial class FestivalTaskListItemBind : BaseView
    {
        public Image imgBg1;
        public Image imgBg2;
        public RectTransform iconBox;
        public TextMeshProUGUI numLab;
        public RectTransform getBtn;
        public RectTransform jumpBtn;
        public Image receivedImg;
        public Image redImg;
        public TextMeshProUGUI titleLab;
        public TextMeshProUGUI timesLab;
        public Image barImg;
        public TextMeshProUGUI valueLab;

        protected override void BindNodes()
        {
            EnsureBound(nameof(imgBg1), imgBg1);
            EnsureBound(nameof(imgBg2), imgBg2);
            EnsureBound(nameof(iconBox), iconBox);
            EnsureBound(nameof(numLab), numLab);
            EnsureBound(nameof(getBtn), getBtn);
            EnsureBound(nameof(jumpBtn), jumpBtn);
            EnsureBound(nameof(receivedImg), receivedImg);
            EnsureBound(nameof(redImg), redImg);
            EnsureBound(nameof(titleLab), titleLab);
            EnsureBound(nameof(timesLab), timesLab);
            EnsureBound(nameof(barImg), barImg);
            EnsureBound(nameof(valueLab), valueLab);
        }
    }
}
