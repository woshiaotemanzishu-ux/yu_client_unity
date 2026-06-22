// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/achv/AchvTotalItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Achv
{
    public partial class AchvTotalItemBind : BaseView
    {
        public Image _left_img;
        public Image _Image1;
        public Image _Image2;
        public Image receivedImg;
        public RectTransform rewardGp;
        public TextMeshProUGUI titleLb;
        public TextMeshProUGUI desLb;
        public RectTransform receiveBtn;
        public Image _Image11;
        public TextMeshProUGUI labelDisplay;
        public TextMeshProUGUI unfinishLb;
        public Image _Image3;
        public Image reddot;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_left_img), _left_img);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(receivedImg), receivedImg);
            EnsureBound(nameof(rewardGp), rewardGp);
            EnsureBound(nameof(titleLb), titleLb);
            EnsureBound(nameof(desLb), desLb);
            EnsureBound(nameof(receiveBtn), receiveBtn);
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(unfinishLb), unfinishLb);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(reddot), reddot);
        }
    }
}
