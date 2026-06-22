// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/destinyTurntable/DestinyTurnTableActiveView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DestinyTurntable
{
    public partial class DestinyTurnTableActiveViewBind : BaseView
    {
        public Image bg;
        public RectTransform _gp_item;
        public Image des;
        public RectTransform go;
        public TextMeshProUGUI count;
        public TextMeshProUGUI time_text;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(_gp_item), _gp_item);
            EnsureBound(nameof(des), des);
            EnsureBound(nameof(go), go);
            EnsureBound(nameof(count), count);
            EnsureBound(nameof(time_text), time_text);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
