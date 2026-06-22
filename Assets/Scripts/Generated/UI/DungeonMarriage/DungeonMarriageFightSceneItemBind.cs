// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonMarriage/DungeonMarriageFightSceneItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonMarriage
{
    public partial class DungeonMarriageFightSceneItemBind : BaseView
    {
        public RectTransform _gp_con;
        public Image _img_bg;
        public Image _img_bg311;
        public TextMeshProUGUI _lb_title11;
        public TextMeshProUGUI _lb_level_tips;
        public Image _img_star0;
        public Image _img_star1;
        public Image _img_star2;
        public ScrollRect Content;
        public TextMeshProUGUI _lb_desc;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_gp_con), _gp_con);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg311), _img_bg311);
            EnsureBound(nameof(_lb_title11), _lb_title11);
            EnsureBound(nameof(_lb_level_tips), _lb_level_tips);
            EnsureBound(nameof(_img_star0), _img_star0);
            EnsureBound(nameof(_img_star1), _img_star1);
            EnsureBound(nameof(_img_star2), _img_star2);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_lb_desc), _lb_desc);
        }
    }
}
