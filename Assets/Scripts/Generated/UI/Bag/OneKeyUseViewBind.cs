// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/bag/OneKeyUseView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Bag
{
    public partial class OneKeyUseViewBind : BaseView
    {
        public Image _img_bg;
        public Image closeBtn;
        public Image _Image3;
        public TextMeshProUGUI _Label1;
        public Image _Image4;
        public Image _Image5;
        public ScrollRect _Scroller1;
        public RectTransform Content1;
        public ScrollRect _Scroller2;
        public TextMeshProUGUI nothingLb;
        public RectTransform useBtn;
        public GameObject _tpl_OneKeyUseTab;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(closeBtn), closeBtn);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_Image4), _Image4);
            EnsureBound(nameof(_Image5), _Image5);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(Content1), Content1);
            EnsureBound(nameof(_Scroller2), _Scroller2);
            EnsureBound(nameof(nothingLb), nothingLb);
            EnsureBound(nameof(useBtn), useBtn);
            EnsureBound(nameof(_tpl_OneKeyUseTab), _tpl_OneKeyUseTab);
        }
    }
}
