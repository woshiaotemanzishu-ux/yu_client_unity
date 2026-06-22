// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/bossdomain/BossDomainStageShow.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Bossdomain
{
    public partial class BossDomainStageShowBind : BaseView
    {
        public Image bg;
        public ScrollRect scroller;
        public RectTransform _gp_reward;
        public Image arrow_up;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(scroller), scroller);
            EnsureBound(nameof(_gp_reward), _gp_reward);
            EnsureBound(nameof(arrow_up), arrow_up);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
