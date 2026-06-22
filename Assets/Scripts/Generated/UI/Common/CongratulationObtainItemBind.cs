// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/common/CongratulationObtainItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Common
{
    public partial class CongratulationObtainItemBind : BaseView
    {
        public RectTransform gp_item;
        public RectTransform gp_award;
        public RectTransform gp_effect;
        public Image _img_1;
        public GameObject _tpl_BaseAwardItem;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(gp_item), gp_item);
            EnsureBound(nameof(gp_award), gp_award);
            EnsureBound(nameof(gp_effect), gp_effect);
            EnsureBound(nameof(_img_1), _img_1);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
