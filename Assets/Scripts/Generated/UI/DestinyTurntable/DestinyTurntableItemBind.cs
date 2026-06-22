// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/destinyTurntable/DestinyTurntableItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DestinyTurntable
{
    public partial class DestinyTurntableItemBind : BaseView
    {
        public RectTransform gp_effect;
        public RectTransform gp;
        public Image Image;
        public RectTransform gp_reward;
        public Image img_select;
        public Image img_get;
        public Image exp;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(gp_effect), gp_effect);
            EnsureBound(nameof(gp), gp);
            EnsureBound(nameof(Image), Image);
            EnsureBound(nameof(gp_reward), gp_reward);
            EnsureBound(nameof(img_select), img_select);
            EnsureBound(nameof(img_get), img_get);
            EnsureBound(nameof(exp), exp);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
