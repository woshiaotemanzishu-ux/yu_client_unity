// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/festival/FestivalLevelAwardView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Festival
{
    public partial class FestivalLevelAwardViewBind : BaseView
    {
        public Image barImg;
        public TextMeshProUGUI barLab;
        public TextMeshProUGUI levelLab;
        public Image bgImg1;
        public RectTransform buyBtn;
        public RectTransform getAllBtn;
        public Image redImg;
        public TextMeshProUGUI timeLab;
        public ScrollRect levelList;
        public RectTransform upBtn;
        public ScrollRect orderList;
        public RectTransform effectBox;
        public GameObject _tpl_FestivalAwardListItem;
        public GameObject _tpl_ComActTimerView;

        protected override void BindNodes()
        {
            EnsureBound(nameof(barImg), barImg);
            EnsureBound(nameof(barLab), barLab);
            EnsureBound(nameof(levelLab), levelLab);
            EnsureBound(nameof(bgImg1), bgImg1);
            EnsureBound(nameof(buyBtn), buyBtn);
            EnsureBound(nameof(getAllBtn), getAllBtn);
            EnsureBound(nameof(redImg), redImg);
            EnsureBound(nameof(timeLab), timeLab);
            EnsureBound(nameof(levelList), levelList);
            EnsureBound(nameof(upBtn), upBtn);
            EnsureBound(nameof(orderList), orderList);
            EnsureBound(nameof(effectBox), effectBox);
            EnsureBound(nameof(_tpl_FestivalAwardListItem), _tpl_FestivalAwardListItem);
            EnsureBound(nameof(_tpl_ComActTimerView), _tpl_ComActTimerView);
        }
    }
}
