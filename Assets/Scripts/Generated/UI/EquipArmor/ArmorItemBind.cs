// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/equipArmor/ArmorItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.EquipArmor
{
    public partial class ArmorItemBind : BaseView
    {
        public RectTransform gp_con;
        public RectTransform _gp_item;
        public Image _img_lock;
        public Image _img_select;
        public TextMeshProUGUI _lb_tips;
        public Image _img_final;
        public Image _img_red;

        protected override void BindNodes()
        {
            EnsureBound(nameof(gp_con), gp_con);
            EnsureBound(nameof(_gp_item), _gp_item);
            EnsureBound(nameof(_img_lock), _img_lock);
            EnsureBound(nameof(_img_select), _img_select);
            EnsureBound(nameof(_lb_tips), _lb_tips);
            EnsureBound(nameof(_img_final), _img_final);
            EnsureBound(nameof(_img_red), _img_red);
        }
    }
}
