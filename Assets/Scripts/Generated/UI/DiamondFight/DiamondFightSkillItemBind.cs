// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/diamondFight/DiamondFightSkillItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DiamondFight
{
    public partial class DiamondFightSkillItemBind : BaseView
    {
        public Image _img_bg;
        public Image _img_bg2;
        public Image _img_skill_icon;
        public TextMeshProUGUI _html_desc;
        public RectTransform _box_state0;
        public RectTransform _box_use;
        public Image _img_use;
        public TextMeshProUGUI _lb_use;
        public RectTransform _box_con;
        public TextMeshProUGUI _lb_count;
        public Image _img_diamond;
        public RectTransform _box_state1;
        public TextMeshProUGUI _lb_cd_time;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_img_skill_icon), _img_skill_icon);
            EnsureBound(nameof(_html_desc), _html_desc);
            EnsureBound(nameof(_box_state0), _box_state0);
            EnsureBound(nameof(_box_use), _box_use);
            EnsureBound(nameof(_img_use), _img_use);
            EnsureBound(nameof(_lb_use), _lb_use);
            EnsureBound(nameof(_box_con), _box_con);
            EnsureBound(nameof(_lb_count), _lb_count);
            EnsureBound(nameof(_img_diamond), _img_diamond);
            EnsureBound(nameof(_box_state1), _box_state1);
            EnsureBound(nameof(_lb_cd_time), _lb_cd_time);
        }
    }
}
