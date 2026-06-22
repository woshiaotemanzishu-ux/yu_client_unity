// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonRune/DungeonRuneFirstTab.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonRune
{
    public partial class DungeonRuneFirstTabBind : BaseView
    {
        public Image _img_bg;
        public TextMeshProUGUI _lb_desc;
        public Image _img_reveived;
        public Image _img_red;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_lb_desc), _lb_desc);
            EnsureBound(nameof(_img_reveived), _img_reveived);
            EnsureBound(nameof(_img_red), _img_red);
        }
    }
}
