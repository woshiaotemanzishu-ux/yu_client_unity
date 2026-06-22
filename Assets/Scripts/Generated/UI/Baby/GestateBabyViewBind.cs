// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/baby/GestateBabyView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Baby
{
    public partial class GestateBabyViewBind : BaseView
    {
        public Image bg1;
        public Image bg2;
        public Image _Image4;
        public Image closeBtn;
        public TextMeshProUGUI _Label1;
        public RectTransform itemGp;
        public RectTransform gestateBtn;
        public Image _Image1;
        public TextMeshProUGUI labelDisplay;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg1), bg1);
            EnsureBound(nameof(bg2), bg2);
            EnsureBound(nameof(_Image4), _Image4);
            EnsureBound(nameof(closeBtn), closeBtn);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(itemGp), itemGp);
            EnsureBound(nameof(gestateBtn), gestateBtn);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
