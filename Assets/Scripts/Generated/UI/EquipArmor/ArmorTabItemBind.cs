// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/equipArmor/ArmorTabItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.EquipArmor
{
    public partial class ArmorTabItemBind : BaseView
    {
        public RectTransform gp_con;
        public Image _img_bg;
        public TextMeshProUGUI _lb_name;
        public Image _img_final;
        public Image _img_red;

        protected override void BindNodes()
        {
            EnsureBound(nameof(gp_con), gp_con);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_img_final), _img_final);
            EnsureBound(nameof(_img_red), _img_red);
        }
    }
}
