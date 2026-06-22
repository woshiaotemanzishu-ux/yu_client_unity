// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonRune/DungeonRuneFirstItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonRune
{
    public partial class DungeonRuneFirstItemBind : BaseView
    {
        public Image _img_bg;
        public Image _img_rank;
        public RectTransform _gp_head;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_time;
        public TextMeshProUGUI _lb_null;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_rank), _img_rank);
            EnsureBound(nameof(_gp_head), _gp_head);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_time), _lb_time);
            EnsureBound(nameof(_lb_null), _lb_null);
        }
    }
}
