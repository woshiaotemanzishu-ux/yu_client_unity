// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/bossMystery/BossMysteryRewardItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.BossMystery
{
    public partial class BossMysteryRewardItemBind : BaseView
    {
        public Image _img_bg;
        public ScrollRect _Scroller1;
        public RectTransform Content;
        public RectTransform _Group1;
        public Image _img_box;
        public TextMeshProUGUI tips;
        public Image _img_now_tips;
        public RectTransform getBtn;
        public Image btn_img;
        public TextMeshProUGUI get_label;
        public Image red_dot;
        public Image get_state;
        public Image none;
        public RectTransform reward_pos_con;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_img_box), _img_box);
            EnsureBound(nameof(tips), tips);
            EnsureBound(nameof(_img_now_tips), _img_now_tips);
            EnsureBound(nameof(getBtn), getBtn);
            EnsureBound(nameof(btn_img), btn_img);
            EnsureBound(nameof(get_label), get_label);
            EnsureBound(nameof(red_dot), red_dot);
            EnsureBound(nameof(get_state), get_state);
            EnsureBound(nameof(none), none);
            EnsureBound(nameof(reward_pos_con), reward_pos_con);
        }
    }
}
