// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonRune/DungeonRuneTargetItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonRune
{
    public partial class DungeonRuneTargetItemBind : BaseView
    {
        public Image _img_bg;
        public TextMeshProUGUI _html_desc;
        public ScrollRect _panel_item;
        public Image _img_received;
        public RectTransform _box_get;
        public Image _img_get;
        public TextMeshProUGUI _lb_get;
        public Image _img_get_red;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_html_desc), _html_desc);
            EnsureBound(nameof(_panel_item), _panel_item);
            EnsureBound(nameof(_img_received), _img_received);
            EnsureBound(nameof(_box_get), _box_get);
            EnsureBound(nameof(_img_get), _img_get);
            EnsureBound(nameof(_lb_get), _lb_get);
            EnsureBound(nameof(_img_get_red), _img_get_red);
        }
    }
}
