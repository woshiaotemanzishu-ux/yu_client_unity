// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/pet/PetProptityView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Pet
{
    public partial class PetProptityViewBind : BaseView
    {
        public RectTransform _Group1;
        public Image click;
        public Image property_bg;
        public Image _Image1;
        public TextMeshProUGUI property_title;
        public ScrollRect prop_scroller;
        public RectTransform Content;
        public TextMeshProUGUI prop_text;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(click), click);
            EnsureBound(nameof(property_bg), property_bg);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(property_title), property_title);
            EnsureBound(nameof(prop_scroller), prop_scroller);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(prop_text), prop_text);
        }
    }
}
