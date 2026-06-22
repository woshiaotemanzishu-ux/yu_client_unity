// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/godequipbuild/GodEquipBuildAttrItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Godequipbuild
{
    public partial class GodEquipBuildAttrItemBind : BaseView
    {
        public RectTransform _gp_show;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_attr;
        public Image _img_up;
        public TextMeshProUGUI _lb_attr_up;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_gp_show), _gp_show);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_attr), _lb_attr);
            EnsureBound(nameof(_img_up), _img_up);
            EnsureBound(nameof(_lb_attr_up), _lb_attr_up);
        }
    }
}
