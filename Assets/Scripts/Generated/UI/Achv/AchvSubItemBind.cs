// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/achv/achvSubItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Achv
{
    public partial class AchvSubItemBind : BaseView
    {
        public Image _Image1;
        public RectTransform _box_progress;
        public Image _progress_bg;
        public Image _progress;
        public TextMeshProUGUI expLb;
        public RectTransform roundBox;
        public RectTransform _box1;
        public Image _Image2;
        public TextMeshProUGUI titleLb;
        public Image receivedImg;
        public RectTransform receiveBtn;
        public Image _Image11;
        public TextMeshProUGUI labelDisplay;
        public TextMeshProUGUI unfinishLb;
        public ScrollRect _Scroller1;
        public RectTransform Content;
        public Image reddot;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_box_progress), _box_progress);
            EnsureBound(nameof(_progress_bg), _progress_bg);
            EnsureBound(nameof(_progress), _progress);
            EnsureBound(nameof(expLb), expLb);
            EnsureBound(nameof(roundBox), roundBox);
            EnsureBound(nameof(_box1), _box1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(titleLb), titleLb);
            EnsureBound(nameof(receivedImg), receivedImg);
            EnsureBound(nameof(receiveBtn), receiveBtn);
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(unfinishLb), unfinishLb);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(reddot), reddot);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
