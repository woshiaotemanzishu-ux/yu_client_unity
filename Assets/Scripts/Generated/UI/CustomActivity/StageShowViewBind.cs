// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/customActivity/StageShowView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.CustomActivity
{
    public partial class StageShowViewBind : BaseView
    {
        public Image bg;
        public ScrollRect scroller;
        public RectTransform _gp_reward;
        public Image arrow_down;
        public Image arrow_up;
        public Image arrow_right;
        public Image arrow_left;
        public Image arrow_centered;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(scroller), scroller);
            EnsureBound(nameof(_gp_reward), _gp_reward);
            EnsureBound(nameof(arrow_down), arrow_down);
            EnsureBound(nameof(arrow_up), arrow_up);
            EnsureBound(nameof(arrow_right), arrow_right);
            EnsureBound(nameof(arrow_left), arrow_left);
            EnsureBound(nameof(arrow_centered), arrow_centered);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
