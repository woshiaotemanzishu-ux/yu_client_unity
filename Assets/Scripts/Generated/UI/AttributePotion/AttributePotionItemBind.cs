// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/attributePotion/attributePotionItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.AttributePotion
{
    public partial class AttributePotionItemBind : BaseView
    {
        public Image _Image11;
        public RectTransform _gp_reward;
        public RectTransform effBox;
        public TextMeshProUGUI _lb_name;
        public RectTransform _Scroller1;
        public Image _Group1;
        public TextMeshProUGUI _lb_attr;
        public RectTransform _btn_use;
        public Image _Image1;
        public TextMeshProUGUI labelDisplay1;
        public Image _red_dot;
        public RectTransform pbox;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(_gp_reward), _gp_reward);
            EnsureBound(nameof(effBox), effBox);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_lb_attr), _lb_attr);
            EnsureBound(nameof(_btn_use), _btn_use);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(labelDisplay1), labelDisplay1);
            EnsureBound(nameof(_red_dot), _red_dot);
            EnsureBound(nameof(pbox), pbox);
        }
    }
}
