// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/ghostWalk/GhostWalkSceneDamageItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GhostWalk
{
    public partial class GhostWalkSceneDamageItemBind : BaseView
    {
        public Image _img_line;
        public Image _img_rank;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_hurt;
        public TextMeshProUGUI _lb_rank;
        public RectTransform _click_box;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_line), _img_line);
            EnsureBound(nameof(_img_rank), _img_rank);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_hurt), _lb_hurt);
            EnsureBound(nameof(_lb_rank), _lb_rank);
            EnsureBound(nameof(_click_box), _click_box);
        }
    }
}
