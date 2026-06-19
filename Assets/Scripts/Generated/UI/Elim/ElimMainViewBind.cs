// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/elim/ElimMainView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Elim
{
    public partial class ElimMainViewBind : BaseView
    {
        public Image _img_bg;
        public Image bg3;
        public Image bg1;
        public Image bg2;
        public ScrollRect _panel_con_board;
        public RectTransform con_board;
        public Image _img_count;
        public TextMeshProUGUI _score;
        public TextMeshProUGUI _lb_myscore;
        public TextMeshProUGUI _game_score;
        public TextMeshProUGUI add_num;
        public TextMeshProUGUI _lb_gk;
        public Image _img_eff_bg;
        public Image start_img;
        public RectTransform _box_round;
        public Image _lb_round;
        public TextMeshProUGUI _count_down;
        public RectTransform _box_eff;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(bg3), bg3);
            EnsureBound(nameof(bg1), bg1);
            EnsureBound(nameof(bg2), bg2);
            EnsureBound(nameof(_panel_con_board), _panel_con_board);
            EnsureBound(nameof(con_board), con_board);
            EnsureBound(nameof(_img_count), _img_count);
            EnsureBound(nameof(_score), _score);
            EnsureBound(nameof(_lb_myscore), _lb_myscore);
            EnsureBound(nameof(_game_score), _game_score);
            EnsureBound(nameof(add_num), add_num);
            EnsureBound(nameof(_lb_gk), _lb_gk);
            EnsureBound(nameof(_img_eff_bg), _img_eff_bg);
            EnsureBound(nameof(start_img), start_img);
            EnsureBound(nameof(_box_round), _box_round);
            EnsureBound(nameof(_lb_round), _lb_round);
            EnsureBound(nameof(_count_down), _count_down);
            EnsureBound(nameof(_box_eff), _box_eff);
        }
    }
}
