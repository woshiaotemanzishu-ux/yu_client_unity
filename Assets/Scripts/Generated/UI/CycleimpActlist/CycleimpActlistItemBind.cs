// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/cycleimpActlist/CycleimpActlistItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.CycleimpActlist
{
    public partial class CycleimpActlistItemBind : BaseView
    {
        public Image _img_bg;
        public ScrollRect _Scroller1;
        public RectTransform _gp_reward;
        public TextMeshProUGUI lb_name;
        public Image img_rank;
        public TextMeshProUGUI lb_rank;
        public TextMeshProUGUI score;
        public TextMeshProUGUI stage_info;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(_gp_reward), _gp_reward);
            EnsureBound(nameof(lb_name), lb_name);
            EnsureBound(nameof(img_rank), img_rank);
            EnsureBound(nameof(lb_rank), lb_rank);
            EnsureBound(nameof(score), score);
            EnsureBound(nameof(stage_info), stage_info);
        }
    }
}
