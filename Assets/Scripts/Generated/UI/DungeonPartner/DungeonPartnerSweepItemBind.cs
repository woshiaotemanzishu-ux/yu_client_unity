// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonPartner/DungeonPartnerSweepItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonPartner
{
    public partial class DungeonPartnerSweepItemBind : BaseView
    {
        public Image _img_bg;
        public Image _img_bg2;
        public TextMeshProUGUI _lb_title;
        public TextMeshProUGUI _lb_desc;
        public TextMeshProUGUI _lb_progress;
        public RectTransform _box_item;
        public ScrollRect _panel_item;
        public RectTransform _box_sweep;
        public Image _img_sweep;
        public TextMeshProUGUI _lb_sweep;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_lb_desc), _lb_desc);
            EnsureBound(nameof(_lb_progress), _lb_progress);
            EnsureBound(nameof(_box_item), _box_item);
            EnsureBound(nameof(_panel_item), _panel_item);
            EnsureBound(nameof(_box_sweep), _box_sweep);
            EnsureBound(nameof(_img_sweep), _img_sweep);
            EnsureBound(nameof(_lb_sweep), _lb_sweep);
        }
    }
}
