// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/cycleimpActlist/CycleimpActlistTipsView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.CycleimpActlist
{
    public partial class CycleimpActlistTipsViewBind : BaseView
    {
        public Image bg;
        public Image img_bg;
        public RectTransform _gp_item;
        public Image img_title;
        public RectTransform go;
        public Image time_gp;
        public TextMeshProUGUI time_text;
        public TextMeshProUGUI des;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(img_bg), img_bg);
            EnsureBound(nameof(_gp_item), _gp_item);
            EnsureBound(nameof(img_title), img_title);
            EnsureBound(nameof(go), go);
            EnsureBound(nameof(time_gp), time_gp);
            EnsureBound(nameof(time_text), time_text);
            EnsureBound(nameof(des), des);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
