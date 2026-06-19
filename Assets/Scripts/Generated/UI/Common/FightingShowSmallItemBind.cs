// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/common/FightingShowSmallItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Common
{
    public partial class FightingShowSmallItemBind : BaseView
    {
        public RectTransform _gp_fire_effect;
        public Image _img;
        public RectTransform _Group1;
        public Image _img_2;
        public TextMeshProUGUI _lb_fighting;
        public RectTransform _box_up;
        public GameObject _tpl_FightingUpItem;
        public GameObject _tpl_WithBtnHSlider;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_gp_fire_effect), _gp_fire_effect);
            EnsureBound(nameof(_img), _img);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_img_2), _img_2);
            EnsureBound(nameof(_lb_fighting), _lb_fighting);
            EnsureBound(nameof(_box_up), _box_up);
            EnsureBound(nameof(_tpl_FightingUpItem), _tpl_FightingUpItem);
            EnsureBound(nameof(_tpl_WithBtnHSlider), _tpl_WithBtnHSlider);
        }
    }
}
