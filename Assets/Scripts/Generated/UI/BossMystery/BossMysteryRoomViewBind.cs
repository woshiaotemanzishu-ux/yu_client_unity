// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/bossMystery/BossMysteryRoomView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.BossMystery
{
    public partial class BossMysteryRoomViewBind : BaseView
    {
        public Image _img_bg;
        public ScrollRect _Scroller1;
        public ScrollRect _scroll_con;
        public RectTransform _Group1;
        public TextMeshProUGUI _Label1;
        public RectTransform cost_icon;
        public TextMeshProUGUI need_cost;
        public TextMeshProUGUI challenge_num;
        public TextMeshProUGUI scene_role;
        public RectTransform challengeBtn;
        public Image btn_img;
        public RectTransform lable_gp;
        public Image destiny_img;
        public TextMeshProUGUI btn_label;
        public Image _img_left;
        public Image _img_right;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(_scroll_con), _scroll_con);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(cost_icon), cost_icon);
            EnsureBound(nameof(need_cost), need_cost);
            EnsureBound(nameof(challenge_num), challenge_num);
            EnsureBound(nameof(scene_role), scene_role);
            EnsureBound(nameof(challengeBtn), challengeBtn);
            EnsureBound(nameof(btn_img), btn_img);
            EnsureBound(nameof(lable_gp), lable_gp);
            EnsureBound(nameof(destiny_img), destiny_img);
            EnsureBound(nameof(btn_label), btn_label);
            EnsureBound(nameof(_img_left), _img_left);
            EnsureBound(nameof(_img_right), _img_right);
        }
    }
}
