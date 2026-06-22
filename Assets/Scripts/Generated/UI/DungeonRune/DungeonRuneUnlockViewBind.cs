// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonRune/DungeonRuneUnlockView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonRune
{
    public partial class DungeonRuneUnlockViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_bg2;
        public Image _img_bg3;
        public Image _img_bg4;
        public Image _img_rune_bg;
        public Image _img_rune_icon;
        public TextMeshProUGUI _lb_rune_name;
        public TextMeshProUGUI _lb_tips;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_img_bg3), _img_bg3);
            EnsureBound(nameof(_img_bg4), _img_bg4);
            EnsureBound(nameof(_img_rune_bg), _img_rune_bg);
            EnsureBound(nameof(_img_rune_icon), _img_rune_icon);
            EnsureBound(nameof(_lb_rune_name), _lb_rune_name);
            EnsureBound(nameof(_lb_tips), _lb_tips);
        }
    }
}
