// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/equipArmor/ArmorAttrView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.EquipArmor
{
    public partial class ArmorAttrViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_title;
        public TextMeshProUGUI _lb_title;
        public ScrollRect _gp_attr;
        public RectTransform Content;
        public RectTransform _gp_none;
        public TextMeshProUGUI _lb_none;
        public Image _img_none;
        public GameObject _tpl_ArmorAttrItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_gp_attr), _gp_attr);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_gp_none), _gp_none);
            EnsureBound(nameof(_lb_none), _lb_none);
            EnsureBound(nameof(_img_none), _img_none);
            EnsureBound(nameof(_tpl_ArmorAttrItem), _tpl_ArmorAttrItem);
        }
    }
}
