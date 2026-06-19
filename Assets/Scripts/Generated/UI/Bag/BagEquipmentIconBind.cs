// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/bag/BagEquipmentIcon.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Bag
{
    public partial class BagEquipmentIconBind : BaseView
    {
        public RectTransform scaleGp;
        public Image icon_bg;
        public Image lock_icon;
        public Image _img_add;
        public RectTransform award_con;
        public Image red_dot;
        public RectTransform group_eff;
        public GameObject _tpl_BaseAwardItem;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(scaleGp), scaleGp);
            EnsureBound(nameof(icon_bg), icon_bg);
            EnsureBound(nameof(lock_icon), lock_icon);
            EnsureBound(nameof(_img_add), _img_add);
            EnsureBound(nameof(award_con), award_con);
            EnsureBound(nameof(red_dot), red_dot);
            EnsureBound(nameof(group_eff), group_eff);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
