// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonRune/DungeonRuneDailyRewardView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonRune
{
    public partial class DungeonRuneDailyRewardViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_bg2;
        public Image _img_close;
        public Image _img_continue;
        public Image _img_red;
        public Image _img_get;
        public TextMeshProUGUI _lb_floor;
        public ScrollRect _panel_reward;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_img_continue), _img_continue);
            EnsureBound(nameof(_img_red), _img_red);
            EnsureBound(nameof(_img_get), _img_get);
            EnsureBound(nameof(_lb_floor), _lb_floor);
            EnsureBound(nameof(_panel_reward), _panel_reward);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
