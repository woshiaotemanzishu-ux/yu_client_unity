// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/attributePotion/attributePotionView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.AttributePotion
{
    public partial class AttributePotionViewBind : BaseView
    {
        public Image _Image1;
        public RectTransform _btn_close;
        public Image _Image11;
        public ScrollRect _Scroller1;
        public RectTransform Content;
        public ScrollRect Content1;
        public GameObject _tpl_attributePotionItem;
        public GameObject _tpl_attributePotionProgressBar;
        public GameObject _tpl_attributePotionTab;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_btn_close), _btn_close);
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(Content1), Content1);
            EnsureBound(nameof(_tpl_attributePotionItem), _tpl_attributePotionItem);
            EnsureBound(nameof(_tpl_attributePotionProgressBar), _tpl_attributePotionProgressBar);
            EnsureBound(nameof(_tpl_attributePotionTab), _tpl_attributePotionTab);
        }
    }
}
