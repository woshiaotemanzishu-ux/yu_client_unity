// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/common/CustomHeadItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Common
{
    public partial class CustomHeadItemBind : BaseView
    {
        public RectTransform click_group;
        public Image team_bg;
        public Image frame;
        public Image bg;
        public Image bg2;
        public Image icon;
        public Image icon_sys_head;
        public RectTransform _level_gp;
        public Image lv_img;
        public TextMeshProUGUI _lev_;

        protected override void BindNodes()
        {
            EnsureBound(nameof(click_group), click_group);
            EnsureBound(nameof(team_bg), team_bg);
            EnsureBound(nameof(frame), frame);
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(bg2), bg2);
            EnsureBound(nameof(icon), icon);
            EnsureBound(nameof(icon_sys_head), icon_sys_head);
            EnsureBound(nameof(_level_gp), _level_gp);
            EnsureBound(nameof(lv_img), lv_img);
            EnsureBound(nameof(_lev_), _lev_);
        }
    }
}
