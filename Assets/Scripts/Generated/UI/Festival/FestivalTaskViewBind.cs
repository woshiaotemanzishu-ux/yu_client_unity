// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/festival/FestivalTaskView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Festival
{
    public partial class FestivalTaskViewBind : BaseView
    {
        public TextMeshProUGUI timeLab;
        public ScrollRect tabList;
        public ScrollRect taskList;
        public RectTransform getAllBtn;
        public Image redImg;
        public Image barImg;
        public TextMeshProUGUI levelLab;
        public TextMeshProUGUI barLab;
        public RectTransform upBtn;
        public GameObject _tpl_FestivalTaskListItem;
        public GameObject _tpl_FestivalTaskTabListItem;
        public GameObject _tpl_ComActTimerView;

        protected override void BindNodes()
        {
            EnsureBound(nameof(timeLab), timeLab);
            EnsureBound(nameof(tabList), tabList);
            EnsureBound(nameof(taskList), taskList);
            EnsureBound(nameof(getAllBtn), getAllBtn);
            EnsureBound(nameof(redImg), redImg);
            EnsureBound(nameof(barImg), barImg);
            EnsureBound(nameof(levelLab), levelLab);
            EnsureBound(nameof(barLab), barLab);
            EnsureBound(nameof(upBtn), upBtn);
            EnsureBound(nameof(_tpl_FestivalTaskListItem), _tpl_FestivalTaskListItem);
            EnsureBound(nameof(_tpl_FestivalTaskTabListItem), _tpl_FestivalTaskTabListItem);
            EnsureBound(nameof(_tpl_ComActTimerView), _tpl_ComActTimerView);
        }
    }
}
