// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/common/FightingUpItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Common
{
    public partial class FightingUpItemBind : BaseView
    {
        public RectTransform _Group1;
        public Image _img_2;
        public TextMeshProUGUI _lb_fighting;
        public GameObject _tpl_WithBtnHSlider;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_img_2), _img_2);
            EnsureBound(nameof(_lb_fighting), _lb_fighting);
            EnsureBound(nameof(_tpl_WithBtnHSlider), _tpl_WithBtnHSlider);
        }
    }
}
