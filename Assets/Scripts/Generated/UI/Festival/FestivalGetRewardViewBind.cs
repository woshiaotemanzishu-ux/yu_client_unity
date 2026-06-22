// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/festival/FestivalGetRewardView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Festival
{
    public partial class FestivalGetRewardViewBind : BaseView
    {
        public Image bgImg;
        public TextMeshProUGUI titleLab;
        public TextMeshProUGUI descLab2;
        public TextMeshProUGUI descLab1;
        public Image bgImg2;
        public ScrollRect getList;
        public Image bgImg1;
        public ScrollRect notList;
        public RectTransform jumpBox;
        public TextMeshProUGUI descLab;
        public Image cost1Img;
        public TextMeshProUGUI buy1Lab;
        public TextMeshProUGUI cost1Lab;
        public TextMeshProUGUI num1Lab;
        public RectTransform closeBtn;
        public GameObject _tpl_FestivalRewardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bgImg), bgImg);
            EnsureBound(nameof(titleLab), titleLab);
            EnsureBound(nameof(descLab2), descLab2);
            EnsureBound(nameof(descLab1), descLab1);
            EnsureBound(nameof(bgImg2), bgImg2);
            EnsureBound(nameof(getList), getList);
            EnsureBound(nameof(bgImg1), bgImg1);
            EnsureBound(nameof(notList), notList);
            EnsureBound(nameof(jumpBox), jumpBox);
            EnsureBound(nameof(descLab), descLab);
            EnsureBound(nameof(cost1Img), cost1Img);
            EnsureBound(nameof(buy1Lab), buy1Lab);
            EnsureBound(nameof(cost1Lab), cost1Lab);
            EnsureBound(nameof(num1Lab), num1Lab);
            EnsureBound(nameof(closeBtn), closeBtn);
            EnsureBound(nameof(_tpl_FestivalRewardItem), _tpl_FestivalRewardItem);
        }
    }
}
