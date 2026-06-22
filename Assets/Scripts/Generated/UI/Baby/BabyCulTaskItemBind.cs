// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/baby/BabyCulTaskItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Baby
{
    public partial class BabyCulTaskItemBind : BaseView
    {
        public Image _Image1;
        public Image _Image2;
        public Image finishImg;
        public TextMeshProUGUI taskDes;
        public TextMeshProUGUI _Label1;
        public TextMeshProUGUI rewardLb;
        public Image rewardImg;
        public RectTransform goBtn;
        public Image _Image11;
        public TextMeshProUGUI labelDisplay;
        public RectTransform getBtn;
        public Image _Image111;
        public TextMeshProUGUI labelDisplay1;
        public Image reddot;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(finishImg), finishImg);
            EnsureBound(nameof(taskDes), taskDes);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(rewardLb), rewardLb);
            EnsureBound(nameof(rewardImg), rewardImg);
            EnsureBound(nameof(goBtn), goBtn);
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(getBtn), getBtn);
            EnsureBound(nameof(_Image111), _Image111);
            EnsureBound(nameof(labelDisplay1), labelDisplay1);
            EnsureBound(nameof(reddot), reddot);
        }
    }
}
