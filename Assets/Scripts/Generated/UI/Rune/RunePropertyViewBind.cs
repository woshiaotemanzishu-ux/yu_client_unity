// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/rune/RunePropertyView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Rune
{
    public partial class RunePropertyViewBind : BaseView
    {
        public Image property_bg;
        public TextMeshProUGUI property_title;
        public Image _Image1;
        public ScrollRect scroll;
        public RectTransform Content;
        public RectTransform none_conta;
        public TextMeshProUGUI tips;
        public Image tips_icon;
        public GameObject _tpl_RunePropertyItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(property_bg), property_bg);
            EnsureBound(nameof(property_title), property_title);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(scroll), scroll);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(none_conta), none_conta);
            EnsureBound(nameof(tips), tips);
            EnsureBound(nameof(tips_icon), tips_icon);
            EnsureBound(nameof(_tpl_RunePropertyItem), _tpl_RunePropertyItem);
        }
    }
}
