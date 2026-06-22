// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/eternity/EternityDamageItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Eternity
{
    public partial class EternityDamageItemBind : BaseView
    {
        public Image _Image1;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_ratio;
        public ScrollRect _Scroller1;
        public RectTransform Content;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_ratio), _lb_ratio);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
