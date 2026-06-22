// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/country/CountryPayItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Country
{
    public partial class CountryPayItemBind : BaseView
    {
        public Image _Image11;
        public RectTransform getBtn;
        public Image _Image1;
        public TextMeshProUGUI labelDisplay;
        public Image nameImg;
        public RectTransform _Scroller1;
        public RectTransform rewardGp;
        public Image getRed;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(getBtn), getBtn);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(nameImg), nameImg);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(rewardGp), rewardGp);
            EnsureBound(nameof(getRed), getRed);
        }
    }
}
