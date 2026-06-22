// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/godequipbuild/GodEquipBMaterialsItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Godequipbuild
{
    public partial class GodEquipBMaterialsItemBind : BaseView
    {
        public Image icon_bg;
        public RectTransform item_group;
        public Image lock_img;
        public Image add_img;
        public TextMeshProUGUI num_label;
        public RectTransform _mask;

        protected override void BindNodes()
        {
            EnsureBound(nameof(icon_bg), icon_bg);
            EnsureBound(nameof(item_group), item_group);
            EnsureBound(nameof(lock_img), lock_img);
            EnsureBound(nameof(add_img), add_img);
            EnsureBound(nameof(num_label), num_label);
            EnsureBound(nameof(_mask), _mask);
        }
    }
}
