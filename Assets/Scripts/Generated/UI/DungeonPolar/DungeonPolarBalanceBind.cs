// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonPolar/DungeonPolarBalance.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonPolar
{
    public partial class DungeonPolarBalanceBind : BaseView
    {
        public RectTransform _box;
        public Image _bg;
        public Image state_img;
        public RectTransform _gp_victory;
        public RectTransform _gp_tip_img;
        public Image _Image1;
        public TextMeshProUGUI _lb_tips;
        public ScrollRect _scroller;
        public RectTransform Content;
        public RectTransform _gp_fairly;
        public TextMeshProUGUI _lb_tilte;
        public TextMeshProUGUI _Label1;
        public GameObject _tpl_DungeonPolarBalanceItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_box), _box);
            EnsureBound(nameof(_bg), _bg);
            EnsureBound(nameof(state_img), state_img);
            EnsureBound(nameof(_gp_victory), _gp_victory);
            EnsureBound(nameof(_gp_tip_img), _gp_tip_img);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_lb_tips), _lb_tips);
            EnsureBound(nameof(_scroller), _scroller);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_gp_fairly), _gp_fairly);
            EnsureBound(nameof(_lb_tilte), _lb_tilte);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_tpl_DungeonPolarBalanceItem), _tpl_DungeonPolarBalanceItem);
        }
    }
}
