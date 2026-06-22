// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonCommon/DungeonGuildHelpItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonCommon
{
    public partial class DungeonGuildHelpItemBind : BaseView
    {
        public RectTransform _box_con;
        public Image _img_bg;
        public Image _img_bg2;
        public TextMeshProUGUI _lb_title;
        public RectTransform _box_head;
        public TextMeshProUGUI _lb_name;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_box_con), _box_con);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_box_head), _box_head);
            EnsureBound(nameof(_lb_name), _lb_name);
        }
    }
}
