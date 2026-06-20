// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/composite/RingCompositeItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Composite
{
    public partial class RingCompositeItemBind : BaseView
    {
        public RectTransform _Group1;
        public Image _Image1;
        public Image selectimg;
        public TextMeshProUGUI _lb_name;
        public RectTransform _gp;
        public Image _red_dot;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(selectimg), selectimg);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_gp), _gp);
            EnsureBound(nameof(_red_dot), _red_dot);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
